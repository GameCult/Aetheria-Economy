#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !UNITY_2019_1_OR_NEWER
#define AK_ENABLE_TIMELINE
#endif
#if AK_ENABLE_TIMELINE

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEngine.Timeline.TrackColor(0.855f, 0.8623f, 0.870f)]
[UnityEngine.Timeline.TrackClipType(typeof(AkEventPlayable))]
[UnityEngine.Timeline.TrackBindingType(typeof(UnityEngine.GameObject))]
[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
#if UNITY_2019_1_OR_NEWER
[UnityEngine.Timeline.HideInMenu]
#endif
/// @brief A track within timeline that holds \ref AkEventPlayable clips. 
/// @details AkEventTracks are bound to a specific GameObject, which is the default emitter for all of the \ref AkEventPlayable clips. There is an option to override this in /ref AkEventPlayable.
/// \sa
/// - \ref AkEventPlayable
/// - \ref AkEventPlayableBehavior
public class AkEventTrack : UnityEngine.Timeline.TrackAsset
{
	public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go, int inputCount)
	{
		var playable = UnityEngine.Playables.ScriptPlayable<AkEventPlayableBehavior>.Create(graph);
		UnityEngine.Playables.PlayableExtensions.SetInputCount(playable, inputCount);

		var clips = GetClips();
		foreach (var clip in clips)
		{
			var akEventPlayable = clip.asset as AkEventPlayable;
			akEventPlayable.owningClip = clip;
		}

		return playable;
	}
}
#endif // AK_ENABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.