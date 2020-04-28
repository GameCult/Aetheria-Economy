// TODO: USE THIS EVERYWHERE
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

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
    private Vector2 _view;

    public Color LabelColor => EditorGUIUtility.isProSkin ? Color.white : Color.black;

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
            DatabaseListView window = GetWindow<DatabaseListView>();
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
        
        _view = BeginScrollView(
            _view, 
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUI.skin.scrollView,
            GUILayout.Width(EditorGUIUtility.currentViewWidth),
            GUILayout.ExpandHeight(true));

        if (currentEventType == EventType.DragUpdated)
            // Indicate that we don't accept drags ourselves
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

        using (var h = new HorizontalScope())
        {
            _connectionString = TextField(_connectionString);
            if (GUILayout.Button("Connect"))
            {
                EditorPrefs.SetString("RethinkDB.URL", _connectionString);
                _connection = RethinkConnection.RethinkConnect(_databaseCache, _connectionString);
            }
        }
        using (var h = new HorizontalScope())
        {
            if (GUILayout.Button("Connect All"))
            {
                EditorPrefs.SetString("RethinkDB.URL", _connectionString);
                _connection = RethinkConnection.RethinkConnect(_databaseCache, _connectionString, true, false);
            }

            if (GUILayout.Button("Save"))
            {
                _databaseCache.Save(new DirectoryInfo(Application.dataPath).Parent.FullName);
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

        using (var h = new HorizontalScope(ListItemStyle))
        {
            _itemsFoldout = Foldout(_itemsFoldout, "Items", true);
        }
        if (_itemsFoldout)
        {
            var items = _databaseCache.AllEntries.Where(item => item is ItemData).Cast<ItemData>().ToArray();
            for (var i = 0; i < _itemTypes.Length; i++)
            {
                using (var h = new HorizontalScope(ListItemStyle))
                {
                    GUILayout.Space(10);
                    _gearFoldouts[i] = Foldout(_gearFoldouts[i],
                        ((NameAttribute) _itemTypes[i].GetCustomAttribute(typeof(NameAttribute)))?.Name ?? FormatTypeName(_itemTypes[i].Name),
                        true);
                }
                if (_gearFoldouts[i])
                {
                    foreach (var item in items.Where(e=>e.GetType()==_itemTypes[i]).OrderBy(item=>item.Name))
                    {
                        var style = ListItemStyle;
                        var selected = SelectedItem == item.ID;
                        using (var h = new HorizontalScope(selected?SelectedStyle:style))
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
                    
                    using (var h = new HorizontalScope(ListItemStyle))
                    {
                        if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                            CreateItem(_itemTypes[i]);
                        GUILayout.Space(20);
                        GUILayout.Label("New " + _itemTypes[i].Name);
                        var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                    }
                }
            }
        }
        
        var entries = _databaseCache.AllEntries.Where(item => !(item is ItemData)).ToArray();
        for (var i = 0; i < _entryTypes.Length; i++)
        {
            using (var h = new HorizontalScope(ListItemStyle))
            {
                _entryFoldouts[i] = Foldout(_entryFoldouts[i],
                    ((NameAttribute) _entryTypes[i].GetCustomAttribute(typeof(NameAttribute)))?.Name ?? FormatTypeName(_entryTypes[i].Name),
                    true);
            }

            if (_entryFoldouts[i])
            {
                int index = 0;
                foreach (var entry in entries.Where(e=>e.GetType()==_entryTypes[i]).OrderBy(entry=>entry is INamedEntry namedEntry ? namedEntry.EntryName : entry.ID.ToString()))
                {
                    index++;
                    var style = ListItemStyle;
                    var selected = SelectedItem == entry.ID;
                    using (var h = new HorizontalScope(selected?SelectedStyle:style))
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
                    
                using (var h = new HorizontalScope(ListItemStyle))
                {
                    if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                        CreateItem(_entryTypes[i]);
                    GUILayout.Space(10);
                    GUILayout.Label("New " + _entryTypes[i].Name);
                    var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                }
            }
        }
            
        EndScrollView();
    }

    private string FormatTypeName(string typeName)
    {
        return (typeName.EndsWith("Data")
            ? typeName.Substring(0, typeName.Length - 4)
            : typeName).SplitCamelCase();
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
