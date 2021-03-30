using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

public class Float2Inspector : InspectorBase<float2>
{
    public override float2 Inspect(string label, float2 value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            GUILayout.Label("X", GUILayout.Width(labelWidth));
            value.x = DelayedFloatField(value.x);
            GUILayout.Label("Y", GUILayout.Width(labelWidth));
            value.y = DelayedFloatField(value.y);
            return value;
        }
    }
}

public class Float3Inspector : InspectorBase<float3>
{
    public override float3 Inspect(string label, float3 value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            GUILayout.Label("X", GUILayout.Width(labelWidth));
            value.x = DelayedFloatField(value.x);
            GUILayout.Label("Y", GUILayout.Width(labelWidth));
            value.y = DelayedFloatField(value.y);
            return value;
        }
    }
}

public class Int2Inspector : InspectorBase<int2>
{
    public override int2 Inspect(string label, int2 value)
    {
        using (var h = new EditorGUILayout.HorizontalScope())
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
}