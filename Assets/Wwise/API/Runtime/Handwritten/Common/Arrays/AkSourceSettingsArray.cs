#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2019 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

public class AkSourceSettingsArray : AkBaseArray<AkSourceSettings>
{
	public AkSourceSettingsArray(int count) : base(count)
	{
	}

	protected override int StructureSize
	{
		get { return AkSoundEnginePINVOKE.CSharp_AkSourceSettings_GetSizeOf(); }
	}

	protected override void DefaultConstructAtIntPtr(System.IntPtr address)
	{
		AkSoundEnginePINVOKE.CSharp_AkSourceSettings_Clear(address);
	}

	protected override AkSourceSettings CreateNewReferenceFromIntPtr(System.IntPtr address)
	{
		return new AkSourceSettings(address, false);
	}

	protected override void CloneIntoReferenceFromIntPtr(System.IntPtr address, AkSourceSettings other)
	{
		AkSoundEnginePINVOKE.CSharp_AkSourceSettings_Clone(address, AkSourceSettings.getCPtr(other));
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.