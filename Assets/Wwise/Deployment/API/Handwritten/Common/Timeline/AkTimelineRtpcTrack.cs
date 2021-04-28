#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !AK_DISABLE_TIMELINE

[UnityEngine.Timeline.TrackColor(0.32f, 0.13f, 0.13f)]
// Specifies the type of Playable Asset this track manages
[UnityEngine.Timeline.TrackClipType(typeof(AkTimelineRtpcPlayable))]
// Use if the track requires a binding to a scene object or asset
[UnityEngine.Timeline.TrackBindingType(typeof(UnityEngine.GameObject))]
public class AkTimelineRtpcTrack : UnityEngine.Timeline.TrackAsset
{
	public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject gameObject, int inputCount)
	{
		var playable = UnityEngine.Playables.ScriptPlayable<AkTimelineRtpcPlayableBehaviour>.Create(graph, inputCount);

		var clips = GetClips();
		foreach (var clip in clips)
		{
			var rtpcPlayable = (clip.asset as AkTimelineRtpcPlayable);
			rtpcPlayable.owningClip = clip;
			rtpcPlayable.SetupClipDisplay();
		}

		return playable;
	}

	public void OnValidate()
	{
		var clips = GetClips();
		foreach (var clip in clips)
			(clip.asset as AkTimelineRtpcPlayable).SetupClipDisplay();
	}
}
#endif // !AK_DISABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
