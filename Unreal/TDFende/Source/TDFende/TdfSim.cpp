#include "TdfSim.h"
#include "HAL/PlatformProcess.h"
#include "Misc/Paths.h"

bool FTdfSim::Load(FString& OutError)
{
	const FString Path = FPaths::Combine(FPaths::ProjectDir(), TEXT("Binaries/Win64/tdfende_sim.dll"));
	Handle = FPlatformProcess::GetDllHandle(*Path);
	if (!Handle)
	{
		OutError = FString::Printf(TEXT("não achei/carreguei %s (rode Tools/SimBridge/PublicarParaUnreal.ps1)"), *Path);
		return false;
	}

#define TDF_BIND(Member, Name) Member = reinterpret_cast<decltype(Member)>(FPlatformProcess::GetDllExport(Handle, TEXT(Name))); \
	if (!Member) { OutError = TEXT("falta a função " Name); Unload(); return false; }
	TDF_BIND(abi_version, "tdf_abi_version")
	TDF_BIND(struct_sizes, "tdf_struct_sizes")
	TDF_BIND(create, "tdf_create")
	TDF_BIND(destroy, "tdf_destroy")
	TDF_BIND(last_error, "tdf_last_error")
	TDF_BIND(enqueue, "tdf_enqueue")
	TDF_BIND(step, "tdf_step")
	TDF_BIND(tick, "tdf_tick")
	TDF_BIND(over, "tdf_over")
	TDF_BIND(snapshot, "tdf_snapshot")
	TDF_BIND(lanes, "tdf_lanes")
	TDF_BIND(enemies, "tdf_enemies")
	TDF_BIND(towers, "tdf_towers")
	TDF_BIND(projectiles, "tdf_projectiles")
	TDF_BIND(territory, "tdf_territory")
	TDF_BIND(events, "tdf_events")
	TDF_BIND(fingerprint, "tdf_fingerprint")
	TDF_BIND(signature, "tdf_signature")
#undef TDF_BIND

	int Sizes[5] = {};
	struct_sizes(Sizes);
	if (abi_version() != TDF_ABI_VERSION || Sizes[0] != sizeof(TdfLane) || Sizes[1] != sizeof(TdfEnemy) || Sizes[2] != sizeof(TdfTower)
		|| Sizes[3] != sizeof(TdfProjectile) || Sizes[4] != sizeof(TdfEvent))
	{
		OutError = FString::Printf(TEXT("ABI diferente do cabeçalho (dll v%d; structs %d,%d,%d,%d,%d)"), abi_version(), Sizes[0], Sizes[1], Sizes[2], Sizes[3], Sizes[4]);
		Unload();
		return false;
	}
	return true;
}

void FTdfSim::Unload()
{
	if (Handle)
	{
		if (destroy) destroy();
		FPlatformProcess::FreeDllHandle(Handle);
		Handle = nullptr;
	}
}

FString FTdfSim::LastError() const
{
	if (!last_error) return TEXT("");
	char Buf[512] = {};
	last_error(Buf, sizeof(Buf));
	return UTF8_TO_TCHAR(Buf);
}

FString FTdfSim::Fingerprint() const
{
	if (!fingerprint) return TEXT("");
	char Buf[512] = {};
	fingerprint(Buf, sizeof(Buf));
	return UTF8_TO_TCHAR(Buf);
}
