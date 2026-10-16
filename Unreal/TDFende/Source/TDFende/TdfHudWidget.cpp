#include "TdfHudWidget.h"
#include "TdfSimActor.h"
#include "TdfBuildStamp.h"
#include "GameFramework/GameUserSettings.h"
#include "Framework/Application/SlateApplication.h"
#include "Engine/Engine.h"
#include "Engine/GameViewportClient.h"
#include "Widgets/SWindow.h"
#include "Blueprint/WidgetTree.h"
#include "Components/Border.h"
#include "Components/Button.h"
#include "Components/CanvasPanel.h"
#include "Components/CanvasPanelSlot.h"
#include "Components/HorizontalBox.h"
#include "Components/HorizontalBoxSlot.h"
#include "Components/Image.h"
#include "Components/SizeBox.h"
#include "Components/Spacer.h"
#include "Components/TextBlock.h"
#include "Components/VerticalBox.h"
#include "Components/VerticalBoxSlot.h"
#include "Components/SceneCaptureComponent2D.h"
#include "Components/SkeletalMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/DirectionalLightComponent.h"
#include "Components/PointLightComponent.h"
#include "Engine/SkeletalMesh.h"
#include "Engine/StaticMesh.h"
#include "Engine/Texture2D.h"
#include "Engine/TextureRenderTarget2D.h"
#include "Animation/AnimSequence.h"
#include "Styling/CoreStyle.h"
#include "Styling/SlateTypes.h"
#include "EngineUtils.h"
#include "UObject/UObjectIterator.h"
#include "ContentStreaming.h"
#if WITH_EDITOR
#include "AssetCompilingManager.h"
#include "ShaderCompiler.h"
#endif
#include "ImageUtils.h"
#include "Misc/CommandLine.h"
#include "Misc/FileHelper.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include <initializer_list>

DEFINE_LOG_CATEGORY_STATIC(LogTdfHud, Log, All);

namespace
{
	const FLinearColor Ink(0.92f, 0.93f, 0.95f);
	const FLinearColor Muted(0.58f, 0.61f, 0.66f);
	const FLinearColor GoldC(1.f, 0.80f, 0.36f);
	const FLinearColor Blue(0.38f, 0.64f, 1.f);
	const FLinearColor RedC(1.f, 0.42f, 0.36f);
	const FLinearColor GreenC(0.55f, 0.92f, 0.55f);
	const FLinearColor Broke(0.86f, 0.38f, 0.33f);
	const FLinearColor Panel(0.015f, 0.018f, 0.022f, 0.80f);

	FSlateBrush Rounded(const FLinearColor& Fill, float Radius, const FLinearColor& Outline = FLinearColor::Transparent, float Width = 0.f)
	{
		FSlateBrush B;
		B.DrawAs = ESlateBrushDrawType::RoundedBox;
		B.TintColor = FSlateColor(Fill);
		B.OutlineSettings = FSlateBrushOutlineSettings(FVector4(Radius, Radius, Radius, Radius), FSlateColor(Outline), Width);
		return B;
	}

	// curva de filme (ACES, ajuste de Narkowicz) e sRGB: a miniatura sai da cena em HDR linear
	float Filmic(float X) { return FMath::Clamp((X * (2.51f * X + 0.03f)) / (X * (2.43f * X + 0.59f) + 0.14f), 0.f, 1.f); }
	uint8 ToSrgb8(float L)
	{
		const float S = L <= 0.0031308f ? L * 12.92f : 1.055f * FMath::Pow(L, 1.f / 2.4f) - 0.055f;
		return (uint8)FMath::Clamp(FMath::RoundToInt(S * 255.f), 0, 255);
	}

	constexpr int32 FilmPx = 384;   // a captura; a miniatura sai com 1/3 (média 3x3: antisserrilhado de graça)
	constexpr int32 ThumbPx = FilmPx / 3;
	constexpr float StudioZ = 150000.f; // 1,5 km acima do mapa: nada do jogo aparece (e a captura só mostra o modelo)
}

UTdfCardButton::UTdfCardButton()
{
	InitIsFocusable(false);
}

void UTdfCardButton::Setup(UTdfHudWidget* InOwner, int32 InIndex, int32 InKind)
{
	Owner = InOwner;
	Index = InIndex;
	Kind = InKind;
	OnClicked.AddDynamic(this, &UTdfCardButton::HandleClicked);
}

void UTdfCardButton::HandleClicked()
{
	if (UTdfHudWidget* H = Owner.Get()) H->OnButton(Kind, Index);
}

void UTdfHudWidget::Bind(ATdfSimActor* InSim)
{
	Sim = InSim;
}

bool UTdfHudWidget::ClickTowerCard(int32 Index)
{
	ATdfSimActor* S = Sim.Get();
	if (!S || !S->GetTowerTypes().IsValidIndex(Index)) return false;
	S->SelectTower(Index);
	return S->GetSelectedTower() == Index;
}

bool UTdfHudWidget::ClickSendCard(int32 Index)
{
	ATdfSimActor* S = Sim.Get();
	return S && S->SendUnit(Index);
}

namespace
{
	const float ScalePercents[3] = { 70.f, 85.f, 100.f };
	void Highlight(const TArray<TObjectPtr<UBorder>>& Frames, int32 Selected)
	{
		for (int32 i = 0; i < Frames.Num(); ++i)
			if (Frames[i]) Frames[i]->SetBrush(i == Selected ? Rounded(FLinearColor(GoldC.R, GoldC.G, GoldC.B, 0.95f), 8.f) : Rounded(FLinearColor::Transparent, 8.f));
	}
}

void UTdfHudWidget::OnButton(int32 Kind, int32 Index)
{
	switch (Kind)
	{
	case 0: ClickTowerCard(Index); break;
	case 1: ClickSendCard(Index); break;
	case 2: SetQuality(Index); break;
	case 3: if (Index >= 0 && Index < 3) SetScreenPercent(ScalePercents[Index]); break;
	case 4: SetFullscreen(Index == 1); break;
	}
}

void UTdfHudWidget::ToggleSettings()
{
	if (!Settings) return;
	Settings->SetVisibility(IsSettingsOpen() ? ESlateVisibility::Hidden : ESlateVisibility::Visible);
}

bool UTdfHudWidget::IsSettingsOpen() const
{
	return Settings && Settings->GetVisibility() == ESlateVisibility::Visible;
}

void UTdfHudWidget::SetQuality(int32 Level)
{
	UGameUserSettings* Gus = UGameUserSettings::GetGameUserSettings();
	if (!Gus) return;
	Quality = FMath::Clamp(Level, 0, 3);
	Gus->SetOverallScalabilityLevel(Quality); // os grupos sg.* (sombra, Lumen, efeitos, folhagem, pós) de uma vez
	Gus->ApplySettings(false);
	Gus->SaveSettings();
	Highlight(QualityFrames, Quality);
}

int32 UTdfHudWidget::SelectedQuality() const { return Quality; }

void UTdfHudWidget::SetScreenPercent(float Percent)
{
	UGameUserSettings* Gus = UGameUserSettings::GetGameUserSettings();
	if (!Gus) return;
	Gus->SetResolutionScaleValueEx(Percent); // o TSR reconstrói a imagem na resolução da tela
	Gus->ApplySettings(false);
	Gus->SaveSettings();
	int32 Best = 0;
	for (int32 i = 1; i < 3; ++i) if (FMath::Abs(ScalePercents[i] - Percent) < FMath::Abs(ScalePercents[Best] - Percent)) Best = i;
	Highlight(ScaleFrames, Best);
}

void UTdfHudWidget::SetFullscreen(bool bFull)
{
	UGameUserSettings* Gus = UGameUserSettings::GetGameUserSettings();
	if (!Gus) return;
	Gus->SetFullscreenMode(bFull ? EWindowMode::WindowedFullscreen : EWindowMode::Windowed);
	Gus->ApplySettings(false);
	Gus->SaveSettings();
	Highlight(WindowFrames, bFull ? 1 : 0);
}

UBorder* UTdfHudWidget::MakeOption(const FString& Text, int32 Kind, int32 Index, TArray<TObjectPtr<UBorder>>& Frames)
{
	UTdfCardButton* Btn = WidgetTree->ConstructWidget<UTdfCardButton>();
	Btn->Setup(this, Index, Kind);
	FButtonStyle St;
	St.SetNormal(Rounded(FLinearColor(0.05f, 0.058f, 0.068f, 0.94f), 6.f, FLinearColor(1.f, 1.f, 1.f, 0.08f), 1.f));
	St.SetHovered(Rounded(FLinearColor(0.11f, 0.12f, 0.135f, 0.97f), 6.f, FLinearColor(1.f, 1.f, 1.f, 0.25f), 1.f));
	St.SetPressed(Rounded(FLinearColor(0.16f, 0.15f, 0.12f, 0.98f), 6.f));
	St.SetNormalPadding(FMargin(12.f, 6.f));
	St.SetPressedPadding(FMargin(12.f, 7.f, 12.f, 5.f));
	Btn->SetStyle(St);
	Btn->SetContent(MakeText(Text, 11, Ink, true));
	UBorder* Frame = WidgetTree->ConstructWidget<UBorder>();
	Frame->SetBrush(Rounded(FLinearColor::Transparent, 8.f));
	Frame->SetPadding(FMargin(2.f));
	Frame->SetContent(Btn);
	Frames.Add(Frame);
	return Frame;
}

void UTdfHudWidget::BuildSettings(UCanvasPanel* Root)
{
	UVerticalBox* V = WidgetTree->ConstructWidget<UVerticalBox>();
	if (UVerticalBoxSlot* Sl = V->AddChildToVerticalBox(MakeText(TEXT("CONFIGURAÇÕES"), 14, Ink, true, 220))) Sl->SetPadding(FMargin(0.f, 0.f, 0.f, 10.f));
	auto Row = [this, V](const TCHAR* Label, std::initializer_list<const TCHAR*> Options, int32 Kind, TArray<TObjectPtr<UBorder>>& Frames)
	{
		if (UVerticalBoxSlot* Sl = V->AddChildToVerticalBox(MakeText(Label, 8, Muted, true, 180))) Sl->SetPadding(FMargin(2.f, 6.f, 0.f, 3.f));
		UHorizontalBox* H = WidgetTree->ConstructWidget<UHorizontalBox>();
		int32 i = 0;
		for (const TCHAR* O : Options)
			if (UHorizontalBoxSlot* Sl = H->AddChildToHorizontalBox(MakeOption(O, Kind, i++, Frames))) Sl->SetPadding(FMargin(0.f, 0.f, 4.f, 0.f));
		V->AddChildToVerticalBox(H);
	};
	Row(TEXT("QUALIDADE GRÁFICA"), {TEXT("Baixa"), TEXT("Média"), TEXT("Alta"), TEXT("Épica")}, 2, QualityFrames);
	Row(TEXT("RESOLUÇÃO DE RENDER (o TSR completa)"), {TEXT("70%"), TEXT("85%"), TEXT("100%")}, 3, ScaleFrames);
	Row(TEXT("TELA"), {TEXT("Janela"), TEXT("Tela cheia")}, 4, WindowFrames);
	if (UVerticalBoxSlot* Sl = V->AddChildToVerticalBox(MakeText(TEXT("Esc fecha · fica salvo para a próxima vez"), 9, Muted, false, 40)))
		Sl->SetPadding(FMargin(2.f, 12.f, 0.f, 0.f));

	Settings = MakePlate(V, Ink);
	Settings->SetPadding(FMargin(22.f, 16.f));
	Settings->SetVisibility(ESlateVisibility::Hidden);
	UCanvasPanelSlot* Sl = Root->AddChildToCanvas(Settings);
	Sl->SetAnchors(FAnchors(0.5f, 0.5f));
	Sl->SetAlignment(FVector2D(0.5f, 0.5f));
	Sl->SetAutoSize(true);

	// o que já está salvo aparece marcado
	if (UGameUserSettings* Gus = UGameUserSettings::GetGameUserSettings())
	{
		Quality = Gus->GetOverallScalabilityLevel();
		Highlight(QualityFrames, Quality);
		float Norm = 1.f, Value = 100.f, Min = 0.f, Max = 100.f;
		Gus->GetResolutionScaleInformationEx(Norm, Value, Min, Max);
		int32 Best = 0;
		for (int32 i = 1; i < 3; ++i) if (FMath::Abs(ScalePercents[i] - Value) < FMath::Abs(ScalePercents[Best] - Value)) Best = i;
		Highlight(ScaleFrames, Best);
		// o modo real da janela (o -windowed da linha de comando vence o que está salvo)
		const TSharedPtr<SWindow> Win = GEngine && GEngine->GameViewport ? GEngine->GameViewport->GetWindow() : nullptr;
		const EWindowMode::Type Mode = Win.IsValid() ? Win->GetWindowMode() : Gus->GetFullscreenMode();
		Highlight(WindowFrames, Mode == EWindowMode::Windowed ? 0 : 1);
	}
}

FVector2D UTdfHudWidget::ShopEdgePoint(float Corner) const
{
	if (!Shop) return FVector2D::ZeroVector;
	return FVector2D(Shop->GetCachedGeometry().GetAbsolutePositionAtCoordinates(FVector2D(Corner, Corner)));
}

FVector2D UTdfHudWidget::CardCenter(int32 Kind, int32 Index) const
{
	const UBorder* Frame = nullptr;
	if (Kind == 0 && TowerCards.IsValidIndex(Index)) Frame = TowerCards[Index].Frame;
	else if (Kind == 1 && SendCards.IsValidIndex(Index)) Frame = SendCards[Index].Frame;
	else if (Kind == 2 && QualityFrames.IsValidIndex(Index)) Frame = QualityFrames[Index];
	if (!Frame) return FVector2D::ZeroVector;
	const FGeometry& G = Frame->GetCachedGeometry();
	return FVector2D(G.GetAbsolutePositionAtCoordinates(FVector2D(0.5, 0.5)));
}

FString UTdfHudWidget::StampText() const { return Stamp ? Stamp->GetText().ToString() : FString(); }

bool UTdfHudWidget::IsPointerOverUi() const
{
	// pela posição do cursor contra a geometria dos painéis, e não por IsHovered: ao apertar o botão o viewport captura o mouse e
	// o Slate deixa de achar que a loja está sob ele (o clique na borda da placa construía uma torre por baixo)
	const FVector2D MousePos(FSlateApplication::Get().GetLastCursorPos()); // a última posição que o Slate recebeu (a do mouse real, ou a injetada no teste)
	auto Under = [&MousePos](const UWidget* W) { return W && W->GetVisibility() != ESlateVisibility::Hidden && W->GetCachedGeometry().IsUnderLocation(FVector2f(MousePos)); };
	return Under(TopLeft) || Under(TopRight) || Under(Shop) || IsSettingsOpen();
}

// ------------------------------------------------------------------ montagem

TSharedRef<SWidget> UTdfHudWidget::RebuildWidget()
{
	if (!WidgetTree) WidgetTree = NewObject<UWidgetTree>(this, TEXT("WidgetTree"), RF_Transient);
	if (!WidgetTree->RootWidget) BuildTree();
	return Super::RebuildWidget();
}

UTextBlock* UTdfHudWidget::MakeText(const FString& Text, int32 Size, const FLinearColor& Color, bool bBold, int32 LetterSpacing)
{
	UTextBlock* T = WidgetTree->ConstructWidget<UTextBlock>();
	FSlateFontInfo F = FCoreStyle::GetDefaultFontStyle(bBold ? "Bold" : "Regular", Size);
	F.LetterSpacing = LetterSpacing;
	T->SetFont(F);
	T->SetText(FText::FromString(Text));
	T->SetColorAndOpacity(FSlateColor(Color));
	T->SetShadowOffset(FVector2D(1.f, 1.f));
	T->SetShadowColorAndOpacity(FLinearColor(0.f, 0.f, 0.f, 0.6f));
	return T;
}

UVerticalBox* UTdfHudWidget::MakeStat(const FString& Label, FStat& Out, const FLinearColor& Color)
{
	UVerticalBox* V = WidgetTree->ConstructWidget<UVerticalBox>();
	V->AddChildToVerticalBox(MakeText(Label, 8, Muted, true, 180));
	Out.Value = MakeText(TEXT("-"), 19, Color, true);
	V->AddChildToVerticalBox(Out.Value);
	return V;
}

UBorder* UTdfHudWidget::MakePlate(UWidget* Content, const FLinearColor& Accent)
{
	UBorder* B = WidgetTree->ConstructWidget<UBorder>();
	B->SetBrush(Rounded(Panel, 10.f, FLinearColor(Accent.R, Accent.G, Accent.B, 0.35f), 1.f));
	B->SetPadding(FMargin(16.f, 10.f, 18.f, 10.f));
	B->SetContent(Content);
	return B;
}

UTdfHudWidget::FCard UTdfHudWidget::MakeCard(int32 Index, bool bSend, float ThumbSize, float WidthPx, UWidget*& OutWidget)
{
	const ATdfSimActor* S = Sim.Get();
	const FTdfCatalogItem& Item = bSend ? S->GetSendTypes()[Index] : S->GetTowerTypes()[Index];
	FCard C;
	C.Price = Item.Cost;

	UTdfCardButton* Btn = WidgetTree->ConstructWidget<UTdfCardButton>();
	Btn->Setup(this, Index, bSend ? 1 : 0);
	FButtonStyle St;
	St.SetNormal(Rounded(FLinearColor(0.05f, 0.058f, 0.068f, 0.94f), 8.f, FLinearColor(1.f, 1.f, 1.f, 0.07f), 1.f));
	St.SetHovered(Rounded(FLinearColor(0.11f, 0.12f, 0.135f, 0.97f), 8.f, FLinearColor(1.f, 1.f, 1.f, 0.22f), 1.f));
	St.SetPressed(Rounded(FLinearColor(0.16f, 0.15f, 0.12f, 0.98f), 8.f, FLinearColor(GoldC.R, GoldC.G, GoldC.B, 0.5f), 1.f));
	St.SetDisabled(Rounded(FLinearColor(0.05f, 0.05f, 0.05f, 0.9f), 8.f));
	St.SetNormalPadding(FMargin(5.f, 5.f, 5.f, 4.f));
	St.SetPressedPadding(FMargin(5.f, 6.f, 5.f, 3.f));
	Btn->SetStyle(St);
	if (bSend)
		Btn->SetToolTipText(FText::FromString(FString::Printf(TEXT("%s — tecla %d\nVida %.0f · velocidade %.1f · %.0f por envio\nRenda +%.0f · recompensa %.0f"),
			*Item.Name, Index + 1, Item.F[0], Item.F[1], Item.F[2], Item.F[3], Item.F[4])));
	else
		Btn->SetToolTipText(FText::FromString(FString::Printf(TEXT("%s — Q / E troca\nAlcance %.1f · recarga %.2f s · dano %.0f%s"),
			*Item.Name, Item.F[0], Item.F[1], Item.F[2], Item.F[3] > 0.f ? *FString::Printf(TEXT(" · área %.1f"), Item.F[3]) : TEXT(""))));

	UVerticalBox* V = WidgetTree->ConstructWidget<UVerticalBox>();
	USizeBox* ThumbBox = WidgetTree->ConstructWidget<USizeBox>();
	ThumbBox->SetWidthOverride(ThumbSize);
	ThumbBox->SetHeightOverride(ThumbSize);
	C.Thumb = WidgetTree->ConstructWidget<UImage>();
	C.Thumb->SetBrush(Rounded(FLinearColor(1.f, 1.f, 1.f, 0.04f), 6.f)); // lugar da miniatura até a foto ficar pronta
	ThumbBox->SetContent(C.Thumb);
	if (UVerticalBoxSlot* Sl = V->AddChildToVerticalBox(ThumbBox)) Sl->SetHorizontalAlignment(HAlign_Center);

	C.Name = MakeText(Item.Name, bSend ? 9 : 10, Ink, true);
	if (UVerticalBoxSlot* Sl = V->AddChildToVerticalBox(C.Name)) { Sl->SetHorizontalAlignment(HAlign_Center); Sl->SetPadding(FMargin(0.f, 2.f, 0.f, 0.f)); }

	UHorizontalBox* Row = WidgetTree->ConstructWidget<UHorizontalBox>();
	C.Cost = MakeText(FString::Printf(TEXT("%d"), Item.Cost), bSend ? 10 : 11, GoldC, true);
	Row->AddChildToHorizontalBox(C.Cost);
	USpacer* Gap = WidgetTree->ConstructWidget<USpacer>();
	if (UHorizontalBoxSlot* Sl = Row->AddChildToHorizontalBox(Gap)) Sl->SetSize(FSlateChildSize(ESlateSizeRule::Fill));
	if (bSend)
	{
		UTextBlock* Key = MakeText(FString::Printf(TEXT("%d"), Index + 1), 9, Muted, true);
		if (UHorizontalBoxSlot* Sl = Row->AddChildToHorizontalBox(Key)) Sl->SetVerticalAlignment(VAlign_Center);
	}
	V->AddChildToVerticalBox(Row);

	USizeBox* Width = WidgetTree->ConstructWidget<USizeBox>();
	Width->SetWidthOverride(WidthPx);
	Width->SetContent(V);
	Btn->SetContent(Width);

	C.Frame = WidgetTree->ConstructWidget<UBorder>();
	C.Frame->SetBrush(Rounded(FLinearColor::Transparent, 10.f));
	C.Frame->SetPadding(FMargin(2.f));
	C.Frame->SetContent(Btn);
	OutWidget = C.Frame;
	return C;
}

void UTdfHudWidget::BuildTree()
{
	ATdfSimActor* S = Sim.Get();
	UCanvasPanel* Root = WidgetTree->ConstructWidget<UCanvasPanel>();
	WidgetTree->RootWidget = Root;
	auto Place = [Root](UWidget* W, const FAnchors& A, const FVector2D& Align, const FVector2D& Pos)
	{
		UCanvasPanelSlot* Sl = Root->AddChildToCanvas(W);
		Sl->SetAnchors(A);
		Sl->SetAlignment(Align);
		Sl->SetPosition(Pos);
		Sl->SetAutoSize(true);
	};
	struct FDef { const TCHAR* Label; FStat* Stat; FLinearColor Color; };
	auto StatRow = [this](const TCHAR* Title, const FLinearColor& Accent, std::initializer_list<FDef> Stats)
	{
		UVerticalBox* V = WidgetTree->ConstructWidget<UVerticalBox>();
		V->AddChildToVerticalBox(MakeText(Title, 10, Accent, true, 220));
		UHorizontalBox* H = WidgetTree->ConstructWidget<UHorizontalBox>();
		for (const FDef& D : Stats)
			if (UHorizontalBoxSlot* Sl = H->AddChildToHorizontalBox(MakeStat(D.Label, *D.Stat, D.Color))) Sl->SetPadding(FMargin(0.f, 2.f, 22.f, 0.f));
		V->AddChildToVerticalBox(H);
		return V;
	};

	// placar
	TopLeft = MakePlate(StatRow(TEXT("VOCÊ"), Blue, {
		{TEXT("VIDAS"), &MyLives, Ink}, {TEXT("OURO"), &MyGold, GoldC}, {TEXT("RENDA"), &MyIncome, GreenC}, {TEXT("TORRES"), &MyTowers, Ink}}), Blue);
	Place(TopLeft, FAnchors(0.f, 0.f), FVector2D(0.f, 0.f), FVector2D(16.f, 14.f));
	TopRight = MakePlate(StatRow(TEXT("INIMIGO (IA)"), RedC, {
		{TEXT("VIDAS"), &FoeLives, Ink}, {TEXT("RENDA"), &FoeIncome, GreenC}, {TEXT("TORRES"), &FoeTowers, Ink}, {TEXT("NA SUA LANE"), &FoeEnemies, RedC}}), RedC);
	Place(TopRight, FAnchors(1.f, 0.f), FVector2D(1.f, 0.f), FVector2D(-16.f, 14.f));

	UVerticalBox* Mid = WidgetTree->ConstructWidget<UVerticalBox>();
	Clock = MakeText(TEXT("0:00"), 22, Ink, true, 60);
	if (UVerticalBoxSlot* Sl = Mid->AddChildToVerticalBox(Clock)) Sl->SetHorizontalAlignment(HAlign_Center);
	Sudden = MakeText(TEXT("MORTE SÚBITA"), 10, RedC, true, 200);
	Sudden->SetVisibility(ESlateVisibility::Collapsed);
	if (UVerticalBoxSlot* Sl = Mid->AddChildToVerticalBox(Sudden)) Sl->SetHorizontalAlignment(HAlign_Center);
	if (UVerticalBoxSlot* Sl = Mid->AddChildToVerticalBox(MakeText(TEXT("R partida nova · F9 salva o replay · Esc configurações"), 8, Muted, false, 40)))
		Sl->SetHorizontalAlignment(HAlign_Center);
	UBorder* ClockPlate = WidgetTree->ConstructWidget<UBorder>();
	ClockPlate->SetBrush(Rounded(Panel, 10.f));
	ClockPlate->SetPadding(FMargin(18.f, 6.f));
	ClockPlate->SetContent(Mid);
	ClockPlate->SetVisibility(ESlateVisibility::HitTestInvisible);
	Place(ClockPlate, FAnchors(0.5f, 0.f), FVector2D(0.5f, 0.f), FVector2D(0.f, 14.f));

	ToastText = MakeText(TEXT(""), 20, FLinearColor(1.f, 0.72f, 0.32f), true, 40);
	ToastText->SetShadowOffset(FVector2D(2.f, 2.f));
	ToastText->SetShadowColorAndOpacity(FLinearColor(0.f, 0.f, 0.f, 0.85f));
	ToastText->SetVisibility(ESlateVisibility::Hidden); // Hidden: segue medido (Collapsed nascia torto no 1º quadro)
	Place(ToastText, FAnchors(0.5f, 0.f), FVector2D(0.5f, 0.f), FVector2D(0.f, 92.f));

	// loja
	UHorizontalBox* Sections = WidgetTree->ConstructWidget<UHorizontalBox>();
	auto Section = [this, Sections](const FString& Title, const FString& Hint, bool bSend, float ThumbSize, float WidthPx, TArray<FCard>& Out, int32 Count)
	{
		UVerticalBox* V = WidgetTree->ConstructWidget<UVerticalBox>();
		UHorizontalBox* Head = WidgetTree->ConstructWidget<UHorizontalBox>();
		Head->AddChildToHorizontalBox(MakeText(Title, 10, Ink, true, 220));
		if (UHorizontalBoxSlot* Sl = Head->AddChildToHorizontalBox(MakeText(Hint, 9, Muted, false, 60))) { Sl->SetPadding(FMargin(10.f, 1.f, 0.f, 0.f)); Sl->SetVerticalAlignment(VAlign_Center); }
		if (UVerticalBoxSlot* Sl = V->AddChildToVerticalBox(Head)) Sl->SetPadding(FMargin(4.f, 0.f, 0.f, 6.f));
		UHorizontalBox* Row = WidgetTree->ConstructWidget<UHorizontalBox>();
		for (int32 i = 0; i < Count; ++i)
		{
			UWidget* W = nullptr;
			Out.Add(MakeCard(i, bSend, ThumbSize, WidthPx, W));
			if (UHorizontalBoxSlot* Sl = Row->AddChildToHorizontalBox(W)) Sl->SetPadding(FMargin(0.f, 0.f, 3.f, 0.f));
		}
		V->AddChildToVerticalBox(Row);
		return V;
	};
	if (S)
	{
		Sections->AddChildToHorizontalBox(Section(TEXT("TORRES"), TEXT("clique ou Q / E · botão esquerdo no campo constrói, direito vende"), false, 84.f, 92.f, TowerCards, S->GetTowerTypes().Num()));
		USpacer* Gap = WidgetTree->ConstructWidget<USpacer>();
		Gap->SetSize(FVector2D(18.f, 1.f));
		Sections->AddChildToHorizontalBox(Gap);
		Sections->AddChildToHorizontalBox(Section(TEXT("ENVIAR BICHOS"), TEXT("clique ou 1 a 9"), true, 64.f, 72.f, SendCards, S->GetSendTypes().Num()));
	}
	Shop = WidgetTree->ConstructWidget<UBorder>();
	Shop->SetBrush(Rounded(Panel, 12.f, FLinearColor(1.f, 1.f, 1.f, 0.10f), 1.f));
	Shop->SetPadding(FMargin(12.f, 10.f, 9.f, 10.f));
	Shop->SetContent(Sections);
	Place(Shop, FAnchors(0.5f, 1.f), FVector2D(0.5f, 1.f), FVector2D(0.f, -12.f));

	BuildSettings(Root);

	// selo de build no canto inferior direito (MANUAL: executável sem selo reprova a captura)
	Stamp = MakeText(TEXT("build ") + TdfBuildStamp(), 8, Muted, false, 20);
	Stamp->SetVisibility(ESlateVisibility::HitTestInvisible);
	Place(Stamp, FAnchors(1.f, 1.f), FVector2D(1.f, 1.f), FVector2D(-8.f, -2.f));

	// fim de partida: a tela escurece e o resultado aparece no meio (não bloqueia o mouse: o R é que recomeça)
	UVerticalBox* End = WidgetTree->ConstructWidget<UVerticalBox>();
	EndTitle = MakeText(TEXT("DERROTA"), 54, Ink, true, 300);
	EndTitle->SetShadowOffset(FVector2D(3.f, 3.f));
	if (UVerticalBoxSlot* Sl = End->AddChildToVerticalBox(EndTitle)) Sl->SetHorizontalAlignment(HAlign_Center);
	if (UVerticalBoxSlot* Sl = End->AddChildToVerticalBox(MakeText(TEXT("R para jogar de novo  ·  F9 salva o replay"), 15, Ink, false, 60)))
	{
		Sl->SetHorizontalAlignment(HAlign_Center);
		Sl->SetPadding(FMargin(0.f, 8.f, 0.f, 0.f));
	}
	EndPanel = WidgetTree->ConstructWidget<UBorder>();
	EndPanel->SetBrushColor(FLinearColor(0.f, 0.f, 0.f, 0.55f));
	EndPanel->SetHorizontalAlignment(HAlign_Center);
	EndPanel->SetVerticalAlignment(VAlign_Center);
	EndPanel->SetContent(End);
	// Hidden (e não Collapsed): continua medido a cada quadro; Collapsed aparecia no 1º quadro com o texto sobreposto
	EndPanel->SetVisibility(ESlateVisibility::Hidden);
	UCanvasPanelSlot* EndSlot = Root->AddChildToCanvas(EndPanel);
	EndSlot->SetAnchors(FAnchors(0.f, 0.f, 1.f, 1.f));
	EndSlot->SetOffsets(FMargin(0.f));
}

// ------------------------------------------------------------------ atualização

void UTdfHudWidget::SetStat(FStat& S, int32 V, bool bPlus)
{
	if (!S.Value || S.Last == V) return;
	S.Last = V;
	S.Value->SetText(FText::FromString(bPlus ? FString::Printf(TEXT("+%d"), V) : FString::FromInt(V)));
}

void UTdfHudWidget::UpdateCard(FCard& C, int32 Gold, bool bSelected)
{
	const int32 State = (Gold >= C.Price ? 1 : 0) | (bSelected ? 2 : 0);
	if (State == C.State) return;
	C.State = State;
	const bool bLit = State & 1;
	C.Frame->SetBrush(bSelected ? Rounded(FLinearColor(GoldC.R, GoldC.G, GoldC.B, 0.95f), 10.f) : Rounded(FLinearColor::Transparent, 10.f));
	C.Thumb->SetColorAndOpacity(bLit ? FLinearColor::White : FLinearColor(0.42f, 0.42f, 0.44f, 0.85f));
	C.Name->SetColorAndOpacity(FSlateColor(bLit ? Ink : Muted));
	C.Cost->SetColorAndOpacity(FSlateColor(bLit ? GoldC : Broke));
}

void UTdfHudWidget::Refresh()
{
	ATdfSimActor* S = Sim.Get();
	if (!S || !S->IsRunning()) return;
	const TdfLane& Me = S->GetLane(0);
	const TdfLane& Foe = S->GetLane(1);
	SetStat(MyLives, Me.Lives);
	SetStat(MyGold, Me.Gold);
	SetStat(MyIncome, Me.Income, true);
	SetStat(MyTowers, Me.TowerCount);
	SetStat(FoeLives, Foe.Lives);
	SetStat(FoeIncome, Foe.Income, true);
	SetStat(FoeTowers, Foe.TowerCount);
	SetStat(FoeEnemies, Me.EnemyCount);

	const int32 Secs = FMath::FloorToInt(Me.MatchTime);
	if (Clock && Secs != LastClock)
	{
		LastClock = Secs;
		Clock->SetText(FText::FromString(FString::Printf(TEXT("%d:%02d"), Secs / 60, Secs % 60)));
	}
	if (Sudden) Sudden->SetVisibility(Me.InSuddenDeath ? ESlateVisibility::HitTestInvisible : ESlateVisibility::Collapsed);
	const int32 Result = S->MatchResult();
	if (EndPanel && Result != LastResult)
	{
		LastResult = Result;
		EndPanel->SetVisibility(Result != 0 ? ESlateVisibility::HitTestInvisible : ESlateVisibility::Hidden);
		EndTitle->SetText(FText::FromString(Result > 0 ? TEXT("VITÓRIA") : TEXT("DERROTA")));
		EndTitle->SetColorAndOpacity(FSlateColor(Result > 0 ? GoldC : RedC));
	}

	for (int32 i = 0; i < TowerCards.Num(); ++i) UpdateCard(TowerCards[i], Me.Gold, i == S->GetSelectedTower());
	for (int32 i = 0; i < SendCards.Num(); ++i) UpdateCard(SendCards[i], Me.Gold, false);

	if (ToastText)
	{
		const float Age = S->GetToastAge();
		const bool bShow = Age < 3.f && !S->GetToast().IsEmpty();
		if (bShow && S->GetToast() != LastToast)
		{
			LastToast = S->GetToast();
			ToastText->SetText(FText::FromString(LastToast));
		}
		ToastText->SetVisibility(bShow ? ESlateVisibility::HitTestInvisible : ESlateVisibility::Hidden);
		if (bShow) ToastText->SetRenderOpacity(FMath::Clamp(3.f - Age, 0.f, 1.f));
		else LastToast.Reset();
	}
}

void UTdfHudWidget::NativeTick(const FGeometry& MyGeometry, float InDeltaTime)
{
	Super::NativeTick(MyGeometry, InDeltaTime);
	// miniaturas: monta o estúdio no primeiro quadro (a pose do bicho chega à placa de vídeo no fim dele) e fotografa
	// alguns quadros depois, com a luz do céu já capturada
	if (ThumbStep == 0) StartThumbnails();
	if (ThumbStep == ThumbShootFrame) FinishThumbnails();
	if (ThumbStep <= ThumbShootFrame) ++ThumbStep;
	Refresh();
}

// ------------------------------------------------------------------ miniaturas 3D

void UTdfHudWidget::StartThumbnails()
{
	ATdfSimActor* S = Sim.Get();
	UWorld* W = GetWorld();
	if (!S || !W) return;

	// a câmera olha na mesma direção da luz do sol (o modelo aparece iluminado de frente, um pouco de lado)
	for (TObjectIterator<UDirectionalLightComponent> It; It; ++It)
		if (It->GetWorld() == W && It->IsVisible())
		{
			StudioView = FRotator(-20.f, It->GetDirection().Rotation().Yaw + 28.f, 0.f);
			break;
		}

	FActorSpawnParameters P;
	P.ObjectFlags |= RF_Transient;
	Studio = W->SpawnActor<AActor>(AActor::StaticClass(), FTransform(FVector(0.f, 0.f, StudioZ)), P);
	if (!Studio) return;
	USceneComponent* Root = NewObject<USceneComponent>(Studio);
	Studio->SetRootComponent(Root);
	Root->RegisterComponent();

	const float FaceYaw = StudioView.Yaw + 180.f; // de frente para a câmera
	int32 Next = 0;
	auto Spot = [&Next]() { return FVector(0.f, (Next++) * 3000.f, StudioZ); };
	for (int32 i = 0; i < S->GetTowerTypes().Num(); ++i)
	{
		UStaticMesh* M = S->TowerArt(i, 1); // o estágio que a loja vende
		if (!M) continue;
		UStaticMeshComponent* C = NewObject<UStaticMeshComponent>(Studio);
		C->SetStaticMesh(M);
		C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
		C->SetupAttachment(Root);
		C->SetWorldLocationAndRotation(Spot(), FRotator(0.f, FaceYaw - 30.f, 0.f));
		C->RegisterComponent();
		C->PrestreamTextures(15.f, false); // ninguém vê o estúdio: sem isto as texturas ficam na resolução mínima
		Shots.Add({C, false, i});
	}
	for (int32 i = 0; i < S->GetSendTypes().Num(); ++i)
	{
		UAnimSequence* Anim = nullptr;
		float YawOffset = 0.f;
		USkeletalMesh* M = S->SendArt(i, Anim, YawOffset);
		if (!M) continue;
		USkeletalMeshComponent* C = NewObject<USkeletalMeshComponent>(Studio);
		C->SetSkeletalMesh(M);
		C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
		C->VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption::AlwaysTickPoseAndRefreshBones;
		C->SetupAttachment(Root);
		// a frente da malha vai para +X com o giro YawOffset; aqui ela olha para a câmera, em três quartos
		C->SetWorldLocationAndRotation(Spot(), FRotator(0.f, FaceYaw + YawOffset - 35.f, 0.f));
		C->RegisterComponent();
		if (Anim)
		{
			C->SetAnimationMode(EAnimationMode::AnimationSingleNode);
			C->SetAnimation(Anim);
			C->SetPosition(Anim->GetPlayLength() * 0.3f, false); // meio do passo
			C->SetPlayRate(0.f);
			C->Play(true);
			C->TickAnimation(0.f, false);
			C->RefreshBoneTransforms();
		}
		C->PrestreamTextures(15.f, true);
		Shots.Add({C, true, i});
	}

	Camera = NewObject<USceneCaptureComponent2D>(Studio);
	Camera->SetupAttachment(Root);
	Camera->bCaptureEveryFrame = false;
	Camera->bCaptureOnMovement = false;
	Camera->PrimitiveRenderMode = ESceneCapturePrimitiveRenderMode::PRM_UseShowOnlyList;
	Camera->CaptureSource = ESceneCaptureSource::SCS_SceneColorHDR; // cor linear e, no alfa, 1 - cobertura: dá para recortar o modelo
	Camera->FOVAngle = 26.f;
	TArray<FEngineShowFlagsSetting> Flags;
	for (const TCHAR* Name : {TEXT("Atmosphere"), TEXT("Fog"), TEXT("VolumetricFog"), TEXT("Cloud"), TEXT("MotionBlur"), TEXT("LumenGlobalIllumination")})
	{
		FEngineShowFlagsSetting F;
		F.ShowFlagName = Name;
		F.Enabled = false;
		Flags.Add(F);
	}
	Camera->SetShowFlagSettings(Flags);
	Camera->PostProcessSettings.bOverride_DynamicGlobalIlluminationMethod = true;
	Camera->PostProcessSettings.DynamicGlobalIlluminationMethod = EDynamicGlobalIlluminationMethod::None;
	Camera->PostProcessSettings.bOverride_ReflectionMethod = true;
	Camera->PostProcessSettings.ReflectionMethod = EReflectionMethod::None;
	Camera->RegisterComponent();
	// luz de preenchimento junto da câmera (só alcança o estúdio): sem ela o lado na sombra fica preto e a foto, estourada
	FillLight = NewObject<UPointLightComponent>(Studio);
	FillLight->SetupAttachment(Root);
	FillLight->SetIntensityUnits(ELightUnits::Candelas);
	FillLight->SetCastShadows(false);
	FillLight->RegisterComponent();
	Film = NewObject<UTextureRenderTarget2D>(this);
	Film->RenderTargetFormat = RTF_RGBA16f;
	Film->ClearColor = FLinearColor(0.f, 0.f, 0.f, 1.f);
	Film->InitAutoFormat(FilmPx, FilmPx);
	Film->UpdateResourceImmediate(true);
	Camera->TextureTarget = Film;
}

bool UTdfHudWidget::Shoot(UPrimitiveComponent* Comp, const FVector& Eye, const FRotator& View, float Fov, TArray<FFloat16Color>& Px)
{
	Camera->ShowOnlyComponents.Reset();
	Camera->ShowOnlyComponents.Add(Comp);
	Camera->FOVAngle = Fov;
	Camera->SetWorldLocationAndRotation(Eye, View);
	if (FillLight)
	{
		// ~4 lux no modelo (o sol do mapa tem 9): ilumina o lado da sombra sem achatar
		const float DistM = FVector::Dist(Eye, Comp->Bounds.Origin) / 100.f;
		FillLight->SetWorldLocation(Eye + FVector(0.f, 0.f, DistM * 30.f));
		FillLight->SetIntensity(4.f * DistM * DistM);
		FillLight->SetAttenuationRadius(DistM * 100.f * 3.f);
	}
	Camera->CaptureScene();
	FTextureRenderTargetResource* Res = Film->GameThread_GetRenderTargetResource();
	return Res && Res->ReadFloat16Pixels(Px) && Px.Num() == FilmPx * FilmPx;
}

bool UTdfHudWidget::Photograph(UPrimitiveComponent* Comp, const FBox& Box, const FRotator& View, const FString& Name, TArray<FFloat16Color>& Px, float& Exposure)
{
	// 1ª foto: distância que enquadra a caixa inteira (cada canto cabe no cone da câmera)
	const FVector F = View.Vector(), R = FRotationMatrix(View).GetUnitAxis(EAxis::Y), U = FRotationMatrix(View).GetUnitAxis(EAxis::Z);
	const FVector Center = Box.GetCenter();
	const float Fov0 = 26.f;
	const float Tan0 = FMath::Tan(FMath::DegreesToRadians(Fov0 * 0.5f));
	float Dist = 10.f;
	for (int32 i = 0; i < 8; ++i)
	{
		const FVector Corner((i & 1) ? Box.Max.X : Box.Min.X, (i & 2) ? Box.Max.Y : Box.Min.Y, (i & 4) ? Box.Max.Z : Box.Min.Z);
		const FVector D = Corner - Center;
		const float Lateral = FMath::Max(FMath::Abs(FVector::DotProduct(D, R)), FMath::Abs(FVector::DotProduct(D, U)));
		Dist = FMath::Max(Dist, Lateral / Tan0 - FVector::DotProduct(D, F));
	}
	const FVector Eye = Center - F * Dist * 1.1f;
	if (!Shoot(Comp, Eye, View, Fov0, Px)) return false;

	// 2ª foto: do mesmo ponto, mirando no que de fato apareceu e fechando o ângulo até o modelo ocupar ~88% do quadro
	// (a caixa de ossos ou de malha às vezes é bem maior que o bicho; o que conta é o que a câmera viu)
	int32 X0 = FilmPx, X1 = -1, Y0 = FilmPx, Y1 = -1;
	for (int32 y = 0; y < FilmPx; ++y)
		for (int32 x = 0; x < FilmPx; ++x)
			if (1.f - Px[y * FilmPx + x].A.GetFloat() >= 0.5f) { X0 = FMath::Min(X0, x); X1 = FMath::Max(X1, x); Y0 = FMath::Min(Y0, y); Y1 = FMath::Max(Y1, y); }
	if (X1 < 0)
	{
		UE_LOG(LogTdfHud, Warning, TEXT("Miniatura de %s: o modelo não apareceu na captura"), *Name);
		return false;
	}
	const float Cx = (X0 + X1 + 1) / float(FilmPx) - 1.f, Cy = (Y0 + Y1 + 1) / float(FilmPx) - 1.f; // centro, de -1 a 1
	const float Half = FMath::Max(X1 - X0 + 1, Y1 - Y0 + 1) / float(FilmPx);                       // meia largura, idem
	const FRotator View2 = (F + R * (Cx * Tan0) - U * (Cy * Tan0)).Rotation();
	const float Fov2 = FMath::Clamp(2.f * FMath::RadiansToDegrees(FMath::Atan(Tan0 * Half / 0.88f)), 1.f, Fov0);
	if (!Shoot(Comp, Eye, View2, Fov2, Px)) return false;

	// exposição: a que leva a luminância média (log) do modelo a um cinza claro
	double LogSum = 0.0;
	int32 Covered = 0;
	for (const FFloat16Color& C : Px)
	{
		if (1.f - C.A.GetFloat() < 0.5f) continue;
		const float L = 0.2126f * C.R.GetFloat() + 0.7152f * C.G.GetFloat() + 0.0722f * C.B.GetFloat();
		LogSum += FMath::Loge(FMath::Max(L, 1e-5f));
		++Covered;
	}
	if (Covered < 50)
	{
		UE_LOG(LogTdfHud, Warning, TEXT("Miniatura de %s: o modelo quase não apareceu na captura (%d pixels)"), *Name, Covered);
		return false;
	}
	Exposure = 0.20f / FMath::Max((float)FMath::Exp(LogSum / Covered), 1e-5f);
	UE_LOG(LogTdfHud, Display, TEXT("Miniatura de %s: %d pixels de modelo (%.0f%%), exposição própria %.3g"), *Name, Covered, 100.f * Covered / (FilmPx * FilmPx), Exposure);
	return true;
}

UTexture2D* UTdfHudWidget::Develop(const TArray<FFloat16Color>& Px, float Exposure, const FString& Name)
{
	TArray<uint8> Out;
	Out.SetNumUninitialized(ThumbPx * ThumbPx * 4);
	for (int32 y = 0; y < ThumbPx; ++y)
		for (int32 x = 0; x < ThumbPx; ++x)
		{
			FLinearColor Sum(0.f, 0.f, 0.f, 0.f);
			for (int32 j = 0; j < 3; ++j)
				for (int32 i = 0; i < 3; ++i)
				{
					const FFloat16Color& C = Px[(y * 3 + j) * FilmPx + x * 3 + i];
					const float Cov = FMath::Clamp(1.f - C.A.GetFloat(), 0.f, 1.f);
					Sum += FLinearColor(C.R.GetFloat(), C.G.GetFloat(), C.B.GetFloat(), Cov);
				}
			const float Cov = Sum.A / 9.f;
			const float Inv = Cov > 1e-3f ? Exposure / (Sum.A) : 0.f; // fundo preto: a cor já vem multiplicada pela cobertura
			uint8* B = &Out[(y * ThumbPx + x) * 4];
			B[0] = ToSrgb8(Filmic(Sum.B * Inv));
			B[1] = ToSrgb8(Filmic(Sum.G * Inv));
			B[2] = ToSrgb8(Filmic(Sum.R * Inv));
			B[3] = (uint8)FMath::Clamp(FMath::RoundToInt(Cov * 255.f), 0, 255);
		}
	UTexture2D* T = UTexture2D::CreateTransient(ThumbPx, ThumbPx, PF_B8G8R8A8, NAME_None, Out);

	// -TdfMiniaturas=<pasta>: grava cada miniatura em PNG (conferir a arte sem abrir o jogo)
	FString Dir;
	if (FParse::Value(FCommandLine::Get(), TEXT("TdfMiniaturas="), Dir))
	{
		TArray64<uint8> Png;
		FImageUtils::PNGCompressImageArray(ThumbPx, ThumbPx, TArrayView64<const FColor>(reinterpret_cast<const FColor*>(Out.GetData()), ThumbPx * ThumbPx), Png);
		FFileHelper::SaveArrayToFile(Png, *FPaths::Combine(Dir, Name + TEXT(".png")));
	}
	return T;
}

void UTdfHudWidget::FinishThumbnails()
{
	ATdfSimActor* S = Sim.Get();
	if (S)
	{
		TowerTex.SetNum(S->GetTowerTypes().Num());
		SendTex.SetNum(S->GetSendTypes().Num());
	}
	if (Studio && Camera && Film && S)
	{
#if WITH_EDITOR
		// rodando pelo editor (-game), texturas e shaders compilam em segundo plano: a foto sairia com o cinza padrão.
		// No jogo empacotado tudo já vem pronto e isto nem existe.
		FAssetCompilingManager::Get().FinishAllCompilation();
		if (GShaderCompilingManager) GShaderCompilingManager->FinishAllCompilation();
#endif
		IStreamingManager::Get().StreamAllResources(3.f); // espera as texturas pedidas no PrestreamTextures (até 3 s, uma vez)
		struct FPhoto { const FShot* Shot; FString Name; TArray<FFloat16Color> Px; float Exposure; };
		TArray<FPhoto> Photos;
		for (const FShot& Shot : Shots)
		{
			FBox Box = Shot.Comp->Bounds.GetBox();
			if (USkeletalMeshComponent* Sk = Cast<USkeletalMeshComponent>(Shot.Comp))
			{
				// bicho: caixa pelos ossos na pose (a caixa guardada na malha mente, BUG-01 do Unity), com folga para a pele
				FBox Bones(ForceInit);
				for (int32 b = 0; b < Sk->GetNumBones(); ++b) Bones += Sk->GetBoneLocation(Sk->GetBoneName(b), EBoneSpaces::WorldSpace);
				if (Bones.IsValid) Box = Bones.ExpandBy(Bones.GetExtent().GetMax() * 0.12f);
			}
			FPhoto P{&Shot, Shot.bSend ? TEXT("bicho_") + S->GetSendTypes()[Shot.Index].Key : TEXT("torre_") + S->GetTowerTypes()[Shot.Index].Key};
			if (Photograph(Shot.Comp, Box, StudioView, P.Name, P.Px, P.Exposure)) Photos.Add(MoveTemp(P));
		}
		// exposição de compromisso: média geométrica entre a do modelo e a mediana de todos. Só a própria deixaria o urso
		// tão claro quanto o rato; só a comum deixaria os bichos de pelo escuro ilegíveis
		TArray<float> All;
		for (const FPhoto& P : Photos) All.Add(P.Exposure);
		All.Sort();
		const float Median = All.Num() ? All[All.Num() / 2] : 1.f;
		for (const FPhoto& P : Photos)
		{
			const FShot& Shot = *P.Shot;
			UTexture2D* T = Develop(P.Px, FMath::Sqrt(P.Exposure * Median), P.Name);
			(Shot.bSend ? SendTex : TowerTex)[Shot.Index] = T;
			FCard& C = (Shot.bSend ? SendCards : TowerCards)[Shot.Index];
			FSlateBrush B;
			B.SetResourceObject(T);
			B.ImageSize = FVector2D(ThumbPx, ThumbPx);
			C.Thumb->SetBrush(B);
		}
		Studio->Destroy();
	}
	Studio = nullptr;
	Camera = nullptr;
	FillLight = nullptr;
	Shots.Reset();
}
