using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Unity.Mathematics.math;

public class DatabaseInspector : EditorWindow
{
    public static DatabaseCache DatabaseCache;
    public DatabaseEntry entry;
    
    // Singleton to avoid multiple instances of window. 
    private static DatabaseInspector _instance;
    
    private DatabaseListView _list;
    private GUIStyle _labelStyle;
    private Vector2 _view;
    private const float IconSize = 1.0f / 16;
    // private static HardpointData _hardpoint;
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
        DatabaseCache.OnDataUpdateLocal += _ => Repaint();
        DatabaseCache.OnDataUpdateRemote += _ => EditorDispatcher.Dispatch(Repaint);
        wantsMouseMove = true;
        _hardpointTypes = Enum.GetNames(typeof(HardpointType));
    }
    
    public void Inspect(object obj, bool inspectablesOnly = false)
    {
        foreach (var field in obj.GetType().GetFields())
        {
            Inspect(obj, field, inspectablesOnly);
        }
    }
    
    public void Inspect(object obj, FieldInfo field, bool inspectablesOnly = false)
    {
        var inspectable = field.GetCustomAttribute<InspectableFieldAttribute>();
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
        else if (type == typeof(float4[]) && inspectable is InspectableAnimationCurveAttribute)
        {
            field.SetValue(obj, InspectAnimationCurve(field.Name.SplitCamelCase(), (float4[]) value));
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
        else if (type == typeof(string))
        {
            if(inspectable is InspectablePrefabAttribute)
                field.SetValue(obj, InspectGameObject(field.Name.SplitCamelCase(), (string) value));
            else
                field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (string) value, inspectable is InspectableTextAttribute));
        }
        else if (type == typeof(GameObject)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (GameObject) value));
        else if (type == typeof(AnimationCurve)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (AnimationCurve) value));
        else if (type == typeof(PerformanceStat)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (PerformanceStat) value, obj as CraftedItemData));
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
                    stats[i] = Inspect(damageTypes[i].SplitCamelCase(), stats[i], obj as CraftedItemData);
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
        else if (type.GetCustomAttribute<InspectableFieldAttribute>() != null)
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
            if(value != null) Inspect(field.GetValue(obj), inspectablesOnly);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = field.FieldType.GenericTypeArguments[0];
            if (listType.GetCustomAttribute<InspectableFieldAttribute>() != null)
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

                            Inspect(o, inspectablesOnly);
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
                                        DatabaseCache.Add(entry);
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

    public string InspectGameObject(string label, string value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<GameObject>(value), typeof(GameObject), false));
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

    public float4[] InspectAnimationCurve(string label, float4[] value)
    {
        var val = value != null && value.Length > 0
            ? new AnimationCurve(value.Select(v => new Keyframe(v.x, v.y, v.z, v.w)).ToArray())
            : new AnimationCurve();
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            val = EditorGUILayout.CurveField(val, Color.yellow, new Rect(0, 0, 1, 1));
        }

        return val.keys.Select(k => float4(k.time, k.value, k.inTangent, k.outTangent)).ToArray();
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
        var valueEntry = DatabaseCache.Get(value);
        using (var h = new EditorGUILayout.HorizontalScope(GUI.skin.box))
        {
            GUILayout.Label((valueEntry as INamedEntry)?.EntryName ?? valueEntry?.ID.ToString() ?? $"Assign a {entryType.Name} by dragging from the list panel.");
                
            if (h.rect.Contains(Event.current.mousePosition) && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform))
            {
                var guid = (Guid) DragAndDrop.GetGenericData("Item");
                var dragEntry = DatabaseCache.Get(guid);
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
                    GUILayout.Label(DatabaseCache.Get<ItemData>(ingredient.Key).Name);
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
                    GUILayout.Label(DatabaseCache.Get(ingredient.Key).ID.ToString());
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
                    GUILayout.Label((DatabaseCache.Get(ingredient) as INamedEntry)?.EntryName ?? DatabaseCache.Get(ingredient).ID.ToString());
                    
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        value.Remove(ingredient);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                var guid = DragAndDrop.GetGenericData("Item");
                var dragObj = DragAndDrop.GetGenericData("Item") is Guid ? DatabaseCache.Get((Guid) guid) : null;
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
    
    public PerformanceStat Inspect(string label, PerformanceStat value, CraftedItemData crafted)
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
                var ingredients = crafted.Ingredients.Keys.Select(DatabaseCache.Get).Where(i => i is CraftedItemData).Cast<CraftedItemData>().ToList();
                var ingredient = value.Ingredient != null ? DatabaseCache.Get(value.Ingredient.Value) as CraftedItemData : null;
                var names = ingredients.Select(i => i.Name).Append("None").ToArray();
                var selected = value.Ingredient == null || !crafted.Ingredients.ContainsKey(value.Ingredient.Value)
                    ? names.Length - 1
                    : ingredients.IndexOf(ingredient);
                var selection = EditorGUILayout.Popup(selected, names);
                value.Ingredient = selection == names.Length - 1 ? (Guid?) null : ingredients[selection].ID;
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

        var dataEntry = entry;
        
        using (new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Print JSON"))
                Debug.Log(entry.ToJson());
            // if (GUILayout.Button("Duplicate"))
            // {
            //     DatabaseCache.Duplicate(entry);
            // }
            // if (GUILayout.Button("Delete Entry"))
            // {
            //     DatabaseCache.Delete(entry);
            //     entry = null;
            //     return;
            // }
        }
        
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("ID");
            EditorGUILayout.LabelField(dataEntry.ID.ToString());
        }

        Inspect(entry, true);
    
        #region Hull
        var hull = entry as HullData;
        if (hull != null)
        {
            EditorGUILayout.Space();

            // TODO: Hull Hardpoints Inspector
            
        }
        #endregion

        // #region Loadout
        // var loadout = entry as LoadoutData;
        // if (loadout != null)
        // {
        //     EditorGUILayout.Space();
        //     EditorGUILayout.LabelField("Loadout Hull", EditorStyles.boldLabel);
        //
        //     var loadoutHull = Database.Get(loadout.Hull) as HullData;
        //
        //     if (loadoutHull != null)
        //     {
        //         // Reserve slots in the loadout for all equippable (non-hull) hardpoints
        //         var hardpoints = loadoutHull.Hardpoints.Where(h => h.Type != HardpointType.Hull).ToArray();
        //         if (loadout.Items.Count < hardpoints.Length)
        //             loadout.Items.AddRange(Enumerable.Repeat(Guid.Empty, hardpoints.Length - loadout.Items.Count));
        //
        //         // EditorGUILayout.Space();
        //         // using (var h = new EditorGUILayout.HorizontalScope())
        //         // {
        //         //     GUILayout.Label($"Equipped Items: {loadout.Items.Sum(i=>((ItemData) Database.Get(i))?.Data.Size ?? 0)}/{loadoutHull.Capacity.Min}", EditorStyles.boldLabel);
        //         //     GUILayout.Label($"Mass: {loadout.Items.Sum(i=>((ItemData) Database.Get(i))?.Data.Mass ?? 0)}", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
        //         // }
        //
        //         int itemIndex = 0;
        //         using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
        //         {
        //             for (; itemIndex < hardpoints.Length; itemIndex++)
        //             {
        //                 var hp = hardpoints[itemIndex];
        //                 
        //                 var loadoutItem = Database.Get(loadout.Items[itemIndex]) as EquippableItemData;
        //
        //                 if (loadoutItem != null && loadoutItem.HardpointType != hp.Type)
        //                 {
        //                     loadout.Items[itemIndex] = Guid.Empty;
        //                     Debug.Log($"Invalid Item \"{loadoutItem.Name}\" in {Enum.GetName(typeof(HardpointType),hp.Type)} hardpoint for loadout \"{loadout.Name}\"");
        //                     Database.Save(entry);
        //                 }
        //                 
        //                 using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
        //                 {
        //                     if (h.rect.Contains(current.mousePosition) &&
        //                         (currentEventType == EventType.DragUpdated || currentEventType == EventType.DragPerform))
        //                     {
        //                         var guid = (Guid) DragAndDrop.GetGenericData("Item");
        //                         var draggedEquippable = Database.Get(guid) as EquippableItemData;
        //                         var good = draggedEquippable != null && draggedEquippable.HardpointType == hp.Type;
        //                         if (currentEventType == EventType.DragUpdated)
        //                             DragAndDrop.visualMode = good ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        //                         else if (currentEventType == EventType.DragPerform && good)
        //                         {
        //                             DragAndDrop.AcceptDrag();
        //                             loadout.Items[itemIndex] = guid;
        //
        //                             Database.Save(entry);
        //                         }
        //                     }
        //
        //                     GUILayout.Label(Enum.GetName(typeof(HardpointType), hp.Type));
        //                     if (loadoutItem == null || loadoutItem.HardpointType != hp.Type)
        //                         GUILayout.Label("Empty Hardpoint", GUILayout.ExpandWidth(false));
        //                     else
        //                     {
        //                         GUILayout.Label(loadoutItem.Name, GUILayout.ExpandWidth(false));
        //                         var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
        //                         GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
        //                         if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        //                             loadout.Items[itemIndex] = Guid.Empty;
        //                     }
        //                 }
        //             }
        //         }
        //
        //         EditorGUILayout.Space();
        //         EditorGUILayout.LabelField("Cargo", EditorStyles.boldLabel);
        //         
        //         using (var v = new EditorGUILayout.VerticalScope(GUI.skin.box))
        //         {
        //             if (v.rect.Contains(current.mousePosition) &&
        //                 (currentEventType == EventType.DragUpdated || currentEventType == EventType.DragPerform))
        //             {
        //                 var guid = (Guid) DragAndDrop.GetGenericData("Item");
        //                 var dragItem = Database.Get(guid) as ItemData;
        //                 if (currentEventType == EventType.DragUpdated)
        //                     DragAndDrop.visualMode = dragItem != null ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        //                 else if (currentEventType == EventType.DragPerform && dragItem != null)
        //                 {
        //                     DragAndDrop.AcceptDrag();
        //                             
        //                     loadout.Items.Add(guid);
        //                     Database.Save(entry);
        //                 }
        //             }
        //             if(itemIndex == loadout.Items.Count)
        //                 GUILayout.Label("Nothing Here!");
        //             for (; itemIndex < loadout.Items.Count; itemIndex++)
        //             {
        //                 var loadoutItem = Database.Get(loadout.Items[itemIndex]) as ItemData;
        //                 using (var h = new EditorGUILayout.HorizontalScope(_list.ListItemStyle))
        //                 {
        //                     GUILayout.Label(loadoutItem?.Name ?? "Invalid Item");
        //                     var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
        //                     GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
        //                     if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        //                         loadout.Items.RemoveAt(itemIndex--);
        //                 }
        //             }
        //         }
        //     }
        // }
        // #endregion
        
        if (EditorGUI.EndChangeCheck())
        {
            DatabaseCache.Add(entry);
        }
            
        EditorGUILayout.EndScrollView();

    }
}
