#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !UNITY_2019_1_OR_NEWER
#define AK_ENABLE_TIMELINE
#endif
#if AK_ENABLE_TIMELINE

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[System.Serializable]
public class AkTimelineRtpcPlayableBehaviour : UnityEngine.Playables.PlayableBehaviour
{
	[UnityEngine.SerializeField]
	private float value = 0.0f;

	public AK.Wwise.RTPC RTPC { set; get; }
	public bool setGlobally { set; get; }
	public UnityEngine.GameObject gameObject { set; get; }

	public override void ProcessFrame(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData frameData, object playerData)
	{
		base.ProcessFrame(playable, frameData, playerData);
		if (RTPC == null)
			return;

		var obj = playerData as UnityEngine.GameObject;
		if (obj != null)
			gameObject = obj;

		if (setGlobally)
			RTPC.SetGlobalValue(value);
		else if (gameObject)
			RTPC.SetValue(gameObject, value);
	}
}

public class AkTimelineRtpcPlayable : UnityEngine.Playables.PlayableAsset, UnityEngine.Timeline.ITimelineClipAsset
{
	public AK.Wwise.RTPC RTPC = new AK.Wwise.RTPC();
	public bool setGlobally = false;
	public AkTimelineRtpcPlayableBehaviour template = new AkTimelineRtpcPlayableBehaviour();

	public void SetupClipDisplay()
	{
#if UNITY_EDITOR
		if (owningClip != null)
			owningClip.displayName = RTPC.Name;
#endif
	}

	public UnityEngine.Timeline.TimelineClip owningClip { get; set; }

	UnityEngine.Timeline.ClipCaps UnityEngine.Timeline.ITimelineClipAsset.clipCaps
	{
		get { return UnityEngine.Timeline.ClipCaps.Looping & UnityEngine.Timeline.ClipCaps.Extrapolation & UnityEngine.Timeline.ClipCaps.Blending; }
	}

	public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject gameObject)
	{
		var playable = UnityEngine.Playables.ScriptPlayable<AkTimelineRtpcPlayableBehaviour>.Create(graph, template);
		var b = playable.GetBehaviour();
		b.RTPC = RTPC;
		b.setGlobally = setGlobally;
		b.gameObject = gameObject;
		return playable;
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(AkTimelineRtpcPlayable))]
	public class Editor : UnityEditor.Editor
	{
		private AkTimelineRtpcPlayable playable;
		private UnityEditor.SerializedProperty RTPC;
		private UnityEditor.SerializedProperty setGlobally;
		private UnityEditor.SerializedProperty Behaviour;

		public void OnEnable()
		{
			playable = target as AkTimelineRtpcPlayable;
			if (playable)
				playable.SetupClipDisplay();

			RTPC = serializedObject.FindProperty("RTPC");
			setGlobally = serializedObject.FindProperty("setGlobally");
			Behaviour = serializedObject.FindProperty("template");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

			using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
			{
				UnityEditor.EditorGUILayout.PropertyField(RTPC, new UnityEngine.GUIContent("RTPC: "));
				UnityEditor.EditorGUILayout.PropertyField(setGlobally, new UnityEngine.GUIContent("Set Globally: "));
			}

			if (Behaviour != null)
				UnityEditor.EditorGUILayout.PropertyField(Behaviour, new UnityEngine.GUIContent("Animated Value: "), true);

			if (playable)
				playable.SetupClipDisplay();

			serializedObject.ApplyModifiedProperties();
		}
	}

#endif //#if UNITY_EDITOR
}
#endif // AK_ENABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
