#pragma once

#include "CoreMinimal.h"
#include "Components/SceneComponent.h"
#include "TdfSim.h"
#include "TdfFxComponent.generated.h"

class UInstancedStaticMeshComponent;
class UMaterialInterface;

/** Estilos de clarão (efeito curto que nasce, cresce, apaga e some sozinho). */
enum class ETdfFx : uint8
{
	Muzzle,   // boca do cano, no disparo
	Impact,   // onde o tiro acerta
	Dust,     // poeira (tiro pesado no chão, bicho que cai)
};

/**
 * U2: efeitos de combate da vista, sem Niagara (tudo instanciado, barato e determinístico):
 *   tiros com a cara do tipo de torre (bala de ferro, bola de fogo, cristal de gelo...), interpolados entre os tiques da Sim
 *   (cada projétil é casado com o do tique anterior: mesma lane e tipo, o mais perto) e com rastro na direção em que andou;
 *   clarões de disparo e de impacto e poeira, que crescem, apagam e somem sozinhos.
 * Cor e brilho vão por instância (dados customizados 0-3: cor RGB e intensidade) no material M_TdfBrilho (aditivo) e M_TdfFumaca
 * (translúcido), criados por Scripts/importa_efeitos.py. Sem eles, cai no material básico da engine.
 */
UCLASS()
class UTdfFxComponent : public USceneComponent
{
	GENERATED_BODY()

public:
	UTdfFxComponent();

	/** Cria os componentes instanciados (chamar no BeginPlay do dono). */
	void Setup();

	/**
	 * Projéteis do snapshot. bNewTick: a Sim andou desde a última chamada (casa com o tique anterior). Alpha: fração do tique atual
	 * (0 = posição do tique anterior, 1 = a do atual). ToWorld converte (lane, x, y, z) da Sim para o mundo.
	 */
	void UpdateShots(const TdfProjectile* Shots, int32 Count, bool bNewTick, float Alpha, TFunctionRef<FVector(int32, float, float, float)> ToWorld);
	/** Clarão do estilo, na cor do tipo de torre (0-5; -1 = neutro), com tamanho relativo. */
	void Flash(const FVector& At, ETdfFx Style, int32 TowerType, float Size = 1.f);
	/** Anda os clarões (crescer, apagar, sumir). */
	void TickFx(float DeltaSeconds);
	/** Partida nova: some tudo (tiros, rastros, clarões, poeira) e esquece o tique anterior. */
	void Clear();

	int32 NumShotInstances() const { return Drawn.Num(); }
	int32 NumTrailInstances() const { return TrailCount; }
	int32 NumFlashes() const { return Live.Num(); }
	/** Onde o projétil i (ordem do último UpdateShots) foi desenhado. */
	FVector ShotDrawPos(int32 Index) const { return Drawn.IsValidIndex(Index) ? Drawn[Index] : FVector(FLT_MAX); }

private:
	struct FShotKey
	{
		int32 Lane = 0, Type = 0;
		FVector Pos = FVector::ZeroVector;
	};
	struct FLive
	{
		FVector Pos = FVector::ZeroVector;
		FLinearColor Color = FLinearColor::White;
		ETdfFx Style = ETdfFx::Impact;
		float Age = 0.f, Life = 0.2f, Size = 1.f;
	};

	UInstancedStaticMeshComponent* MakeIsm(const TCHAR* Name, UMaterialInterface* Material);
	/** Põe as instâncias da lista (transformação + cor RGB e intensidade/opacidade) no componente, trocando as anteriores. */
	void Fill(UInstancedStaticMeshComponent* C, const TArray<FTransform>& Xf, const TArray<FLinearColor>& Data);
	void RedrawLive();

	UPROPERTY() TObjectPtr<UInstancedStaticMeshComponent> Iron;    // bala sólida (canhão, morteiro)
	UPROPERTY() TObjectPtr<UInstancedStaticMeshComponent> Glow;    // núcleo que brilha (gelo, sentinela, fogo, ar) e rastros de luz
	UPROPERTY() TObjectPtr<UInstancedStaticMeshComponent> Smoke;   // rastro de fumaça das balas e poeira
	UPROPERTY() TObjectPtr<UInstancedStaticMeshComponent> Flashes; // clarões (aditivo)
	UPROPERTY() TObjectPtr<UInstancedStaticMeshComponent> Puffs;   // poeira (translúcida)

	TArray<FShotKey> Prev, Cur;
	TArray<FVector> From;          // de onde cada projétil atual veio (posição casada no tique anterior; ele mesmo se é novo)
	TArray<FVector> Drawn;
	int32 TrailCount = 0;
	TArray<FLive> Live;
	bool bLiveDirty = false;
	// buffers reaproveitados (nada de alocação por quadro)
	TArray<FTransform> XIron, XGlow, XSmoke;
	TArray<FLinearColor> DIron, DGlow, DSmoke;
};
