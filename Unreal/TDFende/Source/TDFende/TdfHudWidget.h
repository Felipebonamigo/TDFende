#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include "Components/Button.h"
#include "Math/Float16Color.h"
#include "TdfHudWidget.generated.h"

class ATdfSimActor;
class UTexture2D;
class UBorder;
class UButton;
class UImage;
class UTextBlock;
class UVerticalBox;
class UPrimitiveComponent;
class USceneCaptureComponent2D;
class UTextureRenderTarget2D;

/**
 * Botão de cartão da loja: não pega o foco do teclado (Q/E, 1-9 e WASD continuam no jogo) e sabe qual cartão é
 * (o OnClicked do UButton não passa argumento).
 */
UCLASS()
class UTdfCardButton : public UButton
{
	GENERATED_BODY()

public:
	UTdfCardButton();
	void Setup(class UTdfHudWidget* InOwner, int32 InIndex, bool bInSend);

private:
	TWeakObjectPtr<class UTdfHudWidget> Owner;
	int32 Index = 0;
	bool bSend = false;

	UFUNCTION() void HandleClicked();
};

/**
 * U2: HUD em UMG, montado em C++ (sem asset de Blueprint: a receita continua sem abrir o editor).
 *   placar: você (vidas, ouro, renda, torres) à esquerda, a IA à direita, o relógio no meio;
 *   loja embaixo: um cartão por torre (clique seleciona; Q/E também) e um por envio (clique envia; teclas 1-9 também),
 *   cada um com a miniatura 3D do próprio modelo, nome e preço; apagado quando falta ouro;
 *   aviso curto (comando recusado, fim da partida) no alto.
 * As miniaturas são fotografadas uma vez no começo: um "estúdio" longe do mapa, uma captura de cena por modelo (só ele aparece),
 * lida de volta, recortada pelo alfa de cobertura e exposta pela luminância média do próprio modelo (todas ficam com o mesmo brilho).
 * O contorno da célula sob o mouse continua no ATdfHUD (Canvas), porque acompanha o mundo.
 */
UCLASS()
class UTdfHudWidget : public UUserWidget
{
	GENERATED_BODY()

public:
	/** Liga à Sim (catálogo e placar). Chamar antes do AddToViewport. */
	void Bind(ATdfSimActor* InSim);

	int32 NumTowerCards() const { return TowerCards.Num(); }
	int32 NumSendCards() const { return SendCards.Num(); }
	/** O mesmo caminho do clique do jogador: seleciona a torre / envia o bicho. */
	bool ClickTowerCard(int32 Index);
	bool ClickSendCard(int32 Index);
	/** Cartão aceso = há ouro para ele agora. Apagado continua clicável (dá para escolher a torre antes de juntar o ouro). */
	bool IsTowerCardEnabled(int32 Index) const { return TowerCards.IsValidIndex(Index) && TowerCards[Index].State >= 0 && (TowerCards[Index].State & 1); }
	bool IsSendCardEnabled(int32 Index) const { return SendCards.IsValidIndex(Index) && SendCards[Index].State >= 0 && (SendCards[Index].State & 1); }
	UTexture2D* TowerThumb(int32 Index) const { return TowerTex.IsValidIndex(Index) ? TowerTex[Index].Get() : nullptr; }
	UTexture2D* SendThumb(int32 Index) const { return SendTex.IsValidIndex(Index) ? SendTex[Index].Get() : nullptr; }
	/** As miniaturas já foram fotografadas (ou não há arte para fotografar). */
	bool ThumbnailsDone() const { return ThumbStep > ThumbShootFrame; }
	/** Atualiza placar e cartões pelo último snapshot (a cada quadro; o autoteste chama direto para não depender da ordem do quadro). */
	void Refresh();
	/** O mouse está sobre o HUD: o clique não vai para o mundo. */
	bool IsPointerOverUi() const;
	/** O texto do selo de build no canto ("build <hash>"). */
	FString StampText() const;

protected:
	virtual TSharedRef<SWidget> RebuildWidget() override;
	virtual void NativeTick(const FGeometry& MyGeometry, float InDeltaTime) override;

private:
	struct FCard
	{
		UBorder* Frame = nullptr;   // contorno dourado quando é a torre selecionada
		UImage* Thumb = nullptr;
		UTextBlock* Name = nullptr;
		UTextBlock* Cost = nullptr;
		int32 Price = 0;
		int32 State = -1;           // bit 0 aceso, bit 1 selecionado; -1 = ainda não desenhado
	};
	struct FStat
	{
		UTextBlock* Value = nullptr;
		int32 Last = MIN_int32;
	};
	struct FShot
	{
		UPrimitiveComponent* Comp = nullptr;
		bool bSend = false;
		int32 Index = 0;
	};

	void BuildTree();
	FCard MakeCard(int32 Index, bool bSend, float ThumbPx, float WidthPx, UWidget*& OutWidget);
	UTextBlock* MakeText(const FString& Text, int32 Size, const FLinearColor& Color, bool bBold, int32 LetterSpacing = 0);
	UVerticalBox* MakeStat(const FString& Label, FStat& Out, const FLinearColor& Color);
	UBorder* MakePlate(UWidget* Content, const FLinearColor& Accent);
	void SetStat(FStat& S, int32 V, bool bPlus = false);
	void UpdateCard(FCard& C, int32 Gold, bool bSelected);

	void StartThumbnails();
	void FinishThumbnails();
	/** Fotografa em HDR (enquadrando pelo que a câmera viu) e mede a exposição que deixaria o modelo num cinza claro. */
	bool Photograph(UPrimitiveComponent* Comp, const FBox& Box, const FRotator& View, const FString& Name, TArray<FFloat16Color>& Px, float& Exposure);
	/** Revela a foto: média 3x3, curva de filme, sRGB, alfa da cobertura. */
	UTexture2D* Develop(const TArray<FFloat16Color>& Px, float Exposure, const FString& Name);
	bool Shoot(UPrimitiveComponent* Comp, const FVector& Eye, const FRotator& View, float Fov, TArray<FFloat16Color>& Px);

	TWeakObjectPtr<ATdfSimActor> Sim;
	TArray<FCard> TowerCards, SendCards;
	FStat MyLives, MyGold, MyIncome, MyTowers, FoeLives, FoeIncome, FoeTowers, FoeEnemies;
	UPROPERTY() TObjectPtr<UTextBlock> Clock;
	UPROPERTY() TObjectPtr<UTextBlock> Sudden;
	UPROPERTY() TObjectPtr<UTextBlock> ToastText;
	UPROPERTY() TObjectPtr<UBorder> TopLeft;
	UPROPERTY() TObjectPtr<UBorder> TopRight;
	UPROPERTY() TObjectPtr<UBorder> Shop;
	/** Fim de partida: VITÓRIA / DERROTA por cima de tudo, com "R para jogar de novo". */
	UPROPERTY() TObjectPtr<UBorder> EndPanel;
	UPROPERTY() TObjectPtr<UTextBlock> EndTitle;
	UPROPERTY() TObjectPtr<UTextBlock> Stamp;
	int32 LastResult = 0;
	int32 LastClock = -1;
	FString LastToast;

	UPROPERTY() TArray<TObjectPtr<UTexture2D>> TowerTex;
	UPROPERTY() TArray<TObjectPtr<UTexture2D>> SendTex;
	UPROPERTY() TObjectPtr<AActor> Studio;
	UPROPERTY() TObjectPtr<USceneCaptureComponent2D> Camera;
	UPROPERTY() TObjectPtr<class UPointLightComponent> FillLight;
	UPROPERTY() TObjectPtr<UTextureRenderTarget2D> Film;
	TArray<FShot> Shots;
	FRotator StudioView = FRotator(-18.f, 0.f, 0.f);
	int32 ThumbStep = 0;
	/** Em que quadro do HUD fotografar: a luz do céu (captura em tempo real) leva alguns quadros para existir. */
	static constexpr int32 ThumbShootFrame = 12;
};
