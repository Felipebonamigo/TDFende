#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "TdfDemoGameMode.generated.h"

/**
 * U0: modo de jogo de teste. Em qualquer nível (inclusive o vazio /Engine/Maps/Entry) monta sozinho o necessário para ver a Sim:
 * um TdfSimActor, uma câmera de cima sobre as duas lanes e uma luz. Serve ao teste automático por linha de comando (sem abrir o
 * editor): UnrealEditor.exe TDFende.uproject /Engine/Maps/Entry?game=/Script/TDFende.TdfDemoGameMode -game -TdfReplay=...
 */
UCLASS()
class ATdfDemoGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	ATdfDemoGameMode();
	virtual void StartPlay() override;
};
