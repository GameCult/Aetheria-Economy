//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise states as Unity assets.
public class WwiseSwitchReference : WwiseGroupValueObjectReference
{
	[AkShowOnly]
	[UnityEngine.SerializeField]
	private WwiseSwitchGroupReference WwiseSwitchGroupReference;

	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.Switch; } }

	public override WwiseObjectReference GroupObjectReference
	{
		get { return WwiseSwitchGroupReference; }
		set { WwiseSwitchGroupReference = value as WwiseSwitchGroupReference; }
	}

	public override WwiseObjectType GroupWwiseObjectType { get { return WwiseObjectType.SwitchGroup; } }
}
