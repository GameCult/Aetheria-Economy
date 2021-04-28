#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEditor.CustomEditor(typeof(AkRoomAwareObject))]
public class AkRoomAwareObjectInspector : UnityEditor.Editor
{
	private bool hideDefaultHandle;
	private UnityEditor.SerializedProperty listeners;
	private AkRoomAwareObject m_AkRoomAwareObject;

	private void OnEnable()
	{
		m_AkRoomAwareObject = target as AkRoomAwareObject;
	}

	public override void OnInspectorGUI()
	{
		RigidbodyCheck(m_AkRoomAwareObject.gameObject);
		ColliderCheck(m_AkRoomAwareObject.gameObject);
	}

	public static void ColliderCheck(UnityEngine.GameObject gameObject)
	{
		if (AkWwiseEditorSettings.Instance.ShowSpatialAudioWarningMsg)
		{
			var collider = gameObject.GetComponent<UnityEngine.Collider>();
			if (collider == null || !collider.enabled)
			{
				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
					UnityEditor.EditorGUILayout.HelpBox(
						"Interactions between AkRoomAwareObject and AkRoom require a Collider component on the object.",
						UnityEditor.MessageType.Error);
				}
			}
		}
	}

	public static void RigidbodyCheck(UnityEngine.GameObject gameObject)
	{
		if (AkWwiseEditorSettings.Instance.ShowMissingRigidBodyWarning && gameObject.GetComponent<UnityEngine.Rigidbody>() == null)
		{
			UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

			using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
			{
				UnityEditor.EditorGUILayout.HelpBox(
					"Interactions between AkRoomAwareObject and AkRoom require a Rigidbody component on the object or the room.",
					UnityEditor.MessageType.Warning);

				if (UnityEngine.GUILayout.Button("Add Rigidbody"))
				{
					var rb = UnityEditor.Undo.AddComponent<UnityEngine.Rigidbody>(gameObject);
					rb.useGravity = false;
					rb.isKinematic = true;
				}
			}
		}
	}
}
#endif