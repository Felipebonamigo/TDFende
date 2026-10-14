#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "TdfHUD.generated.h"

/** U2: HUD provisório (texto e retângulos no Canvas). A interface de verdade (UMG, loja de torres e bichos) vem na fase de UI. */
UCLASS()
class ATdfHUD : public AHUD
{
	GENERATED_BODY()

public:
	virtual void DrawHUD() override;

private:
	void Line(const FString& Text, float X, float Y, const FLinearColor& Color, float Scale = 1.f);
};
