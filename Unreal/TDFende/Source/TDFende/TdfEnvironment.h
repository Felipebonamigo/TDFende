#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "TdfEnvironment.generated.h"

class USkyAtmosphereComponent;
class UVolumetricCloudComponent;
class UExponentialHeightFogComponent;
class UDirectionalLightComponent;
class USkyLightComponent;
class UPostProcessComponent;
class UStaticMeshComponent;
class UHierarchicalInstancedStaticMeshComponent;
class UStaticMesh;

/**
 * U1 (fatia de beleza): o mundo em volta das lanes. Céu físico (atmosfera + nuvens volumétricas), névoa de altura, sol e luz do
 * céu dinâmicos (o Lumen faz a luz indireta), pós-processo, um chão grande com o material M_TdfChao e o cenário da Poly Haven
 * (paredões e rochas ao longe, tocos, troncos e pedras perto, grama 3D fora das lanes), espalhado de forma determinística.
 *
 * A arte vem de /Game/TDFende/Ambiente (Scripts/importa_ambiente.py). Sem ela, cai num chão liso: o jogo continua jogável.
 * As lanes ocupam X em [-H/2, H/2] (lane 0) e [L - H/2, L + H/2] (lane 1, L = deslocamento), Y em [-W/2, W/2] (em cm);
 * nada de cenário entra nelas nem no corredor entre as duas.
 */
UCLASS()
class ATdfEnvironment : public AActor
{
	GENERATED_BODY()

public:
	ATdfEnvironment();

	/** Área de jogo (as duas lanes e o corredor), em cm, e onde fica cada lane: chamado pelo modo de jogo antes do BeginPlay. */
	void SetPlayArea(const FBox2D& Area, float InLaneOffsetX, float InLaneHalfX, float InLaneHalfY);

	/** Sol: altura (graus acima do horizonte) e direção. */
	UPROPERTY(EditAnywhere, Category = "TDFende") float SunElevation = 38.f;
	UPROPERTY(EditAnywhere, Category = "TDFende") float SunAzimuth = 125.f;
	UPROPERTY(EditAnywhere, Category = "TDFende") int32 Seed = 7;
	/** Quantas touceiras de grama (fora das lanes). */
	UPROPERTY(EditAnywhere, Category = "TDFende") int32 GrassCount = 2600;

protected:
	virtual void BeginPlay() override;

private:
	void BuildGround();
	void ScatterScenery();
	void ScatterGrass();
	void LineBorders();
	bool InsidePlay(const FVector2D& P, float Margin) const;
	UHierarchicalInstancedStaticMeshComponent* MakeHism(UStaticMesh* Mesh, float CullCm, bool bShadows);

	FBox2D PlayArea = FBox2D(FVector2D(-900.f, -1300.f), FVector2D(2900.f, 1300.f));
	float LaneOffsetX = 2000.f, LaneHalfX = 800.f, LaneHalfY = 1200.f;

	UPROPERTY() USceneComponent* RootScene = nullptr;
	UPROPERTY() USkyAtmosphereComponent* Sky = nullptr;
	UPROPERTY() UVolumetricCloudComponent* Clouds = nullptr;
	UPROPERTY() UExponentialHeightFogComponent* Fog = nullptr;
	UPROPERTY() UDirectionalLightComponent* Sun = nullptr;
	UPROPERTY() USkyLightComponent* SkyLight = nullptr;
	UPROPERTY() UPostProcessComponent* Post = nullptr;
	UPROPERTY() UStaticMeshComponent* Ground = nullptr;
	/** Componentes e malhas criados em tempo de execução (o GC só respeita o que está em UPROPERTY). */
	UPROPERTY() TArray<TObjectPtr<UObject>> KeepAlive;
};
