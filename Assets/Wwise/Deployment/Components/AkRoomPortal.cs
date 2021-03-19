#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
[UnityEngine.AddComponentMenu("Wwise/AkRoomPortal")]
[UnityEngine.RequireComponent(typeof(UnityEngine.BoxCollider))]
[UnityEngine.DisallowMultipleComponent]
/// @brief An AkRoomPortal can connect two AkRoom components together.
/// @details 
public class AkRoomPortal : AkTriggerHandler
{
	/// AkRoomPortals can only connect a maximum of 2 rooms.
	public const int MAX_ROOMS_PER_PORTAL = 2;
	public enum State
	{
		Closed,
		Open
	}

	public State initialState = State.Closed;

	private bool active = true;
	public bool portalActive
	{
		get
		{
			return active;
		}
		set
		{
			active = value;
			AkRoomManager.RegisterPortalUpdate(this);
		}
	}

	public System.Collections.Generic.List<int> closePortalTriggerList = new System.Collections.Generic.List<int>();

	private ulong frontRoomID { get { return IsRoomActive(frontRoom) ? frontRoom.GetID() : AkRoom.INVALID_ROOM_ID; } }
	private ulong backRoomID { get { return IsRoomActive(backRoom) ? backRoom.GetID() : AkRoom.INVALID_ROOM_ID; } }

	/// The front and back rooms connected by the portal.
	/// The first room is on the negative side of the portal(opposite to the direction of the local Z axis)
	/// The second room is on the positive side of the portal.
	[UnityEngine.SerializeField]
	private AkRoom[] rooms = new AkRoom[MAX_ROOMS_PER_PORTAL];

	/// The list of rooms sorted by priority in front and in the back of the portal
	private AkRoom.PriorityList[] roomList = { new AkRoom.PriorityList(), new AkRoom.PriorityList() };

	public AkRoom GetRoom(int index) { return rooms[index]; }

	public AkRoom frontRoom { get { return rooms[1]; } }
	public AkRoom backRoom { get { return rooms[0]; } }

	private AkTransform portalTransform;
	private UnityEngine.BoxCollider portalCollider;
	private bool portalSet = false;

	private void SetRoomPortal()
	{
		if (!enabled)
			return;

		if (IsValid)
		{
			portalTransform.Set(portalCollider.bounds.center, transform.forward, transform.up);
			var extent = UnityEngine.Vector3.Scale(portalCollider.size, transform.localScale) / 2;
			AkSoundEngine.SetRoomPortal(GetID(), portalTransform, extent, active, frontRoomID, backRoomID);
			portalSet = true;
		}
		else
		{
			UnityEngine.Debug.LogError(name + " has identical front and back rooms. It will not be sent to Spatial Audio.");
			if (portalSet)
				AkSoundEngine.RemovePortal(GetID());
			portalSet = false;
		}
	}

	public void UpdateRoomPortal()
	{
		UpdateRooms();
		SetRoomPortal();
	}

	public bool Overlaps(AkRoom room)
	{
		FindOverlappingRooms(roomList);

		for (int i = 0; i < MAX_ROOMS_PER_PORTAL; ++i)
		{
			if (roomList[i].Contains(room))
				return true;
		}

		return false;
	}

	public bool IsValid { get { return frontRoomID != backRoomID; } }

	/// Access the portal's ID
	public ulong GetID() { return (ulong)GetInstanceID(); }

	protected override void Awake()
	{
		portalCollider = GetComponent<UnityEngine.BoxCollider>();
		portalCollider.isTrigger = true;

		portalTransform = new AkTransform();

		// set portal in it's initial state
		portalActive = initialState != State.Closed;

		RegisterTriggers(closePortalTriggerList, ClosePortal);

		base.Awake();
	}

	protected override void Start()
	{
		base.Start();

		//Call the ClosePortal function if registered to the Start Trigger
		if (closePortalTriggerList.Contains(START_TRIGGER_ID))
			ClosePortal(null);
	}

	/// Opens the portal on trigger event
	public override void HandleEvent(UnityEngine.GameObject in_gameObject)
	{
		Open();
	}

	/// Closes the portal on trigger event
	public void ClosePortal(UnityEngine.GameObject in_gameObject)
	{
		Close();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		UnregisterTriggers(closePortalTriggerList, ClosePortal);
	}

	private void OnEnable()
	{
		UpdateRooms();
		AkRoomManager.RegisterPortal(this);
	}

	private void OnDisable()
	{
		AkRoomManager.UnregisterPortal(this);
		if (portalSet)
			AkSoundEngine.RemovePortal(GetID());
		portalSet = false;
	}

	private bool IsRoomActive(AkRoom in_room)
	{
		return in_room != null && in_room.isActiveAndEnabled;
	}

	public void Open()
	{
		portalActive = true;
	}

	public void Close()
	{
		portalActive = false;
	}

	public void FindOverlappingRooms(AkRoom.PriorityList[] roomList)
	{
		var portalCollider = gameObject.GetComponent<UnityEngine.BoxCollider>();
		if (portalCollider == null)
			return;

		// compute halfExtents and divide the local z extent by 2
		var halfExtentZ = portalCollider.size.z / 2;

		// move the center backward
		FillRoomList(UnityEngine.Vector3.forward * -halfExtentZ, roomList[0]);

		// move the center forward
		FillRoomList(UnityEngine.Vector3.forward * halfExtentZ, roomList[1]);
	}

	private void FillRoomList(UnityEngine.Vector3 position, AkRoom.PriorityList list)
	{
		list.Clear();

		position = transform.TransformPoint(position);
		var colliders = UnityEngine.Physics.OverlapSphere(position, 0, -1, UnityEngine.QueryTriggerInteraction.Collide);

		foreach (var collider in colliders)
		{
			var room = collider.gameObject.GetComponent<AkRoom>();
			if (room != null && !list.Contains(room))
				list.Add(room);
		}
	}

	public void UpdateRooms()
	{
		FindOverlappingRooms(roomList);

		bool wasUpdated = false;

		for (var i = 0; i < MAX_ROOMS_PER_PORTAL; ++i)
		{
			var room = roomList[i].GetHighestPriorityActiveAndEnabledRoom();

			if (room != rooms[i])
				wasUpdated = true;

			rooms[i] = room;
		}

		if (wasUpdated)
			AkRoomManager.RegisterPortalUpdate(this);
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (!enabled)
			return;

		UnityEngine.Gizmos.matrix = transform.localToWorldMatrix;

		var centreOffset = UnityEngine.Vector3.zero;
		var sizeMultiplier = UnityEngine.Vector3.one;
		var collider = GetComponent<UnityEngine.BoxCollider>();
		if (collider)
		{
			centreOffset = collider.center;
			sizeMultiplier = collider.size;
		}

		// color faces
		var faceCenterPos = new UnityEngine.Vector3[6];
		faceCenterPos[0] = UnityEngine.Vector3.Scale(new UnityEngine.Vector3(0.5f, 0.0f, 0.0f), sizeMultiplier);
		faceCenterPos[1] = UnityEngine.Vector3.Scale(new UnityEngine.Vector3(0.0f, 0.5f, 0.0f), sizeMultiplier);
		faceCenterPos[2] = UnityEngine.Vector3.Scale(new UnityEngine.Vector3(-0.5f, 0.0f, 0.0f), sizeMultiplier);
		faceCenterPos[3] = UnityEngine.Vector3.Scale(new UnityEngine.Vector3(0.0f, -0.5f, 0.0f), sizeMultiplier);
		faceCenterPos[4] = UnityEngine.Vector3.Scale(new UnityEngine.Vector3(0.0f, 0.0f, 0.5f), sizeMultiplier);
		faceCenterPos[5] = UnityEngine.Vector3.Scale(new UnityEngine.Vector3(0.0f, 0.0f, -0.5f), sizeMultiplier);

		var faceSize = new UnityEngine.Vector3[6];
		faceSize[0] = new UnityEngine.Vector3(0, 1, 1);
		faceSize[1] = new UnityEngine.Vector3(1, 0, 1);
		faceSize[2] = faceSize[0];
		faceSize[3] = faceSize[1];
		faceSize[4] = new UnityEngine.Vector3(1, 1, 0);
		faceSize[5] = faceSize[4];

		UnityEngine.Gizmos.color = new UnityEngine.Color32(255, 204, 0, 100);
		for (var i = 0; i < 4; i++)
		{
			UnityEngine.Gizmos.DrawCube(faceCenterPos[i] + centreOffset, UnityEngine.Vector3.Scale(faceSize[i], sizeMultiplier));
		}

		if (!portalActive)
        {
			UnityEngine.Gizmos.color = new UnityEngine.Color32(255, 204, 0, 30);
			UnityEngine.Gizmos.DrawCube(faceCenterPos[4] + centreOffset, UnityEngine.Vector3.Scale(faceSize[4], sizeMultiplier));
			UnityEngine.Gizmos.DrawCube(faceCenterPos[5] + centreOffset, UnityEngine.Vector3.Scale(faceSize[5], sizeMultiplier));
		}

		// draw line in the center of the portal
		var CornerCenterPos = faceCenterPos;
		CornerCenterPos[0].y += 0.5f * sizeMultiplier.y;
		CornerCenterPos[1].x -= 0.5f * sizeMultiplier.x;
		CornerCenterPos[2].y -= 0.5f * sizeMultiplier.y;
		CornerCenterPos[3].x += 0.5f * sizeMultiplier.x;

		UnityEngine.Gizmos.color = UnityEngine.Color.red;
		for (var i = 0; i < 4; i++)
			UnityEngine.Gizmos.DrawLine(CornerCenterPos[i] + centreOffset, CornerCenterPos[(i + 1) % 4] + centreOffset);
	}
#endif

	#region Obsolete
	[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
	public void SetRoom(int in_roomIndex, AkRoom in_room)
	{
		UnityEngine.Debug.LogFormat("SetRoom is deprecated. Highest priority, active and enabled room will be automatically chosen. Make sure room priorities and game object placements are correct.");
	}

	[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
	public void SetFrontRoom(AkRoom room)
	{
		UnityEngine.Debug.LogFormat("SetFrontRoom is deprecated. Highest priority, active and enabled room will be automatically chosen. Make sure room priorities and game object placements are correct.");
	}

	[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
	public void SetBackRoom(AkRoom room)
	{
		UnityEngine.Debug.LogFormat("SetBackRoom is deprecated. Highest priority, active and enabled room will be automatically chosen. Make sure room priorities and game object placements are correct.");
	}

	[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
	public void UpdateSoundEngineRoomIDs()
	{
		UpdateRoomPortal();
	}

	[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
	public void UpdateOverlappingRooms()
	{
		UpdateRooms();
	}
	#endregion
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.