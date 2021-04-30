#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEditor.CustomEditor(typeof(AkRoom))]
public class AkRoomInspector : UnityEditor.Editor
{
	private readonly AkUnityEventHandlerInspector m_PostEventHandlerInspector = new AkUnityEventHandlerInspector();

	private AkRoom m_AkRoom;
	private UnityEditor.SerializedProperty priority;
	private UnityEditor.SerializedProperty reverbAuxBus;
	private UnityEditor.SerializedProperty reverbLevel;
	private UnityEditor.SerializedProperty transmissionLoss;
	private UnityEditor.SerializedProperty roomToneEvent;
	private UnityEditor.SerializedProperty roomToneAuxSend;

	private void OnEnable()
	{
		m_PostEventHandlerInspector.Init(serializedObject, "triggerList", "Trigger On: ", false);

		m_AkRoom = target as AkRoom;

		reverbAuxBus = serializedObject.FindProperty("reverbAuxBus");
		reverbLevel = serializedObject.FindProperty("reverbLevel");
		transmissionLoss = serializedObject.FindProperty("transmissionLoss");
		priority = serializedObject.FindProperty("priority");
		roomToneEvent = serializedObject.FindProperty("roomToneEvent");
		roomToneAuxSend = serializedObject.FindProperty("roomToneAuxSend");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
		{
			UnityEditor.EditorGUILayout.PropertyField(reverbAuxBus);
			UnityEditor.EditorGUILayout.PropertyField(reverbLevel);
			UnityEditor.EditorGUILayout.PropertyField(transmissionLoss);
			UnityEditor.EditorGUILayout.PropertyField(priority);

			WetTransmissionCheck(m_AkRoom.gameObject);
		}

		UnityEditor.EditorGUILayout.LabelField("Room Tone", UnityEditor.EditorStyles.boldLabel);
		using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
		{
			m_PostEventHandlerInspector.OnGUI();
			UnityEditor.EditorGUILayout.PropertyField(roomToneEvent);
			UnityEditor.EditorGUILayout.PropertyField(roomToneAuxSend);
		}

		AkRoomAwareObjectInspector.RigidbodyCheck(m_AkRoom.gameObject);

		serializedObject.ApplyModifiedProperties();
	}

	public static void WetTransmissionCheck(UnityEngine.GameObject gameObject)
	{
		if (AkWwiseEditorSettings.Instance.ShowSpatialAudioWarningMsg &&
			gameObject.GetComponent<AkSurfaceReflector>() == null &&
			gameObject.GetComponent<UnityEngine.Mesh>() == null)
		{
			// wet transmission supports box, sphere, capsule and mesh colliders
			bool bSupported = false;
			if (gameObject.GetComponent<UnityEngine.BoxCollider>() != null ||
				gameObject.GetComponent<UnityEngine.SphereCollider>() != null ||
				gameObject.GetComponent<UnityEngine.CapsuleCollider>() != null ||
				gameObject.GetComponent<UnityEngine.MeshCollider>() != null ||
				gameObject.GetComponent<AkSurfaceReflector>() != null)
				bSupported = true;

			if (bSupported == false)
			{
				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
					UnityEditor.EditorGUILayout.HelpBox(
						"Wet Transmission is currently only supported with box, sphere, capsule and mesh colliders, or if the game object also has an enabled AkSurfaceReflector component.",
						UnityEditor.MessageType.Warning);
				}
			}
		}
	}
}
#endif