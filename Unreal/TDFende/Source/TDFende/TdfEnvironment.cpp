#include "TdfEnvironment.h"
#include "Components/SkyAtmosphereComponent.h"
#include "Components/VolumetricCloudComponent.h"
#include "Components/ExponentialHeightFogComponent.h"
#include "Components/DirectionalLightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Components/PostProcessComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInterface.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "Math/RandomStream.h"
#include "UObject/ConstructorHelpers.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"

DEFINE_LOG_CATEGORY_STATIC(LogTdfAmb, Log, All);

namespace
{
	const TCHAR* AmbPath = TEXT("/Game/TDFende/Ambiente");

	UStaticMesh* LoadScenery(const TCHAR* Name)
	{
		return LoadObject<UStaticMesh>(nullptr, *FString::Printf(TEXT("%s/Cenario/%s.%s"), AmbPath, Name, Name));
	}
}

ATdfEnvironment::ATdfEnvironment()
{
	PrimaryActorTick.bCanEverTick = false;
	RootScene = CreateDefaultSubobject<USceneComponent>(TEXT("Raiz"));
	SetRootComponent(RootScene);

	// céu físico: a atmosfera pinta o céu e a névoa pela posição do sol; as nuvens são volumétricas (material padrão da engine)
	Sky = CreateDefaultSubobject<USkyAtmosphereComponent>(TEXT("Atmosfera"));
	Sky->SetupAttachment(RootScene);
	Clouds = CreateDefaultSubobject<UVolumetricCloudComponent>(TEXT("Nuvens"));
	Clouds->SetupAttachment(RootScene);

	Fog = CreateDefaultSubobject<UExponentialHeightFogComponent>(TEXT("Nevoa"));
	Fog->SetupAttachment(RootScene);
	Fog->SetFogDensity(0.012f);
	Fog->SetFogHeightFalloff(0.18f);
	Fog->SetStartDistance(1500.f);
	Fog->SetVolumetricFog(true);
	Fog->SetVolumetricFogDistance(9000.f);

	Sun = CreateDefaultSubobject<UDirectionalLightComponent>(TEXT("Sol"));
	Sun->SetupAttachment(RootScene);
	Sun->SetMobility(EComponentMobility::Movable);
	Sun->SetAtmosphereSunLight(true);
	Sun->SetIntensity(9.f); // lux; a exposição automática (faixa estendida) cuida do resto
	Sun->SetLightColor(FLinearColor(1.f, 0.96f, 0.9f));
	Sun->SetDynamicShadowDistanceMovableLight(20000.f);

	SkyLight = CreateDefaultSubobject<USkyLightComponent>(TEXT("LuzDoCeu"));
	SkyLight->SetupAttachment(RootScene);
	SkyLight->SetMobility(EComponentMobility::Movable);
	SkyLight->SetRealTimeCapture(true); // captura a atmosfera e as nuvens a cada quadro (luz do céu coerente com o sol)
	SkyLight->SetIntensity(1.f);

	// pós-processo do jogo inteiro: exposição um pouco acima, brilho e vinheta discretos, cor levemente mais viva
	Post = CreateDefaultSubobject<UPostProcessComponent>(TEXT("PosProcesso"));
	Post->SetupAttachment(RootScene);
	Post->bUnbound = true;
	FPostProcessSettings& S = Post->Settings;
	S.bOverride_AutoExposureBias = true;       S.AutoExposureBias = -0.2f;
	S.bOverride_BloomIntensity = true;         S.BloomIntensity = 0.45f;
	S.bOverride_VignetteIntensity = true;      S.VignetteIntensity = 0.32f;
	S.bOverride_ColorSaturation = true;        S.ColorSaturation = FVector4(1.f, 1.f, 1.f, 1.06f);
	S.bOverride_ColorContrast = true;          S.ColorContrast = FVector4(1.f, 1.f, 1.f, 1.04f);

	static ConstructorHelpers::FObjectFinder<UStaticMesh> Plane(TEXT("/Engine/BasicShapes/Plane.Plane"));
	Ground = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Chao"));
	Ground->SetupAttachment(RootScene);
	Ground->SetStaticMesh(Plane.Object);
	Ground->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	Ground->SetMobility(EComponentMobility::Static);
}

void ATdfEnvironment::SetPlayArea(const FBox2D& Area, float InLaneOffsetX, float InLaneHalfX, float InLaneHalfY)
{
	PlayArea = Area;
	LaneOffsetX = InLaneOffsetX;
	LaneHalfX = InLaneHalfX;
	LaneHalfY = InLaneHalfY;
}

void ATdfEnvironment::BeginPlay()
{
	Super::BeginPlay();
	Sun->SetWorldRotation(FRotator(-SunElevation, SunAzimuth, 0.f));
	BuildGround();
	ScatterScenery();
	ScatterNature();
	LineBorders();
	// -TdfSemGrama: mede quanto a grama custa (A/B de desempenho)
	if (!FParse::Param(FCommandLine::Get(), TEXT("TdfSemGrama"))) ScatterGrass();
}

bool ATdfEnvironment::InsidePlay(const FVector2D& P, float Margin) const
{
	return P.X > PlayArea.Min.X - Margin && P.X < PlayArea.Max.X + Margin && P.Y > PlayArea.Min.Y - Margin && P.Y < PlayArea.Max.Y + Margin;
}

UHierarchicalInstancedStaticMeshComponent* ATdfEnvironment::MakeHism(UStaticMesh* Mesh, float CullCm, bool bShadows)
{
	UHierarchicalInstancedStaticMeshComponent* C = NewObject<UHierarchicalInstancedStaticMeshComponent>(this);
	C->SetStaticMesh(Mesh);
	C->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	C->SetCastShadow(bShadows);
	C->SetCullDistances(0, int32(CullCm));
	C->SetupAttachment(RootScene);
	C->RegisterComponent();
	KeepAlive.Add(C);
	KeepAlive.Add(Mesh);
	return C;
}

void ATdfEnvironment::BuildGround()
{
	// um plano de 600 m: o material usa a posição no mundo como UV, então não estica
	const FVector2D C = PlayArea.GetCenter();
	Ground->SetWorldLocation(FVector(C.X, C.Y, -1.f));
	Ground->SetWorldScale3D(FVector(600.f, 600.f, 1.f));
	if (UMaterialInterface* M4k = LoadObject<UMaterialInterface>(nullptr, *FString::Printf(TEXT("%s/Chao4K/M_TdfChao4K.M_TdfChao4K"), AmbPath)))
	{
		// chão 4K (Poly Haven): campo verde fora, terra pisada e cascalho dentro das lanes; o material precisa saber onde elas estão
		UMaterialInstanceDynamic* Mid = UMaterialInstanceDynamic::Create(M4k, this);
		Mid->SetScalarParameterValue(TEXT("LaneX0"), 0.f);
		Mid->SetScalarParameterValue(TEXT("LaneX1"), LaneOffsetX);
		Mid->SetScalarParameterValue(TEXT("LaneHX"), LaneHalfX);
		Mid->SetScalarParameterValue(TEXT("LaneHY"), LaneHalfY);
		Mid->SetScalarParameterValue(TEXT("Borda"), 320.f);
		Ground->SetMaterial(0, Mid);
		KeepAlive.Add(Mid);
	}
	else if (UMaterialInterface* M = LoadObject<UMaterialInterface>(nullptr, *FString::Printf(TEXT("%s/Materiais/M_TdfChao.M_TdfChao"), AmbPath)))
	{
		Ground->SetMaterial(0, M);
		KeepAlive.Add(M);
	}
	else UE_LOG(LogTdfAmb, Warning, TEXT("Sem M_TdfChao (rode ImportarArte.ps1 -Script importa_ambiente.py): chão liso"));
}

void ATdfEnvironment::ScatterScenery()
{
	FRandomStream R(Seed);
	const FVector2D C = PlayArea.GetCenter();
	const float Half = FMath::Max(PlayArea.GetExtent().X, PlayArea.GetExtent().Y);

	struct FPiece { const TCHAR* Name; int32 Count; float MinDist, MaxDist, MinScale, MaxScale; bool bShadow; bool bLying; };
	const FPiece Pieces[] = {
		// paredões e rochas grandes: o horizonte (a câmera olha para +X; ficam mais desse lado e dos lados)
		{TEXT("SM_rock_face_01"), 7, 2600.f, 6000.f, 0.9f, 1.6f, true, false},
		{TEXT("SM_rock_face_02"), 7, 2600.f, 6000.f, 0.9f, 1.6f, true, false},
		{TEXT("SM_rock_moss_set_01"), 6, 1800.f, 4500.f, 0.6f, 1.1f, true, false},
		// perto: matacões, tocos e troncos caídos
		{TEXT("SM_boulder_01"), 14, 350.f, 2600.f, 0.5f, 1.3f, true, false},
		{TEXT("SM_tree_stump_01"), 7, 400.f, 2400.f, 0.7f, 1.2f, true, false},
		{TEXT("SM_tree_stump_02"), 7, 400.f, 2400.f, 0.7f, 1.2f, true, false},
		{TEXT("SM_dead_tree_trunk"), 6, 500.f, 2600.f, 0.8f, 1.3f, true, true},
		{TEXT("SM_dead_tree_trunk_02"), 5, 600.f, 3000.f, 0.7f, 1.2f, true, true},
		{TEXT("SM_dead_quiver_trunk"), 5, 600.f, 3000.f, 0.9f, 1.4f, true, false},
	};
	int32 Placed = 0;
	for (const FPiece& Pc : Pieces)
	{
		UStaticMesh* M = LoadScenery(Pc.Name);
		if (!M) { UE_LOG(LogTdfAmb, Warning, TEXT("Sem %s"), Pc.Name); continue; }
		UHierarchicalInstancedStaticMeshComponent* H = MakeHism(M, 0.f, Pc.bShadow);
		for (int32 i = 0, Tries = 0; i < Pc.Count && Tries < Pc.Count * 40; ++Tries)
		{
			// ponto ao redor da área de jogo; a frente da câmera (+X) recebe o dobro de chance
			const float Ang = R.FRandRange(-PI, PI);
			const float Dist = Half + R.FRandRange(Pc.MinDist, Pc.MaxDist);
			FVector2D P = C + FVector2D(FMath::Cos(Ang), FMath::Sin(Ang)) * Dist;
			if (P.X < C.X && R.FRand() < 0.5f) continue;
			if (InsidePlay(P, Pc.MinDist * 0.5f)) continue;
			const float Scale = R.FRandRange(Pc.MinScale, Pc.MaxScale);
			const FRotator Rot(Pc.bLying ? 0.f : R.FRandRange(-4.f, 4.f), R.FRandRange(0.f, 360.f), Pc.bLying ? 0.f : R.FRandRange(-4.f, 4.f));
			H->AddInstance(FTransform(Rot, FVector(P.X, P.Y, -R.FRandRange(5.f, 30.f) * Scale), FVector(Scale)), true);
			++i; ++Placed;
		}
	}
	UE_LOG(LogTdfAmb, Display, TEXT("Cenário: %d peças"), Placed);
}

void ATdfEnvironment::ScatterNature()
{
	// árvores, arbusto e rochas do acervo (galeria do Meshy, CC0): importados por importa_ambiente.py em Ambiente/Natureza/<modelo>/
	FAssetRegistryModule& Reg = FModuleManager::LoadModuleChecked<FAssetRegistryModule>(TEXT("AssetRegistry"));
	const FString Dir = FString::Printf(TEXT("%s/Natureza"), AmbPath);
	Reg.Get().ScanPathsSynchronous({ Dir }, false);
	TArray<FAssetData> Assets;
	Reg.Get().GetAssetsByPath(FName(*Dir), Assets, true);
	auto Find = [&Assets](const TCHAR* Key) -> UStaticMesh*
	{
		for (const FAssetData& A : Assets)
			if (A.AssetName.ToString().Contains(Key))
				if (UStaticMesh* M = Cast<UStaticMesh>(A.GetAsset())) return M;
		return nullptr;
	};

	// altura no jogo (cm); os modelos do Meshy vêm todos com ~2 m e são normalizados pela caixa
	struct FKind { const TCHAR* Key; int32 Count; float MinDist, MaxDist, HeightCm; bool bTall; };
	const FKind Kinds[] = {
		{TEXT("oak_tree"), 11, 500.f, 5200.f, 900.f, true},
		{TEXT("olive"), 8, 450.f, 4200.f, 620.f, true},
		{TEXT("cypress_tree_chad"), 8, 600.f, 5600.f, 1150.f, true},
		{TEXT("cypress_tree_swaney"), 8, 600.f, 5600.f, 1050.f, true},
		{TEXT("berry"), 24, 150.f, 2600.f, 120.f, false},
		{TEXT("cracked_gray_rock_stack"), 7, 400.f, 3600.f, 260.f, false},
		{TEXT("kaya3"), 6, 400.f, 3600.f, 230.f, false},
		{TEXT("granite_boulder"), 9, 250.f, 3000.f, 150.f, false},
	};
	FRandomStream R(Seed + 37);
	const FVector2D C = PlayArea.GetCenter();
	const float HalfX = PlayArea.GetExtent().X, HalfY = PlayArea.GetExtent().Y;
	int32 Placed = 0;
	for (const FKind& K : Kinds)
	{
		UStaticMesh* M = Find(K.Key);
		if (!M) { UE_LOG(LogTdfAmb, Warning, TEXT("Sem a natureza %s (rode ImportarArte.ps1 -Script importa_ambiente.py)"), K.Key); continue; }
		const float Base = K.HeightCm / FMath::Max(M->GetBounds().BoxExtent.Z * 2.f, 1.f);
		UHierarchicalInstancedStaticMeshComponent* H = MakeHism(M, 0.f, true);
		for (int32 i = 0, Tries = 0; i < K.Count && Tries < K.Count * 60; ++Tries)
		{
			// anel em volta da área de jogo (retângulo expandido), não um círculo: as lanes são compridas em X
			const float Dist = R.FRandRange(K.MinDist, K.MaxDist);
			const int32 Side = R.RandRange(0, 3);
			FVector2D P;
			if (Side < 2) P = FVector2D(R.FRandRange(PlayArea.Min.X - Dist, PlayArea.Max.X + Dist), Side == 0 ? PlayArea.Min.Y - Dist : PlayArea.Max.Y + Dist);
			else P = FVector2D(Side == 2 ? PlayArea.Min.X - Dist : PlayArea.Max.X + Dist, R.FRandRange(PlayArea.Min.Y - Dist, PlayArea.Max.Y + Dist));
			if (InsidePlay(P, K.bTall ? 350.f : 120.f)) continue;
			// árvore alta não fica entre a câmera (atrás da base, em X menor) e a lane do jogador
			if (K.bTall && P.X < PlayArea.Min.X && FMath::Abs(P.Y - C.Y) < HalfY + 600.f) continue;
			const float S = Base * R.FRandRange(0.8f, 1.25f);
			H->AddInstance(FTransform(FRotator(0.f, R.FRandRange(0.f, 360.f), 0.f), FVector(P.X, P.Y, -3.f), FVector(S)), true);
			++i; ++Placed;
		}
	}
	UE_LOG(LogTdfAmb, Display, TEXT("Natureza: %d peças (árvores, arbustos, rochas)"), Placed);
}

void ATdfEnvironment::LineBorders()
{
	// pedras pequenas contornando cada lane: a borda da área de jogo lida como chão de verdade, não como linha desenhada
	UStaticMesh* Pebbles[] = { LoadScenery(TEXT("SM_rock_07")), LoadScenery(TEXT("SM_rock_09")) };
	UHierarchicalInstancedStaticMeshComponent* H[2] = {};
	for (int32 k = 0; k < 2; ++k) if (Pebbles[k]) H[k] = MakeHism(Pebbles[k], 12000.f, true);
	if (!H[0] && !H[1]) return;
	FRandomStream R(Seed + 11);
	int32 N = 0;
	for (int32 Lane = 0; Lane < 2; ++Lane)
	{
		const float Cx = Lane * LaneOffsetX;
		const FVector2D Lo(Cx - LaneHalfX, -LaneHalfY), Hi(Cx + LaneHalfX, LaneHalfY);
		const FVector2D Corners[5] = { Lo, FVector2D(Hi.X, Lo.Y), Hi, FVector2D(Lo.X, Hi.Y), Lo };
		for (int32 e = 0; e < 4; ++e)
		{
			const FVector2D A = Corners[e], B = Corners[e + 1];
			const FVector2D Dir = (B - A).GetSafeNormal();
			const FVector2D Out(Dir.Y, -Dir.X); // os cantos vão no sentido anti-horário: (y, -x) aponta para fora
			const float Len = (B - A).Size();
			for (float d = 0.f; d < Len; d += R.FRandRange(18.f, 42.f))
			{
				const FVector2D P = A + Dir * d + Out * R.FRandRange(12.f, 48.f);
				const int32 k = (H[0] && (!H[1] || R.FRand() < 0.35f)) ? 0 : 1;
				const float Scale = k == 0 ? R.FRandRange(0.6f, 1.5f) : R.FRandRange(1.0f, 2.6f);
				H[k]->AddInstance(FTransform(FRotator(R.FRandRange(-20.f, 20.f), R.FRandRange(0.f, 360.f), R.FRandRange(-20.f, 20.f)),
					FVector(P.X, P.Y, -2.f), FVector(Scale)), true);
				++N;
			}
		}
	}
	UE_LOG(LogTdfAmb, Display, TEXT("Borda das lanes: %d pedras"), N);
}

void ATdfEnvironment::ScatterGrass()
{
	TArray<UStaticMesh*> Tufts;
	FAssetRegistryModule& Reg = FModuleManager::LoadModuleChecked<FAssetRegistryModule>(TEXT("AssetRegistry"));
	TArray<FAssetData> Assets;
	// no editor em modo de jogo a varredura do registro é assíncrona e pode não ter chegado nesta pasta ainda
	Reg.Get().ScanPathsSynchronous({ FString::Printf(TEXT("%s/Grama"), AmbPath) }, false);
	Reg.Get().GetAssetsByPath(FName(*FString::Printf(TEXT("%s/Grama"), AmbPath)), Assets, true);
	for (const FAssetData& A : Assets)
		if (UStaticMesh* M = Cast<UStaticMesh>(A.GetAsset())) Tufts.Add(M);
	if (Tufts.Num() == 0) { UE_LOG(LogTdfAmb, Warning, TEXT("Sem grama 3D em %s/Grama"), AmbPath); return; }

	TArray<UHierarchicalInstancedStaticMeshComponent*> H;
	TArray<float> Norm; // a touceira da Poly Haven vem com 14-15 cm; o Unity normalizava a maior para 42 cm
	for (UStaticMesh* M : Tufts)
	{
		H.Add(MakeHism(M, 9000.f, false));
		const float Hgt = FMath::Max(M->GetBounds().BoxExtent.Z * 2.f, 1.f);
		Norm.Add(FMath::Clamp(42.f / Hgt, 1.f, 4.f));
	}
	FRandomStream R(Seed + 23);
	const FVector2D C = PlayArea.GetCenter();
	const float Half = FMath::Max(PlayArea.GetExtent().X, PlayArea.GetExtent().Y);
	int32 N = 0;
	for (int32 Tries = 0; N < GrassCount && Tries < GrassCount * 30; ++Tries)
	{
		const FVector2D P = C + FVector2D(R.FRandRange(-Half - 4500.f, Half + 4500.f), R.FRandRange(-Half - 4500.f, Half + 4500.f));
		if (InsidePlay(P, 70.f)) continue;
		// mais denso junto das lanes (é para lá que a câmera olha), rareando com a distância
		const float D = FMath::Max(FMath::Max(PlayArea.Min.X - P.X, P.X - PlayArea.Max.X), FMath::Max(PlayArea.Min.Y - P.Y, P.Y - PlayArea.Max.Y));
		if (R.FRand() > FMath::Exp(-FMath::Max(D, 0.f) / 1800.f)) continue;
		const int32 k = R.RandRange(0, H.Num() - 1);
		H[k]->AddInstance(FTransform(FRotator(0.f, R.FRandRange(0.f, 360.f), 0.f), FVector(P.X, P.Y, -1.f), FVector(Norm[k] * R.FRandRange(0.6f, 1.25f))), true);
		++N;
	}
	UE_LOG(LogTdfAmb, Display, TEXT("Grama: %d touceiras de %d variantes"), N, Tufts.Num());
}
