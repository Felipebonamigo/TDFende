#include "TdfDemoGameMode.h"
#include "TdfSimActor.h"
#include "TdfEnvironment.h"
#include "TdfPlayerController.h"
#include "TdfHUD.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "Engine/DirectionalLight.h"
#include "Components/DirectionalLightComponent.h"
#include "Engine/SkyLight.h"
#include "Components/SkyLightComponent.h"
#include "Engine/World.h"
#include "GameFramework/PlayerController.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "Engine/StaticMesh.h"
#include "Engine/StaticMeshActor.h"
#include "Components/StaticMeshComponent.h"

ATdfDemoGameMode::ATdfDemoGameMode()
{
	PlayerControllerClass = ATdfPlayerController::StaticClass();
	HUDClass = ATdfHUD::StaticClass();
	DefaultPawnClass = nullptr; // a câmera é um ator movido pelo controle (WASD e roda do mouse); nada de pawn
}

void ATdfDemoGameMode::StartPlay()
{
	UWorld* World = GetWorld();
	FActorSpawnParameters P;
	P.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;

	ATdfSimActor* Sim = World->SpawnActor<ATdfSimActor>(FVector::ZeroVector, FRotator::ZeroRotator, P);

	// A grade da Sim é CENTRADA na origem (GridMap: x em [-W/2, W/2], z em [-H/2, H/2]). Lane 0 fica em X = 0, lane 1 em
	// X = LaneOffset (ver ATdfSimActor::ToUnreal). Os tamanhos são os padrão do ator (um replay com outra grade é exceção).
	const float Cell = Sim->CellSizeCm;
	const float LaneOffset = (Sim->GridHeight + 4) * Cell;
	const float HalfX = Sim->GridHeight * Cell * 0.5f, HalfY = Sim->GridWidth * Cell * 0.5f;

	if (FParse::Param(FCommandLine::Get(), TEXT("TdfSemAmbiente")))
	{
		// o cenário de teste antigo (piso cinza por lane, sol e luz do céu simples), para comparar ou rodar sem a arte do ambiente
		if (UStaticMesh* Plane = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Plane.Plane")))
			for (int32 Lane = 0; Lane < 2; ++Lane)
			{
				AStaticMeshActor* Floor = World->SpawnActor<AStaticMeshActor>(FVector(Lane * LaneOffset, 0.f, -2.f), FRotator::ZeroRotator, P);
				Floor->SetMobility(EComponentMobility::Movable);
				Floor->GetStaticMeshComponent()->SetStaticMesh(Plane);
				Floor->SetActorScale3D(FVector(Sim->GridHeight, Sim->GridWidth, 1.f));
			}
		ADirectionalLight* Sun = World->SpawnActor<ADirectionalLight>(FVector(0.f, 0.f, 3000.f), FRotator(-55.f, 30.f, 0.f), P);
		Sun->GetLightComponent()->SetIntensity(3.f);
		ASkyLight* Sky = World->SpawnActor<ASkyLight>(P);
		Sky->GetLightComponent()->SetIntensity(0.6f);
		Sky->GetLightComponent()->SetMobility(EComponentMobility::Movable);
		Sky->GetLightComponent()->RecaptureSky();
	}
	else
	{
		// o mundo (U1): céu, sol, névoa, chão de verdade, cenário e grama em volta das lanes
		ATdfEnvironment* Env = World->SpawnActorDeferred<ATdfEnvironment>(ATdfEnvironment::StaticClass(), FTransform::Identity);
		Env->SetPlayArea(FBox2D(FVector2D(-HalfX, -HalfY), FVector2D(LaneOffset + HalfX, HalfY)), LaneOffset, HalfX, HalfY);
		Env->FinishSpawning(FTransform::Identity);
	}

	// câmera: três-quartos, de trás da base do jogador (os bichos vêm de +X para -X), olhando a lane dele com a da IA ao fundo.
	// -TdfCamX/Y/Z/Pitch/Yaw/Fov fixam outra posição (testes e prints de perto da arte).
	const FVector Target(LaneOffset * 0.30f, 0.f, 0.f);
	float CamPitch = -30.f, CamYaw = 0.f, CamFov = 58.f, CamDist = 3600.f;
	const FRotator Rot0(CamPitch, CamYaw, 0.f);
	FVector Pos = Target - Rot0.Vector() * CamDist;
	FParse::Value(FCommandLine::Get(), TEXT("TdfCamX="), Pos.X);
	FParse::Value(FCommandLine::Get(), TEXT("TdfCamY="), Pos.Y);
	FParse::Value(FCommandLine::Get(), TEXT("TdfCamZ="), Pos.Z);
	FParse::Value(FCommandLine::Get(), TEXT("TdfCamPitch="), CamPitch);
	FParse::Value(FCommandLine::Get(), TEXT("TdfCamYaw="), CamYaw);
	FParse::Value(FCommandLine::Get(), TEXT("TdfCamFov="), CamFov);
	ACameraActor* Cam = World->SpawnActor<ACameraActor>(Pos, FRotator(CamPitch, CamYaw, 0.f), P);
	Cam->GetCameraComponent()->SetFieldOfView(CamFov);
	Cam->GetCameraComponent()->SetConstraintAspectRatio(false);

	Super::StartPlay();

	if (APlayerController* PC = World->GetFirstPlayerController()) PC->SetViewTarget(Cam);
}
