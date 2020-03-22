using System;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


public class SerializerTestWindow : EditorWindow
{
    // Singleton to avoid multiple instances of window. 
    private static SerializerTestWindow _instance;
    
    private static readonly RethinkDB R = RethinkDB.R;
    
    private Connection _connection;
    
    private Object _object;

    private void OnEnable()
    {
        _connection = R.Connection().Hostname("asgard.gamecult.games").Port(RethinkDBConstants.DefaultPort).Timeout(60).Connect();
    }

    public void OnGUI()
    {
        var obj = new ShieldData() {ID = Guid.NewGuid(), Name = "Foo"} as DatabaseEntry;
        // This button serializes the texture reference and print the JSON representation
        
        if (GUILayout.Button("Print MsgPack JSON"))
            Debug.Log(MessagePackSerializer.SerializeToJson(obj));
        
        if (GUILayout.Button("Print Newtonsoft JSON"))
            Debug.Log(JsonConvert.SerializeObject(obj));

        if (GUILayout.Button("Create Database"))
            R.DbCreate("Aetheria").Run(_connection);

        if (GUILayout.Button("Create Table"))
            R.Db("Aetheria").TableCreate("Items").Run(_connection);

        if (GUILayout.Button("Send to RethinkDB"))
            R.Db("Aetheria").Table("Items").Insert(obj).Run(_connection);
    }
    
    #region EditorWindow Boilerplate

    public SerializerTestWindow()
    {
        _instance = this;
    }

    static SerializerTestWindow ShowWindow()
    {
        if (_instance == null)
        {
            SerializerTestWindow window = EditorWindow.GetWindow<SerializerTestWindow>();
            window.titleContent = new GUIContent("Serializer Test");
            _instance = window;
            window.Show();
        }
        else
            _instance.Focus();

        return _instance;
    }
    
    [MenuItem("Window/Serializer Test Tool")]
    static void Init()
    {
        ShowWindow();
    }
    #endregion
}


