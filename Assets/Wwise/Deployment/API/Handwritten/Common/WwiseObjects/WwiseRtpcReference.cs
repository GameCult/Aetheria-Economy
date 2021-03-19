//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise RTPCs as Unity assets.
public class WwiseRtpcReference : WwiseObjectReference
{
	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.GameParameter; } }
}
