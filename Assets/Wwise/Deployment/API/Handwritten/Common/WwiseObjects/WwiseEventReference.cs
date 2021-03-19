//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise events as Unity assets.
public class WwiseEventReference : WwiseObjectReference
{
	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.Event; } }
}
