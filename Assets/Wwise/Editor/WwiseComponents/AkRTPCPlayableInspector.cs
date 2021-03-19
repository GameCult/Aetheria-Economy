﻿#if UNITY_EDITOR
#if !AK_DISABLE_TIMELINE

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
[UnityEditor.CustomEditor(typeof(AkRTPCPlayable))]
public class AkRTPCPlayableInspector : UnityEditor.Editor
{
	private UnityEditor.SerializedProperty Behaviour;
	private UnityEditor.SerializedProperty overrideTrackObject;
	private AkRTPCPlayable playable;
	private UnityEditor.SerializedProperty RTPCObject;
	private UnityEditor.SerializedProperty setRTPCGlobally;

	public void OnEnable()
	{
		playable = target as AkRTPCPlayable;

		setRTPCGlobally = serializedObject.FindProperty("setRTPCGlobally");
		overrideTrackObject = serializedObject.FindProperty("overrideTrackObject");
		RTPCObject = serializedObject.FindProperty("RTPCObject");
		Behaviour = serializedObject.FindProperty("template");

		if (playable != null && playable.OwningClip != null)
			playable.OwningClip.displayName = playable.Parameter.Name;
	}

	public override void OnInspectorGUI()
	{
		UnityEditor.EditorGUILayout.HelpBox(AkSoundEngine.Deprecation_2019_2_0, UnityEditor.MessageType.Warning);

		serializedObject.Update();

		UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

		using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
		{
			if (setRTPCGlobally != null)
			{
				UnityEditor.EditorGUILayout.PropertyField(setRTPCGlobally, new UnityEngine.GUIContent("Set RTPC Globally: "));
				if (!setRTPCGlobally.boolValue)
				{
					if (overrideTrackObject != null)
					{
						UnityEditor.EditorGUILayout.PropertyField(overrideTrackObject,
							new UnityEngine.GUIContent("Override Track Object: "));
						if (overrideTrackObject.boolValue)
						{
							if (RTPCObject != null)
								UnityEditor.EditorGUILayout.PropertyField(RTPCObject, new UnityEngine.GUIContent("RTPC Object: "));
						}
					}
				}
			}
		}

		if (Behaviour != null)
			UnityEditor.EditorGUILayout.PropertyField(Behaviour, new UnityEngine.GUIContent("Animated Value: "), true);

		if (playable != null && playable.OwningClip != null)
			playable.OwningClip.displayName = playable.Parameter.Name;

		serializedObject.ApplyModifiedProperties();
	}
}

#endif // !AK_DISABLE_TIMELINE
#endif //#if UNITY_EDITOR