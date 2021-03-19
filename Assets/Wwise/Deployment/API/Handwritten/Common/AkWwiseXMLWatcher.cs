#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Threading;

public class AkWwiseXMLWatcher
{
	private static readonly AkWwiseXMLWatcher instance = new AkWwiseXMLWatcher();
	public static AkWwiseXMLWatcher Instance { get { return instance; } }

	private System.IO.FileSystemWatcher XmlWatcher;
	private bool ExceptionOccurred;
	private bool fireEvent;

	public event System.Action XMLUpdated;
	public System.Func<bool> PopulateXML;
	private string basePath;

	private AkWwiseXMLWatcher()
	{
		if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !UnityEditor.EditorApplication.isPlaying)
		{
			return;
		}

		StartWatcher();
	}

	public void StartWatcher()
	{
		basePath = AkBasePathGetter.GetPlatformBasePath();
		new Thread(CreateWatcher).Start();
		UnityEditor.EditorApplication.update += OnEditorUpdate;
	}

	public void CreateWatcher()
	{

		try
		{
			if (XmlWatcher != null)
			{
				XmlWatcher.Dispose();
			}

			XmlWatcher = new System.IO.FileSystemWatcher(basePath) {Filter = "*.xml", IncludeSubdirectories = true, };
			// Event handlers that are watching for specific event
			XmlWatcher.Created += RaisePopulateFlag;
			XmlWatcher.Changed += RaisePopulateFlag;

			XmlWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
			XmlWatcher.EnableRaisingEvents = true;
			ExceptionOccurred = false;
		}
		catch
		{
			ExceptionOccurred = true;
		}
	}


	private void OnEditorUpdate()
	{
		var logWarnings = AkBasePathGetter.LogWarnings;
		AkBasePathGetter.LogWarnings = false;
		basePath = AkBasePathGetter.GetPlatformBasePath();
		AkBasePathGetter.LogWarnings = logWarnings;

		if (ExceptionOccurred || basePath != XmlWatcher?.Path)
			new Thread(CreateWatcher).Start();

		if (!fireEvent)
			return;

		fireEvent = false;

		var populate = PopulateXML;
		if (populate == null || !populate())
			return;

		var callback = XMLUpdated;
		if (callback != null)
		{
			callback();
		}

		AkBankManager.ReloadAllBanks();
	}

	private void RaisePopulateFlag(object sender, System.IO.FileSystemEventArgs e)
	{
		fireEvent = true;
	}
}
#endif