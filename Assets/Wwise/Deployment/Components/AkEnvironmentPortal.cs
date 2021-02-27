#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Use this component to define an area that straddles two different AkEnvironment's zones and allow mixing between both zones. \ref unity_use_AkEvironment_AkEvironmentPortal
[UnityEngine.AddComponentMenu("Wwise/AkEnvironmentPortal")]
[UnityEngine.RequireComponent(typeof(UnityEngine.BoxCollider))]
[UnityEngine.ExecuteInEditMode]
public class AkEnvironmentPortal : UnityEngine.MonoBehaviour
{
	public const int MAX_ENVIRONMENTS_PER_PORTAL = 2;

	///The axis used to find the contribution of each environment
	public UnityEngine.Vector3 axis = UnityEngine.Vector3.right;

	///The array is already sorted by position.
	///The first environment is on the negative side of the portal(opposite to the direction of the chosen axis)
	///The second environment is on the positive side of the portal
	public AkEnvironment[] environments = new AkEnvironment[MAX_ENVIRONMENTS_PER_PORTAL];

	private UnityEngine.BoxCollider m_BoxCollider;
	private UnityEngine.BoxCollider BoxCollider
	{
		get
		{
			if (!m_BoxCollider)
				m_BoxCollider = GetComponent<UnityEngine.BoxCollider>();

			return m_BoxCollider;
		}
	}

	public bool EnvironmentsShareAuxBus
	{
		get
		{
			if (environments[0] == null)
				return environments[1] == null;

			if (environments[1] == null)
				return false;

			if (environments[0].data == null)
				return environments[1].data == null;

			if (environments[1].data == null)
				return false;

			return environments[0].data.Id == environments[1].data.Id;
		}
	}

	public float GetAuxSendValueForPosition(UnityEngine.Vector3 in_position, int index)
	{
		//total length of the portal in the direction of axis
		var portalLength = UnityEngine.Vector3.Dot(UnityEngine.Vector3.Scale(BoxCollider.size, transform.lossyScale), axis);

		//transform axis to world coordinates 
		var axisWorld = UnityEngine.Vector3.Normalize(transform.rotation * axis);

		//Get distance form left side of the portal(opposite to the direction of axis) to the game object in the direction of axisWorld
		var dist = UnityEngine.Vector3.Dot(in_position - (transform.position - portalLength * 0.5f * axisWorld), axisWorld);
		dist = UnityEngine.Mathf.Clamp(dist, 0, portalLength);

		//calculate value of the environment referred by index 
		if (index == 0)
			return (portalLength - dist) * (portalLength - dist) / (portalLength * portalLength);

		return dist * dist / (portalLength * portalLength);
	}


#if UNITY_EDITOR
	/// This enables us to detect intersections between portals and environments in the editor
	[System.Serializable]
	public class EnvListWrapper
	{
		public System.Collections.Generic.List<AkEnvironment> list = new System.Collections.Generic.List<AkEnvironment>();
	}

	/// Unity can't serialize an array of list so we wrap the list in a serializable class 
	public EnvListWrapper[] envList =
	{
		new EnvListWrapper(), //All environments on the negative side of each portal(opposite to the direction of the chosen axis)
		new EnvListWrapper() //All environments on the positive side of each portal(same direction as the chosen axis)
	};
#endif
}

#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.