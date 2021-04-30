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
#if UNITY_EDITOR
	public static bool EditorIsSoundEngineLoaded { get; set; }
#endif

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
	public const string Deprecation_2021_1_0 = "This functionality is deprecated as of Wwise v2021.1.0 and will be removed in a future release.";
	#endregion

	#region GameObject wrappers
	public static uint DynamicSequenceOpen(UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie, AkDynamicSequenceType in_eDynamicSequenceType)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_DynamicSequenceOpen__SWIG_0(in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero, (int)in_eDynamicSequenceType);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint DynamicSequenceOpen(UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_DynamicSequenceOpen__SWIG_1(in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint DynamicSequenceOpen(UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{
			uint ret = AkSoundEnginePINVOKE.CSharp_DynamicSequenceOpen__SWIG_2(in_gameObjectID_id);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie, uint in_cExternals, AkExternalSourceInfoArray in_pExternalSources, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_0(in_eventID, in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero, in_cExternals, in_pExternalSources.GetBuffer(), in_PlayingID);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie, uint in_cExternals, AkExternalSourceInfoArray in_pExternalSources)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_1(in_eventID, in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero, in_cExternals, in_pExternalSources.GetBuffer());
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_2(in_eventID, in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_3(in_eventID, in_gameObjectID_id);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie, uint in_cExternals, AkExternalSourceInfoArray in_pExternalSources, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_4(in_pszEventName, in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero, in_cExternals, in_pExternalSources.GetBuffer(), in_PlayingID);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie, uint in_cExternals, AkExternalSourceInfoArray in_pExternalSources)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_5(in_pszEventName, in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero, in_cExternals, in_pExternalSources.GetBuffer());
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, uint in_uFlags, AkCallbackManager.EventCallback in_pfnCallback, object in_pCookie)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		in_pCookie = AkCallbackManager.EventCallbackPackage.Create(in_pfnCallback, in_pCookie, ref in_uFlags);
		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_6(in_pszEventName, in_gameObjectID_id, in_uFlags, in_uFlags != 0 ? (global::System.IntPtr)1 : global::System.IntPtr.Zero, in_pCookie != null ? (global::System.IntPtr)in_pCookie.GetHashCode() : global::System.IntPtr.Zero);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static uint PostEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{
			uint ret = AkSoundEnginePINVOKE.CSharp_PostEvent__SWIG_7(in_pszEventName, in_gameObjectID_id);
			AkCallbackManager.SetLastAddedPlayingID(ret);
			return ret;
		}
	}

	public static AKRESULT ExecuteActionOnEvent(uint in_eventID, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID, int in_uTransitionDuration, AkCurveInterpolation in_eFadeCurve, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_0(in_eventID, (int)in_ActionType, in_gameObjectID_id, in_uTransitionDuration, (int)in_eFadeCurve, in_PlayingID); }
	}

	public static AKRESULT ExecuteActionOnEvent(uint in_eventID, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID, int in_uTransitionDuration, AkCurveInterpolation in_eFadeCurve)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_1(in_eventID, (int)in_ActionType, in_gameObjectID_id, in_uTransitionDuration, (int)in_eFadeCurve); }
	}

	public static AKRESULT ExecuteActionOnEvent(uint in_eventID, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID, int in_uTransitionDuration)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_2(in_eventID, (int)in_ActionType, in_gameObjectID_id, in_uTransitionDuration); }
	}

	public static AKRESULT ExecuteActionOnEvent(uint in_eventID, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_3(in_eventID, (int)in_ActionType, in_gameObjectID_id); }
	}

	public static AKRESULT ExecuteActionOnEvent(string in_pszEventName, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID, int in_uTransitionDuration, AkCurveInterpolation in_eFadeCurve, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_5(in_pszEventName, (int)in_ActionType, in_gameObjectID_id, in_uTransitionDuration, (int)in_eFadeCurve, in_PlayingID); }
	}

	public static AKRESULT ExecuteActionOnEvent(string in_pszEventName, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID, int in_uTransitionDuration, AkCurveInterpolation in_eFadeCurve)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_6(in_pszEventName, (int)in_ActionType, in_gameObjectID_id, in_uTransitionDuration, (int)in_eFadeCurve); }
	}

	public static AKRESULT ExecuteActionOnEvent(string in_pszEventName, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID, int in_uTransitionDuration)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_7(in_pszEventName, (int)in_ActionType, in_gameObjectID_id, in_uTransitionDuration); }
	}

	public static AKRESULT ExecuteActionOnEvent(string in_pszEventName, AkActionOnEventType in_ActionType, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ExecuteActionOnEvent__SWIG_8(in_pszEventName, (int)in_ActionType, in_gameObjectID_id); }
	}

	public static AKRESULT PostMIDIOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, AkMIDIPostArray in_pPosts, ushort in_uNumPosts)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostMIDIOnEvent__SWIG_3(in_eventID, in_gameObjectID_id, in_pPosts.GetBuffer(), in_uNumPosts); }
	}

	public static AKRESULT StopMIDIOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_StopMIDIOnEvent__SWIG_1(in_eventID, in_gameObjectID_id); }
	}

	public static AKRESULT StopMIDIOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, uint in_playingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_StopMIDIOnEvent__SWIG_0(in_eventID, in_gameObjectID_id, in_playingID); }
	}

	public static AKRESULT SeekOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, int in_iPosition, bool in_bSeekToNearestMarker, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_0(in_eventID, in_gameObjectID_id, in_iPosition, in_bSeekToNearestMarker, in_PlayingID); }
	}

	public static AKRESULT SeekOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, int in_iPosition, bool in_bSeekToNearestMarker)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_1(in_eventID, in_gameObjectID_id, in_iPosition, in_bSeekToNearestMarker); }
	}

	public static AKRESULT SeekOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, int in_iPosition)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_2(in_eventID, in_gameObjectID_id, in_iPosition); }
	}

	public static AKRESULT SeekOnEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, int in_iPosition, bool in_bSeekToNearestMarker, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_3(in_pszEventName, in_gameObjectID_id, in_iPosition, in_bSeekToNearestMarker, in_PlayingID); }
	}

	public static AKRESULT SeekOnEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, int in_iPosition, bool in_bSeekToNearestMarker)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_4(in_pszEventName, in_gameObjectID_id, in_iPosition, in_bSeekToNearestMarker); }
	}

	public static AKRESULT SeekOnEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, int in_iPosition)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_5(in_pszEventName, in_gameObjectID_id, in_iPosition); }
	}

	public static AKRESULT SeekOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, float in_fPercent, bool in_bSeekToNearestMarker, uint in_PlayingID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_6(in_eventID, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker, in_PlayingID);
#else
		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_9(in_eventID, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker, in_PlayingID); }
#endif
	}

	public static AKRESULT SeekOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, float in_fPercent, bool in_bSeekToNearestMarker)
	{
		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_7(in_eventID, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker);
#else
		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_10(in_eventID, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker); }
#endif
	}

	public static AKRESULT SeekOnEvent(uint in_eventID, UnityEngine.GameObject in_gameObjectID, float in_fPercent)
	{
		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_8(in_eventID, in_gameObjectID_id, in_fPercent);
#else
		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_11(in_eventID, in_gameObjectID_id, in_fPercent); }
#endif
	}

	public static AKRESULT SeekOnEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, float in_fPercent, bool in_bSeekToNearestMarker, uint in_PlayingID)
	{
		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_9(in_pszEventName, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker, in_PlayingID);
#else
		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_12(in_pszEventName, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker, in_PlayingID); }
#endif
	}

	public static AKRESULT SeekOnEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, float in_fPercent, bool in_bSeekToNearestMarker)
	{
		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_10(in_pszEventName, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker);
#else
		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_13(in_pszEventName, in_gameObjectID_id, in_fPercent, in_bSeekToNearestMarker); }
#endif
	}

	public static AKRESULT SeekOnEvent(string in_pszEventName, UnityEngine.GameObject in_gameObjectID, float in_fPercent)
	{
		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR
		return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_11(in_pszEventName, in_gameObjectID_id, in_fPercent);
#else
		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SeekOnEvent__SWIG_14(in_pszEventName, in_gameObjectID_id, in_fPercent); }
#endif
	}

	public static void CancelEventCallbackGameObject(UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ AkSoundEnginePINVOKE.CSharp_CancelEventCallbackGameObject(in_gameObjectID_id); }
	}

	public static void StopAll(UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ AkSoundEnginePINVOKE.CSharp_StopAll__SWIG_0(in_gameObjectID_id); }
	}

	public static AKRESULT SendPluginCustomGameData(uint in_busID, UnityEngine.GameObject in_busObjectID, AkPluginType in_eType, uint in_uCompanyID, uint in_uPluginID, global::System.IntPtr in_pData, uint in_uSizeInBytes)
	{

		var in_busObjectID_id = AkSoundEngine.GetAkGameObjectID(in_busObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_busObjectID, in_busObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SendPluginCustomGameData(in_busID, in_busObjectID_id, (int)in_eType, in_uCompanyID, in_uPluginID, in_pData, in_uSizeInBytes); }
	}

	public static AKRESULT SetMultiplePositions(UnityEngine.GameObject in_GameObjectID, AkPositionArray in_pPositions, ushort in_NumPositions, AkMultiPositionType in_eMultiPositionType) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetMultiplePositions__SWIG_0(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), in_pPositions.m_Buffer, in_NumPositions, (int)in_eMultiPositionType); }

	public static AKRESULT SetMultiplePositions(UnityEngine.GameObject in_GameObjectID, AkPositionArray in_pPositions, ushort in_NumPositions) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetMultiplePositions__SWIG_1(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), in_pPositions.m_Buffer, in_NumPositions); }

	public static AKRESULT SetMultiplePositions(UnityEngine.GameObject in_GameObjectID, AkChannelEmitterArray in_pPositions, ushort in_NumPositions, AkMultiPositionType in_eMultiPositionType) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetMultiplePositions__SWIG_2(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), in_pPositions.m_Buffer, in_NumPositions, (int)in_eMultiPositionType); }

	public static AKRESULT SetMultiplePositions(UnityEngine.GameObject in_GameObjectID, AkChannelEmitterArray in_pPositions, ushort in_NumPositions) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetMultiplePositions__SWIG_3(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), in_pPositions.m_Buffer, in_NumPositions); }

	public static AKRESULT SetScalingFactor(UnityEngine.GameObject in_GameObjectID, float in_fAttenuationScalingFactor) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetScalingFactor(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), in_fAttenuationScalingFactor); }


	public static AKRESULT AddListener(UnityEngine.GameObject in_emitterGameObj, UnityEngine.GameObject in_listenerGameObj)
	{

		var in_emitterGameObj_id = AkSoundEngine.GetAkGameObjectID(in_emitterGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_emitterGameObj, in_emitterGameObj_id);


		var in_listenerGameObj_id = AkSoundEngine.GetAkGameObjectID(in_listenerGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_listenerGameObj, in_listenerGameObj_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_AddListener(in_emitterGameObj_id, in_listenerGameObj_id); }
	}

	public static AKRESULT RemoveListener(UnityEngine.GameObject in_emitterGameObj, UnityEngine.GameObject in_listenerGameObj)
	{

		var in_emitterGameObj_id = AkSoundEngine.GetAkGameObjectID(in_emitterGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_emitterGameObj, in_emitterGameObj_id);


		var in_listenerGameObj_id = AkSoundEngine.GetAkGameObjectID(in_listenerGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_listenerGameObj, in_listenerGameObj_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_RemoveListener(in_emitterGameObj_id, in_listenerGameObj_id); }
	}

	public static AKRESULT AddDefaultListener(UnityEngine.GameObject in_listenerGameObj)
	{

		var in_listenerGameObj_id = AkSoundEngine.GetAkGameObjectID(in_listenerGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_listenerGameObj, in_listenerGameObj_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_AddDefaultListener(in_listenerGameObj_id); }
	}

	public static AKRESULT RemoveDefaultListener(UnityEngine.GameObject in_listenerGameObj)
	{

		var in_listenerGameObj_id = AkSoundEngine.GetAkGameObjectID(in_listenerGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_listenerGameObj, in_listenerGameObj_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_RemoveDefaultListener(in_listenerGameObj_id); }
	}

	public static AKRESULT ResetListenersToDefault(UnityEngine.GameObject in_emitterGameObj)
	{

		var in_emitterGameObj_id = AkSoundEngine.GetAkGameObjectID(in_emitterGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_emitterGameObj, in_emitterGameObj_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetListenersToDefault(in_emitterGameObj_id); }
	}

	public static AKRESULT SetListenerSpatialization(UnityEngine.GameObject in_uListenerID, bool in_bSpatialized, AkChannelConfig in_channelConfig, float[] in_pVolumeOffsets)
	{

		var in_uListenerID_id = AkSoundEngine.GetAkGameObjectID(in_uListenerID);
		AkSoundEngine.PreGameObjectAPICall(in_uListenerID, in_uListenerID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetListenerSpatialization__SWIG_0(in_uListenerID_id, in_bSpatialized, AkChannelConfig.getCPtr(in_channelConfig), in_pVolumeOffsets); }
	}

	public static AKRESULT SetListenerSpatialization(UnityEngine.GameObject in_uListenerID, bool in_bSpatialized, AkChannelConfig in_channelConfig)
	{

		var in_uListenerID_id = AkSoundEngine.GetAkGameObjectID(in_uListenerID);
		AkSoundEngine.PreGameObjectAPICall(in_uListenerID, in_uListenerID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetListenerSpatialization__SWIG_1(in_uListenerID_id, in_bSpatialized, AkChannelConfig.getCPtr(in_channelConfig)); }
	}

	public static AKRESULT SetRTPCValue(uint in_rtpcID, float in_value, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve, bool in_bBypassInternalValueInterpolation)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_0(in_rtpcID, in_value, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve, in_bBypassInternalValueInterpolation); }
	}

	public static AKRESULT SetRTPCValue(uint in_rtpcID, float in_value, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_1(in_rtpcID, in_value, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve); }
	}

	public static AKRESULT SetRTPCValue(uint in_rtpcID, float in_value, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_2(in_rtpcID, in_value, in_gameObjectID_id, in_uValueChangeDuration); }
	}

	public static AKRESULT SetRTPCValue(uint in_rtpcID, float in_value, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_3(in_rtpcID, in_value, in_gameObjectID_id); }
	}

	public static AKRESULT SetRTPCValue(string in_pszRtpcName, float in_value, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve, bool in_bBypassInternalValueInterpolation)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_5(in_pszRtpcName, in_value, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve, in_bBypassInternalValueInterpolation); }
	}

	public static AKRESULT SetRTPCValue(string in_pszRtpcName, float in_value, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_6(in_pszRtpcName, in_value, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve); }
	}

	public static AKRESULT SetRTPCValue(string in_pszRtpcName, float in_value, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_7(in_pszRtpcName, in_value, in_gameObjectID_id, in_uValueChangeDuration); }
	}

	public static AKRESULT SetRTPCValue(string in_pszRtpcName, float in_value, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetRTPCValue__SWIG_8(in_pszRtpcName, in_value, in_gameObjectID_id); }
	}

	public static AKRESULT ResetRTPCValue(uint in_rtpcID, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve, bool in_bBypassInternalValueInterpolation)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_0(in_rtpcID, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve, in_bBypassInternalValueInterpolation); }
	}

	public static AKRESULT ResetRTPCValue(uint in_rtpcID, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_1(in_rtpcID, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve); }
	}

	public static AKRESULT ResetRTPCValue(uint in_rtpcID, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_2(in_rtpcID, in_gameObjectID_id, in_uValueChangeDuration); }
	}

	public static AKRESULT ResetRTPCValue(uint in_rtpcID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_3(in_rtpcID, in_gameObjectID_id); }
	}

	public static AKRESULT ResetRTPCValue(string in_pszRtpcName, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve, bool in_bBypassInternalValueInterpolation)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_5(in_pszRtpcName, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve, in_bBypassInternalValueInterpolation); }
	}

	public static AKRESULT ResetRTPCValue(string in_pszRtpcName, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration, AkCurveInterpolation in_eFadeCurve)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_6(in_pszRtpcName, in_gameObjectID_id, in_uValueChangeDuration, (int)in_eFadeCurve); }
	}

	public static AKRESULT ResetRTPCValue(string in_pszRtpcName, UnityEngine.GameObject in_gameObjectID, int in_uValueChangeDuration)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_7(in_pszRtpcName, in_gameObjectID_id, in_uValueChangeDuration); }
	}

	public static AKRESULT ResetRTPCValue(string in_pszRtpcName, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ResetRTPCValue__SWIG_8(in_pszRtpcName, in_gameObjectID_id); }
	}

	public static AKRESULT SetSwitch(uint in_switchGroup, uint in_switchState, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetSwitch__SWIG_0(in_switchGroup, in_switchState, in_gameObjectID_id); }
	}

	public static AKRESULT SetSwitch(string in_pszSwitchGroup, string in_pszSwitchState, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetSwitch__SWIG_1(in_pszSwitchGroup, in_pszSwitchState, in_gameObjectID_id); }
	}

	public static AKRESULT PostTrigger(uint in_triggerID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostTrigger__SWIG_0(in_triggerID, in_gameObjectID_id); }
	}

	public static AKRESULT PostTrigger(string in_pszTrigger, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostTrigger__SWIG_1(in_pszTrigger, in_gameObjectID_id); }
	}

	public static AKRESULT SetGameObjectAuxSendValues(UnityEngine.GameObject in_gameObjectID, AkAuxSendArray in_aAuxSendValues, uint in_uNumSendValues)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetGameObjectAuxSendValues(in_gameObjectID_id, in_aAuxSendValues.GetBuffer(), in_uNumSendValues); }
	}

	public static AKRESULT SetGameObjectOutputBusVolume(UnityEngine.GameObject in_emitterObjID, UnityEngine.GameObject in_listenerObjID, float in_fControlValue)
	{

		var in_emitterObjID_id = AkSoundEngine.GetAkGameObjectID(in_emitterObjID);
		AkSoundEngine.PreGameObjectAPICall(in_emitterObjID, in_emitterObjID_id);


		var in_listenerObjID_id = AkSoundEngine.GetAkGameObjectID(in_listenerObjID);
		AkSoundEngine.PreGameObjectAPICall(in_listenerObjID, in_listenerObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetGameObjectOutputBusVolume(in_emitterObjID_id, in_listenerObjID_id, in_fControlValue); }
	}

	public static AKRESULT SetObjectObstructionAndOcclusion(UnityEngine.GameObject in_EmitterID, UnityEngine.GameObject in_ListenerID, float in_fObstructionLevel, float in_fOcclusionLevel)
	{

		var in_EmitterID_id = AkSoundEngine.GetAkGameObjectID(in_EmitterID);
		AkSoundEngine.PreGameObjectAPICall(in_EmitterID, in_EmitterID_id);


		var in_ListenerID_id = AkSoundEngine.GetAkGameObjectID(in_ListenerID);
		AkSoundEngine.PreGameObjectAPICall(in_ListenerID, in_ListenerID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetObjectObstructionAndOcclusion(in_EmitterID_id, in_ListenerID_id, in_fObstructionLevel, in_fOcclusionLevel); }
	}

	public static AKRESULT SetMultipleObstructionAndOcclusion(UnityEngine.GameObject in_EmitterID, UnityEngine.GameObject in_uListenerID, AkObstructionOcclusionValuesArray in_fObstructionOcclusionValues, uint in_uNumOcclusionObstruction)
	{

		var in_EmitterID_id = AkSoundEngine.GetAkGameObjectID(in_EmitterID);
		AkSoundEngine.PreGameObjectAPICall(in_EmitterID, in_EmitterID_id);


		var in_uListenerID_id = AkSoundEngine.GetAkGameObjectID(in_uListenerID);
		AkSoundEngine.PreGameObjectAPICall(in_uListenerID, in_uListenerID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetMultipleObstructionAndOcclusion(in_EmitterID_id, in_uListenerID_id, in_fObstructionOcclusionValues.GetBuffer(), in_uNumOcclusionObstruction); }
	}

	public static AKRESULT PostCode(AkMonitorErrorCode in_eError, AkMonitorErrorLevel in_eErrorLevel, uint in_playingID, UnityEngine.GameObject in_gameObjID, uint in_audioNodeID, bool in_bIsBus)
	{

		var in_gameObjID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjID, in_gameObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostCode__SWIG_0((int)in_eError, (int)in_eErrorLevel, in_playingID, in_gameObjID_id, in_audioNodeID, in_bIsBus); }
	}

	public static AKRESULT PostCode(AkMonitorErrorCode in_eError, AkMonitorErrorLevel in_eErrorLevel, uint in_playingID, UnityEngine.GameObject in_gameObjID, uint in_audioNodeID)
	{

		var in_gameObjID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjID, in_gameObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostCode__SWIG_1((int)in_eError, (int)in_eErrorLevel, in_playingID, in_gameObjID_id, in_audioNodeID); }
	}

	public static AKRESULT PostCode(AkMonitorErrorCode in_eError, AkMonitorErrorLevel in_eErrorLevel, uint in_playingID, UnityEngine.GameObject in_gameObjID)
	{

		var in_gameObjID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjID, in_gameObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostCode__SWIG_2((int)in_eError, (int)in_eErrorLevel, in_playingID, in_gameObjID_id); }
	}

	public static AKRESULT PostString(string in_pszError, AkMonitorErrorLevel in_eErrorLevel, uint in_playingID, UnityEngine.GameObject in_gameObjID, uint in_audioNodeID, bool in_bIsBus)
	{

		var in_gameObjID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjID, in_gameObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostString__SWIG_0(in_pszError, (int)in_eErrorLevel, in_playingID, in_gameObjID_id, in_audioNodeID, in_bIsBus); }
	}

	public static AKRESULT PostString(string in_pszError, AkMonitorErrorLevel in_eErrorLevel, uint in_playingID, UnityEngine.GameObject in_gameObjID, uint in_audioNodeID)
	{

		var in_gameObjID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjID, in_gameObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostString__SWIG_1(in_pszError, (int)in_eErrorLevel, in_playingID, in_gameObjID_id, in_audioNodeID); }
	}

	public static AKRESULT PostString(string in_pszError, AkMonitorErrorLevel in_eErrorLevel, uint in_playingID, UnityEngine.GameObject in_gameObjID)
	{

		var in_gameObjID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjID, in_gameObjID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_PostString__SWIG_2(in_pszError, (int)in_eErrorLevel, in_playingID, in_gameObjID_id); }
	}

	public static AKRESULT GetPosition(UnityEngine.GameObject in_GameObjectID, AkTransform out_rPosition) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetPosition(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), AkTransform.getCPtr(out_rPosition)); }

	public static AKRESULT GetListenerPosition(UnityEngine.GameObject in_uIndex, AkTransform out_rPosition)
	{

		var in_uIndex_id = AkSoundEngine.GetAkGameObjectID(in_uIndex);
		AkSoundEngine.PreGameObjectAPICall(in_uIndex, in_uIndex_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetListenerPosition(in_uIndex_id, AkTransform.getCPtr(out_rPosition)); }
	}

	public static AKRESULT GetRTPCValue(uint in_rtpcID, UnityEngine.GameObject in_gameObjectID, uint in_playingID, out float out_rValue, ref int io_rValueType)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetRTPCValue__SWIG_0(in_rtpcID, in_gameObjectID_id, in_playingID, out out_rValue, ref io_rValueType); }
	}

	public static AKRESULT GetRTPCValue(string in_pszRtpcName, UnityEngine.GameObject in_gameObjectID, uint in_playingID, out float out_rValue, ref int io_rValueType)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetRTPCValue__SWIG_1(in_pszRtpcName, in_gameObjectID_id, in_playingID, out out_rValue, ref io_rValueType); }
	}

	public static AKRESULT GetSwitch(uint in_switchGroup, UnityEngine.GameObject in_gameObjectID, out uint out_rSwitchState)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetSwitch__SWIG_0(in_switchGroup, in_gameObjectID_id, out out_rSwitchState); }
	}

	public static AKRESULT GetSwitch(string in_pstrSwitchGroupName, UnityEngine.GameObject in_GameObj, out uint out_rSwitchState) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetSwitch__SWIG_1(in_pstrSwitchGroupName, AkSoundEngine.GetAkGameObjectID(in_GameObj), out out_rSwitchState); }

	public static AKRESULT GetGameObjectAuxSendValues(UnityEngine.GameObject in_gameObjectID, AkAuxSendArray out_paAuxSendValues, ref uint io_ruNumSendValues)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetGameObjectAuxSendValues(in_gameObjectID_id, out_paAuxSendValues.GetBuffer(), ref io_ruNumSendValues); }
	}

	public static AKRESULT GetGameObjectDryLevelValue(UnityEngine.GameObject in_EmitterID, UnityEngine.GameObject in_ListenerID, out float out_rfControlValue)
	{

		var in_EmitterID_id = AkSoundEngine.GetAkGameObjectID(in_EmitterID);
		AkSoundEngine.PreGameObjectAPICall(in_EmitterID, in_EmitterID_id);


		var in_ListenerID_id = AkSoundEngine.GetAkGameObjectID(in_ListenerID);
		AkSoundEngine.PreGameObjectAPICall(in_ListenerID, in_ListenerID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetGameObjectDryLevelValue(in_EmitterID_id, in_ListenerID_id, out out_rfControlValue); }
	}

	public static AKRESULT GetObjectObstructionAndOcclusion(UnityEngine.GameObject in_EmitterID, UnityEngine.GameObject in_ListenerID, out float out_rfObstructionLevel, out float out_rfOcclusionLevel)
	{

		var in_EmitterID_id = AkSoundEngine.GetAkGameObjectID(in_EmitterID);
		AkSoundEngine.PreGameObjectAPICall(in_EmitterID, in_EmitterID_id);


		var in_ListenerID_id = AkSoundEngine.GetAkGameObjectID(in_ListenerID);
		AkSoundEngine.PreGameObjectAPICall(in_ListenerID, in_ListenerID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetObjectObstructionAndOcclusion(in_EmitterID_id, in_ListenerID_id, out out_rfObstructionLevel, out out_rfOcclusionLevel); }
	}

	public static bool GetIsGameObjectActive(UnityEngine.GameObject in_GameObjId)
	{

		var in_GameObjId_id = AkSoundEngine.GetAkGameObjectID(in_GameObjId);
		AkSoundEngine.PreGameObjectAPICall(in_GameObjId, in_GameObjId_id);

		{ return AkSoundEnginePINVOKE.CSharp_GetIsGameObjectActive(in_GameObjId_id); }
	}

	public static float GetMaxRadius(UnityEngine.GameObject in_GameObjId)
	{

		var in_GameObjId_id = AkSoundEngine.GetAkGameObjectID(in_GameObjId);
		AkSoundEngine.PreGameObjectAPICall(in_GameObjId, in_GameObjId_id);

		{ return AkSoundEnginePINVOKE.CSharp_GetMaxRadius(in_GameObjId_id); }
	}

	public static AKRESULT GetPlayingIDsFromGameObject(UnityEngine.GameObject in_GameObjId, ref uint io_ruNumIDs, uint[] out_aPlayingIDs)
	{

		var in_GameObjId_id = AkSoundEngine.GetAkGameObjectID(in_GameObjId);
		AkSoundEngine.PreGameObjectAPICall(in_GameObjId, in_GameObjId_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_GetPlayingIDsFromGameObject(in_GameObjId_id, ref io_ruNumIDs, out_aPlayingIDs); }
	}

	public static AKRESULT SetImageSource(uint in_srcID, AkImageSourceSettings in_info, uint in_AuxBusID, ulong in_roomID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetImageSource(in_srcID, AkImageSourceSettings.getCPtr(in_info), in_AuxBusID, in_roomID, in_gameObjectID_id); }
	}

	public static AKRESULT RemoveImageSource(uint in_srcID, uint in_AuxBusID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_RemoveImageSource(in_srcID, in_AuxBusID, in_gameObjectID_id); }
	}

	public static AKRESULT ClearImageSources(uint in_AuxBusID, UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_ClearImageSources__SWIG_0(in_AuxBusID, in_gameObjectID_id); }
	}

	public static AKRESULT QueryReflectionPaths(UnityEngine.GameObject in_gameObjectID, uint in_positionIndex, ref UnityEngine.Vector3 out_listenerPos, ref UnityEngine.Vector3 out_emitterPos, AkReflectionPathInfoArray out_aPaths, out uint io_uArraySize)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_QueryReflectionPaths(in_gameObjectID_id, in_positionIndex, ref out_listenerPos, ref out_emitterPos, out_aPaths.GetBuffer(), out io_uArraySize); }
	}

	public static AKRESULT SetGameObjectInRoom(UnityEngine.GameObject in_gameObjectID, ulong in_CurrentRoomID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetGameObjectInRoom(in_gameObjectID_id, in_CurrentRoomID); }
	}

	public static AKRESULT SetEarlyReflectionsAuxSend(UnityEngine.GameObject in_gameObjectID, uint in_auxBusID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetEarlyReflectionsAuxSend(in_gameObjectID_id, in_auxBusID); }
	}

	public static AKRESULT SetEarlyReflectionsVolume(UnityEngine.GameObject in_gameObjectID, float in_fSendVolume)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetEarlyReflectionsVolume(in_gameObjectID_id, in_fSendVolume); }
	}

	public static AKRESULT QueryDiffractionPaths(UnityEngine.GameObject in_gameObjectID, uint in_positionIndex, ref UnityEngine.Vector3 out_listenerPos, ref UnityEngine.Vector3 out_emitterPos, AkDiffractionPathInfoArray out_aPaths, out uint io_uArraySize)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_QueryDiffractionPaths(in_gameObjectID_id, in_positionIndex, ref out_listenerPos, ref out_emitterPos, out_aPaths.GetBuffer(), out io_uArraySize); }
	}

	public static AKRESULT RegisterGameObjInternal(UnityEngine.GameObject in_GameObj) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_RegisterGameObjInternal(AkSoundEngine.GetAkGameObjectID(in_GameObj)); }

	public static AKRESULT UnregisterGameObjInternal(UnityEngine.GameObject in_GameObj) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_UnregisterGameObjInternal(AkSoundEngine.GetAkGameObjectID(in_GameObj)); }

	public static AKRESULT RegisterGameObjInternal_WithName(UnityEngine.GameObject in_GameObj, string in_pszObjName) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_RegisterGameObjInternal_WithName(AkSoundEngine.GetAkGameObjectID(in_GameObj), in_pszObjName); }

	public static AKRESULT SetObjectPosition(UnityEngine.GameObject in_GameObjectID, UnityEngine.Vector3 Pos, UnityEngine.Vector3 Front, UnityEngine.Vector3 Top) { return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetObjectPosition(AkSoundEngine.GetAkGameObjectID(in_GameObjectID), Pos, Front, Top); }

	public static AKRESULT SetListeners(UnityEngine.GameObject in_emitterGameObj, ulong[] in_pListenerGameObjs, uint in_uNumListeners)
	{

		var in_emitterGameObj_id = AkSoundEngine.GetAkGameObjectID(in_emitterGameObj);
		AkSoundEngine.PreGameObjectAPICall(in_emitterGameObj, in_emitterGameObj_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_SetListeners(in_emitterGameObj_id, in_pListenerGameObjs, in_uNumListeners); }
	}

	public static AKRESULT RegisterSpatialAudioListener(UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_RegisterSpatialAudioListener(in_gameObjectID_id); }
	}

	public static AKRESULT UnregisterSpatialAudioListener(UnityEngine.GameObject in_gameObjectID)
	{

		var in_gameObjectID_id = AkSoundEngine.GetAkGameObjectID(in_gameObjectID);
		AkSoundEngine.PreGameObjectAPICall(in_gameObjectID, in_gameObjectID_id);

		{ return (AKRESULT)AkSoundEnginePINVOKE.CSharp_UnregisterSpatialAudioListener(in_gameObjectID_id); }
	}

	#endregion

	public const uint AK_PENDING_EVENT_LOAD_ID = uint.MaxValue;
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.