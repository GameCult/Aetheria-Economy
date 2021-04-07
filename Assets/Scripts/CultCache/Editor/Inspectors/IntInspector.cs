using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

public class IntInspector : BaseInspector<int>
{
    public override int Inspect(string label, int value, object parent, DatabaseInspector inspectorWindow)
    {
        using (new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return DelayedIntField(value);
        }
    }
}

public class RangedIntInspector : BaseInspector<int, InspectableRangedIntAttribute>
{
    public override int Inspect(string label, int value, object parent, DatabaseInspector inspectorWindow, InspectableRangedIntAttribute attribute)
    {
        using (new HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(width));
            return IntSlider(value, attribute.Min, attribute.Max);
        }
    }
}