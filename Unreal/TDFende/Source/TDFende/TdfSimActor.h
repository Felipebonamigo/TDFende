#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "TdfSim.h"
#include "TdfSimActor.generated.h"

class UInstancedStaticMeshComponent;
class UMaterialInstanceDynamic;
class UStaticMesh;

/** Uma linha do catálogo (torre ou envio) como a ponte entrega: tdf_catalog_text. */
struct FTdfCatalogItem
{
	FString Key, Name;
	int32 Cost = 0;
	/** Torre: alcance, recarga, dano, raio de área, raio de fronteira. Envio: vida, velocidade, quantidade, renda, recompensa. */
	float F[6] = {};
};

/**
 * Vista da Sim do TDFende no Unreal (U0/U2). Solte UM destes no nível (ou use ATdfDemoGameMode) e dê Play.
 * Roda a Sim a 30 Hz fixos (acumulador), lê o snapshot e instancia formas simples: cubos (inimigos), cilindros (torres) e esferas
 * (tiros). A vista de verdade (U3) troca as formas por malhas por CHAVE do catálogo.
 *
 * Comandos do jogador (U2): BuildAt / UpgradeAt / SellAt / SendUnit, chamados pelo ATdfPlayerController (mouse e teclado) e pelo
 * autoteste. Tudo entra na fila da Sim e vale no próximo tique; o jogo grava os comandos em <Saved>/Tdf/partida.txt no formato do replay.
 *
 * Com ReplayPath preenchido, reproduz um replay de texto (formato do FlowSim) e escreve o fingerprint no log: tem que ser IGUAL ao de
 * `dotnet run --project Tools/FlowSim -v quiet -- replay <arquivo> --forcar` (critério go/no-go da U0, ainda vale).
 *
 * Coordenadas: Sim (x direita, y cima, z frente; unidades de célula; a grade é centrada na origem) -> Unreal (X = z, Y = x, Z = y; cm).
 * A lane 1 fica deslocada em X.
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
	/** Caminho absoluto de um replay de texto (opcional). Vazio = a IA joga sozinha e o jogador joga pelo mouse e teclado. */
	UPROPERTY(EditAnywhere, Category = "TDFende") FString ReplayPath;

	/**
	 * Opções de linha de comando (teste automático, sem abrir o editor): -TdfReplay=<arquivo> preenche ReplayPath;
	 * -TdfShot=<arquivo.png> e -TdfShotTick=<n> tiram um print do viewport no tique n; -TdfQuit fecha o jogo ao fim do replay;
	 * -TdfQuitTick=<n> fecha o jogo no tique n (depois do print); -TdfSelfTest roda o autoteste de jogo (construir, subir, enviar, vender, conversão célula<->mundo) e sai com 0 ou 1.
	 */

	// ---- comandos do jogador (lane 0) ----
	bool BuildAt(int32 CellX, int32 CellY);
	bool UpgradeAt(int32 CellX, int32 CellY);
	bool SellAt(int32 CellX, int32 CellY);
	bool SendUnit(int32 SendId);
	void SelectTower(int32 TypeId) { SelectedTower = FMath::Clamp(TypeId, 0, FMath::Max(0, TowerTypes.Num() - 1)); }
	void CycleTower(int32 Delta);
	int32 GetSelectedTower() const { return SelectedTower; }

	// ---- perguntas para a vista / HUD ----
	bool IsRunning() const { return bRunning; }
	const TArray<FTdfCatalogItem>& GetTowerTypes() const { return TowerTypes; }
	const TArray<FTdfCatalogItem>& GetSendTypes() const { return SendTypes; }
	const TdfLane& GetLane(int32 Lane) const { return Lanes[FMath::Clamp(Lane, 0, 1)]; }
	int32 GetTick() const { return bRunning ? Sim.tick() : 0; }
	/** Nível da torre do jogador na célula, ou 0 se não há. */
	int32 TowerLevelAt(int32 CellX, int32 CellY) const;
	/** 0 = dá para construir o tipo selecionado ali; senão o RejectReason da Sim. */
	int32 CanBuildSelected(int32 CellX, int32 CellY) const;
	/** -1 sem torre, 0 no nível máximo, senão o custo de subir. */
	int32 UpgradeCost(int32 CellX, int32 CellY) const;
	bool InsideTerritory(int32 CellX, int32 CellY) const;
	/** Última mensagem curta para o HUD (comando recusado etc.). */
	const FString& GetToast() const { return Toast; }
	float GetToastAge() const { return ToastAge; }

	/** Conversões da lane do jogador (lane 0). A célula inválida volta (-1,-1). */
	FIntPoint WorldToCell(const FVector& World) const;
	FVector CellToWorld(int32 CellX, int32 CellY, float Z = 0.f) const;
	bool CellInBounds(int32 CellX, int32 CellY) const { return CellX >= 0 && CellY >= 0 && CellX < GridWidth && CellY < GridHeight; }

protected:
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type Reason) override;
	virtual void Tick(float DeltaSeconds) override;

private:
	struct FReplayCmd { int32 Tick, Kind, X, Y, Arg; };
	static constexpr int32 TowerKinds = 6;
	static constexpr int32 EnemyKinds = 4; // normal, lento/congelado, queimando, voador

	bool LoadReplay(const FString& Path);
	void LoadCatalog();
	void StepOnce();
	void Redraw();
	void ReportCost() const;
	void Issue(int32 Kind, int32 X, int32 Y, int32 Arg, const TCHAR* Word);
	void ToastMsg(const FString& Text) { Toast = Text; ToastAge = 0.f; }
	void SaveRecording() const;
	FVector ToUnreal(int32 Lane, float X, float Y, float Z) const;
	void RunSelfTest(float DeltaSeconds);

	FTdfSim Sim;
	bool bRunning = false;
	float Accumulator = 0.f;
	int32 ReplayTicks = 0;
	int32 NextCmd = 0;
	TArray<FReplayCmd> Replay;
	bool bReplaying = false;

	TArray<FTdfCatalogItem> TowerTypes, SendTypes;
	int32 SelectedTower = 0;
	TdfLane Lanes[2] = {};
	TArray<uint8> TerritoryBuf;
	FString Toast;
	float ToastAge = 99.f;
	TArray<FString> Recorded; // comandos do jogador, formato do replay

	UPROPERTY() USceneComponent* RootScene = nullptr;
	UPROPERTY() UInstancedStaticMeshComponent* EnemyMesh[EnemyKinds] = {};
	UPROPERTY() UInstancedStaticMeshComponent* TowerMesh[TowerKinds] = {};
	UPROPERTY() UInstancedStaticMeshComponent* ShotMesh = nullptr;
	UPROPERTY() UInstancedStaticMeshComponent* TerritoryMesh = nullptr;
	UPROPERTY() TArray<UMaterialInstanceDynamic*> Mids;

	TArray<TdfEnemy> EnemyBuf;
	TArray<TdfTower> TowerBuf;
	TArray<TdfProjectile> ShotBuf;
	TArray<TdfEvent> EventBuf;
	int32 EnemyCountNow = 0, TowerCountNow = 0;

	FString ShotPath;
	int32 ShotTick = 0;
	int32 QuitTick = 0;
	bool bQuitAtEnd = false;
	int32 LastEnemies = 0, MaxEnemies = 0;
	TArray<float> CostStepMs, CostRedrawMs, CostTotalMs;

	// autoteste
	bool bSelfTest = false;
	int32 SelfStep = 0;
	int32 SelfWaitUntilTick = 0;
	int32 SelfFailures = 0;
	int32 SelfGoldBefore = 0;
	void SelfCheck(bool bCondition, const TCHAR* What);
};
