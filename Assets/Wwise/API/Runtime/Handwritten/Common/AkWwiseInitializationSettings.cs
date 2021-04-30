public class AkWwiseInitializationSettings : AkCommonPlatformSettings
{
	[UnityEngine.HideInInspector]
	public System.Collections.Generic.List<string> PlatformSettingsNameList
		= new System.Collections.Generic.List<string>();

	[UnityEngine.HideInInspector]
	public System.Collections.Generic.List<PlatformSettings> PlatformSettingsList
		= new System.Collections.Generic.List<PlatformSettings>();

	[UnityEngine.HideInInspector]
	public System.Collections.Generic.List<string> InvalidReferencePlatforms
		= new System.Collections.Generic.List<string>();

	public bool IsValid
	{
		get { return PlatformSettingsNameList.Count == PlatformSettingsList.Count; }
	}

	public int Count
	{
		get { return PlatformSettingsList.Count; }
	}

	[UnityEngine.HideInInspector]
	public AkCommonUserSettings UserSettings;
	[UnityEngine.HideInInspector]
	public AkCommonAdvancedSettings AdvancedSettings;
	[UnityEngine.HideInInspector]
	public AkCommonCommSettings CommsSettings;

	protected override AkCommonUserSettings GetUserSettings()
	{
		return UserSettings;
	}

	protected override AkCommonAdvancedSettings GetAdvancedSettings()
	{
		return AdvancedSettings;
	}

	protected override AkCommonCommSettings GetCommsSettings()
	{
		return CommsSettings;
	}

	private static readonly string[] AllGlobalValues = new[]
	{
		"UserSettings.m_BasePath",
		"UserSettings.m_StartupLanguage",
		"UserSettings.m_EngineLogging",
		"UserSettings.m_MaximumNumberOfPositioningPaths",
		"UserSettings.m_MemoryCutoffThreshold",
		"UserSettings.m_CommandQueueSize",
		"UserSettings.m_SamplesPerFrame",
		"UserSettings.m_MainOutputSettings.m_AudioDeviceShareset",
		"UserSettings.m_MainOutputSettings.m_DeviceID",
		"UserSettings.m_MainOutputSettings.m_PanningRule",
		"UserSettings.m_MainOutputSettings.m_ChannelConfig.m_ChannelConfigType",
		"UserSettings.m_MainOutputSettings.m_ChannelConfig.m_ChannelMask",
		"UserSettings.m_MainOutputSettings.m_ChannelConfig.m_NumberOfChannels",
		"UserSettings.m_StreamingLookAheadRatio",
		"UserSettings.m_SampleRate",
		"UserSettings.m_NumberOfRefillsInVoice",
		"UserSettings.m_SpatialAudioSettings.m_MaxSoundPropagationDepth",
		"UserSettings.m_SpatialAudioSettings.m_MovementThreshold",
		"UserSettings.m_SpatialAudioSettings.m_NumberOfPrimaryRays",
		"UserSettings.m_SpatialAudioSettings.m_MaxReflectionOrder",
		"UserSettings.m_SpatialAudioSettings.m_MaxPathLength",
		"UserSettings.m_SpatialAudioSettings.m_CPULimitPercentage",
		"UserSettings.m_SpatialAudioSettings.m_EnableDiffractionOnReflections",
		"UserSettings.m_SpatialAudioSettings.m_EnableGeometricDiffractionAndTransmission",
		"UserSettings.m_SpatialAudioSettings.m_CalcEmitterVirtualPosition",
		"UserSettings.m_SpatialAudioSettings.m_UseObstruction",
		"UserSettings.m_SpatialAudioSettings.m_UseOcclusion",
		"CommsSettings.m_PoolSize",
		"CommsSettings.m_DiscoveryBroadcastPort",
		"CommsSettings.m_CommandPort",
		"CommsSettings.m_NotificationPort",
		"CommsSettings.m_InitializeSystemComms",
		"CommsSettings.m_NetworkName",
		"AdvancedSettings.m_IOMemorySize",
		"AdvancedSettings.m_TargetAutoStreamBufferLengthMs",
		"AdvancedSettings.m_UseStreamCache",
		"AdvancedSettings.m_MaximumPinnedBytesInCache",
		"AdvancedSettings.m_EnableGameSyncPreparation",
		"AdvancedSettings.m_ContinuousPlaybackLookAhead",
		"AdvancedSettings.m_MonitorQueuePoolSize",
		"AdvancedSettings.m_MaximumHardwareTimeoutMs",
		"AdvancedSettings.m_SpatialAudioSettings.m_DiffractionShadowAttenuationFactor",
		"AdvancedSettings.m_SpatialAudioSettings.m_DiffractionShadowDegrees",
		"AdvancedSettings.m_RenderDuringFocusLoss",
		"AdvancedSettings.m_UseAsyncOpen",
		"AdvancedSettings.m_SoundBankPersistentDataPath",
		"AdvancedSettings.m_DebugOutOfRangeCheckEnabled",
		"AdvancedSettings.m_DebugOutOfRangeLimit"
	};

	public abstract class PlatformSettings : AkCommonPlatformSettings
	{
		#region Ignore property list management
		[UnityEngine.SerializeField]
		[UnityEngine.HideInInspector]
		private System.Collections.Generic.List<string> IgnorePropertyNameList =
			new System.Collections.Generic.List<string>();

		public void IgnorePropertyValue(string propertyPath)
		{
			if (IsPropertyIgnored(propertyPath))
				return;

			IgnorePropertyNameList.Add(propertyPath);
			SetUseGlobalPropertyValue(propertyPath, false);
		}

		public bool IsPropertyIgnored(string propertyPath)
		{
			return IgnorePropertyNameList.Contains(propertyPath);
		}
		#endregion

		#region Global property list management
		[UnityEngine.SerializeField]
		[UnityEngine.HideInInspector]
		private System.Collections.Generic.List<string> GlobalPropertyNameList =
			new System.Collections.Generic.List<string>();

		protected PlatformSettings()
		{
			SetGlobalPropertyValues(AllGlobalValues);
		}

		public void SetUseGlobalPropertyValue(string propertyPath, bool use)
		{
			if (IsUsingGlobalPropertyValue(propertyPath) == use)
				return;

			if (use)
				GlobalPropertyNameList.Add(propertyPath);
			else
				GlobalPropertyNameList.Remove(propertyPath);

			_GlobalPropertyHashSet = null;
		}

		public void SetGlobalPropertyValues(System.Collections.IEnumerable enumerable)
		{
			foreach (var item in enumerable)
			{
				string propertyPath = item as string;
				if (!IsUsingGlobalPropertyValue(propertyPath))
					GlobalPropertyNameList.Add(propertyPath);
			}
		}

		private bool IsUsingGlobalPropertyValue(string propertyPath)
		{
			return GlobalPropertyNameList.Contains(propertyPath);
		}

		private System.Collections.Generic.HashSet<string> _GlobalPropertyHashSet = null;

		public System.Collections.Generic.HashSet<string> GlobalPropertyHashSet
		{
			get
			{
				if (_GlobalPropertyHashSet == null)
					_GlobalPropertyHashSet = new System.Collections.Generic.HashSet<string>(GlobalPropertyNameList);
				return _GlobalPropertyHashSet;
			}
			set { _GlobalPropertyHashSet = value; }
		}
		#endregion

#if UNITY_EDITOR
		protected static void RegisterPlatformSettingsClass<T>(string platformName) where T : PlatformSettings
		{
			string className = typeof(T).Name;
			string currentClassName;
			if (m_PlatformSettingsClassNames.TryGetValue(platformName, out currentClassName) && currentClassName == className)
			{
				UnityEngine.Debug.LogWarning("WwiseUnity: The class <" + currentClassName + "> is being replaced by <" + className + "> for the reference platform: " + platformName);
				return;
			}

			m_PlatformSettingsClassNames[platformName] = className;
		}

		public static bool IsDistinctPlatform(string platformName)
		{
			return m_PlatformSettingsClassNames.ContainsKey(platformName);
		}

#endif
	}

	public class CommonPlatformSettings : PlatformSettings
	{
		protected override AkCommonUserSettings GetUserSettings()
		{
			return UserSettings;
		}

		protected override AkCommonAdvancedSettings GetAdvancedSettings()
		{
			return AdvancedSettings;
		}

		protected override AkCommonCommSettings GetCommsSettings()
		{
			return CommsSettings;
		}

		[UnityEngine.HideInInspector]
		public AkCommonUserSettings UserSettings;
		[UnityEngine.HideInInspector]
		public AkCommonAdvancedSettings AdvancedSettings;
		[UnityEngine.HideInInspector]
		public AkCommonCommSettings CommsSettings;
	}

	#region Singleton management
	private static AkWwiseInitializationSettings m_Instance = null;
	private static AkBasePlatformSettings m_ActivePlatformSettings = null;

	public static AkWwiseInitializationSettings Instance
	{
		get
		{
			if (m_Instance == null)
			{
#if UNITY_EDITOR
				var name = typeof(AkWwiseInitializationSettings).Name;
				m_Instance = GetOrCreateAsset<AkWwiseInitializationSettings>(name, name);
#else
				m_Instance = CreateInstance<AkWwiseInitializationSettings>();
				UnityEngine.Debug.LogWarning("WwiseUnity: No platform specific settings were created. Default initialization settings will be used.");
#endif
			}

			return m_Instance;
		}
	}

	private static AkBasePlatformSettings GetPlatformSettings(string platformName)
	{
		var instance = Instance;
		if (!instance.IsValid)
			return instance;

		for (var i = 0; i < instance.Count; ++i)
		{
			var platformSettings = instance.PlatformSettingsList[i];
			if (platformSettings && (string.Equals(platformName, instance.PlatformSettingsNameList[i], System.StringComparison.OrdinalIgnoreCase)))
				return platformSettings;
		}

		UnityEngine.Debug.LogWarning("WwiseUnity: Platform specific settings cannot be found for <" + platformName + ">. Using global settings.");
		return instance;
	}

	public static AkBasePlatformSettings ActivePlatformSettings
	{
		get
		{
			if (m_ActivePlatformSettings == null)
				m_ActivePlatformSettings = GetPlatformSettings(AkBasePathGetter.GetPlatformName());

			return m_ActivePlatformSettings;
		}
	}

	private void OnEnable()
	{
		if (m_Instance == null)
			m_Instance = this;
		else if (m_Instance != this)
			UnityEngine.Debug.LogWarning("WwiseUnity: There are multiple AkWwiseInitializationSettings objects instantiated; only one will be used.");
	}
	#endregion

	#region Sound Engine Initialization
	public bool InitializeSoundEngine()
	{
#if UNITY_EDITOR
		Instance.ActiveSettingsHash = GetHashOfActiveSettings();
		Instance.ActiveSettingsHaveChanged = true;
#endif

		UnityEngine.Debug.LogFormat("WwiseUnity: Wwise(R) SDK Version {0}.", AkSoundEngine.WwiseVersion);

		var initResult = AkSoundEngine.Init(ActivePlatformSettings.AkInitializationSettings);
		if (initResult != AKRESULT.AK_Success)
		{
			UnityEngine.Debug.LogError($"WwiseUnity: Failed to initialize the sound engine. Reason: {initResult}");
			AkSoundEngine.Term();
			return false;
		}

		if (AkSoundEngine.InitSpatialAudio(ActivePlatformSettings.AkSpatialAudioInitSettings) != AKRESULT.AK_Success)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Failed to initialize spatial audio.");
		}

		AkSoundEngine.InitCommunication(ActivePlatformSettings.AkCommunicationSettings);
		AkBasePathGetter.EvaluateGamePaths();

		var soundBankBasePath = AkBasePathGetter.SoundBankBasePath;
		if (string.IsNullOrEmpty(soundBankBasePath))
		{
			// this is a nearly impossible situation
			UnityEngine.Debug.LogError("WwiseUnity: Couldn't find SoundBanks base path. Terminating sound engine.");
			AkSoundEngine.Term();
			return false;
		}

		var persistentDataPath = AkBasePathGetter.PersistentDataPath;
		var isBasePathSameAsPersistentPath = soundBankBasePath == persistentDataPath;

#if UNITY_ANDROID
		var canSetBasePath = !isBasePathSameAsPersistentPath;
		var canSetPersistentDataPath = true;
#else
		var canSetBasePath = true;
		var canSetPersistentDataPath = !isBasePathSameAsPersistentPath;
#endif

		if (canSetBasePath && AkSoundEngine.SetBasePath(soundBankBasePath) != AKRESULT.AK_Success)
		{
#if UNITY_EDITOR
			var format = "WwiseUnity: Failed to set SoundBanks base path to <{0}>. Make sure SoundBank path is correctly set under Edit > Project Settings > Wwise > Editor > Asset Management.";
#else
			var format = "WwiseUnity: Failed to set SoundBanks base path to <{0}>. Make sure SoundBank path is correctly set under Edit > Project Settings > Wwise > Initialization.";
#endif

#if !UNITY_ANDROID || UNITY_EDITOR
			// It might be normal for SetBasePath to return AK_PathNotFound on Android. Silence the error log to avoid confusion.
			UnityEngine.Debug.LogErrorFormat(format, soundBankBasePath);
#endif
		}

		if (canSetPersistentDataPath && !string.IsNullOrEmpty(persistentDataPath))
		{
			AkSoundEngine.AddBasePath(persistentDataPath);
		}

		var decodedBankFullPath = AkBasePathGetter.DecodedBankFullPath;
		if (!string.IsNullOrEmpty(decodedBankFullPath))
		{
			// AkSoundEngine.SetDecodedBankPath creates the folders for writing to (if they don't exist)
			AkSoundEngine.SetDecodedBankPath(decodedBankFullPath);

			// Adding decoded bank path last to ensure that it is the first one used when writing decoded banks.
			AkSoundEngine.AddBasePath(decodedBankFullPath);
		}

		AkSoundEngine.SetCurrentLanguage(ActivePlatformSettings.InitialLanguage);

		AkCallbackManager.Init(ActivePlatformSettings.CallbackManagerInitializationSettings);
		UnityEngine.Debug.Log("WwiseUnity: Sound engine initialized successfully.");
		LoadInitBank();
		return true;
	}

	protected virtual void LoadInitBank()
	{
		AkBankManager.LoadInitBank();
	}

	protected virtual void ClearBanks()
	{
		AkSoundEngine.ClearBanks();
	}

	protected virtual void ResetBanks()
	{
		AkBankManager.Reset();
	}


	public bool ResetSoundEngine(bool isPlaying)
	{
		if (isPlaying)
		{
			ClearBanks();
			LoadInitBank();
		}

		AkCallbackManager.Init(ActivePlatformSettings.CallbackManagerInitializationSettings);
		return true;
	}

	public void TerminateSoundEngine()
	{
		if (!AkSoundEngine.IsInitialized())
			return;

		// Stop everything, and make sure the callback buffer is empty. We try emptying as much as possible, and wait 10 ms before retrying.
		// Callbacks can take a long time to be posted after the call to RenderAudio().
		AkSoundEngine.StopAll();
		ClearBanks();
		AkSoundEngine.RenderAudio();

		for (var retry = 0; retry < 5;)
		{
			if (AkCallbackManager.PostCallbacks() == 0)
			{
				SleepForMilliseconds(10);
				++retry;
			}

			SleepForMilliseconds(1);
		}

		AkSoundEngine.Term();

		// Make sure we have no callbacks left after Term. Some might be posted during termination.
		AkCallbackManager.PostCallbacks();

		AkCallbackManager.Term();
		ResetBanks();

#if UNITY_EDITOR
		Instance.ActiveSettingsHash = string.Empty;
		Instance.ActiveSettingsHaveChanged = true;
#endif
	}

	private static void SleepForMilliseconds(double milliseconds)
	{
		using (var tmpEvent = new System.Threading.ManualResetEvent(false))
			tmpEvent.WaitOne(System.TimeSpan.FromMilliseconds(milliseconds));
	}
#endregion

#if UNITY_EDITOR
	public static T GetOrCreateAsset<T>(string className, string fileName) where T : AkCommonPlatformSettings
	{
		var path = System.IO.Path.Combine(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath, fileName + ".asset");
		var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
		if (asset)
			return asset;

		var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name);
		foreach (var assetGuid in guids)
		{
			var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
			asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset)
				return asset;
		}

		asset = CreateInstance(className) as T;
		AkUtilities.CreateFolder(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath);
		UnityEditor.AssetDatabase.CreateAsset(asset, path);
		return asset;
	}

	private static System.Collections.Generic.Dictionary<string, string> m_PlatformSettingsClassNames
		= new System.Collections.Generic.Dictionary<string, string>();

	private const System.Reflection.BindingFlags BindingFlags 
		= System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

	private static string GetHashOfActiveSettingsField(string name, object obj)
	{
		var type = obj.GetType();
		if (type.IsPrimitive || type == typeof(string))
			return name + ": " + obj.ToString() + "\n";

		string ret = string.Empty;
		foreach (var subFieldInfo in type.GetFields(BindingFlags))
		{
			var subObject = subFieldInfo.GetValue(obj);
			var subType = subFieldInfo.FieldType;
			var fields = subType.GetFields(BindingFlags);

			if (fields.Length == 0)
				ret += subFieldInfo.Name + ": " + subObject.ToString() + "\n";
			else
				ret += GetHashOfActiveSettingsField(subFieldInfo.Name, subObject);
		}

		return ret;
	}

	public static string GetHashOfActiveSettings()
	{
		try
		{
			return GetHashOfActiveSettingsField(string.Empty, ActivePlatformSettings);
		}
		catch
		{
			return string.Empty;
		}
	}

	[UnityEngine.HideInInspector]
	private bool ActiveSettingsHaveChanged = true;

	[UnityEngine.HideInInspector]
	private string ActiveSettingsHash;
	public static void UpdatePlatforms()
	{
		if (!AkUtilities.IsWwiseProjectAvailable)
			return;

		var customPlatformSettingsMap = new System.Collections.Generic.Dictionary<string, PlatformSettings>();
		var instance = Instance;
		if (instance.IsValid)
		{
			for (var i = 0; i < instance.Count; ++i)
			{
				var settings = instance.PlatformSettingsList[i];
				var name = instance.PlatformSettingsNameList[i];
				if (settings && !string.IsNullOrEmpty(name))
					customPlatformSettingsMap.Add(name, settings);
			}
		}

		var updated = false;
		var allCustomPlatforms = new System.Collections.Generic.List<string>();
		foreach (var pair in AkUtilities.PlatformMapping)
		{
			var referencePlatform = pair.Key;
			var customPlatformList = pair.Value;

			string className, customClassName;
			if (!m_PlatformSettingsClassNames.TryGetValue(referencePlatform, out className))
			{
				if (!instance.InvalidReferencePlatforms.Contains(referencePlatform))
				{
					instance.InvalidReferencePlatforms.Add(referencePlatform);
					UnityEngine.Debug.LogError("WwiseUnity: A class has not been registered for the reference platform: " + referencePlatform);
				}
				continue;
			}

			foreach (var customWwisePlatform in customPlatformList)
			{
				allCustomPlatforms.Add(customWwisePlatform);
				if (customPlatformSettingsMap.ContainsKey(customWwisePlatform))
					continue;

				PlatformSettings settings;
				if (m_PlatformSettingsClassNames.TryGetValue(customWwisePlatform, out customClassName))
				{
					settings = GetOrCreateAsset<PlatformSettings>(customClassName, customWwisePlatform);
				}
				else
				{
					settings = GetOrCreateAsset<PlatformSettings>(className, customWwisePlatform);
				}
				customPlatformSettingsMap.Add(customWwisePlatform, settings);
				updated = true;
			}
		}

		var customPlatformSettingsToRemoveMap = new System.Collections.Generic.Dictionary<string, PlatformSettings>();
		foreach (var pair in customPlatformSettingsMap)
		{
			var instantiatedCustomPlatform = pair.Key;
			if (!allCustomPlatforms.Contains(instantiatedCustomPlatform))
				customPlatformSettingsToRemoveMap.Add(instantiatedCustomPlatform, pair.Value);
		}

		foreach (var pair in customPlatformSettingsToRemoveMap)
		{
			var instantiatedCustomPlatform = pair.Key;
			customPlatformSettingsMap.Remove(instantiatedCustomPlatform);

			var parentPath = System.IO.Path.Combine("Assets", AkWwiseEditorSettings.WwiseScriptableObjectRelativePath);
			var assetPath = System.IO.Path.Combine(parentPath, instantiatedCustomPlatform + ".asset");
			UnityEditor.AssetDatabase.DeleteAsset(assetPath);
			updated = true;
		}

		if (updated)
		{
			instance.PlatformSettingsNameList.Clear();
			instance.PlatformSettingsList.Clear();

			AkUtilities.RepaintInspector();

			var keys = System.Linq.Enumerable.ToList(customPlatformSettingsMap.Keys);
			keys.Sort();

			foreach (var key in keys)
			{
				instance.PlatformSettingsNameList.Add(key);
				instance.PlatformSettingsList.Add(customPlatformSettingsMap[key]);
			}

			UnityEditor.EditorUtility.SetDirty(instance);
			UnityEditor.AssetDatabase.SaveAssets();
			AkUtilities.RepaintInspector();
		}
	}

#region Custom Editor
#if UNITY_2018_3_OR_NEWER
	class SettingsProvider : UnityEditor.SettingsProvider
#else
	[UnityEditor.MenuItem("Edit/Wwise Initialization Settings..", false, (int)AkWwiseWindowOrder.WwiseInitializationSettings)]
	private static void WwiseInitializationSettingsMenuItem()
	{
		UnityEditor.Selection.activeObject = Instance;
	}

	[UnityEditor.CustomEditor(typeof(AkWwiseInitializationSettings))]
	public class Editor : UnityEditor.Editor
#endif
	{
#if UNITY_2018_3_OR_NEWER
		private SettingsProvider(string path) : base(path, UnityEditor.SettingsScope.Project) { }

		[UnityEditor.SettingsProvider]
		public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
		{
			return new SettingsProvider("Project/Wwise Initialization");
		}

		private UnityEditor.SerializedObject serializedObject;
#endif

		private const string UserSettings = "UserSettings";
		private const string AdvancedSettings = "AdvancedSettings";
		private const string CommsSettings = "CommsSettings";

		private System.Collections.Generic.List<PlatformSettings> PreviousPlatformSettingsList
			= new System.Collections.Generic.List<PlatformSettings>();

		private System.Collections.Generic.List<GlobalSettingsGroupData> GlobalSettingsGroups
			= new System.Collections.Generic.List<GlobalSettingsGroupData>();

		private System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>> GlobalGroupSettingsMap
			= new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>();

		private System.Collections.Generic.List<PlatformSpecificSettingsData> PlatformSpecificSettingsGroups
			= new System.Collections.Generic.List<PlatformSpecificSettingsData>();

#if UNITY_2019_1_OR_NEWER
		public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
#elif UNITY_2018_3_OR_NEWER
		public override void OnActivate(string searchContext, UnityEngine.Experimental.UIElements.VisualElement rootElement)
#else
		public void OnEnable()
#endif
		{
			UpdateRequired = true;

#if UNITY_2018_3_OR_NEWER
			serializedObject = new UnityEditor.SerializedObject(Instance);
#endif

			GlobalGroupSettingsMap.Clear();
			foreach (var settingsGroup in new[] { UserSettings, AdvancedSettings, CommsSettings })
			{
				var property = serializedObject.FindProperty(settingsGroup);
				if (property == null)
					return;

				var type = System.Type.GetType(property.type);
				foreach (var field in type.GetFields())
				{
					var childProperty = property.FindPropertyRelative(field.Name);
					if (childProperty == null)
						continue;

					System.Collections.Generic.HashSet<string> hashSet = null;
					if (!GlobalGroupSettingsMap.TryGetValue(settingsGroup, out hashSet))
					{
						hashSet = new System.Collections.Generic.HashSet<string>();
						GlobalGroupSettingsMap.Add(settingsGroup, hashSet);
					}

					hashSet.Add(childProperty.propertyPath);
				}
			}

			GlobalSettingsGroups.Clear();
			GlobalSettingsGroups.Add(new GlobalSettingsGroupData(UserSettings, serializedObject, "", GlobalGroupSettingsMap[UserSettings]));
			GlobalSettingsGroups.Add(new GlobalSettingsGroupData(AdvancedSettings, serializedObject, "", GlobalGroupSettingsMap[AdvancedSettings]));
			GlobalSettingsGroups.Add(new GlobalSettingsGroupData(CommsSettings, serializedObject, "Wwise Communication Settings", GlobalGroupSettingsMap[CommsSettings]));
		}

#if UNITY_2018_3_OR_NEWER
		public override void OnGUI(string searchContext)
#else
		public override void OnInspectorGUI()
#endif
		{
			if (!Instance.IsValid)
			{
				UnityEditor.EditorGUILayout.HelpBox("Platform names do not correspond with their associated settings data.", UnityEditor.MessageType.Error);
				return;
			}

			var labelWidth = UnityEditor.EditorGUIUtility.labelWidth;
			UnityEditor.EditorGUIUtility.labelWidth += 100;

			UpdatePlatformData();
			DrawHelpBox();

			UnityEditor.EditorGUILayout.Space();

			if (PreviousPlatformSettingsList.Count == 0)
			{
				if (!AkUtilities.IsWwiseProjectAvailable)
				{
					UnityEditor.EditorGUILayout.HelpBox("The Wwise project is not available. Please specify its location within the Wwise Settings.", UnityEditor.MessageType.Warning);
					return;
				}

				UnityEditor.EditorGUILayout.HelpBox("No Wwise platforms have been added. Editing global settings.", UnityEditor.MessageType.Warning);
				UnityEditor.EditorGUILayout.Space();
			}

			UnityEditor.EditorGUI.BeginChangeCheck();

			serializedObject.Update();

			foreach (var setting in GlobalSettingsGroups)
				setting.Draw();

			serializedObject.ApplyModifiedProperties();

			UnityEditor.EditorGUILayout.Space();

			foreach (var setting in PlatformSpecificSettingsGroups)
				setting.Draw();

			if (UnityEditor.EditorGUI.EndChangeCheck())
				Instance.ActiveSettingsHaveChanged = true;

			UnityEditor.EditorGUIUtility.labelWidth = labelWidth;
		}

		private bool UpdateRequired = false;

		private void UpdatePlatformData()
		{
			if (!UpdateRequired)
			{
				var firstNotSecond = System.Linq.Enumerable.Except(PreviousPlatformSettingsList, Instance.PlatformSettingsList);
				var secondNotFirst = System.Linq.Enumerable.Except(Instance.PlatformSettingsList, PreviousPlatformSettingsList);
				var refreshRequired = System.Linq.Enumerable.Any(firstNotSecond) || System.Linq.Enumerable.Any(secondNotFirst);
				if (!refreshRequired)
				{
					foreach (var platformSettings in Instance.PlatformSettingsList)
					{
						if (platformSettings == null)
						{
							refreshRequired = true;
							break;
						}
					}

					if (!refreshRequired)
						return;
				}
			}
			else
			{
				UpdateRequired = false;
			}

			PreviousPlatformSettingsList.Clear();
			PlatformSpecificSettingsGroups.Clear();

			var platformNames = new System.Collections.Generic.HashSet<string>();

			foreach (var setting in GlobalSettingsGroups)
				setting.ClearPlatformData();

			for (var i = 0; i < Instance.Count; ++i)
			{
				var platformSettings = Instance.PlatformSettingsList[i];
				if (!platformSettings)
					continue;

				var platformName = Instance.PlatformSettingsNameList[i];
				if (string.IsNullOrEmpty(platformName))
					continue;

				if (!platformNames.Contains(platformName))
				{
					platformNames.Add(platformName);
					PreviousPlatformSettingsList.Add(platformSettings);

					var platform = new PlatformData
					{
						Settings = platformSettings,
						Name = platformName,
						SerializedObject = new UnityEditor.SerializedObject(platformSettings)
					};

					foreach (var setting in GlobalSettingsGroups)
						setting.SetupPlatform(platform);

					foreach (var settingsGroup in new[] { UserSettings, AdvancedSettings, CommsSettings })
						PlatformSpecificSettingsGroups.Add(new PlatformSpecificSettingsData(platform, settingsGroup, GlobalGroupSettingsMap[settingsGroup]));
				}
			}
		}

		private static void DrawHelpBox()
		{
			if (Instance.ActiveSettingsHaveChanged)
			{
				var hash = GetHashOfActiveSettings();
				Instance.ActiveSettingsHaveChanged = string.IsNullOrEmpty(hash) || hash != Instance.ActiveSettingsHash;
			}

			var helpBoxText = "No changes have been made. Please be advised that changes will take effect once the Editor exits play mode.";
			var messageType = UnityEditor.MessageType.Info;

			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer)
			{
				helpBoxText = "Changes will take effect once the Editor exits play mode.";
			}
			else if (Instance.ActiveSettingsHaveChanged)
			{
				helpBoxText = "Changes have been made and will take effect once the Editor exits play mode.";
				messageType = UnityEditor.MessageType.Warning;
			}

			UnityEditor.EditorGUILayout.HelpBox(helpBoxText, messageType);
		}

		private static System.Collections.Generic.IEnumerable<UnityEditor.SerializedProperty> GetChildren(UnityEditor.SerializedProperty property)
		{
			property = property.Copy();
			var nextElement = property.Copy();
			bool hasNextElement = nextElement.Next(false);
			if (!hasNextElement)
				nextElement = null;

			if (!property.Next(true))
				yield break;

			while (!UnityEditor.SerializedProperty.EqualContents(property, nextElement))
			{
				yield return property.Copy();

				if (!property.Next(false))
					break;
			}
		}

		private static bool DrawFoldout(UnityEditor.SerializedProperty property, UnityEngine.GUIContent label, UnityEngine.FontStyle fontStyle)
		{
			var settingsFoldoutStyle = new UnityEngine.GUIStyle(UnityEditor.EditorStyles.foldout) { fontStyle = fontStyle };
			var value = UnityEditor.EditorGUILayout.Foldout(property.isExpanded, label, true, settingsFoldoutStyle);
			property.isExpanded = value;
			return value;
		}

		private class PlatformData
		{
			public PlatformSettings Settings;
			public UnityEditor.SerializedObject SerializedObject;
			public string Name;
		}

		private class GlobalSettingsGroupData
		{
			string ToolTip;
			string DisplayName;
			UnityEditor.SerializedProperty Property;
			System.Collections.Generic.List<GlobalSettingData> SettingsList;

			public GlobalSettingsGroupData(string settingsGroup, UnityEditor.SerializedObject serializedObject, string displayName, System.Collections.Generic.HashSet<string> propertyHashSet)
			{
				Property = serializedObject.FindProperty(settingsGroup);
				if (Property == null)
					return;

				ToolTip = AkUtilities.GetTooltip(Property);
				DisplayName = string.IsNullOrEmpty(displayName) ? ("Common " + Property.displayName) : displayName;
				SettingsList = new System.Collections.Generic.List<GlobalSettingData>();

				foreach (var childPropertyPath in propertyHashSet)
				{
					var childProperty = serializedObject.FindProperty(childPropertyPath);
					if (childProperty == null)
						continue;

					SettingsList.Add(new GlobalSettingData(childProperty, childPropertyPath));
				}
			}

			public void SetupPlatform(PlatformData platform)
			{
				if (SettingsList == null || SettingsList.Count == 0)
					return;

				foreach (var settings in SettingsList)
					settings.SetupPlatform(platform);
			}

			public void ClearPlatformData()
			{
				if (SettingsList != null)
					foreach (var setting in SettingsList)
						setting.ClearPlatformData();
			}

			public void Draw()
			{
				if (SettingsList == null || SettingsList.Count == 0)
					return;

				using (var verticalScope = new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
#if !UNITY_2018_3_OR_NEWER
					++UnityEditor.EditorGUI.indentLevel;
#endif

					var label = new UnityEngine.GUIContent { text = DisplayName, tooltip = ToolTip };
					if (DrawFoldout(Property, label, UnityEngine.FontStyle.Bold))
						foreach (var settings in SettingsList)
							settings.Draw();

#if !UNITY_2018_3_OR_NEWER
					--UnityEditor.EditorGUI.indentLevel;
#endif
				}
			}

			public class GlobalSettingData
			{
				string ToolTip;
				readonly string DisplayName;
				readonly string PropertyPath;
				readonly bool IsStringValue;
				UnityEditor.SerializedProperty Property;
				readonly UnityEditor.SerializedPropertyType PropertyType;
				System.Collections.Generic.List<PlatformSettingData> SettingsList;
				System.Collections.Generic.List<GlobalSettingData> Children;

				bool HasChildren { get { return Children != null && Children.Count > 0; } }

				public GlobalSettingData(UnityEditor.SerializedProperty property, string propertyPath)
				{
					Property = property;
					PropertyType = property.propertyType;
					PropertyPath = propertyPath;
					DisplayName = property.displayName;
					ToolTip = AkUtilities.GetTooltip(property);

					if (property.type == "string")
					{
						IsStringValue = true;
					}
					else if (property.hasChildren)
					{
						Children = new System.Collections.Generic.List<GlobalSettingData>();
						foreach (var child in GetChildren(property))
							Children.Add(new GlobalSettingData(child, child.propertyPath));
					}
				}

				public void SetupPlatform(PlatformData platform)
				{
					if (HasChildren)
					{
						foreach (var child in Children)
							child.SetupPlatform(platform);
					}
					else
					{
						if (platform.Settings.IsPropertyIgnored(PropertyPath))
							return;

						if (SettingsList == null)
							SettingsList = new System.Collections.Generic.List<PlatformSettingData>();

						SettingsList.Add(new PlatformSettingData(platform, PropertyPath, ToolTip));
					}
				}

				public void ClearPlatformData()
				{
					SettingsList = null;

					if (HasChildren)
						foreach (var child in Children)
							child.ClearPlatformData();
				}

				private bool AnyChildUsesGlobalValue
				{
					get
					{
						if (Children != null && Children.Count > 0)
						{
							foreach (var child in Children)
								if (child.AnyChildUsesGlobalValue)
									return true;

							return false;
						}

						if (SettingsList == null || SettingsList.Count == 0)
							return true;

						foreach (var settings in SettingsList)
							if (settings.Platform.Settings.GlobalPropertyHashSet.Contains(PropertyPath))
								return true;

						return false;
					}
				}

				private bool AllChildrenAreEqual
				{
					get
					{
						if (Children != null && Children.Count > 0)
						{
							foreach (var child in Children)
								if (!child.AllChildrenAreEqual)
									return false;

							return true;
						}

						if (SettingsList == null)
							return true;

						switch (PropertyType)
						{
							case UnityEditor.SerializedPropertyType.Boolean:
								var boolValue = Property.boolValue;
								foreach (var settings in SettingsList)
									if (boolValue != settings.Property.boolValue)
										return false;
								return true;

							case UnityEditor.SerializedPropertyType.Enum:
								var enumValueIndex = Property.enumValueIndex;
								foreach (var settings in SettingsList)
									if (enumValueIndex != settings.Property.enumValueIndex)
										return false;
								return true;

							case UnityEditor.SerializedPropertyType.Float:
								var floatValue = Property.floatValue;
								foreach (var settings in SettingsList)
									if (floatValue != settings.Property.floatValue)
										return false;
								return true;

							case UnityEditor.SerializedPropertyType.Integer:
								var longValue = Property.longValue;
								foreach (var settings in SettingsList)
									if (longValue != settings.Property.longValue)
										return false;
								return true;

							case UnityEditor.SerializedPropertyType.String:
								var stringValue = Property.stringValue;
								foreach (var settings in SettingsList)
									if (stringValue != settings.Property.stringValue)
										return false;
								return true;
						}

						return true;
					}
				}

				public void Draw()
				{
					var hasChanged = false;

					var isString = IsStringValue;
					var hasChildren = HasChildren;

					if (!hasChildren && (SettingsList == null || SettingsList.Count == 0))
						return;

					using (var verticalScope = new UnityEditor.EditorGUILayout.VerticalScope())
					{
						var indentLevel = UnityEditor.EditorGUI.indentLevel++;
						var forceExpand = !AnyChildUsesGlobalValue;
						var label = new UnityEngine.GUIContent(DisplayName, ToolTip);

						if (hasChildren)
						{
							DrawFoldout(Property, label, AllChildrenAreEqual ? UnityEngine.FontStyle.Normal : UnityEngine.FontStyle.Italic);
						}
						else if (!forceExpand)
						{
							DrawFoldout(Property, label, AllChildrenAreEqual ? UnityEngine.FontStyle.Normal : UnityEngine.FontStyle.Italic);

							UnityEditor.EditorGUI.BeginChangeCheck();
							var labelWithTooltipOnly = new UnityEngine.GUIContent { tooltip = ToolTip };
							if (isString)
								UnityEditor.EditorGUILayout.DelayedTextField(Property, labelWithTooltipOnly);
							else
								UnityEditor.EditorGUILayout.PropertyField(Property, labelWithTooltipOnly, false);
							hasChanged = UnityEditor.EditorGUI.EndChangeCheck();
						}
						else
						{
							UnityEditor.EditorGUILayout.LabelField(label);
						}

						if (hasChildren)
						{
							if (Property.isExpanded)
								foreach (var child in Children)
									child.Draw();
						}
						else if (forceExpand || Property.isExpanded)
						{
							foreach (var settings in SettingsList)
								settings.Draw(Property, PropertyPath, forceExpand, ToolTip);
						}
						else if (hasChanged)
						{
							foreach (var settings in SettingsList)
								if (settings.Platform.Settings.GlobalPropertyHashSet.Contains(PropertyPath))
									settings.UpdateValue(Property);
						}

						UnityEditor.EditorGUI.indentLevel = indentLevel;
					}
				}

				public class PlatformSettingData
				{
					public UnityEditor.SerializedProperty Property;
					public PlatformData Platform;

					public PlatformSettingData(PlatformData platform, string propertyPath, string tooltip)
					{
						Platform = platform;
						Property = Platform.SerializedObject.FindProperty(propertyPath);
					}

					public void UpdateValue(UnityEditor.SerializedProperty globalProperty)
					{
						if (Property == null)
							return;

						Platform.SerializedObject.Update();
						PropagateValue(Property, globalProperty);
						Platform.SerializedObject.ApplyModifiedProperties();
					}

					public void Draw(UnityEditor.SerializedProperty globalProperty, string propertyPath, bool forceExpand, string tooltip)
					{
						if (Property == null)
							return;

						var indentLevel = UnityEditor.EditorGUI.indentLevel++;
						var position = UnityEngine.GUILayoutUtility.GetRect(UnityEngine.GUIContent.none, UnityEngine.GUIStyle.none, UnityEngine.GUILayout.Height(UnityEditor.EditorGUIUtility.singleLineHeight));

						var wasUsingGlobalValue = Platform.Settings.GlobalPropertyHashSet.Contains(propertyPath);
						var width = position.width;
						if (!wasUsingGlobalValue)
							position.width = UnityEditor.EditorGUIUtility.labelWidth;

						var isUsingGlobalValue = UnityEditor.EditorGUI.ToggleLeft(position, new UnityEngine.GUIContent(Platform.Name, tooltip), wasUsingGlobalValue);
						position.width = width;

						if (wasUsingGlobalValue != isUsingGlobalValue)
							Platform.Settings.SetUseGlobalPropertyValue(propertyPath, isUsingGlobalValue);

						if (!isUsingGlobalValue)
						{
							position.x += UnityEditor.EditorGUIUtility.labelWidth;
							position.width -= UnityEditor.EditorGUIUtility.labelWidth;
							UnityEditor.EditorGUI.indentLevel = 1; // Not zero, so that a control handle is available

							Platform.SerializedObject.Update();
							UnityEditor.EditorGUI.PropertyField(position, Property, new UnityEngine.GUIContent { tooltip = tooltip });
							Platform.SerializedObject.ApplyModifiedProperties();
						}
						else if (forceExpand)
							PropagateValue(globalProperty, Property);
						else
							UpdateValue(globalProperty);

						UnityEditor.EditorGUI.indentLevel = indentLevel;
					}

					private static void PropagateValue(UnityEditor.SerializedProperty x, UnityEditor.SerializedProperty y)
					{
						//if (x.propertyType != y.propertyType)
						//	return;

						switch (x.propertyType)
						{
							case UnityEditor.SerializedPropertyType.Boolean:
								x.boolValue = y.boolValue;
								break;

							case UnityEditor.SerializedPropertyType.Enum:
								x.longValue = y.longValue;
								break;

							case UnityEditor.SerializedPropertyType.Float:
								x.floatValue = y.floatValue;
								break;

							case UnityEditor.SerializedPropertyType.Integer:
								x.longValue = y.longValue;
								break;

							case UnityEditor.SerializedPropertyType.String:
								x.stringValue = y.stringValue;
								break;

							case UnityEditor.SerializedPropertyType.Generic:
								if (x.type == y.type)
								{
									var XProperty = x.Copy();
									var YProperty = y.Copy();
									var XEndProperty = x.Copy();
									var YEndProperty = y.Copy();
									XEndProperty = XEndProperty.Next(false) ? XEndProperty : null;
									YEndProperty = YEndProperty.Next(false) ? YEndProperty : null;

									while (XProperty.Next(true) && YProperty.Next(true) && !UnityEditor.SerializedProperty.EqualContents(XProperty, XEndProperty) && !UnityEditor.SerializedProperty.EqualContents(YProperty, YEndProperty))
										PropagateValue(XProperty, YProperty);
								}
								break;
						}
					}
				}
			}
		}

		private class PlatformSpecificSettingsData
		{
			public string ToolTip;
			public UnityEditor.SerializedProperty Property;
			public System.Collections.Generic.List<UnityEditor.SerializedProperty> SettingsList;
			public PlatformData Platform;

			public PlatformSpecificSettingsData(PlatformData platform, string propertyPath, System.Collections.Generic.HashSet<string> globalPropertyHashSet)
			{
				Platform = platform;
				Property = Platform.SerializedObject.FindProperty(propertyPath);
				if (Property == null)
					return;

				ToolTip = AkUtilities.GetTooltip(Property);

				System.Collections.Generic.HashSet<string> hashSet = new System.Collections.Generic.HashSet<string>();
				foreach (var childProperty in GetChildren(Property))
					hashSet.Add(childProperty.propertyPath);

				var remainder = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Except(hashSet, globalPropertyHashSet));
				if (remainder.Length > 0)
					SettingsList = new System.Collections.Generic.List<UnityEditor.SerializedProperty>();

				foreach (var childPropertyPath in remainder)
				{
					var childProperty = Platform.SerializedObject.FindProperty(childPropertyPath);
					if (childProperty != null)
						SettingsList.Add(childProperty);
				}
			}

			public void Draw()
			{
				if (SettingsList == null || SettingsList.Count == 0)
					return;

				Platform.SerializedObject.Update();

				using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
#if !UNITY_2018_3_OR_NEWER
					++UnityEditor.EditorGUI.indentLevel;
#endif

					var label = new UnityEngine.GUIContent(Platform.Name + " Specific " + Property.displayName, ToolTip);
					if (DrawFoldout(Property, label, UnityEngine.FontStyle.Bold))
					{
						++UnityEditor.EditorGUI.indentLevel;

						foreach (var child in SettingsList)
							UnityEditor.EditorGUILayout.PropertyField(child, true);

						--UnityEditor.EditorGUI.indentLevel;
					}

#if !UNITY_2018_3_OR_NEWER
					--UnityEditor.EditorGUI.indentLevel;
#endif
				}

				Platform.SerializedObject.ApplyModifiedProperties();
			}
		}
	}
#endregion
#endif
}

