#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
using AK.Wwise.Unity.WwiseAddressables;
#endif

public class AkSoundEngineController
{
	private static AkSoundEngineController ms_Instance;

	public static AkSoundEngineController Instance
	{
		get
		{
			if (ms_Instance == null)
				ms_Instance = new AkSoundEngineController();

			return ms_Instance;
		}
	}

	private AkSoundEngineController()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.pauseStateChanged += OnPauseStateChanged;
#endif
	}

	~AkSoundEngineController()
	{
		if (ms_Instance == this)
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.pauseStateChanged -= OnPauseStateChanged;
			UnityEditor.EditorApplication.update -= LateUpdate;
#endif
			ms_Instance = null;
		}
	}

	public void LateUpdate()
	{
#if UNITY_EDITOR
		if (!AkSoundEngine.EditorIsSoundEngineLoaded)
			return;
#endif

		//Execute callbacks that occurred in last frame (not the current update)
		AkRoomManager.Update();
		AkRoomAwareManager.UpdateRoomAwareObjects();
		AkCallbackManager.PostCallbacks();
#if !(AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES)
		AkBankManager.DoUnloadBanks();
#endif
		AkSoundEngine.RenderAudio();
	}

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
	private AkWwiseAddressablesInitializationSettings GetInitSettingsInstance()
	{
		return AkWwiseAddressablesInitializationSettings.Instance;
	}
#else
	private AkWwiseInitializationSettings GetInitSettingsInstance()
	{
		return AkWwiseInitializationSettings.Instance;
	}
#endif

	public void Init(AkInitializer akInitializer)
	{
		// Only initialize the room mamanger during play.
		bool initRoomManager = true;
#if UNITY_EDITOR
		if (!UnityEditor.EditorApplication.isPlaying)
			initRoomManager = false;
#endif
		if (initRoomManager)
			AkRoomManager.Init();

		if (akInitializer == null)
		{
			UnityEngine.Debug.LogError("WwiseUnity: AkInitializer must not be null. Sound engine will not be initialized.");
			return;
		}

#if UNITY_EDITOR
		if (UnityEngine.Application.isPlaying && !IsTheSingleOwningInitializer(akInitializer))
		{
			UnityEngine.Debug.LogError("WwiseUnity: Sound engine is already initialized.");
			return;
		}

		var arguments = System.Environment.GetCommandLineArgs();
		if (UnityEngine.Application.isBatchMode && System.Array.IndexOf(arguments, "-wwiseEnableWithNoGraphics") < 0)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Sound engine will not be initialized in batch/nographics mode. To override, specify -wwiseEnableWithNoGraphics");
			return;
		}

		var isInitialized = false;
		try
		{
			isInitialized = AkSoundEngine.IsInitialized();
			AkSoundEngine.EditorIsSoundEngineLoaded = true;
		}
		catch (System.DllNotFoundException)
		{
			AkSoundEngine.EditorIsSoundEngineLoaded = false;
			UnityEngine.Debug.LogWarning("WwiseUnity: AkSoundEngine is not loaded.");
			return;
		}
#else
		var isInitialized = AkSoundEngine.IsInitialized();
#endif

		AkLogger.Instance.Init();

		if (isInitialized)
		{
#if UNITY_EDITOR
			if (GetInitSettingsInstance().ResetSoundEngine(UnityEngine.Application.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer))
			{
				UnityEditor.EditorApplication.update += LateUpdate;
			}


			if (UnityEditor.EditorApplication.isPaused && UnityEngine.Application.isPlaying)
			{
				AkSoundEngine.Suspend(true);
			}
#else
			UnityEngine.Debug.LogError("WwiseUnity: Sound engine is already initialized.");
#endif
				return;
		}

#if UNITY_EDITOR
		if (UnityEditor.BuildPipeline.isBuildingPlayer)
			return;
#endif

		if (!GetInitSettingsInstance().InitializeSoundEngine())
			return;

#if UNITY_EDITOR
		OnEnableEditorListener(akInitializer.gameObject);
		UnityEditor.EditorApplication.update += LateUpdate;
#endif
	}

	public void OnDisable()
	{
#if UNITY_EDITOR
		if (!AkSoundEngine.EditorIsSoundEngineLoaded)
			return;

		OnDisableEditorListener();
#endif
	}

	public void Terminate()
	{
#if UNITY_EDITOR
		ClearInitializeState();

		if (!AkSoundEngine.EditorIsSoundEngineLoaded)
			return;
#endif

		GetInitSettingsInstance().TerminateSoundEngine();
		AkRoomManager.Terminate();
	}

	// In the Editor, the sound needs to keep playing when switching windows (remote debugging in Wwise, for example).
	// On iOS, application interruptions are handled in the sound engine already.
#if UNITY_EDITOR || UNITY_IOS
	public void OnApplicationPause(bool pauseStatus)
	{
	}

	public void OnApplicationFocus(bool focus)
	{
	}
#else
	public void OnApplicationPause(bool pauseStatus) 
	{
		ActivateAudio(!pauseStatus);
	}

	public void OnApplicationFocus(bool focus)
	{
#if !UNITY_ANDROID
		ActivateAudio(focus, AkWwiseInitializationSettings.ActivePlatformSettings.RenderDuringFocusLoss);
#endif
	}
#endif

#if UNITY_EDITOR
	// Enable/Disable the audio when pressing play/pause in the editor.
	private void OnPauseStateChanged(UnityEditor.PauseState pauseState)
	{
		if (UnityEngine.Application.isPlaying)
		{
			ActivateAudio(pauseState != UnityEditor.PauseState.Paused);
		}
	}
#endif

#if UNITY_EDITOR || !UNITY_IOS
	private void ActivateAudio(bool activate, bool renderAnyway = false)
	{
		if (AkSoundEngine.IsInitialized())
		{
			if (activate)
				AkSoundEngine.WakeupFromSuspend();
			else
				AkSoundEngine.Suspend(renderAnyway);

			AkSoundEngine.RenderAudio();
		}
	}
#endif

#if UNITY_EDITOR
#region Editor Listener
	private UnityEngine.GameObject editorListenerGameObject;

	private bool IsPlayingOrIsNotInitialized
	{
		get { return UnityEngine.Application.isPlaying || !AkSoundEngine.IsInitialized(); }
	}

	private void OnEnableEditorListener(UnityEngine.GameObject gameObject)
	{
		if (IsPlayingOrIsNotInitialized || editorListenerGameObject != null)
			return;

		editorListenerGameObject = gameObject;
		AkSoundEngine.RegisterGameObj(editorListenerGameObject, editorListenerGameObject.name);

		// Do not create AkGameObj component when adding this listener
		var id = AkSoundEngine.GetAkGameObjectID(editorListenerGameObject);
		AkSoundEngine.AddDefaultListener(id);

		UnityEditor.EditorApplication.update += UpdateEditorListenerPosition;
	}

	private void OnDisableEditorListener()
	{
		if (IsPlayingOrIsNotInitialized || editorListenerGameObject == null)
			return;

		UnityEditor.EditorApplication.update -= UpdateEditorListenerPosition;

		var id = AkSoundEngine.GetAkGameObjectID(editorListenerGameObject);
		AkSoundEngine.RemoveDefaultListener(id);

		AkSoundEngine.UnregisterGameObj(editorListenerGameObject);
		editorListenerGameObject = null;
	}

	private UnityEngine.Vector3 editorListenerPosition = UnityEngine.Vector3.zero;
	private UnityEngine.Vector3 editorListenerForward = UnityEngine.Vector3.zero;
	private UnityEngine.Vector3 editorListenerUp = UnityEngine.Vector3.zero;

	private void UpdateEditorListenerPosition()
	{
		if (IsPlayingOrIsNotInitialized || editorListenerGameObject == null)
			return;

		if (UnityEditor.SceneView.lastActiveSceneView == null)
			return;

		var sceneViewCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
		if (sceneViewCamera == null)
			return;

		var sceneViewTransform = sceneViewCamera.transform;
		if (sceneViewTransform == null)
			return;

		if (editorListenerPosition == sceneViewTransform.position &&
			editorListenerForward == sceneViewTransform.forward &&
			editorListenerUp == sceneViewTransform.up)
			return;

		AkSoundEngine.SetObjectPosition(editorListenerGameObject, sceneViewTransform);

		editorListenerPosition = sceneViewTransform.position;
		editorListenerForward = sceneViewTransform.forward;
		editorListenerUp = sceneViewTransform.up;
	}
#endregion

#region Initialize only once
	private AkInitializer TheAkInitializer = null;

	/// <summary>
	/// Determines whether this AkInitializer is the single one responsible for initializing the sound engine.
	/// </summary>
	/// <param name="akInitializer"></param>
	/// <returns>Returns true when called on the first AkInitializer and false otherwise.</returns>
	private bool IsTheSingleOwningInitializer(AkInitializer akInitializer)
	{
		if (TheAkInitializer == null && akInitializer != null)
		{
			TheAkInitializer = akInitializer;
			return true;
		}

		return false;
	}

	private void ClearInitializeState()
	{
		TheAkInitializer = null;
	}
#endregion
#endif // UNITY_EDITOR
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.