#include "TdfPlayerController.h"
#include "TdfSimActor.h"
#include "TdfHudWidget.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "EngineUtils.h"
#include "Engine/World.h"
#include "GameFramework/PlayerInput.h"
#include "Camera/CameraActor.h"

ATdfPlayerController::ATdfPlayerController()
{
	bShowMouseCursor = true;
	DefaultMouseCursor = EMouseCursor::Crosshairs;
}

void ATdfPlayerController::MoveCamera(float DeltaTime)
{
	ACameraActor* Cam = Cast<ACameraActor>(GetViewTarget());
	if (!Cam || !Sim.IsValid()) return;
	FVector Pos = Cam->GetActorLocation();
	const FVector Fwd = Cam->GetActorForwardVector();
	// mover no plano do chão; mais alto = mais rápido (a sensação de velocidade na tela fica parecida)
	FVector2D Pan(0.f, 0.f);
	if (IsInputKeyDown(EKeys::W) || IsInputKeyDown(EKeys::Up)) Pan.X += 1.f;
	if (IsInputKeyDown(EKeys::S) || IsInputKeyDown(EKeys::Down)) Pan.X -= 1.f;
	if (IsInputKeyDown(EKeys::D) || IsInputKeyDown(EKeys::Right)) Pan.Y += 1.f;
	if (IsInputKeyDown(EKeys::A) || IsInputKeyDown(EKeys::Left)) Pan.Y -= 1.f;
	const float Speed = 600.f + 0.9f * Pos.Z;
	Pos.X += Pan.X * Speed * DeltaTime;
	Pos.Y += Pan.Y * Speed * DeltaTime;
	// zoom: anda ao longo da direção da câmera, entre 4 m e 45 m de altura
	float Wheel = 0.f;
	if (WasInputKeyJustPressed(EKeys::MouseScrollUp)) Wheel += 1.f;
	if (WasInputKeyJustPressed(EKeys::MouseScrollDown)) Wheel -= 1.f;
	if (Wheel != 0.f && FMath::Abs(Fwd.Z) > 0.05f)
	{
		const FVector Next = Pos + Fwd * Wheel * (0.12f * Pos.Z / FMath::Abs(Fwd.Z));
		if (Next.Z > 400.f && Next.Z < 4500.f) Pos = Next;
	}
	// não deixa a câmera fugir das lanes
	const ATdfSimActor* S = Sim.Get();
	const float LaneOffset = (S->GridHeight + 4) * S->CellSizeCm;
	Pos.X = FMath::Clamp(Pos.X, -6000.f, LaneOffset + 3000.f);
	Pos.Y = FMath::Clamp(Pos.Y, -3500.f, 3500.f);
	if (!Pos.Equals(Cam->GetActorLocation())) Cam->SetActorLocation(Pos);
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

	if (!Hud && S->IsRunning() && S->GetTowerTypes().Num() > 0)
	{
		Hud = CreateWidget<UTdfHudWidget>(this, UTdfHudWidget::StaticClass());
		Hud->Bind(S);
		Hud->AddToViewport(0);
		// jogo e interface juntos: o cursor não some nem fica preso ao clicar, e o teclado continua no jogo
		FInputModeGameAndUI Mode;
		Mode.SetHideCursorDuringCapture(false);
		Mode.SetLockMouseToViewportBehavior(EMouseLockMode::DoNotLock);
		SetInputMode(Mode);
		if (FParse::Param(FCommandLine::Get(), TEXT("TdfConfiguracoes"))) Hud->ToggleSettings(); // conferir o painel no print
	}

	// célula sob o mouse: raio da câmera contra o plano do chão (Z = 0); sobre o HUD, nenhuma (o clique é da interface)
	HoverCell = FIntPoint(-1, -1);
	FVector Origin, Dir;
	if (!(Hud && Hud->IsPointerOverUi()) && DeprojectMousePositionToWorld(Origin, Dir) && FMath::Abs(Dir.Z) > KINDA_SMALL_NUMBER)
	{
		const float T = -Origin.Z / Dir.Z;
		if (T > 0.f) HoverCell = S->WorldToCell(Origin + Dir * T);
	}

	MoveCamera(DeltaTime);

	if (Hud && WasInputKeyJustPressed(EKeys::Escape)) Hud->ToggleSettings(); // configurações
	// R: partida nova; F9: salva o replay (como no Unity)
	if (WasInputKeyJustPressed(EKeys::R)) S->RestartMatch();
	if (WasInputKeyJustPressed(EKeys::F9)) { FString Path; S->SaveReplayNow(Path); }
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
