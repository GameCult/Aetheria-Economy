#if !AK_DISABLE_TIMELINE
[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
[UnityEditor.CustomEditor(typeof(AkRTPCTrack))]
public class AkRTPCTrackInspector : UnityEditor.Editor
{
	private UnityEditor.SerializedProperty Parameter;

	public void OnEnable()
	{
		Parameter = serializedObject.FindProperty("Parameter");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

		using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
		{
			UnityEditor.EditorGUILayout.PropertyField(Parameter, new UnityEngine.GUIContent("Parameter: "));
		}

		serializedObject.ApplyModifiedProperties();
	}
}
#endif //!AK_DISABLE_TIMELINE
