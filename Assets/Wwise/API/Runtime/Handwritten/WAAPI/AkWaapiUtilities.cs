#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// This class wraps the client that communicates with the Wwise Authoring application via WAAPI.
/// Given that only one request can be pending on the websocket, a queue is used to consume all calls sequentially.
/// Messages sent to WAAPI use the JSON format and are serialized by Unity Json serialization. 
/// Helper classes (\ref WaapiHelper) for serialization, keywords for WAAPI commands (\ref WaapiKeywords), and classes for serializing message arguments and deserializing responses are found in AkWaapiHelper.cs.
/// Uri.cs contains classes with fields containing URI strings for WAAPI calls and error messages.
/// </summary>
[UnityEditor.InitializeOnLoad]
public class AkWaapiUtilities
{
	static private AkWaapiClient m_WaapiClient;
	static bool isDisconnecting;
	static Dictionary<System.Guid, TransportInfo> m_ItemTransports;
	public static string ErrorMessage;
	/// <summary>
	/// Fired when the connection is closing or closed, the bool parameter represents whether the socket connection is still open (for cleaning up subscriptions).
	/// </summary>
	public static System.Action<bool> Disconnecting;

	/// <summary>
	/// Fired when the connection is established, should be used by external classes to subscribe to topics they are interested in.
	/// </summary>
	public static System.Action Connected;

	/// <summary>
	/// Fired when all commands in the queue have been executed.
	/// </summary>
	public static System.Action QueueConsumed;
	private static ConcurrentQueue<WaapiCommand> waapiCommandQueue = new ConcurrentQueue<WaapiCommand>();


	/// <summary>
	/// Generic delegate function for callbacks that expect to receive a list of objects in response to a WAAPI request.
	/// </summary>
	/// <param name="result"></param>
	public delegate void GetResultListDelegate<T>(List<T> result);

	/// <summary>
	/// Generic delegate function for callbacks that expect to receive a single object in response to a WAAPI request.
	/// </summary>
	/// <param name="result"></param>
	public delegate void GetResultDelegate<T>(T result);

	/// <summary>
	/// Used to store store UnityEngine.Application.dataPath because we can't access it outside of the main loop
	/// </summary>
	private static string dataPath;

	/// <summary>
	/// Bind disconnection method to compilation started delegate and start the async Waapi loop.
	/// </summary>
	static AkWaapiUtilities()
	{
#if UNITY_2019_1_OR_NEWER
		UnityEditor.Compilation.CompilationPipeline.compilationStarted += (object context) => FireDisconnect(true);
#else
		UnityEditor.Compilation.CompilationPipeline.assemblyCompilationStarted +=
			(string assemblyPath) =>
			{
				if (assemblyPath == "Library/ScriptAssemblies/AK.Wwise.Unity.API.dll")
					FireDisconnect(true);
			};
#endif
		isDisconnecting = false;
		dataPath = UnityEngine.Application.dataPath;
		Loop();
	}

	/// <summary>
	/// A simple structure containing an async payload function that will be executed when it is consumed by the command queue.
	/// </summary>
	public struct WaapiCommand
	{
		System.Func<Task> payload;
		public WaapiCommand(System.Func<Task> payload)
		{
			this.payload = payload;
		}
		public async Task Execute()
		{
			await payload.Invoke();
		}
	}

	/// <summary>
	/// Class used to store information about a specific subscription.
	/// </summary>
	public class SubscriptionInfo
	{
		public string Uri;
		public Wamp.PublishHandler Callback;
		public uint SubscriptionId;

		public SubscriptionInfo(string uri, Wamp.PublishHandler cb)
		{
			Uri = uri;
			Callback = cb;
			SubscriptionId = 0;
		}
	};

	/// <summary>
	/// Holds information about a playing transport.
	/// </summary>
	struct TransportInfo
	{
		public int TransportID;
		public uint SubscriptionID;

		public TransportInfo(int transID, uint subsID)
		{
			TransportID = transID;
			SubscriptionID = subsID;
		}
	};

	/// <summary>
	/// Stores TransportInfo of playing Events.
	/// </summary>
	private static Dictionary<System.Guid, TransportInfo> ItemTransports
	{
		get
		{
			if (m_ItemTransports == null)
				m_ItemTransports = new Dictionary<System.Guid, TransportInfo>();
			return m_ItemTransports;
		}
	}

	/// <summary>
	/// WAAPI client wrapping WAMP calls. Lazy instantiated.
	/// </summary>
	private static AkWaapiClient WaapiClient
	{
		get
		{
			if (m_WaapiClient == null)
			{
				m_WaapiClient = new AkWaapiClient();
				m_WaapiClient.Disconnected += Disconnected;
			}
			return m_WaapiClient;
		}
	}

	/// <summary>
	/// Check whether the client is currently connected.
	/// </summary>
	/// <returns></returns>
	public static bool IsConnected()
	{
		if (m_WaapiClient == null) return false;
		return WaapiClient.IsConnected();
	}

	private static bool kill;
	private static int loopSleep = 0;
	private static bool projectConnected = false;

	/// <summary>
	/// Main loop for the WAAPI API. Checks if the client is connected and consumes all commands.
	/// </summary>
	private static async void Loop()
	{
		try
		{
			ErrorMessage = "";

			if (await CheckConnection())
			{
				await ConsumeCommandQueue();
			}

			if (!kill)
			{
				if (loopSleep > 0)
				{
					await Task.Delay(loopSleep * 1000);
				}
			}
		}

		//Handle socket issues caused by closing Wwise Authoring.
		catch (System.Net.WebSockets.WebSocketException)
		{
			UnityEngine.Debug.Log("Wwise Unity : WAAPI disconnected because Wwise Authoring was closed");
			Disconnecting?.Invoke(false);
			waapiCommandQueue = new ConcurrentQueue<WaapiCommand>();
			projectConnected = false;
			try
			{
				await m_WaapiClient.Close();
			}
			//Closing the client will throw other exceptions because it tries to send messages to a closed socket.
			catch (System.Net.Sockets.SocketException)
			{
			}
		}
		catch (Wamp.WampNotConnectedException e)
		{
			ErrorMessage = e.Message;
		}
		finally
		{
			UnityEditor.EditorApplication.delayCall += () => Loop();
		}
	}

	/// <summary>
	/// Consumes all WAAPICommands in the queue and then fires QueueConsumed.
	/// </summary>
	/// <returns>Awaitable Task</returns>
	private static async Task ConsumeCommandQueue()
	{
		bool shouldUpdate = false;
		while (waapiCommandQueue.Count > 0)
		{
			if (waapiCommandQueue.TryDequeue(out WaapiCommand cmd))
			{
				try
				{
					await cmd.Execute();
					shouldUpdate = true;
					ErrorMessage = "";
				}
				catch (Wamp.ErrorException e)
				{
					ErrorMessage msg = UnityEngine.JsonUtility.FromJson<ErrorMessage>(e.Json);
					if (msg != null)
					{
						if (msg.message != null)
							ErrorMessage = msg.message;
					}

					switch (e.Uri)
					{
						case ak.wwise.error.unavailable:
						case ak.wwise.error.unexpected_error:
						case ak.wwise.error.wwise_console:
						case ak.wwise.error.locked:
						case ak.wwise.error.file_error:
							waapiCommandQueue.Enqueue(cmd);
							break;
						case ak.wwise.error.invalid_object:
						case ak.wwise.error.invalid_property:
						case ak.wwise.error.invalid_query:
						case ak.wwise.error.invalid_reference:
						case ak.wwise.error.invalid_options:
						case ak.wwise.error.invalid_json:
						case ak.wwise.error.invalid_arguments:
						default:
							UnityEngine.Debug.Log(ErrorMessage);
							break;
					}
					break;
				}
				catch (Wamp.WampNotConnectedException e)
				{
					waapiCommandQueue.Enqueue(cmd);
					throw (e);
				}
			}
		}
		if (shouldUpdate)
		{
			QueueConsumed?.Invoke();
		}
	}

	/// <summary>
	/// Checks the global WAAPI settings and disconnects if WAAPI is disabled or connection settings have changed.
	/// If disconnected, try to connect with current settings.
	/// </summary>
	/// <returns>True if the client is connected</returns>
	private static async Task<bool> CheckConnection()
	{
		if (AkWwiseEditorSettings.Instance.UseWaapi)
		{
			// If WAAPI connection settings have changed, unsubcribe and close the connection.
			if (ConnectionSettingsChanged() && WaapiClient.IsConnected())
			{
				FireDisconnect(false);
				return true;
			}

			if (!WaapiClient.IsConnected())
			{
				try
				{
					await m_WaapiClient.Connect(GetUri());
				}
				catch (System.Exception)
				{
					ConnectionFailed("Connection refused");
				}
			}

			if (WaapiClient.IsConnected())
			{
				var projectOpen = await CheckProjectLoaded();
				if (!projectConnected && projectOpen)
				{
					projectConnected = true;
					loopSleep = 0;
					Connected?.Invoke();
				}
				else if (projectConnected && !projectOpen)
				{
					FireDisconnect(false);
					return true;
				}
			}
		}

		else
		{
			if (WaapiClient.IsConnected() && !isDisconnecting)
			{
				FireDisconnect(false);
				return true;
			}
		}

		return WaapiClient.IsConnected() && projectConnected;
	}

	/// <summary>
	/// Tries to communicate with Wwise and compares the current open project with the project path specified in the Unity Wwise Editor settings.
	/// </summary>
	/// <returns>True if the correct wwise project is open in Wwise.</returns>
	private async static Task<bool> CheckProjectLoaded()
	{
		try
		{
			var result = await GetProjectInfo();
			if (result.Count == 0)
			{
				throw new Wamp.ErrorException("Did not get a response from Wwise project");
			}
			var projectInfo = result[0];
#if UNITY_EDITOR_OSX
			var d1 = AkUtilities.ParseOsxPathFromWinePath(projectInfo.filePath);
#else
			var d1 = projectInfo.filePath;
#endif
			var d2 = AkUtilities.GetFullPath(dataPath, AkWwiseEditorSettings.Instance.WwiseProjectPath);
			d1 = d1.Replace("/", "\\");
			d2 = d2.Replace("/", "\\");
			if (d1 != d2)
			{
				ConnectionFailed($"The wrong project({projectInfo.name}) is open in Wwise");
				return false;
			}
		}

		catch (Wamp.ErrorException e)
		{
			if (e.Json != null)
			{
				ErrorMessage msg = UnityEngine.JsonUtility.FromJson<ErrorMessage>(e.Json);
				if (msg != null)
				{
					if (msg.message != null)
						ErrorMessage = msg.message;
				}
			}
			if (e.Uri == "ak.wwise.locked")
			{
				return true;
			}


			ConnectionFailed($"No project is open in Wwise yet");
			return false;
		}

		return true;
	}

	private static void ConnectionFailed(string message)
	{
		loopSleep = Math.Min(Math.Max(loopSleep * 2, 1), 32);
		ErrorMessage = $"{message} - Retrying in {loopSleep}s";
	}

	/// <summary>
	/// Starts the diconnection process. 
	/// Invokes Disconnecting() so that other classes using WAAPI can clean up and add commands to unsubscribe from topics.
	/// Consumes the last batch of commands in the command queue then closes the client.
	/// </summary>
	private static void FireDisconnect(bool killLoop)
	{
		projectConnected = false;
		isDisconnecting = true;
		Disconnecting?.Invoke(true);
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await CloseClient(killLoop)));
	}

	private static async Task CloseClient(bool killLoop)
	{
		await WaapiClient.Close();
		isDisconnecting = false;
		if (killLoop)
		{
			kill = true;
		}
	}

	/// <summary>
	/// Invoked after the client has disconnected from Wwise authoring.
	/// </summary>
	public static void Disconnected()
	{
		Disconnecting?.Invoke(false);
	}

	private static string GetUri()
	{
		return $"ws://{AkWwiseEditorSettings.Instance.WaapiIP}:{AkWwiseEditorSettings.Instance.WaapiPort}/waapi";
	}

	static string ip;
	static string port;
	static string projectPath;
	private static bool ConnectionSettingsChanged()
	{
		bool changed = false;
		if (ip != AkWwiseEditorSettings.Instance.WaapiIP)
		{
			ip = AkWwiseEditorSettings.Instance.WaapiIP;
			changed = true;
		}

		if (port != AkWwiseEditorSettings.Instance.WaapiPort)
		{
			port = AkWwiseEditorSettings.Instance.WaapiPort;
			changed = true;
		}
		if (projectPath != AkWwiseEditorSettings.Instance.WwiseProjectPath)
		{
			projectPath = AkWwiseEditorSettings.Instance.WwiseProjectPath;
			changed = true;
		}
		return changed;
	}

	/// <summary>
	/// Returns a rich text string representing the current WAAPI connection status.
	/// </summary>
	/// <returns></returns>
	public static string GetStatusString()
	{
		var returnString = "";
		if (!AkWwiseEditorSettings.Instance.UseWaapi)
		{
			returnString += "<color=red> Waapi disabled in project settings </color>";
		}
		else if (WaapiClient.wamp != null)
		{
			var state = WaapiClient.wamp.SocketState();
			switch (state)
			{
				case System.Net.WebSockets.WebSocketState.Open:
					returnString += "<color=green> Connected</color>";
					break;
				case System.Net.WebSockets.WebSocketState.Closed:
					returnString += "<color=red> Disconnected </color>";
					break;
				case System.Net.WebSockets.WebSocketState.Connecting:
					returnString += $"<color=orange> Connecting to { GetUri()}</color>";
					break;
				default:
					returnString += $"<color=orange> Connecting to { GetUri()}</color>";
					break;
			}
		}
		else
		{
			returnString += "<color=red> Disconnected </color>";
		}
		if (ErrorMessage != string.Empty)
			returnString += $" <color=red>{ErrorMessage}</color>";
		return returnString;
	}

	private static async Task<List<WwiseObjectInfo>> GetProjectInfo()
	{
		var args = new WaqlArgs($"from type {WaapiKeywords.PROJECT}");
		var options = new ReturnOptions(new string[] { "filePath" });

		var result = await WaapiClient.Call(ak.wwise.core.@object.get, args, options);
		var ret = UnityEngine.JsonUtility.FromJson<ReturnWwiseObjects>(result).@return;

		return ParseObjectInfo(ret);
	}

	/// <summary>
	/// Use this function to enqueue a command with no expected return object.
	/// </summary>
	/// <param name="uri">The URI of the waapi command</param>
	/// <param name="args">The command-specific arguments</param>
	/// <param name="options">The command-specific options</param>
	public static void QueueCommand(string uri, string args, string options)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await WaapiClient.Call(uri, args, options)));
	}

	/// <summary>
	/// Use this function to enqueue a command with an expected return object of type T.
	/// The command will deserialize the respone as type T and pass it to the callback.
	/// /// </summary>
	/// <typeparam name="T"> Type of the expected return object</typeparam>
	/// <param name="uri">The URI of the waapi command</param>
	/// <param name="args">The command-specific arguments</param>
	/// <param name="options">The command-specific options</param>
	/// <param name="callback">Function accepting an argument of type T</param>
	public static void QueueCommandWithReturnType<T>(string uri, GetResultDelegate<T> callback, string args = null, string options = null)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () =>
			{
				var result = await WaapiClient.Call(uri, args, options);
				callback(UnityEngine.JsonUtility.FromJson<T>(result));
			}));
	}

	/// <summary>
	/// Enqueues a command with a payload that desirializes the list of wwise objects from the response.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="options"></param>
	/// <param name="callback"></param>
	public static void QueueCommandWithReturnWwiseObjects<T>(WaqlArgs args, ReturnOptions options, GetResultListDelegate<T> callback)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () =>
			{
				var result = await WaapiClient.Call(ak.wwise.core.@object.get, args, options);
				var ret = UnityEngine.JsonUtility.FromJson<ReturnWwiseObjects<T>>(result);
				callback.Invoke(ret.@return);
			}));
	}

	/// <summary>
	/// Generic function for fetching a Wwise object with custom return options.
	/// </summary>
	/// <typeparam name="T"> Type of the object to be deserialized from the response.</typeparam>
	/// <param name="guid">GUID of the target object.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="callback">Function accteping a list of T objects.</param>
	public static void GetWwiseObject<T>(System.Guid guid, ReturnOptions options, GetResultListDelegate<T> callback)
	{
		GetWwiseObjects(new List<System.Guid>() { guid }, options, callback);
	}

	/// <summary>
	/// Generic function for fetching a list of Wwise objects with custom return options.
	/// </summary>
	/// <typeparam name="T"> Type of the object to be deserialized from the response.</typeparam>
	/// <param name="guids">GUIDs of the target objects.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="callback">Function accteping a list of T objects.</param>
	public static void GetWwiseObjects<T>(List<System.Guid> guids, ReturnOptions options, GetResultListDelegate<T> callback)
	{
		string guidString = "";
		foreach (var guid in guids)
		{
			guidString += $"{guid:B} ,";
		}

		var args = new WaqlArgs($"from object \"{guidString}\" ");
		QueueCommandWithReturnWwiseObjects(args, options, callback);
	}

	/// <summary>
	/// Enqueues a waapi command to fetch the specified object and all of its ancestors in the heirarchy.
	/// Passes the list of WwiseObjectInfo containing the specified object and ancestors to the callback. 
	/// </summary>
	/// <param name="guid">GUID of the target object.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="callback">Function accepting a list of WwiseObjectInfo.</param>
	public static void GetWwiseObjectAndAncestors<T>(System.Guid guid, ReturnOptions options, GetResultListDelegate<T> callback)
	{
		var args = new WaqlArgs($"from object \"{guid:B}\"  select this, ancestors orderby path");

		QueueCommandWithReturnWwiseObjects(args, options, callback);
	}

	/// <summary>
	/// Enqueues a waapi comand to fetch the specified object and all of its descendants in the heirarchy to a specified depth.
	/// Passes the list of WwiseObjectInfo containing the specified object and descendants to the callback. 
	/// </summary>
	/// <param name="guid">GUID of the target object.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="depth"> Depth of descendants to fetch. If -1, fetches all descendants.</param>
	/// <param name="callback">Function accepting a list of WwiseObjectInfo.</param>
	public static void GetWwiseObjectAndDescendants<T>(System.Guid guid, ReturnOptions options, int depth, GetResultListDelegate<T> callback)
	{
		GetWwiseObjectAndDescendants(guid.ToString("B"), options, depth, callback);
	}

	/// <summary>
	/// Composes a WAQL "from object" request based on the parameters and enqueues a WAAPI command.
	/// Passes the list of WwiseObjectInfo containing the results to the callback
	/// </summary>
	/// <param name="identifier">Can bethe target object GUID or path within the heirarchy.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="depth">Depth of descendants to fetch. If -1, fetches all descendants.</param>
	/// <param name="callback">Function accepting a list of WwiseObjectInfo.</param>
	public static void GetWwiseObjectAndDescendants<T>(string identifier, ReturnOptions options, int depth, GetResultListDelegate<T> callback)
	{
		WaqlArgs args;
		if (depth > 0)
		{
			string selectString = System.String.Join(" ", ArrayList.Repeat(" select this, children", depth).ToArray());
			args = new WaqlArgs($"from object \"{identifier}\" {selectString} orderby path");
		}
		else
		{
			args = new WaqlArgs($"from object \"{identifier}\" select descendants orderby path");
		}

		QueueCommandWithReturnWwiseObjects(args, options, callback);
	}

	/// <summary>
	/// Composes a WAQL "search" request based on the parameters and enqueues a WAAPI command.
	/// Passes the list of WwiseObjectInfo containing the search results to the callback
	/// </summary>
	/// <param name="searchString">Characters to search for.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="objectType">An optional object type used to filter search results.</param>
	/// <param name="callback">Function accepting a list of WwiseObjectInfo.</param>
	public static void Search<T>(string searchString, WwiseObjectType objectType, ReturnOptions options, GetResultListDelegate<T> callback)
	{
		WaqlArgs args;
		if (objectType == WwiseObjectType.None)
		{
			args = new WaqlArgs($"from search \"{searchString}\" orderby path");
		}
		else
		{
			args = new WaqlArgs($"from search \"{searchString}\" where type=\"{WaapiKeywords.WwiseObjectTypeStrings[objectType]}\" orderby path");
		}

		QueueCommandWithReturnWwiseObjects(args, options, callback);
	}

	/// <summary>
	/// Get the children of a given object.
	/// </summary>
	/// <param name="guid">GUID of the target object.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	/// <param name="callback">Function accepting a list of WwiseObjectInfo.</param>
	public static void GetChildren<T>(System.Guid guid, ReturnOptions options, GetResultListDelegate<T> callback)
	{
		if (guid == System.Guid.Empty)
			return;

		var args = new WaqlArgs($"from object \"{guid:B}\" select children orderby path");

		QueueCommandWithReturnWwiseObjects(args, options, callback);
	}

	/// <summary>
	/// Get the WwiseObjectInfo for the project.
	/// </summary>
	/// <param name="callback">Function accepting a list of WwiseObjectInfo. The first element of the list will be the project info.</param>
	/// <param name="options">Specifies which object properties to include in the response</param>
	public static void GetProject<T>(GetResultListDelegate<T> callback, ReturnOptions options)
	{
		var args = new WaqlArgs($"from type {WaapiKeywords.PROJECT}");

		QueueCommandWithReturnWwiseObjects(args, options, callback);
	}

	/// <summary>
	/// Parse the response WwiseObjectInfoJsonObject of a "from object" request and implicit cast the objects to WwiseObjectInfo.
	/// </summary>
	/// <param name="returnObjects"></param>
	/// <returns></returns>
	public static List<WwiseObjectInfo> ParseObjectInfo(List<WwiseObjectInfoJsonObject> returnObjects)
	{
		var returnInfo = new List<WwiseObjectInfo>(returnObjects.Count);
		foreach (var info in returnObjects)
		{
			returnInfo.Add(info);
		}
		return returnInfo;
	}

	/// <summary>
	/// Select the object in Wwise Authoring.
	/// Creates a WaapiCommand object containing a lambda call to SelectObjectInAuthoringAsync and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="guid">GUID of the object to be selected.</param>
	public static void SelectObjectInAuthoring(System.Guid guid)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await SelectObjectInAuthoringAsync(guid)));
	}

	/// <summary>
	/// Creates and sends a WAAPI command to select a Wwise object.
	/// </summary>
	/// <param name="guid">GUID of the object to be selected.</param>
	/// <returns></returns>
	static private async Task SelectObjectInAuthoringAsync(System.Guid guid)
	{
		if (guid == System.Guid.Empty) return;
		var args = new ArgsCommand(WaapiKeywords.FIND_IN_PROJECT_EXPLORER, new string[] { guid.ToString("B") });
		await WaapiClient.Call(ak.wwise.ui.commands.execute, args, null);
	}

	/// <summary>
	/// Open the OS file browser to the folder containing this object's Work Unit.
	/// Creates a WaapiCommand object containing a lambda call to OpenWorkUnitInExplorerAsync and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="guid">GUID of the object to be found.</param>
	public static void OpenWorkUnitInExplorer(System.Guid guid)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await OpenWorkUnitInExplorerAsync(guid)));
	}

	/// <summary>
	/// Open the OS file browser to the folder containing the generated SoundBank.
	/// Creates a WaapiCommand object containing a lambda call to OpenSoundBankInExplorer and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="guid">GUID of the SoundBank to be found.</param>
	public static void OpenSoundBankInExplorer(System.Guid guid)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await OpenSoundBankInExplorerAsync(guid)));
	}

	/// <summary>
	/// Uses a waapi call to get the object's file path, then opens the containing folder in the system's file browser.
	/// </summary>
	/// <param name="guid">GUID of the object to be found.</param>
	/// <returns>Awaitable Task.</returns>
	private static async Task OpenWorkUnitInExplorerAsync(System.Guid guid)
	{
		var args = new WaqlArgs($"from object \"{guid:B}\"");
		var options = new ReturnOptions(new string[] { "filePath" });
		var result = await WaapiClient.Call(ak.wwise.core.@object.get, args, options);
		var ret = UnityEngine.JsonUtility.FromJson<ReturnWwiseObjects>(result);
		var filePath = ret.@return[0].filePath;
		filePath = filePath.Replace("\\", "/");

#if UNITY_EDITOR_OSX
		filePath = AkUtilities.ParseOsxPathFromWinePath(filePath);
#endif
		UnityEditor.EditorUtility.RevealInFinder(filePath);
	}

	/// <summary>
	/// Uses a waapi call to get the SoundBank's generated bank path, then opens the containing folder in the system's file browser.
	/// </summary>
	/// <param name="guid">GUID of the object to be found.</param>
	/// <returns>Awaitable Task.</returns>
	private static async Task OpenSoundBankInExplorerAsync(System.Guid guid)
	{
		var args = new WaqlArgs($"from object \"{guid:B}\"");
		var options = new ReturnOptions(new string[] { "soundbankBnkFilePath" });
		var result = await WaapiClient.Call(ak.wwise.core.@object.get, args, options);
		var ret = UnityEngine.JsonUtility.FromJson<ReturnWwiseObjects>(result);
		var filePath = ret.@return[0].soundbankBnkFilePath;

#if UNITY_EDITOR_OSX
		filePath = AkUtilities.ParseOsxPathFromWinePath(filePath);
#endif
		UnityEditor.EditorUtility.RevealInFinder(filePath);
	}

	/// <summary>
	/// Rename an object in Wwise authoring.
	/// Creates a WaapiCommand object containing a lambda call to RenameAsync and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="guid">GUID of the object to be renamed.</param>
	/// <param name="newName">New name for the wwise object.</param>
	public static void Rename(System.Guid guid, string newName)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await RenameAsync(guid, newName)
		));
	}

	/// <summary>
	/// Sends a WAAPI command to rename a Wwise object.
	/// </summary>
	/// <param name="guid">GUID of the object to be renamed.</param>
	/// <param name="newName">New name for the wwise object.</param>
	/// <returns>Awaitable Task.</returns>
	private static async Task RenameAsync(System.Guid guid, string newName)
	{
		var args = new ArgsRename(guid.ToString("B"), newName);
		await WaapiClient.Call(ak.wwise.core.@object.setName, args, null);
	}


	/// <summary>
	/// Delete an object in wwise authoring. Work Units cannot be deleted in this manner.
	/// Creates a WaapiCommand object containing a lambda call to DeleteAsync and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="guid">GUID of the object to be deleted.</param>
	public static void Delete(System.Guid guid)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await DeleteAsync(guid)
		));
	}

	/// <summary>
	/// Sends three WAAPI commands:
	/// 1. Begin an undo group.
	/// 2. Delete the specified object.
	/// 3. Close the undo group.
	/// </summary>
	/// <param name="guid">GUID of the object to be deleted.</param>
	/// <returns>Awaitable Task.</returns>
	private static async Task DeleteAsync(System.Guid guid)
	{
		await WaapiClient.Call(ak.wwise.core.undo.beginGroup);
		await WaapiClient.Call(ak.wwise.core.@object.delete, new ArgsObject(guid.ToString("b")));
		await WaapiClient.Call(ak.wwise.core.undo.endGroup, new ArgsDisplayName(WaapiKeywords.DELETE_ITEMS));
	}

	/// <summary>
	/// Checks if Wwise object is playable.
	/// </summary>
	/// <param name="type">WwiseObjectType of object to check.</param>
	/// <returns>True if playable</returns>
	public static bool IsPlayable(WwiseObjectType type)
	{
		return (type == WwiseObjectType.Event);
	}

	/// <summary>
	/// Play or pause an object in Wwise authoring.
	/// Creates a WaapiCommand object containing a lambda call to TogglePlayEventAsync and adds it to the waapiCommandQueue.
	///</summary>
	/// <param name="objectType">Used to check whether the object is playable.</param>
	/// <param name="guid">GUID of the object to be played.</param>
	static public void TogglePlayEvent(WwiseObjectType objectType, System.Guid guid)
	{
		if (IsPlayable(objectType))
		{
			waapiCommandQueue.Enqueue(new WaapiCommand(
				async () => await TogglePlayEventAsync(guid)));
		}
	}

	/// <summary>
	/// Play or pause an object in Wwise authoring. Opens a new transport in wwise to play the sound if it does not exist yet.
	/// </summary>
	/// <param name="guid">GUID of the object to be played.</param>
	/// <returns></returns>
	static async private Task TogglePlayEventAsync(System.Guid guid)
	{
		var transportID = await GetTransport(guid);
		var args = new ArgsPlay(WaapiKeywords.PLAYSTOP, transportID);
		var result = await WaapiClient.Call(ak.wwise.core.transport.executeAction, args, null);
	}

	/// <summary>
	/// Find the open transport in ItemTransports or create a new one.
	/// </summary>
	/// <param name="guid">GUID of the object.</param>
	/// <returns></returns>
	static async private Task<int> GetTransport(System.Guid guid)
	{
		TransportInfo transportInfo;
		if (!ItemTransports.TryGetValue(guid, out transportInfo))
		{
			transportInfo = await CreateTransport(guid);
		}
		return transportInfo.TransportID;
	}

	/// <summary>
	/// Send a WAAPI call to create a transport in Wwise.
	/// Subscribe to the ak.wwise.core.transport.stateChanged topic of the new transport.
	/// Add the transport info to ItemTransports.
	/// </summary>
	/// <param name="guid">GUID of the Event</param>
	/// <returns></returns>
	static async private Task<TransportInfo> CreateTransport(System.Guid guid)
	{
		var args = new ArgsObject(guid.ToString("B"));
		var result = await WaapiClient.Call(ak.wwise.core.transport.create, args, null, timeout: 1000);
		int transportID = UnityEngine.JsonUtility.FromJson<ReturnTransport>(result).transport;
		var options = new TransportOptions(transportID);
		uint subscriptionID = await WaapiClient.Subscribe(ak.wwise.core.transport.stateChanged, options, HandleTransportStateChanged);

		var transport = new TransportInfo(transportID, subscriptionID);
		ItemTransports.Add(guid, transport);
		return transport;
	}

	/// <summary>
	/// Handle the messages published by a transport when its state is changed.
	/// If stopped, enqueue a command with DestroyTransport as its payload.
	/// </summary>
	/// <param name="message"></param>
	static private void HandleTransportStateChanged(string message)
	{
		TransportState transport = UnityEngine.JsonUtility.FromJson<TransportState>(message);
		System.Guid itemID = new System.Guid(transport.@object);
		int transportID = transport.transport;

		if (transport.state == WaapiKeywords.STOPPED)
		{
			waapiCommandQueue.Enqueue(new WaapiCommand(
				async () => await DestroyTransport(itemID)));
		}

		else if (transport.state == WaapiKeywords.PLAYING && !ItemTransports.ContainsKey(itemID))
		{
			ItemTransports.Add(itemID, new TransportInfo(transportID, 0));
		}
	}

	/// <summary>
	/// Send a WAAPI command to stop the specific transport.
	/// </summary>
	/// <param name="in_transportID">ID of the transport.</param>
	/// <returns></returns>
	static private async Task StopTransport(int in_transportID)
	{
		var args = new ArgsPlay(WaapiKeywords.STOP, in_transportID);
		var result = await WaapiClient.Call(ak.wwise.core.transport.executeAction, args, null);
	}

	/// <summary>
	/// Unsubscribe from the transport topic and send a WAAPI command to destroy the transport in Wwise.
	/// </summary>
	/// <param name="in_itemID"> GUID of the Event.</param>
	/// <returns></returns>
	static async Task<string> DestroyTransport(System.Guid in_itemID)
	{
		if (!ItemTransports.ContainsKey(in_itemID))
			return null;

		if (ItemTransports[in_itemID].SubscriptionID != 0)
			await WaapiClient.Unsubscribe(ItemTransports[in_itemID].SubscriptionID);

		var args = new ArgsTransport(ItemTransports[in_itemID].TransportID);
		var result = await WaapiClient.Call(ak.wwise.core.transport.destroy, args, null);
		ItemTransports.Remove(in_itemID);
		return result;
	}

	/// <summary>
	/// Stops all playing transports.
	/// Creates a WaapiCommand object containing a lambda call to StopAllTransportsAsync and adds it to the waapiCommandQueue.
	/// </summary>
	static public void StopAllTransports()
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
			async () => await StopAllTransportsAsync()));
	}

	/// <summary>
	/// Stops all playing transports.
	/// </summary>
	/// <returns>Awaitable task.</returns>
	static private async Task StopAllTransportsAsync()
	{
		foreach (var item in ItemTransports)
		{
			await StopTransport(item.Value.TransportID);
		}
	}

	/// <summary>
	/// Subscribe to WAAPI topic. Refer to WAAPI reference documentation for a list of topics and their options.
	/// Creates a WaapiCommand object containing a lambda call to SubscribeAsync and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="topic">The topic URI to subscribe to.</param>
	/// <param name="subscriptionCallback">Delegate function to call when the topic is published.</param>
	/// <param name="handshakeCallback">Action to be executed once the subscription has been made. 
	/// This should store the subscription ID so that the subscription can be cleaned up when it is no longer needed.</param>
	public static void Subscribe(string topic, Wamp.PublishHandler subscriptionCallback, System.Action<SubscriptionInfo> handshakeCallback)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
		   async () => handshakeCallback(await SubscribeAsync(new SubscriptionInfo(topic, subscriptionCallback)))));
	}

	/// <summary>
	/// Subscribe to WAAPI topic. Refer to WAAPI reference documentation for a list of topics and their options.
	/// Creates and sends a WAAPI command to subscribe to the topic.
	/// </summary>
	/// <param name="subscription">SubscriptionInfo object containing the topic URI and the message handling callback.</param>
	/// <returns>Updated SubscriptionInfo object containing the subscription ID (uint). </returns>
	private static async Task<SubscriptionInfo> SubscribeAsync(SubscriptionInfo subscription)
	{
		var options = new ReturnOptions(new string[] { "id", "parent", "name", "type", "childrenCount", "path", "workunitType" });
		uint id = await WaapiClient.Subscribe(subscription.Uri, options, subscription.Callback);
		subscription.SubscriptionId = id;
		return subscription;
	}


	/// <summary>
	/// Unsubscribe from an existing subscription.
	/// Creates a WaapiCommand object containing a lambda call to UnsubscribeAsync and adds it to the waapiCommandQueue.
	/// </summary>
	/// <param name="id"> The subscription ID received from the initial subscription.</param>
	public static void Unsubscribe(uint id)
	{
		waapiCommandQueue.Enqueue(new WaapiCommand(
		 async () => await UnsubscribeAsync(id)));
	}

	/// <summary>
	/// Unsubscribe from a subscription.
	/// </summary>
	/// <param name="id"> The subscription ID received from the initial subscription.</param>
	/// <returns>Awaitable Task.</returns>
	private static async Task UnsubscribeAsync(uint id)
	{
		await WaapiClient.Unsubscribe(id);
	}

	/// <summary>
	/// Deserializes the objects published by the ak.wwise.ui.selectionChanged topic.
	/// </summary>
	/// <param name="json">Json string containing the message.</param>
	/// <returns>List of WwiseObjectInfo objects. </returns>
	public static List<WwiseObjectInfo> ParseSelectedObjects(string json)
	{
		var info = UnityEngine.JsonUtility.FromJson<SelectedWwiseObjects>(json);
		var ret = new List<WwiseObjectInfo>();
		foreach (var child in info.objects)
		{
			ret.Add(child);
		}
		return ret;
	}

	/// <summary>
	/// Deserializes the object published by the ak.wwise.core.object.nameChanged topic.
	/// </summary>
	/// <param name="json">Json string containing the message.</param>
	/// <returns>WwiseRenameInfo containing the object information and the new name.</returns>
	public static WwiseRenameInfo ParseRenameObject(string json)
	{
		var info = UnityEngine.JsonUtility.FromJson<WwiseRenameInfo>(json);
		info.ParseInfo();
		return info;
	}

	/// <summary>
	/// Deserializes the object published by the ak.wwise.core.object.childAdded or ak.wwise.core.object.childRemoved topic.
	/// </summary>
	/// <param name="json">Json string containing the message.</param>
	/// <returns> WwiseChildModifiedInfo containing parent and child object information.</returns>
	public static WwiseChildModifiedInfo ParseChildAddedOrRemoved(string json)
	{
		var info = UnityEngine.JsonUtility.FromJson<WwiseChildModifiedInfo>(json);
		info.ParseInfo();
		return info;
	}
}
#endif