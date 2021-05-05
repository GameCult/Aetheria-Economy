using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using MessagePack.Formatters;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : Editor
{
    private bool _showIcons;
    private bool _showProperties;
    private bool _showDatabase;

//    private ScriptableItem _prefab;
    
    public override void OnInspectorGUI()
    {
        var data = (GameSettings) target;
        
        serializedObject.Update();

        DrawDefaultInspector();

        var hardpointNames = Enum.GetNames(typeof(HardpointType));
        if(data.ItemIcons.Length!=hardpointNames.Length)
            Array.Resize(ref data.ItemIcons, hardpointNames.Length);

        if (GUILayout.Button("Hardpoint Icons", EditorStyles.toolbarButton))
            _showIcons = !_showIcons;
        
        if(_showIcons)
            for (var i = 0; i < data.ItemIcons.Length; i++)
                data.ItemIcons[i] = (Sprite) EditorGUILayout.ObjectField(hardpointNames[i], data.ItemIcons[i], typeof(Sprite), false);

        serializedObject.ApplyModifiedProperties();
    }
}