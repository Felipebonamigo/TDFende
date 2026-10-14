#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "TdfSim.h"
#include "TdfSimActor.generated.h"

class UInstancedStaticMeshComponent;

/**
 * U0: desenha a Sim do TDFende com formas simples. Solte UM destes no nível e dê Play.
 * Roda a Sim a 30 Hz fixos (acumulador), lê o snapshot e instancia cubos (inimigos), cilindros (torres) e esferas (tiros).
 * Com ReplayPath preenchido, reproduz um replay de texto (formato do FlowSim) e escreve o fingerprint no log: tem que ser IGUAL
 * ao de `dotnet run --project Tools/FlowSim -v quiet -- replay <arquivo> --forcar`. Esse é o critério go/no-go da U0.
 *
 * Coordenadas: Sim (x direita, y cima, z frente; unidades de célula; a grade é centrada na origem) -> Unreal (X = z, Y = x, Z = y; cm). A lane 1 fica deslocada em X.
 */
UCLASS()
class ATdfSimActor : public AActor
{
	GENERATED_BODY()

public:
	ATdfSimActor();

	UPROPERTY(EditAnywhere, Category = "TDFende") int32 Seed = 2024;
	/** 0 Fácil, 1 Normal, 2 Difícil. */
	UPROPERTY(EditAnywhere, Category = "TDFende") int32 Difficulty = 1;
	UPROPERTY(EditAnywhere, Category = "TDFende") int32 GridWidth = 24;
	UPROPERTY(EditAnywhere, Category = "TDFende") int32 GridHeight = 16;
	UPROPERTY(EditAnywhere, Category = "TDFende") float CellSizeCm = 100.f;
	/** Caminho absoluto de um replay de texto (opcional). Vazio = a IA joga sozinha e o jogador não faz nada. */
	UPROPERTY(EditAnywhere, Category = "TDFende") FString ReplayPath;

	/**
	 * Opções de linha de comando (teste automático, sem abrir o editor): -TdfReplay=<arquivo> preenche ReplayPath;
	 * -TdfShot=<arquivo.png> e -TdfShotTick=<n> tiram um print do viewport no tique n; -TdfQuit fecha o jogo ao fim do replay.
	 * No fim do replay o log traz o FINGERPRINT e o custo por quadro (step + Redraw + events).
	 */

protected:
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type Reason) override;
	virtual void Tick(float DeltaSeconds) override;

private:
	struct FReplayCmd { int32 Tick, Kind, X, Y, Arg; };

	bool LoadReplay(const FString& Path);
	void StepOnce();
	void Redraw();
	void ReportCost() const;
	FVector ToUnreal(int32 Lane, float X, float Y, float Z) const;

	FTdfSim Sim;
	bool bRunning = false;
	float Accumulator = 0.f;
	int32 ReplayTicks = 0;
	int32 NextCmd = 0;
	TArray<FReplayCmd> Replay;

	UPROPERTY() UInstancedStaticMeshComponent* EnemyMesh = nullptr;
	UPROPERTY() UInstancedStaticMeshComponent* TowerMesh = nullptr;
	UPROPERTY() UInstancedStaticMeshComponent* ShotMesh = nullptr;

	TArray<TdfEnemy> EnemyBuf;
	TArray<TdfTower> TowerBuf;
	TArray<TdfProjectile> ShotBuf;
	TArray<TdfEvent> EventBuf;

	FString ShotPath;
	int32 ShotTick = 0;
	bool bQuitAtEnd = false;
	int32 LastEnemies = 0, MaxEnemies = 0;
	TArray<float> CostStepMs, CostRedrawMs, CostTotalMs;
};
