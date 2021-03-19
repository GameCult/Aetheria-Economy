#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Contains C# functions exposed from the Wwise C++ API.
/// 
/// The AkSoundEngine class contains functions converted to C# from the following C++ namespaces: 
/// - AK::Monitor
/// - AK::MusicEngine
/// - AK::SoundEngine
/// - AK::SoundEngine::DynamicDialogue
/// - AK::SoundEngine::Query
/// - AK::SpatialAudio
public partial class AkSoundEngine
{
	#region String Marshalling

	/// <summary>
	///     Converts "char*" C-strings to C# strings.
	/// </summary>
	/// <param name="ptr">"char*" memory pointer passed to C# as an IntPtr.</param>
	/// <returns>Converted string.</returns>
	public static string StringFromIntPtrString(System.IntPtr ptr)
	{
		return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
	}

	/// <summary>
	///     Converts "wchar_t*" C-strings to C# strings.
	/// </summary>
	/// <param name="ptr">"wchar_t*" memory pointer passed to C# as an IntPtr.</param>
	/// <returns>Converted string.</returns>
	public static string StringFromIntPtrWString(System.IntPtr ptr)
	{
		return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
	}
	#endregion

	#region GameObject Hash Function

	/// <summary>
	///     The type for hash functions used to convert a Unity Game Object into an integer.
	/// </summary>
	/// <param name="gameObject">The Unity Game Object.</param>
	/// <returns>The AkGameObjectID used by the sound engine.</returns>
	public delegate ulong GameObjectHashFunction(UnityEngine.GameObject gameObject);

	private static ulong InternalGameObjectHash(UnityEngine.GameObject gameObject)
	{
		return gameObject == null ? AK_INVALID_GAME_OBJECT : (ulong) gameObject.GetInstanceID();
	}

	/// <summary>
	///     The user assignable hash function used to convert a Unity Game Object into an AkGameObjectID used by the sound
	///     engine. Used by GetAkGameObjectID().
	/// </summary>
	public static GameObjectHashFunction GameObjectHash
	{
		set { gameObjectHash = value == null ? InternalGameObjectHash : value; }
	}

	private static GameObjectHashFunction gameObjectHash = InternalGameObjectHash;

	/// <summary>
	///     The hash function used to convert a Unity Game Object into an AkGameObjectID used by the sound engine.
	/// </summary>
	public static ulong GetAkGameObjectID(UnityEngine.GameObject gameObject)
	{
		return gameObjectHash(gameObject);
	}

	#endregion

	#region Registration Functions

	/// <summary>
	///     Registers a Unity Game Object with an ID obtained from GetAkGameObjectID().
	/// </summary>
	/// <param name="gameObject">The Unity Game Object.</param>
	/// <returns></returns>
	public static AKRESULT RegisterGameObj(UnityEngine.GameObject gameObject)
	{
		var id = GetAkGameObjectID(gameObject);
		var res = (AKRESULT) AkSoundEnginePINVOKE.CSharp_RegisterGameObjInternal(id);
		PostRegisterGameObjUserHook(res, gameObject, id);
		return res;
	}

	/// <summary>
	///     Registers a Unity Game Object with an ID obtained from GetAkGameObjectID().
	/// </summary>
	/// <param name="gameObject">The Unity Game Object.</param>
	/// <param name="name">The name that is visible in the Wwise Profiler.</param>
	/// <returns></returns>
	public static AKRESULT RegisterGameObj(UnityEngine.GameObject gameObject, string name)
	{
		var id = GetAkGameObjectID(gameObject);
		var res = (AKRESULT) AkSoundEnginePINVOKE.CSharp_RegisterGameObjInternal_WithName(id, name);
		PostRegisterGameObjUserHook(res, gameObject, id);
		return res;
	}

	/// <summary>
	///     Unregisters a Unity Game Object with an ID obtained from GetAkGameObjectID().
	/// </summary>
	/// <param name="gameObject">The Unity Game Object.</param>
	/// <returns></returns>
	public static AKRESULT UnregisterGameObj(UnityEngine.GameObject gameObject)
	{
		var id = GetAkGameObjectID(gameObject);
		var res = (AKRESULT) AkSoundEnginePINVOKE.CSharp_UnregisterGameObjInternal(id);
		PostUnregisterGameObjUserHook(res, gameObject, id);
		return res;
	}

	#endregion

	#region Helper Functions

	public static string WwiseVersion
	{
		get
		{
			var majorMinor = GetMajorMinorVersion();
			var subminorBuild = GetSubminorBuildVersion();
			var major = majorMinor >> 16;
			var minor = majorMinor & 0xFFFF;
			var subMinor = subminorBuild >> 16;
			var build = subminorBuild & 0xFFFF;
			return string.Format("{0}.{1}.{2} Build {3}", major, minor, subMinor, build);
		}
	}

	public static AKRESULT SetObjectPosition(UnityEngine.GameObject gameObject, UnityEngine.Transform transform)
	{
		var id = GetAkGameObjectID(gameObject);
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetObjectPosition(id, transform.position, transform.forward, transform.up);
	}

	public static AKRESULT SetObjectPosition(UnityEngine.GameObject gameObject, float posX, float posY, float posZ,
		float frontX, float frontY, float frontZ, float topX, float topY, float topZ)
	{
		var id = GetAkGameObjectID(gameObject);
		var position = new UnityEngine.Vector3(posX, posY, posZ);
		var forward = new UnityEngine.Vector3(frontX, frontY, frontZ);
		var up = new UnityEngine.Vector3(topX, topY, topZ);
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetObjectPosition(id, position, forward, up);
	}

	#endregion

	#region User Hooks

	public static void PreGameObjectAPICall(UnityEngine.GameObject gameObject, ulong id)
	{
		PreGameObjectAPICallUserHook(gameObject, id);
	}

	/// <summary>
	///     User hook called within all Wwise integration functions that receive GameObjects and do not perform
	///     (un)registration. This is called
	///     before values are sent to the native plugin code. An example use could be to register game objects that were not
	///     previously registered.
	/// </summary>
	/// <param name="gameObject">The GameObject being processed.</param>
	/// <param name="id">The ulong returned from GameObjectHash that represents this GameObject in Wwise.</param>
	static partial void PreGameObjectAPICallUserHook(UnityEngine.GameObject gameObject, ulong id);

	/// <summary>
	///     User hook called after RegisterGameObj(). An example use could be to add the id and gameObject to a dictionary upon
	///     AK_Success.
	/// </summary>
	/// <param name="result">The result from calling RegisterGameObj() on gameObject.</param>
	/// <param name="gameObject">The GameObject that RegisterGameObj() was called on.</param>
	/// <param name="id">The ulong returned from GameObjectHash that represents this GameObject in Wwise.</param>
	static partial void PostRegisterGameObjUserHook(AKRESULT result, UnityEngine.GameObject gameObject, ulong id);

	/// <summary>
	///     User hook called after UnregisterGameObj(). An example use could be to remove the id and gameObject from a
	///     dictionary upon AK_Success.
	/// </summary>
	/// <param name="result">The result from calling UnregisterGameObj() on gameObject.</param>
	/// <param name="gameObject">The GameObject that UnregisterGameObj() was called on.</param>
	/// <param name="id">The ulong returned from GameObjectHash that represents this GameObject in Wwise.</param>
	static partial void PostUnregisterGameObjUserHook(AKRESULT result, UnityEngine.GameObject gameObject, ulong id);

	#endregion

	#region Deprecation Strings
	public const string Deprecation_2018_1_2 = "This functionality is deprecated as of Wwise v2018.1.2 and will be removed in a future release.";
	public const string Deprecation_2018_1_6 = "This functionality is deprecated as of Wwise v2018.1.6 and will be removed in a future release.";
	public const string Deprecation_2019_2_0 = "This functionality is deprecated as of Wwise v2019.2.0 and will be removed in a future release.";
	public const string Deprecation_2019_2_2 = "This functionality is deprecated as of Wwise v2019.2.2 and will be removed in a future release.";
	#endregion
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.