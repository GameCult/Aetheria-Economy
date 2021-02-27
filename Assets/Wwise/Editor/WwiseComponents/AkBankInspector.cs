#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEditor.CanEditMultipleObjects]
[UnityEditor.CustomEditor(typeof(AkBank))]
public class AkBankInspector : AkBaseInspector
{
	private readonly AkUnityEventHandlerInspector m_LoadBankEventHandlerInspector = new AkUnityEventHandlerInspector();
	private readonly AkUnityEventHandlerInspector m_UnloadBankEventHandlerInspector = new AkUnityEventHandlerInspector();
	private UnityEditor.SerializedProperty decode;
	private UnityEditor.SerializedProperty loadAsync;
	private UnityEditor.SerializedProperty saveDecoded;

	private void OnEnable()
	{
		m_LoadBankEventHandlerInspector.Init(serializedObject, "triggerList", "Load On: ", false);
		m_UnloadBankEventHandlerInspector.Init(serializedObject, "unloadTriggerList", "Unload On: ", false);

		loadAsync = serializedObject.FindProperty("loadAsynchronous");
		decode = serializedObject.FindProperty("decodeBank");
		saveDecoded = serializedObject.FindProperty("saveDecodedBank");
	}

	public override void OnChildInspectorGUI()
	{
		m_LoadBankEventHandlerInspector.OnGUI();
		m_UnloadBankEventHandlerInspector.OnGUI();

		using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
		{
			UnityEditor.EditorGUILayout.PropertyField(loadAsync, new UnityEngine.GUIContent("Asynchronous:"));
			UnityEditor.EditorGUILayout.PropertyField(decode, new UnityEngine.GUIContent("Decode compressed data:"));

			if (!decode.boolValue)
				return;

			var oldSaveDecodedValue = saveDecoded.boolValue;
			UnityEditor.EditorGUILayout.PropertyField(saveDecoded, new UnityEngine.GUIContent("Save decoded bank:"));
			if (!oldSaveDecodedValue || saveDecoded.boolValue)
				return;

			var bank = target as AkBank;
			AkBasePathGetter.EvaluateGamePaths();
			var decodedBankPath = System.IO.Path.Combine(AkBasePathGetter.DecodedBankFullPath, bank.data.Name + ".bnk");

			try
			{
				System.IO.File.Delete(decodedBankPath);
			}
			catch (System.Exception e)
			{
				UnityEngine.Debug.Log("WwiseUnity: Could not delete existing decoded SoundBank. Please delete it manually. " + e);
			}
		}
	}
}
#endif
