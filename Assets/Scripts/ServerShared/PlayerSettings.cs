using System;
using MessagePack;

[MessagePackObject, Serializable]
public class PlayerSettings
{
    [Key(0)]
    public string Name;

    [Key(1)]
    public float ShutdownPerformance = .25f;
}