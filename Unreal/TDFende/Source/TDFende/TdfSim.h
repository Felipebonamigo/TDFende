#pragma once

#include "CoreMinimal.h"

// Evita o dllimport do cabeçalho C: a DLL é carregada à mão e as funções vêm por ponteiro.
#define TDF_API
#include "tdfende_sim.h"

/** Ponte nativa da Sim (Tools/SimBridge). Carrega tdfende_sim.dll e expõe as funções tdf_* como ponteiros. Uma instância por jogo. */
class FTdfSim
{
public:
	bool Load(FString& OutError);
	void Unload();
	bool IsLoaded() const { return Handle != nullptr; }

	int (*abi_version)() = nullptr;
	int (*struct_sizes)(int*) = nullptr;
	int (*create)(int, int, int, int, const char*, const char*) = nullptr;
	void (*destroy)() = nullptr;
	int (*last_error)(char*, int) = nullptr;
	int (*enqueue)(int, int, int, int) = nullptr;
	int (*step)(int) = nullptr;
	int (*tick)() = nullptr;
	int (*over)() = nullptr;
	int (*snapshot)() = nullptr;
	int (*lanes)(TdfLane*) = nullptr;
	int (*enemies)(TdfEnemy*, int) = nullptr;
	int (*towers)(TdfTower*, int) = nullptr;
	int (*projectiles)(TdfProjectile*, int) = nullptr;
	int (*territory)(unsigned char*, int) = nullptr;
	int (*events)(TdfEvent*, int) = nullptr;
	int (*fingerprint)(char*, int) = nullptr;
	int (*signature)(char*, int) = nullptr;

	FString LastError() const;
	FString Fingerprint() const;

private:
	void* Handle = nullptr;
};
