#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2019 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

public class AkExternalSourceInfoArray : AkBaseArray<AkExternalSourceInfo>
{
	public AkExternalSourceInfoArray(int count) : base(count)
	{
	}

	protected override int StructureSize
	{
		get { return AkSoundEnginePINVOKE.CSharp_AkExternalSourceInfo_GetSizeOf(); }
	}

	protected override void DefaultConstructAtIntPtr(System.IntPtr address)
	{
		AkSoundEnginePINVOKE.CSharp_AkExternalSourceInfo_Clear(address);
	}

	protected override void ReleaseAllocatedMemoryFromReferenceAtIntPtr(System.IntPtr address)
	{
		AkSoundEnginePINVOKE.CSharp_AkExternalSourceInfo_szFile_set(address, null);
	}

	protected override AkExternalSourceInfo CreateNewReferenceFromIntPtr(System.IntPtr address)
	{
		return new AkExternalSourceInfo(address, false);
	}

	protected override void CloneIntoReferenceFromIntPtr(System.IntPtr address, AkExternalSourceInfo other)
	{
		AkSoundEnginePINVOKE.CSharp_AkExternalSourceInfo_Clone(address, AkExternalSourceInfo.getCPtr(other));
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.