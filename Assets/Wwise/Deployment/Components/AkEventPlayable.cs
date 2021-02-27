#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !AK_DISABLE_TIMELINE
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief A playable asset containing a Wwise event that can be placed within a \ref AkEventTrack in a timeline.
/// @details Use this class to play Wwise events from a timeline and synchronise them to the animation. Events will be emitted from the GameObject that is bound to the AkEventTrack. Use the overrideTrackEmitterObject option to choose a different GameObject from which to emit the Wwise event. 
/// \sa
/// - \ref AkEventTrack
/// - \ref AkEventPlayableBehavior
[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
public class AkEventPlayable : UnityEngine.Playables.PlayableAsset, UnityEngine.Timeline.ITimelineClipAsset
{
	public AK.Wwise.Event akEvent = new AK.Wwise.Event();

	[UnityEngine.SerializeField]
	private AkCurveInterpolation blendInCurve = AkCurveInterpolation.AkCurveInterpolation_Linear;
	[UnityEngine.SerializeField]
	private AkCurveInterpolation blendOutCurve = AkCurveInterpolation.AkCurveInterpolation_Linear;

	[UnityEngine.SerializeField]
	private UnityEngine.ExposedReference<UnityEngine.GameObject> emitterObjectRef;

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
		var playable = UnityEngine.Playables.ScriptPlayable<AkEventPlayableBehavior>.Create(graph);

		var eventObject = emitterObjectRef.Resolve(graph.GetResolver());
		if (eventObject == null)
			eventObject = owner;

		if (eventObject == null || akEvent == null)
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
		b.eventObject = eventObject;
		b.overrideTrackEmitterObject = eventObject != null;
		b.eventDurationMin = eventDurationMin;
		b.eventDurationMax = eventDurationMax;
		return playable;
	}
}

#endif // !AK_DISABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.