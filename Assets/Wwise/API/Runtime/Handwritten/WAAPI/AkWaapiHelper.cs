//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Base class for Json serializable objects. 
/// Implements implicit cast to string using UnityEngine.JsonUtility.ToJson.
/// </summary>
[System.Serializable]
public class JsonSerializable
{
	public static implicit operator string(JsonSerializable o) => UnityEngine.JsonUtility.ToJson(o);
}

/// <summary>
/// Abstract base class for WAAPI command arguments.
/// </summary>
[System.Serializable]
public abstract class Args : JsonSerializable
{
}

/// <summary>
/// WAAPI arguments containing a WAQL string.
/// </summary>
[System.Serializable]
public class WaqlArgs : Args
{
	public string waql;
	public WaqlArgs(string query)
	{
		waql = query;
	}
}

/// <summary>
/// WAAPI arguments containing an object identfier.
/// </summary>
[System.Serializable]
public class ArgsObject : Args
{
	public string @object;

	public ArgsObject(string objectId)
	{
		@object = objectId;
	}
}


/// <summary>
/// WAAPI arguments containing an object identfier and new value used when renaming an object.
/// </summary>
[System.Serializable]
public class ArgsRename : Args
{
	public string @object;
	public string value;

	public ArgsRename(string objectId, string value)
	{
		@object = objectId;
		this.value = value;
	}
}

/// <summary>
/// WAAPI arguments containing an object identfier and name used when deleting an object.
/// </summary>
[System.Serializable]
public class ArgsDisplayName : Args
{
	public string displayName;

	public ArgsDisplayName(string displayName)
	{
		this.displayName = displayName;
	}
}

/// <summary>
/// WAAPI arguments containing an object identfier and command field.
/// Used by AkWaapiUtilities.SelectObjectInAuthoringAsync().
/// </summary>
[System.Serializable]
public class ArgsCommand : Args
{
	public string[] @objects;
	public string command;
	public ArgsCommand(string c, string[] objectIds)
	{
		command = c;
		objects = objectIds;
	}
}

/// <summary>
/// WAAPI arguments containing an Event identfier and transport ID.
/// Used to toggle Event playback.
/// </summary>
[System.Serializable]
public class ArgsPlay : Args
{
	public string action;
	public int transport;
	public ArgsPlay(string a, int t) { action = a; transport = t; }
}

/// <summary>
/// WAAPI arguments containing a transport ID.
/// Used to specify transports in transport-specific commands.
/// </summary>
[System.Serializable]
public class ArgsTransport : Args
{
	public int transport;
	public ArgsTransport(int t) { transport = t; }
}

/// <summary>
/// Abstract base class for WAAPI command options.
/// </summary>
[System.Serializable]
public class Options : JsonSerializable
{

}

/// <summary>
/// WAAPI options to specify the names of fields to return in a WAAPI request returning WwiseObjects.
/// </summary>
[System.Serializable]
public class ReturnOptions : Options
{
	public string[] @return;

	public ReturnOptions(string [] infokeys)
	{
		@return = infokeys;
	}
}

/// <summary>
/// WAAPI options used to specify the transport ID when subscribing.
/// </summary>
[System.Serializable]
public class TransportOptions : Options
{
	public int transport;
	public TransportOptions(int id)
	{
		transport = id;
	}
}

/// <summary>
/// Used to deserialize the response from an ak.wwise.core.transport.create command. 
/// Contains the transport ID.
/// </summary>
[System.Serializable]
public class ReturnTransport : JsonSerializable
{
	public int transport;
}

/// <summary>
/// Used to deserialize transport-state information receieved from the transport.stateChanged topic.
/// </summary>
[System.Serializable]
public class TransportState : JsonSerializable
{
	public string gameObject;
	public string state;
	public string @object;
	public int transport;
}

/// <summary>
/// Used to deserialize WAAPI error messages.
/// </summary>
[System.Serializable]
public class ErrorMessage : JsonSerializable
{
	public string message;
	public ErrorDetails details;
}

/// <summary>
/// Used to deserialize details in WAAPI error messages.
/// </summary>
[System.Serializable]
public class ErrorDetails : JsonSerializable
{
	public string [] reasons;
	public string procedureUri;
}

/// <summary>
/// Class used to deserialize a WAAPI response containing Wwise objects.
/// </summary>
[System.Serializable]
public class ReturnWwiseObjects : JsonSerializable
{
	public List<WwiseObjectInfoJsonObject> @return;
}

/// <summary>
/// Generic class to deserialize a WAAPI response containing Wwise objects with custom return options.
/// </summary>
[System.Serializable]
public class ReturnWwiseObjects<T> : JsonSerializable
{
	public List<T> @return;
}

/// <summary>
/// Class used to deserialize selected Wwise objects published on the ak.wwise.ui.selectionChanged topic.
/// </summary>
[System.Serializable]
public class SelectedWwiseObjects : JsonSerializable
{
	public List<WwiseObjectInfoJsonObject> objects;
}

/// <summary>
/// Used to deserialize information published on the ak.wwise.core.@object.nameChanged topic.
/// </summary>
[System.Serializable]
public class WwiseRenameInfo : JsonSerializable
{
	public WwiseObjectInfoJsonObject @object;
	public string newName;
	public string oldName;

	public WwiseObjectInfo objectInfo;
	public void ParseInfo()
	{
		objectInfo = @object;
	}
}


/// <summary>
/// Used to deserialize information published on the ak.wwise.core.@object.childAdded and ak.wwise.core.@object.childRemoved topics.
/// </summary>
[System.Serializable]
public class WwiseChildModifiedInfo : JsonSerializable
{
	public WwiseObjectInfoJsonObject parent;
	public WwiseObjectInfoJsonObject child;

	public WwiseObjectInfo parentInfo;
	public WwiseObjectInfo childInfo;

	public void ParseInfo()
	{
		parentInfo = parent;
		childInfo = child;
	}
}

/// <summary>
/// Used to deserialize information from a request for a Wwise object.
/// Implements an implicit cast to WwiseObjectInfo.
/// </summary>
[System.Serializable]
public class WwiseObjectInfoJsonObject
{
	public string id;
	public WwiseObjectInfoParent parent;
	public string name;
	public string type;
	public int childrenCount;
	public string path;
	public string filePath;
	public string workunitType;
	public string soundbankBnkFilePath;

	public static implicit operator WwiseObjectInfo(WwiseObjectInfoJsonObject info)
	{
		return ToObjectInfo(info);
	}

	public static WwiseObjectInfo ToObjectInfo(WwiseObjectInfoJsonObject info)
	{
		var type = info.type == null ? "" : info.type;
		var wutype = info.workunitType == null ? "" : info.workunitType;
		var objectType = WaapiHelper.GetWwiseObjectTypeFromString(type.ToLower(), wutype.ToLower());
		var parentID = info.parent.id == null ? System.Guid.Empty : System.Guid.Parse(info.parent.id);
		var objectGuid = info.id == null ? System.Guid.Empty : System.Guid.Parse(info.id);

		return new WwiseObjectInfo
		{
			objectGUID = objectGuid,
			name = info.name,
			type = objectType,
			childrenCount = info.childrenCount,
			path = info.path,
			workUnitType = wutype,
			parentID = parentID,
			filePath = info.filePath,
			soundbankBnkFilePath = info.soundbankBnkFilePath
		};
	}
}

/// <summary>
/// Contains the GUID of the returned object's parent.
/// </summary>
[System.Serializable]
public class WwiseObjectInfoParent
{
	public string id;
}


/// <summary>
/// Class containing the information returned by a WAAPI request for an object.
/// </summary>
[System.Serializable]
public struct WwiseObjectInfo
{
	public System.Guid objectGUID;
	public System.Guid parentID;
	public string name;
	public WwiseObjectType type;
	public int childrenCount;
	public string path;
	public string workUnitType;
	public string filePath;
	public string soundbankBnkFilePath;
}

/// <summary>
/// Contains a helper function GetWwiseObjectTypeFromString.
/// </summary>
public static class WaapiHelper
{
	public static WwiseObjectType GetWwiseObjectTypeFromString(string typeString, string workUnitType)
	{
		if (!WaapiKeywords.typeStringDict.ContainsKey(typeString))
			return WwiseObjectType.None;

		if (workUnitType != string.Empty)
		{
			if (workUnitType == "folder")
			{
				return WaapiKeywords.typeStringDict["physicalfolder"];
			}

			return WaapiKeywords.typeStringDict[typeString];
		}
		return WaapiKeywords.typeStringDict[typeString];
	}
}

/// <summary>
/// Contains fields for specific WAAPI keywords.
/// </summary>
public class WaapiKeywords
{
	public const string ACTION = "action";
	public const string ANCESTORS = "ancestors";
	public const string AT = "@";
	public const string AUX_BUSSES = "auxBusses";
	public const string BACK_SLASH = "\\";
	public const string BANK_DATA = "bankData";
	public const string BANK_INFO = "bankInfo";
	public const string CHILD = "child";
	public const string CHILDREN = "children";
	public const string CHILDREN_COUNT = "childrenCount";
	public const string CLASSID = "classId";
	public const string COMMAND = "command";
	public const string DATA = "data";
	public const string DELETE_ITEMS = "Delete Items";
	public const string DESCENDANTS = "descendants";
	public const string DISPLAY_NAME = "displayName";
	public const string DRAG_DROP_ITEMS = "Drag Drop Items";
	public const string EVENT = "event";
	public const string EVENTS = "events";
	public const string FILEPATH = "filePath";
	public const string FIND_IN_PROJECT_EXPLORER = "FindInProjectExplorerSyncGroup1";
	public const string FOLDER = "Folder";
	public const string FROM = "from";
	public const string ID = "id";
	public const string INCLUSIONS = "inclusions";
	public const string INFO_FILE = "infoFile";
	public const string IS_CONNECTED = "isConnected";
	public const string LANGUAGE = "language";
	public const string LANGUAGES = "languages";
	public const string MAX = "max";
	public const string MAX_RADIUS_ATTENUATION = "audioSource:maxRadiusAttenuation";
	public const string MESSSAGE = "message";
	public const string MIN = "min";
	public const string NAME = "name";
	public const string NAMECONTAINS = "name:contains";
	public const string NEW = "new";
	public const string NEW_NAME = "newName";
	public const string NOTES = "notes";
	public const string OBJECT = "object";
	public const string OBJECTS = "objects";
	public const string OF_TYPE = "ofType";
	public const string OLD_NAME = "oldName";
	public const string PARENT = "parent";
	public const string PATH = "path";
	public const string PHYSICAL_FOLDER = "PhysicalFolder";
	public const string PLATFORM = "platform";
	public const string PLATFORMS = "platforms";
	public const string PLAY = "play";
	public const string PLAYING = "playing";
	public const string PLAYSTOP = "playStop";
	public const string PLUGININFO_OPTIONS = "pluginInfo";
	public const string PLUGININFO_RESPONSE = "PluginInfo";
	public const string PROJECT = "Project";
	public const string PROPERTY = "property";
	public const string RADIUS = "radius";
	public const string RANGE = "range";
	public const string REBUILD = "rebuild";
	public const string REDO = "Redo";
	public const string RESTRICTION = "restriction";
	public const string RETURN = "return";
	public const string SEARCH = "search";
	public const string SELECT = "select";
	public const string SIZE = "size";
	public const string SKIP_LANGUAGES = "skipLanguages";
	public const string SOUNDBANK = "soundbank";
	public const string SOUNDBANKS = "soundbanks";
	public const string STATE = "state";
	public const string STOP = "stop";
	public const string STOPPED = "stopped";
	public const string STRUCTURE = "structure";
	public const string TRANSFORM = "transform";
	public const string TRANSPORT = "transport";
	public const string TYPE = "type";
	public const string UI = "ui";
	public const string UNDO = "Undo";
	public const string VALUE = "value";
	public const string VOLUME = "Volume";
	public const string WHERE = "where";
	public const string WORKUNIT_TYPE = "workunit:type";
	public const string OPEN_SOUNDBANK_FOLDER = "OpenContainingFolderSoundbank";
	public const string OPEN_WORKUNIT_FOLDER = "OpenContainingFolderWorkUnit";
	public const string OPEN_WAV_FOLDER = "OpenContainingFolderWAV";

	/// <summary>
	/// Maps WwiseObjectType to strings.
	/// </summary>
	public static ReadOnlyDictionary<WwiseObjectType, string> WwiseObjectTypeStrings = new ReadOnlyDictionary<WwiseObjectType, string>(new Dictionary<WwiseObjectType, string>()
		{
			{WwiseObjectType.None, "None"},
			{WwiseObjectType.AuxBus, "AuxiliaryBus"},
			{WwiseObjectType.Bus, "Bus"},
			{WwiseObjectType.Event, "Event"},
			{WwiseObjectType.Folder, "Folder"},
			{WwiseObjectType.PhysicalFolder, "PhysicalFolder"},
			{WwiseObjectType.Project, "Project"},
			{WwiseObjectType.Soundbank, "SoundBank"},
			{WwiseObjectType.State, "State"},
			{WwiseObjectType.StateGroup, "StateGroup"},
			{WwiseObjectType.Switch, "Switch"},
			{WwiseObjectType.SwitchGroup, "SwitchGroup"},
			{WwiseObjectType.WorkUnit, "WorkUnit"},
			{WwiseObjectType.GameParameter, "Game Parametr"},
			{WwiseObjectType.Trigger, "Trigger"},
			{WwiseObjectType.AcousticTexture, "AcousticTexture"}
		});

	/// <summary>
	/// Maps root folder names to displayed strings.
	/// </summary>
	public static ReadOnlyDictionary<string, string> FolderDisplaynames = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()
	{
		{"Master-Mixer Hierarchy", "Auxiliary Busses" },
		{ "Events", "Events"},
		{ "States", "States"},
		{ "SoundBanks", "SoundBanks"},
		{ "Switches", "Switches"},
		{ "Virtual Acoustics", "Virtual Acoustics"},
	});

	/// <summary>
	/// Maps strings to WwiseObjectType.
	/// </summary>
	public static ReadOnlyDictionary<string, WwiseObjectType> typeStringDict = new ReadOnlyDictionary<string, WwiseObjectType>(new Dictionary<string, WwiseObjectType>()
	{
		["auxbus"] = WwiseObjectType.AuxBus,
		["bus"] = WwiseObjectType.Bus,
		["event"] = WwiseObjectType.Event,
		["folder"] = WwiseObjectType.Folder,
		["physicalfolder"] = WwiseObjectType.PhysicalFolder,
		["soundbank"] = WwiseObjectType.Soundbank,
		["project"] = WwiseObjectType.Project,
		["state"] = WwiseObjectType.State,
		["stategroup"] = WwiseObjectType.StateGroup,
		["switch"] = WwiseObjectType.Switch,
		["switchgroup"] = WwiseObjectType.SwitchGroup,
		["workunit"] = WwiseObjectType.WorkUnit,
		["gameparameter"] = WwiseObjectType.GameParameter,
		["trigger"] = WwiseObjectType.Trigger,
		["acoustictexture"] = WwiseObjectType.AcousticTexture
	});
}