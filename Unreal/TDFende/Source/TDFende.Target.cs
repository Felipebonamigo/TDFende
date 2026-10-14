using UnrealBuildTool;

public class TDFendeTarget : TargetRules
{
	public TDFendeTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.Latest;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.Add("TDFende");
	}
}
