#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// <summary>
///     This component makes a GameObject aware of AkRoom components.
///     When using Spatial Audio rooms, all emitters and the spatial audio listener should have this component.
/// </summary>

[UnityEngine.AddComponentMenu("Wwise/Spatial Audio/AkRoomAwareObject")]
[UnityEngine.RequireComponent(typeof(AkGameObj))]
[UnityEngine.DisallowMultipleComponent]
public class AkRoomAwareObject : UnityEngine.MonoBehaviour
{
	private static readonly System.Collections.Generic.Dictionary<UnityEngine.Collider, AkRoomAwareObject> ColliderToRoomAwareObjectMap = new System.Collections.Generic.Dictionary<UnityEngine.Collider, AkRoomAwareObject>();

	public static AkRoomAwareObject GetAkRoomAwareObjectFromCollider(UnityEngine.Collider collider)
	{
		AkRoomAwareObject roomAwareObject = null;
		return ColliderToRoomAwareObjectMap.TryGetValue(collider, out roomAwareObject) ? roomAwareObject : null;
	}

	public UnityEngine.Collider m_Collider;
	private readonly AkRoom.PriorityList roomPriorityList = new AkRoom.PriorityList();

	private void Awake()
	{
		m_Collider = GetComponent<UnityEngine.Collider>();
		if (m_Collider != null)
			ColliderToRoomAwareObjectMap.Add(m_Collider, this);
	}

	private void OnEnable()
	{
		AkRoomAwareManager.RegisterRoomAwareObject(this);

		for (int i = 0; i < roomPriorityList.Count; ++i)
			roomPriorityList[i].TryEnter(this);
	}

	private void OnDisable()
	{
		for (int i = 0; i < roomPriorityList.Count; ++i)
			roomPriorityList[i].Exit(this);

		AkRoomAwareManager.UnregisterRoomAwareObject(this);

		SetGameObjectInRoom(null);
	}

	private void OnDestroy()
	{
		ColliderToRoomAwareObjectMap.Remove(m_Collider);
	}

	public void SetGameObjectInHighestPriorityActiveAndEnabledRoom()
	{
		SetGameObjectInRoom(roomPriorityList.GetHighestPriorityActiveAndEnabledRoom());
	}

	private void SetGameObjectInRoom(AkRoom room)
	{
		AkSoundEngine.SetGameObjectInRoom(gameObject, room == null ? AkRoom.INVALID_ROOM_ID : room.GetID());
	}

	/// <summary>
	///     Called when entering a room.
	/// </summary>
	/// <param name="room">The room.</param>
	public void EnteredRoom(AkRoom room)
	{
		roomPriorityList.Add(room);
	}

	/// <summary>
	///     Called when exiting a room.
	/// </summary>
	/// <param name="room">The room.</param>
	public void ExitedRoom(AkRoom room)
	{
		roomPriorityList.Remove(room);
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.