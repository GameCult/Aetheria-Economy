#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2019 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief This manager tracks the AkRoomAwareObjects and the AkRooms in which they enter and exit.
/// @details At the end of the frame, the AkRoomAwareObject is set in the highest priority AkRoom in Spatial Audio.
public static class AkRoomAwareManager
{
	private static readonly System.Collections.Generic.HashSet<AkRoomAwareObject> m_RoomAwareObjects =
		new System.Collections.Generic.HashSet<AkRoomAwareObject>();

	private static readonly System.Collections.Generic.HashSet<AkRoomAwareObject> m_RoomAwareObjectToUpdate =
		new System.Collections.Generic.HashSet<AkRoomAwareObject>();

	public static void RegisterRoomAwareObject(AkRoomAwareObject roomAwareObject)
	{
		m_RoomAwareObjects.Add(roomAwareObject);
		RegisterRoomAwareObjectForUpdate(roomAwareObject);
	}

	public static void UnregisterRoomAwareObject(AkRoomAwareObject roomAwareObject)
	{
		m_RoomAwareObjects.Remove(roomAwareObject);
		m_RoomAwareObjectToUpdate.Remove(roomAwareObject);
	}

	public static void RegisterRoomAwareObjectForUpdate(AkRoomAwareObject roomAwareObject)
	{
		m_RoomAwareObjectToUpdate.Add(roomAwareObject);
	}

	public static void ObjectEnteredRoom(UnityEngine.Collider collider, AkRoom room)
	{
		if (!collider)
			return;

		ObjectEnteredRoom(AkRoomAwareObject.GetAkRoomAwareObjectFromCollider(collider), room);
	}

	public static void ObjectEnteredRoom(AkRoomAwareObject roomAwareObject, AkRoom room)
	{
		if (!roomAwareObject || !room)
			return;

		var enteredRoom = room.TryEnter(roomAwareObject);
		if (enteredRoom)
		{
			roomAwareObject.EnteredRoom(room);
			RegisterRoomAwareObjectForUpdate(roomAwareObject);
		}
	}

	public static void ObjectExitedRoom(UnityEngine.Collider collider, AkRoom room)
	{
		if (!collider)
			return;

		ObjectExitedRoom(AkRoomAwareObject.GetAkRoomAwareObjectFromCollider(collider), room);
	}

	public static void ObjectExitedRoom(AkRoomAwareObject roomAwareObject, AkRoom room)
	{
		if (!roomAwareObject || !room)
			return;

		room.Exit(roomAwareObject);
		roomAwareObject.ExitedRoom(room);
		RegisterRoomAwareObjectForUpdate(roomAwareObject);
	}

	public static void UpdateRoomAwareObjects()
	{
		foreach (var roomAwareObject in m_RoomAwareObjectToUpdate)
		{
			if (m_RoomAwareObjects.Contains(roomAwareObject))
				roomAwareObject.SetGameObjectInHighestPriorityActiveAndEnabledRoom();
		}
		m_RoomAwareObjectToUpdate.Clear();
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.