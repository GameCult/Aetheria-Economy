#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
[UnityEngine.AddComponentMenu("Wwise/AkEarlyReflections")]
[UnityEngine.RequireComponent(typeof(AkGameObj))]
[UnityEngine.DisallowMultipleComponent]
///@brief Set an early reflections auxiliary bus and send volume for a particular game object.
public class AkEarlyReflections : UnityEngine.MonoBehaviour
{
	[UnityEngine.Tooltip("The early reflections auxiliary bus for all sounds playing on this particular game object. The early reflection auxiliary bus specified in the authoring tool has precedence.")]
	/// The early reflections auxiliary bus for this particular game object.
	/// Geometrical reflection calculation inside spatial audio is enabled for a game object if any sound playing on the game object has a valid early reflections aux bus specified in the authoring tool,
	/// or if an aux bus is specified via this parameter. The early reflection auxiliary bus specified in the authoring tool has precedence.
	/// Users may apply this function to avoid duplicating sounds in the actor-mixer hierarchy solely for the sake of specifying a unique early reflection bus, or in any situation where the same 
	/// sound should be played on different game objects with different early reflection aux buses (the early reflection bus must be left blank in the authoring tool if the user intends to specify it through the API).
	public AK.Wwise.AuxBus reflectionsAuxBus = new AK.Wwise.AuxBus();

	[UnityEngine.Range(0, 1)]
	[UnityEngine.Tooltip("The early reflections send volume for all sounds playing on this particular game object. It is combined with the early reflections volume specified in the authoring tool.")]
	/// The early reflections send volume for this particular game object.
	/// This parameter is used to control the volume of the early reflections send. It is combined with the early reflections volume specified in the authoring tool, and is applied to all sounds playing on the game object.
	/// Setting to 0.f will disable all reflection processing for this game object. Valid range 0.f-1.f.
	public float reflectionsVolume = 1;

	private void OnEnable()
	{
		if (reflectionsAuxBus != null)
			AkSoundEngine.SetEarlyReflectionsAuxSend(gameObject, reflectionsAuxBus.Id);

		AkSoundEngine.SetEarlyReflectionsVolume(gameObject, reflectionsVolume);
	}

	public void SetEarlyReflectionsVolume(float volume)
	{
		if (reflectionsVolume != volume)
		{
			AkSoundEngine.SetEarlyReflectionsVolume(gameObject, volume);
			reflectionsVolume = volume;
		}
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.