using System.IO;
using UnrealBuildTool;

public class TDFende : ModuleRules
{
	public TDFende(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "InputCore", "AssetRegistry", "UMG", "Slate", "SlateCore", "RenderCore", "RHI" });

		// O cabeçalho C da Sim é UM só, em Tools/SimBridge/include (raiz do repositório = 4 níveis acima deste módulo).
		string RepoRoot = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "..", ".."));
		PrivateIncludePaths.Add(Path.Combine(RepoRoot, "Tools", "SimBridge", "include"));

		// A DLL nativa é carregada em tempo de execução (FPlatformProcess::GetDllHandle) de Binaries/Win64/tdfende_sim.dll,
		// copiada por Tools/SimBridge/PublicarParaUnreal.ps1. No pacote final entra como dependência de execução.
		RuntimeDependencies.Add("$(ProjectDir)/Binaries/Win64/tdfende_sim.dll");
	}
}
