#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Defines the behavior of a \ref AkEventPlayable within a \ref AkEventTrack.
/// \sa
/// - \ref AkEventTrack
/// - \ref AkEventPlayable
[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
public class AkEventPlayableBehavior : UnityEngine.Playables.PlayableBehaviour
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
		get { return UnityEditor.SessionState.GetBool("AkEventPlayableBehavior.CanPostEvents", true); }
		set { UnityEditor.SessionState.SetBool("AkEventPlayableBehavior.CanPostEvents", value); }
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

	public bool overrideTrackEmitterObject;

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

		if (!overrideTrackEmitterObject)
		{
			var obj = playerData as UnityEngine.GameObject;
			if (obj != null)
				eventObject = obj;
		}

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
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.