#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2019 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

public class AkDeviceDescriptionArray : AkBaseArray<AkDeviceDescription>
{
	public AkDeviceDescriptionArray(int count) : base(count)
	{
	}

	protected override int StructureSize
	{
		get { return AkSoundEnginePINVOKE.CSharp_AkDeviceDescription_GetSizeOf(); }
	}

	protected override void DefaultConstructAtIntPtr(System.IntPtr address)
	{
		AkSoundEnginePINVOKE.CSharp_AkDeviceDescription_Clear(address);
	}

	protected override AkDeviceDescription CreateNewReferenceFromIntPtr(System.IntPtr address)
	{
		return new AkDeviceDescription(address, false);
	}

	protected override void CloneIntoReferenceFromIntPtr(System.IntPtr address, AkDeviceDescription other)
	{
		AkSoundEnginePINVOKE.CSharp_AkDeviceDescription_Clone(address, AkDeviceDescription.getCPtr(other));
	}
}
#endif