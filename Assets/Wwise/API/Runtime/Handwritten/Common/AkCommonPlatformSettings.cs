﻿public class AkBasePlatformSettings : UnityEngine.ScriptableObject
{
	public virtual AkInitializationSettings AkInitializationSettings
	{
		get { return new AkInitializationSettings(); }
	}

	public virtual AkSpatialAudioInitSettings AkSpatialAudioInitSettings
	{
		get { return new AkSpatialAudioInitSettings(); }
	}

	public virtual AkCallbackManager.InitializationSettings CallbackManagerInitializationSettings
	{
		get { return new AkCallbackManager.InitializationSettings(); }
	}

	public virtual string SoundBankPersistentDataPath
	{
		get { return null; }
	}

	public virtual string InitialLanguage
	{
		get { return "English(US)"; }
	}

	public virtual bool RenderDuringFocusLoss
	{
		get { return false; }
	}

	public virtual string SoundbankPath
	{
		get { return AkBasePathGetter.DefaultBasePath; }
	}

	public virtual AkCommunicationSettings AkCommunicationSettings
	{
		get { return new AkCommunicationSettings(); }
	}

	public virtual bool UseAsyncOpen
	{
		get { return false; }
	}
}

[System.Serializable]
public class AkCommonOutputSettings
{
	[UnityEngine.Tooltip("The name of a custom audio device to be used. Custom audio devices are defined in the Audio Device Shareset section of the Wwise project. Leave this empty to output normally through the default audio device.")]
	public string m_AudioDeviceShareset = string.Empty;

	[UnityEngine.Tooltip("Device specific identifier, when multiple devices of the same type are possible.  If only one device is possible, leave to 0.")]
	public uint m_DeviceID = AkSoundEngine.AK_INVALID_UNIQUE_ID;

	public enum PanningRule
	{
		Speakers = 0,
		Headphones = 1
	}

	[UnityEngine.Tooltip("Rule for 3D panning of signals routed to a stereo bus. In \"Speakers\" mode, the angle of the front loudspeakers is used. In \"Headphones\" mode, the speaker angles are superseded with constant power panning between two virtual microphones spaced 180 degrees apart.")]
	public PanningRule m_PanningRule = PanningRule.Speakers;

	[System.Serializable]
	public class ChannelConfiguration
	{
		public enum ChannelConfigType
		{
			Anonymous = 0x0,
			Standard = 0x1,
			Ambisonic = 0x2
		}

		[UnityEngine.Tooltip("A code that completes the identification of channels by uChannelMask. Anonymous: Channel mask == 0 and channels; Standard: Channels must be identified with standard defines in AkSpeakerConfigs; Ambisonic: Channel mask == 0 and channels follow standard ambisonic order.")]
		public ChannelConfigType m_ChannelConfigType = ChannelConfigType.Anonymous;

		public enum ChannelMask
		{
			NONE = 0x0,

			/// Standard speakers (channel mask):
			FRONT_LEFT = 0x1,        ///< Front left speaker bit mask
			FRONT_RIGHT = 0x2,       ///< Front right speaker bit mask
			FRONT_CENTER = 0x4,      ///< Front center speaker bit mask
			LOW_FREQUENCY = 0x8,     ///< Low-frequency speaker bit mask
			BACK_LEFT = 0x10,        ///< Rear left speaker bit mask
			BACK_RIGHT = 0x20,       ///< Rear right speaker bit mask
			BACK_CENTER = 0x100, ///< Rear center speaker ("surround speaker") bit mask
			SIDE_LEFT = 0x200,   ///< Side left speaker bit mask
			SIDE_RIGHT = 0x400,  ///< Side right speaker bit mask

			/// "Height" speakers.
			TOP = 0x800,     ///< Top speaker bit mask
			HEIGHT_FRONT_LEFT = 0x1000,  ///< Front left speaker bit mask
			HEIGHT_FRONT_CENTER = 0x2000,    ///< Front center speaker bit mask
			HEIGHT_FRONT_RIGHT = 0x4000, ///< Front right speaker bit mask
			HEIGHT_BACK_LEFT = 0x8000,   ///< Rear left speaker bit mask
			HEIGHT_BACK_CENTER = 0x10000,    ///< Rear center speaker bit mask
			HEIGHT_BACK_RIGHT = 0x20000, ///< Rear right speaker bit mask

			//
			// Supported speaker setups. Those are the ones that can be used in the Wwise Sound Engine audio pipeline.
			//
			SETUP_MONO = FRONT_CENTER,        ///< 1.0 setup channel mask
			SETUP_0POINT1 = LOW_FREQUENCY,    ///< 0.1 setup channel mask
			SETUP_1POINT1 = (FRONT_CENTER | LOW_FREQUENCY),    ///< 1.1 setup channel mask
			SETUP_STEREO = (FRONT_LEFT | FRONT_RIGHT), ///< 2.0 setup channel mask
			SETUP_2POINT1 = (SETUP_STEREO | LOW_FREQUENCY),    ///< 2.1 setup channel mask
			SETUP_3STEREO = (SETUP_STEREO | FRONT_CENTER), ///< 3.0 setup channel mask
			SETUP_3POINT1 = (SETUP_3STEREO | LOW_FREQUENCY),   ///< 3.1 setup channel mask
			SETUP_4 = (SETUP_STEREO | SIDE_LEFT | SIDE_RIGHT),  ///< 4.0 setup channel mask
			SETUP_4POINT1 = (SETUP_4 | LOW_FREQUENCY), ///< 4.1 setup channel mask
			SETUP_5 = (SETUP_4 | FRONT_CENTER),    ///< 5.0 setup channel mask
			SETUP_5POINT1 = (SETUP_5 | LOW_FREQUENCY), ///< 5.1 setup channel mask
			SETUP_6 = (SETUP_4 | BACK_LEFT | BACK_RIGHT),   ///< 6.0 setup channel mask
			SETUP_6POINT1 = (SETUP_6 | LOW_FREQUENCY), ///< 6.1 setup channel mask
			SETUP_7 = (SETUP_6 | FRONT_CENTER),    ///< 7.0 setup channel mask
			SETUP_7POINT1 = (SETUP_7 | LOW_FREQUENCY), ///< 7.1 setup channel mask
			SETUP_SURROUND = (SETUP_STEREO | BACK_CENTER), ///< Legacy surround setup channel mask

			// Note. DPL2 does not really have 4 channels, but it is used by plugins to differentiate from stereo setup.
			SETUP_DPL2 = (SETUP_4),       ///< Legacy DPL2 setup channel mask

			SETUP_HEIGHT_4 = (HEIGHT_FRONT_LEFT | HEIGHT_FRONT_RIGHT | HEIGHT_BACK_LEFT | HEIGHT_BACK_RIGHT),    ///< 4 speaker height layer.
			SETUP_HEIGHT_5 = (SETUP_HEIGHT_4 | HEIGHT_FRONT_CENTER),                                                                   ///< 5 speaker height layer.
			SETUP_HEIGHT_ALL = (SETUP_HEIGHT_5 | HEIGHT_BACK_CENTER),                                                                      ///< All height speaker layer.

			// Auro speaker setups
			SETUP_AURO_222 = (SETUP_4 | HEIGHT_FRONT_LEFT | HEIGHT_FRONT_RIGHT),    ///< Auro-222 setup channel mask
			SETUP_AURO_8 = (SETUP_AURO_222 | HEIGHT_BACK_LEFT | HEIGHT_BACK_RIGHT),     ///< Auro-8 setup channel mask
			SETUP_AURO_9 = (SETUP_AURO_8 | FRONT_CENTER),                                          ///< Auro-9.0 setup channel mask
			SETUP_AURO_9POINT1 = (SETUP_AURO_9 | LOW_FREQUENCY),                                           ///< Auro-9.1 setup channel mask
			SETUP_AURO_10 = (SETUP_AURO_9 | TOP),                                                  ///< Auro-10.0 setup channel mask		
			SETUP_AURO_10POINT1 = (SETUP_AURO_10 | LOW_FREQUENCY),                                         ///< Auro-10.1 setup channel mask	
			SETUP_AURO_11 = (SETUP_AURO_10 | HEIGHT_FRONT_CENTER),                                 ///< Auro-11.0 setup channel mask
			SETUP_AURO_11POINT1 = (SETUP_AURO_11 | LOW_FREQUENCY),                                         ///< Auro-11.1 setup channel mask	
			SETUP_AURO_11_740 = (SETUP_7 | SETUP_HEIGHT_4),                                        ///< Auro-11.0 (7+4) setup channel mask
			SETUP_AURO_11POINT1_740 = (SETUP_AURO_11_740 | LOW_FREQUENCY),                                     ///< Auro-11.1 (7+4) setup channel mask
			SETUP_AURO_13_751 = (SETUP_7 | SETUP_HEIGHT_5 | TOP),                       ///< Auro-13.0 setup channel mask
			SETUP_AURO_13POINT1_751 = (SETUP_AURO_13_751 | LOW_FREQUENCY),                                     ///< Auro-13.1 setup channel mask

			// Dolby speaker setups: in Dolby nomenclature, [#plane].[lfe].[#height]
			SETUP_DOLBY_5_0_2 = (SETUP_5 | HEIGHT_FRONT_LEFT | HEIGHT_FRONT_RIGHT), ///< Dolby 5.0.2 setup channel mask
			SETUP_DOLBY_5_1_2 = (SETUP_DOLBY_5_0_2 | LOW_FREQUENCY),                                   ///< Dolby 5.1.2 setup channel mask
			SETUP_DOLBY_6_0_2 = (SETUP_6 | HEIGHT_FRONT_LEFT | HEIGHT_FRONT_RIGHT), ///< Dolby 6.0.2 setup channel mask
			SETUP_DOLBY_6_1_2 = (SETUP_DOLBY_6_0_2 | LOW_FREQUENCY),                                   ///< Dolby 6.1.2 setup channel mask
			SETUP_DOLBY_6_0_4 = (SETUP_DOLBY_6_0_2 | HEIGHT_BACK_LEFT | HEIGHT_BACK_RIGHT), ///< Dolby 6.0.4 setup channel mask
			SETUP_DOLBY_6_1_4 = (SETUP_DOLBY_6_0_4 | LOW_FREQUENCY),                                   ///< Dolby 6.1.4 setup channel mask
			SETUP_DOLBY_7_0_2 = (SETUP_7 | HEIGHT_FRONT_LEFT | HEIGHT_FRONT_RIGHT), ///< Dolby 7.0.2 setup channel mask
			SETUP_DOLBY_7_1_2 = (SETUP_DOLBY_7_0_2 | LOW_FREQUENCY),                                   ///< Dolby 7.1.2 setup channel mask
			SETUP_DOLBY_7_0_4 = (SETUP_DOLBY_7_0_2 | HEIGHT_BACK_LEFT | HEIGHT_BACK_RIGHT), ///< Dolby 7.0.4 setup channel mask
			SETUP_DOLBY_7_1_4 = (SETUP_DOLBY_7_0_4 | LOW_FREQUENCY),                                   ///< Dolby 7.1.4 setup channel mask

			SETUP_ALL_SPEAKERS = (SETUP_7POINT1 | BACK_CENTER | SETUP_HEIGHT_ALL | TOP), ///< All speakers.
		};

		[UnityEngine.Tooltip("A bit field, whose channel identifiers depend on AkChannelConfigType (up to 20).")]
		[AkEnumFlag(typeof(ChannelMask))]
		public ChannelMask m_ChannelMask = ChannelMask.NONE;

		[UnityEngine.Tooltip("The number of channels, identified (deduced from channel mask) or anonymous (set directly).")]
		public uint m_NumberOfChannels = 0;

		public void CopyTo(AkChannelConfig config)
		{
			switch (m_ChannelConfigType)
			{
				case ChannelConfigType.Anonymous:
					config.SetAnonymous(m_NumberOfChannels);
					break;

				case ChannelConfigType.Standard:
					config.SetStandard((uint)m_ChannelMask);
					break;

				case ChannelConfigType.Ambisonic:
					config.SetAmbisonic(m_NumberOfChannels);
					break;
			}
		}
	}

	[UnityEngine.Tooltip("Channel configuration for this output. Hardware might not support the selected configuration.")]
	public ChannelConfiguration m_ChannelConfig = new ChannelConfiguration();

	public void CopyTo(AkOutputSettings settings)
	{
		settings.audioDeviceShareset = string.IsNullOrEmpty(m_AudioDeviceShareset) ? AkSoundEngine.AK_INVALID_UNIQUE_ID : AkUtilities.ShortIDGenerator.Compute(m_AudioDeviceShareset);
		settings.idDevice = m_DeviceID;
		settings.ePanningRule = (AkPanningRule)m_PanningRule;
		m_ChannelConfig.CopyTo(settings.channelConfig);
	}
}

[System.Serializable]
public partial class AkCommonUserSettings
{
	[UnityEngine.Tooltip("Path for the SoundBanks. This must contain one sub folder per platform, with the same as in the Wwise project.")]
	public string m_BasePath = AkBasePathGetter.DefaultBasePath;

	[UnityEngine.Tooltip("Language sub-folder used at startup.")]
	public string m_StartupLanguage = "English(US)";

	[UnityEngine.Tooltip("Enable Wwise engine logging. This is used to turn on/off the logging of the Wwise engine.")]
	public bool m_EngineLogging = AkCallbackManager.InitializationSettings.DefaultIsLoggingEnabled;

	[UnityEngine.Tooltip("Maximum number of automation paths for positioning sounds.")]
	public uint m_MaximumNumberOfPositioningPaths = 255;

	[UnityEngine.Tooltip("Size of the command queue.")]
	public uint m_CommandQueueSize = 256 * 1024;

	[UnityEngine.Tooltip("Number of samples per audio frame (256, 512, 1024, or 2048).")]
	public uint m_SamplesPerFrame = 1024;

	[UnityEngine.Tooltip("Main output device settings.")]
	public AkCommonOutputSettings m_MainOutputSettings;

	protected static string GetPluginPath()
	{
#if UNITY_EDITOR_WIN
		return System.IO.Path.GetFullPath(AkUtilities.GetPathInPackage(@"Runtime\Plugins\Windows\x86_64\DSP"));
#elif UNITY_EDITOR_OSX
		return System.IO.Path.GetFullPath(AkUtilities.GetPathInPackage("Runtime/Plugins/Mac/DSP"));
#elif UNITY_STANDALONE_WIN
		string potentialPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Plugins" + System.IO.Path.DirectorySeparatorChar);
		string architectureName = "x86";
#if UNITY_64
		architectureName += "_64";
#endif
		if(System.IO.File.Exists(System.IO.Path.Combine(potentialPath, "AkSoundEngine.dll")))
		{
			return potentialPath;
		}
		else if(System.IO.File.Exists(System.IO.Path.Combine(potentialPath, architectureName, "AkSoundEngine.dll")))
		{
			return System.IO.Path.Combine(potentialPath, architectureName);
		}
		else
		{
			UnityEngine.Debug.Log("Cannot find Wwise plugin path");
			return null;
		}
#elif UNITY_ANDROID || UNITY_WSA
		return null;
#elif UNITY_STADIA
		return System.IO.Path.Combine(UnityEngine.Application.dataPath, ".." + System.IO.Path.DirectorySeparatorChar);
#else
		return System.IO.Path.Combine(UnityEngine.Application.dataPath, "Plugins" + System.IO.Path.DirectorySeparatorChar);
#endif
	}

	public virtual void CopyTo(AkInitSettings settings)
	{
		settings.uMaxNumPaths = m_MaximumNumberOfPositioningPaths;
		settings.uCommandQueueSize = m_CommandQueueSize;
		settings.uNumSamplesPerFrame = m_SamplesPerFrame;
		m_MainOutputSettings.CopyTo(settings.settingsMainOutput);
		settings.szPluginDLLPath = GetPluginPath();
		UnityEngine.Debug.Log("WwiseUnity: Setting Plugin DLL path to: " + (settings.szPluginDLLPath == null ? "NULL" : settings.szPluginDLLPath));
	}

	[UnityEngine.Tooltip("Multiplication factor for all streaming look-ahead heuristic values.")]
	[UnityEngine.Range(0, 1)]
	public float m_StreamingLookAheadRatio = 1.0f;

	public void CopyTo(AkMusicSettings settings)
	{
		settings.fStreamingLookAheadRatio = m_StreamingLookAheadRatio;
	}

	public void CopyTo(AkStreamMgrSettings settings)
	{
	}

	public virtual void CopyTo(AkDeviceSettings settings) { }

	[UnityEngine.Tooltip("Sampling Rate. Default is 48000 Hz. Use 24000hz for low quality. Any positive reasonable sample rate is supported; however, be careful setting a custom value. Using an odd or really low sample rate may cause the sound engine to malfunction.")]
	public uint m_SampleRate = 48000;

	[UnityEngine.Tooltip("Number of refill buffers in voice buffer. Set to 2 for double-buffered, defaults to 4.")]
	public ushort m_NumberOfRefillsInVoice = 4;

	partial void SetSampleRate(AkPlatformInitSettings settings);

	public virtual void CopyTo(AkPlatformInitSettings settings)
	{
		SetSampleRate(settings);
		settings.uNumRefillsInVoice = m_NumberOfRefillsInVoice;
	}

	[System.Serializable]
	public class SpatialAudioSettings
	{
		[UnityEngine.Tooltip("Maximum number of portals that sound can propagate through.")]
		[UnityEngine.Range(0, AkSoundEngine.AK_MAX_SOUND_PROPAGATION_DEPTH)]
		public uint m_MaxSoundPropagationDepth = AkSoundEngine.AK_MAX_SOUND_PROPAGATION_DEPTH;

		[UnityEngine.Tooltip("Distance (in game units) that an emitter or listener has to move to trigger a recalculation of reflections/diffraction. Larger values can reduce the CPU load at the cost of reduced accuracy.")]
		public float m_MovementThreshold = 1.0f;

		[UnityEngine.Tooltip("The number of primary rays used in stochastic ray casting.")]
		/// The number of primary rays used in stochastic ray casting.
		public uint m_NumberOfPrimaryRays = 100;

		[UnityEngine.Range(0, 4)]
		[UnityEngine.Tooltip("The maximum number of reflections that will be processed for a sound path before it reaches the listener.")]
		[UnityEngine.Serialization.FormerlySerializedAs("m_ReflectionsOrder")]
		/// The maximum number of reflections that will be processed for a sound path before it reaches the listener.
		/// Valid range: 1-4.
		public uint m_MaxReflectionOrder = 1;

		[UnityEngine.Tooltip("Length of the rays that are cast inside Spatial Audio. Effectively caps the maximum length of an individual segment in a reflection or diffraction path.")]
        /// Length of the rays that are cast inside Spatial Audio. Effectively caps the maximum length of an individual segment in a reflection or diffraction path.
        public float m_MaxPathLength = 10000.0f;

        [UnityEngine.Tooltip("Controls the maximum percentage of an audio frame used by the raytracing engine. Percentage [0, 100] of the current audio frame. A value of 0 indicates no limit on the amount of CPU used for raytracing.")]
		/// Controls the maximum percentage of an audio frame used by the raytracing engine. Percentage [0, 100] of the current audio frame. A value of 0 indicates no limit on the amount of CPU used for raytracing.
		public float m_CPULimitPercentage = 0.0f;

        [UnityEngine.Tooltip("Enable computation of diffraction along reflection paths.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_EnableDiffraction")]
        /// Enable computation of diffraction along reflection paths.
        public bool m_EnableDiffractionOnReflections = true;

		[UnityEngine.Tooltip("Enable computation of geometric diffraction and transmission paths for all sources that have that have the \"Enable Diffraction and Transmission\" box checked in the Positioning tab of the Wwise Property Editor. This flag enables sound paths around (diffraction) and thorugh (transmission) geometry. Setting to EnableGeometricDiffractionAndTransmission to false implies that geometry is only to be used for reflection calculation. Diffraction edges must be enabled on geometry for diffraction calculation. If EnableGeometricDiffractionAndTransmission is false but a sound has \"Enable Diffraction and Transmission\" checked in the positioning tab of the authoring tool, the sound will only diffract through portals but pass through geometry as if it is not there. One would typically disable this setting if the game intends to perform its own obstruction calculation, but in the situation where geometry is still passed to spatial audio for reflection calculation.")]
		[UnityEngine.Serialization.FormerlySerializedAs("m_EnableDirectPathDiffraction")]
		/// Enable direct path diffraction.
		public bool m_EnableGeometricDiffractionAndTransmission = true;

        [UnityEngine.Tooltip("An emitter that is diffracted through a portal or around geometry will have its apparent or virtual position calculated by Wwise Spatial Audio and passed on to the sound engine.")]
		/// An emitter that is diffracted through a portal or around geometry will have its apparent or virtual position calculated by Wwise Spatial Audio and passed on to the sound engine.
		public bool m_CalcEmitterVirtualPosition = true;

        [UnityEngine.Tooltip("Use the Wwise obstruction curve for modeling the effect of diffraction on a sound. Diffraction is only applied to sounds that have the \"Enable Diffraction and Transmission\" box checked in the Positioning tab of the Wwise Property Editor. Diffraction can also be applied using the diffraction built-in parameter, mapped to an RTPC (the built-in parameter is populated whether or not UseObstruction is checked). While the obstruction curve is a global setting for all sounds, using it to simulate diffraction is preferred over an RTPC, because it provides greater accuracy when modeling multiple diffraction paths, or a combination of diffraction and transmission paths. This is due to the fact that RTPCs can not be separately applied to individual sound paths. Only the path with the least amount of diffraction is sent to the RTPC.")]
		/// Use the Wwise obstruction curve for modeling the effect of diffraction on a sound. Diffraction is only applied to sounds that have the \"Enable Diffraction and Transmission\" box checked in the Positioning tab of the Wwise Property Editor. Diffraction can also be applied using the diffraction built-in parameter, mapped to an RTPC (the built-in parameter is populated whether or not UseObstruction is checked). While the obstruction curve is a global setting for all sounds, using it to simulate diffraction is preferred over an RTPC, because it provides greater accuracy when modeling multiple diffraction paths, or a combination of diffraction and transmission paths. This is due to the fact that RTPCs can not be separately applied to individual sound paths. Only the path with the least amount of diffraction is sent to the RTPC.
		public bool m_UseObstruction = true;

        [UnityEngine.Tooltip("Use the Wwise occlusion curve for modeling the effect of transmission loss on a sound. The transmission loss factor is applied using the occlusion curve defined in the wwise project settings. Transmission loss is only applied to sounds that have the \"Enable Diffraction and Transmission\" box checked in the Positioning tab of the Wwise Property Editor. Transmission loss can also be applied using the transmission loss built-in parameter, mapped to an RTPC (the built-in parameter is populated whether or not UseOcclusion is checked). While the occlusion curve is a global setting for all sounds, using it to simulate transmission loss is preferred over an RTPC, because it provides greater accuracy when modeling both transmission and diffraction. This is due to the fact that RTPCs can not be applied to individual sound paths, therefore any parameter mapped to a transmission loss RTPC will also affect any potential diffraction paths originating from an emitter.")]
		/// Use the Wwise occlusion curve for modeling the effect of transmission loss on a sound. The transmission loss factor is applied using the occlusion curve defined in the wwise project settings. Transmission loss is only applied to sounds that have the \"Enable Diffraction and Transmission\" box checked in the Positioning tab of the Wwise Property Editor. Transmission loss can also be applied using the transmission loss built-in parameter, mapped to an RTPC (the built-in parameter is populated whether or not UseOcclusion is checked). While the occlusion curve is a global setting for all sounds, using it to simulate transmission loss is preferred over an RTPC, because it provides greater accuracy when modeling both transmission and diffraction. This is due to the fact that RTPCs can not be applied to individual sound paths, therefore any parameter mapped to a transmission loss RTPC will also affect any potential diffraction paths originating from an emitter.
		public bool m_UseOcclusion = true;
	}

	[UnityEngine.Tooltip("Spatial audio common settings.")]
	public SpatialAudioSettings m_SpatialAudioSettings;

	public virtual void CopyTo(AkSpatialAudioInitSettings settings)
	{
		settings.uMaxSoundPropagationDepth = m_SpatialAudioSettings.m_MaxSoundPropagationDepth;
		settings.fMovementThreshold = m_SpatialAudioSettings.m_MovementThreshold;
		settings.uNumberOfPrimaryRays = m_SpatialAudioSettings.m_NumberOfPrimaryRays;
		settings.uMaxReflectionOrder = m_SpatialAudioSettings.m_MaxReflectionOrder;
		settings.fMaxPathLength = m_SpatialAudioSettings.m_MaxPathLength;
		settings.fCPULimitPercentage = m_SpatialAudioSettings.m_CPULimitPercentage;
		settings.bEnableDiffractionOnReflection = m_SpatialAudioSettings.m_EnableDiffractionOnReflections;
		settings.bEnableGeometricDiffractionAndTransmission = m_SpatialAudioSettings.m_EnableGeometricDiffractionAndTransmission;
		settings.bCalcEmitterVirtualPosition = m_SpatialAudioSettings.m_CalcEmitterVirtualPosition;
        settings.bUseObstruction = m_SpatialAudioSettings.m_UseObstruction;
        settings.bUseOcclusion = m_SpatialAudioSettings.m_UseOcclusion;
    }

	public virtual void CopyTo(AkUnityPlatformSpecificSettings settings) { }

	public virtual void Validate()
	{
		if (m_SpatialAudioSettings.m_MovementThreshold < 0.0f)
		{
			m_SpatialAudioSettings.m_MovementThreshold = 0.0f;
		}

		if (m_SpatialAudioSettings.m_MaxPathLength < 0.0f)
		{
			m_SpatialAudioSettings.m_MaxPathLength = 0.0f;
		}

		if (m_SpatialAudioSettings.m_CPULimitPercentage < 0.0f)
		{
			m_SpatialAudioSettings.m_CPULimitPercentage = 0.0f;
		}
		else if (m_SpatialAudioSettings.m_CPULimitPercentage > 100.0f)
		{
			m_SpatialAudioSettings.m_CPULimitPercentage = 100.0f;
		}
	}
}

[System.Serializable]
public class AkCommonAdvancedSettings
{
	[UnityEngine.Tooltip("Size of memory pool for I/O (for automatic streams). It is passed directly to AK::MemoryMgr::CreatePool(), after having been rounded down to a multiple of uGranularity.")]
	public uint m_IOMemorySize = 2 * 1024 * 1024;

	[UnityEngine.Tooltip("Targeted automatic stream buffer length (ms). When a stream reaches that buffering, it stops being scheduled for I/O except if the scheduler is idle.")]
	public float m_TargetAutoStreamBufferLengthMs = 380.0f;

	[UnityEngine.Tooltip("If true the device attempts to reuse IO buffers that have already been streamed from disk. This is particularly useful when streaming small looping sounds. The drawback is a small CPU hit when allocating memory, and a slightly larger memory footprint in the StreamManager pool.")]
	public bool m_UseStreamCache = false;

	[UnityEngine.Tooltip("Maximum number of bytes that can be \"pinned\" using AK::SoundEngine::PinEventInStreamCache() or AK::IAkStreamMgr::PinFileInCache()")]
	public uint m_MaximumPinnedBytesInCache = unchecked((uint)(-1));

	public virtual void CopyTo(AkDeviceSettings settings)
	{
		settings.uIOMemorySize = m_IOMemorySize;
		settings.fTargetAutoStmBufferLength = m_TargetAutoStreamBufferLengthMs;
		settings.bUseStreamCache = m_UseStreamCache;
		settings.uMaxCachePinnedBytes = m_MaximumPinnedBytesInCache;
	}

	[UnityEngine.Tooltip("Set to true to enable AK::SoundEngine::PrepareGameSync usage.")]
	public bool m_EnableGameSyncPreparation = false;

	[UnityEngine.Tooltip("Number of quanta ahead when continuous containers should instantiate a new voice before which next sounds should start playing. This look-ahead time allows I/O to occur, and is especially useful to reduce the latency of continuous containers with trigger rate or sample-accurate transitions.")]
	public uint m_ContinuousPlaybackLookAhead = 1;

	[UnityEngine.Tooltip("Size of the monitoring queue pool. This parameter is not used in Release build.")]
	public uint m_MonitorQueuePoolSize = 1024 * 1024;

	[UnityEngine.Tooltip("Amount of time to wait for hardware devices to trigger an audio interrupt. If there is no interrupt after that time, the sound engine will revert to silent mode and continue operating until the hardware finally comes back.")]
	public uint m_MaximumHardwareTimeoutMs = 1000;

	[UnityEngine.Tooltip("Debug setting: Enable checks for out-of-range (and NAN) floats in the processing code. Do not enable in any normal usage, this setting uses a lot of CPU. Will print error messages in the log if invalid values are found at various point in the pipeline. Contact AK Support with the new error messages for more information.")]
	public bool m_DebugOutOfRangeCheckEnabled = false;

	[UnityEngine.Tooltip("Debug setting: Only used when bDebugOutOfRangeCheckEnabled is true. This defines the maximum values samples can have. Normal audio must be contained within +1/-1. This limit should be set higher to allow temporary or short excursions out of range. Default is 16.")]
	public float m_DebugOutOfRangeLimit = 16.0f;

	public virtual void CopyTo(AkInitSettings settings)
	{
		settings.bEnableGameSyncPreparation = m_EnableGameSyncPreparation;
		settings.uContinuousPlaybackLookAhead = m_ContinuousPlaybackLookAhead;
		settings.uMonitorQueuePoolSize = m_MonitorQueuePoolSize;
		settings.uMaxHardwareTimeoutMs = m_MaximumHardwareTimeoutMs;
		settings.bDebugOutOfRangeCheckEnabled = m_DebugOutOfRangeCheckEnabled;
		settings.fDebugOutOfRangeLimit = m_DebugOutOfRangeLimit;
	}

	public virtual void CopyTo(AkPlatformInitSettings settings) { }

	public virtual void CopyTo(AkUnityPlatformSpecificSettings settings) { }

	[UnityEngine.Tooltip("The state of the \"in_bRenderAnyway\" argument passed to the AkSoundEngine.Suspend() function when the \"OnApplicationFocus\" Unity callback is received with \"false\" as its argument.")]
	public bool m_RenderDuringFocusLoss;

	[UnityEngine.Tooltip("Sets the sub-folder underneath UnityEngine.Application.persistentDataPath that will be used as the SoundBank base path. This is useful when the Init.bnk needs to be downloaded. Setting this to an empty string uses the typical SoundBank base path resolution. Setting this to \".\" uses UnityEngine.Application.persistentDataPath.")]
	public string m_SoundBankPersistentDataPath;

	[UnityEngine.Tooltip("Use Async Open in the low-level IO hook.")]
	public bool m_UseAsyncOpen = false;
}

[System.Serializable]
public class AkCommonCommSettings
{
	[UnityEngine.Tooltip("Size of the communication pool.")]
	public uint m_PoolSize = 256 * 1024;

	public static ushort DefaultDiscoveryBroadcastPort = 24024;

	[UnityEngine.Tooltip("The port where the authoring application broadcasts \"Game Discovery\" requests to discover games running on the network. Default value: 24024. (Cannot be set to 0)")]
	public ushort m_DiscoveryBroadcastPort = DefaultDiscoveryBroadcastPort;

	[UnityEngine.Tooltip("The \"command\" channel port. Set to 0 to request a dynamic/ephemeral port.")]
	public ushort m_CommandPort;

	[UnityEngine.Tooltip("The \"notification\" channel port. Set to 0 to request a dynamic/ephemeral port.")]
	public ushort m_NotificationPort;

	[UnityEngine.Tooltip("Indicates whether the communication system should be initialized. Some consoles have critical requirements for initialization of their communications system. Set to false only if your game already uses sockets before sound engine initialization.")]
	public bool m_InitializeSystemComms = true;

	[UnityEngine.Tooltip("The name used to identify this game within the authoring application. Leave empty to use \"UnityEngine.Application.productName\".")]
	public string m_NetworkName;

	[UnityEngine.Tooltip("HTCS communication can only be used with a Nintendo Switch Development Build")]
	public AkCommunicationSettings.AkCommSystem m_commSystem = AkCommunicationSettings.AkCommSystem.AkCommSystem_Socket;

	public virtual void CopyTo(AkCommunicationSettings settings)
	{
		settings.uPoolSize = m_PoolSize;
		settings.uDiscoveryBroadcastPort = m_DiscoveryBroadcastPort;
		settings.uCommandPort = m_CommandPort;
		settings.uNotificationPort = m_NotificationPort;
		settings.bInitSystemLib = m_InitializeSystemComms;
		settings.commSystem = m_commSystem;

		string networkName = m_NetworkName;
		if (string.IsNullOrEmpty(networkName))
			networkName = UnityEngine.Application.productName;

#if UNITY_EDITOR
		networkName += " (Editor)";
#endif

		settings.szAppNetworkName = networkName;
	}

	public virtual void Validate() { }
}

public abstract class AkCommonPlatformSettings : AkBasePlatformSettings
{
	protected abstract AkCommonUserSettings GetUserSettings();

	protected abstract AkCommonAdvancedSettings GetAdvancedSettings();

	protected abstract AkCommonCommSettings GetCommsSettings();

	public override AkInitializationSettings AkInitializationSettings
	{
		get
		{
			var settings = base.AkInitializationSettings;
			var userSettings = GetUserSettings();
			userSettings.CopyTo(settings.deviceSettings);
			userSettings.CopyTo(settings.streamMgrSettings);
			userSettings.CopyTo(settings.initSettings);
			userSettings.CopyTo(settings.platformSettings);
			userSettings.CopyTo(settings.musicSettings);
			userSettings.CopyTo(settings.unityPlatformSpecificSettings);

			var advancedSettings = GetAdvancedSettings();
			advancedSettings.CopyTo(settings.deviceSettings);
			advancedSettings.CopyTo(settings.initSettings);
			advancedSettings.CopyTo(settings.platformSettings);
			advancedSettings.CopyTo(settings.unityPlatformSpecificSettings);

			settings.useAsyncOpen = advancedSettings.m_UseAsyncOpen;

			return settings;
		}
	}

	public override AkSpatialAudioInitSettings AkSpatialAudioInitSettings
	{
		get
		{
			var settings = base.AkSpatialAudioInitSettings;
			GetUserSettings().CopyTo(settings);
			return settings;
		}
	}

	public override AkCallbackManager.InitializationSettings CallbackManagerInitializationSettings
	{
		get
		{
			return new AkCallbackManager.InitializationSettings { IsLoggingEnabled = GetUserSettings().m_EngineLogging };
		}
	}

	public override string InitialLanguage
	{
		get { return GetUserSettings().m_StartupLanguage; }
	}

	public override string SoundBankPersistentDataPath
	{
		get { return GetAdvancedSettings().m_SoundBankPersistentDataPath; }
	}

	public override bool RenderDuringFocusLoss
	{
		get { return GetAdvancedSettings().m_RenderDuringFocusLoss; }
	}

	public override string SoundbankPath
	{
		get { return GetUserSettings().m_BasePath; }
	}

	public override bool UseAsyncOpen
	{
		get { return GetAdvancedSettings().m_UseAsyncOpen; }
	}

	public override AkCommunicationSettings AkCommunicationSettings
	{
		get
		{
			var settings = base.AkCommunicationSettings;
			GetCommsSettings().CopyTo(settings);
			return settings;
		}
	}

#region parameter validation
#if UNITY_EDITOR
	void OnValidate()
	{
		GetUserSettings().Validate();
		GetCommsSettings().Validate();
	}
#endif
#endregion
}
