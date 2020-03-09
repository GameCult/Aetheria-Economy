using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        _inspector = DatabaseInspector.ShowWindow();
        _itemTypes = typeof(IItem).GetAllInterfaceClasses();
        _entryTypes = typeof(IDatabaseEntry).GetAllInterfaceClasses().Where(t => !t.GetInterfaces().Contains(typeof(IItem))&& !t.GetInterfaces().Contains(typeof(IItemInstance))).ToArray();
        _gearFoldouts = new bool[_itemTypes.Length];
        _entryFoldouts = new bool[_entryTypes.Length];
        InitStyles();
        Database.OnDataUpdate += Repaint;
    }

    private void InitStyles()
    {
        _listStyleOdd = new GUIStyle
        {
            normal = {background = XKCDColors.LightGrey.ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
        _listStyleEven = new GUIStyle
        {
            normal = {background = Color.Lerp(XKCDColors.LightGrey,Color.white,.5f).ToTexture()},
            margin = new RectOffset(0, 0, 0, 0)
        };
        SelectedStyle = new GUIStyle
        {
            normal = {background = XKCDColors.Greyblue.ToTexture()},
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

        Action<IDatabaseEntry> select = item =>
        {
            SelectedItem = item.GetId();
            Repaint();
            _inspector.entry = Database.Get(SelectedItem);
            _inspector.Repaint();
        };

        using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
        {
            _itemsFoldout = EditorGUILayout.Foldout(_itemsFoldout, "Items", true);
        }
        if (_itemsFoldout)
        {
            var items = Database.AllEntries.Where(item => item is IItem).Cast<IItem>().ToArray();
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
                        var selected = SelectedItem == item.GetId();
                        using (var h = new EditorGUILayout.HorizontalScope(selected?SelectedStyle:style))
                        {
                            if (h.rect.Contains(currentEvent.mousePosition))
                            {
                                if (currentEventType == EventType.MouseDrag)
                                {
                                    DragAndDrop.PrepareStartDrag();
                                    DragAndDrop.SetGenericData("Item", item.GetId());
                                    DragAndDrop.StartDrag("Database Item");
                                }
                                else if (currentEventType == EventType.MouseUp)
                                    select(item);
                            }
                            GUILayout.Space(20);
                            GUILayout.Label(item.Entry.Name, selected ? EditorStyles.whiteLabel : GUI.skin.label);
                        }
                    }
                    
                    using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
                    {
                        if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                            CreateItem(_itemTypes[i]);
                        GUILayout.Space(20);
                        GUILayout.Label("New " + _itemTypes[i].Name);
                        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                    }
                }
            }
        }
        
        var entries = Database.AllEntries.Where(item => !(item is IItem)).ToArray();
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
                foreach (var entry in entries.Where(e=>e.GetType()==_entryTypes[i]))
                {
                    var style = ListItemStyle;
                    var selected = SelectedItem == entry.GetId();
                    using (var h = new EditorGUILayout.HorizontalScope(selected?SelectedStyle:style))
                    {
                        if (h.rect.Contains(currentEvent.mousePosition))
                        {
                            if (currentEventType == EventType.MouseDrag)
                            {
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.SetGenericData("Item", entry.GetId());
                                DragAndDrop.StartDrag("Database Item");
                            }
                            else if (currentEventType == EventType.MouseUp)
                                select(entry);
                        }
                        GUILayout.Space(10);
                        GUILayout.Label(entry.Entry.Name, selected ? EditorStyles.whiteLabel : GUI.skin.label);
                    }
                }
                    
                using (var h = new EditorGUILayout.HorizontalScope(ListItemStyle))
                {
                    if(GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                        CreateItem(_entryTypes[i]);
                    GUILayout.Space(10);
                    GUILayout.Label("New " + _entryTypes[i].Name);
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
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
        GUI.SetNextControlName("");
        GUI.FocusControl("");
    }

    private void CreateItem(Type type)
    {
        var newEntry = (IDatabaseEntry) Activator.CreateInstance(type);
        SelectedItem = newEntry.GetId();
        newEntry.Entry.Name = $"New {type.Name}";
        Database.Save(newEntry);
    }
}

public class DatabaseInspector : EditorWindow
{
    public IDatabaseEntry entry;
    
    // Singleton to avoid multiple instances of window. 
    private static DatabaseInspector _instance;
    
    private DatabaseListView _list;
    private GUIStyle _labelStyle;
    private Vector2 _view;
    private const float IconSize = 1.0f / 16;
    private static HardpointData _hardpoint;
    private static float2 _dragOffset;
    private bool _ctrlDown;
    private string[] _hardpointTypes;
    private const int width = 100;
    private const int labelWidth = 20;

    // Set instance on reloading the window, else it gets lost after script reload (due to PlayMode changes, ...).
    public DatabaseInspector()
    {
        _instance = this;
    }

    public static DatabaseInspector ShowWindow()
    {
        if (_instance == null)
        {
            // "Get existing open window or if none, make a new one:" says documentation.
            // But if called after script reloads a second instance will be opened! => Custom singleton required.
            DatabaseInspector window = GetWindow<DatabaseInspector>();
            window.titleContent = new GUIContent("DB Inspector");
            _instance = window;
            window.Show();
        }
        else
            _instance.Focus();

        return _instance;
    }

    void OnEnable()
    {
        _list = DatabaseListView.ShowWindow();
        Database.OnDataUpdate += Repaint;
        wantsMouseMove = true;
        _hardpoint = null;
        _hardpointTypes = Enum.GetNames(typeof(HardpointType));
    }
    
    public void Inspect(object obj, bool inspectablesOnly = false, ICraftable craftable = null)
    {
        foreach (var field in obj.GetType().GetFields())
        {
            Inspect(obj, field, inspectablesOnly, craftable);
        }
    }
    
    public void Inspect(object obj, FieldInfo field, bool inspectablesOnly = false, ICraftable craftable = null)
    {
        var inspectable = field.GetCustomAttribute<InspectableAttribute>();
        if (inspectable == null && inspectablesOnly) return;

        var type = field.FieldType;
        var value = field.GetValue(obj);

        var link = inspectable as InspectableDatabaseLinkAttribute;
        if (type == typeof(float))
        {
            if (inspectable is RangedFloatInspectableAttribute)
            {
                var range = inspectable as RangedFloatInspectableAttribute;
                field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (float) value, range.Min, range.Max));
            }
            else if (inspectable is TemperatureInspectableAttribute)
                field.SetValue(obj, InspectTemperature(field.Name.SplitCamelCase(), (float) value));
            else
                field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (float) value));
        }
        else if (type == typeof(int))
        {
            if (inspectable is RangedIntInspectableAttribute)
            {
                var range = inspectable as RangedIntInspectableAttribute;
                field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (int) value, range.Min, range.Max));
            }
            else 
                field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (int) value));
        }
        else if (type.IsEnum) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (int) field.GetValue(obj), Enum.GetNames(field.FieldType)));
        //else if (field.FieldType == typeof(Color)) Inspect(field.Name, () => (Color) field.GetValue(obj), c => field.SetValue(obj, c));
        else if (type == typeof(bool)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (bool) value));
        else if (type == typeof(string)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (string) value, inspectable is InspectableTextAttribute));
        else if (type == typeof(GameObject)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (GameObject) value));
        else if (type == typeof(AnimationCurve)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (AnimationCurve) value));
        else if (type == typeof(PerformanceStat)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (PerformanceStat) value, craftable));
        else if (type == typeof(PerformanceStat[]))
        {
            EditorGUILayout.LabelField(field.Name.SplitCamelCase(), EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var stats = (PerformanceStat[]) value;
                var damageTypes = Enum.GetNames(typeof(DamageType));
                if(stats==null || stats.Length!=damageTypes.Length)
                    stats = new PerformanceStat[damageTypes.Length];
                for (var i = 0; i < stats.Length; i++)
                {
                    stats[i] = Inspect(damageTypes[i].SplitCamelCase(), stats[i], craftable);
                }
                field.SetValue(obj, stats);
            }
        }
        else if (type == typeof(Dictionary<Guid, int>))
        {
            var dict = (Dictionary<Guid, int>) field.GetValue(obj);
            Inspect(field.Name.SplitCamelCase(), ref dict);
        }
        else if (type == typeof(Dictionary<Guid, float>))
        {
            var dict = (Dictionary<Guid, float>) field.GetValue(obj);
            Inspect(field.Name.SplitCamelCase(), ref dict);
        }
        else if (type == typeof(List<Guid>) && link != null)
        {
            var list = (List<Guid>) field.GetValue(obj);
            if(list==null) field.SetValue(obj, (list = new List<Guid>()));
            Inspect(field.Name.SplitCamelCase(), ref list, link.EntryType);
        }
        else if (type == typeof(Guid) && link != null) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (Guid) value, link.EntryType));
        else if (type.GetCustomAttribute<InspectableAttribute>() != null)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(field.Name.SplitCamelCase(), EditorStyles.boldLabel);
                if (type.IsInterface)
                {
                    var subTypes = type.GetAllInterfaceClasses();
                    var selected = value != null ? Array.IndexOf(subTypes, value.GetType()) + 1 : 0;
                    var newSelection = EditorGUILayout.Popup(selected,
                        subTypes.Select(t => t.Name.SplitCamelCase()).Prepend("None").ToArray());
                    if(newSelection != selected)
                        field.SetValue(obj, value = newSelection==0?null:Activator.CreateInstance(subTypes[newSelection-1]));
                } else if(value == null) field.SetValue(obj, value = Activator.CreateInstance(type));
            }
            if(value != null) Inspect(field.GetValue(obj), inspectablesOnly, craftable);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = field.FieldType.GenericTypeArguments[0];
            if (listType.GetCustomAttribute<InspectableAttribute>() != null)
            {
                EditorGUILayout.Space();
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    var list = (IList) field.GetValue(obj);
                    if (list == null)
                    {
                        list = (IList) Activator.CreateInstance(type);
                        field.SetValue(obj, list);
                    }
                    foreach (var o in list)
                    {
                        using (new EditorGUILayout.VerticalScope(_list.ListItemStyle))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label(o.GetType().Name.SplitCamelCase(), EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                                var rect = EditorGUILayout.GetControlRect(false,
                                    GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1,
                                    Color.black, 0, 0);
                                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                                {
                                    list.Remove(o);
                                    break;
                                }
                            }
                            
                            EditorGUILayout.Space();

                            Inspect(o, inspectablesOnly, craftable);
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                    {
                        GUILayout.Label($"Add new {listType.Name}", GUILayout.ExpandWidth(true));
                        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        {
                            if (listType.IsInterface)
                            {
                                var menu = new GenericMenu();
                                foreach (var behaviorType in listType.GetAllInterfaceClasses())
                                {
                                    menu.AddItem(new GUIContent(behaviorType.Name), false, () =>
                                    {
                                        list.Add(Activator.CreateInstance(behaviorType));
                                        Database.Save(entry);
                                    });
                                }
                                menu.ShowAsContext();
                            }
                            else
                            {
                                list.Add(Activator.CreateInstance(listType));
                            }
                        }
                    }
                }
            }
            else if (typeof(Object).IsAssignableFrom(listType))
            {
                EditorGUILayout.Space();
                using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    var list = (IList) field.GetValue(obj);
                    if (list == null)
                    {
                        list = (IList) Activator.CreateInstance(type);
                        field.SetValue(obj, list);
                    }
                    
                    if(list.Count==0)
                        using (new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                            GUILayout.Label($"Add a {listType.Name} by dragging from the project hierarchy.");
                    
                    foreach (var oo in list)
                    {
                        var o = (Object) oo;
                        using (new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                        {
                            GUILayout.Label(o.name.SplitCamelCase(), EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                            var rect = EditorGUILayout.GetControlRect(false,
                                GUILayout.Width(EditorGUIUtility.singleLineHeight));
                            GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1,
                                Color.black, 0, 0);
                            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                            {
                                list.Remove(o);
                                break;
                            }
                        }
                    }
                    
                    if (v.rect.Contains(Event.current.mousePosition) && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform))
                    {
                        var dragObj = DragAndDrop.objectReferences.FirstOrDefault();
                        //var dragEntry = Database.Get(guid);
                        if(Event.current.type == EventType.DragUpdated)
                            DragAndDrop.visualMode = dragObj?.GetType() == listType ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        else if(Event.current.type == EventType.DragPerform)
                        {
                            if (dragObj?.GetType() == listType)
                            {
                                DragAndDrop.AcceptDrag();
                                GUI.changed = true;
                                list.Add(dragObj);
                            }
                        }
                    }
                }
            }
            else Debug.Log($"Field \"{field.Name}\" is a list of non-Inspectable type {listType.Name}. No inspector was generated.");
        }
        else if (typeof(Object).IsAssignableFrom(type))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(field.Name.SplitCamelCase(), GUILayout.Width(width));
                field.SetValue(obj, EditorGUILayout.ObjectField((Object)field.GetValue(obj),type,false));
            }
        }
//        else if (type == typeof(AudioClip))
//        {
//            using (new EditorGUILayout.HorizontalScope())
//            {
//                GUILayout.Label(field.Name.SplitCamelCase(), GUILayout.Width(width));
//                field.SetValue(obj, EditorGUILayout.ObjectField((AudioClip)field.GetValue(obj),typeof(AudioClip),false));
//            }
//        }
        else Debug.Log($"Field \"{field.Name}\" has unknown type {field.FieldType.Name}. No inspector was generated.");
    }

    public bool Inspect(string label, bool value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return EditorGUILayout.Toggle(value);
        }
    }

    public float Inspect(string label, float value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return EditorGUILayout.DelayedFloatField(value);
        }
    }
    
    public string Inspect(string label, string value, bool area = false)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            if(area)
                return EditorGUILayout.TextArea(value,GUILayout.MaxWidth(300));
            return EditorGUILayout.DelayedTextField(value);
        }
    }
    
    public float InspectTemperature(string label, float value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            GUILayout.Label("°K", _labelStyle, GUILayout.Width(labelWidth));
            value = EditorGUILayout.DelayedFloatField(value);//, GUILayout.ExpandWidth(false)
            GUILayout.Label("°C", _labelStyle, GUILayout.Width(labelWidth));
            value = EditorGUILayout.DelayedFloatField(value - 273.15f) + 273.15f;
            GUILayout.Label("°F", _labelStyle, GUILayout.Width(labelWidth));
            value = (EditorGUILayout.DelayedFloatField((value - 273.15f) * 1.8f + 32) - 32) / 1.8f + 273.15f;
                
            return value;
        }
    }

    public float Inspect(string label, float value, float min, float max)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return EditorGUILayout.Slider(value, min, max);
        }
    }

    public int Inspect(string label, int value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return EditorGUILayout.DelayedIntField(value);
        }
    }

    public int Inspect(string label, int value, int min, int max)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return EditorGUILayout.IntSlider(value, min, max);
        }
    }

    public int Inspect(string label, int value, string[] enumOptions)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return EditorGUILayout.Popup(value, enumOptions);
        }
    }

    public GameObject Inspect(string label, GameObject value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return (GameObject) EditorGUILayout.ObjectField(value, typeof(GameObject), false);
        }
    }

    public AnimationCurve Inspect(string label, AnimationCurve value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            if (value == null)
                value = new AnimationCurve();
            value = EditorGUILayout.CurveField(value, Color.yellow, new Rect(0, 0, 1, 1));
        }

        return value;
    }

    public Guid Inspect(string label, Guid value, Type entryType)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        var valueEntry = Database.Get(value);
        using (var h = new EditorGUILayout.HorizontalScope(GUI.skin.box))
        {
            GUILayout.Label(valueEntry?.Entry.Name ?? $"Assign a {entryType.Name} by dragging from the list panel.");
                
            if (h.rect.Contains(Event.current.mousePosition) && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform))
            {
                var guid = (Guid) DragAndDrop.GetGenericData("Item");
                var dragEntry = Database.Get(guid);
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = dragEntry != null && dragEntry.GetType()==entryType ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (guid != Guid.Empty && dragEntry.GetType()==entryType)
                    {
                        DragAndDrop.AcceptDrag();
                        GUI.changed = true;
                        return guid;
                    }
                }
            }
        }

        return value;
    }
    
    public void Inspect(string label, ref Dictionary<Guid,int> value)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label("Nothing Here! Add an item by dragging from the list panel.");
                }
            }
            foreach (var ingredient in value.ToArray())
            {
                using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label(Database.Get(ingredient.Key).Entry.Name);
                    value[ingredient.Key] = EditorGUILayout.DelayedIntField(value[ingredient.Key], GUILayout.Width(50));
                    
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none) || value[ingredient.Key]==0)
                        value.Remove(ingredient.Key);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                else if(Event.current.type == EventType.DragPerform)
                {
                    var guid = DragAndDrop.GetGenericData("Item");
                    if (guid is Guid)
                    {
                        DragAndDrop.AcceptDrag();
                        value[(Guid) guid] = 1;
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public void Inspect(string label, ref Dictionary<Guid,float> value)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label("Nothing Here! Add an item by dragging from the list panel.");
                }
            }
            foreach (var ingredient in value.ToArray())
            {
                using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label(Database.Get(ingredient.Key).Entry.Name);
                    value[ingredient.Key] = EditorGUILayout.DelayedFloatField(value[ingredient.Key], GUILayout.Width(50));
                    
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        value.Remove(ingredient.Key);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                else if(Event.current.type == EventType.DragPerform)
                {
                    var guid = DragAndDrop.GetGenericData("Item");
                    if (guid is Guid)
                    {
                        DragAndDrop.AcceptDrag();
                        value[(Guid) guid] = 1;
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public void Inspect(string label, ref List<Guid> value, Type entryType)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label($"Nothing Here! Add a {entryType.Name} by dragging from the list panel.");
                }
            }
            foreach (var ingredient in value.ToArray())
            {
                using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label(Database.Get(ingredient).Entry.Name);
                    
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        value.Remove(ingredient);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                var guid = DragAndDrop.GetGenericData("Item");
                var dragObj = DragAndDrop.GetGenericData("Item") is Guid ? ((Guid) guid).Get() : null;
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = entryType.IsInstanceOfType(dragObj) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (entryType.IsInstanceOfType(dragObj))
                    {
                        DragAndDrop.AcceptDrag();
                        value.Add((Guid) guid);
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public PerformanceStat Inspect(string label, PerformanceStat value, ICraftable craftable)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(width));
                GUILayout.Label("Min", _labelStyle, GUILayout.Width(labelWidth));
                value.Min = EditorGUILayout.DelayedFloatField(value.Min);
                GUILayout.Label("Max", _labelStyle, GUILayout.Width(labelWidth + 5));
                value.Max = EditorGUILayout.DelayedFloatField(value.Max);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(width));
                GUILayout.Label("H", _labelStyle);
                value.HeatDependent = EditorGUILayout.Toggle(value.HeatDependent);
                GUILayout.Label("D", _labelStyle);
                value.DurabilityDependent = EditorGUILayout.Toggle(value.DurabilityDependent);
                GUILayout.Label("QEx", _labelStyle, GUILayout.Width(labelWidth + 5));
                value.QualityExponent = EditorGUILayout.DelayedFloatField(value.QualityExponent);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(width));
                GUILayout.Label("Ingredient", GUILayout.ExpandWidth(false));
                var ingredients = craftable.CraftingIngredients.Keys.Select(Database.Get).Where(i => i is CraftedItem).Cast<CraftedItem>().ToList();
                var ingredient = Database.Get(value.Ingredient) as CraftedItem;
                var names = ingredients.Select(i => i.Entry.Name).Append("None").ToArray();
                var selected = value.Ingredient == Guid.Empty || !craftable.CraftingIngredients.ContainsKey(value.Ingredient)
                    ? names.Length - 1
                    : ingredients.IndexOf(ingredient);
                var selection = EditorGUILayout.Popup(selected, names);
                value.Ingredient = selection == names.Length - 1 ? Guid.Empty : ingredients[selection].GetId();
            }

            EditorGUILayout.Space();

            return value;
        }
    }

    public void OnGUI()
    {
        _labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {normal = {textColor = Color.black}};
        
        Event current = Event.current;
        EventType currentEventType = current.type;
        if (currentEventType == EventType.KeyDown && current.keyCode == KeyCode.LeftControl)
            _ctrlDown = true;
        if (currentEventType == EventType.KeyUp && current.keyCode == KeyCode.LeftControl)
            _ctrlDown = false;
        
        if (entry==null)
        {
            EditorGUILayout.LabelField($"{(_list.SelectedItem==Guid.Empty?"No Entry Selected":"Selected Entry Not Found in Database")}\n{_list.SelectedItem}");
            return;
        }
        
        _view = EditorGUILayout.BeginScrollView(
            _view, 
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUI.skin.scrollView,
            GUILayout.Width(EditorGUIUtility.currentViewWidth),
            GUILayout.ExpandHeight(true));

        EditorGUI.BeginChangeCheck();

        var dataEntry = entry.Entry;
        
        using (new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Print JSON"))
                Debug.Log(entry.ToJson());
            if (GUILayout.Button("Duplicate"))
            {
                Database.Duplicate(entry);
            }
            if (GUILayout.Button("Delete Entry"))
            {
                Database.Delete(entry);
                entry = null;
                return;
            }
        }
        
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("ID");
            EditorGUILayout.LabelField(dataEntry.ID.ToString());
        }

        Inspect(entry, true, entry as ICraftable);
    
        #region Hull
        var hull = entry as Hull;
        if (hull != null)
        {
            EditorGUILayout.Space();

            // TODO: Hull Hardpoints Inspector
            
        }
        #endregion

        #region Loadout
        var loadout = entry as LoadoutDefinition;
        if (loadout != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Loadout Hull", EditorStyles.boldLabel);

            var loadoutHull = Database.Get(loadout.Hull) as Hull;

            if (loadoutHull != null)
            {
                // Reserve slots in the loadout for all equippable (non-hull) hardpoints
                var hardpoints = loadoutHull.Hardpoints.Where(h => h.Type != HardpointType.Hull).ToArray();
                if (loadout.Items.Count < hardpoints.Length)
                    loadout.Items.AddRange(Enumerable.Repeat(Guid.Empty, hardpoints.Length - loadout.Items.Count));

                EditorGUILayout.Space();
                using (var h = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Equipped Items: {loadout.Items.Sum(i=>((IItem) Database.Get(i))?.Data.Size ?? 0)}/{loadoutHull.Capacity.Min}", EditorStyles.boldLabel);
                    GUILayout.Label($"Mass: {loadout.Items.Sum(i=>((IItem) Database.Get(i))?.Data.Mass ?? 0)}", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                }

                int itemIndex = 0;
                using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    for (; itemIndex < hardpoints.Length; itemIndex++)
                    {
                        var hp = hardpoints[itemIndex];
                        
                        var loadoutItem = Database.Get(loadout.Items[itemIndex]) as IEquippable;

                        if (loadoutItem != null && loadoutItem.HardpointType != hp.Type)
                        {
                            loadout.Items[itemIndex] = Guid.Empty;
                            Debug.Log($"Invalid Item \"{loadoutItem.Entry.Name}\" in {Enum.GetName(typeof(HardpointType),hp.Type)} hardpoint for loadout \"{loadout.Entry.Name}\"");
                            Database.Save(entry);
                        }
                        
                        using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                        {
                            if (h.rect.Contains(current.mousePosition) &&
                                (currentEventType == EventType.DragUpdated || currentEventType == EventType.DragPerform))
                            {
                                var guid = (Guid) DragAndDrop.GetGenericData("Item");
                                var draggedEquippable = Database.Get(guid) as IEquippable;
                                var good = draggedEquippable != null && draggedEquippable.HardpointType == hp.Type;
                                if (currentEventType == EventType.DragUpdated)
                                    DragAndDrop.visualMode = good ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                                else if (currentEventType == EventType.DragPerform && good)
                                {
                                    DragAndDrop.AcceptDrag();
                                    loadout.Items[itemIndex] = guid;

                                    Database.Save(entry);
                                }
                            }

                            GUILayout.Label(Enum.GetName(typeof(HardpointType), hp.Type));
                            if (loadoutItem == null || loadoutItem.HardpointType != hp.Type)
                                GUILayout.Label("Empty Hardpoint", GUILayout.ExpandWidth(false));
                            else
                            {
                                GUILayout.Label(loadoutItem.Entry.Name, GUILayout.ExpandWidth(false));
                                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                                    loadout.Items[itemIndex] = Guid.Empty;
                            }
                        }
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Cargo", EditorStyles.boldLabel);
                
                using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    if (v.rect.Contains(current.mousePosition) &&
                        (currentEventType == EventType.DragUpdated || currentEventType == EventType.DragPerform))
                    {
                        var guid = (Guid) DragAndDrop.GetGenericData("Item");
                        var dragItem = Database.Get(guid) as IItem;
                        if (currentEventType == EventType.DragUpdated)
                            DragAndDrop.visualMode = dragItem != null ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        else if (currentEventType == EventType.DragPerform && dragItem != null)
                        {
                            DragAndDrop.AcceptDrag();
                                    
                            loadout.Items.Add(guid);
                            Database.Save(entry);
                        }
                    }
                    if(itemIndex == loadout.Items.Count)
                        GUILayout.Label("Nothing Here!");
                    for (; itemIndex < loadout.Items.Count; itemIndex++)
                    {
                        var loadoutItem = Database.Get(loadout.Items[itemIndex]) as IItem;
                        using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
                        {
                            GUILayout.Label(loadoutItem?.Entry.Name ?? "Invalid Item");
                            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                            GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                                loadout.Items.RemoveAt(itemIndex--);
                        }
                    }
                }
            }
        }
        #endregion
        
        if (EditorGUI.EndChangeCheck())
        {
            Database.Save(entry);
        }
            
        EditorGUILayout.EndScrollView();

    }
}
