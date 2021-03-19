#if UNITY_EDITOR
using System.Collections.Generic;

public partial class AkBuildPreprocessor
{
	/// <summary>
	///     User hook called to retrieve the custom platform name used to determine the base path. Do not modify platformName
	///     to use default platform names.
	/// </summary>
	/// <param name="platformName">The custom platform name.</param>
	static partial void GetCustomPlatformName(ref string platformName, UnityEditor.BuildTarget target);

	public static Dictionary<UnityEditor.BuildTarget, string> BuildTargetToPlatformName = new Dictionary<UnityEditor.BuildTarget, string>();

	private static string GetPlatformName(UnityEditor.BuildTarget target)
	{
		var platformSubDir = string.Empty;
		GetCustomPlatformName(ref platformSubDir, target);
		if (!string.IsNullOrEmpty(platformSubDir))
			return platformSubDir;

		if (BuildTargetToPlatformName.ContainsKey(target))
		{
			return BuildTargetToPlatformName[target];
		}
		return target.ToString();
	}
}

#if UNITY_2018_1_OR_NEWER
public partial class AkBuildPreprocessor : UnityEditor.Build.IPreprocessBuildWithReport, UnityEditor.Build.IPostprocessBuildWithReport
#else
public partial class AkBuildPreprocessor : UnityEditor.Build.IPreprocessBuild, UnityEditor.Build.IPostprocessBuild
#endif
{
	public int callbackOrder
	{
		get { return 0; }
	}

	private string destinationSoundBankFolder = string.Empty;

	public static bool CopySoundbanks(bool generate, string platformName, ref string destinationFolder)
	{
		if (string.IsNullOrEmpty(platformName))
		{
			UnityEngine.Debug.LogErrorFormat("WwiseUnity: Could not determine platform name for <{0}> platform", platformName);
			return false;
		}

		if (generate)
		{
			var platforms = new System.Collections.Generic.List<string> { platformName };
			AkUtilities.GenerateSoundbanks(platforms);
		}

		string sourceFolder;
		if (!AkBasePathGetter.GetSoundBankPaths(platformName, out sourceFolder, out destinationFolder))
			return false;

		if (!AkUtilities.DirectoryCopy(sourceFolder, destinationFolder, true))
		{
			destinationFolder = null;
			UnityEngine.Debug.LogErrorFormat("WwiseUnity: Could not copy SoundBank folder for <{0}> platform", platformName);
			return false;
		}

		UnityEngine.Debug.LogFormat("WwiseUnity: Copied SoundBank folder to streaming assets folder <{0}> for <{1}> platform build", destinationFolder, platformName);
		return true;
	}

	public static void DeleteSoundbanks(string destinationFolder)
	{
		if (string.IsNullOrEmpty(destinationFolder))
			return;

		System.IO.Directory.Delete(destinationFolder, true);
		UnityEngine.Debug.LogFormat("WwiseUnity: Deleting streaming assets folder <{0}>", destinationFolder);
	}

	public void OnPreprocessBuildInternal(UnityEditor.BuildTarget target, string path)
	{
		if (AkWwiseEditorSettings.Instance.CopySoundBanksAsPreBuildStep)
		{
			var platformName = GetPlatformName(target);
			if (!CopySoundbanks(AkWwiseEditorSettings.Instance.GenerateSoundBanksAsPreBuildStep, platformName, ref destinationSoundBankFolder))
			{
				UnityEngine.Debug.LogErrorFormat("WwiseUnity: SoundBank folder has not been copied for <{0}> target at <{1}>. This will likely result in a build without sound!!!", target, path);
			}
		}

		// @todo sjl - only update for target platform
		AkPluginActivator.Update(true);
		AkPluginActivator.ActivatePluginsForDeployment(target, true);
	}

	public void OnPostprocessBuildInternal(UnityEditor.BuildTarget target, string path)
	{
		AkPluginActivator.ActivatePluginsForDeployment(target, false);
		DeleteSoundbanks(destinationSoundBankFolder);
		destinationSoundBankFolder = string.Empty;
	}

#if UNITY_2018_1_OR_NEWER
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        OnPreprocessBuildInternal(report.summary.platform, report.summary.outputPath);
    }

    public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        OnPostprocessBuildInternal(report.summary.platform, report.summary.outputPath);
    }
#else
    public void OnPreprocessBuild(UnityEditor.BuildTarget target, string path)
	{
		OnPreprocessBuildInternal(target, path);
	}

	public void OnPostprocessBuild(UnityEditor.BuildTarget target, string path)
	{
		OnPostprocessBuildInternal(target, path);
	}
#endif
}
#endif // #if UNITY_EDITOR