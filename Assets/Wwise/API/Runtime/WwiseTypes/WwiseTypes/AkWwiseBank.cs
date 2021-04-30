#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
using AK.Wwise.Unity.WwiseAddressables;
#endif

namespace AK.Wwise
{
	[System.Serializable]
	///@brief This type can be used to load/unload SoundBanks.
	public class Bank : BaseType
	{
		public override WwiseObjectType WwiseObjectType { get { return WwiseObjectType.Soundbank; } }
		public WwiseBankReference WwiseObjectReference;

		public override WwiseObjectReference ObjectReference
		{
			get { return WwiseObjectReference; }
			set { WwiseObjectReference = value as WwiseBankReference; }
		}

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
		public override bool IsValid()
		{
			return base.IsValid() && WwiseObjectReference.AddressableBank !=null;
		}

		public void Load(bool decodeBank = false, bool saveDecodedBank = false)
		{
			if (IsValid())
				AkAddressableBankManager.Instance.LoadBank(WwiseObjectReference.AddressableBank, decodeBank, saveDecodedBank);
		}

		public void LoadAsync(AkCallbackManager.BankCallback callback = null)
		{
			throw new System.Exception("Wwise Addressables : Use Load() when loading banks with the Wwise Addressables package");
		}
		public void Unload()
		{
			if (IsValid())
				AkAddressableBankManager.Instance.UnloadBank(WwiseObjectReference.AddressableBank);
		}
#else
		public void Load(bool decodeBank = false, bool saveDecodedBank = false)
		{
			if (IsValid())
				AkBankManager.LoadBank(Name, decodeBank, saveDecodedBank);
		}

		public void LoadAsync(AkCallbackManager.BankCallback callback = null)
		{
			if (IsValid())
				AkBankManager.LoadBankAsync(Name, callback);
		}

		public void Unload()
		{
			if (IsValid())
				AkBankManager.UnloadBank(Name);
		}
#endif
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.