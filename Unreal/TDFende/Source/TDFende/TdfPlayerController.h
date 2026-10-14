#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "TdfPlayerController.generated.h"

class ATdfSimActor;

/**
 * U2: o jogador. Mouse e teclado viram comandos da Sim (lane 0), pelo ATdfSimActor:
 *   botão esquerdo  = construir a torre selecionada na célula sob o mouse; se já há torre ali, subir de nível
 *   botão direito   = vender a torre da célula
 *   Q / E           = torre anterior / próxima        1 a 9 = enviar o bicho do mesmo número
 */
UCLASS()
class ATdfPlayerController : public APlayerController
{
	GENERATED_BODY()

public:
	ATdfPlayerController();

	/** Célula do jogador sob o mouse, ou (-1,-1). */
	FIntPoint GetHoverCell() const { return HoverCell; }
	ATdfSimActor* GetSim() const { return Sim.Get(); }

	virtual void PlayerTick(float DeltaTime) override;

private:
	TWeakObjectPtr<ATdfSimActor> Sim;
	FIntPoint HoverCell = FIntPoint(-1, -1);
};
