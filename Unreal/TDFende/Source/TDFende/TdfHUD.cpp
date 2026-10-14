#include "TdfHUD.h"
#include "TdfPlayerController.h"
#include "TdfSimActor.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"

void ATdfHUD::Line(const FString& Text, float X, float Y, const FLinearColor& Color, float Scale)
{
	DrawText(Text, Color, X, Y, GEngine->GetMediumFont(), Scale);
}

void ATdfHUD::DrawHUD()
{
	Super::DrawHUD();
	ATdfPlayerController* PC = Cast<ATdfPlayerController>(GetOwningPlayerController());
	ATdfSimActor* S = PC ? PC->GetSim() : nullptr;
	if (!S || !S->IsRunning()) return;

	// placar, loja e avisos ficam no HUD em UMG (UTdfHudWidget); aqui só o que acompanha o mundo:
	// barras de vida dos bichos feridos
	for (const FTdfBar& B : S->GetBars())
	{
		FVector2D P;
		if (!PC->ProjectWorldLocationToScreen(B.Head, P)) continue;
		if (P.X < -40.f || P.Y < -20.f || P.X > Canvas->ClipX + 40.f || P.Y > Canvas->ClipY + 20.f) continue;
		const float W = 26.f + 16.f * FMath::Clamp(B.MaxHp / 600.f, 0.f, 1.f), Hh = 4.f; // bicho mais forte, barra mais larga
		// fundo escuro; com gelo fica azulado, com fogo alaranjado
		const FLinearColor Back = (B.Flags & TDF_ENEMY_BURNING) ? FLinearColor(0.55f, 0.18f, 0.02f, 0.85f)
			: (B.Flags & (TDF_ENEMY_FROZEN | TDF_ENEMY_SLOWED)) ? FLinearColor(0.05f, 0.30f, 0.55f, 0.85f) : FLinearColor(0.f, 0.f, 0.f, 0.6f);
		DrawRect(Back, P.X - W * 0.5f - 1.f, P.Y - 1.f, W + 2.f, Hh + 2.f);
		const FLinearColor Fill = B.Frac > 0.5f ? FMath::Lerp(FLinearColor(1.f, 0.85f, 0.2f), FLinearColor(0.35f, 0.9f, 0.35f), (B.Frac - 0.5f) * 2.f)
			: FMath::Lerp(FLinearColor(0.95f, 0.22f, 0.15f), FLinearColor(1.f, 0.85f, 0.2f), B.Frac * 2.f);
		DrawRect(Fill, P.X - W * 0.5f, P.Y, W * B.Frac, Hh);
	}

	// a célula sob o mouse, com contorno e o custo do que o clique faria
	const FLinearColor Green(0.5f, 1.f, 0.55f), Red(1.f, 0.4f, 0.35f);
	const TdfLane& Me = S->GetLane(0);
	const TArray<FTdfCatalogItem>& Towers = S->GetTowerTypes();
	const FIntPoint H = PC->GetHoverCell();
	if (H.X < 0) return;

	const int32 Level = S->TowerLevelAt(H.X, H.Y);
	FString Info;
	FLinearColor C = Green;
	if (Level > 0)
	{
		const int32 Cost = S->UpgradeCost(H.X, H.Y);
		Info = Cost == 0 ? FString::Printf(TEXT("torre nível %d (máximo)"), Level) : FString::Printf(TEXT("subir para o nível %d: %d"), Level + 1, Cost);
		C = Cost > 0 && Me.Gold >= Cost ? FLinearColor(1.f, 0.9f, 0.3f) : Red;
	}
	else
	{
		const int32 Why = S->CanBuildSelected(H.X, H.Y);
		const FTdfCatalogItem* T = Towers.IsValidIndex(S->GetSelectedTower()) ? &Towers[S->GetSelectedTower()] : nullptr;
		Info = FString::Printf(TEXT("construir %s: %d%s"), T ? *T->Name : TEXT("?"), T ? T->Cost : 0, S->InsideTerritory(H.X, H.Y) ? TEXT("  (no território)") : TEXT(""));
		if (Why != 0) C = Red;
	}
	const float Half = S->CellSizeCm * 0.5f;
	FVector2D P[4];
	const FVector Cc = S->CellToWorld(H.X, H.Y, 2.f);
	const FVector Corners[4] = { Cc + FVector(-Half, -Half, 0), Cc + FVector(-Half, Half, 0), Cc + FVector(Half, Half, 0), Cc + FVector(Half, -Half, 0) };
	bool bOk = true;
	for (int32 i = 0; i < 4; ++i) bOk &= PC->ProjectWorldLocationToScreen(Corners[i], P[i]);
	if (bOk)
		for (int32 i = 0; i < 4; ++i) DrawLine(P[i].X, P[i].Y, P[(i + 1) % 4].X, P[(i + 1) % 4].Y, C, 2.f);
	float Mx, My;
	if (PC->GetMousePosition(Mx, My)) Line(Info, Mx + 16.f, My + 12.f, C, 1.f);
}
