#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if UNITY_EDITOR

using UnityEditor;
using System.Linq;

[System.Serializable]
public class WwiseSettings
{
	public const string Filename = "WwiseSettings.xml";

	public static string Path
	{
		get { return System.IO.Path.Combine(UnityEngine.Application.dataPath, Filename); }
	}

	public static bool Exists { get { return System.IO.File.Exists(Path); } }

	public bool CopySoundBanksAsPreBuildStep = true;
	public bool GenerateSoundBanksAsPreBuildStep = false;
	public string SoundbankPath;
	public string GeneratedSoundbanksPath;

	public bool CreatedPicker = false;
	public bool CreateWwiseGlobal = true;
	public bool CreateWwiseListener = true;
	public bool ShowMissingRigidBodyWarning = true;
	public bool ShowSpatialAudioWarningMsg = true;
	public string WwiseInstallationPathMac;
	public string WwiseInstallationPathWindows;
	public string WwiseProjectPath;

	public bool UseWaapi = true;
	public string WaapiPort = "8080";
	public string WaapiIP = "127.0.0.1";

	[System.Xml.Serialization.XmlIgnore]
	public string WwiseInstallationPath
	{
#if UNITY_EDITOR_OSX
		get { return WwiseInstallationPathMac; }
		set { WwiseInstallationPathMac = value; }
#else
		get { return WwiseInstallationPathWindows; }
		set { WwiseInstallationPathWindows = value; }
#endif
	}

	internal static WwiseSettings LoadSettings()
	{
		var settings = new WwiseSettings();

		try
		{
			var path = Path;
			if (System.IO.File.Exists(path))
			{
				var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(WwiseSettings));
				using (var xmlFileStream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					settings = xmlSerializer.Deserialize(xmlFileStream) as WwiseSettings;
			}
			else
			{
				var projectDir = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
				var foundWwiseProjects = System.IO.Directory.GetFiles(projectDir, "*.wproj", System.IO.SearchOption.AllDirectories);
				if (foundWwiseProjects.Length > 0)
					settings.WwiseProjectPath = AkUtilities.MakeRelativePath(UnityEngine.Application.dataPath, foundWwiseProjects[0]);
				else
					settings.WwiseProjectPath = string.Empty;

				settings.SoundbankPath = AkBasePathGetter.DefaultBasePath;
			}
		}
		catch
		{
		}

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
		if (string.IsNullOrEmpty(settings.GeneratedSoundbanksPath))
		{
			var baseDir = "GeneratedSoundBanks";
			var platformSoundBankPaths = AkUtilities.GetAllBankPaths(AkUtilities.GetFullPath(UnityEngine.Application.dataPath, settings.WwiseProjectPath));
			if (platformSoundBankPaths.Count > 0)
			{
				if (platformSoundBankPaths.ContainsKey(AkBasePathGetter.GetPlatformName()))
				{
					baseDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(platformSoundBankPaths[AkBasePathGetter.GetPlatformName()]));
				}
				else
				{
					baseDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(platformSoundBankPaths.Values.First()));
				}
			}
			var generatedSoundbanksDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(settings.WwiseProjectPath), baseDir);
			settings.GeneratedSoundbanksPath = generatedSoundbanksDir;
		}
		settings.CheckGeneratedBanksPath();
#endif
		return settings;
	}

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
	public void CheckGeneratedBanksPath()
	{
			var fullGeneratedSoundbanksPath = AkUtilities.GetFullPath(UnityEngine.Application.dataPath, GeneratedSoundbanksPath);
			var appDataPath = UnityEngine.Application.dataPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

			if (!fullGeneratedSoundbanksPath.Contains(appDataPath))
			{
				UnityEngine.Debug.LogWarning("GeneratedSoundbanksPath is currently set to a path outside of the Assets folder. Generated SoundBanks will not be properly imported for Addressables. Please change this in Project Settings > Wwise Editor.");
			}
	}
#endif


	public void SaveSettings()
	{
		try
		{
			var xmlDoc = new System.Xml.XmlDocument();
			var xmlSerializer = new System.Xml.Serialization.XmlSerializer(GetType());
			using (var xmlStream = new System.IO.MemoryStream())
			{
				var streamWriter = new System.IO.StreamWriter(xmlStream, System.Text.Encoding.UTF8);
				xmlSerializer.Serialize(streamWriter, this);
				xmlStream.Position = 0;
				xmlDoc.Load(xmlStream);
				xmlDoc.Save(Path);
			}
		}
		catch
		{
			UnityEngine.Debug.LogErrorFormat("WwiseUnity: Unable to save settings to file <{0}>. Please ensure that this file path can be written to.", Path);
		}
	}
}

public class AkWwiseEditorSettings
{
	private static WwiseSettings s_Instance;

	public static WwiseSettings Instance
	{
		get
		{
			if (s_Instance == null)
				s_Instance = WwiseSettings.LoadSettings();
			return s_Instance;
		}
	}

	public static void Reload()
	{
		s_Instance = WwiseSettings.LoadSettings();
	}

	public static string WwiseProjectAbsolutePath
	{
		get { return AkUtilities.GetFullPath(UnityEngine.Application.dataPath, Instance.WwiseProjectPath); }
	}

	public static string WwiseScriptableObjectRelativePath
	{
		get { return System.IO.Path.Combine(System.IO.Path.Combine("Assets", "Wwise"), "ScriptableObjects"); }
	}

	#region GUI
#if UNITY_2018_3_OR_NEWER
	class SettingsProvider : UnityEditor.SettingsProvider
#else
	class EditorWindow : UnityEditor.EditorWindow
#endif
	{
		class Styles
		{
			public static string WwiseProject = "Wwise Project";
			public static UnityEngine.GUIContent WwiseProjectPath = new UnityEngine.GUIContent("Wwise Project Path*", "Location of the Wwise project associated with this game. It is recommended to put it in the Unity Project root folder, outside the Assets folder.");

			public static string WwiseApplicationPath = "Wwise Application Path";
			public static UnityEngine.GUIContent WwiseInstallationPath = new UnityEngine.GUIContent("Wwise Application Path", "Location of the Wwise application. This is required to generate the SoundBanks in Unity.");

			public static string AssetManagement = "Asset Management";
			public static UnityEngine.GUIContent SoundbankPath = new UnityEngine.GUIContent("SoundBanks Path*", "Location of the SoundBanks relative to (and within) the StreamingAssets folder.");
			public static UnityEngine.GUIContent CopySoundBanksAsPreBuildStep = new UnityEngine.GUIContent("Copy SoundBanks at pre-Build step", "Copies the SoundBanks in the appropriate location for building and deployment. It is recommended to leave this box checked.");
			public static UnityEngine.GUIContent GenerateSoundBanksAsPreBuildStep = new UnityEngine.GUIContent("Generate SoundBanks at pre-Build step", "Generates the SoundBanks before copying them during pre-Build step. It is recommended to leave this box unchecked if SoundBanks are generated on a specific build machine.");

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
			public static UnityEngine.GUIContent GeneratedSoundbankPath = new UnityEngine.GUIContent("Generated SoundBanks Path*", "Destination folder for generated SoundBanks. Changing this will update your wwise project settings");
#endif

			public static string GlobalSettings = "Global Settings";
			public static UnityEngine.GUIContent CreateWwiseGlobal = new UnityEngine.GUIContent("Create WwiseGlobal GameObject", "The WwiseGlobal object is a GameObject that contains the Initializing and Terminating scripts for the Wwise Sound Engine. In the Editor workflow, it is added to every scene, so that it can be properly previewed in the Editor. In the game, only one instance is created, in the first scene, and it is persisted throughout the game. It is recommended to leave this box checked.");
			public static UnityEngine.GUIContent CreateWwiseListener = new UnityEngine.GUIContent("Add Listener to Main Camera", "In order for positioning to work, the AkAudioListener script needs to be attached to the main camera in every scene. If you wish for your listener to be attached to another GameObject, uncheck this box");

			public static string InEditorWarnings = "In Editor Warnings";
			public static UnityEngine.GUIContent ShowMissingRigidBodyWarning = new UnityEngine.GUIContent("Show warning for missing RigidBody", "Interactions between AkGameObj and AkEnvironment or AkRoom require a Rigidbody component on the object or the environment/room. It is recommended to leave this box checked.");
			public static UnityEngine.GUIContent ShowSpatialAudioWarningMsg = new UnityEngine.GUIContent("Show warning for missing Collider", "Interactions between AkRoomAwareObject and AkRoom require a Collider component on the object. It is recommended to leave this box checked.");

			public static string WaapiSection = "Wwise Authoring API (WAAPI)";
			public static UnityEngine.GUIContent UseWaapi = new UnityEngine.GUIContent("Connect to Wwise");
			public static UnityEngine.GUIContent WaapiIP = new UnityEngine.GUIContent("WAAPI IP address");
			public static UnityEngine.GUIContent WaapiPort = new UnityEngine.GUIContent("WAAPI port");

			public static string MandatorySettings = "* Mandatory settings";

			private static UnityEngine.GUIStyle version;
			public static UnityEngine.GUIStyle Version
			{
				get
				{
					if (version != null)
						return version;

					version = new UnityEngine.GUIStyle(UnityEditor.EditorStyles.whiteLargeLabel);
					if (!UnityEngine.Application.HasProLicense())
					{
						version.active.textColor =
							version.focused.textColor =
							version.hover.textColor =
							version.normal.textColor = UnityEngine.Color.black;
					}
					return version;
				}
			}

			private static UnityEngine.GUIStyle textField;
			public static UnityEngine.GUIStyle TextField
			{
				get
				{
					if (textField == null)
						textField = new UnityEngine.GUIStyle("textfield");
					return textField;
				}
			}
		}

		private static bool Ellipsis()
		{
			return UnityEngine.GUILayout.Button("...", UnityEngine.GUILayout.Width(30));
		}

#if UNITY_2018_3_OR_NEWER
		private SettingsProvider(string path) : base(path, UnityEditor.SettingsScope.Project) { }

		[UnityEditor.SettingsProvider]
		public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
		{
			return new SettingsProvider("Project/Wwise Editor") { keywords = GetSearchKeywordsFromGUIContentProperties<Styles>() };
		}

		public override void OnGUI(string searchContext)
#else
		[UnityEditor.MenuItem("Edit/Wwise Settings...", false, (int)AkWwiseWindowOrder.WwiseSettings)]
		public static void Init()
		{
			// Get existing open window or if none, make a new one:
			var window = GetWindow(typeof(EditorWindow));
			window.position = new UnityEngine.Rect(100, 100, 850, 360);
			window.titleContent = new UnityEngine.GUIContent("Wwise Settings");
		}

		private void OnGUI()
#endif
		{
			bool changed = false;

			var labelWidth = UnityEditor.EditorGUIUtility.labelWidth;
			UnityEditor.EditorGUIUtility.labelWidth += 100;

			var settings = Instance;

			UnityEngine.GUILayout.Label(string.Format("Wwise v{0} Settings.", AkSoundEngine.WwiseVersion), Styles.Version);
			UnityEngine.GUILayout.Label(Styles.WwiseProject, UnityEditor.EditorStyles.boldLabel);

			using (new UnityEngine.GUILayout.HorizontalScope("box"))
			{
				UnityEditor.EditorGUILayout.PrefixLabel(Styles.WwiseProjectPath);
				UnityEditor.EditorGUILayout.SelectableLabel(settings.WwiseProjectPath, Styles.TextField, UnityEngine.GUILayout.Height(17));

				if (Ellipsis())
				{
					var OpenInPath = System.IO.Path.GetDirectoryName(AkUtilities.GetFullPath(UnityEngine.Application.dataPath, settings.WwiseProjectPath));
					var WwiseProjectPathNew = UnityEditor.EditorUtility.OpenFilePanel("Select your Wwise Project", OpenInPath, "wproj");
					if (WwiseProjectPathNew.Length != 0)
					{
						if (WwiseProjectPathNew.EndsWith(".wproj") == false)
						{
							UnityEditor.EditorUtility.DisplayDialog("Error", "Please select a valid .wproj file", "Ok");
						}
						else
						{
							settings.WwiseProjectPath = AkUtilities.MakeRelativePath(UnityEngine.Application.dataPath, WwiseProjectPathNew);
							changed = true;
						}
					}
				}
			}

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
			UnityEngine.GUILayout.Label(Styles.WwiseApplicationPath, UnityEditor.EditorStyles.boldLabel);

			using (new UnityEngine.GUILayout.HorizontalScope("box"))
			{
				UnityEditor.EditorGUILayout.PrefixLabel(Styles.WwiseInstallationPath);
				UnityEditor.EditorGUILayout.SelectableLabel(settings.WwiseInstallationPath, Styles.TextField, UnityEngine.GUILayout.Height(17));

				if (Ellipsis())
				{
#if UNITY_EDITOR_OSX
					var path = UnityEditor.EditorUtility.OpenFilePanel("Select your Wwise application.", "/Applications/", "");
#else
					var path = UnityEditor.EditorUtility.OpenFolderPanel("Select your Wwise application.", System.Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "");
#endif
					if (path.Length != 0)
					{
						settings.WwiseInstallationPath = System.IO.Path.GetFullPath(path);
						changed = true;
					}
				}
			}

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
			UnityEngine.GUILayout.Label(Styles.AssetManagement, UnityEditor.EditorStyles.boldLabel);


#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
			using (new UnityEngine.GUILayout.VerticalScope("box"))
			{
				using (new UnityEngine.GUILayout.HorizontalScope())
				{
					UnityEditor.EditorGUILayout.PrefixLabel(Styles.GeneratedSoundbankPath);
					UnityEditor.EditorGUILayout.SelectableLabel(settings.GeneratedSoundbanksPath, Styles.TextField, UnityEngine.GUILayout.Height(17));

					if (Ellipsis())
					{
						var FullPath = AkUtilities.GetFullPath(UnityEngine.Application.dataPath, settings.GeneratedSoundbanksPath);
						var OpenInPath = System.IO.Path.GetDirectoryName(FullPath);
						var path = UnityEditor.EditorUtility.OpenFolderPanel("Select your generated SoundBanks destination folder", OpenInPath, FullPath.Substring(OpenInPath.Length + 1));
						if (path.Length != 0)
						{
							bool dirsEmpty = (System.IO.Directory.GetDirectories(path).Length == 0) && (System.IO.Directory.GetFiles(path).Length == 0);
							if (!dirsEmpty)
							{
								UnityEditor.EditorUtility.DisplayDialog("Error", "The SoundBanks destination folder should be empty", "Ok");
							}
							else if (!path.Contains(UnityEngine.Application.dataPath))
							{
								UnityEditor.EditorUtility.DisplayDialog("Error", "The SoundBanks destination folder must be located within the Unity project 'Assets' folder.", "Ok");
							}
							else if (path == UnityEngine.Application.dataPath)
							{
								UnityEditor.EditorUtility.DisplayDialog("Error", "The SoundBanks destination folder cannot be the 'Assets' folder.", "Ok");
							}
							else
							{
								var newPath = AkUtilities.MakeRelativePath(UnityEngine.Application.dataPath, path);
								var previousPath = settings.GeneratedSoundbanksPath;
								if (previousPath != newPath)
								{
									settings.GeneratedSoundbanksPath = newPath;
									var projectPath = AkUtilities.GetFullPath(UnityEngine.Application.dataPath, settings.WwiseProjectPath);
									var relPath = AkUtilities.MakeRelativePath(System.IO.Path.GetDirectoryName(projectPath), path);
									AkUtilities.SetSoundbanksDestinationFoldersInWproj(projectPath, relPath);

									var fullPreviousPath = AkUtilities.GetFullPath(UnityEngine.Application.dataPath, previousPath);
									var appDataPath = UnityEngine.Application.dataPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

									if (!string.IsNullOrEmpty(previousPath) && System.IO.Directory.Exists(fullPreviousPath)) 
									{
										UnityEditor.AssetDatabase.Refresh();
										if (fullPreviousPath.Contains(appDataPath))
										{
											var destination = System.IO.Path.Combine("Assets", newPath);
											AkUtilities.MoveAssetsFromDirectory(fullPreviousPath, destination, true);
										}
										else
										{
											AkUtilities.DirectoryCopy(fullPreviousPath, path, true);
										}
										UnityEditor.AssetDatabase.Refresh();
									}
									changed = true;
								}
							}
						}
					}
				}
			}
#else

			using (new UnityEngine.GUILayout.VerticalScope("box"))
			{
				using (new UnityEngine.GUILayout.HorizontalScope())
				{
					UnityEditor.EditorGUILayout.PrefixLabel(Styles.SoundbankPath);
					UnityEditor.EditorGUILayout.SelectableLabel(settings.SoundbankPath, Styles.TextField, UnityEngine.GUILayout.Height(17));

					if (Ellipsis())
					{
						var FullPath = AkUtilities.GetFullPath(UnityEngine.Application.streamingAssetsPath, settings.SoundbankPath);
						var OpenInPath = System.IO.Path.GetDirectoryName(FullPath);
						var path = UnityEditor.EditorUtility.OpenFolderPanel("Select your SoundBanks destination folder", OpenInPath, FullPath.Substring(OpenInPath.Length + 1));
						if (path.Length != 0)
						{
							var stremingAssetsIndex = UnityEngine.Application.dataPath.Split('/').Length;
							var folders = path.Split('/');

							if (folders.Length - 1 < stremingAssetsIndex || !string.Equals(folders[stremingAssetsIndex], "StreamingAssets", System.StringComparison.OrdinalIgnoreCase))
							{
								UnityEditor.EditorUtility.DisplayDialog("Error", "The SoundBank destination folder must be located within the Unity project 'StreamingAssets' folder.", "Ok");
							}
							else
							{
								var previousPath = settings.SoundbankPath;
								var newPath = AkUtilities.MakeRelativePath(UnityEngine.Application.streamingAssetsPath, path);

								if (previousPath != newPath)
								{
									settings.SoundbankPath = newPath;
									changed = true;
								}
							}
						}
					}
				}

				UnityEditor.EditorGUI.BeginChangeCheck();
				settings.CopySoundBanksAsPreBuildStep = UnityEditor.EditorGUILayout.Toggle(Styles.CopySoundBanksAsPreBuildStep, settings.CopySoundBanksAsPreBuildStep);
				UnityEngine.GUI.enabled = settings.CopySoundBanksAsPreBuildStep;
				settings.GenerateSoundBanksAsPreBuildStep = UnityEditor.EditorGUILayout.Toggle(Styles.GenerateSoundBanksAsPreBuildStep, settings.GenerateSoundBanksAsPreBuildStep);
				UnityEngine.GUI.enabled = true;
				if (UnityEditor.EditorGUI.EndChangeCheck())
					changed = true;
			}
#endif

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
			UnityEngine.GUILayout.Label(Styles.GlobalSettings, UnityEditor.EditorStyles.boldLabel);

			UnityEditor.EditorGUI.BeginChangeCheck();
			using (new UnityEngine.GUILayout.VerticalScope("box"))
			{
				settings.CreateWwiseGlobal = UnityEditor.EditorGUILayout.Toggle(Styles.CreateWwiseGlobal, settings.CreateWwiseGlobal);
				settings.CreateWwiseListener = UnityEditor.EditorGUILayout.Toggle(Styles.CreateWwiseListener, settings.CreateWwiseListener);
			}

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
			UnityEngine.GUILayout.Label(Styles.InEditorWarnings, UnityEditor.EditorStyles.boldLabel);

			using (new UnityEngine.GUILayout.VerticalScope("box"))
			{
				settings.ShowMissingRigidBodyWarning = UnityEditor.EditorGUILayout.Toggle(Styles.ShowMissingRigidBodyWarning, settings.ShowMissingRigidBodyWarning);
				settings.ShowSpatialAudioWarningMsg = UnityEditor.EditorGUILayout.Toggle(Styles.ShowSpatialAudioWarningMsg, settings.ShowSpatialAudioWarningMsg);
			}

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
			UnityEngine.GUILayout.Label(Styles.WaapiSection, UnityEditor.EditorStyles.boldLabel);
			using (new UnityEngine.GUILayout.VerticalScope("box"))
			{
				settings.UseWaapi = UnityEditor.EditorGUILayout.Toggle(Styles.UseWaapi, settings.UseWaapi);
				settings.WaapiPort = UnityEditor.EditorGUILayout.TextField(Styles.WaapiPort, settings.WaapiPort);
				settings.WaapiIP = UnityEditor.EditorGUILayout.TextField(Styles.WaapiIP, settings.WaapiIP);
			}

			if (UnityEditor.EditorGUI.EndChangeCheck())
				changed = true;

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
			UnityEngine.GUILayout.Label(Styles.MandatorySettings);

			UnityEditor.EditorGUIUtility.labelWidth = labelWidth;

			if (changed)
				settings.SaveSettings();
		}
		#endregion
	}
}
#endif // UNITY_EDITOR
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
