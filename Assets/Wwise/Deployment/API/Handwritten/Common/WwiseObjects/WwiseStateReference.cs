//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise states as Unity assets.
public class WwiseStateReference : WwiseGroupValueObjectReference
{
	[AkShowOnly]
	[UnityEngine.SerializeField]
	private WwiseStateGroupReference WwiseStateGroupReference;

	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.State; } }

	public override WwiseObjectReference GroupObjectReference
	{
		get { return WwiseStateGroupReference; }
		set { WwiseStateGroupReference = value as WwiseStateGroupReference; }
	}

	public override WwiseObjectType GroupWwiseObjectType { get { return WwiseObjectType.StateGroup; } }
}
