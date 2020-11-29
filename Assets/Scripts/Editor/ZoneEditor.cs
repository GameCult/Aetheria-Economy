using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneEditor : EditorWindow
{
    // Singleton to avoid multiple instances of window.
    private static ZoneEditor _instance;
    public static ZoneEditor Instance => _instance ?? GetWindow<ZoneEditor>();
    [MenuItem("Window/Aetheria/Zone Editor")]
    static void Init() => Instance.Show();

    private SectorRenderer _sectorRenderer;
    private DirectoryInfo _filePath;
    private GameContext _context;
    private DatabaseCache _cache;
    private Zone _zone;

    private void OnEnable()
    {
        _instance = this;
        _filePath = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
        
        // Create Game Context
        _cache = new DatabaseCache();
        _cache.Load(_filePath.FullName);
        _context = new GameContext(_cache, Debug.Log);
        
        // Find Sector Renderer and apply context
        _sectorRenderer = FindObjectOfType<SectorRenderer>();
        _sectorRenderer.Context = _context;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Load Zone"))
        {
            GenericMenu menu = new GenericMenu();
            foreach(var file in _filePath.EnumerateFiles("*.zone"))
                menu.AddItem(new GUIContent(file.Name), false, () => LoadZone(file));
            menu.ShowAsContext();
        }
        
        if (GUILayout.Button("Clear Zone"))
            _sectorRenderer.ClearZone();
        
        if (GUILayout.Button("Save Zone"))
            SaveZone();
        
        if (GUILayout.Button("Update Orbits"))
        {
            _zone.Update(.01f);
            //_sectorRenderer.LateUpdate();
        }
    }
    
    // Does the rendering of the map editor in the scene view.
    private void OnSceneGUI(SceneView sceneView) { }

    void OnFocus()
    {
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI; // Just in case
        SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }

    private void LoadZone(FileInfo zoneFile)
    {
        var zonePack = MessagePackSerializer.Deserialize<ZonePack>(File.ReadAllBytes(zoneFile.FullName));
        _zone = new Zone(_context, zonePack);
        _sectorRenderer.LoadZone(_zone);
    }

    public void SaveZone()
    {
        var dir = Path.Combine(_filePath.FullName, $"{_zone.Data.Name}.zone");
        File.WriteAllBytes(dir,MessagePackSerializer.Serialize(_zone.Pack()));
    }
}
