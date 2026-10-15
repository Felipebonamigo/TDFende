#include "TdfBuildStamp.h"
#include "HAL/PlatformProcess.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"

FString TdfBuildStamp()
{
	static FString Cached;
	if (!Cached.IsEmpty()) return Cached;
	// jogo empacotado: o Empacotar.ps1 grava o selo em <projeto>\Selo.txt
	FString FromFile;
	if (FFileHelper::LoadFileToString(FromFile, *FPaths::Combine(FPaths::ProjectDir(), TEXT("Selo.txt"))) && !FromFile.TrimStartAndEnd().IsEmpty())
		return Cached = FromFile.TrimStartAndEnd();
#if WITH_EDITOR
	// pelo editor (-game): pergunta ao git do repositório (o projeto fica em <repo>\Unreal\TDFende)
	int32 Code = -1;
	FString Out, Err;
	const FString Repo = FPaths::ConvertRelativePathToFull(FPaths::Combine(FPaths::ProjectDir(), TEXT(".."), TEXT("..")));
	if (FPlatformProcess::ExecProcess(TEXT("git"), *FString::Printf(TEXT("-C \"%s\" describe --always --dirty"), *Repo), &Code, &Out, &Err) && Code == 0
		&& !Out.TrimStartAndEnd().IsEmpty())
		return Cached = Out.TrimStartAndEnd();
#endif
	return Cached = TEXT("sem selo");
}
