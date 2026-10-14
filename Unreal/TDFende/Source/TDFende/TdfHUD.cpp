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

	const FLinearColor White(1, 1, 1), Gold(1.f, 0.82f, 0.25f), Red(1.f, 0.4f, 0.35f), Grey(0.75f, 0.75f, 0.75f), Green(0.5f, 1.f, 0.55f);
	const TdfLane& Me = S->GetLane(0);
	const TdfLane& Foe = S->GetLane(1);

	DrawRect(FLinearColor(0.f, 0.f, 0.f, 0.55f), 8.f, 8.f, 520.f, 74.f);
	Line(FString::Printf(TEXT("VOCÊ   vidas %d   ouro %d   renda %d   torres %d"), Me.Lives, Me.Gold, Me.Income, Me.TowerCount), 16.f, 14.f, Gold, 1.2f);
	Line(FString::Printf(TEXT("IA     vidas %d   renda %d   torres %d   inimigos %d"), Foe.Lives, Foe.Income, Foe.TowerCount, Foe.EnemyCount), 16.f, 38.f, Red, 1.f);
	Line(FString::Printf(TEXT("tempo %.0f s%s"), Me.MatchTime, Me.InSuddenDeath ? TEXT("   MORTE SÚBITA") : TEXT("")), 16.f, 58.f, Grey, 0.9f);

	// torres: Q / E troca a selecionada
	const TArray<FTdfCatalogItem>& Towers = S->GetTowerTypes();
	float Y = Canvas->ClipY - 150.f;
	DrawRect(FLinearColor(0.f, 0.f, 0.f, 0.55f), 8.f, Y - 8.f, 520.f, 150.f);
	Line(TEXT("Torres (Q / E troca) — clique esquerdo constrói ou sobe, direito vende"), 16.f, Y, Grey, 0.9f);
	for (int32 i = 0; i < Towers.Num(); ++i)
	{
		const bool bSel = i == S->GetSelectedTower();
		const FString Txt = FString::Printf(TEXT("%s%s %d"), bSel ? TEXT("> ") : TEXT("  "), *Towers[i].Name, Towers[i].Cost);
		Line(Txt, 16.f + (i % 3) * 170.f, Y + 24.f + (i / 3) * 24.f, bSel ? Green : (Me.Gold >= Towers[i].Cost ? White : Grey), bSel ? 1.1f : 1.f);
	}

	// envios: teclas 1 a 9
	const TArray<FTdfCatalogItem>& Sends = S->GetSendTypes();
	Line(TEXT("Enviar bichos (teclas 1-9)"), 16.f, Y + 78.f, Grey, 0.9f);
	for (int32 i = 0; i < Sends.Num(); ++i)
	{
		const FString Txt = FString::Printf(TEXT("[%d] %s %d"), i + 1, *Sends[i].Name, Sends[i].Cost);
		Line(Txt, 16.f + (i % 5) * 100.f, Y + 100.f + (i / 5) * 22.f, Me.Gold >= Sends[i].Cost ? White : Grey, 0.85f);
	}

	// célula sob o mouse: contorno e custo
	const FIntPoint H = PC->GetHoverCell();
	if (H.X >= 0)
	{
		const int32 Level = S->TowerLevelAt(H.X, H.Y);
		FString Info; FLinearColor C = Green;
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

	// aviso curto (comando recusado, fim da partida)
	if (S->GetToastAge() < 3.f && !S->GetToast().IsEmpty())
		Line(S->GetToast(), Canvas->ClipX * 0.5f - 140.f, 24.f, FLinearColor(1.f, 0.7f, 0.3f, FMath::Clamp(3.f - S->GetToastAge(), 0.f, 1.f)), 1.4f);
}
