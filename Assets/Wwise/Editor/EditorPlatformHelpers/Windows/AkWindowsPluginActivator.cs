#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
public class AkWindowsPluginActivator
{
	static AkWindowsPluginActivator()
	{
		AkPluginActivator.BuildTargetToPlatformName.Add(UnityEditor.BuildTarget.StandaloneWindows, "Windows");
		AkPluginActivator.BuildTargetToPlatformName.Add(UnityEditor.BuildTarget.StandaloneWindows64, "Windows");
		AkBuildPreprocessor.BuildTargetToPlatformName.Add(UnityEditor.BuildTarget.StandaloneWindows, "Windows");
		AkBuildPreprocessor.BuildTargetToPlatformName.Add(UnityEditor.BuildTarget.StandaloneWindows64, "Windows");
	}
}
#endif