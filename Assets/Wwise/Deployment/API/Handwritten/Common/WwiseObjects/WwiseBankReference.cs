//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

/// @brief Represents Wwise banks as Unity assets.
public class WwiseBankReference : WwiseObjectReference
{
	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.Soundbank; } }
}
