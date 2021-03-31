/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JsonKnownTypes;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

public abstract class DatabaseListView : EditorWindow
{
    public static GUIStyle ListStyleOdd;
    public static GUIStyle ListStyleEven;
    
    public Guid SelectedItem;
    public GUIStyle SelectedStyle;

    public static RethinkDB R = RethinkDB.R;
    
    protected CultCache cultCache;
    private static DatabaseInspector _inspector;
    private Type[] _entryTypes;
    private bool[] _typeFoldouts;
    private string _connectionString;
    private Vector2 _view;
    private RethinkQueryStatus _queryStatus;

    private HashSet<int> _groupFoldouts = new HashSet<int>(); 
    
    protected abstract string DatabaseName { get; }
    protected abstract string FilePath { get; }
    protected virtual DatabaseEntryGroup[] Groupers => new DatabaseEntryGroup[] { };

    protected class DatabaseEntryGroup<T, K> : DatabaseEntryGroup where T : DatabaseEntry
    {
        public Func<T,K> GroupSelector { get; }
        public Func<K,string> GroupLabel { get; }
        public Action<T, K> Activator { get; }
        
        public DatabaseEntryGroup(Func<T, K> groupSelector, Func<K, string> groupLabel, Action<T, K> activator)
        {
            GroupSelector = groupSelector;
            GroupLabel = groupLabel;
            Activator = activator;
        }

        public override bool CanGroup(Type type)
        {
            return typeof(T) == type;
        }

        public override object SelectGroup(DatabaseEntry o)
        {
            if (o is T t) return GroupSelector(t);
            return null;
        }

        public override string GetGroupName(object o)
        {
            if (o is K k) return GroupLabel(k);
            return "ERROR";
        }

        public override int HashGroup(object o)
        {
            if (o is K k) return k.GetHashCode();
            return 0;
        }

        public override void Activate(DatabaseEntry o, object g)
        {
            if(o is T t && g is K k) Activator(t, k);
            throw new ArgumentException(
                $"Entry group activation failed! Expected: object type {typeof(T)}, group type {typeof(K)}; received: object type {o.GetType()}, group type {g.GetType()}");
        }
    }

    protected abstract class DatabaseEntryGroup
    {
        public abstract bool CanGroup(Type type);
        public abstract object SelectGroup(DatabaseEntry o);
        public abstract string GetGroupName(object o);
        public abstract int HashGroup(object o);
        public abstract void Activate(DatabaseEntry o, object g);
    }

    public Color LabelColor => EditorGUIUtility.isProSkin ? Color.white : Color.black;

    private bool _listItemStyle;
    public GUIStyle ListItemStyle =>
        (_listItemStyle = !_listItemStyle) ? ListStyleEven : ListStyleOdd;
    
    void OnEnable()
    {
        ListStyleOdd = new GUIStyle
        {
            normal = {background = EditorGUIUtility.isProSkin ? XKCDColors.DarkGrey.ToTexture() : XKCDColors.LightGrey.ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
        ListStyleEven = new GUIStyle
        {
            normal = {background = EditorGUIUtility.isProSkin ? Color.Lerp(XKCDColors.DarkGrey,Color.black,.5f).ToTexture() : Color.Lerp(XKCDColors.LightGrey,Color.white,.5f).ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
        
        cultCache = new CultCache(FilePath);
        
        cultCache.OnDataUpdateLocal += _ => Repaint();
        cultCache.OnDataInsertLocal += _ => Repaint();
        cultCache.OnDataDeleteLocal += _ => Repaint();
        cultCache.OnDataUpdateRemote += _ => EditorDispatcher.Dispatch(Repaint);
        cultCache.OnDataInsertRemote += _ => EditorDispatcher.Dispatch(Repaint);
        cultCache.OnDataDeleteRemote += _ => EditorDispatcher.Dispatch(Repaint);

        DatabaseInspector.CultCache = cultCache;
        _inspector = DatabaseInspector.Instance;
        _inspector.Show();
        
        _entryTypes = typeof(DatabaseEntry).GetAllChildClasses()
            .Where(t => t.GetCustomAttribute<InspectableAttribute>() != null).ToArray();
        
        _typeFoldouts = new bool[_entryTypes.Length];

        if (EditorPrefs.HasKey("RethinkDB.URL"))
            _connectionString = EditorPrefs.GetString("RethinkDB.URL");
        else
            _connectionString = "Enter DB URL";
        
        InitStyles();
    }

    private void InitStyles()
    {
        SelectedStyle = new GUIStyle
        {
            normal = {background = EditorGUIUtility.isProSkin ? XKCDColors.DarkGreyBlue.ToTexture() : XKCDColors.Greyblue.ToTexture()},
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
                JsonKnownTypesSettingsManager.RegisterTypeAssembly<ItemData>();
                _queryStatus = RethinkConnection.RethinkConnect(cultCache, _connectionString, DatabaseName);
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
                cultCache.Save();
                Debug.Log("Local DB Cache Saved!");
            }

            if (GUILayout.Button("Load"))
            {
                cultCache.Load();
                Debug.Log("Loaded DB From Local Cache!");
            }
        }

        if (_queryStatus != null && _queryStatus.RetrievedEntries < _queryStatus.TotalEntries)
        {
            var progressRect = GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, (float)_queryStatus.RetrievedEntries/_queryStatus.TotalEntries, "Sync Progress");
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
        
        for (var i = 0; i < _entryTypes.Length; i++)
        {
            using (var h = new HorizontalScope(ListItemStyle))
            {
                _typeFoldouts[i] = Foldout(_typeFoldouts[i],
                    ((NameAttribute) _entryTypes[i].GetCustomAttribute(typeof(NameAttribute)))?.Name ?? FormatTypeName(_entryTypes[i].Name),
                    true);
            }

            if (_typeFoldouts[i])
            {
                // TODO: NESTED GROUPS!
                var entries = cultCache.GetAll(_entryTypes[i])
                    .OrderBy(entry => entry is INamedEntry namedEntry ? namedEntry.EntryName : entry.ID.ToString());
                var grouper = Groupers.FirstOrDefault(g => g.CanGroup(_entryTypes[i]));
                if (grouper != null)
                {
                    var groups = entries.GroupBy(o => grouper.SelectGroup(o));
                    foreach (var group in groups)
                    {
                        var groupHash = grouper.HashGroup(group.Key);
                        var groupName = grouper.GetGroupName(group.Key);
                        using (var h = new HorizontalScope(ListItemStyle))
                        {
                            GUILayout.Space(10);
                            if (Foldout(_groupFoldouts.Contains(groupHash), FormatTypeName(groupName), true))
                                _groupFoldouts.Add(groupHash);
                            else _groupFoldouts.Remove(groupHash);
                        }

                        if (_groupFoldouts.Contains(groupHash))
                        {
                            int index = 0;
                            foreach (var entry in group)
                            {
                                index++;
                                var style = ListItemStyle;
                                var selected = SelectedItem == entry.ID;
                                using (new HorizontalScope(selected?SelectedStyle:style))
                                {
                                    using (var h = new HorizontalScope())
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

                                        GUILayout.Space(20);
                                        GUILayout.Label((entry as INamedEntry)?.EntryName ?? index.ToString(), selected ? EditorStyles.whiteLabel : GUI.skin.label);
                                    }
                                }
                            }
                    
                            using (var h = new HorizontalScope(ListItemStyle))
                            {
                                if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                                    CreateItem(_entryTypes[i], entry => grouper.Activate(entry, group.Key));
                                GUILayout.Space(20);
                                GUILayout.Label($"New {groupName}");
                            }
                        }
                    }
                }
                else
                {
                    int index = 0;
                    foreach (var entry in entries)
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
                }
                    
                using (var h = new HorizontalScope(ListItemStyle))
                {
                    if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                        CreateItem(_entryTypes[i]);
                    GUILayout.Space(10);
                    GUILayout.Label($"New {_entryTypes[i].Name}");
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
        _inspector.entry = cultCache.Get(SelectedItem);
        _inspector.Repaint();
    }

    private DatabaseEntry CreateItem(Type type, Action<DatabaseEntry> onCreate = null)
    {
        var newEntry = (DatabaseEntry) Activator.CreateInstance(type);
        if(newEntry is INamedEntry entry)
            entry.EntryName = $"New {FormatTypeName(type.Name)}";
        onCreate?.Invoke(newEntry);
        cultCache.Add(newEntry);
        Select(newEntry);
        return newEntry;
    }
}
