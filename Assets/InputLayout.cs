

using MessagePack;
using Newtonsoft.Json;

public class InputLayout
{
    
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class InputLayoutKey
{
    [Key(0), JsonProperty("mainLabel")] public string MainLabel;
    [Key(1), JsonProperty("altLabel")] public string AltLabel;
    [Key(2), JsonProperty("path")] public string InputSystemPath;
    [Key(3), JsonProperty("width")] public float Width;
    [Key(4), JsonProperty("height")] public int Height;
}