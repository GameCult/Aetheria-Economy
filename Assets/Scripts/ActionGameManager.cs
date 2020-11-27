using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class ActionGameManager : MonoBehaviour
{
    public DatabaseCache Cache { get; private set; }
    public GameContext Context { get; private set; }
    public Zone CurrentZone { get; private set; }

    public event EventHandler<ZoneChangeEventArgs> ZoneChanged; 

    void Start()
    {
        var filePath = new DirectoryInfo(Application.dataPath).Parent;
        Cache = new DatabaseCache();
        Cache.Load(filePath.FullName);
        Context = new GameContext(Cache, Debug.Log);

        var zoneFile = Path.Combine(filePath.FullName, "Home.msgpack");
        
        // If the game has already been run, there will be a Home file containing a ZonePack; if not, generate one
        var zonePack = File.Exists(zoneFile) ? 
            MessagePackSerializer.Deserialize<ZonePack>(File.ReadAllBytes(zoneFile)) : 
            ZoneGenerator.GenerateZone(
                context: Context,
                name: "Home",
                position: float2.zero,
                mapLayers: Context.MapLayers.Values,
                resources: Context.Resources,
                mass: 100000,
                radius: 1500
            );
        
        //var oldZone = CurrentZone;
        CurrentZone = Context.GetZone(zonePack);
        //ZoneChanged?.Invoke(this, new ZoneChangeEventArgs(oldZone, CurrentZone));
    }

    void Update()
    {
        Context.Time = Time.time;
        Context.Update();
    }
}

public class ZoneChangeEventArgs : EventArgs
{
    public Zone OldZone { get; set; }
    public Zone NewZone { get; set; }

    public ZoneChangeEventArgs(Zone oldZone, Zone newZone)
    {
        OldZone = oldZone;
        NewZone = newZone;
    }
}
