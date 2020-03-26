using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using LiteNetLib;
using MessagePack;
using UniRx;
using UnityEngine;

public static class CultClient {
    public static Action<string> Logger = s => 
        MainThreadDispatcher.Post(_=>Debug.Log(s),null);

    public static event Action<string> OnError;
    public static int Ping;

    private static NetManager _client;
    private static NetPeer _peer;
    private static Dictionary<Type, NotAnActionCollection> _messageCallbacks = new Dictionary<Type, NotAnActionCollection>();
    private static Guid _token;
    private static MD5 _hasher = MD5.Create();

    public static bool Verified => _token != Guid.Empty;
    public static bool Connected => _client != null;

    public static void Send<T>(T m) where T : Message
    {
        if (Connected)
        {
            if (Verified || m is LoginMessage || m is RegisterMessage || m is VerifyMessage)
            {
                Logger($"Sending message {MessagePackSerializer.SerializeToJson(m as Message)}");
                _peer.Send(m as Message);
            }
            else Logger("Cannot send message, client is not verified!");
        }
        else Logger("Cannot send message, client is not connected!");
    }

    public static void ClearMessageListeners()
    {
        _messageCallbacks.Clear();
    }

    public static void AddMessageListener<T>(Action<T> callback) where T : Message
    {
        if (!_messageCallbacks.ContainsKey(typeof(T)))
            _messageCallbacks[typeof(T)] = new ActionCollection<T>();
        ((ActionCollection<T>)_messageCallbacks[typeof(T)]).Add(callback);
    }

    public static void Login(string auth, string password)
    {
        if (8 < password.Length && password.Length < 32)
            Send(new LoginMessage
            {
                Auth = auth,
                Password = _hasher.ComputeHash(Encoding.UTF8.GetBytes(password))
            });
        else Logger("Invalid Password");
    }

    public static void Register(string email, string username, string password)
    {
        if (8 < password.Length && password.Length < 32)
            Send(new RegisterMessage
            {
                Email = email,
                Name = username,
                Password = _hasher.ComputeHash(Encoding.UTF8.GetBytes(password))
            });
        else Logger("Invalid Password");
    }

    public static void Connect(string host = "localhost", int port = 3075)
    {
        EventBasedNetListener listener = new EventBasedNetListener();
        _client = new NetManager(listener)
        {
//            UnsyncedEvents = true,
//            MergeEnabled = true,
//            NatPunchEnabled = true
        };
        _client.Start(3074);
        _peer = _client.Connect(host, port, "aetheria-cc65a44d");
        Observable.EveryUpdate().Subscribe(_ => _client.PollEvents());
        listener.NetworkErrorEvent += (point, code) => Logger($"{point.Address}: Error {code}");
        listener.NetworkReceiveEvent += (peer, reader, method) =>
        {
            Logger($"Received message: {MessagePackSerializer.ConvertToJson(new ReadOnlyMemory<byte>(reader.RawData))}");
            var message = MessagePackSerializer.Deserialize<Message>(reader.RawData);
            var type = message.GetType();
            
            if (!Verified)
            {
                switch (message)
                {
                    case LoginSuccessMessage loginSuccess:
                        _token = loginSuccess.Session;
                        return;
                    case ErrorMessage error:
                        OnError?.Invoke(error.Error);
                        return;
                }
            }
            
            if (_messageCallbacks.ContainsKey(type))
                typeof(ActionCollection<>).MakeGenericType(new[] {type}).GetMethod("Invoke")
                    .Invoke(_messageCallbacks[type], new object[] {message});
            else
                Logger(
                    $"Received {type.Name} message but no one is listening for it so I'll just leave it here " +
                    $"¯\\_(ツ)_/¯\n{MessagePackSerializer.ConvertToJson(new ReadOnlyMemory<byte>(reader.RawData))}");

        };
        
        listener.PeerConnectedEvent += peer =>
        {
            Logger($"Peer {peer.EndPoint.Address}:{peer.EndPoint.Port} connected.");
            _peer = peer;
        };
        
        listener.PeerDisconnectedEvent += (peer, info) =>
        {
            Logger($"Peer {peer.EndPoint.Address}:{peer.EndPoint.Port} disconnected: {info.Reason}.");
            _peer = null;
        };
        
        listener.NetworkLatencyUpdateEvent +=
            (peer, latency) => Ping = latency; //Logger($"Ping received: {latency} ms");
    }
}
