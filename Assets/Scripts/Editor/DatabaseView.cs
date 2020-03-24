using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using UniRx;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;
using Object = UnityEngine.Object;

public class DatabaseListView : EditorWindow
{
    // Singleton to avoid multiple instances of window. 
    private static DatabaseListView _instance;
    public Guid SelectedItem;
    public GUIStyle SelectedStyle;

    public static RethinkDB R = RethinkDB.R;
    private Connection _connection;
    
    private DatabaseCache _databaseCache;
    private bool[] _gearFoldouts;
    private bool _itemsFoldout;
    private bool _compoundFoldout;
    private bool _simpleFoldout;
    private bool _commoditiesFoldout;
    //private bool _consumablesFoldout;
    private Type[] _itemTypes;
    private bool _listItemStyle;
    private GUIStyle _listStyleOdd;
    private GUIStyle _listStyleEven;
    private static DatabaseInspector _inspector;
    private Type[] _entryTypes;
    private bool[] _entryFoldouts;
    private string _connectionString;
    private const float LineHeight = 20;

    // Set instance on reloading the window, else it gets lost after script reload (due to PlayMode changes, ...).
    public DatabaseListView()
    {
        _instance = this;
    }

    public static DatabaseListView ShowWindow()
    {
        if (_instance == null)
        {
            // "Get existing open window or if none, make a new one:" says documentation.
            // But if called after script reloads a second instance will be opened! => Custom singleton required.
            DatabaseListView window = EditorWindow.GetWindow<DatabaseListView>();
            window.titleContent = new GUIContent("DB List");
            _instance = window;
            _instance.InitStyles();
            window.Show();
        }
        else
            _instance.Focus();

        return _instance;
    }

    public GUIStyle ListItemStyle =>
        (_listItemStyle = !_listItemStyle) ? _listStyleEven : _listStyleOdd;

    [MenuItem("Window/Aetheria Database Tools")]
    static void Init()
    {
        ShowWindow();
    }

    void OnEnable()
    {
        // Add Unity.Mathematics serialization support to RethinkDB Driver
        //Converter.Serializer.Converters.Add(new MathJsonConverter());
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
        
        // Create database cache
        _databaseCache = new DatabaseCache();
        _databaseCache.OnDataUpdateLocal += _ => Repaint();
        _databaseCache.OnDataUpdateRemote += _ => EditorDispatcher.Dispatch(Repaint);

        DatabaseInspector.DatabaseCache = _databaseCache;
        _inspector = DatabaseInspector.ShowWindow();
        
        _itemTypes = typeof(ItemData).GetAllChildClasses()
            .Where(t => t.GetCustomAttribute<InspectableAttribute>() != null).ToArray();
        _entryTypes = typeof(DatabaseEntry).GetAllChildClasses()
            .Where(t => t.GetCustomAttribute<InspectableAttribute>() != null && !typeof(ItemData).IsAssignableFrom(t)).ToArray();
        
        _gearFoldouts = new bool[_itemTypes.Length];
        _entryFoldouts = new bool[_entryTypes.Length];

        if (EditorPrefs.HasKey("RethinkDB.URL"))
            _connectionString = EditorPrefs.GetString("RethinkDB.URL");
        else
            _connectionString = "Enter DB URL";
        
        InitStyles();
    }

    private void InitStyles()
    {
        var isDark = EditorGUIUtility.isProSkin;
        _listStyleOdd = new GUIStyle
        {
            normal = {background = isDark ? XKCDColors.DarkGrey.ToTexture() : XKCDColors.LightGrey.ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
        _listStyleEven = new GUIStyle
        {
            normal = {background = isDark ? Color.Lerp(XKCDColors.DarkGrey,Color.black,.5f).ToTexture() : Color.Lerp(XKCDColors.LightGrey,Color.white,.5f).ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
        SelectedStyle = new GUIStyle
        {
            normal = {background = isDark ? XKCDColors.DarkGreyBlue.ToTexture() : XKCDColors.Greyblue.ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
    }

    public void OnGUI()
    {
        Event currentEvent = Event.current;
        EventType currentEventType = currentEvent.type;

        if (currentEventType == EventType.DragUpdated)
            // Indicate that we don't accept drags ourselves
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

        using (var h = new EditorGUILayout.HorizontalScope())
        {
            _connectionString = EditorGUILayout.TextField(_connectionString);
            if (GUILayout.Button("Connect"))
            {
                EditorPrefs.SetString("RethinkDB.URL", _connectionString);
                _connection = R.Connection().Hostname(_connectionString).Port(RethinkDBConstants.DefaultPort).Timeout(60).Connect();
                Debug.Log("Connected to RethinkDB");
        
                // When entries are changed locally, push the changes to RethinkDB
                _databaseCache.OnDataUpdateLocal += async entry =>
                {
                    var result = await R.Db("Aetheria").Table("Items").Update(entry).RunAsync(_connection);
                    Debug.Log($"Uploaded entry to RethinkDB: {entry.ID} result: {result}");
                };
                
                _databaseCache.OnDataInsertLocal += async entry =>
                {
                    var result = await R.Db("Aetheria").Table("Items").Insert(entry).RunAsync(_connection);
                    Debug.Log($"Inserted entry to RethinkDB: {entry.ID} result: {result}");
                };

                _databaseCache.OnDataDeleteLocal += async entry =>
                {
                    var result = await R.Db("Aetheria").Table("Items").Get(entry.ID).Delete().RunAsync(_connection);
                    Debug.Log($"Deleted entry from RethinkDB: {entry.ID} result: {result}");
                };
        
                // Get all item data from RethinkDB
                Task.Run(async () =>
                {
                    var result = await R.Db("Aetheria").Table("Items").RunCursorAsync<DatabaseEntry>(_connection);
                    while (await result.MoveNextAsync())
                    {
                        var entry = result.Current;
                        Debug.Log($"Received entry from RethinkDB: {entry.ID}");
                        _databaseCache.Add(entry, true);
                    }
                }).WrapErrors();
        
                // Subscribe to changes from RethinkDB
                Task.Run(async () =>
                {
                    var result = await R.Db("Aetheria").Table("Items").Changes()
                        .RunChangesAsync<DatabaseEntry>(_connection);
                    while (await result.MoveNextAsync())
                    {
                        var change = result.Current;
                        if(change.OldValue == null)
                            Debug.Log($"Received change from RethinkDB (Entry Created): {change.NewValue.ID}");
                        else if(change.NewValue == null)
                            Debug.Log($"Received change from RethinkDB (Entry Deleted): {change.OldValue.ID}");
                        else
                            Debug.Log($"Received change from RethinkDB: {change.NewValue.ID}");
                        _databaseCache.Add(change.NewValue, true);
                    }
                }).WrapErrors();
            }
        }
        GUILayout.Space(5);

        Action<DatabaseEntry> select = item =>
        {
            SelectedItem = item.ID;
            Repaint();
            _inspector.entry = _databaseCache.Get(SelectedItem);
            _inspector.Repaint();
        };

        using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
        {
            _itemsFoldout = EditorGUILayout.Foldout(_itemsFoldout, "Items", true);
        }
        if (_itemsFoldout)
        {
            var items = _databaseCache.AllEntries.Where(item => item is ItemData).Cast<ItemData>().ToArray();
            for (var i = 0; i < _itemTypes.Length; i++)
            {
                using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
                {
                    GUILayout.Space(10);
                    _gearFoldouts[i] = EditorGUILayout.Foldout(_gearFoldouts[i],
                        ((NameAttribute) _itemTypes[i].GetCustomAttribute(typeof(NameAttribute)))?.Name ?? _itemTypes[i].Name,
                        true);
                }
                if (_gearFoldouts[i])
                {
                    foreach (var item in items.Where(e=>e.GetType()==_itemTypes[i]))
                    {
                        var style = ListItemStyle;
                        var selected = SelectedItem == item.ID;
                        using (var h = new EditorGUILayout.HorizontalScope(selected?SelectedStyle:style))
                        {
                            if (h.rect.Contains(currentEvent.mousePosition))
                            {
                                if (currentEventType == EventType.MouseDrag)
                                {
                                    DragAndDrop.PrepareStartDrag();
                                    DragAndDrop.SetGenericData("Item", item.ID);
                                    DragAndDrop.StartDrag("Database Item");
                                }
                                else if (currentEventType == EventType.MouseUp)
                                    select(item);
                            }

                            GUILayout.Space(20);
                            GUILayout.Label(item.Name, selected ? EditorStyles.whiteLabel : GUI.skin.label);
                        }
                    }
                    
                    using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
                    {
                        if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                            CreateItem(_itemTypes[i]);
                        GUILayout.Space(20);
                        GUILayout.Label("New " + _itemTypes[i].Name);
                        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, EditorGUIUtility.isProSkin?Color.white:Color.black, 0, 0);
                    }
                }
            }
        }
        
        var entries = _databaseCache.AllEntries.Where(item => !(item is ItemData)).ToArray();
        for (var i = 0; i < _entryTypes.Length; i++)
        {
            using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
            {
                _entryFoldouts[i] = EditorGUILayout.Foldout(_entryFoldouts[i],
                    ((NameAttribute) _entryTypes[i].GetCustomAttribute(typeof(NameAttribute)))?.Name ?? _entryTypes[i].Name,
                    true);
            }

            if (_entryFoldouts[i])
            {
                int index = 0;
                foreach (var entry in entries.Where(e=>e.GetType()==_entryTypes[i]))
                {
                    index++;
                    var style = ListItemStyle;
                    var selected = SelectedItem == entry.ID;
                    using (var h = new EditorGUILayout.HorizontalScope(selected?SelectedStyle:style))
                    {
                        if (h.rect.Contains(currentEvent.mousePosition))
                        {
                            if (currentEventType == EventType.MouseDrag)
                            {
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.SetGenericData("Item", entry.ID);
                                DragAndDrop.StartDrag("Database Item");
                            }
                            else if (currentEventType == EventType.MouseUp)
                                select(entry);
                        }
                        GUILayout.Space(10);
                        GUILayout.Label((entry as INamedEntry)?.EntryName ?? index.ToString(), selected ? EditorStyles.whiteLabel : GUI.skin.label);
                    }
                }
                    
                using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
                {
                    if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                        CreateItem(_entryTypes[i]);
                    GUILayout.Space(10);
                    GUILayout.Label("New " + _entryTypes[i].Name);
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, EditorGUIUtility.isProSkin?Color.white:Color.black, 0, 0);
                }
            }
        }
        
//        EditorGUILayout.BeginHorizontal(ListItemStyle);
//        _commoditiesFoldout = EditorGUILayout.Foldout(_commoditiesFoldout, "Commodities", true);
//        EditorGUILayout.EndHorizontal();
//        if (_commoditiesFoldout)
//        {
//            using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
//            {
//                GUILayout.Space(10);
//                _simpleFoldout = EditorGUILayout.Foldout(_simpleFoldout, "Simple", true);
//            }
//            if (_simpleFoldout)
//            {
//                foreach (var item in Database.Items.Values.Where(e=>e.GetType()==typeof(ItemData)).Cast<ItemData>())
//                {
//                    var style = ListItemStyle;
//                    var selected = SelectedItem == item.ID;
//                    using (var h = new EditorGUILayout.HorizontalScope(selected?_selectedStyle:style))
//                    {
//                        if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
//                            select(item);
//                        GUILayout.Space(20);
//                        GUILayout.Label(item.Name, selected ? EditorStyles.whiteLabel : GUI.skin.label);
//                    }
//                }
//                
//                using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
//                {
//                    if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
//                        CreateItem(typeof(ItemData));
//                    GUILayout.Space(20);
//                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
//                    GUI.DrawTexture(rect, GlobalData.Instance.plus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
//                    GUILayout.Label("New Simple Commodity");
//                }
//            }
//
//            using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
//            {
//                GUILayout.Space(10);
//                _compoundFoldout = EditorGUILayout.Foldout(_compoundFoldout, "Compound", true);
//            }
//            if (_compoundFoldout)
//            {
//                foreach (var item in Database.Items.Values.Where(e=>e.GetType()==typeof(CraftedItem)).Cast<CraftedItem>())
//                {
//                    var style = ListItemStyle;
//                    var selected = SelectedItem == item.GetId();
//                    using (var h = new EditorGUILayout.HorizontalScope(selected?_selectedStyle:style))
//                    {
//                        if (GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
//                            select(item);
//                        GUILayout.Space(20);
//                        GUILayout.Label(item.Item.Name, selected ? EditorStyles.whiteLabel : GUI.skin.label);
//                    }
//                }
//
//                using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
//                {
//                    if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
//                        CreateItem(typeof(CraftedItem));
//                    GUILayout.Space(20);
//                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
//                    GUI.DrawTexture(rect, GlobalData.Instance.plus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
//                    GUILayout.Label("New Compound Commodity");
//                }
//            }
//        }
//        EditorGUILayout.BeginHorizontal(ListItemStyle);
//        _consumablesFoldout = EditorGUILayout.Foldout(_consumablesFoldout, "Consumables", true);
//        EditorGUILayout.EndHorizontal();
//        if (_consumablesFoldout)
//        {
//            
//        }
        
        // HACK: This ensures none of the controls in this window can be focused (usually from clicking foldouts)
        //GUI.SetNextControlName("");
        //GUI.FocusControl("");
    }

    private void CreateItem(Type type)
    {
        var newEntry = (DatabaseEntry) Activator.CreateInstance(type);
        SelectedItem = newEntry.ID;
        if(newEntry is INamedEntry entry)
            entry.EntryName = $"New {type.Name}";
        _databaseCache.Add(newEntry);
    }
}
