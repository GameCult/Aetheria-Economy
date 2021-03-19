#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
///@brief (DEPRECATED) This script is deprecated as of 2019.2. Early reflections, Diffraction and Room Reverb can all be enabled per sound in the Sound Property Editor of the Authoring.
/// @details Some functionalities were moved to different components. See the AkEarlyReflections and AkSpatialAudioDebugDraw components for more details.
public class AkSpatialAudioEmitter : UnityEngine.MonoBehaviour
#if UNITY_EDITOR
	, AK.Wwise.IMigratable
#endif
{
	[UnityEngine.Header("Early Reflections")]
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, the early reflections auxiliary bus can be set per sound, in the Authoring tool, or per game object, with the AkEarlyReflections component.")]
	/// (DEPRECATED) As of 2019.2, the early reflections auxiliary bus can be set per sound, in the Authoring tool, or per game object, with the AkEarlyReflections component.
	public AK.Wwise.AuxBus reflectAuxBus = new AK.Wwise.AuxBus();
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, the Reflection Max Path Length is set by the sound's Attenuation Max Distance value in the Authoring tool.")]
	/// (DEPRECATED) As of 2019.2, the Reflection Max Path Length is set by the sound's Attenuation Max Distance value in the Authoring tool.
	public float reflectionMaxPathLength = 1000;
	[UnityEngine.Range(0, 1)]
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, the early reflections send volume can be set per sound, in the Authoring tool, or for all sunds playing on a game object, with the AkEarlyReflections component.")]
	/// (DEPRECATED) As of 2019.2, the early reflections send volume can be set per sound, in the Authoring tool, or for all sunds playing on a game object, with the AkEarlyReflections component.
	public float reflectionsAuxBusGain = 1;	
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, the Reflection Order is set in the Spatial Audio Initialization Settings.")]
	/// (DEPRECATED) As of 2019.2, the Reflection Order is set in the Spatial Audio Initialization Settings.
	public uint reflectionsOrder = 1;

	[UnityEngine.Header("Rooms")]
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, the Room Reverb Aux Bus Gain is set by the Game-Defined Auxiliary Sends Volume in the Sound Property Editor in the Authoring tool.")]
	/// (DEPRECATED) As of 2019.2, the Room Reverb Aux Bus Gain is set by the Game-Defined Auxiliary Sends Volume in the Sound Property Editor in the Authoring tool.
	public float roomReverbAuxBusGain = 1;

	[UnityEngine.Header("Geometric Diffraction")]
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool.")]
	/// (DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool.
	public uint diffractionMaxEdges = 0;
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool.")]
	/// (DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool.
	public uint diffractionMaxPaths = 0;
	[UnityEngine.Tooltip("(DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool.")]
	/// (DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool.
	public uint diffractionMaxPathLength = 0;

#if UNITY_EDITOR
	[UnityEngine.Header("Debug Draw")]

	[UnityEngine.Tooltip("(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.")]
	/// (DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.
	public bool drawFirstOrderReflections = false;
	[UnityEngine.Tooltip("(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.")]
	/// (DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.
	public bool drawSecondOrderReflections = false;
	[UnityEngine.Tooltip("(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.")]
	/// (DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.
	public bool drawHigherOrderReflections = false;
	[UnityEngine.Tooltip("(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.")]
	/// (DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component.
	public bool drawDiffractionPaths = false;

	[UnityEditor.CustomEditor(typeof(AkSpatialAudioEmitter))]
	[UnityEditor.CanEditMultipleObjects]
	private class Editor : UnityEditor.Editor
	{
		private UnityEditor.SerializedProperty reflectAuxBus;
		private UnityEditor.SerializedProperty reflectionMaxPathLength;
		private UnityEditor.SerializedProperty reflectionsAuxBusGain;
		private UnityEditor.SerializedProperty reflectionsOrder;

		private UnityEditor.SerializedProperty roomReverbAuxBusGain;

		private UnityEditor.SerializedProperty diffractionMaxEdges;
		private UnityEditor.SerializedProperty diffractionMaxPaths;
		private UnityEditor.SerializedProperty diffractionMaxPathLength;

		private UnityEditor.SerializedProperty drawFirstOrderReflections;
		private UnityEditor.SerializedProperty drawSecondOrderReflections;
		private UnityEditor.SerializedProperty drawHigherOrderReflections;
		private UnityEditor.SerializedProperty drawDiffractionPaths;

		public void OnEnable()
		{
			reflectAuxBus = serializedObject.FindProperty("reflectAuxBus");
			reflectionMaxPathLength = serializedObject.FindProperty("reflectionMaxPathLength");
			reflectionsAuxBusGain = serializedObject.FindProperty("reflectionsAuxBusGain");
			reflectionsOrder = serializedObject.FindProperty("reflectionsOrder");

			roomReverbAuxBusGain = serializedObject.FindProperty("roomReverbAuxBusGain");

			diffractionMaxEdges = serializedObject.FindProperty("diffractionMaxEdges");
			diffractionMaxPaths = serializedObject.FindProperty("diffractionMaxPaths");
			diffractionMaxPathLength = serializedObject.FindProperty("diffractionMaxPathLength");

			drawFirstOrderReflections = serializedObject.FindProperty("drawFirstOrderReflections");
			drawSecondOrderReflections = serializedObject.FindProperty("drawSecondOrderReflections");
			drawHigherOrderReflections = serializedObject.FindProperty("drawHigherOrderReflections");
			drawDiffractionPaths = serializedObject.FindProperty("drawDiffractionPaths");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var wasEnabled = UnityEngine.GUI.enabled;
			UnityEngine.GUI.enabled = false;

			UnityEditor.EditorGUILayout.PropertyField(reflectAuxBus, new UnityEngine.GUIContent("Reflect Aux Bus", "(DEPRECATED) As of 2019.2, the early reflections auxiliary bus can be set per sound, in the Authoring tool, or per game object, with the AkEarlyReflections component."));
			UnityEditor.EditorGUILayout.PropertyField(reflectionMaxPathLength, new UnityEngine.GUIContent("Reflection Max Path Length", "(DEPRECATED) As of 2019.2, the Reflection Max Path Length is set by the sound's Attenuation Max Distance value in the Authoring tool."));
			UnityEditor.EditorGUILayout.PropertyField(reflectionsAuxBusGain, new UnityEngine.GUIContent("Reflections Aux Bus Gain", "(DEPRECATED) As of 2019.2, the early reflections send volume can be set per sound, in the Authoring tool, or for all sunds playing on a game object, with the AkEarlyReflections component."));
			UnityEditor.EditorGUILayout.PropertyField(reflectionsOrder, new UnityEngine.GUIContent("Reflections Order", "(DEPRECATED) As of 2019.2, the Reflection Order is set in the Spatial Audio Initialization Settings."));

			UnityEditor.EditorGUILayout.PropertyField(roomReverbAuxBusGain, new UnityEngine.GUIContent("Room Reverb Aux Bus Gain", "(DEPRECATED) As of 2019.2, the Room Reverb Aux Bus Gain is set by the Game-Defined Auxiliary Sends Volume in the Sound Property Editor in the Authoring tool."));

			UnityEditor.EditorGUILayout.PropertyField(diffractionMaxEdges, new UnityEngine.GUIContent("Diffraction Max Edges", "(DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool."));
			UnityEditor.EditorGUILayout.PropertyField(diffractionMaxPaths, new UnityEngine.GUIContent("Diffraction Max Paths", "(DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool."));
			UnityEditor.EditorGUILayout.PropertyField(diffractionMaxPathLength, new UnityEngine.GUIContent("Diffraction Max Path Length", "(DEPRECATED) As of 2019.2, diffraction is enabled in the Sound Property Editor in the Authoring tool."));

			UnityEditor.EditorGUILayout.PropertyField(drawFirstOrderReflections, new UnityEngine.GUIContent("Draw First Order Reflections", "(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component."));
			UnityEditor.EditorGUILayout.PropertyField(drawSecondOrderReflections, new UnityEngine.GUIContent("Draw Second Order Reflections", "(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component."));
			UnityEditor.EditorGUILayout.PropertyField(drawHigherOrderReflections, new UnityEngine.GUIContent("Draw Higher Order Reflections", "(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component."));
			UnityEditor.EditorGUILayout.PropertyField(drawDiffractionPaths, new UnityEngine.GUIContent("Draw Diffraction Paths", "(DEPRECATED) Spatial Audio Debug Drawing were moved to the new AkSpatialAudioDebugDraw component."));

			UnityEngine.GUI.enabled = wasEnabled;

			// button to add AkEarlyReflections
			bool bShowButton = false;
			foreach (var obj in targets)
			{
				AkSpatialAudioEmitter spatialAudioEmitter = obj as AkSpatialAudioEmitter;
				if (spatialAudioEmitter.gameObject.GetComponent<AkEarlyReflections>() == null)
					bShowButton = true;
			}

			if (bShowButton)
			{
				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
					UnityEditor.EditorGUILayout.HelpBox(
						"If you want to keep setting Early Reflections per game object, consider adding an AkEarlyReflections component.",
						UnityEditor.MessageType.Warning);

					if (UnityEngine.GUILayout.Button("Add AkEarlyReflections"))
					{
						foreach (var obj in targets)
						{
							AkSpatialAudioEmitter spatialAudioEmitter = obj as AkSpatialAudioEmitter;
							if (spatialAudioEmitter.gameObject.GetComponent<AkEarlyReflections>() == null)
							{
								var er = UnityEditor.Undo.AddComponent<AkEarlyReflections>(spatialAudioEmitter.gameObject);
								er.reflectionsAuxBus.ObjectReference = spatialAudioEmitter.reflectAuxBus.ObjectReference;
								er.reflectionsVolume = spatialAudioEmitter.reflectionsAuxBusGain;
							}
						}
					}
				}
			}

			// button to add AkSpatialAudioDebugDraw
			bShowButton = false;
			foreach (var obj in targets)
			{
				AkSpatialAudioEmitter spatialAudioEmitter = obj as AkSpatialAudioEmitter;
				if (spatialAudioEmitter.gameObject.GetComponent<AkSpatialAudioDebugDraw>() == null)
					bShowButton = true;
			}

			if (bShowButton)
			{
				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
					UnityEditor.EditorGUILayout.HelpBox(
						"For debugging purposes, early reflection and diffraction paths can be shown in the scene with the AkSpatialAudioDebugDraw component.",
						UnityEditor.MessageType.Warning);

					if (UnityEngine.GUILayout.Button("Add AkSpatialAudioDebugDraw"))
					{
						foreach (var obj in targets)
						{
							AkSpatialAudioEmitter spatialAudioEmitter = obj as AkSpatialAudioEmitter;
							if (spatialAudioEmitter.gameObject.GetComponent<AkSpatialAudioDebugDraw>() == null)
							{
								var dd = UnityEditor.Undo.AddComponent<AkSpatialAudioDebugDraw>(spatialAudioEmitter.gameObject);
								dd.drawFirstOrderReflections = spatialAudioEmitter.drawFirstOrderReflections;
								dd.drawSecondOrderReflections = spatialAudioEmitter.drawSecondOrderReflections;
								dd.drawHigherOrderReflections = spatialAudioEmitter.drawHigherOrderReflections;
								dd.drawDiffractionPaths = spatialAudioEmitter.drawDiffractionPaths;
							}
						}
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}

	#region WwiseMigration
	bool AK.Wwise.IMigratable.Migrate(UnityEditor.SerializedObject obj)
    {
		if (!AkUtilities.IsMigrationRequired(AkUtilities.MigrationStep.NewScriptableObjectFolder_v2019_2_0))
			return false;

		UnityEditor.Undo.AddComponent<AkRoomAwareObject>(gameObject);

		return true;
	}
	#endregion
#endif
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.