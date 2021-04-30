namespace AK.Wwise.Editor
{
	public abstract class BaseTypeDrawer : UnityEditor.PropertyDrawer
	{
		public override void OnGUI(UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label)
		{
			// Get unique control Id
			int controlId = UnityEngine.GUIUtility.GetControlID(label, UnityEngine.FocusType.Passive);
			UnityEditor.EditorGUI.BeginProperty(position, label, property);

			var wwiseObjectReference = property.FindPropertyRelative("WwiseObjectReference");
			HandleDragAndDrop(wwiseObjectReference, position);

			position = UnityEditor.EditorGUI.PrefixLabel(position, controlId, label);

			var style = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.button);
			style.alignment = UnityEngine.TextAnchor.MiddleLeft;
			style.fontStyle = UnityEngine.FontStyle.Normal;

			var componentName = GetComponentName(wwiseObjectReference);
			if (string.IsNullOrEmpty(componentName))
			{
				componentName = "No " + WwiseObjectType + " is currently selected";
				style.normal.textColor = UnityEngine.Color.red;
			}

			if (UnityEngine.GUI.Button(position, componentName, style))
			{
				new AkWwiseComponentPicker.PickerCreator
				{
					objectType = WwiseObjectType,
					wwiseObjectReference = wwiseObjectReference,
					serializedObject = property.serializedObject,
					//Current selected object
					currentWwiseObjectReference = GetWwiseObjectReference(wwiseObjectReference),
					//We're currently clicking focus windows must be the right
					pickedSourceEditorWindow = UnityEditor.EditorWindow.focusedWindow,
					//Useful to control event source
					pickedSourceControlId = controlId,
					pickerPosition = AkUtilities.GetLastRectAbsolute(position),
				};
			}

			// Check picker window close event and we're in the right drawer instance using control ID
			if (UnityEngine.Event.current.commandName == AkWwiseComponentPicker.PickerClosedEventName &&
				controlId == AkWwiseComponentPicker.GetObjectPickerControlID())
			{
				var oldValue = GetWwiseObjectReference(wwiseObjectReference);
				var newValue = AkWwiseComponentPicker.GetObjectPickerObjectReference();

				if (oldValue != newValue)
				{
					// Serialized object updating
					wwiseObjectReference.serializedObject.Update();
					SetSerializedObject(wwiseObjectReference, newValue);
					wwiseObjectReference.serializedObject.ApplyModifiedProperties();

					// Force GUI modification, to send back to the base component drawer
					UnityEngine.GUI.changed = true;
				}
			}

			UnityEditor.EditorGUI.EndProperty();
		}

		protected abstract WwiseObjectType WwiseObjectType { get; }

		protected virtual string GetComponentName(UnityEditor.SerializedProperty wwiseObjectReference)
		{
			var reference = wwiseObjectReference.objectReferenceValue as WwiseObjectReference;
			return reference ? reference.DisplayName : string.Empty;
		}

		// These are to be able use other type instead of WwiseObjectReference
		protected virtual WwiseObjectReference GetWwiseObjectReference(UnityEditor.SerializedProperty serializedProperty)
		{
			return serializedProperty.objectReferenceValue as WwiseObjectReference;
		}

		protected virtual void SetSerializedObject(UnityEditor.SerializedProperty serializedProperty, WwiseObjectReference wwiseObjectReference)
		{
			serializedProperty.objectReferenceValue = wwiseObjectReference;
		}

		private void HandleDragAndDrop(UnityEditor.SerializedProperty wwiseObjectReference, UnityEngine.Rect dropArea)
		{
			var currentEvent = UnityEngine.Event.current;
			if (!dropArea.Contains(currentEvent.mousePosition))
				return;

			if (currentEvent.type != UnityEngine.EventType.DragUpdated && currentEvent.type != UnityEngine.EventType.DragPerform)
				return;

			var reference = AkWwiseTypes.DragAndDropObjectReference;
			if (reference != null && reference.WwiseObjectType != WwiseObjectType)
				reference = null;

			UnityEditor.DragAndDrop.visualMode = reference != null ? UnityEditor.DragAndDropVisualMode.Link : UnityEditor.DragAndDropVisualMode.Rejected;

			if (currentEvent.type == UnityEngine.EventType.DragPerform)
			{
				UnityEditor.DragAndDrop.AcceptDrag();

				if (reference != null)
				{
					SetSerializedObject(wwiseObjectReference, reference);
				}

				UnityEditor.DragAndDrop.PrepareStartDrag();
				UnityEngine.GUIUtility.hotControl = 0;
			}

			currentEvent.Use();
		}
	}
}
