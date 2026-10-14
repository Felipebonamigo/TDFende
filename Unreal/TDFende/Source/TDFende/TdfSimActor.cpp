#include "TdfSimActor.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Misc/FileHelper.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "HAL/PlatformMisc.h"
#include "HAL/PlatformTime.h"
#include "UObject/ConstructorHelpers.h"
#include "UnrealClient.h"
#include "Kismet/KismetSystemLibrary.h"

DEFINE_LOG_CATEGORY_STATIC(LogTdf, Log, All);

static constexpr float FixedStep = 1.f / 30.f; // o passo fixo da Sim (TowerWarsConfig.FixedStep)

namespace
{
	const FLinearColor TowerColors[6] = {
		FLinearColor(0.55f, 0.58f, 0.65f), // canhão
		FLinearColor(0.55f, 0.38f, 0.22f), // morteiro
		FLinearColor(0.35f, 0.75f, 1.00f), // gelo
		FLinearColor(0.95f, 0.88f, 0.40f), // sentinela
		FLinearColor(1.00f, 0.38f, 0.10f), // fogo
		FLinearColor(0.65f, 0.92f, 0.65f), // ar
	};
	const FLinearColor EnemyColors[4] = {
		FLinearColor(0.85f, 0.12f, 0.10f), // normal
		FLinearColor(0.30f, 0.80f, 0.95f), // lento / congelado
		FLinearColor(1.00f, 0.55f, 0.05f), // queimando
		FLinearColor(0.70f, 0.30f, 0.95f), // voador
	};

	const TCHAR* RejectText(int32 Reason)
	{
		switch (Reason)
		{
		case 1: return TEXT("Partida decidida");
		case 2: return TEXT("Tipo desconhecido");
		case 3: return TEXT("Ouro insuficiente");
		case 4: return TEXT("Célula inválida");
		case 5: return TEXT("Tem inimigo em cima");
		case 6: return TEXT("Fecharia o caminho dos inimigos");
		case 7: return TEXT("Sem torre aqui");
		case 8: return TEXT("Torre já está no nível máximo");
		default: return TEXT("Comando recusado");
		}
	}
}

ATdfSimActor::ATdfSimActor()
{
	PrimaryActorTick.bCanEverTick = true;
	RootScene = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
	SetRootComponent(RootScene);

	static ConstructorHelpers::FObjectFinder<UStaticMesh> Cube(TEXT("/Engine/BasicShapes/Cube.Cube"));
	static ConstructorHelpers::FObjectFinder<UStaticMesh> Cylinder(TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
	static ConstructorHelpers::FObjectFinder<UStaticMesh> Sphere(TEXT("/Engine/BasicShapes/Sphere.Sphere"));

	auto Make = [this](const FString& Name, UStaticMesh* Mesh) -> UInstancedStaticMeshComponent*
	{
		UInstancedStaticMeshComponent* C = CreateDefaultSubobject<UInstancedStaticMeshComponent>(*Name);
		C->SetupAttachment(RootScene);
		C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
		C->SetCastShadow(false);
		C->SetStaticMesh(Mesh);
		return C;
	};
	for (int32 i = 0; i < EnemyKinds; ++i) EnemyMesh[i] = Make(FString::Printf(TEXT("Inimigos%d"), i), Cube.Object);
	for (int32 i = 0; i < TowerKinds; ++i) TowerMesh[i] = Make(FString::Printf(TEXT("Torres%d"), i), Cylinder.Object);
	ShotMesh = Make(TEXT("Tiros"), Sphere.Object);
	TerritoryMesh = Make(TEXT("Territorio"), Cube.Object);
}

void ATdfSimActor::BeginPlay()
{
	Super::BeginPlay();

	// cores: o material básico da engine tem o parâmetro "Color"
	UMaterialInterface* Base = LoadObject<UMaterialInterface>(nullptr, TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial"));
	auto Paint = [this, Base](UInstancedStaticMeshComponent* C, const FLinearColor& Color)
	{
		if (!Base || !C) return;
		UMaterialInstanceDynamic* M = UMaterialInstanceDynamic::Create(Base, this);
		M->SetVectorParameterValue(TEXT("Color"), Color);
		C->SetMaterial(0, M);
		Mids.Add(M);
	};
	for (int32 i = 0; i < EnemyKinds; ++i) Paint(EnemyMesh[i], EnemyColors[i]);
	for (int32 i = 0; i < TowerKinds; ++i) Paint(TowerMesh[i], TowerColors[i]);
	Paint(ShotMesh, FLinearColor(1.f, 0.95f, 0.3f));
	Paint(TerritoryMesh, FLinearColor(0.10f, 0.20f, 0.36f));

	FString Err;
	if (!Sim.Load(Err)) { UE_LOG(LogTdf, Error, TEXT("Ponte: %s"), *Err); return; }

	FParse::Value(FCommandLine::Get(), TEXT("TdfReplay="), ReplayPath);
	FParse::Value(FCommandLine::Get(), TEXT("TdfShot="), ShotPath);
	FParse::Value(FCommandLine::Get(), TEXT("TdfShotTick="), ShotTick);
	FParse::Value(FCommandLine::Get(), TEXT("TdfQuitTick="), QuitTick);
	bQuitAtEnd = FParse::Param(FCommandLine::Get(), TEXT("TdfQuit"));
	bSelfTest = FParse::Param(FCommandLine::Get(), TEXT("TdfSelfTest"));
	if (!ReplayPath.IsEmpty())
	{
		if (LoadReplay(ReplayPath)) bReplaying = true;
		else UE_LOG(LogTdf, Error, TEXT("Não consegui ler o replay %s"), *ReplayPath);
	}

	if (Sim.create(Seed, Difficulty, GridWidth, GridHeight, nullptr, nullptr) != 0)
	{
		UE_LOG(LogTdf, Error, TEXT("tdf_create falhou: %s"), *Sim.LastError());
		Sim.Unload();
		return;
	}
	char Sig[128] = {};
	Sim.signature(Sig, sizeof(Sig));
	UE_LOG(LogTdf, Log, TEXT("Ponte ok. Assinatura: %s"), UTF8_TO_TCHAR(Sig));
	LoadCatalog();

	EnemyBuf.SetNum(4096); TowerBuf.SetNum(1024); ShotBuf.SetNum(1024); EventBuf.SetNum(4096);
	TerritoryBuf.SetNumZeroed(2 * GridWidth * GridHeight);
	bRunning = true;
}

void ATdfSimActor::EndPlay(const EEndPlayReason::Type Reason)
{
	SaveRecording();
	Sim.Unload();
	bRunning = false;
	Super::EndPlay(Reason);
}

void ATdfSimActor::LoadCatalog()
{
	auto Parse = [this](int32 Kind, TArray<FTdfCatalogItem>& Out)
	{
		Out.Reset();
		TArray<FString> Lines;
		Sim.CatalogText(Kind).ParseIntoArrayLines(Lines);
		for (const FString& Line : Lines)
		{
			TArray<FString> P;
			Line.ParseIntoArray(P, TEXT(";"), false);
			if (P.Num() < 3) continue;
			FTdfCatalogItem It;
			It.Key = P[0]; It.Name = P[1]; It.Cost = FCString::Atoi(*P[2]);
			for (int32 i = 0; i < 6 && 3 + i < P.Num(); ++i) It.F[i] = FCString::Atof(*P[3 + i]);
			Out.Add(It);
		}
	};
	Parse(0, TowerTypes);
	Parse(1, SendTypes);
	UE_LOG(LogTdf, Log, TEXT("Catálogo: %d torres, %d envios"), TowerTypes.Num(), SendTypes.Num());
}

bool ATdfSimActor::LoadReplay(const FString& Path)
{
	FString Text;
	if (!FFileHelper::LoadFileToString(Text, *Path)) return false;
	TArray<FString> Lines;
	Text.ParseIntoArrayLines(Lines);
	for (const FString& Line : Lines)
	{
		TArray<FString> P;
		Line.TrimStartAndEnd().ParseIntoArray(P, TEXT(" "), true);
		if (P.Num() < 2) continue;
		if (P[0] == TEXT("seed")) { Seed = FCString::Atoi(*P[1]); continue; }
		if (P[0] == TEXT("difficulty")) { Difficulty = P[1].StartsWith(TEXT("F")) ? 0 : P[1].StartsWith(TEXT("Di")) ? 2 : 1; continue; }
		if (P[0] == TEXT("grid") && P.Num() >= 3) { GridWidth = FCString::Atoi(*P[1]); GridHeight = FCString::Atoi(*P[2]); continue; }
		if (P[0] == TEXT("ticks")) { ReplayTicks = FCString::Atoi(*P[1]); continue; }
		if (!P[0].IsNumeric()) continue; // sends/towers/rules/final e o cabeçalho
		FReplayCmd C{FCString::Atoi(*P[0]), -1, 0, 0, 0};
		if (P[1] == TEXT("build") && P.Num() >= 4) { C.Kind = TDF_CMD_BUILD; C.X = FCString::Atoi(*P[2]); C.Y = FCString::Atoi(*P[3]); C.Arg = P.Num() >= 5 ? FCString::Atoi(*P[4]) : 0; }
		else if (P[1] == TEXT("upgrade") && P.Num() >= 4) { C.Kind = TDF_CMD_UPGRADE; C.X = FCString::Atoi(*P[2]); C.Y = FCString::Atoi(*P[3]); }
		else if (P[1] == TEXT("sell") && P.Num() >= 4) { C.Kind = TDF_CMD_SELL; C.X = FCString::Atoi(*P[2]); C.Y = FCString::Atoi(*P[3]); }
		else if (P[1] == TEXT("send") && P.Num() >= 3) { C.Kind = TDF_CMD_SEND; C.Arg = FCString::Atoi(*P[2]); }
		if (C.Kind >= 0) Replay.Add(C);
	}
	return true;
}

// ------------------------------------------------------------------ comandos do jogador

void ATdfSimActor::Issue(int32 Kind, int32 X, int32 Y, int32 Arg, const TCHAR* Word)
{
	Sim.enqueue(Kind, X, Y, Arg);
	// formato do replay: "<tique> build x y tipo" | "upgrade x y" | "sell x y" | "send id"
	const int32 T = Sim.tick();
	if (Kind == TDF_CMD_BUILD) Recorded.Add(FString::Printf(TEXT("%d %s %d %d %d"), T, Word, X, Y, Arg));
	else if (Kind == TDF_CMD_SEND) Recorded.Add(FString::Printf(TEXT("%d %s %d"), T, Word, Arg));
	else Recorded.Add(FString::Printf(TEXT("%d %s %d %d"), T, Word, X, Y));
}

bool ATdfSimActor::BuildAt(int32 CellX, int32 CellY)
{
	if (!bRunning || bReplaying || !CellInBounds(CellX, CellY)) return false;
	Issue(TDF_CMD_BUILD, CellX, CellY, SelectedTower, TEXT("build"));
	return true;
}

bool ATdfSimActor::UpgradeAt(int32 CellX, int32 CellY)
{
	if (!bRunning || bReplaying || !CellInBounds(CellX, CellY)) return false;
	Issue(TDF_CMD_UPGRADE, CellX, CellY, 0, TEXT("upgrade"));
	return true;
}

bool ATdfSimActor::SellAt(int32 CellX, int32 CellY)
{
	if (!bRunning || bReplaying || !CellInBounds(CellX, CellY)) return false;
	Issue(TDF_CMD_SELL, CellX, CellY, 0, TEXT("sell"));
	return true;
}

bool ATdfSimActor::SendUnit(int32 SendId)
{
	if (!bRunning || bReplaying || SendId < 0 || SendId >= SendTypes.Num()) return false;
	Issue(TDF_CMD_SEND, 0, 0, SendId, TEXT("send"));
	return true;
}

void ATdfSimActor::CycleTower(int32 Delta)
{
	if (TowerTypes.Num() == 0) return;
	SelectedTower = (SelectedTower + Delta + TowerTypes.Num()) % TowerTypes.Num();
}

void ATdfSimActor::SaveRecording() const
{
	if (Recorded.Num() == 0 || bReplaying) return;
	FString Out = FString::Printf(TEXT("tdfende-replay 2\nseed %d\ndifficulty %s\ngrid %d %d\nticks %d\n"), Seed,
		Difficulty == 0 ? TEXT("Facil") : Difficulty == 2 ? TEXT("Dificil") : TEXT("Normal"), GridWidth, GridHeight, Sim.IsLoaded() ? Sim.tick() : 0);
	for (const FString& L : Recorded) Out += L + TEXT("\n");
	const FString Path = FPaths::Combine(FPaths::ProjectSavedDir(), TEXT("Tdf"), TEXT("partida.txt"));
	FFileHelper::SaveStringToFile(Out, *Path);
	UE_LOG(LogTdf, Display, TEXT("Partida gravada (%d comandos): %s"), Recorded.Num(), *Path);
}

// ------------------------------------------------------------------ perguntas

int32 ATdfSimActor::TowerLevelAt(int32 CellX, int32 CellY) const
{
	for (int32 i = 0; i < TowerCountNow; ++i)
		if (TowerBuf[i].Lane == 0 && TowerBuf[i].CellX == CellX && TowerBuf[i].CellY == CellY) return TowerBuf[i].Level;
	return 0;
}

int32 ATdfSimActor::CanBuildSelected(int32 CellX, int32 CellY) const
{
	if (!bRunning || !Sim.can_build) return 4;
	return Sim.can_build(CellX, CellY, SelectedTower);
}

int32 ATdfSimActor::UpgradeCost(int32 CellX, int32 CellY) const
{
	if (!bRunning || !Sim.upgrade_cost) return -1;
	return Sim.upgrade_cost(CellX, CellY);
}

bool ATdfSimActor::InsideTerritory(int32 CellX, int32 CellY) const
{
	if (!CellInBounds(CellX, CellY)) return false;
	const int32 I = CellY * GridWidth + CellX;
	return TerritoryBuf.IsValidIndex(I) && TerritoryBuf[I] != 0;
}

FVector ATdfSimActor::CellToWorld(int32 CellX, int32 CellY, float Z) const
{
	// o centro da célula (x, y) na Sim é ((x + 0.5) - W/2, (y + 0.5) - H/2) em células; Unreal: X = z, Y = x
	const float SimX = (CellX + 0.5f) - GridWidth * 0.5f;
	const float SimZ = (CellY + 0.5f) - GridHeight * 0.5f;
	return FVector(SimZ * CellSizeCm, SimX * CellSizeCm, Z);
}

FIntPoint ATdfSimActor::WorldToCell(const FVector& World) const
{
	const int32 Cx = FMath::FloorToInt(World.Y / CellSizeCm + GridWidth * 0.5f);
	const int32 Cy = FMath::FloorToInt(World.X / CellSizeCm + GridHeight * 0.5f);
	return CellInBounds(Cx, Cy) ? FIntPoint(Cx, Cy) : FIntPoint(-1, -1);
}

FVector ATdfSimActor::ToUnreal(int32 Lane, float X, float Y, float Z) const
{
	const float LaneOffsetX = Lane * (GridHeight + 4) * CellSizeCm;
	return FVector(Z * CellSizeCm + LaneOffsetX, X * CellSizeCm, Y * CellSizeCm);
}

// ------------------------------------------------------------------ passo e desenho

void ATdfSimActor::StepOnce()
{
	const int32 T = Sim.tick();
	while (NextCmd < Replay.Num() && Replay[NextCmd].Tick <= T)
	{
		const FReplayCmd& C = Replay[NextCmd++];
		Sim.enqueue(C.Kind, C.X, C.Y, C.Arg);
	}
	Sim.step(1);
	if (ReplayTicks > 0 && Sim.tick() == ReplayTicks)
	{
		UE_LOG(LogTdf, Display, TEXT("FINGERPRINT (compare com o FlowSim): %s"), *Sim.Fingerprint());
		ReportCost();
		if (bQuitAtEnd) UKismetSystemLibrary::QuitGame(this, nullptr, EQuitPreference::Quit, false);
	}
	if (!ShotPath.IsEmpty() && ShotTick > 0 && Sim.tick() == ShotTick)
	{
		FScreenshotRequest::RequestScreenshot(ShotPath, false, false);
		UE_LOG(LogTdf, Display, TEXT("Print pedido no tique %d: %s"), ShotTick, *ShotPath);
	}
	if (QuitTick > 0 && Sim.tick() == QuitTick) UKismetSystemLibrary::QuitGame(this, nullptr, EQuitPreference::Quit, false);
}

void ATdfSimActor::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (!bRunning) return;
	ToastAge += DeltaSeconds;

	// custo da ponte por quadro: tudo o que este Tick faz com a Sim (passos, snapshot, cópia, instâncias, eventos)
	const double T0 = FPlatformTime::Seconds();
	Accumulator += FMath::Min(DeltaSeconds, 0.25f);
	while (Accumulator >= FixedStep && !Sim.over())
	{
		Accumulator -= FixedStep;
		StepOnce();
		if (ReplayTicks > 0 && Sim.tick() >= ReplayTicks) { Accumulator = 0.f; break; } // replay acabou: congela
	}
	const double T1 = FPlatformTime::Seconds();
	Redraw();
	const int32 NEv = FMath::Min<int32>(Sim.events(EventBuf.GetData(), EventBuf.Num()), EventBuf.Num());
	for (int32 i = 0; i < NEv; ++i)
	{
		const TdfEvent& E = EventBuf[i];
		if (E.Kind == TDF_EV_COMMAND_REJECTED) ToastMsg(RejectText(E.B));
		else if (E.Kind == TDF_EV_MATCH_ENDED) ToastMsg(E.A == 0 ? TEXT("Você perdeu a partida") : TEXT("Você venceu a partida!"));
	}
	const double T2 = FPlatformTime::Seconds();
	if (ReplayTicks == 0 || Sim.tick() < ReplayTicks)
	{
		CostStepMs.Add(float((T1 - T0) * 1000.0));
		CostRedrawMs.Add(float((T2 - T1) * 1000.0));
		CostTotalMs.Add(float((T2 - T0) * 1000.0));
		MaxEnemies = FMath::Max(MaxEnemies, LastEnemies);
	}
	if (bSelfTest) RunSelfTest(DeltaSeconds);
}

static void LogCostStats(const TCHAR* Name, TArray<float> V)
{
	if (V.Num() == 0) return;
	V.Sort();
	double Sum = 0; for (float X : V) Sum += X;
	auto Pct = [&V](float P) { return V[FMath::Clamp(int32(P * V.Num()), 0, V.Num() - 1)]; };
	UE_LOG(LogTdf, Display, TEXT("CUSTO %s (ms/quadro, %d quadros): média %.3f  p50 %.3f  p95 %.3f  p99 %.3f  máx %.3f"),
		Name, V.Num(), Sum / V.Num(), Pct(0.5f), Pct(0.95f), Pct(0.99f), V.Last());
}

void ATdfSimActor::ReportCost() const
{
	LogCostStats(TEXT("step"), CostStepMs);
	LogCostStats(TEXT("Redraw+events"), CostRedrawMs);
	LogCostStats(TEXT("TOTAL"), CostTotalMs);
	UE_LOG(LogTdf, Display, TEXT("Máximo de inimigos vivos no snapshot: %d"), MaxEnemies);
}

void ATdfSimActor::Redraw()
{
	Sim.snapshot();
	Sim.lanes(Lanes);
	LastEnemies = Sim.enemies(EnemyBuf.GetData(), EnemyBuf.Num());
	const int32 NE = FMath::Min<int32>(LastEnemies, EnemyBuf.Num());
	const int32 NT = FMath::Min<int32>(Sim.towers(TowerBuf.GetData(), TowerBuf.Num()), TowerBuf.Num());
	const int32 NS = FMath::Min<int32>(Sim.projectiles(ShotBuf.GetData(), ShotBuf.Num()), ShotBuf.Num());
	EnemyCountNow = NE; TowerCountNow = NT;

	for (int32 k = 0; k < EnemyKinds; ++k) EnemyMesh[k]->ClearInstances();
	for (int32 i = 0; i < NE; ++i)
	{
		const TdfEnemy& E = EnemyBuf[i];
		const int32 Kind = (E.Flags & TDF_ENEMY_FLYING) ? 3 : (E.Flags & TDF_ENEMY_BURNING) ? 2 : (E.Flags & (TDF_ENEMY_FROZEN | TDF_ENEMY_SLOWED)) ? 1 : 0;
		const float Scale = 0.25f * (0.6f + 0.8f * FMath::Clamp(E.MaxHp / 400.f, 0.f, 1.f));
		EnemyMesh[Kind]->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(E.Lane, E.X, E.Y, E.Z), FVector(Scale)));
	}
	for (int32 k = 0; k < TowerKinds; ++k) TowerMesh[k]->ClearInstances();
	for (int32 i = 0; i < NT; ++i)
	{
		const TdfTower& T = TowerBuf[i];
		TowerMesh[FMath::Clamp(T.TypeId, 0, TowerKinds - 1)]->AddInstance(
			FTransform(FRotator::ZeroRotator, ToUnreal(T.Lane, T.X, T.Y + 0.4f, T.Z), FVector(0.35f, 0.35f, 0.4f + 0.1f * T.Level)));
	}
	ShotMesh->ClearInstances();
	for (int32 i = 0; i < NS; ++i)
	{
		const TdfProjectile& S = ShotBuf[i];
		ShotMesh->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(S.Lane, S.X, S.Y, S.Z), FVector(0.08f)));
	}

	// território: só reconstrói as instâncias quando muda (muda quando uma torre entra, sobe ou sai)
	TArray<uint8> Now;
	Now.SetNumZeroed(2 * GridWidth * GridHeight);
	Sim.territory(Now.GetData(), Now.Num());
	if (Now != TerritoryBuf || TerritoryMesh->GetInstanceCount() == 0)
	{
		TerritoryBuf = Now;
		TerritoryMesh->ClearInstances();
		for (int32 Lane = 0; Lane < 2; ++Lane)
			for (int32 Y = 0; Y < GridHeight; ++Y)
				for (int32 X = 0; X < GridWidth; ++X)
					if (TerritoryBuf[Lane * GridWidth * GridHeight + Y * GridWidth + X])
					{
						const FVector P = CellToWorld(X, Y, 1.f) + FVector(Lane * (GridHeight + 4) * CellSizeCm, 0.f, 0.f);
						TerritoryMesh->AddInstance(FTransform(FRotator::ZeroRotator, P, FVector(CellSizeCm * 0.0096f, CellSizeCm * 0.0096f, 0.02f)));
					}
	}
}

// ------------------------------------------------------------------ autoteste (-TdfSelfTest)

void ATdfSimActor::SelfCheck(bool bCondition, const TCHAR* What)
{
	UE_LOG(LogTdf, Display, TEXT("AUTOTESTE %s: %s"), bCondition ? TEXT("OK   ") : TEXT("FALHOU"), What);
	if (!bCondition) ++SelfFailures;
}

void ATdfSimActor::RunSelfTest(float)
{
	const int32 T = Sim.tick();
	if (T < SelfWaitUntilTick || T < 5) return;
	const int32 Cx = 9, Cy = 6;
	switch (SelfStep)
	{
	case 0:
	{
		SelfCheck(TowerTypes.Num() == 6 && SendTypes.Num() == 9, TEXT("catálogo chegou pela ponte (6 torres, 9 envios)"));
		SelfCheck(CanBuildSelected(Cx, Cy) == 0, TEXT("CanBuild: lugar livre com ouro"));
		SelfCheck(CanBuildSelected(-1, 0) != 0, TEXT("CanBuild: fora do mapa é recusado"));
		SelfCheck(!BuildAt(-1, 0), TEXT("BuildAt fora do mapa nem entra na fila"));
		// conversão célula <-> mundo: ida e volta em todas as células, e o centro cai dentro da célula certa
		bool bRound = true;
		for (int32 Y = 0; Y < GridHeight; ++Y)
			for (int32 X = 0; X < GridWidth; ++X)
				if (WorldToCell(CellToWorld(X, Y)) != FIntPoint(X, Y)) bRound = false;
		SelfCheck(bRound, TEXT("WorldToCell(CellToWorld(c)) == c em todas as células"));
		SelfCheck(WorldToCell(FVector(GridHeight * CellSizeCm, 0.f, 0.f)) == FIntPoint(-1, -1), TEXT("WorldToCell fora da grade devolve (-1,-1)"));
		SelfGoldBefore = Lanes[0].Gold;
		SelectTower(2); // gelo
		SelfCheck(BuildAt(Cx, Cy), TEXT("BuildAt aceito"));
		SelfWaitUntilTick = T + 4;
		break;
	}
	case 1:
		SelfCheck(TowerLevelAt(Cx, Cy) == 1, TEXT("a torre apareceu no snapshot, nível 1"));
		SelfCheck(Lanes[0].Gold < SelfGoldBefore + 10, TEXT("o ouro baixou (construção paga)"));
		SelfCheck(UpgradeCost(Cx, Cy) > 0, TEXT("UpgradeCost > 0"));
		SelfCheck(CanBuildSelected(Cx, Cy) != 0, TEXT("a célula ocupada não aceita outra torre"));
		SelfCheck(InsideTerritory(Cx, Cy), TEXT("a torre projeta território sobre a própria célula"));
		UpgradeAt(Cx, Cy);
		SelfWaitUntilTick = T + 4;
		break;
	case 2:
		SelfCheck(TowerLevelAt(Cx, Cy) == 2, TEXT("UpgradeAt: nível 2"));
		SelfGoldBefore = Lanes[0].TotalSent;
		SelfCheck(SendUnit(1), TEXT("SendUnit aceito"));
		SelfCheck(!SendUnit(99), TEXT("SendUnit com id inexistente nem entra na fila"));
		SelfWaitUntilTick = T + 4;
		break;
	case 3:
		SelfCheck(Lanes[0].TotalSent > SelfGoldBefore, TEXT("o envio contou (TotalSent subiu)"));
		SellAt(Cx, Cy);
		SelfWaitUntilTick = T + 4;
		break;
	case 4:
		SelfCheck(TowerLevelAt(Cx, Cy) == 0, TEXT("SellAt: a torre saiu"));
		SelfCheck(Recorded.Num() == 4, TEXT("os 4 comandos válidos foram gravados para replay"));
		UE_LOG(LogTdf, Display, TEXT("AUTOTESTE %s (%d falha(s))"), SelfFailures == 0 ? TEXT("PASSOU") : TEXT("REPROVOU"), SelfFailures);
		bSelfTest = false;
		FPlatformMisc::RequestExitWithStatus(true, SelfFailures == 0 ? 0 : 1);
		return;
	}
	++SelfStep;
}
