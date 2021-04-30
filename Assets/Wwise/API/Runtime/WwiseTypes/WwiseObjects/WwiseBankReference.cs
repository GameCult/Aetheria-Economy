//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2018 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.IO;
using UnityEditor;

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
using AK.Wwise.Unity.WwiseAddressables;
#endif

/// @brief Represents Wwise banks as Unity assets.
/// 
public class WwiseBankReference : WwiseObjectReference
{
#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
	[UnityEngine.SerializeField, AkShowOnly]
	private WwiseAddressableSoundBank bank;
	
	public WwiseAddressableSoundBank AddressableBank => bank;


#if UNITY_EDITOR

	public void OnEnable()
	{
		AkAssetUtilities.AddressableBankUpdated += UpdateAddressableBankReference;
	}

	public override void CompleteData()
	{
		SetAddressableBank(WwiseAddressableSoundBank.GetAddressableBankAsset(DisplayName));
	}

	public override bool IsComplete()
	{
		return bank != null;
	}

	public void SetAddressableBank(WwiseAddressableSoundBank asset)
	{
		bank = asset;
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	public bool UpdateAddressableBankReference(WwiseAddressableSoundBank asset, string name)
	{
		if (name == ObjectName)
		{
			SetAddressableBank(asset);
			return true;
		}
		return false;
	}

	public static bool FindBankReferenceAndSetAddressableBank(WwiseAddressableSoundBank addressableAsset, string name)
	{
		var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(WwiseBankReference).Name);
		WwiseBankReference asset;
		foreach (var assetGuid in guids)
		{
			var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
			asset = UnityEditor.AssetDatabase.LoadAssetAtPath<WwiseBankReference>(assetPath);
			if (asset && asset.ObjectName == name)
			{
				asset.SetAddressableBank(addressableAsset);
				return true;
			}
		}
		return false;
	}

	public void OnDestroy()
	{
		AkAssetUtilities.AddressableBankUpdated -= UpdateAddressableBankReference;
	}

#endif
#endif

	public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.Soundbank; } }
}
