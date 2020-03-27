using System;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Unity.Mathematics.math;

public class SerializerTestWindow : EditorWindow
{
    // Singleton to avoid multiple instances of window. 
    private static SerializerTestWindow _instance;
    
    private static readonly RethinkDB R = RethinkDB.R;
    
    private Connection _connection;
    
    private Object _object;

    private void OnEnable()
    {
        _connection = R.Connection().Hostname(EditorPrefs.GetString("RethinkDB.URL")).Port(RethinkDBConstants.DefaultPort).Timeout(60).Connect();
    }

    public void OnGUI()
    {
        var obj = new ShieldData() {ID = Guid.NewGuid(), Name = "Bar" , HeatPerformanceCurve = new []{float4(0,0,0,0), float4(1,1,1,1)}} as DatabaseEntry;
        
        // JsonSerializer serializer = new JsonSerializer();
        // serializer.Converters.Add(new MathJsonConverter());
        // serializer.Converters.Add(Converter.DateTimeConverter);
        // serializer.Converters.Add(Converter.BinaryConverter);
        // serializer.Converters.Add(Converter.GroupingConverter);
        // serializer.Converters.Add(Converter.PocoExprConverter);
        
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new MathJsonConverter(),
                Converter.DateTimeConverter,
                Converter.BinaryConverter,
                Converter.GroupingConverter,
                Converter.PocoExprConverter
            }
        };
        
        Converter.Serializer.Converters.Add(new MathJsonConverter());
        
        // Set extensions to default resolver.
        var resolver = CompositeResolver.Create(
            MathResolver.Instance,
            NativeGuidResolver.Instance,
            StandardResolver.Instance
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        MessagePackSerializer.DefaultOptions = options;
        
        if (GUILayout.Button("Print MsgPack JSON"))
            Debug.Log(MessagePackSerializer.SerializeToJson(obj, options));

        //var writer = new StringWriter();
        if (GUILayout.Button("Print Newtonsoft JSON"))
        {
            //serializer.Serialize(writer, obj);
            Debug.Log(JsonConvert.SerializeObject(obj));
        }

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


