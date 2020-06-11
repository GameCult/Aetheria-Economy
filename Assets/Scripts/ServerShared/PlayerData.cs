using System;
using MessagePack;
using Newtonsoft.Json;

[RethinkTable("Users"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class PlayerData : DatabaseEntry, INamedEntry
{
    [JsonProperty("email"), Key(1)]
    public string Email;

    [JsonProperty("password"), Key(2)]
    public string Password;

    [JsonProperty("username"), Key(3)]
    public string Username;

    [JsonProperty("corporation"), Key(4)]
    public Guid Corporation;
    
    [IgnoreMember] public string EntryName
    {
        get => Username;
        set => Username = value;
    }
}