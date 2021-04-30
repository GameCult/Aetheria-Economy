//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise state groups as Unity assets.
public class WwiseStateGroupReference : WwiseObjectReference
{
	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.StateGroup; } }
}
