using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cinemachine;
using MessagePack;
using UniRx;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class ActionGameManager : MonoBehaviour
{
    public SectorRenderer SectorRenderer;
    public CinemachineVirtualCamera TopDownCamera;
    public CinemachineVirtualCamera FollowCamera;
    
    private DirectoryInfo _filePath;
    private bool _editMode;
    private float _time;
    
    public DatabaseCache Cache { get; private set; }
    public GameContext Context { get; private set; }
    public Zone Zone { get; private set; }

    void Start()
    {
        ConsoleController.MessageReceiver = this;
        _filePath = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
        Cache = new DatabaseCache();
        Cache.Load(_filePath.FullName);
        Context = new GameContext(Cache, Debug.Log);

        var zoneFile = Path.Combine(_filePath.FullName, "Home.zone");
        
        // If the game has already been run, there will be a Home file containing a ZonePack; if not, generate one
        var zonePack = File.Exists(zoneFile) ? 
            MessagePackSerializer.Deserialize<ZonePack>(File.ReadAllBytes(zoneFile)) : 
            ZoneGenerator.GenerateZone(
                context: Context,
                name: Guid.NewGuid().ToString().Substring(0,8),
                position: float2.zero,
                mapLayers: Context.MapLayers.Values,
                resources: Context.Resources,
                mass: 100000,
                radius: 1500
            );
        
        Zone = Context.GetZone(zonePack);
        SectorRenderer.Context = Context;
        SectorRenderer.LoadZone(Zone);

        ConsoleController.AddCommand("editmode", _ => ToggleEditMode());
        // ConsoleController.AddCommand("savezone", args =>
        // {
        //     if (args.Length > 0)
        //         Zone.Value.Data.Name = args[0];
        //     SaveZone();
        // });
    }

    public void ToggleEditMode()
    {
        _editMode = !_editMode;
        FollowCamera.gameObject.SetActive(!_editMode);
        TopDownCamera.gameObject.SetActive(_editMode);
    }

    void Update()
    {
        if(!_editMode)
        {
            _time += Time.deltaTime;
            Context.Time = Time.time;
        }
        Context.Update();
    }
}
