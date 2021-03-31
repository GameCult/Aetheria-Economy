using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

public class ListInspector<T> : BaseInspector<List<T>>
{
    private bool _listItemStyle;
    private static HashSet<int> _listItemFoldouts = new HashSet<int>();
    public GUIStyle ListItemStyle =>
        (_listItemStyle = !_listItemStyle) ? DatabaseListView.ListStyleEven : DatabaseListView.ListStyleOdd;
    
    public override List<T> Inspect(string label, List<T> list, object parent, DatabaseInspector inspectorWindow)
    {
        var listType = typeof(T);
        if (listType.GetCustomAttribute<InspectableAttribute>() != null)
        {
            Space();
            using (new VerticalScope(GUI.skin.box))
            {
                if (list == null)
                {
                    list = new List<T>();
                }
                else
                {
                    var sorted = true;
                    var order = int.MinValue;
                    foreach(var element in list)
                    {
                        if (element == null)
                        {
                            list.Remove(element);
                            return list;
                        }
                        var elementType = element.GetType();
                        var elementOrder = elementType.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
                        if (elementOrder < order)
                            sorted = false;
                        order = elementOrder;
                    }
                
                    if (!sorted)
                    {
                        list.Sort((x, y) => (x.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0).CompareTo(y.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0));
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
                    
                    using (new VerticalScope(ListItemStyle))
                    {
                        using (new HorizontalScope())
                        {
                            //if (listType.IsInterface)
                            if(Foldout(_listItemFoldouts.Contains(o.GetHashCode()), o.ToString(), true))
                                _listItemFoldouts.Add(o.GetHashCode());
                            else
                                _listItemFoldouts.Remove(o.GetHashCode());

                            
                            // var rect = GetControlRect(false,
                            //     GUILayout.Width(EditorGUIUtility.singleLineHeight));
                            // GUI.DrawTexture(rect, Icons.Instance.minus, ScaleMode.StretchToFill, true, 1,
                            //     LabelColor, 0, 0);
                            if (GUILayout.Button("-", GUILayout.Width((EditorGUIUtility.singleLineHeight - 3)*2), GUILayout.Height(EditorGUIUtility.singleLineHeight-3)))
                            {
                                list.Remove(o);
                                break;
                            }
                        }
                        if(_listItemFoldouts.Contains(o.GetHashCode()))
                        {
                            Space();

                            inspectorWindow.Inspect(o, true);
                        }
                    }
                    
                    if(_tinted)
                        GUI.backgroundColor = originalColor;
                }
                using (new HorizontalScope(ListItemStyle))
                {
                    GUILayout.Label($"Add new {listType.Name}", GUILayout.ExpandWidth(true));
                    // var rect = GetControlRect(false, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    // GUI.DrawTexture(rect, Icons.Instance.plus, ScaleMode.StretchToFill, true, 1, LabelColor, 0, 0);
                    if (GUILayout.Button("+", GUILayout.Width((EditorGUIUtility.singleLineHeight - 3)*2), GUILayout.Height(EditorGUIUtility.singleLineHeight-3)))
                    {
                        if (listType.IsInterface)
                        {
                            var menu = new GenericMenu();
                            foreach (var childType in listType.GetAllInterfaceClasses())
                            {
                                menu.AddItem(new GUIContent(childType.Name), false, () =>
                                {
                                    list.Add((T)Activator.CreateInstance(childType));
                                });
                            }
                            menu.ShowAsContext();
                        }
                        else if (listType.IsAbstract)
                        {
                            var menu = new GenericMenu();
                            foreach (var childType in listType.GetAllChildClasses().Where(t=>!t.IsAbstract))
                            {
                                menu.AddItem(new GUIContent(childType.Name), false, () =>
                                {
                                    list.Add((T)Activator.CreateInstance(childType));
                                });
                            }
                            menu.ShowAsContext();
                        }
                        else
                        {
                            list.Add(Activator.CreateInstance<T>());
                        }
                    }
                }
            }
        }
        else Debug.Log($"Field \"{label}\" is a list of non-Inspectable type {listType.Name}. No inspector was generated.");

        return list;
    }
}