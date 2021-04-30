/******************************************************************************

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

*******************************************************************************/

using System.Linq;

/// <summary>
/// WAMP protocol implementation using only strings and regular expressions. This implements only a subset of the WAMP feature set and is only compatible with Wwise.
/// </summary>
public class Wamp
{
	/// <summary>Exception thrown during WAMP operations when the timeout is reached.</summary> 
	public class TimeoutException : System.Exception
	{
		public TimeoutException(string message)
			: base(message)
		{
		}
	}

	public class WampNotConnectedException : System.Exception
	{
		public WampNotConnectedException(string message)
		: base(message)
		{
		}
	}

	/// <summary>Exception thrown during WAMP operations.</summary> 
	public class ErrorException : System.Exception
	{
		internal string Json { get; set; }
		internal Messages MessageId { get; set; }
		internal int RequestId { get; set; }
		internal string Uri { get; set; }

		public ErrorException(string message)
			: base(message)
		{
		}

		public static ErrorException FromResponse(string response)
		{
			// [ERROR, CALL, CALL.Request|id, Details|dict, Error|uri, Arguments|list, ArgumentsKw|dict]
			string pattern = @"^\[\s*8,\s*(\d+)\s*,\s*(\d+)\s*,\s*\{\s*\}\s*,\s*""([^,\s]+)""\s*,\[\s*\]\s*,\s*(\{)";
			var match = System.Text.RegularExpressions.Regex.Match(response, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
			if (match.Groups.Count != 5)
				throw new ErrorException("Invalid ERROR message.");

			Messages messageId = (Messages)int.Parse(match.Groups[1].Value);
			int requestId = int.Parse(match.Groups[2].Value);
			string uri = match.Groups[3].Value;
			string json = response.Substring(match.Groups[4].Index, response.Length - match.Groups[4].Index - 1);

			return new ErrorException($"Error {uri} in {messageId.ToString()} operation.")
			{
				Json = json,
				MessageId = messageId,
				RequestId = requestId,
				Uri = uri
			};
		}

	}

	/// <summary>Messages ids defined by the WAMP protocol</summary> 
	internal enum Messages : int
	{
		HELLO = 1,
		WELCOME = 2,
		GOODBYE = 6,
		ERROR = 8,
		SUBSCRIBE = 32,
		SUBSCRIBED = 33,
		UNSUBSCRIBE = 34,
		UNSUBSCRIBED = 35,
		EVENT = 36,
		CALL = 48,
		RESULT = 50
	}

	/// <summary>Encapsulate a response from the server.</summary> 
	private class Response
	{
		public Messages MessageId { get; set; }
		public int RequestId { get; set; }
		public int ContextSpecificResultId { get; set; }
		public uint SubscriptionId { get; set; }
		public string Json { get; set; }
	}

	/// <summary>Publish events are delegates registered with Subscribe.</summary> 
	public delegate void PublishHandler(string json);
	public delegate void DisconnectedHandler();

	public event DisconnectedHandler Disconnected;

	private System.Net.WebSockets.ClientWebSocket ws;
	private int sessionId = 0;
	private int currentRequestId = 0;
	private System.Threading.CancellationTokenSource stopServerTokenSource = new System.Threading.CancellationTokenSource();
	private System.Threading.Tasks.TaskCompletionSource<Response> taskCompletion = new System.Threading.Tasks.TaskCompletionSource<Response>();
	private System.Collections.Concurrent.ConcurrentDictionary<uint, PublishHandler> subscriptions = new System.Collections.Concurrent.ConcurrentDictionary<uint, PublishHandler>();

	private async System.Threading.Tasks.Task Send(string msg, int timeout)
	{
		try
		{
			using (var cts = new System.Threading.CancellationTokenSource(timeout))
			{
				var segment = new System.ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(msg));
				await ws.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);
			}
		}
		catch (System.Threading.Tasks.TaskCanceledException)
		{
			throw new TimeoutException("Timeout when sending message.");
		}
	}

	/// <summary>Parse a WAMP message from the server.</summary> 
	private Response Parse(string msg)
	{
		const string msgTypePattern = @"^\[\s*(\d+)";
		var match = System.Text.RegularExpressions.Regex.Match(msg, msgTypePattern, System.Text.RegularExpressions.RegexOptions.Singleline);

		if (match.Groups.Count != 2)
			throw new ErrorException("Error while parsing response from server.");

		Messages messageId = (Messages)int.Parse(match.Groups[1].Value);
		switch (messageId)
		{
			case Messages.WELCOME:
				return ParseWelcome(msg);
			case Messages.GOODBYE:
				return ParseGoodbye(msg);
			case Messages.SUBSCRIBED:
				return ParseSubscribed(msg);
			case Messages.UNSUBSCRIBED:
				return ParseUnsubscribed(msg);
			case Messages.EVENT:
				return ParseEvent(msg);
			case Messages.RESULT:
				return ParseResult(msg);
			case Messages.ERROR:
				throw ErrorException.FromResponse(msg);
			default:
				throw new ErrorException("Unexpected result from server.");
		}
	}

	private static Response ParseResult(string msg)
	{
		// [RESULT, CALL.Request|id, Details|dict, YIELD.Arguments|list, YIELD.ArgumentsKw | dict]
		const string pattern = @"^\[\s*50,\s*(\d+)\s*,\s*\{\s*\}\s*,\s*\[\s*\]\s*,\s*(\{)";
		var match = System.Text.RegularExpressions.Regex.Match(msg, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
		if (!match.Success || match.Groups.Count != 3)
			throw new ErrorException("Invalid RESULT message.");

		return new Response()
		{
			MessageId = Messages.RESULT,
			RequestId = (int.Parse(match.Groups[1].Value)),
			Json = msg.Substring(match.Groups[2].Index, msg.Length - match.Groups[2].Index - 1)
		};
	}

	private static Response ParseSubscribed(string msg)
	{
		// [SUBSCRIBED, SUBSCRIBE.Request|id, Subscription|id]
		const string pattern = @"^\[\s*33,\s*(\d+)\s*,\s*(\d+)\s*]$";
		var match = System.Text.RegularExpressions.Regex.Match(msg, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
		if (!match.Success || match.Groups.Count != 3)
			throw new ErrorException("Invalid SUBSCRIBED message.");

		return new Response()
		{
			MessageId = Messages.SUBSCRIBED,
			RequestId = (int.Parse(match.Groups[1].Value)),
			//ContextSpecificResultId = (int.Parse(match.Groups[2].Value))
			SubscriptionId = (uint.Parse(match.Groups[2].Value))
		};
	}

	private static Response ParseUnsubscribed(string msg)
	{
		// [UNSUBSCRIBED, UNSUBSCRIBE.Request|id]
		const string pattern = @"^\[\s*35,\s*(\d+)\s*]$";
		var match = System.Text.RegularExpressions.Regex.Match(msg, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
		if (!match.Success || match.Groups.Count != 2)
			throw new ErrorException("Invalid UNSUBSCRIBED message.");

		return new Response()
		{
			MessageId = Messages.UNSUBSCRIBED,
			RequestId = (int.Parse(match.Groups[1].Value))
		};
	}

	private static Response ParseGoodbye(string msg)
	{
		// [GOODBYE, Details|dict, Reason|uri]
		const string pattern = @"^\[\s*6";
		var match = System.Text.RegularExpressions.Regex.Match(msg, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
		if (!match.Success || match.Groups.Count != 1)
			throw new ErrorException("Invalid GOODBYE message.");

		return new Response()
		{
			MessageId = Messages.GOODBYE
		};
	}

	private static Response ParseWelcome(string msg)
	{
		// [WELCOME, Session|id, Details|dict]
		const string pattern = @"^\[\s*2,\s*(\d+)";
		var match = System.Text.RegularExpressions.Regex.Match(msg, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
		if (!match.Success || match.Groups.Count != 2)
			throw new ErrorException("Invalid WELCOME message.");

		return new Response()
		{
			MessageId = Messages.WELCOME,
			RequestId = 0,
			ContextSpecificResultId = (int.Parse(match.Groups[1].Value))
		};
	}

	private static Response ParseEvent(string msg)
	{
		// [EVENT, SUBSCRIBED.Subscription|id, PUBLISHED.Publication|id, Details|dict, PUBLISH.Arguments|list, PUBLISH.ArgumentKw|dict]
		const string pattern = @"^\[\s*36,\s*(\d+)\s*,\s*(\d+)\s*,\s*\{\s*\}\s*,\s*\[\s*\]\s*,\s*(\{)";
		var match = System.Text.RegularExpressions.Regex.Match(msg, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
		if (match.Groups.Count != 4)
			throw new ErrorException("Invalid EVENT message.");

		return new Response()
		{
			MessageId = Messages.EVENT,
			RequestId = (int.Parse(match.Groups[2].Value)),
			ContextSpecificResultId = (int.Parse(match.Groups[1].Value)),
			Json = msg.Substring(match.Groups[3].Index, msg.Length - match.Groups[3].Index - 1)
		};
	}


	private async System.Threading.Tasks.Task<Response> ReceiveMessage()
	{
		// Receive one web socket message
		System.Collections.Generic.List<System.Collections.Generic.IEnumerable<byte>> segments = new System.Collections.Generic.List<System.Collections.Generic.IEnumerable<byte>>();

		try
		{
			while (true)
			{
				byte[] buffer = new byte[4096];
				var segment = new System.ArraySegment<byte>(buffer, 0, buffer.Length);
				System.Net.WebSockets.WebSocketReceiveResult rcvResult = await ws.ReceiveAsync(segment, stopServerTokenSource.Token);

				// Accumulate the byte arrays in a list, we will join them later
				segments.Add(segment.Skip(segment.Offset).Take(rcvResult.Count));

				if (rcvResult.EndOfMessage)
					break;
			}
		}
		catch (System.Net.WebSockets.WebSocketException e)
		{
			throw e.InnerException;
		}
		catch (System.Exception)
		{
			throw new ErrorException("Error receiving response from server.");
		}

		try
		{
			byte[] bytes = segments.SelectMany(t => t).ToArray<byte>();
			string msg = System.Text.Encoding.UTF8.GetString(bytes);
			return Parse(msg);
		}
		catch (ErrorException e)
		{
			// Dispatch already built error
			throw e;
		}
		catch (System.Exception)
		{
			throw new ErrorException("Error while parsing response from server.");
		}
	}

	/// <summary>
	/// Wait for the next response
	/// </summary>
	/// <returns>The response from the server.</returns>
	private async System.Threading.Tasks.Task<Response> Receive(int timeout)
	{
		System.Threading.Tasks.Task task = await System.Threading.Tasks.Task.WhenAny(
			taskCompletion.Task,
			System.Threading.Tasks.Task.Delay(timeout));

		if (task != taskCompletion.Task)
		{
			taskCompletion = new System.Threading.Tasks.TaskCompletionSource<Response>();

			// Timeout reached
			throw new TimeoutException("Timeout when receiving message.");
		}

		if (task.Exception != null)
		{
			taskCompletion = new System.Threading.Tasks.TaskCompletionSource<Response>();

			if (task.Exception.InnerException.InnerException != null)
				throw task.Exception.InnerException.InnerException;
			throw task.Exception;
		}

		var result = taskCompletion.Task.Result;

		// Since we can't re-use the task completion, create a new one for the next message
		taskCompletion = new System.Threading.Tasks.TaskCompletionSource<Response>();

		return result;
	}

	/// <summary>
	/// Wait for the next response and do some validation on the response
	/// </summary>
	/// <param name="message">What message to expect</param>
	/// <param name="requestId">What request id to expect</param>
	/// <param name="timeout">The maximum timeout in milliseconds for the function to execute. Will raise exception when timeout is reached.</param>
	/// <returns></returns>
	private async System.Threading.Tasks.Task<Response> ReceiveExpect(Messages message, int requestId, int timeout)
	{
		// Should receive the expected message or ERROR
		Response response = await Receive(timeout);

		if (response.MessageId != message)
			throw new ErrorException($"{message.ToString()}: invalid response. Did not receive expected answer.");

		if (response.RequestId != requestId)
			throw new ErrorException($"{message.ToString()}: invalid request id for result.");

		return response;
	}

	/// <summary>
	/// Connect to the specified host, handshake and prepare the listening task.
	/// </summary>
	/// <param name="host">The URI of the host, usually something like ws://host:port</param>
	/// <param name="timeout">The maximum timeout in milliseconds for the function to execute. Will raise exception when timeout is reached.</param>
	/// <returns></returns>
	internal async System.Threading.Tasks.Task Connect(string host, int timeout)
	{
		try
		{
			System.Uri uri = new System.Uri(host);
			using (var cts = new System.Threading.CancellationTokenSource())
			{
				// Connect
				if (ws == null)
					ws = new System.Net.WebSockets.ClientWebSocket();
				await ws.ConnectAsync(uri, cts.Token);
			}
			{
				// [HELLO, Realm|uri, Details|dict]
				await Send($"[{(int)Messages.HELLO},\"realm1\"]", timeout);
			}

			StartListen();

			{
				// Should receive the WELCOME
				Response response = await ReceiveExpect(Messages.WELCOME, 0, timeout);

				sessionId = response.ContextSpecificResultId;
			}
		}
		catch (System.Net.WebSockets.WebSocketException e)
		{
			ws.Dispose();
			ws = new System.Net.WebSockets.ClientWebSocket();
			throw new ErrorException(e.ToString());
		}
		catch (System.Exception e)
		{
			throw new ErrorException(e.ToString());
		}
	}

	/// <summary>
	/// Tell the connection state of the WebSocket client.
	/// </summary>
	/// <returns>Return true if the connection is open and ready.</returns>
	internal bool IsConnected()
	{
		if (ws == null)
			return false;

		return ws.State == System.Net.WebSockets.WebSocketState.Open;
	}

	internal System.Net.WebSockets.WebSocketState SocketState()
	{
		if (ws == null) return System.Net.WebSockets.WebSocketState.None;
		return ws.State;
	}

	/// <summary>Close the connection.</summary>
	/// <param name="timeout">The maximum timeout in milliseconds for the function to execute. Will raise exception when timeout is reached.</param>
	internal async System.Threading.Tasks.Task Close(int timeout)
	{
		// [GOODBYE, Details|dict, Reason|uri]
		try
		{
			await Send($"[{(int)Messages.GOODBYE},{{}},\"bye_from_csharp_client\"]", timeout);
			Response response = await ReceiveExpect(Messages.GOODBYE, 0, timeout);

			stopServerTokenSource.Cancel();

			using (var cts = new System.Threading.CancellationTokenSource(timeout))
			{
				await ws.CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "wamp_close", cts.Token);
			}
		}
		catch (System.Net.WebSockets.WebSocketException)
		{
			ws.Dispose();
			stopServerTokenSource.Cancel();
			return;
		}
	}

	private void ProcessEvent(Response message)
	{
		int subscriptionId = message.ContextSpecificResultId;

		PublishHandler publishEvent = null;
		if (!subscriptions.TryGetValue((uint)subscriptionId, out publishEvent))
			throw new ErrorException("UNSUBSCRIBE: unknown subscription id.");

		publishEvent(message.Json);
	}

	private void StartListen()
	{
		// Start the receive task, that will remain running for the whole connection
		System.Threading.CancellationToken ct = stopServerTokenSource.Token;
		var task = System.Threading.Tasks.Task.Factory.StartNew(() =>
		{
			ct.ThrowIfCancellationRequested();
			while (true)
			{
				try
				{
					System.Threading.Tasks.Task<Response> receiveTask = ReceiveMessage();
					receiveTask.Wait();

					if (receiveTask.Result.MessageId == Messages.EVENT)
						ProcessEvent(receiveTask.Result);
					else if (taskCompletion != null)
						taskCompletion.SetResult(receiveTask.Result);
					else
						throw new ErrorException("Received WAMP message that we did not expect.");

					if (ct.IsCancellationRequested)
					{
						break;
					}
				}
				catch (System.Exception e)
				{
					if (e.InnerException.GetType() == typeof(System.Net.WebSockets.WebSocketException))
					{
						var exception = e.InnerException as System.Net.WebSockets.WebSocketException;
						if (exception.WebSocketErrorCode == System.Net.WebSockets.WebSocketError.ConnectionClosedPrematurely)
						{
							if (taskCompletion != null)
								taskCompletion.SetException(e);

							OnDisconnect();

							return;
						}
					}

					if (ws.State != System.Net.WebSockets.WebSocketState.Open)
					{
						OnDisconnect();
						return;
					}


					// Signal the exception to the other thread and continue to listen
					if (taskCompletion != null)
						taskCompletion.SetException(e);
				}
			}
		}, stopServerTokenSource.Token);
	}

	private void OnDisconnect()
	{
		if (Disconnected != null)
		{
			Disconnected();
		}
	}

	/// <summary>
	/// Invoke an RPC function from the uri
	/// </summary>
	/// <param name="uri">URI of the function</param>
	/// <param name="args">Arguments</param>
	/// <param name="options">Options</param>
	/// <param name="timeout">The maximum timeout in milliseconds for the function to execute. Will raise exception when Waapi.TimeoutException is reached.</param>
	/// <returns></returns>
	internal async System.Threading.Tasks.Task<string> Call(string uri, string args, string options, int timeout)
	{
		int requestId = ++currentRequestId;

		// [CALL, Request|id, Options|dict, Procedure|uri, Arguments|list, ArgumentsKw|dict]
		await Send($"[{(int)Messages.CALL},{requestId},{options},\"{uri}\",[],{args}]", timeout);

		// Should receive the RESULT or ERROR
		Response response = await ReceiveExpect(Messages.RESULT, requestId, timeout);
		return response.Json;
	}

	/// <summary>
	/// Subscribe to a WAMP topic.
	/// </summary>
	/// <param name="topic">The topic to which subscribe.</param>
	/// <param name="options">The options the subscription.</param>
	/// <param name="publishEvent">The delegate function to call when the topic is published.</param>
	/// <param name="timeout">The maximum timeout in milliseconds for the function to execute. Will raise Waapi.TimeoutException when timeout is reached.</param>
	/// <returns>Subscription id, that you can use to unsubscribe.</returns>
	internal async System.Threading.Tasks.Task<uint> Subscribe(string topic, string options, PublishHandler publishEvent, int timeout)
	{
		int requestId = ++currentRequestId;

		// [SUBSCRIBE, Request|id, Options|dict, Topic|uri]
		await Send($"[{(int)Messages.SUBSCRIBE},{requestId},{options},\"{topic}\"]", timeout);

		// Should receive the SUBSCRIBED or ERROR
		Response response = await ReceiveExpect(Messages.SUBSCRIBED, requestId, timeout);

		subscriptions.TryAdd(response.SubscriptionId, publishEvent);
		return response.SubscriptionId;
	}

	/// <summary>Unsubscribe from a subscription.</summary>
	/// <param name="subscriptionId">The subscription id received from the initial subscription.</param>
	/// <param name="timeout">The maximum timeout in milliseconds for the function to execute. Will raise Waapi.TimeoutException when timeout is reached.</param>
	internal async System.Threading.Tasks.Task Unsubscribe(uint subscriptionId, int timeout)
	{
		int requestId = ++currentRequestId;

		// [UNSUBSCRIBE, Request|id, SUBSCRIBED.Subscription|id]
		await Send($"[{(int)Messages.UNSUBSCRIBE},{requestId},{subscriptionId}]", timeout);

		Response response = await ReceiveExpect(Messages.UNSUBSCRIBED, requestId, timeout);
		PublishHandler oldEvent;
		subscriptions.TryRemove(subscriptionId, out oldEvent);
	}
}