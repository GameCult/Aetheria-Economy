[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true)]
public class AkEnumFlagAttribute : UnityEngine.PropertyAttribute
{
	public System.Type Type;

	public AkEnumFlagAttribute(System.Type type)
	{
		Type = type;
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(AkEnumFlagAttribute))]
	public class PropertyDrawer : UnityEditor.PropertyDrawer
	{
		public override void OnGUI(UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label)
		{
			UnityEditor.EditorGUI.BeginProperty(position, label, property);
			var flagAttribute = (AkEnumFlagAttribute)attribute;
			property.longValue = UnityEditor.EditorGUI.EnumFlagsField(position, new UnityEngine.GUIContent(label.text, AkUtilities.GetTooltip(property)), (System.Enum)System.Enum.ToObject(flagAttribute.Type, property.longValue)).GetHashCode();
			UnityEditor.EditorGUI.EndProperty();
		}
	}
#endif
}
