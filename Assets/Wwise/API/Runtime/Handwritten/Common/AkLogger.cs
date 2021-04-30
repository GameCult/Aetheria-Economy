#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////


public class AkLogger
{
	// @todo sjl: Have SWIG specify the delegate's signature (possibly in AkSoundEngine) so that we can automatically determine the appropriate string marshaling.
	[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
	public delegate void ErrorLoggerInteropDelegate(
		[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
		string message);

	private static AkLogger ms_Instance = new AkLogger();
	private ErrorLoggerInteropDelegate errorLoggerDelegate = WwiseInternalLogError;

	private AkLogger()
	{
		if (ms_Instance == null)
		{
			ms_Instance = this;
			AkSoundEngine.SetErrorLogger(errorLoggerDelegate);
		}
	}

	public static AkLogger Instance { get { return ms_Instance; } }

	~AkLogger()
	{
		if (ms_Instance == this)
		{
			ms_Instance = null;
			errorLoggerDelegate = null;
			AkSoundEngine.SetErrorLogger();
		}
	}

	public void Init()
	{
		// used to force instantiation of this singleton
	}

	[AOT.MonoPInvokeCallback(typeof(ErrorLoggerInteropDelegate))]
	public static void WwiseInternalLogError(string message)
	{
		UnityEngine.Debug.LogErrorFormat("Wwise: {0}", message);
	}

	public static void Message(string message)
	{
		UnityEngine.Debug.LogFormat("WwiseUnity: {0}", message);
	}

	public static void Warning(string message)
	{
		UnityEngine.Debug.LogWarningFormat("WwiseUnity: {0}", message);
	}

	public static void Error(string message)
	{
		UnityEngine.Debug.LogErrorFormat("WwiseUnity: {0}", message);
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.