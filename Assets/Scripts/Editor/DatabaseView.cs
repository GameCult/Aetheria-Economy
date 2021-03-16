﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
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
    public static DatabaseListView Instance => _instance ? _instance : GetWindow<DatabaseListView>();
    [MenuItem("Window/Aetheria Database Tools")]
    static void Init() => Instance.Show();
    private void Awake()
    {
        _instance = this;
    }
    
    public Guid SelectedItem;
    public GUIStyle SelectedStyle;

    public static RethinkDB R = RethinkDB.R;
    
    private DatabaseCache _databaseCache;
    private bool[] _itemTypeFoldouts;
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
    private RethinkQueryStatus _queryStatus;
    private Dictionary<Guid, HashSet<Guid>> _itemBlueprints = new Dictionary<Guid, HashSet<Guid>>();
    private HashSet<Guid> _itemBlueprintFoldouts = new HashSet<Guid>();
    private HashSet<string> _itemGroupFoldouts = new HashSet<string>();
    private string _filePath => Path.Combine(ActionGameManager.GameDataDirectory.FullName, _fileName);
    private string _fileName = "AetherDB.msgpack";

    public Color LabelColor => EditorGUIUtility.isProSkin ? Color.white : Color.black;

    public GUIStyle ListItemStyle =>
        (_listItemStyle = !_listItemStyle) ? _listStyleEven : _listStyleOdd;
    
    void OnEnable()
    {
        //_filePath = ;
        // Create database cache
        _databaseCache = new DatabaseCache(_filePath);
        
        var onInsert = new Action<DatabaseEntry>(entry =>
        {
            if (entry is ItemData itemData)
            {
                if(!_itemBlueprints.ContainsKey(itemData.ID))
                    _itemBlueprints[itemData.ID] = new HashSet<Guid>();
            }
            if (entry is BlueprintData blueprintData)
            {
                if(!_itemBlueprints.ContainsKey(blueprintData.Item))
                    _itemBlueprints[blueprintData.Item] = new HashSet<Guid>();
                _itemBlueprints[blueprintData.Item].Add(blueprintData.ID);
            }
        });
        _databaseCache.OnDataInsertLocal += onInsert;
        _databaseCache.OnDataInsertRemote += onInsert;

        var onDelete = new Action<DatabaseEntry>(entry =>
        {
            if (entry is BlueprintData blueprintData)
                _itemBlueprints[blueprintData.Item].Remove(blueprintData.ID);
        });
        _databaseCache.OnDataDeleteLocal += onDelete;
        _databaseCache.OnDataDeleteRemote += onDelete;
        
        _databaseCache.OnDataUpdateLocal += _ => Repaint();
        _databaseCache.OnDataInsertLocal += _ => Repaint();
        _databaseCache.OnDataDeleteLocal += _ => Repaint();
        _databaseCache.OnDataUpdateRemote += _ => EditorDispatcher.Dispatch(Repaint);
        _databaseCache.OnDataInsertRemote += _ => EditorDispatcher.Dispatch(Repaint);
        _databaseCache.OnDataDeleteRemote += _ => EditorDispatcher.Dispatch(Repaint);

        DatabaseInspector.DatabaseCache = _databaseCache;
        _inspector = DatabaseInspector.Instance;
        _inspector.Show();
        
        _itemTypes = typeof(ItemData).GetAllChildClasses()
            .Where(t => t.GetCustomAttribute<InspectableAttribute>() != null).ToArray();
        _entryTypes = typeof(DatabaseEntry).GetAllChildClasses()
            .Where(t => t.GetCustomAttribute<InspectableAttribute>() != null && !typeof(ItemData).IsAssignableFrom(t)).ToArray();
        
        _itemTypeFoldouts = new bool[_itemTypes.Length];
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

        using (var h = new HorizontalScope())
        {
            _connectionString = TextField(_connectionString);
            if (GUILayout.Button("Connect"))
            {
                EditorPrefs.SetString("RethinkDB.URL", _connectionString);
                _queryStatus = RethinkConnection.RethinkConnect(_databaseCache, _connectionString);
            }
            // if (GUILayout.Button("Connect All"))
            // {
            //     EditorPrefs.SetString("RethinkDB.URL", _connectionString);
            //     _queryStatus = RethinkConnection.RethinkConnect(_databaseCache, _connectionString, true, false);
            // }
        }
        using (var h = new HorizontalScope())
        {
            //_fileName = TextField(_fileName);
            if (GUILayout.Button("Save"))
            {
                _databaseCache.Save();
                Debug.Log("Local DB Cache Saved!");
            }

            if (GUILayout.Button("Load"))
            {
                _databaseCache.Load();
                Debug.Log("Loaded DB From Local Cache!");
            }
        }

        using (new HorizontalScope())
        {
            if (GUILayout.Button("Delete Orphaned Blueprints"))
            {
                int count = 0;
                foreach (var blueprintData in _databaseCache.GetAll<BlueprintData>().ToArray())
                {
                    if (_databaseCache.Get(blueprintData.Item) == null)
                    {
                        _databaseCache.Delete(blueprintData);
                        count++;
                    }
                }
                Debug.Log($"Deleted {count} orphaned blueprints!");
            }
        }

        if (_queryStatus != null && _queryStatus.RetrievedItems < _queryStatus.GalaxyEntries + _queryStatus.ItemsEntries)
        {
            var progressRect = GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, (float)_queryStatus.RetrievedItems/(_queryStatus.GalaxyEntries + _queryStatus.ItemsEntries), "Sync Progress");
        }
        GUILayout.Space(5);
        
        _view = BeginScrollView(
            _view, 
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUI.skin.scrollView,
            GUILayout.Width(EditorGUIUtility.currentViewWidth),
            GUILayout.ExpandHeight(true));
        
        // if(GUILayout.Button("Log Cache Count"))
        //     Debug.Log($"Cache contains {_databaseCache.AllEntries.Count()} elements");

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
                    _itemTypeFoldouts[i] = Foldout(_itemTypeFoldouts[i],
                        ((NameAttribute) _itemTypes[i].GetCustomAttribute(typeof(NameAttribute)))?.Name ?? FormatTypeName(_itemTypes[i].Name),
                        true);
                }
                if (_itemTypeFoldouts[i])
                {
                    var typedItems = items.Where(e => e.GetType() == _itemTypes[i]).OrderBy(item => item.Name);
                    IEnumerable<IGrouping<string, ItemData>> itemGroups = new List<IGrouping<string, ItemData>>();
                    if(_itemTypes[i] == typeof(SimpleCommodityData))
                        itemGroups = typedItems.GroupBy(item => Enum.GetName(typeof(SimpleCommodityCategory), (item as SimpleCommodityData).Category));
                    else if(_itemTypes[i] == typeof(CompoundCommodityData))
                        itemGroups = typedItems.GroupBy(item => Enum.GetName(typeof(CompoundCommodityCategory), (item as CompoundCommodityData).Category));
                    else if(_itemTypes[i] == typeof(GearData))
                        itemGroups = typedItems.GroupBy(item =>
                        {
                            var hp = Enum.GetName(typeof(HardpointType), (item as GearData).Hardpoint);
                            if (hp == null)
                                hp = "Invalid Hardpoint";
                            return hp;
                        });
                    else if (_itemTypes[i] == typeof(HullData))
                        itemGroups = typedItems.GroupBy(item => Enum.GetName(typeof(HullType), (item as HullData).HullType));
                    else itemGroups = typedItems.GroupBy(item => "All");

                    foreach (var itemGroup in itemGroups)
                    {
                        using (var h = new HorizontalScope(ListItemStyle))
                        {
                            GUILayout.Space(20);
                            if (Foldout(_itemGroupFoldouts.Contains(itemGroup.Key), FormatTypeName(itemGroup.Key), true))
                                _itemGroupFoldouts.Add(itemGroup.Key);
                            else _itemGroupFoldouts.Remove(itemGroup.Key);
                        }

                        if (_itemGroupFoldouts.Contains(itemGroup.Key))
                        {
                            foreach (var item in itemGroup)
                            {
                                var style = ListItemStyle;
                                var selected = SelectedItem == item.ID;
                                using (new HorizontalScope(selected?SelectedStyle:style))
                                {
                                    using (var h = new HorizontalScope())
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
                                                Select(item);
                                        }

                                        GUILayout.Space(30);
                                        GUILayout.Label(item.Name, selected ? EditorStyles.whiteLabel : GUI.skin.label);
                                    }

                                    using (var h = new HorizontalScope(GUILayout.Width(15)))
                                    {
                                        if (GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                                        {
                                            if(_itemBlueprintFoldouts.Contains(item.ID))
                                                _itemBlueprintFoldouts.Remove(item.ID);
                                            else
                                                _itemBlueprintFoldouts.Add(item.ID);
                                        }
                                        GUILayout.Label(_itemBlueprints[item.ID].Count.ToString());
                                        var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                        if (Event.current.type == EventType.Repaint)
                                        {
                                            var controlId = GUIUtility.GetControlID(1337, FocusType.Keyboard, position);
                                            EditorStyles.foldout.Draw(rect, GUIContent.none, controlId, _itemBlueprintFoldouts.Contains(item.ID));
                                        }
                                    }
                                }
                                
                                if (_itemBlueprintFoldouts.Contains(item.ID))
                                {
                                    foreach (var blueprintData in _itemBlueprints[item.ID]
                                        .Select(id => _databaseCache.Get<BlueprintData>(id))
                                        .OrderBy(bp => bp.Quality))
                                    {
                                        var blueprintStyle = ListItemStyle;
                                        var blueprintSelected = SelectedItem == blueprintData.ID;
                                        using (var h = new HorizontalScope(blueprintSelected ? SelectedStyle : blueprintStyle))
                                        {
                                            if (h.rect.Contains(currentEvent.mousePosition))
                                            {
                                                if (currentEventType == EventType.MouseDrag)
                                                {
                                                    DragAndDrop.PrepareStartDrag();
                                                    DragAndDrop.SetGenericData("Item", blueprintData.ID);
                                                    DragAndDrop.StartDrag("Database Item");
                                                }
                                                else if (currentEventType == EventType.MouseUp)
                                                    Select(blueprintData);
                                            }

                                            GUILayout.Space(40);
                                            GUILayout.Label(blueprintData.Name,
                                                blueprintSelected ? EditorStyles.whiteLabel : GUI.skin.label);
                                        }
                                    }

                                    using (var h = new HorizontalScope(ListItemStyle))
                                    {
                                        if (GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                                        {
                                            var blueprint = new BlueprintData
                                            {
                                                Item = item.ID,
                                                Name = item.Name
                                            };
                                            _databaseCache.Add(blueprint);
                                            Select(blueprint);
                                        }
                                        GUILayout.Space(40);
                                        GUILayout.Label("New Blueprint");
                                        var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor,
                                            0, 0);
                                    }
                                }
                            }
                            
                            using (var h = new HorizontalScope(ListItemStyle))
                            {
                                if (GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                                {
                                    CreateItem(_itemTypes[i], newItem =>
                                    {
                                        if(newItem is SimpleCommodityData simpleCommodityData)
                                        {
                                            simpleCommodityData.Category =
                                                (SimpleCommodityCategory) Enum.Parse(typeof(SimpleCommodityCategory),
                                                    itemGroup.Key);
                                            simpleCommodityData.Name = $"New {itemGroup.Key}";
                                        }
                                        else if(newItem is CompoundCommodityData compoundCommodityData)
                                        {
                                            compoundCommodityData.Category =
                                                (CompoundCommodityCategory) Enum.Parse(
                                                    typeof(CompoundCommodityCategory), itemGroup.Key);
                                            compoundCommodityData.Name = $"New {itemGroup.Key}";
                                        }
                                        else if(newItem is GearData gearData)
                                        {
                                            gearData.Hardpoint =
                                                (HardpointType) Enum.Parse(typeof(HardpointType), itemGroup.Key);
                                            gearData.Name = $"New {itemGroup.Key}";
                                        }
                                        else if(newItem is HullData hullData)
                                        {
                                            hullData.HullType = (HullType) Enum.Parse(typeof(HullType), itemGroup.Key);
                                            hullData.Name = $"New {itemGroup.Key}";
                                        }
                                    });
                                }
                                GUILayout.Space(30);
                                GUILayout.Label("New " + itemGroup.Key);
                                var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                            }
                        }
                        
                    }

                    if (!itemGroups.Any())
                    {
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
        }
        
        //var entries = _databaseCache.AllEntries.Where(item => !(item is ItemData)).ToArray();
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
                foreach (var entry in _databaseCache.GetAll(_entryTypes[i]).OrderBy(entry=>entry is INamedEntry namedEntry ? namedEntry.EntryName : entry.ID.ToString()))
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
                                Select(entry);
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
    
    void Select(DatabaseEntry item)
    {
        SelectedItem = item.ID;
        Repaint();
        _inspector.entry = _databaseCache.Get(SelectedItem);
        _inspector.Repaint();
    }

    private DatabaseEntry CreateItem(Type type, Action<DatabaseEntry> onCreate = null)
    {
        var newEntry = (DatabaseEntry) Activator.CreateInstance(type);
        if(newEntry is INamedEntry entry)
            entry.EntryName = $"New {FormatTypeName(type.Name)}";
        onCreate?.Invoke(newEntry);
        _databaseCache.Add(newEntry);
        Select(newEntry);
        return newEntry;
    }
}
