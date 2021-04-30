#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2019 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

public class AkObstructionOcclusionValuesArray : AkBaseArray<AkObstructionOcclusionValues>
{
	public AkObstructionOcclusionValuesArray(int count) : base(count)
	{
	}

	protected override int StructureSize
	{
		get { return AkSoundEnginePINVOKE.CSharp_AkObstructionOcclusionValues_GetSizeOf(); }
	}

	protected override void DefaultConstructAtIntPtr(System.IntPtr address)
	{
		AkSoundEnginePINVOKE.CSharp_AkObstructionOcclusionValues_Clear(address);
	}

	protected override AkObstructionOcclusionValues CreateNewReferenceFromIntPtr(System.IntPtr address)
	{
		return new AkObstructionOcclusionValues(address, false);
	}

	protected override void CloneIntoReferenceFromIntPtr(System.IntPtr address, AkObstructionOcclusionValues other)
	{
		AkSoundEnginePINVOKE.CSharp_AkObstructionOcclusionValues_Clone(address, AkObstructionOcclusionValues.getCPtr(other));
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.