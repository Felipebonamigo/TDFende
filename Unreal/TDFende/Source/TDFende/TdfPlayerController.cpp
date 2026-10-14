#include "TdfPlayerController.h"
#include "TdfSimActor.h"
#include "EngineUtils.h"
#include "Engine/World.h"
#include "GameFramework/PlayerInput.h"

ATdfPlayerController::ATdfPlayerController()
{
	bShowMouseCursor = true;
	DefaultMouseCursor = EMouseCursor::Crosshairs;
}

void ATdfPlayerController::PlayerTick(float DeltaTime)
{
	Super::PlayerTick(DeltaTime);

	if (!Sim.IsValid())
	{
		for (TActorIterator<ATdfSimActor> It(GetWorld()); It; ++It) { Sim = *It; break; }
		if (!Sim.IsValid()) return;
	}
	ATdfSimActor* S = Sim.Get();

	// célula sob o mouse: raio da câmera contra o plano do chão (Z = 0)
	HoverCell = FIntPoint(-1, -1);
	FVector Origin, Dir;
	if (DeprojectMousePositionToWorld(Origin, Dir) && FMath::Abs(Dir.Z) > KINDA_SMALL_NUMBER)
	{
		const float T = -Origin.Z / Dir.Z;
		if (T > 0.f) HoverCell = S->WorldToCell(Origin + Dir * T);
	}

	if (WasInputKeyJustPressed(EKeys::Q)) S->CycleTower(-1);
	if (WasInputKeyJustPressed(EKeys::E)) S->CycleTower(+1);
	static const FKey Digits[9] = { EKeys::One, EKeys::Two, EKeys::Three, EKeys::Four, EKeys::Five, EKeys::Six, EKeys::Seven, EKeys::Eight, EKeys::Nine };
	for (int32 i = 0; i < 9; ++i)
		if (WasInputKeyJustPressed(Digits[i])) S->SendUnit(i);

	if (HoverCell.X >= 0)
	{
		if (WasInputKeyJustPressed(EKeys::LeftMouseButton))
		{
			if (S->TowerLevelAt(HoverCell.X, HoverCell.Y) > 0) S->UpgradeAt(HoverCell.X, HoverCell.Y);
			else S->BuildAt(HoverCell.X, HoverCell.Y);
		}
		if (WasInputKeyJustPressed(EKeys::RightMouseButton)) S->SellAt(HoverCell.X, HoverCell.Y);
	}
}
