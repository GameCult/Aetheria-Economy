/*

The content of this file includes portions of the AUDIOKINETIC Wwise Technology
released in source code form as part of the SDK installer package.

Commercial License Usage

Licensees holding valid commercial licenses to the AUDIOKINETIC Wwise Technology
may use this file in accordance with the end user license agreement provided 
with the software or, alternatively, in accordance with the terms contained in a
written agreement between you and Audiokinetic Inc.

Apache License Usage

Alternatively, this file may be used under the Apache License, Version 2.0 (the 
"Apache License"); you may not use this file except in compliance with the 
Apache License. You may obtain a copy of the Apache License at 
http://www.apache.org/licenses/LICENSE-2.0.

Unless required by applicable law or agreed to in writing, software distributed
under the Apache License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES
OR CONDITIONS OF ANY KIND, either express or implied. See the Apache License for
the specific language governing permissions and limitations under the License.

  Version: <VERSION>  Build: <BUILDNUMBER>
  Copyright (c) <COPYRIGHTYEAR> Audiokinetic Inc.

*/
/// <summary>URI strings to use in WAAPI calls. For a complete description, refer to the official Wwise SDK documentation.</summary>
public class ak
{
	public class soundengine
	{
		/// <summary>Set multiple positions for a single game object. Setting multiple positions for a single game object is a way to simulate multiple emission sources while using the resources of only one voice. This can be used to simulate wall openings, area sounds, or multiple objects emitting the same sound in the same area. See <tt>AK::SoundEngine::SetMultiplePositions</tt>.</summary>
		public const string setMultiplePositions = "ak.soundengine.setMultiplePositions";
		/// <summary>Set the scaling factor of a game object. Modify the attenuation computations on this game object to simulate sounds with a larger or smaller area of effect. See <tt>AK::SoundEngine::SetScalingFactor</tt>.</summary>
		public const string setScalingFactor = "ak.soundengine.setScalingFactor";
		/// <summary>Asynchronously post an Event to the sound engine (by event ID). See <tt>AK::SoundEngine::PostEvent</tt>.</summary>
		public const string postEvent = "ak.soundengine.postEvent";
		/// <summary>Set the value of a real-time parameter control. See <tt>AK::SoundEngine::SetRTPCValue</tt>.</summary>
		public const string setRTPCValue = "ak.soundengine.setRTPCValue";
		/// <summary>Set a game object's obstruction and occlusion levels. This function is used to affect how an object should be heard by a specific listener. See <tt>AK::SoundEngine::SetObjectObstructionAndOcclusion</tt>.</summary>
		public const string setObjectObstructionAndOcclusion = "ak.soundengine.setObjectObstructionAndOcclusion";
		/// <summary>Set a single game object's active listeners. By default, all new game objects have no listeners active, but this behavior can be overridden with <tt>SetDefaultListeners()</tt>. Inactive listeners are not computed. See <tt>AK::SoundEngine::SetListeners</tt>.</summary>
		public const string setListeners = "ak.soundengine.setListeners";
		/// <summary>Execute an action on all nodes that are referenced in the specified event in an action of type play. See <tt>AK::SoundEngine::ExecuteActionOnEvent</tt>.</summary>
		public const string executeActionOnEvent = "ak.soundengine.executeActionOnEvent";
		/// <summary>Set a listener's spatialization parameters. This lets you define listener-specific volume offsets for each audio channel. See <tt>AK::SoundEngine::SetListenerSpatialization</tt>.</summary>
		public const string setListenerSpatialization = "ak.soundengine.setListenerSpatialization";
		/// <summary>Reset the value of a real-time parameter control to its default value, as specified in the Wwise project. See <tt>AK::SoundEngine::ResetRTPCValue</tt>.</summary>
		public const string resetRTPCValue = "ak.soundengine.resetRTPCValue";
		/// <summary>Unregister a game object. Registering a game object twice does nothing. Unregistering it once unregisters it no matter how many times it has been registered. Unregistering a game object while it is in use is allowed, but the control over the parameters of this game object is lost. For example, say a sound associated with this game object is a 3D moving sound. It will stop moving when the game object is unregistered, and there will be no way to regain control over the game object. See <tt>AK::SoundEngine::UnregisterGameObj</tt>.</summary>
		public const string unregisterGameObj = "ak.soundengine.unregisterGameObj";
		/// <summary>Stop the current content, associated to the specified playing ID, from playing. See <tt>AK::SoundEngine::StopPlayingID</tt>.</summary>
		public const string stopPlayingID = "ak.soundengine.stopPlayingID";
		/// <summary>Set the Auxiliary Busses to route the specified game object. See <tt>AK::SoundEngine::SetGameObjectAuxSendValues</tt>.</summary>
		public const string setGameObjectAuxSendValues = "ak.soundengine.setGameObjectAuxSendValues";
		/// <summary>Seek inside all playing objects that are referenced in Play Actions of the specified Event. See <tt>AK::SoundEngine::SeekOnEvent</tt>.</summary>
		public const string seekOnEvent = "ak.soundengine.seekOnEvent";
		/// <summary>Register a game object. Registering a game object twice does nothing. Unregistering it once unregisters it no matter how many times it has been registered. See <tt>AK::SoundEngine::RegisterGameObj</tt>.</summary>
		public const string registerGameObj = "ak.soundengine.registerGameObj";
		/// <summary>Set a the default active listeners for all subsequent game objects that are registered. See <tt>AK::SoundEngine::SetDefaultListeners</tt>.</summary>
		public const string setDefaultListeners = "ak.soundengine.setDefaultListeners";
		/// <summary>Set the position of a game object. See <tt>AK::SoundEngine::SetPosition</tt>.</summary>
		public const string setPosition = "ak.soundengine.setPosition";
		/// <summary>Display a message in the profiler's Capture Log view.</summary>
		public const string postMsgMonitor = "ak.soundengine.postMsgMonitor";
		/// <summary>Set the output bus volume (direct) to be used for the specified game object. See <tt>AK::SoundEngine::SetGameObjectOutputBusVolume</tt>.</summary>
		public const string setGameObjectOutputBusVolume = "ak.soundengine.setGameObjectOutputBusVolume";
		/// <summary>Set the State of a Switch Group. See <tt>AK::SoundEngine::SetSwitch</tt>.</summary>
		public const string setSwitch = "ak.soundengine.setSwitch";
		/// <summary>Stop playing the current content associated to the specified game object ID. If no game object is specified, all sounds will be stopped. See <tt>AK::SoundEngine::StopAll</tt>.</summary>
		public const string stopAll = "ak.soundengine.stopAll";
		/// <summary>Post the specified Trigger. See <tt>AK::SoundEngine::PostTrigger</tt>.</summary>
		public const string postTrigger = "ak.soundengine.postTrigger";

		public class error
		{
			public const string invalid_playing_id = "ak.soundengine.invalid_playing_id";
			public const string wrong_volumeOffsets_length = "ak.soundengine.wrong_volumeOffsets_length";
		}
	}
	public class wwise
	{
		public class error
		{
			public const string invalid_arguments = "ak.wwise.invalid_arguments";
			public const string invalid_options = "ak.wwise.invalid_options";
			public const string invalid_json = "ak.wwise.invalid_json";
			public const string invalid_object = "ak.wwise.invalid_object";
			public const string invalid_property = "ak.wwise.invalid_property";
			public const string invalid_reference = "ak.wwise.invalid_reference";
			public const string invalid_query = "ak.wwise.query.invalid_query";
			public const string file_error = "ak.wwise.file_error";
			public const string unavailable = "ak.wwise.unavailable";
			public const string unexpected_error = "ak.wwise.unexpected_error";
			public const string locked = "ak.wwise.locked";
			public const string connection_failed = "ak.wwise.connection_failed";
			public const string already_connected = "ak.wwise.already_connected";
			public const string wwise_console = "ak.wwise.wwise_console";
		}
		public class debug
		{
			/// <summary>Private use only.</summary>
			public const string testAssert = "ak.wwise.debug.testAssert";
			/// <summary>Sent when an assert has failed. This is only available with Debug builds.</summary>
			public const string assertFailed = "ak.wwise.debug.assertFailed";
			/// <summary>Enable or disable the automation mode for Wwise. This reduces the potential interruptions caused by message boxes and dialogs. For instance, enabling the automation mode silently accepts: project migration, project load log, EULA acceptance, project licence display and generic message boxes.</summary>
			public const string enableAutomationMode = "ak.wwise.debug.enableAutomationMode";
			/// <summary>Enables debug assertions. Every call to enableAsserts with false increments the ref count. Calling with true will decrement the ref count. This is only available with Debug builds.</summary>
			public const string enableAsserts = "ak.wwise.debug.enableAsserts";
		}
		public class core
		{
			public class audioSourcePeaks
			{
				/// <summary>Get the min/max peak pairs, in a given region of an audio source, as a collection of binary strings (one per channel). The strings are base-64 encoded 16-bit signed int arrays, with min and max values being interleaved. If getCrossChannelPeaks is true, there will be only one binary string representing peaks across all channels globally.</summary>
				public const string getMinMaxPeaksInRegion = "ak.wwise.core.audioSourcePeaks.getMinMaxPeaksInRegion";
				/// <summary>Get the min/max peak pairs in the entire trimmed region of an audio source, for each channel, as an array of binary strings (one per channel). The strings are base-64 encoded 16-bit signed int arrays, with min and max values being interleaved. If getCrossChannelPeaks is true, there will be only one binary string representing peaks across all channels globally.</summary>
				public const string getMinMaxPeaksInTrimmedRegion = "ak.wwise.core.audioSourcePeaks.getMinMaxPeaksInTrimmedRegion";
			}
			public class remote
			{
				/// <summary>Retrieves the connection status.</summary>
				public const string getConnectionStatus = "ak.wwise.core.remote.getConnectionStatus";
				/// <summary>Retrieves all consoles available for connecting Wwise Authoring to a Sound Engine instance.</summary>
				public const string getAvailableConsoles = "ak.wwise.core.remote.getAvailableConsoles";
				/// <summary>Disconnects the Wwise Authoring application from a connected Wwise Sound Engine running executable.</summary>
				public const string disconnect = "ak.wwise.core.remote.disconnect";
				/// <summary>Connects the Wwise Authoring application to a Wwise Sound Engine running executable. The host must be running code with communication enabled.</summary>
				public const string connect = "ak.wwise.core.remote.connect";
			}
			public class log
			{
				/// <summary>Sent when an item is added to the log. This could be used to retrieve items added to the SoundBank generation log. To retrieve the complete log, refer to ak.wwise.core.log.get.</summary>
				public const string itemAdded = "ak.wwise.core.log.itemAdded";
				/// <summary>Retrieve the latest log for a specific channel. Refer to ak.wwise.core.log.itemadded to be notified when an item is added to the log.</summary>
				public const string get = "ak.wwise.core.log.get";
			}
			/// <summary>Retrieve global Wwise information.</summary>
			public const string getInfo = "ak.wwise.core.getInfo";
			public class @object
			{
				/// <summary>Sent when an object reference is changed.</summary>
				public const string referenceChanged = "ak.wwise.core.object.referenceChanged";
				/// <summary>Moves an object to the given parent. Returns the moved object.</summary>
				public const string move = "ak.wwise.core.object.move";
				/// <summary>Sent when an attenuation curve's link/unlink is changed.</summary>
				public const string attenuationCurveLinkChanged = "ak.wwise.core.object.attenuationCurveLinkChanged";
				/// <summary>Sent when an object is added as a child to another object.</summary>
				public const string childAdded = "ak.wwise.core.object.childAdded";
				/// <summary>Retrieves the list of all object types registered in Wwise's object model.</summary>
				public const string getTypes = "ak.wwise.core.object.getTypes";
				/// <summary>Sent when the watched property of an object changes.</summary>
				public const string propertyChanged = "ak.wwise.core.object.propertyChanged";
				/// <summary>Creates an object of type 'type', as a child of 'parent'. Refer to ak.wwise.core.audio.import to import audio files to Wwise.</summary>
				public const string create = "ak.wwise.core.object.create";
				/// <summary>Performs a query, returns specified data for each object in query result.</summary>
				public const string get = "ak.wwise.core.object.get";
				/// <summary>Sent prior to an object's deletion.</summary>
				public const string preDeleted = "ak.wwise.core.object.preDeleted";
				/// <summary>Sent when an object is renamed. Publishes the object which the name was changed.</summary>
				public const string nameChanged = "ak.wwise.core.object.nameChanged";
				/// <summary>Sent following an object's deletion.</summary>
				public const string postDeleted = "ak.wwise.core.object.postDeleted";
				/// <summary>Sent when the object's notes are changed.</summary>
				public const string notesChanged = "ak.wwise.core.object.notesChanged";
				/// <summary>Retrieves information about an object property.</summary>
				public const string getPropertyInfo = "ak.wwise.core.object.getPropertyInfo";
				/// <summary>Renames an object.</summary>
				public const string setName = "ak.wwise.core.object.setName";
				/// <summary>Sets the object's notes.</summary>
				public const string setNotes = "ak.wwise.core.object.setNotes";
				/// <summary>Sets the specified attenuation curve for a given attenuation object.</summary>
				public const string setAttenuationCurve = "ak.wwise.core.object.setAttenuationCurve";
				/// <summary>Sets a property value of an object for a specific platform. Refer to  ak.wwise.core.object.setreference to set a reference to an object.</summary>
				public const string setProperty = "ak.wwise.core.object.setProperty";
				/// <summary>Copies an object to the given parent.</summary>
				public const string copy = "ak.wwise.core.object.copy";
				/// <summary>Return true if a property is enabled based on the values of the properties it depends on.</summary>
				public const string isPropertyEnabled = "ak.wwise.core.object.isPropertyEnabled";
				/// <summary>Sets the randomizer values of a property of an object for a specific platform.</summary>
				public const string setRandomizer = "ak.wwise.core.object.setRandomizer";
				/// <summary>Sets an object's reference value.</summary>
				public const string setReference = "ak.wwise.core.object.setReference";
				/// <summary>Sent when an attenuation curve is changed.</summary>
				public const string attenuationCurveChanged = "ak.wwise.core.object.attenuationCurveChanged";
				/// <summary>Sent when an object is created.</summary>
				public const string created = "ak.wwise.core.object.created";
				/// <summary>Sent when an object is removed from the children of another object.</summary>
				public const string childRemoved = "ak.wwise.core.object.childRemoved";
				/// <summary>Retrieves the list of property and reference names for an object.</summary>
				public const string getPropertyNames = "ak.wwise.core.object.getPropertyNames";
				/// <summary>Gets the specified attenuation curve for a given attenuation object.</summary>
				public const string getAttenuationCurve = "ak.wwise.core.object.getAttenuationCurve";
				/// <summary>Sent when one or many curves are changed.</summary>
				public const string curveChanged = "ak.wwise.core.object.curveChanged";
				/// <summary>Deletes the specified object.</summary>
				public const string delete = "ak.wwise.core.object.delete";
				/// <summary>Retrieves the list of property and reference names for an object.</summary>
				public const string getPropertyAndReferenceNames = "ak.wwise.core.object.getPropertyAndReferenceNames";
			}
			public class undo
			{
				/// <summary>Ends the last undo group.</summary>
				public const string endGroup = "ak.wwise.core.undo.endGroup";
				/// <summary>Cancels the last undo group. Please note that this does not revert the operations made since the last ak.wwise.core.undo.begingroup call.</summary>
				public const string cancelGroup = "ak.wwise.core.undo.cancelGroup";
				/// <summary>Begins an undo group. Make sure to call ak.wwise.core.undo.endgroup exactly once for every ak.wwise.core.beginUndoGroup call you make. Calls to ak.wwise.core.undo.beginGroup can be nested.</summary>
				public const string beginGroup = "ak.wwise.core.undo.beginGroup";
			}
			public class profiler
			{
				/// <summary>Returns the current time of the specified profiler cursor in milliseconds.</summary>
				public const string getCursorTime = "ak.wwise.core.profiler.getCursorTime";
				/// <summary>Start the profiler capture and return the time at the beginning of the capture in milliseconds.</summary>
				public const string startCapture = "ak.wwise.core.profiler.startCapture";
				/// <summary>Retrieves all parameters affecting voice volume, highpass and lowpass for a voice path, resolved from pipeline IDs.</summary>
				public const string getVoiceContributions = "ak.wwise.core.profiler.getVoiceContributions";
				/// <summary>Retrieves the voices at a specific profiler capture time.</summary>
				public const string getVoices = "ak.wwise.core.profiler.getVoices";
				/// <summary>Retrieves the busses at a specific profiler capture time.</summary>
				public const string getBusses = "ak.wwise.core.profiler.getBusses";
				/// <summary>Stop the profiler capture and return the time at the end of the capture in milliseconds.</summary>
				public const string stopCapture = "ak.wwise.core.profiler.stopCapture";
			}
			public class project
			{
				/// <summary>Sent when the after the project is completely closed.</summary>
				public const string postClosed = "ak.wwise.core.project.postClosed";
				/// <summary>Sent when the project has been successfully loaded.</summary>
				public const string loaded = "ak.wwise.core.project.loaded";
				/// <summary>Sent when the project begins closing.</summary>
				public const string preClosed = "ak.wwise.core.project.preClosed";
				/// <summary>Saves the current project.</summary>
				public const string save = "ak.wwise.core.project.save";
				/// <summary>Sent when the project has been saved.</summary>
				public const string saved = "ak.wwise.core.project.saved";
			}
			public class transport
			{
				/// <summary>Gets the state of the given transport object.</summary>
				public const string getState = "ak.wwise.core.transport.getState";
				/// <summary>Sent when the transport's state has changed.</summary>
				public const string stateChanged = "ak.wwise.core.transport.stateChanged";
				/// <summary>Creates a transport object for the given Wwise object.  The return transport object can be used to play, stop, pause and resume the Wwise object via the other transport functions.</summary>
				public const string create = "ak.wwise.core.transport.create";
				/// <summary>Returns the list of transport objects.</summary>
				public const string getList = "ak.wwise.core.transport.getList";
				/// <summary>Destroys the given transport object.</summary>
				public const string destroy = "ak.wwise.core.transport.destroy";
				/// <summary>Executes an action on the given transport object, or all transports if no transport is specified.</summary>
				public const string executeAction = "ak.wwise.core.transport.executeAction";
			}
			public class soundbank
			{
				/// <summary>Retrieves a SoundBank's inclusion list.</summary>
				public const string getInclusions = "ak.wwise.core.soundbank.getInclusions";
				/// <summary>Sent when a single SoundBank is generated. This could be sent multiple times during SoundBank generation, for every SoundBank generated and for every platform. To generate SoundBanks, refer to ak.wwise.ui.commands.execute with one of the SoundBank generation commands.</summary>
				public const string generated = "ak.wwise.core.soundbank.generated";
				/// <summary>Modifies a SoundBank's inclusion list.  The 'operation' argument determines how the 'inclusions' argument modifies the SoundBank's inclusion list; 'inclusions' may be added to / removed from / replace the SoundBank's inclusion list.</summary>
				public const string setInclusions = "ak.wwise.core.soundbank.setInclusions";
			}
			public class audio
			{
				/// <summary>Create Wwise objects and import audio files. This function is using the same importation processor available through the Tab Delimited import in the Audio File Importer. The function returns an array of all objects created, replaced or re-used. Use the options to specify how the objects are returned.</summary>
				public const string import = "ak.wwise.core.audio.import";
				/// <summary>Scripted object creation and audio file import from a tab-delimited file.</summary>
				public const string importTabDelimited = "ak.wwise.core.audio.importTabDelimited";
				/// <summary>Sent at the end of an import operation.</summary>
				public const string imported = "ak.wwise.core.audio.imported";
			}
			public class switchContainer
			{
				/// <summary>Remove an assignment between a Switch Container's child and a State.</summary>
				public const string removeAssignment = "ak.wwise.core.switchContainer.removeAssignment";
				/// <summary>Returns the list of assignments between a Switch Container's children and states.</summary>
				public const string getAssignments = "ak.wwise.core.switchContainer.getAssignments";
				/// <summary>Sent when an assignment is removed from a Switch Container.</summary>
				public const string assignmentRemoved = "ak.wwise.core.switchContainer.assignmentRemoved";
				/// <summary>Assign a Switch Container's child to a Switch. This is the equivalent of doing a drag&drop of the child to a state in the Assigned Objects view. The child is always added at the end for each state.</summary>
				public const string addAssignment = "ak.wwise.core.switchContainer.addAssignment";
				/// <summary>Sent when an assignment is added to a Switch Container.</summary>
				public const string assignmentAdded = "ak.wwise.core.switchContainer.assignmentAdded";
			}
			public class plugin
			{
				/// <summary>Retrieves the list of all object types registered in Wwise's object model.</summary>
				public const string getList = "ak.wwise.core.plugin.getList";
				/// <summary>Retrieves information about an object property.</summary>
				public const string getProperty = "ak.wwise.core.plugin.getProperty";
				/// <summary>Retrieves the list of property and reference names for an object.</summary>
				public const string getProperties = "ak.wwise.core.plugin.getProperties";
			}
		}
		public class ui
		{
			public class project
			{
				/// <summary>Closes the current project.</summary>
				public const string close = "ak.wwise.ui.project.close";
				/// <summary>Opens a project, specified by path. Please refer to ak.wwise.core.project.loaded for further explanations on how to be notified when the operation has completed.</summary>
				public const string open = "ak.wwise.ui.project.open";
			}
			/// <summary>Bring Wwise main window to foreground. Refer to SetForegroundWindow and AllowSetForegroundWindow on MSDN for more information on the restrictions. Refer to ak.wwise.core.getInfo to obtain the Wwise process ID for AllowSetForegroundWindow.</summary>
			public const string bringToForeground = "ak.wwise.ui.bringToForeground";
			public class commands
			{
				/// <summary>Unregister an array of add-on UI commands.</summary>
				public const string unregister = "ak.wwise.ui.commands.unregister";
				/// <summary>Sent when a command is executed. The objects for which the command is executed are sent in the publication.</summary>
				public const string executed = "ak.wwise.ui.commands.executed";
				/// <summary>Executes a command. Some commands can take a list of objects as parameter.</summary>
				public const string execute = "ak.wwise.ui.commands.execute";
				/// <summary>Register an array of add-on commands. Registered commands remain until the Wwise process is terminated. Refer to to ak.wwise.ui.commands.executed.</summary>
				public const string register = "ak.wwise.ui.commands.register";
				/// <summary>Get the list of commands.</summary>
				public const string getCommands = "ak.wwise.ui.commands.getCommands";
			}
			/// <summary>Retrieves the list of objects currently selected by the user in the active view.</summary>
			public const string getSelectedObjects = "ak.wwise.ui.getSelectedObjects";
			/// <summary>Sent when the selection changes in the project.</summary>
			public const string selectionChanged = "ak.wwise.ui.selectionChanged";
		}
		public class waapi
		{
			/// <summary>Retrieve the list of topics to which a client can subscribe.</summary>
			public const string getTopics = "ak.wwise.waapi.getTopics";
			/// <summary>Retrieve the list of functions.</summary>
			public const string getFunctions = "ak.wwise.waapi.getFunctions";
			/// <summary>Retrieve the JSON schema of a Waapi URI.</summary>
			public const string getSchema = "ak.wwise.waapi.getSchema";
		}
	}
}
