#include "TdfDemoGameMode.h"
#include "TdfSimActor.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "Engine/DirectionalLight.h"
#include "Components/DirectionalLightComponent.h"
#include "Engine/SkyLight.h"
#include "Components/SkyLightComponent.h"
#include "Engine/World.h"
#include "GameFramework/PlayerController.h"
#include "Engine/StaticMesh.h"
#include "Engine/StaticMeshActor.h"
#include "Components/StaticMeshComponent.h"

void ATdfDemoGameMode::StartPlay()
{
	UWorld* World = GetWorld();
	FActorSpawnParameters P;
	P.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;

	ATdfSimActor* Sim = World->SpawnActor<ATdfSimActor>(FVector::ZeroVector, FRotator::ZeroRotator, P);

	// A grade da Sim é CENTRADA na origem (GridMap: x em [-W/2, W/2], z em [-H/2, H/2]). Lane 0 fica em X = 0, lane 1 em
	// X = LaneOffset (ver ATdfSimActor::ToUnreal); a câmera olha o meio das duas.
	const float Cell = Sim->CellSizeCm;
	const float LaneOffset = (Sim->GridHeight + 4) * Cell;
	const float Center = LaneOffset * 0.5f;

	// um piso por lane (o plano básico tem 100 x 100 cm)
	if (UStaticMesh* Plane = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Plane.Plane")))
	{
		for (int32 Lane = 0; Lane < 2; ++Lane)
		{
			AStaticMeshActor* Floor = World->SpawnActor<AStaticMeshActor>(FVector(Lane * LaneOffset, 0.f, -2.f), FRotator::ZeroRotator, P);
			Floor->SetMobility(EComponentMobility::Movable);
			Floor->GetStaticMeshComponent()->SetStaticMesh(Plane);
			Floor->SetActorScale3D(FVector(Sim->GridHeight, Sim->GridWidth, 1.f));
		}
	}

	ACameraActor* Cam = World->SpawnActor<ACameraActor>(FVector(Center, 0.f, 4200.f), FRotator(-90.f, 0.f, 0.f), P);
	Cam->GetCameraComponent()->SetFieldOfView(80.f);
	Cam->GetCameraComponent()->SetConstraintAspectRatio(false);

	ADirectionalLight* Sun = World->SpawnActor<ADirectionalLight>(FVector(0.f, 0.f, 3000.f), FRotator(-55.f, 30.f, 0.f), P);
	Sun->GetLightComponent()->SetIntensity(8.f);
	ASkyLight* Sky = World->SpawnActor<ASkyLight>(P);
	Sky->GetLightComponent()->SetIntensity(1.5f);
	Sky->GetLightComponent()->SetMobility(EComponentMobility::Movable);
	Sky->GetLightComponent()->RecaptureSky();

	Super::StartPlay();

	if (APlayerController* PC = World->GetFirstPlayerController()) PC->SetViewTarget(Cam);
}
