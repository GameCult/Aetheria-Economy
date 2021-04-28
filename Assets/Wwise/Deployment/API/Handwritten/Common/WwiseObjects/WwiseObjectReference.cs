//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise objects as Unity assets.
public abstract class WwiseObjectReference : UnityEngine.ScriptableObject
{
	#region Serialized fields
	[AkShowOnly]
	[UnityEngine.SerializeField]
	private string objectName = string.Empty;

	[AkShowOnly]
	[UnityEngine.SerializeField]
	private uint id = AkSoundEngine.AK_INVALID_UNIQUE_ID;

	[AkShowOnly]
	[UnityEngine.SerializeField]
	private string guid = string.Empty;
	#endregion

	#region Properties
	/// <summary>
	/// The Wwise GUID which is represented by the ScriptableObject's asset file name.
	/// </summary>
	public System.Guid Guid
	{
		get { return string.IsNullOrEmpty(guid) ? System.Guid.Empty : new System.Guid(guid); }
	}

	/// <summary>
	/// The name of the Wwise object.
	/// </summary>
	public string ObjectName { get { return objectName; } }

	/// <summary>
	/// The display name for the Wwise object.
	/// </summary>
	public virtual string DisplayName { get { return ObjectName; } }

	/// <summary>
	/// The Wwise ID.
	/// </summary>
	public uint Id { get { return id; } }

	/// <summary>
	/// The type of the Wwise object resource (for example: Event, State or Switch).
	/// </summary>
	public abstract WwiseObjectType WwiseObjectType { get; }
	#endregion

#if UNITY_EDITOR
	#region Creation and File Management
	private static readonly System.Collections.Generic.Dictionary<WwiseObjectType, System.Type> m_WwiseObjectReferenceClasses
		= new System.Collections.Generic.Dictionary<WwiseObjectType, System.Type>
	{
		{ WwiseObjectType.AcousticTexture, typeof(WwiseAcousticTextureReference) },
		{ WwiseObjectType.AuxBus, typeof(WwiseAuxBusReference) },
		{ WwiseObjectType.Soundbank, typeof(WwiseBankReference) },
		{ WwiseObjectType.Event, typeof(WwiseEventReference) },
		{ WwiseObjectType.GameParameter, typeof(WwiseRtpcReference) },
		{ WwiseObjectType.StateGroup, typeof(WwiseStateGroupReference) },
		{ WwiseObjectType.State, typeof(WwiseStateReference) },
		{ WwiseObjectType.SwitchGroup, typeof(WwiseSwitchGroupReference) },
		{ WwiseObjectType.Switch, typeof(WwiseSwitchReference) },
		{ WwiseObjectType.Trigger, typeof(WwiseTriggerReference) },
	};

	private static WwiseObjectReference Create(WwiseObjectType wwiseObjectType)
	{
		System.Type type = null;
		if (m_WwiseObjectReferenceClasses.TryGetValue(wwiseObjectType, out type))
			return (WwiseObjectReference)CreateInstance(type);

		return CreateInstance<WwiseObjectReference>();
	}

	private static WwiseObjectReference FindExistingWwiseObject(WwiseObjectType wwiseObjectType, System.Guid guid, string path)
	{
		var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<WwiseObjectReference>(path);
		if (asset)
			return asset;

		System.Type type = null;
		if (!m_WwiseObjectReferenceClasses.TryGetValue(wwiseObjectType, out type))
			return null;

		var guids = UnityEditor.AssetDatabase.FindAssets("t:" + type.Name);
		foreach (var assetGuid in guids)
		{
			var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
			asset = UnityEditor.AssetDatabase.LoadAssetAtPath<WwiseObjectReference>(assetPath);
			if (asset && asset.WwiseObjectType == wwiseObjectType && asset.Guid == guid)
				return asset;
		}

		return null;
	}

	private static bool UpdateWwiseObjectData(WwiseObjectReference wwiseObjectReference, string name)
	{
		var id = AkUtilities.ShortIDGenerator.Compute(name);
		if (wwiseObjectReference.objectName == name && wwiseObjectReference.id == id)
			return false;

		wwiseObjectReference.objectName = name;
		wwiseObjectReference.id = id;
		return true;
	}

	public static string GetParentPath(WwiseObjectType wwiseObjectType)
	{
		return System.IO.Path.Combine(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath, wwiseObjectType.ToString());
	}

	public static string GetAssetFileName(System.Guid guid)
	{
		return guid.ToString().ToUpper() + ".asset";
	}

	public static WwiseObjectReference FindOrCreateWwiseObject(WwiseObjectType wwiseObjectType, string name, System.Guid guid)
	{
		var parentPath = GetParentPath(wwiseObjectType);
		var path = System.IO.Path.Combine(parentPath, GetAssetFileName(guid));
		var asset = FindExistingWwiseObject(wwiseObjectType, guid, path);
		var assetExists = asset != null;
		if (!assetExists)
		{
			AkUtilities.CreateFolder(parentPath);
			asset = Create(wwiseObjectType);
			asset.guid = guid.ToString().ToUpper();
		}

		var changed = UpdateWwiseObjectData(asset, name);
		if (!assetExists)
			UnityEditor.AssetDatabase.CreateAsset(asset, path);
		else if (changed)
			UnityEditor.EditorUtility.SetDirty(asset);

		return asset;
	}

	public static void UpdateWwiseObject(WwiseObjectType wwiseObjectType, string name, System.Guid guid)
	{
		var path = System.IO.Path.Combine(GetParentPath(wwiseObjectType), GetAssetFileName(guid));
		var asset = FindExistingWwiseObject(wwiseObjectType, guid, path);
		if (asset && UpdateWwiseObjectData(asset, name))
			UnityEditor.EditorUtility.SetDirty(asset);
	}

	public static void DeleteWwiseObject(WwiseObjectType wwiseObjectType, System.Guid guid)
	{
		var path = System.IO.Path.Combine(GetParentPath(wwiseObjectType), GetAssetFileName(guid));
		var guidString = UnityEditor.AssetDatabase.AssetPathToGUID(path);
		if (!string.IsNullOrEmpty(guidString))
			UnityEditor.AssetDatabase.DeleteAsset(path);
	}
	#endregion

	#region WwiseMigration
	private class WwiseObjectData
	{
		public string objectName;
	}

	private static System.Collections.Generic.Dictionary<WwiseObjectType, System.Collections.Generic.Dictionary<System.Guid, WwiseObjectData>> WwiseObjectDataMap
		= new System.Collections.Generic.Dictionary<WwiseObjectType, System.Collections.Generic.Dictionary<System.Guid, WwiseObjectData>>();

	public static void ClearWwiseObjectDataMap()
	{
		WwiseObjectDataMap.Clear();
	}

	public static void UpdateWwiseObjectDataMap(WwiseObjectType wwiseObjectType, string name, System.Guid guid)
	{
		System.Collections.Generic.Dictionary<System.Guid, WwiseObjectData> map = null;
		if (!WwiseObjectDataMap.TryGetValue(wwiseObjectType, out map))
		{
			map = new System.Collections.Generic.Dictionary<System.Guid, WwiseObjectData>();
			WwiseObjectDataMap.Add(wwiseObjectType, map);
		}

		WwiseObjectData data = null;
		if (!map.TryGetValue(guid, out data))
		{
			data = new WwiseObjectData();
			map.Add(guid, data);
		}

		data.objectName = name;
	}

	public static WwiseObjectReference GetWwiseObjectForMigration(WwiseObjectType wwiseObjectType, byte[] valueGuid, int id)
	{
		if (valueGuid == null)
		{
			return null;
		}

		System.Collections.Generic.Dictionary<System.Guid, WwiseObjectData> map = null;
		if (!WwiseObjectDataMap.TryGetValue(wwiseObjectType, out map) || map == null)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Cannot find WwiseObjectReferences of type <WwiseObjectType." + wwiseObjectType + ">.");
			return null;
		}

		var guid = System.Guid.Empty;
		WwiseObjectData data = null;

		try
		{
			guid = new System.Guid(valueGuid);
		}
		catch
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Invalid guid for WwiseObjectReference of type <WwiseObjectType." + wwiseObjectType + ">.");
			return null;
		}

		var formattedId = (uint)id;
		if (guid != System.Guid.Empty && !map.TryGetValue(guid, out data))
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Cannot find guid <" + guid.ToString() + "> for WwiseObjectReference of type <WwiseObjectType." + wwiseObjectType + "> in Wwise Project.");

			foreach (var pair in map)
			{
				if (AkUtilities.ShortIDGenerator.Compute(pair.Value.objectName) == formattedId)
				{
					guid = pair.Key;
					data = pair.Value;
					UnityEngine.Debug.LogWarning("WwiseUnity: Found guid <" + guid.ToString() + "> for <" + pair.Value.objectName + ">.");
					break;
				}
			}
		}

		if (data == null)
		{
			return null;
		}

		var objectReference = FindOrCreateWwiseObject(wwiseObjectType, data.objectName, guid);
		if (objectReference && objectReference.Id != formattedId)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: ID mismatch for WwiseObjectReference of type <WwiseObjectType." + wwiseObjectType + ">. Expected <" + formattedId + ">. Found <" + objectReference.Id + ">.");
		}

		return objectReference;
	}
	#endregion
#endif
}

/// @brief Represents Wwise group value objects (such as states and switches) as Unity assets.
public abstract class WwiseGroupValueObjectReference : WwiseObjectReference
{
	#region Properties
	/// <summary>
	/// The group object reference.
	/// </summary>
	public abstract WwiseObjectReference GroupObjectReference { get; set; }

	/// <summary>
	/// The type of the Wwise object resource (for example: Event, State or Switch).
	/// </summary>
	public abstract WwiseObjectType GroupWwiseObjectType { get; }

	/// <summary>
	/// The display name for the Wwise object.
	/// </summary>
	public override string DisplayName
	{
		get
		{
#if AK_DISPLAY_GROUP_TYPES_WITH_SINGLE_NAME
			return ObjectName;
#else
			var groupReference = GroupObjectReference;
			if (!groupReference)
				return ObjectName;

			return groupReference.ObjectName + " / " + ObjectName;
#endif // AK_DISPLAY_GROUP_TYPES_WITH_SINGLE_NAME
		}
	}
	#endregion

#if UNITY_EDITOR
	public void SetupGroupObjectReference(string name, System.Guid guid)
	{
		var objectReference = FindOrCreateWwiseObject(GroupWwiseObjectType, name, guid);
		if (objectReference != GroupObjectReference)
		{
			GroupObjectReference = objectReference;
			UnityEditor.EditorUtility.SetDirty(this);
		}
	}

	#region WwiseMigration
	public static WwiseGroupValueObjectReference GetWwiseObjectForMigration(WwiseObjectType wwiseObjectType, byte[] valueGuid, int id, byte[] groupGuid, int groupId)
	{
		var objectReference = GetWwiseObjectForMigration(wwiseObjectType, valueGuid, id);
		if (!objectReference)
			return null;

		var groupValueObjectReference = objectReference as WwiseGroupValueObjectReference;
		if (!groupValueObjectReference)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Not setting WwiseObjectReference since it is not a WwiseGroupValueObjectReference.");
			return null;
		}

		var groupObjectReference = GetWwiseObjectForMigration(groupValueObjectReference.GroupWwiseObjectType, groupGuid, groupId);
		if (!groupObjectReference)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Not setting WwiseObjectReference since its GroupObjectReference cannot be determined.");
			return null;
		}

		groupValueObjectReference.GroupObjectReference = groupObjectReference;
		UnityEditor.EditorUtility.SetDirty(groupValueObjectReference);
		return groupValueObjectReference;
	}
	#endregion
#endif
}
