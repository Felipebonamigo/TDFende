using UnrealBuildTool;

public class TDFendeEditorTarget : TargetRules
{
	public TDFendeEditorTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.Latest;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.Add("TDFende");
	}
}
