//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise triggers as Unity assets.
public class WwiseTriggerReference : WwiseObjectReference
{
	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.Trigger; } }
}
