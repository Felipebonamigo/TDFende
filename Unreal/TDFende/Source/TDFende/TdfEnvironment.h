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
class USkeletalMeshComponent;

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

	virtual void Tick(float DeltaSeconds) override;

	/**
	 * Clima (pré-visualização do tema, só luz, névoa e cor; a arte é a mesma): padrao, tarde, nevoa, outono, noite. Devolve false se o
	 * nome não existe. `-TdfTema=<nome>` aplica ao começar; "padrao" volta ao que o ambiente tinha.
	 */
	bool ApplyTheme(const FString& Name);
	/** Altura do sol em graus acima do horizonte, como está agora. */
	float SunElevationNow() const;

	/**
	 * Águias de ambiente: algumas, alto no céu, em volta da área de jogo, no modelo e na animação do próprio jogo (as mesmas da
	 * águia que se envia). Orbitam devagar, cada uma num raio, altura e sentido; a asa bate na animação de voo.
	 */
	int32 NumBirds() const { return Birds.Num(); }
	FVector BirdPos(int32 Index) const;
	/** Anda as águias (o Tick chama; o autoteste chama direto para não esperar). */
	void AdvanceBirds(float DeltaSeconds);

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
	void ScatterNature();
	void LineBorders();
	void SpawnBirds();

	struct FBird
	{
		USkeletalMeshComponent* Comp = nullptr;
		float Radius = 0.f, Height = 0.f, Angle = 0.f, Speed = 0.f; // volta em rad/s, com sinal: sentido
		float Bank = 0.f;                                            // inclinação nas curvas
	};
	TArray<FBird> Birds;
	float BirdScale = 1.f;
	float BirdYawOffset = -90.f; // a malha da águia olha para -Y no arquivo: giro que leva a frente ao +X (o mesmo do bicho do jogo)
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
