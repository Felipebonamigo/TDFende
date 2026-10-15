#include "TdfFxComponent.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInterface.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "UObject/ConstructorHelpers.h"

namespace
{
	/** Como é o tiro de cada torre (canhão, morteiro, gelo, sentinela, fogo, ar). Tamanhos em cm; brilho em unidades de emissão. */
	struct FShotLook
	{
		bool bIron;            // bala sólida (fumaça atrás) ou núcleo que brilha (rastro de luz)
		FLinearColor Color;    // cor do núcleo / do rastro
		float Core;            // diâmetro do núcleo
		float Stretch;         // núcleo alongado na direção do voo (virote da sentinela)
		float Glow;            // brilho do núcleo
		float TrailWidth;      // espessura do rastro
		float TrailGlow;       // brilho do rastro (ou opacidade da fumaça)
		FLinearColor Flash;    // cor do clarão de disparo e de impacto
	};
	// brilhos medidos no print: acima de ~10 a cor estoura para branco (a cena é exposta para um sol de 9 lux)
	const FShotLook Looks[6] = {
		{true, FLinearColor(0.05f, 0.05f, 0.055f), 14.f, 1.f, 0.f, 9.f, 0.30f, FLinearColor(1.f, 0.62f, 0.25f)},
		{true, FLinearColor(0.06f, 0.05f, 0.045f), 20.f, 1.f, 0.f, 13.f, 0.36f, FLinearColor(1.f, 0.55f, 0.2f)},
		{false, FLinearColor(0.25f, 0.65f, 1.f), 12.f, 1.f, 6.f, 6.f, 2.5f, FLinearColor(0.3f, 0.7f, 1.f)},
		{false, FLinearColor(1.f, 0.78f, 0.35f), 7.f, 3.f, 8.f, 3.f, 3.f, FLinearColor(1.f, 0.8f, 0.4f)},
		{false, FLinearColor(1.f, 0.38f, 0.05f), 18.f, 1.f, 9.f, 12.f, 4.f, FLinearColor(1.f, 0.4f, 0.06f)},
		{false, FLinearColor(0.8f, 0.92f, 1.f), 16.f, 1.f, 2.f, 10.f, 1.2f, FLinearColor(0.85f, 0.95f, 1.f)},
	};
	const FLinearColor DustColor(0.30f, 0.26f, 0.21f);
	const FLinearColor SmokeColor(0.55f, 0.55f, 0.56f);

	const FShotLook& Look(int32 Type) { return Looks[FMath::Clamp(Type, 0, 5)]; }
	float EaseOut(float T) { T = FMath::Clamp(T, 0.f, 1.f); return 1.f - (1.f - T) * (1.f - T); }
}

UTdfFxComponent::UTdfFxComponent()
{
	PrimaryComponentTick.bCanEverTick = false; // quem anda os efeitos é o dono (TickFx), no mesmo passo da vista
}

UInstancedStaticMeshComponent* UTdfFxComponent::MakeIsm(const TCHAR* Name, UMaterialInterface* Material)
{
	UStaticMesh* Sphere = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Sphere.Sphere"));
	UInstancedStaticMeshComponent* C = NewObject<UInstancedStaticMeshComponent>(GetOwner(), Name);
	C->SetStaticMesh(Sphere);
	C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	C->SetCastShadow(false);
	C->SetNumCustomDataFloats(4);
	if (Material) C->SetMaterial(0, Material);
	C->SetupAttachment(this);
	C->RegisterComponent();
	return C;
}

void UTdfFxComponent::Setup()
{
	UMaterialInterface* GlowMat = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/TDFende/Efeitos/M_TdfBrilho.M_TdfBrilho"));
	UMaterialInterface* SmokeMat = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/TDFende/Efeitos/M_TdfFumaca.M_TdfFumaca"));
	UMaterialInterface* Basic = LoadObject<UMaterialInterface>(nullptr, TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial"));
	if (!GlowMat || !SmokeMat) UE_LOG(LogTemp, Warning, TEXT("Efeitos: faltam os materiais (rode ImportarArte.ps1 -Script importa_efeitos.py)"));
	Iron = MakeIsm(TEXT("TirosFerro"), Basic);
	Glow = MakeIsm(TEXT("TirosBrilho"), GlowMat ? GlowMat : Basic);
	Smoke = MakeIsm(TEXT("TirosFumaca"), SmokeMat ? SmokeMat : Basic);
	Flashes = MakeIsm(TEXT("Claroes"), GlowMat ? GlowMat : Basic);
	Puffs = MakeIsm(TEXT("Poeira"), SmokeMat ? SmokeMat : Basic);
	Iron->SetCastShadow(true); // bala de ferro é sólida: faz sombra
	if (Basic)
	{
		UMaterialInstanceDynamic* Dark = UMaterialInstanceDynamic::Create(Basic, this);
		Dark->SetVectorParameterValue(TEXT("Color"), FLinearColor(0.035f, 0.035f, 0.04f));
		Iron->SetMaterial(0, Dark);
	}
}

void UTdfFxComponent::Fill(UInstancedStaticMeshComponent* C, const TArray<FTransform>& Xf, const TArray<FLinearColor>& Data)
{
	if (!C) return;
	C->ClearInstances();
	if (Xf.Num() == 0) return;
	C->AddInstances(Xf, false, true);
	for (int32 i = 0; i < Data.Num(); ++i)
	{
		const float V[4] = { Data[i].R, Data[i].G, Data[i].B, Data[i].A };
		C->SetCustomData(i, TArrayView<const float>(V, 4), i == Data.Num() - 1);
	}
}

void UTdfFxComponent::UpdateShots(const TdfProjectile* Shots, int32 Count, bool bNewTick, float Alpha, TFunctionRef<FVector(int32, float, float, float)> ToWorld)
{
	if (bNewTick || Cur.Num() != Count)
	{
		if (bNewTick) Swap(Prev, Cur);
		Cur.SetNum(Count, EAllowShrinking::No);
		From.SetNum(Count, EAllowShrinking::No);
		for (int32 i = 0; i < Count; ++i)
			Cur[i] = { Shots[i].Lane, Shots[i].TowerType, ToWorld(Shots[i].Lane, Shots[i].X, Shots[i].Y, Shots[i].Z) };
		// casa cada projétil com o do tique anterior (mesma lane e tipo, o mais perto, até 2,5 m); o resto é tiro novo
		TBitArray<> Used(false, Prev.Num());
		for (int32 i = 0; i < Count; ++i)
		{
			int32 Best = -1;
			float BestD = 250.f * 250.f;
			for (int32 j = 0; j < Prev.Num(); ++j)
			{
				if (Used[j] || Prev[j].Lane != Cur[i].Lane || Prev[j].Type != Cur[i].Type) continue;
				const float D = FVector::DistSquared(Prev[j].Pos, Cur[i].Pos);
				if (D < BestD) { BestD = D; Best = j; }
			}
			if (Best >= 0) Used[Best] = true;
			From[i] = Best >= 0 ? Prev[Best].Pos : Cur[i].Pos;
		}
	}

	XIron.Reset(); XGlow.Reset(); XSmoke.Reset(); DIron.Reset(); DGlow.Reset(); DSmoke.Reset();
	Drawn.SetNum(Count, EAllowShrinking::No);
	TrailCount = 0;
	const float A = FMath::Clamp(Alpha, 0.f, 1.f);
	for (int32 i = 0; i < Count; ++i)
	{
		const FShotLook& L = Look(Cur[i].Type);
		const FVector P = FMath::Lerp(From[i], Cur[i].Pos, A);
		Drawn[i] = P;
		const FVector Step = Cur[i].Pos - From[i];
		const float Moved = Step.Size();
		const FVector Dir = Moved > 1.f ? Step / Moved : FVector::ForwardVector;
		const FQuat Q = Dir.ToOrientationQuat();
		const FVector CoreScale = FVector(L.Core * L.Stretch, L.Core, L.Core) / 100.f; // a esfera básica tem 100 cm
		if (L.bIron)
		{
			XIron.Add(FTransform(Q, P, CoreScale));
			DIron.Add(L.Color);
		}
		else
		{
			XGlow.Add(FTransform(Q, P, CoreScale));
			DGlow.Add(FLinearColor(L.Color.R, L.Color.G, L.Color.B, L.Glow));
		}
		// rastro: atrás do tiro, na direção em que ele andou, mais comprido quanto mais rápido
		if (Moved > 1.f)
		{
			const float Len = FMath::Clamp(Moved * 2.5f, 20.f, 160.f);
			const FTransform T(Q, P - Dir * (Len * 0.5f + L.Core * 0.3f), FVector(Len, L.TrailWidth, L.TrailWidth) / 100.f);
			if (L.bIron) { XSmoke.Add(T); DSmoke.Add(FLinearColor(SmokeColor.R, SmokeColor.G, SmokeColor.B, L.TrailGlow)); }
			else { XGlow.Add(T); DGlow.Add(FLinearColor(L.Color.R, L.Color.G, L.Color.B, L.TrailGlow)); }
			++TrailCount;
		}
	}
	// o material básico da engine (sem os de efeito) só tem a cor pelo parâmetro "Color": a bala fica cinza, tudo bem
	Fill(Iron, XIron, DIron);
	Fill(Glow, XGlow, DGlow);
	Fill(Smoke, XSmoke, DSmoke);
}

void UTdfFxComponent::Flash(const FVector& At, ETdfFx Style, int32 TowerType, float Size)
{
	FLive F;
	F.Pos = At;
	F.Style = Style;
	F.Size = Size;
	F.Color = Style == ETdfFx::Dust ? DustColor : TowerType >= 0 ? Look(TowerType).Flash : FLinearColor(1.f, 0.9f, 0.7f);
	F.Life = Style == ETdfFx::Muzzle ? 0.12f : Style == ETdfFx::Impact ? 0.25f : Style == ETdfFx::Gust ? 0.4f : 0.9f;
	if (Style == ETdfFx::Gust) F.Color = FLinearColor(0.85f, 0.95f, 1.f);
	Live.Add(F);
	bLiveDirty = true; // desenha uma vez por quadro, no TickFx (num quadro com 50 eventos, refazer a cada um era quadrático)
}

void UTdfFxComponent::TickFx(float DeltaSeconds)
{
	if (Live.Num() == 0 && !bLiveDirty) return;
	for (int32 i = Live.Num() - 1; i >= 0; --i)
	{
		Live[i].Age += DeltaSeconds;
		if (Live[i].Age >= Live[i].Life) Live.RemoveAtSwap(i, EAllowShrinking::No);
	}
	RedrawLive();
	bLiveDirty = Live.Num() > 0; // o último quadro com clarão ainda limpa as instâncias
}

void UTdfFxComponent::Clear()
{
	Prev.Reset();
	Cur.Reset();
	From.Reset();
	Drawn.Reset();
	TrailCount = 0;
	Live.Reset();
	bLiveDirty = false;
	for (UInstancedStaticMeshComponent* C : {Iron.Get(), Glow.Get(), Smoke.Get(), Flashes.Get(), Puffs.Get()})
		if (C) C->ClearInstances();
}

void UTdfFxComponent::RedrawLive()
{
	XGlow.Reset(); DGlow.Reset(); XSmoke.Reset(); DSmoke.Reset();
	for (const FLive& F : Live)
	{
		const float T = F.Age / F.Life;
		switch (F.Style)
		{
		case ETdfFx::Muzzle: // estoura rápido
		{
			const float D = 26.f * F.Size * (1.f + 0.6f * EaseOut(T));
			XGlow.Add(FTransform(FQuat::Identity, F.Pos, FVector(D / 100.f)));
			DGlow.Add(FLinearColor(F.Color.R, F.Color.G, F.Color.B, 14.f * (1.f - T)));
			break;
		}
		case ETdfFx::Impact: // cresce e apaga
		{
			const float D = 28.f * F.Size * (0.5f + 1.0f * EaseOut(T));
			XGlow.Add(FTransform(FQuat::Identity, F.Pos, FVector(D / 100.f)));
			DGlow.Add(FLinearColor(F.Color.R, F.Color.G, F.Color.B, 9.f * (1.f - T) * (1.f - T)));
			break;
		}
		case ETdfFx::Gust: // disco achatado que abre rápido rente ao chão e apaga
		{
			const float D = 60.f * F.Size * (0.4f + 1.6f * EaseOut(T));
			XGlow.Add(FTransform(FQuat::Identity, F.Pos + FVector(0.f, 0.f, 15.f), FVector(D / 100.f, D / 100.f, D * 0.12f / 100.f)));
			DGlow.Add(FLinearColor(F.Color.R, F.Color.G, F.Color.B, 3.f * (1.f - T)));
			break;
		}
		case ETdfFx::Dust: // nuvem que sobe, abre e some
		{
			const float D = 50.f * F.Size * (0.6f + 1.6f * EaseOut(T));
			XSmoke.Add(FTransform(FQuat::Identity, F.Pos + FVector(0.f, 0.f, 10.f + 30.f * T), FVector(D / 100.f, D / 100.f, D * 0.7f / 100.f)));
			DSmoke.Add(FLinearColor(F.Color.R, F.Color.G, F.Color.B, 0.35f * (1.f - T)));
			break;
		}
		}
	}
	Fill(Flashes, XGlow, DGlow);
	Fill(Puffs, XSmoke, DSmoke);
}
