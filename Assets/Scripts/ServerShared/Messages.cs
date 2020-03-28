using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using MessagePack;
using Unity.Mathematics;

[Union(0,typeof(PingMessage))]
[Union(1,typeof(LoginMessage))]
[Union(2,typeof(RegisterMessage))]
[Union(3,typeof(VerifyMessage))]
[Union(4,typeof(ErrorMessage))]
[Union(5,typeof(LoginSuccessMessage))]
[Union(6,typeof(ChatMessage))]
[Union(7,typeof(ChatBroadcastMessage))]
[Union(8,typeof(ChangeNameMessage))]
[Union(9,typeof(GalaxyRequestMessage))]
[Union(10,typeof(GalaxyResponseMessage))]
[Union(11,typeof(ZoneRequestMessage))]
[Union(12,typeof(ZoneResponseMessage))]
[MessagePackObject]
public abstract class Message
{
    [IgnoreMember] public NetPeer Peer;
}

[MessagePackObject]
public class PingMessage : Message
{
    [Key(0)] public float SendTime;
}


[MessagePackObject]
public class LoginMessage : Message
{
    [Key(0)] public string Auth;
    [Key(1)] public byte[] Password;
}

[MessagePackObject]
public class RegisterMessage : Message
{
    [Key(0)] public string Name;
    [Key(1)] public string Email;
    [Key(2)] public byte[] Password;
}

[MessagePackObject]
public class VerifyMessage : Message
{
    [Key(0)] public Guid Session;
}

[MessagePackObject]
public class ErrorMessage : Message
{
    [Key(0)] public string Error;
}

[MessagePackObject]
public class LoginSuccessMessage : Message
{
    [Key(0)] public Guid Session;
}

[MessagePackObject]
public class ChatMessage : Message
{
    [Key(0)] public string Text;
}

[MessagePackObject]
public class ChatBroadcastMessage : Message
{
    [Key(0)] public string User;
    [Key(1)] public string Text;
}

[MessagePackObject]
public class ChangeNameMessage : Message
{
    [Key(0)] public string Name;
}

[MessagePackObject]
public class GalaxyRequestMessage : Message
{
    //[Key(0)] public int RequiredByMessagePack;
}

[MessagePackObject]
public class GalaxyResponseMessage : Message
{
    [Key(0)] public GalaxyResponseZone[] Zones;
    [Key(1)] public GalaxyMapLayerData StarDensity;
    [Key(2)] public GlobalData GlobalData;
}

[MessagePackObject]
public class GalaxyResponseZone
{
    [Key(0)] public Guid ZoneID;
    [Key(1)] public Guid[] Links;
    [Key(2)] public string Name;
    [Key(3)] public float2 Position;
}

[MessagePackObject]
public class ZoneRequestMessage : Message
{
    [Key(0)] public Guid ZoneID;
}

[MessagePackObject]
public class ZoneResponseMessage : Message
{
    [Key(0)] public ZoneData Zone;
    [Key(1)] public DatabaseEntry[] Contents;
}