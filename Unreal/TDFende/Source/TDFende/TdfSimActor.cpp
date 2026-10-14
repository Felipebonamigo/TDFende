#include "TdfSimActor.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Misc/FileHelper.h"
#include "UObject/ConstructorHelpers.h"
#include "HAL/PlatformTime.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "UnrealClient.h"
#include "Kismet/KismetSystemLibrary.h"

DEFINE_LOG_CATEGORY_STATIC(LogTdf, Log, All);

static constexpr float FixedStep = 1.f / 30.f; // o passo fixo da Sim (TowerWarsConfig.FixedStep)

ATdfSimActor::ATdfSimActor()
{
	PrimaryActorTick.bCanEverTick = true;
	USceneComponent* Root = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
	SetRootComponent(Root);

	auto MakeMesh = [this, Root](const TCHAR* Name, const TCHAR* Asset) -> UInstancedStaticMeshComponent*
	{
		UInstancedStaticMeshComponent* C = CreateDefaultSubobject<UInstancedStaticMeshComponent>(Name);
		C->SetupAttachment(Root);
		C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
		static ConstructorHelpers::FObjectFinder<UStaticMesh> Cube(TEXT("/Engine/BasicShapes/Cube.Cube"));
		static ConstructorHelpers::FObjectFinder<UStaticMesh> Cylinder(TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
		static ConstructorHelpers::FObjectFinder<UStaticMesh> Sphere(TEXT("/Engine/BasicShapes/Sphere.Sphere"));
		const FString A(Asset);
		UStaticMesh* M = A == TEXT("cube") ? Cube.Object : A == TEXT("cyl") ? Cylinder.Object : Sphere.Object;
		C->SetStaticMesh(M);
		return C;
	};
	EnemyMesh = MakeMesh(TEXT("Inimigos"), TEXT("cube"));
	TowerMesh = MakeMesh(TEXT("Torres"), TEXT("cyl"));
	ShotMesh = MakeMesh(TEXT("Tiros"), TEXT("sphere"));
}

void ATdfSimActor::BeginPlay()
{
	Super::BeginPlay();
	FString Err;
	if (!Sim.Load(Err)) { UE_LOG(LogTdf, Error, TEXT("Ponte: %s"), *Err); return; }

	if (Sim.create(Seed, Difficulty, GridWidth, GridHeight, nullptr, nullptr) != 0)
	{
		UE_LOG(LogTdf, Error, TEXT("tdf_create falhou: %s"), *Sim.LastError());
		Sim.Unload();
		return;
	}
	char Sig[128] = {};
	Sim.signature(Sig, sizeof(Sig));
	UE_LOG(LogTdf, Log, TEXT("Ponte ok. Assinatura: %s"), UTF8_TO_TCHAR(Sig));

	FParse::Value(FCommandLine::Get(), TEXT("TdfReplay="), ReplayPath);
	FParse::Value(FCommandLine::Get(), TEXT("TdfShot="), ShotPath);
	FParse::Value(FCommandLine::Get(), TEXT("TdfShotTick="), ShotTick);
	bQuitAtEnd = FParse::Param(FCommandLine::Get(), TEXT("TdfQuit"));
	if (!ReplayPath.IsEmpty() && !LoadReplay(ReplayPath))
	{
		UE_LOG(LogTdf, Error, TEXT("Não consegui ler o replay %s"), *ReplayPath);
	}
	EnemyBuf.SetNum(4096); TowerBuf.SetNum(1024); ShotBuf.SetNum(1024); EventBuf.SetNum(4096);
	bRunning = true;
}

void ATdfSimActor::EndPlay(const EEndPlayReason::Type Reason)
{
	Sim.Unload();
	bRunning = false;
	Super::EndPlay(Reason);
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
}

void ATdfSimActor::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (!bRunning) return;

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
	Sim.events(EventBuf.GetData(), EventBuf.Num()); // por ora só drena (a vista de verdade usa os eventos para som e partículas)
	const double T2 = FPlatformTime::Seconds();
	if (ReplayTicks == 0 || Sim.tick() < ReplayTicks)
	{
		CostStepMs.Add(float((T1 - T0) * 1000.0));
		CostRedrawMs.Add(float((T2 - T1) * 1000.0));
		CostTotalMs.Add(float((T2 - T0) * 1000.0));
		MaxEnemies = FMath::Max(MaxEnemies, LastEnemies);
	}
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

FVector ATdfSimActor::ToUnreal(int32 Lane, float X, float Y, float Z) const
{
	const float LaneOffsetX = Lane * (GridHeight + 4) * CellSizeCm;
	return FVector(Z * CellSizeCm + LaneOffsetX, X * CellSizeCm, Y * CellSizeCm);
}

void ATdfSimActor::Redraw()
{
	Sim.snapshot();
	LastEnemies = Sim.enemies(EnemyBuf.GetData(), EnemyBuf.Num());
	const int32 NE = FMath::Min<int32>(LastEnemies, EnemyBuf.Num());
	const int32 NT = FMath::Min<int32>(Sim.towers(TowerBuf.GetData(), TowerBuf.Num()), TowerBuf.Num());
	const int32 NS = FMath::Min<int32>(Sim.projectiles(ShotBuf.GetData(), ShotBuf.Num()), ShotBuf.Num());

	EnemyMesh->ClearInstances();
	for (int32 i = 0; i < NE; ++i)
	{
		const TdfEnemy& E = EnemyBuf[i];
		const float Scale = 0.25f * (0.6f + 0.8f * FMath::Clamp(E.MaxHp / 400.f, 0.f, 1.f));
		EnemyMesh->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(E.Lane, E.X, E.Y, E.Z), FVector(Scale)));
	}
	TowerMesh->ClearInstances();
	for (int32 i = 0; i < NT; ++i)
	{
		const TdfTower& T = TowerBuf[i];
		TowerMesh->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(T.Lane, T.X, T.Y + 0.4f, T.Z), FVector(0.35f, 0.35f, 0.4f + 0.1f * T.Level)));
	}
	ShotMesh->ClearInstances();
	for (int32 i = 0; i < NS; ++i)
	{
		const TdfProjectile& S = ShotBuf[i];
		ShotMesh->AddInstance(FTransform(FRotator::ZeroRotator, ToUnreal(S.Lane, S.X, S.Y, S.Z), FVector(0.08f)));
	}
}
