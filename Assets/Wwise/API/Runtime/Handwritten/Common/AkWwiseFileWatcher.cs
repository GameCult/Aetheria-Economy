#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Threading;

public class AkWwiseFileWatcher
{
	private static readonly AkWwiseFileWatcher instance = new AkWwiseFileWatcher();
	public static AkWwiseFileWatcher Instance { get { return instance; } }

	private System.IO.FileSystemWatcher XmlWatcher;
	private System.IO.FileSystemWatcher WprojWatcher;
	private bool XmlExceptionOccurred;
	private bool ProjectExceptionOccurred;
	private bool xmlChanged;
	private bool wprojChanged;

	public event System.Action XMLUpdated;
	public event System.Action<string> WwiseProjectUpdated;
	public System.Func<bool> PopulateXML;
	private string generatedSoundbanksPath;
	private string wwiseProjectPath;

	private AkWwiseFileWatcher()
	{
		if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !UnityEditor.EditorApplication.isPlaying)
		{
			return;
		}

		StartWatchers();
	}

	public void StartWatchers()
	{
		generatedSoundbanksPath = AkBasePathGetter.GetPlatformBasePath();
		wwiseProjectPath = AkBasePathGetter.GetWwiseProjectDirectory();

		new Thread(CreateXmlWatcher).Start();
		new Thread(CreateProjectWatcher).Start();

		WwiseProjectUpdated += AkUtilities.SoundBankDestinationsUpdated;
		UnityEditor.EditorApplication.update += OnEditorUpdate;
	}

	public void CreateXmlWatcher()
	{

		try
		{
			if (XmlWatcher != null)
			{
				XmlWatcher.Dispose();
			}

			XmlWatcher = new System.IO.FileSystemWatcher(generatedSoundbanksPath) {Filter = "*.xml", IncludeSubdirectories = true, };
			// Event handlers that are watching for specific event
			XmlWatcher.Created += RaiseXmlFlag;
			XmlWatcher.Changed += RaiseXmlFlag;

			XmlWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
			XmlWatcher.EnableRaisingEvents = true;
			XmlExceptionOccurred = false;
		}
		catch
		{
			XmlExceptionOccurred = true;
		}
	}

	public void CreateProjectWatcher()
	{

		try
		{
			if (XmlWatcher != null)
			{
				WprojWatcher.Dispose();
			}

			WprojWatcher = new System.IO.FileSystemWatcher(wwiseProjectPath) { Filter = "*.wproj", IncludeSubdirectories = false, };
			// Event handlers that are watching for specific event
			WprojWatcher.Created += RaiseProjectFlag;
			WprojWatcher.Changed += RaiseProjectFlag;

			WprojWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
			WprojWatcher.EnableRaisingEvents = true;
			ProjectExceptionOccurred= false;
		}
		catch
		{
			ProjectExceptionOccurred = true;
		}
	}


	private void OnEditorUpdate()
	{
		HandleXmlChange();
		HandleWprojChange();
	}

	private void HandleXmlChange()
	{
		var logWarnings = AkBasePathGetter.LogWarnings;
		AkBasePathGetter.LogWarnings = false;
		generatedSoundbanksPath = AkBasePathGetter.GetPlatformBasePath();

		if (XmlExceptionOccurred || generatedSoundbanksPath != XmlWatcher?.Path)
		{
			new Thread(CreateXmlWatcher).Start();
		}
		
		if (!xmlChanged)
			return;

		xmlChanged = false;

		var populate = PopulateXML;
		if (populate == null || !populate())
			return;

		var callback = XMLUpdated;
		if (callback != null)
		{
			callback();
		}
	}


	private void HandleWprojChange()
	{
		wwiseProjectPath = AkBasePathGetter.GetWwiseProjectDirectory();

		if (ProjectExceptionOccurred || wwiseProjectPath != WprojWatcher?.Path)
		{
			new Thread(CreateProjectWatcher).Start();
		}

		if (!wprojChanged)
			return;

		wprojChanged = false;
		WwiseProjectUpdated?.Invoke(WprojWatcher.Path);
	}

	private void RaiseXmlFlag(object sender, System.IO.FileSystemEventArgs e)
	{
		xmlChanged = true;
	}


	private void RaiseProjectFlag(object sender, System.IO.FileSystemEventArgs e)
	{
		wprojChanged = true;
	}
}
#endif
