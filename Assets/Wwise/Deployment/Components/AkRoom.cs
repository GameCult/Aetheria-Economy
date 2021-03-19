#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
[UnityEngine.AddComponentMenu("Wwise/AkRoom")]
[UnityEngine.RequireComponent(typeof(UnityEngine.Collider))]
[UnityEngine.DisallowMultipleComponent]
/// @brief An AkRoom is an enclosed environment that can only communicate to the outside/other rooms with AkRoomPortals
/// @details The AkRoom component uses its required Collider component to determine when AkRoomAwareObjects enter and exit the room using the OnTriggerEnter and OnTriggerExit callbacks.
public class AkRoom : AkTriggerHandler
{
	public static ulong INVALID_ROOM_ID = unchecked((ulong)-1.0f);

	public static ulong GetAkRoomID(AkRoom room)
	{
		return room == null ? INVALID_ROOM_ID : room.GetID();
	}

	public static int RoomCount { get; private set; }

	#region Fields
	[UnityEngine.Tooltip("Higher number has a higher priority")]
	/// In cases where a game object is in an area with two rooms, the higher priority room will be chosen for AK::SpatialAudio::SetGameObjectInRoom()
	/// The higher the priority number, the higher the priority of a room.
	public int priority = 0;

	/// The reverb auxiliary bus.
	public AK.Wwise.AuxBus reverbAuxBus = new AK.Wwise.AuxBus();

	[UnityEngine.Range(0, 1)]
	/// The reverb control value for the send to the reverb aux bus.
	public float reverbLevel = 1;

	[UnityEngine.Range(0, 1)]
	/// Occlusion level modeling transmission through walls.
	public float wallOcclusion = 1;

	/// Wwise Event to be posted on the room game object.
	public AK.Wwise.Event roomToneEvent = new AK.Wwise.Event();

	[UnityEngine.Range(0, 1)]
	[UnityEngine.Tooltip("Send level for sounds that are posted on the room game object; adds reverb to ambience and room tones. Valid range: (0.f-1.f). A value of 0 disables the aux send.")]
	/// Send level for sounds that are posted on the room game object; adds reverb to ambience and room tones. Valid range: (0.f-1.f). A value of 0 disables the aux send.
	public float roomToneAuxSend = 0;

	/// This is the list of AkRoomAwareObjects that have entered this AkRoom
	private System.Collections.Generic.List<AkRoomAwareObject> roomAwareObjectsEntered = new System.Collections.Generic.List<AkRoomAwareObject>();

	/// This is the list of AkRoomAwareObjects that have entered this AkRoom while it was inactive or disabled.
	private System.Collections.Generic.List<AkRoomAwareObject> roomAwareObjectsDetectedWhileDisabled = new System.Collections.Generic.List<AkRoomAwareObject>();

	#endregion

	public bool TryEnter(AkRoomAwareObject roomAwareObject)
	{
		if (roomAwareObject)
		{
			if (isActiveAndEnabled)
			{
				if(!roomAwareObjectsEntered.Contains(roomAwareObject))
					roomAwareObjectsEntered.Add(roomAwareObject);
				return true;
			}
			else
			{
				if (!roomAwareObjectsDetectedWhileDisabled.Contains(roomAwareObject))
					roomAwareObjectsDetectedWhileDisabled.Add(roomAwareObject);
				return false;
			}
		}
		return false;
	}

	public void Exit(AkRoomAwareObject roomAwareObject)
	{
		if (roomAwareObject)
		{
			roomAwareObjectsEntered.Remove(roomAwareObject);
			roomAwareObjectsDetectedWhileDisabled.Remove(roomAwareObject);
		}
	}

	/// Access the room's ID
	public ulong GetID()
	{
		return AkSoundEngine.GetAkGameObjectID(gameObject);
	}

	private void OnEnable()
	{
		var roomParams = new AkRoomParams
		{
			Up = transform.up,
			Front = transform.forward,

			ReverbAuxBus = reverbAuxBus.Id,
			ReverbLevel = reverbLevel,
			WallOcclusion = wallOcclusion,

			RoomGameObj_AuxSendLevelToSelf = roomToneAuxSend,
			RoomGameObj_KeepRegistered = roomToneEvent.IsValid(),
		};

		RoomCount++;
		AkSoundEngine.SetRoom(GetID(), roomParams, name);

		/// In case a room is disabled and re-enabled. 
		AkRoomManager.RegisterRoomUpdate(this);

		// if objects entered the room while disabled, enter them now
		for (var i = 0; i < roomAwareObjectsDetectedWhileDisabled.Count; ++i)
			AkRoomAwareManager.ObjectEnteredRoom(roomAwareObjectsDetectedWhileDisabled[i], this);

		roomAwareObjectsDetectedWhileDisabled.Clear();
	}

	private void OnDisable()
	{
		for (var i = 0; i < roomAwareObjectsEntered.Count; ++i)
		{
			roomAwareObjectsEntered[i].ExitedRoom(this);
			AkRoomAwareManager.RegisterRoomAwareObjectForUpdate(roomAwareObjectsEntered[i]);
			roomAwareObjectsDetectedWhileDisabled.Add(roomAwareObjectsEntered[i]);
		}
		roomAwareObjectsEntered.Clear();

		AkRoomManager.RegisterRoomUpdate(this);

		// stop sounds applied to the room game object
		AkSoundEngine.StopAll(gameObject);

		RoomCount--;
		AkSoundEngine.RemoveRoom(GetID());
	}

	private void OnTriggerEnter(UnityEngine.Collider in_other)
	{
		AkRoomAwareManager.ObjectEnteredRoom(in_other, this);
	}

	private void OnTriggerExit(UnityEngine.Collider in_other)
	{
		AkRoomAwareManager.ObjectExitedRoom(in_other, this);
	}

	public void PostRoomTone()
	{
		if (roomToneEvent.IsValid())
			AkSoundEngine.PostEventOnRoom(roomToneEvent.Id, GetID());
	}

	public override void HandleEvent(UnityEngine.GameObject in_gameObject)
	{
		PostRoomTone();
	}

	public class PriorityList
	{
		private static readonly CompareByPriority s_compareByPriority = new CompareByPriority();

		/// Contains all active rooms sorted by priority.
		private System.Collections.Generic.List<AkRoom> rooms = new System.Collections.Generic.List<AkRoom>();

		public ulong GetHighestPriorityActiveAndEnabledRoomID()
		{
			var room = GetHighestPriorityActiveAndEnabledRoom();
			return room == null ? INVALID_ROOM_ID : room.GetID();
		}
		public AkRoom GetHighestPriorityActiveAndEnabledRoom()
		{
			for (int i = 0; i < rooms.Count; i++)
			{
				if (rooms[i].isActiveAndEnabled)
					return rooms[i];
			}

			return null;
		}

		public int Count { get { return rooms.Count; } }

		public void Clear()
		{
			rooms.Clear();
		}

		public void Add(AkRoom room)
		{
			var index = BinarySearch(room);
			if (index < 0)
				rooms.Insert(~index, room);
		}

		public void Remove(AkRoom room)
		{
			rooms.Remove(room);
		}

		public bool Contains(AkRoom room)
		{
			return room && rooms.Contains(room);
		}

		public int BinarySearch(AkRoom room)
		{
			return room ? rooms.BinarySearch(room, s_compareByPriority) : -1;
		}

		public AkRoom this[int index]
		{
			get { return rooms[index]; }
		}

		private class CompareByPriority : System.Collections.Generic.IComparer<AkRoom>
		{
			public virtual int Compare(AkRoom a, AkRoom b)
			{
				var result = a.priority.CompareTo(b.priority);
				if (result == 0 && a != b)
					return 1;

				return -result; // inverted to have highest priority first
			}
		}
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.