#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
[UnityEngine.AddComponentMenu("Wwise/Spatial Audio/AkRadialEmitter")]
[UnityEngine.RequireComponent(typeof(AkGameObj))]
[UnityEngine.DisallowMultipleComponent]
/// @brief A radial emitter is for sounds that are not point sources, but instead originate from a region of space.
/// @details A radial emitter is described by an inner and outer radius. The radii are used in spread and distance calculations, simulating a radial sound source.
/// Since all game objects have a position and orientation, the position (center) and local axes are defined by the AkGameObj component required by this component.
public class AkRadialEmitter : UnityEngine.MonoBehaviour
{
	#region Fields
	[UnityEngine.Tooltip("Define the outer radius around each sound position to simulate a radial sound source. If the listener is outside the outer radius, the spread is defined by the area that the sphere takes in the listener field of view. When the listener intersects the outer radius, the spread is exactly 50%. When the listener is in between the inner and outer radius, the spread interpolates linearly from 50% to 100%.")]
	/// Define an outer radius around each sound position to simulate a radial sound source.
	/// The distance used for applying attenuation curves is taken as the distance between the listener and the point on the sphere, defined by the sound position and the outer radius, that is closest to the listener. 
	/// The spread for each sound position is calculated as follows:
	/// - If the listener is outside the outer radius, the spread is defined by the area that the sphere takes in the listener field of view. Specifically, this angle is calculated as 2.0*asinf( outerRadius / distance ), where distance is the distance between the listener and the sound position.
	///	- When the listener intersects the outer radius (the listener is exactly outerRadius units away from the sound position), the spread is exactly 50%.
	/// - When the listener is in between the inner and outer radius, the spread interpolates linearly from 50% to 100% as the listener transitions from the outer radius towards the inner radius.
	/// Note that transmission and diffraction calculations in Spatial Audio always use the center of the sphere (the position(s) passed into \c AK::SoundEngine::SetPosition or \c AK::SoundEngine::SetMultiplePositions) for raycasting. 
	/// To obtain accurate diffraction and transmission calculations for radial sources, where different parts of the volume may take different paths through or around geometry,
	/// it is necessary to pass multiple sound positions into \c AK::SoundEngine::SetMultiplePositions to allow the engine to 'sample' the area at different points.
	public float outerRadius = 0.0f;
	[UnityEngine.Tooltip("Define an inner radius around each sound position to simulate a radial sound source. If the listener is inside the inner radius, the spread is 100%.")]
	/// Define an inner radius around each sound position to simulate a radial sound source. If the listener is inside the inner radius, the spread is 100%.
	/// Note that transmission and diffraction calculations in Spatial Audio always use the center of the sphere (the position(s) passed into \c AK::SoundEngine::SetPosition or \c AK::SoundEngine::SetMultiplePositions) for raycasting. 
	/// To obtain accurate diffraction and transmission calculations for radial sources, where different parts of the volume may take different paths through or around geometry,
	/// it is necessary to pass multiple sound positions into \c AK::SoundEngine::SetMultiplePositions to allow the engine to 'sample' the area at different points.
	public float innerRadius = 0.0f;
	#endregion

	public void SetGameObjectOuterRadius(float in_outerRadius)
	{
		AkSoundEngine.SetGameObjectRadius(AkSoundEngine.GetAkGameObjectID(gameObject), in_outerRadius, innerRadius);
	}

	public void SetGameObjectInnerRadius(float in_innerRadius)
	{
		AkSoundEngine.SetGameObjectRadius(AkSoundEngine.GetAkGameObjectID(gameObject), outerRadius, in_innerRadius);
	}

	public void SetGameObjectRadius(float in_outerRadius, float in_innerRadius)
	{
		AkSoundEngine.SetGameObjectRadius(AkSoundEngine.GetAkGameObjectID(gameObject), in_outerRadius, in_innerRadius);
	}

	public void SetGameObjectRadius()
	{
		AkSoundEngine.SetGameObjectRadius(AkSoundEngine.GetAkGameObjectID(gameObject), outerRadius, innerRadius);
	}

	public void SetGameObjectRadius(UnityEngine.GameObject in_gameObject)
	{
		AkSoundEngine.SetGameObjectRadius(AkSoundEngine.GetAkGameObjectID(in_gameObject), outerRadius, innerRadius);
	}

	private void OnEnable()
	{
		SetGameObjectRadius();
	}

#if UNITY_EDITOR
	private void Update()
	{
		if (UnityEditor.EditorApplication.isPlaying)
			SetGameObjectRadius();
	}

	private void OnDrawGizmosSelected()
	{
		if (!enabled)
		{
			return;
		}

		AkAmbient Ambient = GetComponent<AkAmbient>();
		bool showSpheres = true;
		if (Ambient && Ambient.multiPositionTypeLabel == MultiPositionTypeLabel.Large_Mode)
			showSpheres = false;

		if (showSpheres)
		{
			UnityEngine.Color SphereColor = UnityEngine.Color.yellow;
			SphereColor.a = 0.25f;
			UnityEngine.Gizmos.color = SphereColor;

			UnityEngine.Gizmos.DrawSphere(gameObject.transform.position, innerRadius);
			UnityEngine.Gizmos.DrawSphere(gameObject.transform.position, outerRadius);
		}
	}

	[UnityEditor.CustomEditor(typeof(AkRadialEmitter))]
	public class AkRadialEmitterInspector : UnityEditor.Editor
	{
		private AkRadialEmitter m_AkRadialEmitter;

		private UnityEditor.SerializedProperty outerRadius;
		private UnityEditor.SerializedProperty innerRadius;

		private void OnEnable()
		{
			m_AkRadialEmitter = target as AkRadialEmitter;

			outerRadius = serializedObject.FindProperty("outerRadius");
			innerRadius = serializedObject.FindProperty("innerRadius");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (m_AkRadialEmitter.outerRadius < 0.0f)
				m_AkRadialEmitter.outerRadius = 0.0f;
			if (m_AkRadialEmitter.innerRadius < 0.0f)
				m_AkRadialEmitter.innerRadius = 0.0f;
			if (m_AkRadialEmitter.innerRadius > m_AkRadialEmitter.outerRadius)
				m_AkRadialEmitter.innerRadius = m_AkRadialEmitter.outerRadius;

			UnityEditor.EditorGUILayout.PropertyField(outerRadius);
			UnityEditor.EditorGUILayout.PropertyField(innerRadius);

			EventCheck(m_AkRadialEmitter.gameObject);

			serializedObject.ApplyModifiedProperties();
		}

		public static void EventCheck(UnityEngine.GameObject gameObject)
		{
			if (AkWwiseEditorSettings.Instance.ShowSpatialAudioWarningMsg && gameObject.GetComponent<AkEvent>() == null)
			{
				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
				{
					UnityEditor.EditorGUILayout.HelpBox(
						"Radial emitters are expected to emit sound. Add an AkEvent or an AkAmbient component to this game object.",
						UnityEditor.MessageType.Warning);

					if (UnityEngine.GUILayout.Button("Add AkEvent"))
						UnityEditor.Undo.AddComponent<AkEvent>(gameObject);

					if (UnityEngine.GUILayout.Button("Add AkAmbient"))
						UnityEditor.Undo.AddComponent<AkAmbient>(gameObject);
				}
			}
		}
	}
#endif
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.