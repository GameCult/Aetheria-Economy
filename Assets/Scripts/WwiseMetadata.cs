using Newtonsoft.Json;

[JsonObject]
public class WwiseMetadataFile
{
    public WwiseMetaSoundBanksInfo SoundBanksInfo;
}

[JsonObject]
public class WwiseMetaSoundBanksInfo
{
    public WwiseMetaSoundBank[] SoundBanks;
}

[JsonObject]
public class WwiseMetaSoundBank
{
    public int Id;
    public string ShortName;
    public WwiseMetaObject[] IncludedEvents;
    public WwiseMetaObject[] GameParameters;
}

[JsonObject]
public class WwiseMetaObject
{
    public int Id;
    public string Name;
}