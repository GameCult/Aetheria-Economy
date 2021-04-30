#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
public class AkMacPluginActivator
{
	static AkMacPluginActivator()
	{
		AkPluginActivator.BuildTargetToPlatformName.Add(UnityEditor.BuildTarget.StandaloneOSX, "Mac");
		AkBuildPreprocessor.BuildTargetToPlatformName.Add(UnityEditor.BuildTarget.StandaloneOSX, "Mac");
	}
}
#endif