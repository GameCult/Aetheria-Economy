#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !AK_DISABLE_TIMELINE

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEngine.Timeline.TrackColor(0.855f, 0.8623f, 0.870f)]
[UnityEngine.Timeline.TrackClipType(typeof(AkTimelineEventPlayable))]
[UnityEngine.Timeline.TrackBindingType(typeof(UnityEngine.GameObject))]
/// @brief A track within timeline that holds \ref AkTimelineEventPlayable clips. 
/// @details AkTimelineEventTracks are bound to specific GameObjects, which are the default emitters for all of the associated \ref AkTimelineEventPlayable clips.
/// \sa
/// - \ref AkTimelineEventPlayable
/// - \ref AkTimelineEventPlayableBehavior
public class AkTimelineEventTrack : UnityEngine.Timeline.TrackAsset
{
	public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go, int inputCount)
	{
		var playable = UnityEngine.Playables.ScriptPlayable<AkTimelineEventPlayableBehavior>.Create(graph);
		UnityEngine.Playables.PlayableExtensions.SetInputCount(playable, inputCount);

		var clips = GetClips();
		foreach (var clip in clips)
		{
			var eventPlayable = clip.asset as AkTimelineEventPlayable;
			eventPlayable.owningClip = clip;
		}

		return playable;
	}
}
#endif // !AK_DISABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
