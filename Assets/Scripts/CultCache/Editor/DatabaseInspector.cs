/* This Source Code Form is subject to the terms of the Mozilla Public
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
    public static CultCache CultCache;
    public DatabaseEntry entry;
    
    // Singleton to avoid multiple instances of window.
    private static DatabaseInspector _instance;
    public static DatabaseInspector Instance => _instance ? _instance : GetWindow<DatabaseInspector>();
    
    private Vector2 _view;
    private const int width = 150;
    private GUIStyle _warning;
    private HashSet<int> _listItemFoldouts = new HashSet<int>();
    private Material _schematicMat;
    private Dictionary<(Type targetType, HashSet<Type> attributes), (object instance, Type type)> _inspectors = 
        new Dictionary<(Type targetType, HashSet<Type> attributes), (object instance, Type type)>();

    private Dictionary<Type, Type> _genericInspectors = new Dictionary<Type, Type>();

    public Color LabelColor => EditorGUIUtility.isProSkin ? Color.white : Color.black;

    void OnEnable()
    {
        _instance = this;
        CultCache.OnDataUpdateLocal += _ => Repaint();
        CultCache.OnDataUpdateRemote += _ => EditorDispatcher.Dispatch(Repaint);
        wantsMouseMove = true;
        _warning = new GUIStyle(EditorStyles.boldLabel);
        _warning.normal.textColor = Color.red;

        var inspectorTypes = typeof(BaseInspector<>).GetAllGenericChildClasses()
            .Concat(typeof(BaseInspector<,>).GetAllGenericChildClasses())
            .Concat(typeof(BaseInspector<,,>).GetAllGenericChildClasses());
        foreach (var type in inspectorTypes)
        {
            var genericArguments = type.BaseType.GetGenericArguments();
            if(genericArguments.Any())
            {
                if (genericArguments[0].ContainsGenericParameters)
                {
                    _genericInspectors.Add(genericArguments[0].GetGenericTypeDefinition(), type);
                }
                else
                {
                    _inspectors.Add((genericArguments[0], new HashSet<Type>(genericArguments.Skip(1))), (Activator.CreateInstance(type), type));
                }
            }
        }
    }
    
    public void Inspect(object obj, bool inspectablesOnly = false)
    {
        foreach (var field in obj.GetType().GetFields().OrderBy(f=>f.GetCustomAttribute<KeyAttribute>()?.IntKey ?? 0))
        {
            Inspect(obj, field, inspectablesOnly);
        }

        // if (obj is EquippableItemData equippableItemData)
        // {
        //     var restricted = false;
        //     var doubleRestricted = false;
        //     HullType type = HullType.Ship;
        //     foreach (var behavior in equippableItemData.Behaviors)
        //     {
        //         var restriction = behavior.GetType().GetCustomAttribute<EntityTypeRestrictionAttribute>();
        //         if (restriction != null)
        //         {
        //             if (restricted && restriction.Type != type)
        //                 doubleRestricted = true;
        //             restricted = true;
        //             type = restriction.Type;
        //         }
        //     }
        //     if(doubleRestricted)
        //         GUILayout.Label("ITEM UNEQUIPPABLE: BEHAVIOR HULL TYPE CONFLICT", _warning);
        //     else if (restricted)
        //         GUILayout.Label($"Item restricted by behavior to hull type {Enum.GetName(typeof(HullType), type)}", EditorStyles.boldLabel);
        // }
    }
    
    public void Inspect(object obj, FieldInfo field, bool inspectablesOnly = false)
    {
        var inspectable = field.GetCustomAttribute<InspectableAttribute>();
        if (inspectable == null && inspectablesOnly) return;

        var type = field.FieldType;
        var value = field.GetValue(obj);

        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if(_genericInspectors.ContainsKey(genericTypeDefinition))
            {
                var genericInspectorGenericType = _genericInspectors[genericTypeDefinition];
                var genericInspectorType = genericInspectorGenericType.MakeGenericType(type.GenericTypeArguments[0]);
                var genericInspector = Activator.CreateInstance(genericInspectorType);
                var parameters = new[] {field.Name.SplitCamelCase(), field.GetValue(obj), obj, this};
                field.SetValue(obj, genericInspectorType.GetMethod("Inspect").Invoke(genericInspector, parameters));
                return;
            }
        }
        var potentialInspectors = _inspectors.Keys
            .Where(pi =>
                pi.targetType == type &&
                pi.attributes.All(attributeType => field.GetCustomAttribute(attributeType) != null));
        if (potentialInspectors.Any())
        {
            var preferredInspector = potentialInspectors.MaxBy(i => i.attributes.Count);
            var parameters = new[] {field.Name.SplitCamelCase(), field.GetValue(obj), obj, this}
                .Concat(preferredInspector.attributes.Select<Type, object>(attributeType => field.GetCustomAttribute(attributeType)))
                .ToArray();
            field.SetValue(obj, _inspectors[preferredInspector].type.GetMethod("Inspect").Invoke(_inspectors[preferredInspector].instance, parameters));
        }
        else if (type.IsEnum)
        {
            var isflags = type.GetCustomAttributes<FlagsAttribute>().Any();
            var names = Enum.GetNames(field.FieldType);
            field.SetValue(obj,
                InspectEnum(field.Name.SplitCamelCase(), (int) field.GetValue(obj), isflags ? names.Skip(1).ToArray() : names, isflags));
        }
        else if (type.GetCustomAttribute<InspectableAttribute>() != null)
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
        else Debug.Log($"Field \"{field.Name}\" has unknown type {field.FieldType.Name}. No inspector was generated.");
    }

    public int InspectEnum(string label, int value, string[] enumOptions, bool flags)
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

    public void OnGUI()
    {
        if (entry==null)
        {
            LabelField("No Entry Selected!");
            return;
        }
        
        if(entry is INamedEntry namedEntry)
            GUILayout.Label(namedEntry.EntryName, EditorStyles.boldLabel);
        
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
            // if(GUILayout.Button("Print JSON"))
            //     Debug.Log(entry.ToJson());
            if (GUILayout.Button("Delete Entry"))
            {
                CultCache.Delete(entry);
                entry = null;
                return;
            }

            if (GUILayout.Button("Clone Entry"))
            {
                var copy = MessagePackSerializer.Deserialize<DatabaseEntry>(previousBytes);
                copy.ID = Guid.NewGuid();
                CultCache.Add(copy);
            }
        }
        
        using (var h = new HorizontalScope())
        {
            GUILayout.Label("ID");
            LabelField(dataEntry.ID.ToString());
        }

        Inspect(entry, true);

        if (EditorGUI.EndChangeCheck())
        {
            if(!previousBytes.ByteEquals(MessagePackSerializer.Serialize(entry)))
                CultCache.Add(entry);
        }
            
        EndScrollView();
    }
}
