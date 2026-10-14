#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "TdfHUD.generated.h"

/** U2: o que acompanha o mundo (contorno da célula sob o mouse e o custo do clique). Placar e loja: UTdfHudWidget (UMG). */
UCLASS()
class ATdfHUD : public AHUD
{
	GENERATED_BODY()

public:
	virtual void DrawHUD() override;

private:
	void Line(const FString& Text, float X, float Y, const FLinearColor& Color, float Scale = 1.f);
};
