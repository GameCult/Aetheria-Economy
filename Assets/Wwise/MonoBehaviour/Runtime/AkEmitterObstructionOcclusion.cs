#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEngine.AddComponentMenu("Wwise/AkEmitterObstructionOcclusion")]
[UnityEngine.RequireComponent(typeof(AkGameObj))]
/// @brief Completely obstructs/occludes the emitter of the current game object from its listeners if at least one object is between them.
public class AkEmitterObstructionOcclusion : AkObstructionOcclusion
{
	private AkGameObj m_gameObj;

	private void Awake()
	{
		InitIntervalsAndFadeRates();
		m_gameObj = GetComponent<AkGameObj>();
	}

	protected override void UpdateCurrentListenerList()
	{
		if (AkSpatialAudioListener.TheSpatialAudioListener != null && AkRoom.RoomCount > 0)
		{
			currentListenerList.Add(AkSpatialAudioListener.TheSpatialAudioListener);
			return;
		}

		if (m_gameObj.IsUsingDefaultListeners)
			currentListenerList.AddRange(AkAudioListener.DefaultListeners.ListenerList);
		currentListenerList.AddRange(m_gameObj.ListenerList);
	}

	protected override void SetObstructionOcclusion(
		System.Collections.Generic.KeyValuePair<AkAudioListener, ObstructionOcclusionValue> ObsOccPair)
	{
		if (AkSpatialAudioListener.TheSpatialAudioListener != null && AkRoom.RoomCount > 0)
			AkSoundEngine.SetObjectObstructionAndOcclusion(gameObject, ObsOccPair.Key.gameObject, ObsOccPair.Value.currentValue, 0.0f);
		else
			AkSoundEngine.SetObjectObstructionAndOcclusion(gameObject, ObsOccPair.Key.gameObject, 0.0f, ObsOccPair.Value.currentValue);
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.