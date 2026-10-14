#pragma once

#include "CoreMinimal.h"

// O cabeçalho C declara as funções como dllimport; aqui a DLL é carregada à mão e elas vêm por ponteiro (nunca são chamadas
// pelo nome), então só interessam os tipos e as constantes. O aviso de macro redefinida é esperado e inofensivo.
#pragma warning(push)
#pragma warning(disable: 4005)
#include "tdfende_sim.h"
#pragma warning(pop)

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
