#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !AK_DISABLE_TIMELINE

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Defines the behavior of a \ref AkTimelineEventPlayable within a \ref AkTimelineEventTrack.
/// \sa
/// - \ref AkTimelineEventTrack
/// - \ref AkTimelineEventPlayable
public class AkTimelineEventPlayableBehavior : UnityEngine.Playables.PlayableBehaviour
{
	private float currentDuration = -1f;
	private float currentDurationProportion = 1f;
	private bool eventIsPlaying;
	private bool fadeinTriggered;
	private bool fadeoutTriggered;
	private float previousEventStartTime;

	private const uint CallbackFlags = (uint)(AkCallbackType.AK_EndOfEvent | AkCallbackType.AK_Duration);

	private void CallbackHandler(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
	{
		if (in_type == AkCallbackType.AK_EndOfEvent)
		{
			eventIsPlaying = fadeinTriggered = fadeoutTriggered = false;
		}
		else if (in_type == AkCallbackType.AK_Duration)
		{
			var estimatedDuration = (in_info as AkDurationCallbackInfo).fEstimatedDuration;
			currentDuration = estimatedDuration * currentDurationProportion / 1000f;
		}
	}

#if UNITY_EDITOR
	private static bool CanPostEvents
	{
		get { return UnityEditor.SessionState.GetBool("AkTimelineEventPlayableBehavior.CanPostEvents", true); }
		set { UnityEditor.SessionState.SetBool("AkTimelineEventPlayableBehavior.CanPostEvents", value); }
	}

	[UnityEditor.InitializeOnLoadMethod]
	private static void DetermineCanPostEvents()
	{
		UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (string text, UnityEditor.Compilation.CompilerMessage[] messages) =>
		{
			if (!UnityEditor.EditorApplication.isPlaying)
				CanPostEvents = false;
		};

		UnityEditor.EditorApplication.playModeStateChanged += (UnityEditor.PlayModeStateChange playMode) =>
		{
			if (playMode == UnityEditor.PlayModeStateChange.ExitingEditMode)
				CanPostEvents = true;
		};
	}
#endif

	[System.Flags]
	private enum Actions
	{
		None = 0,
		Playback = 1 << 0,
		Retrigger = 1 << 1,
		DelayedStop = 1 << 2,
		Seek = 1 << 3,
		FadeIn = 1 << 4,
		FadeOut = 1 << 5
	}
	private Actions requiredActions;

	private const int scrubPlaybackLengthMs = 100;

	public AK.Wwise.Event akEvent;

	public float eventDurationMax;
	public float eventDurationMin;

	public float blendInDuration;
	public float blendOutDuration;
	public float easeInDuration;
	public float easeOutDuration;

	public AkCurveInterpolation blendInCurve;
	public AkCurveInterpolation blendOutCurve;

	public UnityEngine.GameObject eventObject;

	public bool retriggerEvent;
	private bool wasScrubbingAndRequiresRetrigger;
	public bool StopEventAtClipEnd;

	private bool IsScrubbing(UnityEngine.Playables.FrameData info)
	{
#if !UNITY_2018_2_OR_NEWER
		// We disable scrubbing in edit mode, due to an issue with how FrameData.EvaluationType is handled in edit mode.
		// This is a known issue and Unity are aware of it: https://fogbugz.unity3d.com/default.asp?953109_kitf7pso0vmjm0m0
		if (!UnityEngine.Application.isPlaying)
			return false;
#endif
		return info.evaluationType == UnityEngine.Playables.FrameData.EvaluationType.Evaluate;
	}

	public override void PrepareFrame(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
	{
		base.PrepareFrame(playable, info);

		if (akEvent == null)
			return;

		var shouldPlay = ShouldPlay(playable);
		if (IsScrubbing(info) && shouldPlay)
		{
			requiredActions |= Actions.Seek;

			if (!eventIsPlaying)
			{
				requiredActions |= Actions.Playback | Actions.DelayedStop;
				CheckForFadeInFadeOut(playable);
			}
		}
		else if (!eventIsPlaying && (requiredActions & Actions.Playback) == 0)
		{
			// The clip is playing but the event hasn't been triggered. We need to start the event and jump to the correct time.
			requiredActions |= Actions.Retrigger;
			CheckForFadeInFadeOut(playable);
		}
		else
		{
			CheckForFadeOut(playable, UnityEngine.Playables.PlayableExtensions.GetTime(playable));
		}
	}

	private const float alph = 0.05f;

	public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
	{
		base.OnBehaviourPlay(playable, info);

		if (akEvent == null)
			return;

		var shouldPlay = ShouldPlay(playable);
		if (!shouldPlay)
			return;

		requiredActions |= Actions.Playback;

		if (IsScrubbing(info))
		{
			wasScrubbingAndRequiresRetrigger = true;
			// If we've explicitly set the playhead, only play a small snippet.
			requiredActions |= Actions.DelayedStop;
		}
		else if (GetProportionalTime(playable) > alph)
		{
			// we need to jump to the correct position in the case where the event is played from some non-start position.
			requiredActions |= Actions.Seek;
		}

		CheckForFadeInFadeOut(playable);
	}

	public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
	{
		wasScrubbingAndRequiresRetrigger = false;

		base.OnBehaviourPause(playable, info);
		if (eventObject != null && akEvent != null && StopEventAtClipEnd)
		{
			StopEvent();
		}
	}

	public override void ProcessFrame(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info, object playerData)
	{
		base.ProcessFrame(playable, info, playerData);

		if (akEvent == null)
			return;

		var obj = playerData as UnityEngine.GameObject;
		if (obj != null)
			eventObject = obj;

		if (eventObject == null)
			return;

		if ((requiredActions & Actions.Playback) != 0)
			PlayEvent();

		if ((requiredActions & Actions.Seek) != 0)
			SeekToTime(playable);

		if ((retriggerEvent || wasScrubbingAndRequiresRetrigger) && (requiredActions & Actions.Retrigger) != 0)
			RetriggerEvent(playable);

		if ((requiredActions & Actions.DelayedStop) != 0)
			StopEvent(scrubPlaybackLengthMs);

		if (!fadeinTriggered && (requiredActions & Actions.FadeIn) != 0)
			TriggerFadeIn(playable);

		if (!fadeoutTriggered && (requiredActions & Actions.FadeOut) != 0)
			TriggerFadeOut(playable);

		requiredActions = Actions.None;
	}

	/** Check the playable time against the Wwise event duration to see if playback should occur.
     */
	private bool ShouldPlay(UnityEngine.Playables.Playable playable)
	{
		var previousTime = UnityEngine.Playables.PlayableExtensions.GetPreviousTime(playable);
		var currentTime = UnityEngine.Playables.PlayableExtensions.GetTime(playable);
		if (previousTime == 0.0 && System.Math.Abs(currentTime - previousTime) > 1.0)
			return false;

		if (retriggerEvent)
			return true;

		// If max and min duration values from metadata are equal, we can assume a deterministic event.
		if (eventDurationMax == eventDurationMin && eventDurationMin != -1f)
			return currentTime < eventDurationMax;

		currentTime -= previousEventStartTime;

		var maxDuration = currentDuration == -1f ? (float)UnityEngine.Playables.PlayableExtensions.GetDuration(playable) : currentDuration;
		return currentTime < maxDuration;
	}

	private void CheckForFadeInFadeOut(UnityEngine.Playables.Playable playable)
	{
		var currentClipTime = UnityEngine.Playables.PlayableExtensions.GetTime(playable);
		if (blendInDuration > currentClipTime || easeInDuration > currentClipTime)
			requiredActions |= Actions.FadeIn;

		CheckForFadeOut(playable, currentClipTime);
	}

	private void CheckForFadeOut(UnityEngine.Playables.Playable playable, double currentClipTime)
	{
		var timeLeft = UnityEngine.Playables.PlayableExtensions.GetDuration(playable) - currentClipTime;
		if (blendOutDuration >= timeLeft || easeOutDuration >= timeLeft)
			requiredActions |= Actions.FadeOut;
	}

	private void TriggerFadeIn(UnityEngine.Playables.Playable playable)
	{
		var currentClipTime = UnityEngine.Playables.PlayableExtensions.GetTime(playable);
		var fadeDuration = UnityEngine.Mathf.Max(easeInDuration, blendInDuration) - currentClipTime;
		if (fadeDuration > 0)
		{
			fadeinTriggered = true;
			akEvent.ExecuteAction(eventObject, AkActionOnEventType.AkActionOnEventType_Pause, 0, blendOutCurve);
			akEvent.ExecuteAction(eventObject, AkActionOnEventType.AkActionOnEventType_Resume, (int)(fadeDuration * 1000), blendInCurve);
		}
	}

	private void TriggerFadeOut(UnityEngine.Playables.Playable playable)
	{
		fadeoutTriggered = true;

		var fadeDuration = UnityEngine.Playables.PlayableExtensions.GetDuration(playable) - UnityEngine.Playables.PlayableExtensions.GetTime(playable);
		akEvent.ExecuteAction(eventObject, AkActionOnEventType.AkActionOnEventType_Stop, (int)(fadeDuration * 1000), blendOutCurve);
	}

	private void StopEvent(int transition = 0)
	{
		if (!eventIsPlaying)
			return;

		akEvent.Stop(eventObject, transition);

#if UNITY_EDITOR
		if (!UnityEditor.EditorApplication.isPlaying)
			eventIsPlaying = false;
#endif
	}

	private bool PostEvent()
	{
		fadeinTriggered = fadeoutTriggered = false;

		uint playingID;

#if UNITY_EDITOR
		if (!CanPostEvents)
		{
			playingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
		}
		else if (!UnityEditor.EditorApplication.isPlaying)
		{
			playingID = akEvent.Post(eventObject);
		}
		else
#endif
		{
			playingID = akEvent.Post(eventObject, CallbackFlags, CallbackHandler, null);
		}

		eventIsPlaying = playingID != AkSoundEngine.AK_INVALID_PLAYING_ID;
		return eventIsPlaying;
	}

	private void PlayEvent()
	{
		if (!PostEvent())
			return;

		currentDurationProportion = 1f;
		previousEventStartTime = 0f;
	}

	private void RetriggerEvent(UnityEngine.Playables.Playable playable)
	{
		wasScrubbingAndRequiresRetrigger = false;

		if (!PostEvent())
			return;

		currentDurationProportion = 1f - SeekToTime(playable);
		previousEventStartTime = (float)UnityEngine.Playables.PlayableExtensions.GetTime(playable);
	}

	private float GetProportionalTime(UnityEngine.Playables.Playable playable)
	{
		// If max and min duration values from metadata are equal, we can assume a deterministic event.
		if (eventDurationMax == eventDurationMin && eventDurationMin != -1f)
		{
			// If the timeline clip has length greater than the event duration, we want to loop.
			return (float)UnityEngine.Playables.PlayableExtensions.GetTime(playable) % eventDurationMax / eventDurationMax;
		}

		var currentTime = (float)UnityEngine.Playables.PlayableExtensions.GetTime(playable) - previousEventStartTime;
		var maxDuration = currentDuration == -1f ? (float)UnityEngine.Playables.PlayableExtensions.GetDuration(playable) : currentDuration;
		// If the timeline clip has length greater than the event duration, we want to loop.
		return currentTime % maxDuration / maxDuration;
	}

	// Seek to the current time, taking looping into account.
	private float SeekToTime(UnityEngine.Playables.Playable playable)
	{
		var proportionalTime = GetProportionalTime(playable);
		if (proportionalTime >= 1f) // Avoids Wwise "seeking beyond end of event: audio will stop" error.
			return 1f;

#if UNITY_EDITOR
		if (!CanPostEvents)
			return proportionalTime;
#endif

		if (eventIsPlaying)
			AkSoundEngine.SeekOnEvent(akEvent.Id, eventObject, proportionalTime);

		return proportionalTime;
	}
}

/// @brief A playable asset containing a Wwise event that can be placed within a \ref AkTimelineEventTrack in a timeline.
/// @details Use this class to play Wwise events from a timeline and synchronize them to the animation. Events will be emitted from the GameObject that is bound to the AkTimelineEventTrack.
/// \sa
/// - \ref AkTimelineEventTrack
/// - \ref AkTimelineEventPlayableBehavior
public class AkTimelineEventPlayable : UnityEngine.Playables.PlayableAsset, UnityEngine.Timeline.ITimelineClipAsset
{
	public AK.Wwise.Event akEvent = new AK.Wwise.Event();

	[UnityEngine.SerializeField]
	private AkCurveInterpolation blendInCurve = AkCurveInterpolation.AkCurveInterpolation_Linear;
	[UnityEngine.SerializeField]
	private AkCurveInterpolation blendOutCurve = AkCurveInterpolation.AkCurveInterpolation_Linear;

	public float eventDurationMax = -1f;
	public float eventDurationMin = -1f;

	[System.NonSerialized]
	public UnityEngine.Timeline.TimelineClip owningClip;

	[UnityEngine.SerializeField]
	private bool retriggerEvent = false;

	public bool UseWwiseEventDuration = true;

	[UnityEngine.SerializeField]
	private bool StopEventAtClipEnd = true;

	UnityEngine.Timeline.ClipCaps UnityEngine.Timeline.ITimelineClipAsset.clipCaps
	{
		get { return UnityEngine.Timeline.ClipCaps.Looping | UnityEngine.Timeline.ClipCaps.Blending; }
	}

	public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject owner)
	{
		var playable = UnityEngine.Playables.ScriptPlayable<AkTimelineEventPlayableBehavior>.Create(graph);
		if (akEvent == null)
			return playable;

		var b = playable.GetBehaviour();
		b.akEvent = akEvent;
		b.blendInCurve = blendInCurve;
		b.blendOutCurve = blendOutCurve;

		if (owningClip != null)
		{
			b.easeInDuration = (float)owningClip.easeInDuration;
			b.easeOutDuration = (float)owningClip.easeOutDuration;
			b.blendInDuration = (float)owningClip.blendInDuration;
			b.blendOutDuration = (float)owningClip.blendOutDuration;
		}
		else
			b.easeInDuration = b.easeOutDuration = b.blendInDuration = b.blendOutDuration = 0;

		b.retriggerEvent = retriggerEvent;
		b.StopEventAtClipEnd = StopEventAtClipEnd;
		b.eventObject = owner;
		b.eventDurationMin = eventDurationMin;
		b.eventDurationMax = eventDurationMax;
		return playable;
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(AkTimelineEventPlayable))]
	public class Editor : UnityEditor.Editor
	{
		private AkTimelineEventPlayable m_AkTimelineEventPlayable;
		private UnityEditor.SerializedProperty akEvent;
		private UnityEditor.SerializedProperty retriggerEvent;
		private UnityEditor.SerializedProperty UseWwiseEventDuration;
		private UnityEditor.SerializedProperty StopEventAtClipEnd;
		private UnityEditor.SerializedProperty blendInCurve;
		private UnityEditor.SerializedProperty blendOutCurve;

		public void OnEnable()
		{
			m_AkTimelineEventPlayable = target as AkTimelineEventPlayable;
			if (m_AkTimelineEventPlayable == null)
				return;

			akEvent = serializedObject.FindProperty("akEvent");
			retriggerEvent = serializedObject.FindProperty("retriggerEvent");
			UseWwiseEventDuration = serializedObject.FindProperty("UseWwiseEventDuration");
			StopEventAtClipEnd = serializedObject.FindProperty("StopEventAtClipEnd");
			blendInCurve = serializedObject.FindProperty("blendInCurve");
			blendOutCurve = serializedObject.FindProperty("blendOutCurve");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

			using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
			{
				UnityEditor.EditorGUILayout.PropertyField(akEvent, new UnityEngine.GUIContent("Event: "));
				UnityEditor.EditorGUILayout.PropertyField(blendInCurve);
				UnityEditor.EditorGUILayout.PropertyField(blendOutCurve);
			}

			using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
			{
				UnityEditor.EditorGUILayout.PropertyField(UseWwiseEventDuration, new UnityEngine.GUIContent("Use Wwise Event Duration: ", "The clip duration is set to the duration of the Wwise Event"));

				if (!UpdateClipInformation(m_AkTimelineEventPlayable.owningClip, m_AkTimelineEventPlayable.akEvent, serializedObject, UseWwiseEventDuration.boolValue))
				{
					UnityEditor.EditorGUILayout.HelpBox(string.Format("The duration of the Wwise event \"{0}\" has not been determined. Playback for this event may be inconsistent. " +
						"Ensure that the event is associated with a generated SoundBank!", m_AkTimelineEventPlayable.akEvent.Name), UnityEditor.MessageType.Warning);
				}

				if (!UseWwiseEventDuration.boolValue)
				{
					var StopEventAtClipEndValue = StopEventAtClipEnd.boolValue;
					var retriggerEventValue = retriggerEvent.boolValue;

					UnityEditor.EditorGUILayout.PropertyField(StopEventAtClipEnd, new UnityEngine.GUIContent("Stop Event At End Of Clip: "));
					UnityEditor.EditorGUILayout.PropertyField(retriggerEvent, new UnityEngine.GUIContent("Loop: ", "When checked, an event will loop until the end of the clip."));

					if (retriggerEvent.boolValue && !StopEventAtClipEnd.boolValue)
					{
						if (!retriggerEventValue)
							StopEventAtClipEnd.boolValue = true;
						else if (StopEventAtClipEndValue)
							retriggerEvent.boolValue = false;
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private static void UpdateProgressBar(int index, int count)
		{
			float progress = (float)index / count;
			UnityEditor.EditorUtility.DisplayProgressBar("Wwise Integration", "Fixing clip durations of AkTimelineEventPlayables...", progress);
		}

		[UnityEditor.InitializeOnLoadMethod]
		public static void SetupSoundbankSetting()
		{
			AkUtilities.EnableBoolSoundbankSettingInWproj("SoundBankGenerateEstimatedDuration", AkWwiseEditorSettings.WwiseProjectAbsolutePath);

			UnityEditor.EditorApplication.update += RunOnce;
			AkWwiseXMLWatcher.Instance.XMLUpdated += UpdateAllClips;
		}

		private static void RunOnce()
		{
			UpdateAllClips();
			UnityEditor.EditorApplication.update -= RunOnce;
		}

		private static void UpdateAllClips()
		{
			var guids = UnityEditor.AssetDatabase.FindAssets("t:AkTimelineEventPlayable", new[] { "Assets" });
			if (guids.Length < 1)
				return;

			var processedGuids = new System.Collections.Generic.HashSet<string>();

			for (var i = 0; i < guids.Length; i++)
			{
				UpdateProgressBar(i, guids.Length);

				var guid = guids[i];
				if (processedGuids.Contains(guid))
					continue;

				processedGuids.Add(guid);

				var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
				var instanceIds = new System.Collections.Generic.List<int>();
				foreach (var obj in objects)
				{
					if (obj == null)
						continue;

					var id = obj.GetInstanceID();
					if (!instanceIds.Contains(id))
						instanceIds.Add(id);
				}

				for (; instanceIds.Count > 0; instanceIds.RemoveAt(0))
				{
					var id = instanceIds[0];
					objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
					foreach (var obj in objects)
					{
						if (obj && obj.GetInstanceID() == id)
						{
							var playable = obj as AkTimelineEventPlayable;
							if (playable)
							{
								var serializedObject = new UnityEditor.SerializedObject(playable);
								var setClipDuration = serializedObject.FindProperty("UseWwiseEventDuration").boolValue;
								UpdateClipInformation(playable.owningClip, playable.akEvent, serializedObject, setClipDuration);
								serializedObject.ApplyModifiedProperties();
							}
							break;
						}
					}
				}
			}

			UnityEditor.EditorUtility.ClearProgressBar();
		}

		/// <summary>
		/// The minimum clip duration. This value is set to 1/60 of a second which generally represents the time of 1 frame.
		/// </summary>
		private const double MinimumDurationInSeconds = 1.0 / 60;

		/// <summary>
		/// Updates the associated clip information and the event durations.
		/// </summary>
		/// <returns>Returns true if the Wwise event is found in the project data.</returns>
		private static bool UpdateClipInformation(UnityEngine.Timeline.TimelineClip clip, AK.Wwise.Event akEvent,
			UnityEditor.SerializedObject serializedObject, bool setClipDuration)
		{
			var clipDuration = MinimumDurationInSeconds;
			var maxDuration = -1.0f;
			var minDuration = -1.0f;

			AkUtilities.GetEventDurations(akEvent.Id, ref maxDuration, ref minDuration);
			if (maxDuration != -1.0f)
			{
				serializedObject.FindProperty("eventDurationMin").floatValue = minDuration;
				serializedObject.FindProperty("eventDurationMax").floatValue = maxDuration;

				if (maxDuration > clipDuration)
					clipDuration = maxDuration;
			}

			if (clip != null)
			{
				clip.displayName = akEvent.Name;
				if (setClipDuration)
					clip.duration = clipDuration;
			}

			return maxDuration != -1.0f;
		}
	}

#endif //#if UNITY_EDITOR
}

#endif // !AK_DISABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
