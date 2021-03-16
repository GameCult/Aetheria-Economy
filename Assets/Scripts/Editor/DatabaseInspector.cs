﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Unity.Mathematics.math;
using static UnityEditor.EditorGUILayout;
using int2 = Unity.Mathematics.int2;
using Object = UnityEngine.Object;

public class DatabaseInspector : EditorWindow
{
    public static DatabaseCache DatabaseCache;
    public DatabaseEntry entry;
    
    // Singleton to avoid multiple instances of window.
    private static DatabaseInspector _instance;
    public static DatabaseInspector Instance => _instance ? _instance : GetWindow<DatabaseInspector>();
    
    private DatabaseListView _list;
    private GUIStyle _labelStyle;
    private Vector2 _view;
    private const float IconSize = 1.0f / 16;
    // private static HardpointData _hardpoint;
    private static float2 _dragOffset;
    private bool _ctrlDown;
    private string[] _hardpointTypes;
    private const int width = 150;
    private const int labelWidth = 20;
    private const int toggleWidth = 18;
    private const int arrowWidth = 22;
    private Material _galaxyMat;
    private Texture2D _white;
    private GUIStyle _warning;
    private HashSet<int> _listItemFoldouts = new HashSet<int>();
    private Material _schematicMat;

    public Color LabelColor => EditorGUIUtility.isProSkin ? Color.white : Color.black;


    void OnEnable()
    {
        _instance = this;
        _list = DatabaseListView.Instance;
        _list.Show();
        DatabaseCache.OnDataUpdateLocal += _ => Repaint();
        DatabaseCache.OnDataUpdateRemote += _ => EditorDispatcher.Dispatch(Repaint);
        wantsMouseMove = true;
        _hardpointTypes = Enum.GetNames(typeof(HardpointType));
        _galaxyMat = new Material(Shader.Find("Unlit/GalaxyMap"));
        _schematicMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        _white = Color.white.ToTexture();
        _warning = new GUIStyle(EditorStyles.boldLabel);
        _warning.normal.textColor = Color.red;
    }
    
    public void Inspect(object obj, bool inspectablesOnly = false)
    {
        foreach (var field in obj.GetType().GetFields().OrderBy(f=>f.GetCustomAttribute<KeyAttribute>()?.IntKey ?? 0))
        {
            Inspect(obj, field, inspectablesOnly);
        }

        if (obj is EquippableItemData equippableItemData)
        {
            var restricted = false;
            var doubleRestricted = false;
            HullType type = HullType.Ship;
            foreach (var behavior in equippableItemData.Behaviors)
            {
                var restriction = behavior.GetType().GetCustomAttribute<EntityTypeRestrictionAttribute>();
                if (restriction != null)
                {
                    if (restricted && restriction.Type != type)
                        doubleRestricted = true;
                    restricted = true;
                    type = restriction.Type;
                }
            }
            if(doubleRestricted)
                GUILayout.Label("ITEM UNEQUIPPABLE: BEHAVIOR HULL TYPE CONFLICT", _warning);
            else if (restricted)
                GUILayout.Label($"Item restricted by behavior to hull type {Enum.GetName(typeof(HullType), type)}", EditorStyles.boldLabel);
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
            else if (inspectable is InspectableTemperatureAttribute)
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
        else if (type == typeof(int2))
        {
            field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (int2) field.GetValue(obj)));
        }
        else if (type == typeof(float3))
        {
            if (inspectable is InspectableColorAttribute)
            {
                field.SetValue(obj, ColorField(field.Name.SplitCamelCase(), ((float3)field.GetValue(obj)).ToColor()).ToFloat3());
            }
        }
        else if (type.IsEnum)
        {
            var isflags = type.GetCustomAttributes<FlagsAttribute>().Any();
            var names = Enum.GetNames(field.FieldType);
            field.SetValue(obj,
                Inspect(field.Name.SplitCamelCase(), (int) field.GetValue(obj), isflags ? names.Skip(1).ToArray() : names, isflags));
        }
        //else if (field.FieldType == typeof(Color)) Inspect(field.Name, () => (Color) field.GetValue(obj), c => field.SetValue(obj, c));
        else if (type == typeof(bool)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (bool) value));
        else if (type == typeof(string))
        {
            if (inspectable is InspectableUnityObjectAttribute unityObjectAttribute)
            {
                var inspectMethod = typeof(DatabaseInspector).GetMethod("InspectUnityObject");
                var generic = inspectMethod.MakeGenericMethod(unityObjectAttribute.ObjectType);
                field.SetValue(obj, generic.Invoke(this, new[] {value}));
            }
            if(inspectable is InspectablePrefabAttribute)
                field.SetValue(obj, InspectUnityObject<GameObject>(field.Name.SplitCamelCase(), (string) value));
            else if(inspectable is InspectableTextureAttribute)
                field.SetValue(obj, InspectUnityObject<Texture2D>(field.Name.SplitCamelCase(), (string) value));
            else if(inspectable is InspectableTextAssetAttribute)
                field.SetValue(obj, InspectUnityObject<TextAsset>(field.Name.SplitCamelCase(), (string) value));
            else
                field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (string) value, inspectable is InspectableTextAttribute));
        }
        else if (type == typeof(Type) && inspectable is InspectableTypeAttribute typeAttribute) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (Type) value, typeAttribute.Type));
        else if (type == typeof(GameObject)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (GameObject) value));
        else if (type == typeof(AnimationCurve)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (AnimationCurve) value));
        else if (type == typeof(Shape)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (Shape) value, (obj is HullData hull) ? hull : null));
        else if (type == typeof(PerformanceStat)) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (PerformanceStat) value, field.GetCustomAttribute<SimplePerformanceStatAttribute>() != null));
        else if (type == typeof(PerformanceStat[]))
        {
            LabelField(field.Name.SplitCamelCase(), EditorStyles.boldLabel);
            using (new VerticalScope(GUI.skin.box))
            {
                var stats = (PerformanceStat[]) value;
                var damageTypes = Enum.GetNames(typeof(DamageType));
                if(stats==null || stats.Length!=damageTypes.Length)
                    stats = new PerformanceStat[damageTypes.Length];
                for (var i = 0; i < stats.Length; i++)
                {
                    Inspect(damageTypes[i].SplitCamelCase(), stats[i]);
                }
                field.SetValue(obj, stats);
            }
        }
        else if (type == typeof(StatReference))
        {
            var statRef = (StatReference) field.GetValue(obj);
            Inspect(field.Name.SplitCamelCase(), ref statRef);
        }
        else if (type == typeof(Dictionary<Guid, int>))
        {
            var dict = (Dictionary<Guid, int>) field.GetValue(obj);
            Inspect(field.Name.SplitCamelCase(), ref dict, link.EntryType);
        }
        else if (type == typeof(Dictionary<Guid, float>))
        {
            var dict = (Dictionary<Guid, float>) field.GetValue(obj);
            var ranged = field.GetCustomAttribute<RangedFloatAttribute>();
            if(ranged != null)
                Inspect(field.Name.SplitCamelCase(), ref dict, link.EntryType, ranged.Min, ranged.Max);
            else Inspect(field.Name.SplitCamelCase(), ref dict, link.EntryType);
        }
        else if (type == typeof(List<BlueprintStatEffect>))
        {
            var list = (List<BlueprintStatEffect>) field.GetValue(obj);
            if(list==null) field.SetValue(obj, list = new List<BlueprintStatEffect>());
            
            var blueprint = obj as BlueprintData;
            var blueprintItem = DatabaseCache.Get(blueprint.Item);
            if (blueprintItem is EquippableItemData equippableBlueprintItem)
            {
                Space();
                LabelField(field.Name.SplitCamelCase(), EditorStyles.boldLabel);

                using (var v = new VerticalScope(GUI.skin.box))
                {
                    var ingredients = blueprint.Ingredients.Keys
                        .Select(i => DatabaseCache.Get(i))
                        .Where(i => i is CraftedItemData)
                        .Cast<CraftedItemData>().ToArray();
                    var ingredientNames = ingredients.Select(i => i.Name).ToArray();
                    foreach (var effect in list)
                    {
                        using (var v2 = new VerticalScope(_list.ListItemStyle))
                        {
                            using (var h = new HorizontalScope())
                            {
                                GUILayout.Label("Ingredient", GUILayout.Width(width));
                                if(ingredients.Length==0)
                                    GUILayout.Label("No Ingredients!");
                                else
                                {
                                    var selectedIngredientIndex =
                                        Array.FindIndex(ingredients, item => item.ID == effect.Ingredient);
                                    if (selectedIngredientIndex == -1)
                                    {
                                        selectedIngredientIndex = 0;
                                        GUI.changed = true;
                                    }
                                    var newSelection = Popup(selectedIngredientIndex, ingredientNames);
                                    if (newSelection != selectedIngredientIndex)
                                        GUI.changed = true;
                                    effect.Ingredient = ingredients[newSelection].ID;
                                }
                                
                                var rect = GetControlRect(false,
                                    GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1,
                                    LabelColor, 0, 0);
                                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                                {
                                    list.Remove(effect);
                                    break;
                                }
                            }
                            
                            Inspect(ref effect.StatReference, equippableBlueprintItem);
                        }
                        
                    }

                    using (var h = new HorizontalScope(_list.ListItemStyle))
                    {
                        GUILayout.Label("Add new Stat Effect", GUILayout.ExpandWidth(true));
                        var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        {
                            list.Add(new BlueprintStatEffect());
                        }
                    }
                }
            }
        }
        else if (type == typeof(List<Guid>) && link != null)
        {
            var list = (List<Guid>) field.GetValue(obj);
            if(list==null) field.SetValue(obj, list = new List<Guid>());
            Inspect(field.Name.SplitCamelCase(), ref list, link.EntryType);
        }
        else if (type == typeof(Guid) && link != null) field.SetValue(obj, Inspect(field.Name.SplitCamelCase(), (Guid) value, link.EntryType));
        else if (type.GetCustomAttribute<InspectableFieldAttribute>() != null)
        {
            Space();
            using (new HorizontalScope())
            {
                LabelField(field.Name.SplitCamelCase(), EditorStyles.boldLabel);
                if (type.IsInterface)
                {
                    var subTypes = type.GetAllInterfaceClasses();
                    var selected = value != null ? Array.IndexOf(subTypes, value.GetType()) + 1 : 0;
                    var newSelection = Popup(selected,
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
                Space();
                using (new VerticalScope(GUI.skin.box))
                {
                    var list = (IList) field.GetValue(obj);
                    if (list == null)
                    {
                        list = (IList) Activator.CreateInstance(type);
                        field.SetValue(obj, list);
                        GUI.changed = true;
                    }
                    else
                    {
                        var sorted = true;
                        var order = int.MinValue;
                        foreach(var element in list)
                        {
                            var elementType = element.GetType();
                            var elementOrder = elementType.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
                            if (elementOrder < order)
                                sorted = false;
                            order = elementOrder;
                        }
                    
                        if (!sorted)
                        {
                            var sortedList = list
                                .Cast<object>()
                                .OrderBy(o => o.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0);
                            list = (IList) Activator.CreateInstance(type);
                            foreach (var o in sortedList)
                                list.Add(o);
                            field.SetValue(obj, list);
                            GUI.changed = true;
                        }
                    }
                    foreach (var o in list)
                    {
                        if (o == null)
                        {
                            list.Remove(o);
                            break;
                        }
                        bool _tinted = false;
                        var originalColor = GUI.backgroundColor;
                        if (o is ITintInspector tint)
                        {
                            _tinted = true;
                            GUI.backgroundColor = tint.TintColor.ToColor();
                        }
                        
                        using (new VerticalScope(_list.ListItemStyle))
                        {
                            using (new HorizontalScope())
                            {
                                //if (listType.IsInterface)
                                if(Foldout(_listItemFoldouts.Contains(o.GetHashCode()), o.ToString(), true))
                                    _listItemFoldouts.Add(o.GetHashCode());
                                else
                                    _listItemFoldouts.Remove(o.GetHashCode());
                                // using (var h = new HorizontalScope())
                                // {
                                    // if (GUI.Button(h.rect, GUIContent.none, GUIStyle.none))
                                    // {
                                    //     if(_listItemFoldouts.Contains(o.GetHashCode()))
                                    //         _listItemFoldouts.Remove(o.GetHashCode());
                                    //     else
                                    //         _listItemFoldouts.Add(o.GetHashCode());
                                    // }
                                    // var foldoutRect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                    // if (Event.current.type == EventType.Repaint)
                                    // {
                                    //     var controlId = GUIUtility.GetControlID(1337, FocusType.Keyboard, position);
                                    //     EditorStyles.foldout.Draw(foldoutRect, GUIContent.none, controlId, !_listItemFoldouts.Contains(o.GetHashCode()));
                                    // }
                                    // GUILayout.Label(o.ToString(), EditorStyles.boldLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                                // }

                                var rect = GetControlRect(false,
                                    GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1,
                                    LabelColor, 0, 0);
                                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                                {
                                    list.Remove(o);
                                    break;
                                }
                            }
                            if(_listItemFoldouts.Contains(o.GetHashCode()))
                            {
                                Space();

                                Inspect(o, inspectablesOnly);
                            }
                        }
                        
                        if(_tinted)
                            GUI.backgroundColor = originalColor;
                    }
                    using (new HorizontalScope(_list.ListItemStyle))
                    {
                        GUILayout.Label($"Add new {listType.Name}", GUILayout.ExpandWidth(true));
                        var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
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
                            else if (listType.IsAbstract)
                            {
                                var menu = new GenericMenu();
                                foreach (var behaviorType in listType.GetAllChildClasses().Where(t=>!t.IsAbstract))
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
                Space();
                using (var v = new VerticalScope(GUI.skin.box))
                {
                    var list = (IList) field.GetValue(obj);
                    if (list == null)
                    {
                        list = (IList) Activator.CreateInstance(type);
                        field.SetValue(obj, list);
                    }
                    
                    if(list.Count==0)
                        using (new HorizontalScope(_list.ListItemStyle))
                            GUILayout.Label($"Add a {listType.Name} by dragging from the project hierarchy.");
                    
                    foreach (var oo in list)
                    {
                        var o = (Object) oo;
                        using (new HorizontalScope(_list.ListItemStyle))
                        {
                            GUILayout.Label(o.name.SplitCamelCase(), EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                            var rect = GetControlRect(false,
                                GUILayout.Width(EditorGUIUtility.singleLineHeight));
                            GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1,
                                LabelColor, 0, 0);
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
            using (new HorizontalScope())
            {
                GUILayout.Label(field.Name.SplitCamelCase(), GUILayout.Width(width));
                field.SetValue(obj, ObjectField((Object)field.GetValue(obj),type,false));
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
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return Toggle(value);
        }
    }

    public float Inspect(string label, float value)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return DelayedFloatField(value);
        }
    }
    
    public string Inspect(string label, string value, bool area = false)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            if (area)
            {
                EditorStyles.textArea.wordWrap = true;
                return DelayedTextField(value, EditorStyles.textArea, /*GUILayout.Width(EditorGUIUtility.currentViewWidth-width-10), */GUILayout.Height(100));
            }
            return DelayedTextField(value);
        }
    }
    
    public float InspectTemperature(string label, float value)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            GUILayout.Label("°K", _labelStyle, GUILayout.Width(labelWidth));
            value = DelayedFloatField(value);//, GUILayout.ExpandWidth(false)
            GUILayout.Label("°C", _labelStyle, GUILayout.Width(labelWidth));
            value = DelayedFloatField(value - 273.15f) + 273.15f;
            GUILayout.Label("°F", _labelStyle, GUILayout.Width(labelWidth));
            value = (DelayedFloatField((value - 273.15f) * 1.8f + 32) - 32) / 1.8f + 273.15f;
                
            return value;
        }
    }

    public float Inspect(string label, float value, float min, float max)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return Slider(value, min, max);
        }
    }

    public int Inspect(string label, int value)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return DelayedIntField(value);
        }
    }

    public int2 Inspect(string label, int2 value)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            GUILayout.Label("X", GUILayout.Width(labelWidth));
            value.x = DelayedIntField(value.x);
            GUILayout.Label("Y", GUILayout.Width(labelWidth));
            value.y = DelayedIntField(value.y);
            if (GUILayout.Button("\u2190", GUILayout.Width(arrowWidth)))
                value.x--;
            if (GUILayout.Button("\u2192", GUILayout.Width(arrowWidth)))
                value.x++;
            if (GUILayout.Button("\u2191", GUILayout.Width(arrowWidth)))
                value.y++;
            if (GUILayout.Button("\u2193", GUILayout.Width(arrowWidth)))
                value.y--;
            return value;
        }
    }

    public int Inspect(string label, int value, int min, int max)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return IntSlider(value, min, max);
        }
    }

    public int Inspect(string label, int value, string[] enumOptions, bool flags)
    {
        if (flags)
        {
            Space();
            LabelField(label, EditorStyles.boldLabel);
            using (var v = new VerticalScope(GUI.skin.box))
            {
                for (var i = 0; i < enumOptions.Length; i++)
                {
                    using (var h = new HorizontalScope())
                    {
                        GUILayout.Label(enumOptions[i], GUILayout.Width(width));
                        var set = Toggle(1 << i == (1 << i & value));
                        if (set)
                            value |= 1 << i;
                        else value &= ~(1 << i);
                    }
                }
            }

            return value;
        }

        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return Popup(value, enumOptions);
        }
    }

    public Type Inspect(string label, Type value, Type parentType)
    {
        var types = parentType.GetAllChildClasses();
        var enumOptions = new[] {"None"}.Concat(types.Select(t => t.Name)).ToArray();
        var index = Array.IndexOf(types, value) + 1;

        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            index = Popup(index, enumOptions);
        }

        return index == 0 ? null : types[index - 1];
    }

    public string InspectUnityObject<T>(string label, string value) where T : Object
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return AssetDatabase.GetAssetPath(ObjectField(AssetDatabase.LoadAssetAtPath<T>(value), typeof(T), false));
        }
    }

    public GameObject Inspect(string label, GameObject value)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return (GameObject) ObjectField(value, typeof(GameObject), false);
        }
    }

    public float4[] InspectAnimationCurve(string label, float4[] value)
    {
        var val = value != null && value.Length > 0
            ? value.ToCurve()
            : new AnimationCurve();
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            val = CurveField(val, Color.yellow, new Rect(0, 0, 1, 1));
        }

        return val.keys.Select(k => float4(k.time, k.value, k.inTangent, k.outTangent)).ToArray();
    }

    public AnimationCurve Inspect(string label, AnimationCurve value)
    {
        using (var h = new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            if (value == null)
                value = new AnimationCurve();
            value = CurveField(value, Color.yellow, new Rect(0, 0, 1, 1));
        }

        return value;
    }

    public Guid Inspect(string label, Guid value, Type entryType)
    {
        LabelField(label, EditorStyles.boldLabel);
        var valueEntry = DatabaseCache.Get(value);
        using (var h = new HorizontalScope(GUI.skin.box))
        {
            GUILayout.Label((valueEntry as INamedEntry)?.EntryName ?? valueEntry?.ID.ToString() ?? $"Assign a {entryType.Name} by dragging from the list panel.");
                
            if (h.rect.Contains(Event.current.mousePosition) && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform))
            {
                var guid = (Guid) DragAndDrop.GetGenericData("Item");
                var dragEntry = DatabaseCache.Get(guid);
                var dragValid = dragEntry != null && entryType.IsInstanceOfType(dragEntry) && dragEntry != entry;
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = dragValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (dragValid)
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
    
    public void Inspect(string label, ref Dictionary<Guid,int> value, Type referenceType)
    {
        Space();
        LabelField(label, EditorStyles.boldLabel);

        using (var v = new VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label("Drag from list to add item");
                }
            }
            foreach (var ingredient in value.ToArray())
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    var entry = DatabaseCache.Get(ingredient.Key);
                    if (entry == null)
                    {
                        value.Remove(ingredient.Key);
                        GUI.changed = true;
                        return;
                    }
                    if(entry is INamedEntry named)
                        GUILayout.Label(named.EntryName);
                    else GUILayout.Label(entry.ID.ToString());
                    
                    value[ingredient.Key] = DelayedIntField(value[ingredient.Key], GUILayout.Width(50));
                    
                    var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none) || value[ingredient.Key]==0)
                        value.Remove(ingredient.Key);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                var dragData = DragAndDrop.GetGenericData("Item");
                var isId = dragData is Guid guid;
                var dragEntry = isId ? DatabaseCache.Get(guid) : null;
                var correctType = referenceType.IsInstanceOfType(dragEntry);
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = correctType ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (isId && correctType)
                    {
                        DragAndDrop.AcceptDrag();
                        value[guid] = 1;
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public void Inspect(string label, ref Dictionary<Guid,float> value, Type referenceType)
    {
        Space();
        LabelField(label, EditorStyles.boldLabel);

        using (var v = new VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label("Drag from list to add item");
                }
            }
            foreach (var ingredient in value.ToArray())
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    var entry = DatabaseCache.Get(ingredient.Key);
                    if (entry == null)
                    {
                        value.Remove(ingredient.Key);
                        GUI.changed = true;
                        return;
                    }
                    
                    if(entry is INamedEntry named)
                        GUILayout.Label(named.EntryName);
                    else GUILayout.Label(entry.ID.ToString());
                    value[ingredient.Key] = DelayedFloatField(value[ingredient.Key], GUILayout.Width(50));
                    
                    var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        value.Remove(ingredient.Key);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                var dragData = DragAndDrop.GetGenericData("Item");
                var isId = dragData is Guid guid;
                var dragEntry = isId ? DatabaseCache.Get(guid) : null;
                var correctType = referenceType.IsInstanceOfType(dragEntry);
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = correctType ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (isId && correctType)
                    {
                        DragAndDrop.AcceptDrag();
                        value[guid] = 1;
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public void Inspect(string label, ref Dictionary<Guid,float> value, Type referenceType, float min, float max)
    {
        Space();
        LabelField(label, EditorStyles.boldLabel);

        using (var v = new VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label("Drag from list to add item");
                }
            }
            foreach (var ingredient in value.ToArray())
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    var entry = DatabaseCache.Get(ingredient.Key);
                    if (entry == null)
                    {
                        value.Remove(ingredient.Key);
                        GUI.changed = true;
                        return;
                    }
                    
                    if(entry is INamedEntry named)
                        GUILayout.Label(named.EntryName);
                    else GUILayout.Label(entry.ID.ToString());
                    value[ingredient.Key] = Slider(value[ingredient.Key], min, max);
                    
                    var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        value.Remove(ingredient.Key);
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                var dragData = DragAndDrop.GetGenericData("Item");
                var isId = dragData is Guid guid;
                var dragEntry = isId ? DatabaseCache.Get(guid) : null;
                var correctType = referenceType.IsInstanceOfType(dragEntry);
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = correctType ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (isId && correctType)
                    {
                        DragAndDrop.AcceptDrag();
                        value[guid] = max;
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public void Inspect(string label, ref List<Guid> value, Type entryType)
    {
        Space();
        LabelField(label, EditorStyles.boldLabel);

        using (var v = new VerticalScope(GUI.skin.box))
        {
            if (value.Count == 0)
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    GUILayout.Label($"Drag from list to add {entryType.Name}");
                }
            }
            foreach (var guid in value.ToArray())
            {
                using (var h = new HorizontalScope(_list.ListItemStyle))
                {
                    var entry = DatabaseCache.Get(guid);
                    if (entry == null)
                    {
                        value.Remove(guid);
                        GUI.changed = true;
                    }
                    else
                    {
                        GUILayout.Label((entry as INamedEntry)?.EntryName ?? entry.ID.ToString());
                    
                        var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                        GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        {
                            value.Remove(guid);
                        }
                    }
                }
            }

            if (v.rect.Contains(Event.current.mousePosition))
            {
                var guid = DragAndDrop.GetGenericData("Item");
                var dragObj = DragAndDrop.GetGenericData("Item") is Guid ? DatabaseCache.Get((Guid) guid) : null;
                var dragValid = entryType.IsInstanceOfType(dragObj) && dragObj != entry;
                if(Event.current.type == EventType.DragUpdated)
                    DragAndDrop.visualMode = dragValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                else if(Event.current.type == EventType.DragPerform)
                {
                    if (dragValid)
                    {
                        DragAndDrop.AcceptDrag();
                        value.Add((Guid) guid);
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    public void Inspect(string label, ref StatReference effect)
    {
        using (new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            
            using (new VerticalScope())
            {
                var targetObjects = typeof(BehaviorData).GetAllChildClasses()
                    .Concat(typeof(EquippableItemData).GetAllChildClasses()).ToArray();
                var objectNames = targetObjects.Select(b => b.Name).ToArray();
                var selectedIndex = Array.IndexOf(objectNames, effect.Target);
                if (selectedIndex == -1)
                {
                    selectedIndex = 0;
                    GUI.changed = true;
                }
                using (new HorizontalScope())
                {
                    GUILayout.Label("Target", GUILayout.Width(width));
                    var newSelection = Popup(selectedIndex, objectNames);
                    if (newSelection != selectedIndex)
                        GUI.changed = true;
                    effect.Target = objectNames[newSelection];
                }

                using (new HorizontalScope())
                {
                    GUILayout.Label("Stat", GUILayout.Width(width));
                    var stats = targetObjects[selectedIndex].GetFields()
                        .Where(f => f.FieldType == typeof(PerformanceStat)).ToArray();
                    if(stats.Length==0)
                        GUILayout.Label("No Stats!");
                    else
                    {
                        var statNames = stats.Select(s => s.Name).ToArray();
                        var selectedStatIndex = Array.IndexOf(statNames, effect.Stat);
                        if (selectedStatIndex == -1)
                        {
                            selectedStatIndex = 0;
                            GUI.changed = true;
                        }
                        var newSelection = Popup(selectedStatIndex, statNames);
                        if (newSelection != selectedStatIndex)
                            GUI.changed = true;
                        effect.Stat = statNames[newSelection];
                    }
                }
            }
        }
    }

    public void Inspect(ref StatReference effect, EquippableItemData item)
    {
        using (new VerticalScope())
        {
            var behaviorNames = new []{item.Name}.Concat(item.Behaviors.Select(b => b.GetType().Name)).ToArray();
            var selectedBehaviorIndex = Array.IndexOf(behaviorNames, effect.Target);
            if (selectedBehaviorIndex == -1)
            {
                selectedBehaviorIndex = 0;
                GUI.changed = true;
            }
            using (new HorizontalScope())
            {
                GUILayout.Label("Behavior", GUILayout.Width(width));
                var newSelection = Popup(selectedBehaviorIndex, behaviorNames);
                if (newSelection != selectedBehaviorIndex)
                    GUI.changed = true;
                effect.Target = behaviorNames[newSelection];
            }

            using (new HorizontalScope())
            {
                GUILayout.Label("Stat", GUILayout.Width(width));
                var stats = (selectedBehaviorIndex == 0 ? item.GetType() : item.Behaviors[selectedBehaviorIndex-1].GetType()).GetFields()
                    .Where(f => f.FieldType == typeof(PerformanceStat)).ToArray();
                if(stats.Length==0)
                    GUILayout.Label("No Stats!");
                else
                {
                    var statNames = stats.Select(s => s.Name).ToArray();
                    var selectedStatIndex = Array.IndexOf(statNames, effect.Stat);
                    if (selectedStatIndex == -1)
                    {
                        selectedStatIndex = 0;
                        GUI.changed = true;
                    }
                    var newSelection = Popup(selectedStatIndex, statNames);
                    if (newSelection != selectedStatIndex)
                        GUI.changed = true;
                    effect.Stat = statNames[newSelection];
                }
            }
        }
    }

    public Shape Inspect(string label, Shape value, HullData hull = null)
    {
        if(value == null)
        {
            value = new Shape(1, 1);
            value[int2.zero] = true;
        }
        using (new VerticalScope())
        {
            Texture2D schematic = null;
            if(hull != null && !string.IsNullOrEmpty(hull.Schematic))
                schematic = AssetDatabase.LoadAssetAtPath<Texture2D>(hull.Schematic);
            using (new HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(width));
                GUILayout.Label("Size", GUILayout.ExpandWidth(false));
                if(schematic == null)
                {
                    GUILayout.Label("X", GUILayout.Width(labelWidth));
                    value.Width = DelayedIntField(value.Width);
                    GUILayout.Label("Y", GUILayout.Width(labelWidth));
                    value.Height = DelayedIntField(value.Height);
                }
                else
                {
                    GUILayout.Label("X", GUILayout.Width(labelWidth));
                    value.Width = DelayedIntField(value.Width);
                    value.Height = Mathf.RoundToInt(value.Width * ((float)schematic.height / schematic.width));
                    GUILayout.Label("Y", GUILayout.Width(labelWidth));
                    LabelField($"{value.Height}");
                }
            }

            using (new HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(width));
                using (var a = new VerticalScope())
                {
                    if(schematic != null)
                    {
                        EditorGUI.DrawPreviewTexture(a.rect, schematic, _schematicMat);
                    }
                    for (var y = value.Height - 1; y >= 0; y--)
                    {
                        using (new HorizontalScope())
                        {
                            for (var x = 0; x < value.Width; x++)
                            {
                                var oldColor = GUI.backgroundColor;
                                if(hull != null)
                                {
                                    var hardpoint = hull.Hardpoints
                                        .FirstOrDefault(hp =>
                                            hp.Shape.Coordinates
                                                .Select(v => v + hp.Position)
                                                .Any(v => v.x == x && v.y == y));
                                    if (hardpoint != null)
                                        GUI.backgroundColor = hardpoint.TintColor.ToColor();
                                }
                                value[int2(x, y)] = Toggle(value[int2(x, y)], GUILayout.Width(toggleWidth));
                                GUI.backgroundColor = oldColor;
                            }
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        return value;
    }
    
    public PerformanceStat Inspect(string label, PerformanceStat value, bool isSimple = false)
    {
        if(value == null)
            value = new PerformanceStat();
        using (new VerticalScope())
        {
            using (new HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(width));
                GUILayout.Label("Min", _labelStyle, GUILayout.Width(labelWidth));
                value.Min = DelayedFloatField(value.Min);
                GUILayout.Label("Max", _labelStyle, GUILayout.Width(labelWidth + 5));
                value.Max = DelayedFloatField(value.Max);
            }

            // Only display heat, durability and quality effects if the stat actually varies
            if (Math.Abs(value.Min - value.Max) > .0001f)
            {
                using (new HorizontalScope())
                {
                    GUILayout.Label("", GUILayout.Width(width));
                    if (!isSimple)
                    {
                        GUILayout.Label("H", _labelStyle);
                        value.HeatDependent = Toggle(value.HeatDependent);
                        GUILayout.Label("HEx", _labelStyle, GUILayout.Width(labelWidth + 5));
                        value.HeatExponentMultiplier = DelayedFloatField(value.HeatExponentMultiplier);
                        GUILayout.Label("D", _labelStyle);
                        value.DurabilityDependent = Toggle(value.DurabilityDependent);
                        GUILayout.Label("QEx", _labelStyle, GUILayout.Width(labelWidth + 5));
                        value.QualityExponent = DelayedFloatField(value.QualityExponent);
                    }
                    else
                    {
                        GUILayout.Label("Quality Exponent", _labelStyle);
                        value.QualityExponent = DelayedFloatField(value.QualityExponent);
                    }
                }
            }


            // using (new HorizontalScope())
            // {
            //     GUILayout.Label("", GUILayout.Width(width));
            //     GUILayout.Label("Ingredient", GUILayout.ExpandWidth(false));
            //     var ingredients = crafted.Ingredients.Keys.Select(DatabaseCache.Get).Where(i => i is CraftedItemData).Cast<CraftedItemData>().ToList();
            //     var ingredient = value.Ingredient != null ? DatabaseCache.Get(value.Ingredient.Value) as CraftedItemData : null;
            //     var names = ingredients.Select(i => i.Name).Append("None").ToArray();
            //     var selected = value.Ingredient == null || !crafted.Ingredients.ContainsKey(value.Ingredient.Value)
            //         ? names.Length - 1
            //         : ingredients.IndexOf(ingredient);
            //     var selection = Popup(selected, names);
            //     value.Ingredient = selection == names.Length - 1 ? (Guid?) null : ingredients[selection].ID;
            // }

            Space();
        }

        return value;
    }

    public void OnGUI()
    {
        _labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {normal = {textColor = LabelColor}};
        
        Event current = Event.current;
        EventType currentEventType = current.type;
        if (currentEventType == EventType.KeyDown && current.keyCode == KeyCode.LeftControl)
            _ctrlDown = true;
        if (currentEventType == EventType.KeyUp && current.keyCode == KeyCode.LeftControl)
            _ctrlDown = false;
        
        if (entry==null)
        {
            LabelField($"{(_list.SelectedItem==Guid.Empty?"No Entry Selected":"Selected Entry Not Found in Database")}\n{_list.SelectedItem}");
            return;
        }
        
        _view = BeginScrollView(
            _view, 
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUI.skin.scrollView,
            GUILayout.Width(EditorGUIUtility.currentViewWidth),
            GUILayout.ExpandHeight(true));

        // Serialize inspected object so we can check whether it has actually changed
        RegisterResolver.Register();
        var previousBytes = MessagePackSerializer.Serialize(entry);
        
        EditorGUI.BeginChangeCheck();

        var dataEntry = entry;
        
        using (new HorizontalScope())
        {
            if(GUILayout.Button("Print JSON"))
                Debug.Log(entry.ToJson());
            // if (GUILayout.Button("Duplicate"))
            // {
            //     DatabaseCache.Duplicate(entry);
            // }
            if (GUILayout.Button("Delete Entry"))
            {
                DatabaseCache.Delete(entry);
                entry = null;
                return;
            }

            if (GUILayout.Button("Clone Entry"))
            {
                var copy = JsonConvert.DeserializeObject<DatabaseEntry>(JsonConvert.SerializeObject(entry));
                copy.ID = Guid.NewGuid();
                DatabaseCache.Add(copy);
            }
        }
        
        using (var h = new HorizontalScope())
        {
            GUILayout.Label("ID");
            LabelField(dataEntry.ID.ToString());
        }

        Inspect(entry, true);
    
        #region Hull
        var hull = entry as HullData;
        if (hull != null)
        {
            Space();

            // TODO: Hull Hardpoints Inspector
            
        }
        #endregion

        #region Loadout
        var loadout = entry as LoadoutData;
        if (loadout != null)
        {
            Space();
            LabelField("Loadout Hull", EditorStyles.boldLabel);

            if (DatabaseCache.Get(loadout.Hull) is HullData loadoutHull)
            {
                // Reserve slots in the loadout for all equippable (non-hull) hardpoints
                var hardpoints = loadoutHull.Hardpoints.Where(h => h.Type != HardpointType.Hull).ToArray();
                if (loadout.Gear.Count < hardpoints.Length)
                    loadout.Gear.AddRange(Enumerable.Repeat(Guid.Empty, hardpoints.Length - loadout.Gear.Count));
        
                Space();
                using (var h = new HorizontalScope())
                {
                    // GUILayout.Label($"Equipped Items: {loadout.Gear.Sum(i=>((ItemData) DatabaseCache.Get(i))?.Size ?? 0)}/{loadoutHull.Capacity.Min}", EditorStyles.boldLabel);
                    GUILayout.Label($"Mass: {loadout.Gear.Sum(i=>((ItemData) DatabaseCache.Get(i))?.Mass ?? 0)}", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                }
        
                int itemIndex = 0;
                using (var v = new VerticalScope(GUI.skin.box))
                {
                    for (; itemIndex < hardpoints.Length; itemIndex++)
                    {
                        var hp = hardpoints[itemIndex];
                        
                        var loadoutItem = DatabaseCache.Get(loadout.Gear[itemIndex]) as EquippableItemData;
        
                        if (loadoutItem != null && loadoutItem.HardpointType != hp.Type)
                        {
                            loadout.Gear[itemIndex] = Guid.Empty;
                            Debug.Log($"Invalid Item \"{loadoutItem.Name}\" in {Enum.GetName(typeof(HardpointType),hp.Type)} hardpoint for loadout \"{loadout.Name}\"");
                            GUI.changed = true;
                        }
                        
                        using (var h = new HorizontalScope(_list.ListItemStyle))
                        {
                            if (h.rect.Contains(current.mousePosition) &&
                                (currentEventType == EventType.DragUpdated || currentEventType == EventType.DragPerform))
                            {
                                var guid = (Guid) DragAndDrop.GetGenericData("Item");
                                var draggedEquippable = DatabaseCache.Get(guid) as EquippableItemData;
                                var good = draggedEquippable != null && draggedEquippable.HardpointType == hp.Type;
                                if (currentEventType == EventType.DragUpdated)
                                    DragAndDrop.visualMode = good ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                                else if (currentEventType == EventType.DragPerform && good)
                                {
                                    DragAndDrop.AcceptDrag();
                                    loadout.Gear[itemIndex] = guid;
        
                                    GUI.changed = true;
                                }
                            }
        
                            GUILayout.Label(Enum.GetName(typeof(HardpointType), hp.Type));
                            if (loadoutItem == null || loadoutItem.HardpointType != hp.Type)
                                GUILayout.Label("Empty Hardpoint", GUILayout.ExpandWidth(false));
                            else
                            {
                                GUILayout.Label(loadoutItem.Name, GUILayout.ExpandWidth(false));
                                var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                                GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1, Color.black, 0, 0);
                                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                                    loadout.Gear[itemIndex] = Guid.Empty;
                            }
                        }
                    }
                }
                
                for (; itemIndex < loadout.Gear.Count; itemIndex++)
                {
                    loadout.Gear.RemoveAt(itemIndex--);
                    GUI.changed = true;
                }
            }
        }
        #endregion
        
        if (EditorGUI.EndChangeCheck())
        {
            if(!previousBytes.ByteEquals(MessagePackSerializer.Serialize(entry)))
                DatabaseCache.Add(entry);
        }
            
        EndScrollView();

    }
}
