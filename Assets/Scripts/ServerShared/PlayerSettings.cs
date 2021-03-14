using System;
using MessagePack;

[MessagePackObject, Serializable]
public class PlayerSettings
{
    [Key(0)] public string Name = "Anonymous";
    [Key(1)] public SavedGame CurrentRun;
}