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
#include "Engine/Engine.h"
#include "Misc/DateTime.h"
#include "RenderCore.h"
#include "RHI.h"
#include "TdfPlayerController.h"
#include "TdfHudWidget.h"
#include "TdfFxComponent.h"
#include "TdfBuildStamp.h"
#include "TdfEnvironment.h"
#include "EngineUtils.h"
#include "GameFramework/GameUserSettings.h"
#include "Camera/CameraActor.h"
#include "Engine/GameViewportClient.h"
#include "Framework/Application/SlateApplication.h"
#include "Framework/Application/IInputProcessor.h"
#include "Widgets/SViewport.h"
#include "Widgets/SWindow.h"
#include "InputKeyEventArgs.h"

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
	Fx = CreateDefaultSubobject<UTdfFxComponent>(TEXT("Efeitos"));
	Fx->SetupAttachment(RootScene);
	TerritoryMesh = Make(TEXT("Territorio"), Plane.Object);
	TerritoryMesh1 = Make(TEXT("Territorio1"), Plane.Object);
	OutlineMesh = Make(TEXT("Contorno"), Cube.Object);
	OutlineMesh1 = Make(TEXT("Contorno1"), Cube.Object);
	OutlineMesh->NumCustomDataFloats = 4;
	OutlineMesh1->NumCustomDataFloats = 4;
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
	Fx->Setup();
	if (UMaterialInterface* Glow = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/TDFende/Efeitos/M_TdfBrilho.M_TdfBrilho")))
	{
		OutlineMesh->SetMaterial(0, Glow);
		OutlineMesh1->SetMaterial(0, Glow);
	}
	// véus de efeito no bicho (material de sobreposição): gelo, lentidão, fogo (este pulsa)
	if (UMaterialInterface* St = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/TDFende/Efeitos/M_TdfStatus.M_TdfStatus")))
	{
		const FLinearColor Cor[3] = { FLinearColor(0.35f, 0.75f, 1.f), FLinearColor(0.45f, 0.62f, 0.9f), FLinearColor(1.f, 0.42f, 0.08f) };
		const float Intensidade[3] = { 1.6f, 0.7f, 1.3f }, Pulso[3] = { 0.f, 0.f, 0.35f }; // medidos no print: mais forte apaga a pelagem
		for (int32 i = 0; i < 3; ++i)
		{
			UMaterialInstanceDynamic* M = UMaterialInstanceDynamic::Create(St, this);
			M->SetVectorParameterValue(TEXT("Cor"), Cor[i]);
			M->SetScalarParameterValue(TEXT("Intensidade"), Intensidade[i]);
			M->SetScalarParameterValue(TEXT("Pulso"), Pulso[i]);
			StatusMids.Add(M);
		}
	}
	else UE_LOG(LogTdf, Warning, TEXT("Sem M_TdfStatus: bicho congelado ou queimando não muda de cara (rode ImportarArte.ps1 -Script importa_efeitos.py)"));
	// território: véu translúcido sobre o chão (M_TdfTerritorio, do importa_ambiente.py); azul é seu, vermelho é da IA
	if (UMaterialInterface* Veil = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/TDFende/Ambiente/Materiais/M_TdfTerritorio.M_TdfTerritorio")))
	{
		const FLinearColor LaneColor[2] = { FLinearColor(0.20f, 0.55f, 1.0f), FLinearColor(1.0f, 0.32f, 0.22f) };
		UInstancedStaticMeshComponent* Veils[2] = { TerritoryMesh, TerritoryMesh1 };
		for (int32 i = 0; i < 2; ++i)
		{
			UMaterialInstanceDynamic* M = UMaterialInstanceDynamic::Create(Veil, this);
			M->SetVectorParameterValue(TEXT("Cor"), LaneColor[i]);
			M->SetScalarParameterValue(TEXT("Opacidade"), 0.10f); // o contorno marca o limite; o véu só tinge
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
	FString ExecList;
	if (FParse::Value(FCommandLine::Get(), TEXT("TdfExec="), ExecList, false))
	{
		TArray<FString> Items;
		ExecList.ParseIntoArray(Items, TEXT(";"), true);
		for (const FString& It : Items)
		{
			FString L, R;
			if (It.Split(TEXT(":"), &L, &R)) ExecAt.Add({FCString::Atoi(*L), R});
		}
	}
	FString TickList;
	if (FParse::Value(FCommandLine::Get(), TEXT("TdfShotTicks="), TickList, false))
	{
		TArray<FString> Parts;
		TickList.ParseIntoArray(Parts, TEXT(","), true);
		for (const FString& S : Parts) ShotTicks.Add(FCString::Atoi(*S));
	}
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
	UE_LOG(LogTdf, Display, TEXT("[TDFende] build %s"), *TdfBuildStamp()); // selo no log, como o Player.log do Unity
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

bool ATdfSimActor::RestartMatch()
{
	if (!bRunning || bReplaying || !Sim.IsLoaded()) return false;
	SaveRecording(); // a partida que acabou fica em partida.txt

	// vista: tudo da partida anterior sai da tela (bicho volta ao pool sem cair, morto some, tiros e clarões somem)
	TArray<int64> Ids;
	Views.GetKeys(Ids);
	for (int64 Id : Ids) KillView(Id, true);
	for (const FDying& D : Dying) RecycleDying(D);
	Dying.Reset();
	Fx->Clear();
	Bars.Reset();
	LeakedAt.Reset();

	// Sim: partida nova com outra semente, como o R do Unity (a semente vai no replay: continua reprodutível)
	Sim.destroy();
	const int32 Old = Seed;
	Seed = int32(FDateTime::Now().GetTicks() % 2000000000);
	if (Seed == Old) ++Seed;
	if (Sim.create(Seed, Difficulty, GridWidth, GridHeight, nullptr, nullptr) != 0)
	{
		UE_LOG(LogTdf, Error, TEXT("tdf_create falhou ao reiniciar: %s"), *Sim.LastError());
		bRunning = false;
		return false;
	}
	Recorded.Reset();
	Accumulator = 0.f;
	LastRedrawTick = -1;
	TowerSignature = MAX_uint32; // refaz as torres no próximo desenho
	bFingerprintLogged = false;
	FMemory::Memzero(TerritoryBuf.GetData(), TerritoryBuf.Num());
	Redraw(); // placar e perguntas do HUD já valem para a partida nova
	ToastMsg(TEXT("Partida nova"));
	UE_LOG(LogTdf, Display, TEXT("Partida nova (semente %d)"), Seed);
	return true;
}

FString ATdfSimActor::RecordingText() const
{
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
	return Out;
}

bool ATdfSimActor::SaveReplayNow(FString& OutPath)
{
	if (!bRunning || bReplaying || !Sim.IsLoaded()) return false;
	// como o F9 do Unity: um arquivo por vez que aperta, com data e hora no nome
	OutPath = FPaths::Combine(FPaths::ProjectSavedDir(), TEXT("Tdf"), TEXT("replays"),
		FString::Printf(TEXT("tdfende-%s.txt"), *FDateTime::Now().ToString(TEXT("%Y%m%d-%H%M%S"))));
	const bool bOk = FFileHelper::SaveStringToFile(RecordingText(), *OutPath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
	ToastMsg(bOk ? FString::Printf(TEXT("Replay salvo: %s"), *FPaths::GetCleanFilename(OutPath)) : FString(TEXT("Não consegui salvar o replay")));
	UE_LOG(LogTdf, Display, TEXT("F9: %s %s"), bOk ? TEXT("replay salvo em") : TEXT("falhou ao salvar"), *OutPath);
	return bOk;
}

int32 ATdfSimActor::MatchResult() const
{
	if (!bRunning || !Sim.IsLoaded() || !Sim.over()) return 0;
	return Lanes[1].Dead && !Lanes[0].Dead ? 1 : -1;
}

FString ATdfSimActor::RecordingPath()
{
	return FPaths::Combine(FPaths::ProjectSavedDir(), TEXT("Tdf"), TEXT("partida.txt"));
}

void ATdfSimActor::SaveRecording() const
{
	if (Recorded.Num() == 0 || bReplaying || !Sim.IsLoaded()) return;
	const FString Path = RecordingPath();
	FFileHelper::SaveStringToFile(RecordingText(), *Path, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
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
		FScreenshotRequest::RequestScreenshot(ShotPath, true, false); // com a interface (o HUD em UMG é Slate, fora do viewport)
		UE_LOG(LogTdf, Display, TEXT("Print pedido no tique %d: %s"), ShotTick, *ShotPath);
	}
	// -TdfExec=<tique>:<comando>;<tique>:<comando>: roda um comando de console no tique (medir: ProfileGPU, stat unit...)
	for (const TPair<int32, FString>& C : ExecAt)
		if (C.Key == Sim.tick() && GEngine)
		{
			UE_LOG(LogTdf, Display, TEXT("Comando no tique %d: %s"), C.Key, *C.Value);
			GEngine->Exec(GetWorld(), *C.Value);
		}
	// -TdfShotTicks=a,b,c: um print por tique da lista, <arquivo>_<tique>.png (achar o momento certo sem rodar várias vezes)
	if (!ShotPath.IsEmpty() && ShotTicks.Contains(Sim.tick()))
	{
		const FString P = FPaths::Combine(FPaths::GetPath(ShotPath), FString::Printf(TEXT("%s_%d.png"), *FPaths::GetBaseFilename(ShotPath), Sim.tick()));
		FScreenshotRequest::RequestScreenshot(P, true, false);
		UE_LOG(LogTdf, Display, TEXT("Print pedido no tique %d: %s (inimigos %d, tiros %d)"), Sim.tick(), *P, EnemyCountNow, Fx->NumShotInstances());
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
		// quem segura o quadro: jogo, render ou placa de vídeo (último quadro de cada); quadro longo com os três curtos = fora do jogo
		UE_LOG(LogTdf, Display, TEXT("FPS %.1f (quadro medio %.1f ms) no tique %d | jogo %.1f ms, render %.1f ms, GPU %.1f ms"),
			FpsFrames / FpsWindow, 1000.f * FpsWindow / FpsFrames, Sim.tick(), FPlatformTime::ToMilliseconds(GGameThreadTime),
			FPlatformTime::ToMilliseconds(GRenderThreadTime), FPlatformTime::ToMilliseconds(RHIGetGPUFrameCycles()));
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
	// eventos dos tiques que acabaram de rodar: os de vazamento antes do desenho (bicho que vazou não cai morto)
	const int32 NEv = FMath::Min<int32>(Sim.events(EventBuf.GetData(), EventBuf.Num()), EventBuf.Num());
	LeakedAt.Reset();
	for (int32 i = 0; i < NEv; ++i)
		if (EventBuf[i].Kind == TDF_EV_ENEMY_DESPAWNED && EventBuf[i].A == 2)
			LeakedAt.Add(ToUnreal(EventBuf[i].Lane, EventBuf[i].X, 0.f, EventBuf[i].Z));
	Redraw();
	for (int32 i = 0; i < NEv; ++i)
	{
		const TdfEvent& E = EventBuf[i];
		if (E.Kind == TDF_EV_COMMAND_REJECTED) ToastMsg(RejectText(E.B));
		else if (E.Kind == TDF_EV_MATCH_ENDED)
		{
			// o painel de VITÓRIA/DERROTA do HUD diz o resultado (MatchResult); aqui só o registro
			UE_LOG(LogTdf, Display, TEXT("Fim da partida no tique %d: %s"), E.C, E.A == 0 ? TEXT("derrota") : TEXT("vitória"));
			// com -TdfShot, um print do fim (o painel de VITÓRIA/DERROTA)
			if (!ShotPath.IsEmpty())
				FScreenshotRequest::RequestScreenshot(FPaths::Combine(FPaths::GetPath(ShotPath), FPaths::GetBaseFilename(ShotPath) + TEXT("_fim.png")), true, false);
		}
		else if (E.Kind == TDF_EV_TOWER_FIRED) FireFx(E);
		else if (E.Kind == TDF_EV_ENEMY_HIT) HitFx(E);
		else if (E.Kind == TDF_EV_STATUS_APPLIED) StatusFx(E);
	}
	Fx->TickFx(DeltaSeconds);
	TickDying(DeltaSeconds);
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
	V.DeathAnim = LoadObject<UAnimSequence>(nullptr, *FString::Printf(TEXT("/Game/TDFende/Bichos/%sBicho_Death.%sBicho_Death"), *Key, *Key));
	V.TargetCm = TargetHeightCm(Key);
	if (V.Mesh) KeepAlive.Add(V.Mesh);
	if (V.Anim) KeepAlive.Add(V.Anim);
	if (V.DeathAnim) KeepAlive.Add(V.DeathAnim);
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
	FEnemyView& W = Views.FindOrAdd(ViewId(E));
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
	UMaterialInterface* Veil = StatusMaterial(E.Flags);
	if (W.Comp->GetOverlayMaterial() != Veil) W.Comp->SetOverlayMaterial(Veil);
	// ave: o corpo já está no alto (GroundZ) e a "altura" medida é a envergadura
	AddBar(E, Draw + FVector(0.f, 0.f, V.GroundZ), SendTypes.IsValidIndex(E.TypeId) && SendTypes[E.TypeId].Key == TEXT("aguia") ? 30.f : V.TargetCm);
	W.bSeen = true;
	return true;
}

void ATdfSimActor::FireFx(const TdfEvent& E)
{
	// a Sim diz onde fica a boca do cano; o tipo da torre é o da torre mais perto, na mesma lane
	const FVector Muzzle = ToUnreal(E.Lane, E.X, E.Y, E.Z);
	int32 Type = -1;
	float Best = FLT_MAX;
	for (int32 i = 0; i < TowerCountNow; ++i)
	{
		const TdfTower& T = TowerBuf[i];
		if (T.Lane != E.Lane) continue;
		const float D = FVector::DistSquared2D(ToUnreal(T.Lane, T.X, T.Y, T.Z), Muzzle);
		if (D < Best) { Best = D; Type = T.TypeId; }
	}
	Fx->Flash(Muzzle, ETdfFx::Muzzle, Type, Type == 1 ? 1.4f : 1.f);
	if (Type == 0 || Type == 1) Fx->Flash(Muzzle, ETdfFx::Dust, -1, Type == 1 ? 0.7f : 0.45f); // fumaça da pólvora
}

void ATdfSimActor::HitFx(const TdfEvent& E)
{
	// onde está o bicho atingido (lane + vaga); se já saiu do snapshot, o despawn cuida da poeira
	for (int32 i = 0; i < EnemyCountNow; ++i)
	{
		const TdfEnemy& En = EnemyBuf[i];
		if (En.Lane != E.Lane || En.Slot != E.A) continue;
		const FTypeVisual* V = TypeVisuals.Find(En.TypeId);
		const float Body = V && V->Mesh ? V->GroundZ + V->TargetCm * 0.55f : 30.f;
		const FVector At = EnemyWorldPos(En) + FVector(0.f, 0.f, (En.Flags & TDF_ENEMY_FLYING) && V ? V->GroundZ : Body);
		Fx->Flash(At, ETdfFx::Impact, E.C, FMath::Clamp(E.F / 25.f, 0.6f, 1.8f));
		if (E.C == 0 || E.C == 1) Fx->Flash(FVector(At.X, At.Y, 0.f), ETdfFx::Dust, -1, E.C == 1 ? 1.3f : 0.7f); // bala pesada levanta terra
		return;
	}
}

void ATdfSimActor::TerritoryEdges(const uint8* Cells, int32 W, int32 H, TArray<FIntVector4>& Out)
{
	Out.Reset();
	auto In = [Cells, W, H](int32 X, int32 Y) { return X >= 0 && Y >= 0 && X < W && Y < H && Cells[Y * W + X] != 0; };
	for (int32 Y = 0; Y < H; ++Y)
		for (int32 X = 0; X < W; ++X)
		{
			if (!In(X, Y)) continue;
			if (!In(X - 1, Y)) Out.Add(FIntVector4(X, Y, X, Y + 1));
			if (!In(X + 1, Y)) Out.Add(FIntVector4(X + 1, Y, X + 1, Y + 1));
			if (!In(X, Y - 1)) Out.Add(FIntVector4(X, Y, X + 1, Y));
			if (!In(X, Y + 1)) Out.Add(FIntVector4(X, Y + 1, X + 1, Y + 1));
		}
}

void ATdfSimActor::StatusFx(const TdfEvent& E)
{
	if (E.C != 3) return; // 0 lentidão, 1 gelo, 2 fogo: o véu do bicho já mostra; 3 empurrão: onda de ar + poeira
	for (int32 i = 0; i < EnemyCountNow; ++i)
	{
		const TdfEnemy& En = EnemyBuf[i];
		if (En.Lane != E.Lane || En.Slot != E.A) continue;
		const FVector Feet = EnemyWorldPos(En);
		Fx->Flash(Feet, ETdfFx::Gust, -1, 1.f);
		Fx->Flash(Feet, ETdfFx::Dust, -1, 0.8f);
		return;
	}
}

UMaterialInterface* ATdfSimActor::StatusMaterial(int32 Flags) const
{
	const int32 K = (Flags & TDF_ENEMY_BURNING) ? 2 : (Flags & TDF_ENEMY_FROZEN) ? 0 : (Flags & TDF_ENEMY_SLOWED) ? 1 : -1; // fogo vence
	return StatusMids.IsValidIndex(K) ? StatusMids[K].Get() : nullptr;
}

void ATdfSimActor::AddBar(const TdfEnemy& E, const FVector& Feet, float HeightCm)
{
	if (E.MaxHp <= 0.f || E.Hp <= 0.f || E.Hp >= E.MaxHp) return; // só bicho ferido (e vivo) mostra barra
	FTdfBar B;
	B.Head = Feet + FVector(0.f, 0.f, HeightCm + 18.f);
	B.Frac = E.Hp / E.MaxHp;
	B.MaxHp = E.MaxHp;
	B.Flags = E.Flags;
	Bars.Add(B);
}

void ATdfSimActor::KillView(int64 Id, bool bLeaked)
{
	FEnemyView W;
	if (!Views.RemoveAndCopyValue(Id, W) || !W.Comp) return;
	USkeletalMeshComponent* C = W.Comp;
	C->SetOverlayMaterial(nullptr);
	if (bLeaked)
	{
		// vazou pela base: some ali mesmo (quem viu foi a barra de vidas)
		C->SetVisibility(false);
		C->SetComponentTickEnabled(false);
		Pool.FindOrAdd(W.Type).Add(C);
		return;
	}
	const FTypeVisual& V = TypeVisual(W.Type);
	FDying D;
	D.Comp = C;
	D.Type = W.Type;
	D.Pos = C->GetComponentLocation();
	D.Yaw = W.Yaw;
	D.Scale = V.Fit;
	if (V.DeathAnim)
	{
		C->SetAnimation(V.DeathAnim);
		C->Play(false);
		D.DeathLen = V.DeathAnim->GetPlayLength();
	}
	Dying.Add(D);
	Fx->Flash(FVector(D.Pos.X, D.Pos.Y, 0.f), ETdfFx::Dust, -1, FMath::Clamp(V.TargetCm / 50.f, 0.6f, 2.5f));
}

void ATdfSimActor::RecycleDying(const FDying& D)
{
	D.Comp->SetVisibility(false);
	D.Comp->SetComponentTickEnabled(false);
	const FTypeVisual& V = TypeVisual(D.Type);
	if (D.DeathLen > 0.f && V.Anim) { D.Comp->SetAnimation(V.Anim); D.Comp->Play(true); } // volta a andar quando reaproveitado
	Pool.FindOrAdd(D.Type).Add(D.Comp);
}

void ATdfSimActor::TickDying(float DeltaSeconds)
{
	for (int32 i = Dying.Num() - 1; i >= 0; --i)
	{
		FDying& D = Dying[i];
		D.Age += DeltaSeconds;
		const FTypeVisual& V = TypeVisual(D.Type);
		// com animação de morte: toca, espera um pouco e afunda; sem ela: tomba de lado (ave cai do alto), espera e afunda
		const float Fall = D.DeathLen > 0.f ? D.DeathLen : 0.4f;
		const float SinkStart = Fall + 0.6f, End = SinkStart + 0.9f;
		if (D.Age >= End)
		{
			RecycleDying(D);
			Dying.RemoveAtSwap(i, EAllowShrinking::No);
			continue;
		}
		FVector P = D.Pos;
		FQuat Q = FRotator(0.f, D.Yaw + V.YawOffset, 0.f).Quaternion();
		if (D.DeathLen <= 0.f)
		{
			const float T = FMath::Clamp(D.Age / Fall, 0.f, 1.f);
			const float Ease = 1.f - (1.f - T) * (1.f - T);
			const FVector Heading = FRotator(0.f, D.Yaw, 0.f).Vector();
			Q = FQuat(Heading, FMath::DegreesToRadians(84.f * Ease)) * Q;          // tomba de lado
			P.Z = FMath::Lerp(D.Pos.Z, V.GroundZ > 50.f ? 0.f : D.Pos.Z, T * T);   // ave despenca até o chão
		}
		if (D.Age > SinkStart) P.Z -= (D.Age - SinkStart) / (End - SinkStart) * FMath::Max(V.TargetCm, 30.f) * 0.8f;
		D.Comp->SetWorldTransform(FTransform(Q, P, FVector(D.Scale)));
	}
}

FVector ATdfSimActor::EnemyWorldPos(const TdfEnemy& E) const
{
	// a Sim guarda o centro do corpo (y = 0,5); na vista o pé fica no chão e quem voa sobe pelo GroundZ da arte
	return ToUnreal(E.Lane, E.X, 0.f, E.Z);
}

void ATdfSimActor::ReleaseUnseenViews()
{
	// quem sumiu do snapshot: vazou pela base (evento de despawn com motivo "vazou" ali perto) ou morreu
	TArray<TPair<int64, bool>, TInlineAllocator<32>> Gone;
	for (auto& Pair : Views)
	{
		if (Pair.Value.bSeen) { Pair.Value.bSeen = false; continue; }
		bool bLeaked = false;
		for (const FVector& L : LeakedAt) bLeaked |= FVector::DistSquared2D(L, Pair.Value.CurPos) < 250.f * 250.f;
		Gone.Add({Pair.Key, bLeaked});
	}
	for (const TPair<int64, bool>& G : Gone) KillView(G.Key, G.Value);
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

UStaticMesh* ATdfSimActor::TowerArt(int32 Type, int32 Stage) const
{
	if (!bUseArt || !TowerTypes.IsValidIndex(Type)) return nullptr;
	FString K = TowerTypes[Type].Key;
	K[0] = FChar::ToUpper(K[0]);
	const FString Name = FString::Printf(TEXT("Torre_%s_%d"), *K, Stage);
	return LoadObject<UStaticMesh>(nullptr, *FString::Printf(TEXT("/Game/TDFende/Torres/%s.%s"), *Name, *Name));
}

USkeletalMesh* ATdfSimActor::SendArt(int32 Type, UAnimSequence*& OutAnim, float& OutYawOffset)
{
	OutAnim = nullptr;
	if (!bUseArt || !SendTypes.IsValidIndex(Type)) return nullptr;
	FTypeVisual& V = TypeVisual(Type);
	OutAnim = V.Anim;
	OutYawOffset = V.YawOffset;
	return V.Mesh;
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
	Bars.Reset();
	for (int32 i = 0; i < NE; ++i)
	{
		const TdfEnemy& E = EnemyBuf[i];
		if (bUseArt && UpdateEnemyView(E, bNewTick, Alpha, LastDeltaSeconds)) continue;
		const int32 Kind = (E.Flags & TDF_ENEMY_FLYING) ? 3 : (E.Flags & TDF_ENEMY_BURNING) ? 2 : (E.Flags & (TDF_ENEMY_FROZEN | TDF_ENEMY_SLOWED)) ? 1 : 0;
		const float Scale = 0.25f * (0.6f + 0.8f * FMath::Clamp(E.MaxHp / 400.f, 0.f, 1.f));
		const FVector Feet = ToUnreal(E.Lane, E.X, 0.f, E.Z);
		EnemyMesh[Kind]->AddInstance(FTransform(FRotator::ZeroRotator, Feet + FVector(0.f, 0.f, 50.f * Scale), FVector(Scale)));
		AddBar(E, Feet, 100.f * Scale);
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
	// tiros: a cara do tipo de torre, interpolados entre os tiques e com rastro (UTdfFxComponent)
	Fx->UpdateShots(ShotBuf.GetData(), NS, bNewTick, Alpha, [this](int32 L, float X, float Y, float Z) { return ToUnreal(L, X, Y, Z); });

	// território: só reconstrói as instâncias quando muda (muda quando uma torre entra, sobe ou sai)
	TerritoryNow.SetNumZeroed(2 * GridWidth * GridHeight, EAllowShrinking::No);
	Sim.territory(TerritoryNow.GetData(), TerritoryNow.Num());
	if (TerritoryNow != TerritoryBuf || TerritoryMesh->GetInstanceCount() + TerritoryMesh1->GetInstanceCount() == 0)
	{
		Swap(TerritoryBuf, TerritoryNow);
		UInstancedStaticMeshComponent* Veils[2] = { TerritoryMesh, TerritoryMesh1 };
		UInstancedStaticMeshComponent* Lines[2] = { OutlineMesh, OutlineMesh1 };
		const FLinearColor LaneGlow[2] = { FLinearColor(0.25f, 0.6f, 1.f, 2.2f), FLinearColor(1.f, 0.35f, 0.22f, 2.2f) };
		TArray<FIntVector4> Edges;
		for (int32 Lane = 0; Lane < 2; ++Lane)
		{
			const FVector LaneShift(Lane * (GridHeight + 4) * CellSizeCm, 0.f, 0.f);
			// véu contínuo (células encostadas, sem fresta): a grade de quadradinhos sumiu
			Veils[Lane]->ClearInstances();
			TArray<FTransform> Cells;
			const uint8* Grid = TerritoryBuf.GetData() + Lane * GridWidth * GridHeight;
			for (int32 Y = 0; Y < GridHeight; ++Y)
				for (int32 X = 0; X < GridWidth; ++X)
					if (Grid[Y * GridWidth + X])
						Cells.Add(FTransform(FRotator::ZeroRotator, CellToWorld(X, Y, 3.f) + LaneShift, FVector(CellSizeCm * 0.01f, CellSizeCm * 0.01f, 1.f)));
			Veils[Lane]->AddInstances(Cells, false);
			// contorno: uma faixa fina que brilha em cada lado de célula que dá para fora
			if (!Lines[Lane]) continue;
			Lines[Lane]->ClearInstances();
			TerritoryEdges(Grid, GridWidth, GridHeight, Edges);
			TArray<FTransform> Segs;
			for (const FIntVector4& E : Edges)
			{
				// canto (cx, cy) da grade -> mundo: X = (cy - H/2) * célula, Y = (cx - W/2) * célula (como CellToWorld, sem o meio)
				const FVector A((E.Y - GridHeight * 0.5f) * CellSizeCm, (E.X - GridWidth * 0.5f) * CellSizeCm, 5.f);
				const FVector B((E.W - GridHeight * 0.5f) * CellSizeCm, (E.Z - GridWidth * 0.5f) * CellSizeCm, 5.f);
				const bool bAlongX = FMath::Abs(B.X - A.X) > 1.f;
				const float Len = CellSizeCm + 7.f; // passa um pouco do canto: as esquinas fecham
				Segs.Add(FTransform(FRotator::ZeroRotator, (A + B) * 0.5f + LaneShift,
					FVector(bAlongX ? Len : 7.f, bAlongX ? 7.f : Len, 2.f) / 100.f));
			}
			Lines[Lane]->AddInstances(Segs, false);
			for (int32 i = 0; i < Segs.Num(); ++i)
			{
				const float V[4] = { LaneGlow[Lane].R, LaneGlow[Lane].G, LaneGlow[Lane].B, LaneGlow[Lane].A };
				Lines[Lane]->SetCustomData(i, TArrayView<const float>(V, 4), i == Segs.Num() - 1);
			}
		}
	}
}

// ------------------------------------------------------------------ autoteste (-TdfSelfTest)

void ATdfSimActor::SelfCheck(bool bCondition, const TCHAR* What)
{
	UE_LOG(LogTdf, Display, TEXT("AUTOTESTE %s: %s"), bCondition ? TEXT("OK   ") : TEXT("FALHOU"), What);
	if (!bCondition) ++SelfFailures;
}

namespace
{
	/**
	 * Durante o autoteste do controle só valem os eventos de mouse que o teste injeta: o mouse real (o usuário trabalhando em outra
	 * janela, o cursor mexido) apagava cliques e o teste reprovava em ~15% das execuções. Retornar true = o evento foi consumido.
	 */
	class FTdfTestShield : public IInputProcessor
	{
	public:
		bool bInjecting = false;
		int32 RealEvents = 0; // eventos de mouse reais descartados (o usuário mexeu o mouse durante o teste)
		bool Drop() { if (!bInjecting) ++RealEvents; return !bInjecting; }
		virtual void Tick(const float, FSlateApplication&, TSharedRef<ICursor>) override {}
		virtual bool HandleMouseMoveEvent(FSlateApplication&, const FPointerEvent&) override { return Drop(); }
		virtual bool HandleMouseButtonDownEvent(FSlateApplication&, const FPointerEvent&) override { return Drop(); }
		virtual bool HandleMouseButtonUpEvent(FSlateApplication&, const FPointerEvent&) override { return Drop(); }
		virtual bool HandleMouseButtonDoubleClickEvent(FSlateApplication&, const FPointerEvent&) override { return Drop(); }
		virtual bool HandleMouseWheelOrGestureEvent(FSlateApplication&, const FPointerEvent&, const FPointerEvent*) override { return Drop(); }
		virtual const TCHAR* GetDebugName() const override { return TEXT("TdfTestShield"); }
	};
	TSharedPtr<FTdfTestShield> GShield;
}

bool ATdfSimActor::RunInputTest()
{
	// Mouse e teclas de verdade: o mouse entra pelo Slate (o mesmo caminho do mouse físico: hover, botões do UMG, viewport) e as
	// teclas pelo PlayerController (o foco do teclado de uma janela sem usuário não é confiável). Uma fase por vez, com espera de
	// quadros entre elas (o hover e os tiques da Sim andam a cada quadro).
	ATdfPlayerController* PC = Cast<ATdfPlayerController>(GetWorld()->GetFirstPlayerController());
	UTdfHudWidget* Hud = SelfHud();
	UGameViewportClient* VC = GEngine ? GEngine->GameViewport : nullptr;
	if (!PC || !Hud || !VC || !VC->GetWindow().IsValid() || !VC->GetGameViewportWidget().IsValid())
	{
		SelfCheck(false, TEXT("controle: sem controle, HUD ou janela para testar"));
		return true;
	}
	if (SelfInputWait > 0) { --SelfInputWait; return false; }

	FSlateApplication& App = FSlateApplication::Get();
	const TSharedRef<SWindow> Win = VC->GetWindow().ToSharedRef();
	if (!GShield.IsValid())
	{
		GShield = MakeShared<FTdfTestShield>();
		App.RegisterInputPreProcessor(GShield, 0); // antes de tudo: o mouse real não chega nem ao viewport nem ao UMG
	}
	auto Move = [&](const FVector2D& P)
	{
		GShield->bInjecting = true;
		App.SetCursorPos(P);
		const FPointerEvent E(0, FVector2f(P), FVector2f(SelfMouse), TSet<FKey>(), EKeys::Invalid, 0.f, FModifierKeysState());
		App.ProcessMouseMoveEvent(E);
		SelfMouse = P;
		GShield->bInjecting = false;
	};
	auto Button = [&](bool bDown)
	{
		GShield->bInjecting = true;
		TSet<FKey> Pressed;
		if (bDown) Pressed.Add(EKeys::LeftMouseButton);
		const FPointerEvent E(0, FVector2f(SelfMouse), FVector2f(SelfMouse), Pressed, EKeys::LeftMouseButton, 0.f, FModifierKeysState());
		if (bDown) App.ProcessMouseButtonDownEvent(Win->GetNativeWindow(), E);
		else App.ProcessMouseButtonUpEvent(E);
		GShield->bInjecting = false;
	};
	auto Key = [&](const FKey& K, bool bDown) { PC->InputKey(FInputKeyEventArgs::CreateSimulated(K, bDown ? IE_Pressed : IE_Released, bDown ? 1.f : 0.f, 1, FInputDeviceId::CreateFromInternalId(0))); };
	auto Wait = [&](int32 Frames) { SelfInputWait = Frames; ++SelfInputPhase; };

	switch (SelfInputPhase)
	{
	case 0: // mouse sobre um cartão de torre que cobre uma célula onde DÁ para construir (senão o teste não provaria nada)
	{
		if (SelfFailuresBeforeInput < 0) SelfFailuresBeforeInput = SelfFailures;
		SelfInputGoldBefore = Lanes[0].Gold;
		const FGeometry& VG = VC->GetGameViewportWidget()->GetCachedGeometry();
		if (!SelfInputShifted)
		{
			// no enquadramento normal a lane termina acima da loja: anda a câmera para a frente até a lane ficar sob os cartões
			if (ACameraActor* Cam = Cast<ACameraActor>(PC->GetViewTarget())) Cam->AddActorWorldOffset(FVector(900.f, 0.f, 0.f));
			SelfInputShifted = true;
			SelfInputWait = 6;
			return false;
		}
		SelfInputCard = -1;
		for (int32 K = 0; K < TowerTypes.Num() && SelfInputCard < 0; ++K)
		{
			const FVector2D L = VG.AbsoluteToLocal(Hud->CardCenter(0, K));
			FVector O, D;
			if (!PC->DeprojectScreenPositionToWorld(L.X, L.Y, O, D) || FMath::Abs(D.Z) < KINDA_SMALL_NUMBER) continue;
			const FIntPoint C = WorldToCell(O + D * (-O.Z / D.Z));
			if (C.X >= 0 && CanBuildSelected(C.X, C.Y) == 0) SelfInputCard = K;
		}
		SelfCheck(SelfInputCard >= 0, TEXT("controle: há um cartão de torre cobrindo uma célula livre (o teste de clique por baixo vale)"));
		if (SelfInputCard < 0) return true;
		Move(Hud->CardCenter(0, SelfInputCard));
		Wait(6);
		return false;
	}
	case 1: Button(true); Wait(2); return false;
	case 2: Button(false); Wait(20); return false;
	case 3:
	{
		SelfCheck(GetSelectedTower() == SelfInputCard, TEXT("controle: clicar no cartão seleciona a torre"));
		SelfCheck(Lanes[0].TowerCount == 0 && Lanes[0].Gold >= SelfInputGoldBefore && Recorded.Num() == 0, TEXT("controle: clicar no cartão não constrói no campo por baixo"));
		// uma célula livre, bem no meio da tela, longe do HUD
		const FVector2D Size = VC->GetGameViewportWidget()->GetCachedGeometry().GetLocalSize();
		SelfInputCell = FIntPoint(-1, -1);
		for (int32 Y = 0; Y < GridHeight && SelfInputCell.X < 0; ++Y)
			for (int32 X = 0; X < GridWidth; ++X)
			{
				FVector2D S;
				if (CanBuildSelected(X, Y) != 0 || !PC->ProjectWorldLocationToScreen(CellToWorld(X, Y), S)) continue;
				// fora da faixa do placar (em cima) e da loja (embaixo, ~170 px) e longe do painel de configurações (no meio, até abrir)
				if (S.X > Size.X * 0.15 && S.X < Size.X * 0.85 && S.Y > Size.Y * 0.15 && S.Y < Size.Y - 230.f) { SelfInputCell = FIntPoint(X, Y); break; }
			}
		FVector2D S;
		const bool bOk = SelfInputCell.X >= 0 && PC->ProjectWorldLocationToScreen(CellToWorld(SelfInputCell.X, SelfInputCell.Y), S);
		SelfCheck(bOk, TEXT("controle: achei uma célula livre no meio da tela"));
		if (!bOk) return true;
		Move(VC->GetGameViewportWidget()->GetCachedGeometry().LocalToAbsolute(S));
		Wait(6);
		return false;
	}
	case 4: Button(true); Wait(2); return false;
	case 5: Button(false); Wait(24); return false;
	case 6:
		SelfCheck(Lanes[0].TowerCount == 1 && TowerLevelAt(SelfInputCell.X, SelfInputCell.Y) == 1, TEXT("controle: clicar no campo constrói a torre selecionada na célula do mouse"));
		Key(EKeys::Escape, true); Wait(2); return false;
	case 7: Key(EKeys::Escape, false); Wait(4); return false;
	case 8:
	{
		SelfCheck(Hud->IsSettingsOpen(), TEXT("controle: Esc abre as configurações"));
		SelfInputQualityBefore = UGameUserSettings::GetGameUserSettings()->GetOverallScalabilityLevel();
		Move(Hud->CardCenter(2, 0)); // botão "Baixa"
		Wait(6);
		return false;
	}
	case 9: Button(true); Wait(2); return false;
	case 10: Button(false); Wait(6); return false;
	case 11:
	{
		SelfCheck(UGameUserSettings::GetGameUserSettings()->GetOverallScalabilityLevel() == 0 && Hud->SelectedQuality() == 0, TEXT("controle: clicar em Baixa muda a qualidade gráfica"));
		Hud->SetQuality(SelfInputQualityBefore >= 0 ? SelfInputQualityBefore : 3); // devolve a qualidade de antes
		// com o painel aberto, clicar no campo (célula livre) não constrói
		FVector2D S;
		for (int32 Y = GridHeight - 1; Y >= 0 && SelfInputCell2.X < 0; --Y)
			for (int32 X = 0; X < GridWidth; ++X)
				if (CanBuildSelected(X, Y) == 0 && PC->ProjectWorldLocationToScreen(CellToWorld(X, Y), S)) { SelfInputCell2 = FIntPoint(X, Y); break; }
		if (SelfInputCell2.X >= 0 && PC->ProjectWorldLocationToScreen(CellToWorld(SelfInputCell2.X, SelfInputCell2.Y), S))
			Move(VC->GetGameViewportWidget()->GetCachedGeometry().LocalToAbsolute(S));
		Wait(6);
		return false;
	}
	case 12: Button(true); Wait(2); return false;
	case 13: Button(false); Wait(24); return false;
	case 14:
		SelfCheck(Lanes[0].TowerCount == 1, TEXT("controle: com o painel aberto, clicar no campo não constrói"));
		Key(EKeys::Escape, true); Wait(2); return false;
	case 15: Key(EKeys::Escape, false); Wait(4); return false;
	case 16:
		SelfCheck(!Hud->IsSettingsOpen(), TEXT("controle: Esc fecha as configurações"));
		SelfSeedBefore = Seed;
		Key(EKeys::R, true); Wait(2); return false;
	case 17: Key(EKeys::R, false); Wait(8); return false;
	case 18:
		SelfCheck(Seed != SelfSeedBefore && Lanes[0].TowerCount == 0, TEXT("controle: a tecla R começa outra partida"));
		++SelfInputPhase;
		return false;
	case 19: // clique na borda da placa da loja (fora dos botões): o Slate deixa passar, quem segura é o IsPointerOverUi
	{
		const FGeometry& VG = VC->GetGameViewportWidget()->GetCachedGeometry();
		const FVector2D Size = VG.GetLocalSize();
		FVector2D Picked = FVector2D::ZeroVector;
		const FVector2D ShopTopLeft = VG.AbsoluteToLocal(Hud->ShopEdgePoint(0.f));
		const FVector2D ShopBottomRight = VG.AbsoluteToLocal(Hud->ShopEdgePoint(1.f));
		for (float X = ShopTopLeft.X + 4.f; X < ShopBottomRight.X - 4.f && Picked.IsZero(); X += 12.f)
		{
			FVector O, D;
			const float Y = ShopTopLeft.Y + 3.f; // dentro do preenchimento da placa, acima dos cartões
			if (!PC->DeprojectScreenPositionToWorld(X, Y, O, D) || FMath::Abs(D.Z) < KINDA_SMALL_NUMBER) continue;
			const FIntPoint C = WorldToCell(O + D * (-O.Z / D.Z));
			if (C.X >= 0 && CanBuildSelected(C.X, C.Y) == 0) Picked = FVector2D(X, Y);
		}
		SelfCheck(!Picked.IsZero() && Picked.Y < Size.Y, TEXT("controle: achei um ponto da borda da loja sobre uma célula livre (o teste vale)"));
		if (Picked.IsZero()) return true;
		Move(VG.LocalToAbsolute(Picked));
		Wait(6);
		return false;
	}
	case 20:
		Button(true); Wait(2); return false;
	case 21: Button(false); Wait(24); return false;
	case 22:
		SelfCheck(Lanes[0].TowerCount == 0, TEXT("controle: clicar na borda da loja não constrói no campo por baixo"));
		return FinishInputTest();
	}
	return FinishInputTest();
}

bool ATdfSimActor::FinishInputTest()
{
	// Se o mouse REAL mexeu durante o controle e alguma checagem dele reprovou, o resultado dessa parte não vale (o mouse de verdade
	// mexe o cursor que o Slate também lê): sai como INCONCLUSIVO (código 4), e os scripts repetem. Sem mouse real mexendo, a falha vale.
	const int32 InputFails = SelfFailures - FMath::Max(SelfFailuresBeforeInput, 0);
	if (InputFails > 0 && GShield.IsValid() && GShield->RealEvents > 0)
	{
		UE_LOG(LogTdf, Display, TEXT("AUTOTESTE INCONCLUSIVO: o mouse real mexeu durante o controle (%d eventos descartados); %d checagem(ns) do controle não valem"), GShield->RealEvents, InputFails);
		SelfFailures -= InputFails;
		SelfInconclusive = true;
	}
	return true;
}

UTdfHudWidget* ATdfSimActor::SelfHud() const
{
	const ATdfPlayerController* PC = Cast<ATdfPlayerController>(GetWorld()->GetFirstPlayerController());
	return PC ? PC->GetHud() : nullptr;
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
		// o HUD nasce no primeiro quadro do controle e fotografa as miniaturas nos dois seguintes: espera até ~5 s
		const UTdfHudWidget* Ready = SelfHud();
		if ((!Ready || !Ready->ThumbnailsDone()) && ++SelfHudWait < 300) return;
		SelfCheck(TowerTypes.Num() == 6 && SendTypes.Num() == 9, TEXT("catálogo chegou pela ponte (6 torres, 9 envios)"));
		// HUD em UMG: placar e loja com um cartão por torre e por envio; o cartão é o mesmo caminho do clique do jogador
		{
			UTdfHudWidget* Hud = SelfHud();
			SelfCheck(Hud && Hud->IsInViewport(), TEXT("HUD em UMG na tela"));
			SelfCheck(Hud && Hud->NumTowerCards() == TowerTypes.Num() && Hud->NumSendCards() == SendTypes.Num(),
				TEXT("loja: um cartão por torre (6) e por envio (9)"));
			SelfCheck(Hud && Hud->ClickTowerCard(3) && GetSelectedTower() == 3, TEXT("clicar no cartão da torre 4 a seleciona"));
			if (bUseArt && TypeVisual(0).Mesh)
			{
				bool bThumbs = Hud != nullptr;
				for (int32 i = 0; bThumbs && i < TowerTypes.Num(); ++i) bThumbs = Hud->TowerThumb(i) != nullptr;
				for (int32 i = 0; bThumbs && i < SendTypes.Num(); ++i) bThumbs = Hud->SendThumb(i) != nullptr;
				SelfCheck(bThumbs, TEXT("miniaturas 3D de cada torre e de cada bicho nos cartões"));
			}
		}
		// configurações (Esc): abre, troca a qualidade gráfica de verdade (GameUserSettings) e fecha; volta a qualidade de antes
		if (UTdfHudWidget* Hud = SelfHud())
		{
			UGameUserSettings* Gus = UGameUserSettings::GetGameUserSettings();
			const int32 Before = Gus ? Gus->GetOverallScalabilityLevel() : -1;
			Hud->ToggleSettings();
			const bool bOpen = Hud->IsSettingsOpen();
			Hud->SetQuality(1);
			const bool bLow = Gus && Gus->GetOverallScalabilityLevel() == 1 && Hud->SelectedQuality() == 1;
			Hud->SetQuality(Before >= 0 ? Before : 3);
			Hud->ToggleSettings();
			SelfCheck(bOpen && bLow && !Hud->IsSettingsOpen(), TEXT("configurações: Esc abre, qualidade muda de verdade, Esc fecha"));
		}
		else SelfCheck(false, TEXT("configurações (sem HUD)"));
		// águias de ambiente: existem, voam alto e orbitam (a posição muda quando o tempo anda)
		{
			ATdfEnvironment* Env = nullptr;
			for (TActorIterator<ATdfEnvironment> It(GetWorld()); It; ++It) { Env = *It; break; }
			if (bUseArt && TypeVisual(4).Mesh)
			{
				SelfCheck(Env && Env->NumBirds() >= 3, TEXT("ambiente: águias circulando no céu (3 ou mais)"));
				if (Env && Env->NumBirds() > 0)
				{
					const FVector A = Env->BirdPos(0);
					Env->AdvanceBirds(2.f);
					const FVector B = Env->BirdPos(0);
					SelfCheck(A.Z > 500.f && A.Z < 4500.f && FVector::Dist2D(A, B) > 100.f, TEXT("ambiente: a águia voa alto e anda em volta (a posição muda com o tempo)"));
				}
			}
		}
		// selo de build (U4): o hash do git no canto do HUD; executável sem selo reprova
		{
			const FString Stamp = TdfBuildStamp();
			UTdfHudWidget* Hud = SelfHud();
			SelfCheck(!Stamp.IsEmpty() && Stamp != TEXT("sem selo") && Hud && Hud->StampText() == TEXT("build ") + Stamp,
				*FString::Printf(TEXT("selo de build no HUD (\"%s\")"), *Stamp));
		}
		// contorno do território: só os lados de célula que dão para fora (vizinho fora do território ou da grade)
		{
			uint8 G[9] = {0, 0, 0, 0, 1, 0, 0, 0, 0}; // 3x3, só o meio
			TArray<FIntVector4> Edges;
			TerritoryEdges(G, 3, 3, Edges);
			const bool bOne = Edges.Num() == 4;
			G[5] = 1; // e o vizinho à direita: 2x1 tem 6 lados de fora
			TerritoryEdges(G, 3, 3, Edges);
			const bool bTwo = Edges.Num() == 6;
			uint8 Corner[4] = {1, 0, 0, 0}; // célula no canto da grade: a borda da grade também é limite
			TerritoryEdges(Corner, 2, 2, Edges);
			SelfCheck(bOne && bTwo && Edges.Num() == 4, TEXT("contorno do território: só os lados que dão para fora"));
		}
		// efeitos de combate, com dados de mentira direto na vista (não passam pela Sim: a partida não muda)
		{
			TdfProjectile Shots[2] = {};
			Shots[0].Lane = 0; Shots[0].TowerType = 2; Shots[0].X = 1.f; Shots[0].Y = 0.5f; Shots[0].Z = 1.f;
			Shots[1].Lane = 0; Shots[1].TowerType = 0; Shots[1].X = -2.f; Shots[1].Y = 0.5f; Shots[1].Z = 3.f;
			auto ToWorld = [this](int32 L, float X, float Y, float Z) { return ToUnreal(L, X, Y, Z); };
			Fx->UpdateShots(Shots, 2, true, 0.f, ToWorld);
			SelfCheck(Fx->NumShotInstances() == 2 && Fx->NumTrailInstances() == 0, TEXT("tiro novo: um núcleo por projétil, ainda sem rastro"));
			Shots[0].Z += 0.3f; Shots[1].Z += 0.3f;
			Fx->UpdateShots(Shots, 2, true, 0.5f, ToWorld);
			const FVector Mid = ToUnreal(0, 1.f, 0.5f, 1.15f);
			SelfCheck(Fx->NumTrailInstances() == 2 && Fx->ShotDrawPos(0).Equals(Mid, 1.f), TEXT("tiro que andou: rastro e posição interpolada entre os tiques"));
			Fx->Flash(Mid, ETdfFx::Impact, 2, 1.f);
			const int32 Flashes = Fx->NumFlashes();
			Fx->TickFx(3.f);
			SelfCheck(Flashes >= 1 && Fx->NumFlashes() == 0, TEXT("clarão aparece e some sozinho"));
			// empurrão (efeito 3 da Sim): onda de ar e poeira onde o bicho está
			TdfEnemy Pushed = {};
			Pushed.Lane = 1; Pushed.Slot = 4001; Pushed.Generation = 1; Pushed.Z = 4.f; Pushed.Hp = Pushed.MaxHp = 50.f;
			if (EnemyBuf.Num() <= EnemyCountNow) EnemyBuf.SetNum(EnemyCountNow + 1);
			EnemyBuf[EnemyCountNow++] = Pushed;
			TdfEvent Ev = {};
			Ev.Kind = TDF_EV_STATUS_APPLIED; Ev.Lane = 1; Ev.A = 4001; Ev.C = 3; Ev.F = 0.5f;
			StatusFx(Ev);
			--EnemyCountNow;
			SelfCheck(Fx->NumFlashes() >= 2, TEXT("empurrão mostra a onda e a poeira"));
			Fx->TickFx(3.f);
		}
		// bicho de mentira na lane da IA: véu de efeito, barra de vida e morte (cai e só depois volta ao pool)
		if (bUseArt && TypeVisual(0).Mesh)
		{
			TdfEnemy Fake = {};
			Fake.Lane = 1; Fake.Slot = 4000; Fake.Generation = 1; Fake.TypeId = 0; Fake.Z = 5.f; Fake.Hp = 50.f; Fake.MaxHp = 100.f;
			Fake.Flags = TDF_ENEMY_FROZEN;
			const int32 BarsBefore = Bars.Num();
			UpdateEnemyView(Fake, true, 1.f, 0.016f);
			const FEnemyView* V = Views.Find(ViewId(Fake));
			USkeletalMeshComponent* C = V ? V->Comp : nullptr;
			SelfCheck(C && StatusMaterial(TDF_ENEMY_FROZEN) && C->GetOverlayMaterial() == StatusMaterial(TDF_ENEMY_FROZEN), TEXT("bicho congelado ganha o véu de gelo"));
			SelfCheck(Bars.Num() == BarsBefore + 1 && FMath::IsNearlyEqual(Bars.Last().Frac, 0.5f), TEXT("bicho ferido ganha barra de vida (metade)"));
			Fake.Flags = 0;
			UpdateEnemyView(Fake, false, 1.f, 0.016f);
			SelfCheck(C && C->GetOverlayMaterial() == nullptr, TEXT("o véu sai quando o efeito acaba"));
			KillView(ViewId(Fake), false);
			SelfCheck(C && NumDying() == 1 && C->IsVisible() && !Views.Contains(ViewId(Fake)), TEXT("bicho morto fica caído na cena"));
			TickDying(5.f);
			SelfCheck(C && NumDying() == 0 && !C->IsVisible() && Pool.FindOrAdd(0).Contains(C), TEXT("depois da queda o bicho volta ao pool"));
		}
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
		SelfStartGold = Lanes[0].Gold;
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
		if (UTdfHudWidget* Hud = SelfHud())
		{
			// cartão apagado = sem ouro para aquilo agora (o ouro acabou de baixar com a construção)
			Hud->Refresh();
			bool bMatch = true;
			for (int32 i = 0; i < TowerTypes.Num(); ++i) bMatch &= Hud->IsTowerCardEnabled(i) == (Lanes[0].Gold >= TowerTypes[i].Cost);
			for (int32 i = 0; i < SendTypes.Num(); ++i) bMatch &= Hud->IsSendCardEnabled(i) == (Lanes[0].Gold >= SendTypes[i].Cost);
			SelfCheck(bMatch, TEXT("cartão aceso só quando há ouro para ele"));
		}
		else SelfCheck(false, TEXT("cartão aceso só quando há ouro para ele (sem HUD)"));
		UpgradeAt(Cx, Cy);
		SelfWaitUntilTick = T + 4;
		break;
	case 2:
		SelfCheck(TowerLevelAt(Cx, Cy) == 2, TEXT("UpgradeAt: nível 2"));
		SelfGoldBefore = Lanes[0].TotalSent;
		SelfCheck(SelfHud() && SelfHud()->ClickSendCard(1), TEXT("cartão de envio 2 envia (SendUnit aceito)"));
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
		{
			// F9: salva o replay da partida em andamento num arquivo novo, no formato do FlowSim
			FString Saved, Text;
			const bool bSaved = SaveReplayNow(Saved);
			SelfCheck(bSaved && FFileHelper::LoadFileToString(Text, *Saved) && Text.StartsWith(TEXT("tdfende-replay 2")) && Text.Contains(TEXT("\nfinal "))
				&& Text.Contains(Recorded.Last()), TEXT("F9 salva o replay (cabeçalho, estado final e os comandos)"));
			// R: partida nova, com outra semente; a anterior fica gravada em partida.txt (o FlowSim a reproduz no fim do teste)
			SelfSeedBefore = Seed;
			SelfCheck(RestartMatch(), TEXT("R reinicia a partida"));
		}
		SelfWaitUntilTick = 5; // o tique volta a zero
		break;
	case 5:
	{
		const int32 StartGold = Lanes[0].Gold;
		SelfCheck(Sim.tick() < 30 && Seed != SelfSeedBefore && Recorded.Num() == 0, TEXT("partida nova: tique do começo, outra semente, gravação vazia"));
		SelfCheck(Lanes[0].TowerCount == 0 && TowerLevelAt(Cx, Cy) == 0 && StartGold >= SelfStartGold, TEXT("partida nova: sem torres e com o ouro do começo"));
		SelfCheck(Views.Num() == 0 && Dying.Num() == 0 && Fx->NumShotInstances() == 0, TEXT("partida nova: nenhum bicho, morto ou tiro da anterior na tela"));
		FString Text;
		SelfCheck(FFileHelper::LoadFileToString(Text, *RecordingPath()) && Text.Contains(TEXT(" sell ")), TEXT("a partida anterior ficou gravada em partida.txt"));
	}
		SelfStep = 6; // controle de verdade (mouse e teclas pelo Slate), depois o GC e a saída
		return;
	case 6:
		// -TdfSemControle: pula o teste de mouse (não mexe o cursor do usuário; usado pelo CacarQueda.ps1, que só quer o desenho)
		if (!FParse::Param(FCommandLine::Get(), TEXT("TdfSemControle")) && !RunInputTest()) return; // um passo por quadro; devolve true quando acabou
		SelfStep = 7;
		return;
	case 7:
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
		UE_LOG(LogTdf, Display, TEXT("AUTOTESTE %s (%d falha(s))"), SelfFailures != 0 ? TEXT("REPROVOU") : SelfInconclusive ? TEXT("PASSOU, MAS O CONTROLE FICOU INCONCLUSIVO") : TEXT("PASSOU"), SelfFailures);
		bSelfTest = false;
		// a saída forçada encerra o processo na hora: sem o Flush o resultado não chegava ao log do jogo empacotado
		GLog->Flush();
		FPlatformMisc::RequestExitWithStatus(true, SelfFailures != 0 ? 1 : SelfInconclusive ? 4 : 0); // 4 = inconclusivo: o mouse real mexeu
		return;
	}
	++SelfStep;
}
