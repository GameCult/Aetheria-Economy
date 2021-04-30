#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEditor.InitializeOnLoad]
public class AkWwiseXMLBuilder
{
	private static readonly System.DateTime s_LastParsed = System.DateTime.MinValue;

	static AkWwiseXMLBuilder()
	{
		AkWwiseFileWatcher.Instance.PopulateXML += Populate;
		UnityEditor.EditorApplication.playModeStateChanged += PlayModeChanged;
	}

	private static void PlayModeChanged(UnityEditor.PlayModeStateChange mode)
	{
		if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode)
		{
			AkWwiseProjectInfo.Populate();
			AkWwiseFileWatcher.Instance.StartWatchers();
		}
	}

	public static bool Populate()
	{
		if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling)
		{
			return false;
		}

		try
		{
			// Try getting the SoundbanksInfo.xml file for Windows or Mac first, then try to find any other available platform.
			var logWarnings = AkBasePathGetter.LogWarnings;
			AkBasePathGetter.LogWarnings = false;
			var FullSoundbankPath = AkBasePathGetter.GetPlatformBasePath();
			AkBasePathGetter.LogWarnings = logWarnings;

			var filename = System.IO.Path.Combine(FullSoundbankPath, "SoundbanksInfo.xml");
			if (!System.IO.File.Exists(filename))
			{
				FullSoundbankPath = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, AkWwiseEditorSettings.Instance.SoundbankPath);

				if (!System.IO.Directory.Exists(FullSoundbankPath))
					return false;

				var foundFiles = System.IO.Directory.GetFiles(FullSoundbankPath, "SoundbanksInfo.xml", System.IO.SearchOption.AllDirectories);
				if (foundFiles.Length == 0)
					return false;

				filename = foundFiles[0];
			}

			var time = System.IO.File.GetLastWriteTime(filename);
			if (time <= s_LastParsed)
			{
				return false;
			}

			var doc = new System.Xml.XmlDocument();
			doc.Load(filename);

			var bChanged = false;
			var soundBanks = doc.GetElementsByTagName("SoundBanks");
			for (var i = 0; i < soundBanks.Count; i++)
			{
				var soundBank = soundBanks[i].SelectNodes("SoundBank");
				for (var j = 0; j < soundBank.Count; j++)
				{
					bChanged = SerialiseSoundBank(soundBank[j]) || bChanged;
				}
			}

			return bChanged;
		}
		catch
		{
			return false;
		}
	}

	private static bool SerialiseSoundBank(System.Xml.XmlNode node)
	{
		var bChanged = false;
		var includedEvents = node.SelectNodes("IncludedEvents");
		for (var i = 0; i < includedEvents.Count; i++)
		{
			var events = includedEvents[i].SelectNodes("Event");
			for (var j = 0; j < events.Count; j++)
			{
				bChanged = SerialiseEventData(events[j]) || bChanged;
			}
		}

		return bChanged;
	}

	private static float GetFloatFromString(string s)
	{
		return string.Compare(s, "Infinite") == 0 ? UnityEngine.Mathf.Infinity : float.Parse(s);
	}

	private static bool SerialiseEventData(System.Xml.XmlNode node)
	{
		var maxAttenuationAttribute = node.Attributes["MaxAttenuation"];
		var durationMinAttribute = node.Attributes["DurationMin"];
		var durationMaxAttribute = node.Attributes["DurationMax"];
		if (maxAttenuationAttribute == null && durationMinAttribute == null && durationMaxAttribute == null)
			return false;

		var bChanged = false;
		var name = node.Attributes["Name"].Value;
		foreach (var wwu in AkWwiseProjectInfo.GetData().EventWwu)
		{
			var eventData = wwu.Find(name);
			if (eventData == null)
				continue;

			if (maxAttenuationAttribute != null)
			{
				var maxAttenuation = float.Parse(maxAttenuationAttribute.Value);
				if (eventData.maxAttenuation != maxAttenuation)
				{
					eventData.maxAttenuation = maxAttenuation;
					bChanged = true;
				}
			}

			if (durationMinAttribute != null)
			{
				var minDuration = GetFloatFromString(durationMinAttribute.Value);
				if (eventData.minDuration != minDuration)
				{
					eventData.minDuration = minDuration;
					bChanged = true;
				}
			}

			if (durationMaxAttribute != null)
			{
				var maxDuration = GetFloatFromString(durationMaxAttribute.Value);
				if (eventData.maxDuration != maxDuration)
				{
					eventData.maxDuration = maxDuration;
					bChanged = true;
				}
			}
		}
		return bChanged;
	}
}
#endif