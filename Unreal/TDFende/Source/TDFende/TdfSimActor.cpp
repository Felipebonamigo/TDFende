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
#include "Components/SkeletalMeshComponent.h"
#include "Engine/SkeletalMesh.h"
#include "Animation/AnimSequence.h"

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
	static ConstructorHelpers::FObjectFinder<UStaticMesh> Plane(TEXT("/Engine/BasicShapes/Plane.Plane"));

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
	TerritoryMesh = Make(TEXT("Territorio"), Plane.Object);
	TerritoryMesh1 = Make(TEXT("Territorio1"), Plane.Object);
}

void ATdfSimActor::BeginPlay()
{
	Super::BeginPlay();
	// CellToWorld/WorldToCell, os bichos (espaço do mundo) e as instâncias (espaço local) só concordam com o ator na origem
	SetActorLocationAndRotation(FVector::ZeroVector, FRotator::ZeroRotator);

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
	// território: véu translúcido sobre o chão (M_TdfTerritorio, do importa_ambiente.py); azul é seu, vermelho é da IA
	if (UMaterialInterface* Veil = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/TDFende/Ambiente/Materiais/M_TdfTerritorio.M_TdfTerritorio")))
	{
		const FLinearColor LaneColor[2] = { FLinearColor(0.20f, 0.55f, 1.0f), FLinearColor(1.0f, 0.32f, 0.22f) };
		UInstancedStaticMeshComponent* Veils[2] = { TerritoryMesh, TerritoryMesh1 };
		for (int32 i = 0; i < 2; ++i)
		{
			UMaterialInstanceDynamic* M = UMaterialInstanceDynamic::Create(Veil, this);
			M->SetVectorParameterValue(TEXT("Cor"), LaneColor[i]);
			M->SetScalarParameterValue(TEXT("Opacidade"), 0.16f);
			Veils[i]->SetMaterial(0, M);
			Veils[i]->SetCastShadow(false);
			Mids.Add(M);
		}
	}
	else
	{
		Paint(TerritoryMesh, FLinearColor(0.10f, 0.20f, 0.36f));
		Paint(TerritoryMesh1, FLinearColor(0.36f, 0.12f, 0.10f));
	}

	FString Err;
	if (!Sim.Load(Err)) { UE_LOG(LogTdf, Error, TEXT("Ponte: %s"), *Err); return; }

	FParse::Value(FCommandLine::Get(), TEXT("TdfReplay="), ReplayPath);
	FParse::Value(FCommandLine::Get(), TEXT("TdfShot="), ShotPath);
	FParse::Value(FCommandLine::Get(), TEXT("TdfShotTick="), ShotTick);
	FParse::Value(FCommandLine::Get(), TEXT("TdfQuitTick="), QuitTick);
	FParse::Value(FCommandLine::Get(), TEXT("TdfDificuldade="), Difficulty); // 0 Fácil, 1 Normal, 2 Difícil
	FParse::Value(FCommandLine::Get(), TEXT("TdfQuitSeconds="), QuitSeconds);
	bQuitAtEnd = FParse::Param(FCommandLine::Get(), TEXT("TdfQuit"));
	bSelfTest = FParse::Param(FCommandLine::Get(), TEXT("TdfSelfTest"));
	bUseArt = !FParse::Param(FCommandLine::Get(), TEXT("TdfSemArte"));
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
	UE_LOG(LogTdf, Log, TEXT("Ponte ok. Assinatura: %s (semente %d, dificuldade %d)"), UTF8_TO_TCHAR(Sig), Seed, Difficulty);
	LoadCatalog();
	if (FParse::Param(FCommandLine::Get(), TEXT("TdfPreview"))) BuildPreview();
	else if (bUseArt)
	{
		// pré-carrega a arte de todos os bichos e torres agora: carregar na hora em que o primeiro aparece travava um quadro (~100 ms)
		for (int32 i = 0; i < SendTypes.Num(); ++i)
			if (TypeVisual(i).Mesh)
			{
				USkeletalMeshComponent* C = AcquireComp(i); // cria um e mede a escala
				C->SetVisibility(false);
				C->SetComponentTickEnabled(false);
				Pool.FindOrAdd(i).Add(C);
			}
		for (int32 t = 0; t < TowerTypes.Num(); ++t)
			for (int32 s = 1; s <= 3; ++s) TowerIsm(t, s);
	}

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

FString ATdfSimActor::RecordingPath()
{
	return FPaths::Combine(FPaths::ProjectSavedDir(), TEXT("Tdf"), TEXT("partida.txt"));
}

void ATdfSimActor::SaveRecording() const
{
	if (Recorded.Num() == 0 || bReplaying || !Sim.IsLoaded()) return;
	// mesmo formato do Replay.Serialize (C#): o FlowSim reproduz e confere o estado final ("final")
	char Sig[192] = {};
	Sim.signature(Sig, sizeof(Sig));
	TArray<FString> Parts;
	FString(UTF8_TO_TCHAR(Sig)).ParseIntoArray(Parts, TEXT(" "), true);
	FString Out = FString::Printf(TEXT("tdfende-replay 2\nseed %d\ndifficulty %s\ngrid %d %d\nticks %d\n"), Seed,
		Difficulty == 0 ? TEXT("Fácil") : Difficulty == 2 ? TEXT("Difícil") : TEXT("Normal"), GridWidth, GridHeight, Sim.tick());
	if (Parts.Num() == 3)
		Out += FString::Printf(TEXT("sends %s\ntowers %s\nrules %s\n"), *Parts[0], *Parts[1], *Parts[2]);
	Out += FString::Printf(TEXT("final %s\n"), *Sim.Fingerprint());
	for (const FString& L : Recorded) Out += L + TEXT("\n");
	const FString Path = RecordingPath();
	FFileHelper::SaveStringToFile(Out, *Path, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
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
	if (bReplaying && !bFingerprintLogged && ((ReplayTicks > 0 && Sim.tick() == ReplayTicks) || Sim.over()))
	{
		bFingerprintLogged = true;
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
	LastDeltaSeconds = DeltaSeconds;
	WallTime += DeltaSeconds; FpsWindow += DeltaSeconds; ++FpsFrames;
	if (FpsWindow >= 5.f)
	{
		UE_LOG(LogTdf, Display, TEXT("FPS %.1f (quadro medio %.1f ms) no tique %d"), FpsFrames / FpsWindow, 1000.f * FpsWindow / FpsFrames, Sim.tick());
		FpsWindow = 0.f; FpsFrames = 0;
	}
	if (QuitSeconds > 0.f && WallTime >= QuitSeconds) UKismetSystemLibrary::QuitGame(this, nullptr, EQuitPreference::Quit, false);

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

// ------------------------------------------------------------------ malhas reais

namespace
{
	/** Altura de cada bicho no jogo, em cm (as do Unity: AnimalLoader/ModelLib, 1 celula = 100 cm). */
	float TargetHeightCm(const FString& Key)
	{
		static const TMap<FString, float> H = {
			{TEXT("rato"), 15.f}, {TEXT("cachorro"), 43.f}, {TEXT("lobo"), 47.f}, {TEXT("javali"), 39.f}, {TEXT("aguia"), 90.f},
			{TEXT("urso"), 57.f}, {TEXT("tigre"), 54.f}, {TEXT("rinoceronte"), 62.f}, {TEXT("elefante"), 196.f}};
		const float* F = H.Find(Key);
		return F ? *F : 50.f;
	}
	int32 StageForLevel(int32 Level) { return Level <= 2 ? 1 : Level <= 4 ? 2 : 3; } // TowerStages.ForLevel
}

ATdfSimActor::FTypeVisual& ATdfSimActor::TypeVisual(int32 Type)
{
	FTypeVisual& V = TypeVisuals.FindOrAdd(Type);
	if (V.bTried) return V;
	V.bTried = true;
	if (!SendTypes.IsValidIndex(Type)) return V;
	const FString& Key = SendTypes[Type].Key;
	V.Mesh = LoadObject<USkeletalMesh>(nullptr, *FString::Printf(TEXT("/Game/TDFende/Bichos/%s.%s"), *Key, *Key));
	for (const TCHAR* Suffix : {TEXT("Bicho_Walk"), TEXT("Bicho_Run"), TEXT("_Anim")})
	{
		V.Anim = LoadObject<UAnimSequence>(nullptr, *FString::Printf(TEXT("/Game/TDFende/Bichos/%s%s.%s%s"), *Key, Suffix, *Key, Suffix));
		if (V.Anim) break;
	}
	V.TargetCm = TargetHeightCm(Key);
	if (V.Mesh) KeepAlive.Add(V.Mesh);
	if (V.Anim) KeepAlive.Add(V.Anim);
	UE_LOG(LogTdf, Display, TEXT("Arte do bicho %s: malha %s, animacao %s"), *Key, V.Mesh ? TEXT("sim") : TEXT("NAO (formas simples)"),
		V.Anim ? *V.Anim->GetName() : TEXT("nenhuma"));
	return V;
}

USkeletalMeshComponent* ATdfSimActor::AcquireComp(int32 Type)
{
	FTypeVisual& V = TypeVisual(Type);
	TArray<USkeletalMeshComponent*>& P = Pool.FindOrAdd(Type);
	if (P.Num() > 0)
	{
		USkeletalMeshComponent* C = P.Pop();
		C->SetVisibility(true);
		C->SetComponentTickEnabled(true);
		return C;
	}
	USkeletalMeshComponent* C = NewObject<USkeletalMeshComponent>(this);
	C->SetSkeletalMesh(V.Mesh);
	C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	C->SetCastShadow(true);
	C->VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption::OnlyTickPoseWhenRendered;
	C->SetAnimationMode(EAnimationMode::AnimationSingleNode);
	if (V.Anim) { C->SetAnimation(V.Anim); C->Play(true); C->SetPosition(FMath::FRand() * V.Anim->GetPlayLength()); }
	C->SetupAttachment(RootScene);
	C->RegisterComponent();
	KeepAlive.Add(C);

	if (V.Fit < 0.f)
	{
		// mede o bicho pelos ossos na pose de andar (a caixa guardada na malha mente: BUG-01 do Unity) e acha a escala
		C->TickAnimation(0.05f, false);
		C->RefreshBoneTransforms();
		FVector Lo(FLT_MAX), Hi(-FLT_MAX);
		for (int32 i = 0; i < C->GetNumBones(); ++i)
		{
			const FVector L = C->GetBoneLocation(C->GetBoneName(i), EBoneSpaces::ComponentSpace);
			Lo = Lo.ComponentMin(L); Hi = Hi.ComponentMax(L);
		}
		const FVector Size = Hi - Lo;
		// ave: escala pela envergadura (asa aberta mede pouco na altura), como o AnimalLoader do Unity
		const bool bBird = SendTypes[Type].Key == TEXT("aguia");
		const float Height = bBird ? FMath::Max(Size.X, Size.Y) : (Size.Z > 0.5f ? Size.Z : FMath::Max3(Size.X, Size.Y, Size.Z));
		V.Fit = Height > 0.01f ? V.TargetCm / Height : 1.f;
		V.GroundZ = Height > 0.01f ? (bBird ? 110.f : -Lo.Z * V.Fit) : 0.f; // ave voa a 1,1 m (Unity: 1.1)
		UE_LOG(LogTdf, Display, TEXT("Medida do bicho %s pelos ossos: %.1f x %.1f x %.1f cm -> escala %.3f (altura alvo %.0f cm)"),
			*SendTypes[Type].Key, Size.X, Size.Y, Size.Z, V.Fit, V.TargetCm);
	}
	return C;
}

bool ATdfSimActor::UpdateEnemyView(const TdfEnemy& E, bool bNewTick, float Alpha, float DeltaSeconds)
{
	FTypeVisual& V = TypeVisual(E.TypeId);
	if (!V.Mesh) return false;
	const int64 Id = (int64(E.Lane) << 48) | (int64(E.Slot) << 20) | int64(E.Generation & 0xFFFFF);
	FEnemyView& W = Views.FindOrAdd(Id);
	const FVector P = EnemyWorldPos(E);
	if (!W.Comp)
	{
		W.Comp = AcquireComp(E.TypeId);
		W.Type = E.TypeId;
		W.PrevPos = W.CurPos = P;
		W.Yaw = W.TargetYaw = 180.f; // desce para -X (a base fica em X menor)
	}
	else if (bNewTick)
	{
		W.PrevPos = W.CurPos;
		W.CurPos = P;
		const FVector D = W.CurPos - W.PrevPos;
		const float Step = D.Size2D();
		if (Step > 150.f) W.PrevPos = W.CurPos;                       // teleporte (volta para outra lane): não desliza pela tela
		else if (Step > 0.5f && Step < 60.f) W.TargetYaw = FMath::RadiansToDegrees(FMath::Atan2(D.Y, D.X)); // empurrão não vira o bicho
	}
	// a tela fica até um tique atrás da Sim e desliza entre os dois últimos (30 Hz -> qualquer taxa de quadros)
	const FVector Draw = FMath::Lerp(W.PrevPos, W.CurPos, FMath::Clamp(Alpha, 0.f, 1.f));
	W.Yaw = FMath::FixedTurn(W.Yaw, W.TargetYaw, 540.f * DeltaSeconds);
	W.Comp->SetWorldTransform(FTransform(FRotator(0.f, W.Yaw + V.YawOffset, 0.f), Draw + FVector(0.f, 0.f, V.GroundZ), FVector(V.Fit)));
	W.bSeen = true;
	return true;
}

FVector ATdfSimActor::EnemyWorldPos(const TdfEnemy& E) const
{
	// a Sim guarda o centro do corpo (y = 0,5); na vista o pé fica no chão e quem voa sobe pelo GroundZ da arte
	return ToUnreal(E.Lane, E.X, 0.f, E.Z);
}

void ATdfSimActor::ReleaseUnseenViews()
{
	for (auto It = Views.CreateIterator(); It; ++It)
	{
		if (It->Value.bSeen) { It->Value.bSeen = false; continue; }
		if (It->Value.Comp)
		{
			It->Value.Comp->SetVisibility(false);
			It->Value.Comp->SetComponentTickEnabled(false); // guardado no pool: não anima nem atualiza ossos
			Pool.FindOrAdd(It->Value.Type).Add(It->Value.Comp);
		}
		It.RemoveCurrent();
	}
}

void ATdfSimActor::BuildPreview()
{
	// bichos: uma fileira em X = +300, de frente para a camera (que olha para +X): a frente da malha tem que apontar para -X
	for (int32 i = 0; i < SendTypes.Num(); ++i)
	{
		FTypeVisual& V = TypeVisual(i);
		if (!V.Mesh) continue;
		USkeletalMeshComponent* C = AcquireComp(i);
		const FVector P(300.f, -1000.f + i * 250.f, V.GroundZ);
		C->SetWorldLocationAndRotation(P, FRotator(0.f, 180.f + V.YawOffset, 0.f));
		C->SetWorldScale3D(FVector(V.Fit));
	}
	// torres: seis tipos em Y, tres estagios em X = -250, -450, -650 (do menor para o maior, indo para longe da camera)
	for (int32 t = 0; t < TowerTypes.Num(); ++t)
	{
		FString K = TowerTypes[t].Key;
		K[0] = FChar::ToUpper(K[0]);
		for (int32 s = 1; s <= 3; ++s)
		{
			const FString Name = FString::Printf(TEXT("Torre_%s_%d"), *K, s);
			UStaticMesh* M = LoadObject<UStaticMesh>(nullptr, *FString::Printf(TEXT("/Game/TDFende/Torres/%s.%s"), *Name, *Name));
			if (!M) continue;
			UStaticMeshComponent* C = NewObject<UStaticMeshComponent>(this);
			C->SetStaticMesh(M);
			C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
			C->SetupAttachment(RootScene);
			C->RegisterComponent();
			KeepAlive.Add(C);
			C->SetWorldLocation(FVector(-50.f - s * 220.f, -600.f + t * 250.f, 0.f));
		}
	}
}

UInstancedStaticMeshComponent* ATdfSimActor::TowerIsm(int32 Type, int32 Stage)
{
	const int32 Id = Type * 4 + Stage;
	if (UInstancedStaticMeshComponent** Found = TowerIsms.Find(Id)) return *Found;
	UInstancedStaticMeshComponent* C = nullptr;
	if (TowerTypes.IsValidIndex(Type))
	{
		FString K = TowerTypes[Type].Key;
		K[0] = FChar::ToUpper(K[0]);
		const FString Name = FString::Printf(TEXT("Torre_%s_%d"), *K, Stage);
		if (UStaticMesh* M = LoadObject<UStaticMesh>(nullptr, *FString::Printf(TEXT("/Game/TDFende/Torres/%s.%s"), *Name, *Name)))
		{
			C = NewObject<UInstancedStaticMeshComponent>(this);
			C->SetStaticMesh(M);
			C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
			C->SetCastShadow(true);
			C->SetupAttachment(RootScene);
			C->RegisterComponent();
			KeepAlive.Add(C);
			KeepAlive.Add(M);
		}
		else UE_LOG(LogTdf, Display, TEXT("Sem malha para %s: formas simples"), *Name);
	}
	TowerIsms.Add(Id, C);
	return C;
}

void ATdfSimActor::Redraw()
{
	Sim.snapshot();
	Sim.lanes(Lanes);
	// as funções devolvem quantos EXISTEM: se não couber, cresce o buffer e copia de novo
	LastEnemies = Sim.enemies(EnemyBuf.GetData(), EnemyBuf.Num());
	if (LastEnemies > EnemyBuf.Num()) { EnemyBuf.SetNum(LastEnemies * 2); Sim.enemies(EnemyBuf.GetData(), EnemyBuf.Num()); }
	int32 TowersExist = Sim.towers(TowerBuf.GetData(), TowerBuf.Num());
	if (TowersExist > TowerBuf.Num()) { TowerBuf.SetNum(TowersExist * 2); Sim.towers(TowerBuf.GetData(), TowerBuf.Num()); }
	int32 ShotsExist = Sim.projectiles(ShotBuf.GetData(), ShotBuf.Num());
	if (ShotsExist > ShotBuf.Num()) { ShotBuf.SetNum(ShotsExist * 2); Sim.projectiles(ShotBuf.GetData(), ShotBuf.Num()); }
	const int32 NE = FMath::Clamp<int32>(LastEnemies, 0, EnemyBuf.Num());
	const int32 NT = FMath::Clamp<int32>(TowersExist, 0, TowerBuf.Num());
	const int32 NS = FMath::Clamp<int32>(ShotsExist, 0, ShotBuf.Num());
	EnemyCountNow = NE; TowerCountNow = NT;
	const int32 TickNow = Sim.tick();
	const bool bNewTick = TickNow != LastRedrawTick;
	LastRedrawTick = TickNow;
	const float Alpha = Accumulator / FixedStep;

	for (int32 k = 0; k < EnemyKinds; ++k) EnemyMesh[k]->ClearInstances();
	for (int32 i = 0; i < NE; ++i)
	{
		const TdfEnemy& E = EnemyBuf[i];
		if (bUseArt && UpdateEnemyView(E, bNewTick, Alpha, LastDeltaSeconds)) continue;
		const int32 Kind = (E.Flags & TDF_ENEMY_FLYING) ? 3 : (E.Flags & TDF_ENEMY_BURNING) ? 2 : (E.Flags & (TDF_ENEMY_FROZEN | TDF_ENEMY_SLOWED)) ? 1 : 0;
		const float Scale = 0.25f * (0.6f + 0.8f * FMath::Clamp(E.MaxHp / 400.f, 0.f, 1.f));
		EnemyMesh[Kind]->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(E.Lane, E.X, 0.f, E.Z) + FVector(0.f, 0.f, 50.f * Scale), FVector(Scale)));
	}
	if (bUseArt) ReleaseUnseenViews();
	// torres são estáticas: só refaz as instâncias quando alguma entra, sobe ou sai (assinatura das torres)
	uint32 Sig = HashCombine(GetTypeHash(NT), GetTypeHash(bUseArt));
	for (int32 i = 0; i < NT; ++i)
	{
		const TdfTower& T = TowerBuf[i];
		Sig = HashCombine(Sig, GetTypeHash(T.Lane * 7919 + T.CellX * 131 + T.CellY * 17 + T.TypeId * 3 + T.Level * 104729));
	}
	const bool bTowersChanged = Sig != TowerSignature;
	TowerSignature = Sig;
	if (bTowersChanged)
	{
	for (int32 k = 0; k < TowerKinds; ++k) TowerMesh[k]->ClearInstances();
	for (auto& Pair : TowerIsms) if (Pair.Value) Pair.Value->ClearInstances();
	for (int32 i = 0; i < NT; ++i)
	{
		const TdfTower& T = TowerBuf[i];
		if (bUseArt)
		{
			if (UInstancedStaticMeshComponent* Art = TowerIsm(T.TypeId, StageForLevel(T.Level)))
			{
				Art->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(T.Lane, T.X, T.Y, T.Z), FVector::OneVector));
				continue;
			}
		}
		TowerMesh[FMath::Clamp(T.TypeId, 0, TowerKinds - 1)]->AddInstance(
			FTransform(FRotator::ZeroRotator, ToUnreal(T.Lane, T.X, T.Y + 0.4f, T.Z), FVector(0.35f, 0.35f, 0.4f + 0.1f * T.Level)));
	}
	}
	ShotMesh->ClearInstances();
	for (int32 i = 0; i < NS; ++i)
	{
		const TdfProjectile& S = ShotBuf[i];
		ShotMesh->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(S.Lane, S.X, S.Y, S.Z), FVector(0.08f)));
	}

	// território: só reconstrói as instâncias quando muda (muda quando uma torre entra, sobe ou sai)
	TerritoryNow.SetNumZeroed(2 * GridWidth * GridHeight, EAllowShrinking::No);
	Sim.territory(TerritoryNow.GetData(), TerritoryNow.Num());
	if (TerritoryNow != TerritoryBuf || TerritoryMesh->GetInstanceCount() + TerritoryMesh1->GetInstanceCount() == 0)
	{
		Swap(TerritoryBuf, TerritoryNow);
		UInstancedStaticMeshComponent* Veils[2] = { TerritoryMesh, TerritoryMesh1 };
		for (int32 Lane = 0; Lane < 2; ++Lane)
		{
			Veils[Lane]->ClearInstances();
			TArray<FTransform> Cells;
			for (int32 Y = 0; Y < GridHeight; ++Y)
				for (int32 X = 0; X < GridWidth; ++X)
					if (TerritoryBuf[Lane * GridWidth * GridHeight + Y * GridWidth + X])
					{
						const FVector P = CellToWorld(X, Y, 3.f) + FVector(Lane * (GridHeight + 4) * CellSizeCm, 0.f, 0.f);
						Cells.Add(FTransform(FRotator::ZeroRotator, P, FVector(CellSizeCm * 0.0097f, CellSizeCm * 0.0097f, 1.f)));
					}
			Veils[Lane]->AddInstances(Cells, false);
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
		{
			// a Sim põe quem anda em y = 0,5 (o centro do corpo); na vista o pé fica no chão (o Unity achata, EnemyView.Flat)
			TdfEnemy Walker = {}; Walker.Y = 0.5f; Walker.X = 1.f; Walker.Z = 2.f;
			SelfCheck(FMath::IsNearlyZero(EnemyWorldPos(Walker).Z), TEXT("bicho que anda fica com o pé no chão (Z = 0)"));
		}
		SaveRecording();
		SelfCheck(FPaths::FileExists(RecordingPath()), TEXT("a partida foi gravada em Saved/Tdf/partida.txt"));
		// coleta de lixo: o que a vista cria em tempo de execução (componente de bicho no pool, malha, animação, torre
		// instanciada) tem que sobreviver ao GC. Sem isso o jogo caía a ~60 s de partida (o intervalo do GC).
		if (bUseArt && TypeVisual(0).Mesh)
		{
			USkeletalMeshComponent* C = AcquireComp(0);
			C->SetVisibility(false);
			Pool.FindOrAdd(0).Add(C); // devolvido ao pool, como um bicho que morreu
			UInstancedStaticMeshComponent* Ism = TowerIsm(0, 1);
			TWeakObjectPtr<USkeletalMeshComponent> WeakComp(C);
			TWeakObjectPtr<USkeletalMesh> WeakMesh(TypeVisual(0).Mesh);
			TWeakObjectPtr<UAnimSequence> WeakAnim(TypeVisual(0).Anim);
			TWeakObjectPtr<UInstancedStaticMeshComponent> WeakIsm(Ism);
			CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS, true);
			SelfCheck(WeakComp.IsValid(), TEXT("GC: o componente de bicho no pool sobrevive"));
			SelfCheck(WeakMesh.IsValid(), TEXT("GC: a malha do bicho sobrevive"));
			SelfCheck(!TypeVisual(0).Anim || WeakAnim.IsValid(), TEXT("GC: a animação do bicho sobrevive"));
			SelfCheck(!Ism || WeakIsm.IsValid(), TEXT("GC: a torre instanciada sobrevive"));
		}
		UE_LOG(LogTdf, Display, TEXT("AUTOTESTE %s (%d falha(s))"), SelfFailures == 0 ? TEXT("PASSOU") : TEXT("REPROVOU"), SelfFailures);
		bSelfTest = false;
		FPlatformMisc::RequestExitWithStatus(true, SelfFailures == 0 ? 0 : 1);
		return;
	}
	++SelfStep;
}
