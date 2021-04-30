#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

public class AkWwiseComponentPicker : UnityEditor.EditorWindow
{
	/// <summary>
	///  Return the last picked object
	/// </summary>
	public static WwiseObjectReference GetObjectPickerObjectReference()
	{
		return s_componentPicker != null ? s_componentPicker.m_CurrentObjectReference : default;
	}

	/// <summary>
	///   Return last control Id which opened the window
	/// </summary>
	public static int GetObjectPickerControlID()
	{
		return s_componentPicker != null ? s_componentPicker.m_ObjectSelectorId : default;
	}

	public const string PickerClosedEventName = "AkWwiseComponentPickerClosed";

	public static AkWwiseComponentPicker s_componentPicker;

	private AkWwiseTreeView m_treeView;

	private bool m_close;
	private UnityEditor.SerializedProperty m_WwiseObjectReference;
	private UnityEditor.SerializedObject m_serializedObject;
	private WwiseObjectType m_type;
	UnityEditor.IMGUI.Controls.SearchField m_SearchField;

	private WwiseObjectReference m_CurrentObjectReference;
	private UnityEditor.EditorWindow m_PickedSourceEditorWindow;
	private int m_ObjectSelectorId = 0;

	/// <summary>
	///  The window to repaint after closing the picker
	/// </summary>
	public static UnityEditor.EditorWindow LastFocusedWindow = null;

	private void Update()
	{
		//Unity sometimes generates an error when the window is closed from the OnGUI function.
		//So We close it here
		if (m_close)
		{
			Close();

			if (LastFocusedWindow)
			{
				UnityEditor.EditorApplication.delayCall += LastFocusedWindow.Repaint;
				LastFocusedWindow = null;
			}
		}
	}

	private void OnGUI()
	{
		using (new UnityEngine.GUILayout.VerticalScope())
		{
			UnityEngine.GUILayout.Space(10);
			m_treeView.StoredSearchString = m_SearchField.OnGUI(UnityEngine.GUILayoutUtility.GetRect(position.width - 60, 20), m_treeView.StoredSearchString);
			UnityEngine.GUILayout.FlexibleSpace();
			UnityEngine.Rect lastRect = UnityEngine.GUILayoutUtility.GetLastRect();

			m_treeView.OnGUI(new UnityEngine.Rect(lastRect.x, lastRect.y, position.width, lastRect.height));

			using (new UnityEngine.GUILayout.HorizontalScope("box"))
			{
				if (UnityEngine.GUILayout.Button("Ok"))
				{
					//Get the selected item
					var selectedItem = m_treeView.dataSource.Find(m_treeView.state.lastClickedID);

					SetGuid(selectedItem);
				}
				else if (UnityEngine.GUILayout.Button("Cancel"))
					m_close = true;
				else if (UnityEngine.GUILayout.Button("Reset"))
				{
					ResetGuid();
					m_close = true;
				}
			}
		}
	}

	private void SetGuid(AkWwiseTreeViewItem in_element)
	{
		if (in_element == null || m_type != in_element.objectType) return;

		m_serializedObject.Update();
		var reference = WwiseObjectReference.FindOrCreateWwiseObject(m_type, in_element.name, in_element.objectGuid);
		var groupReference = reference as WwiseGroupValueObjectReference;
		if (groupReference)
		{
			var parent = in_element.parent as AkWwiseTreeViewItem;
			groupReference.SetupGroupObjectReference(parent.name, parent.objectGuid);
		}

		m_CurrentObjectReference = reference;
		if (m_PickedSourceEditorWindow)
		{
			m_PickedSourceEditorWindow.SendEvent(UnityEditor.EditorGUIUtility.CommandEvent(PickerClosedEventName));
		}
		else
		{
			m_serializedObject.Update();
			m_WwiseObjectReference.objectReferenceValue = reference;
			m_serializedObject.ApplyModifiedProperties();
		}

		m_close = true;
	}

	private void ResetGuid()
	{
		m_CurrentObjectReference = null;
		if (m_PickedSourceEditorWindow)
		{
			m_PickedSourceEditorWindow.SendEvent(UnityEditor.EditorGUIUtility.CommandEvent(PickerClosedEventName));
		}
		else
		{
			m_serializedObject.Update();
			m_WwiseObjectReference.objectReferenceValue = null;
			m_serializedObject.ApplyModifiedProperties();

		}
	}

	public class PickerCreator
	{
		public UnityEditor.SerializedProperty wwiseObjectReference;
		public WwiseObjectType objectType;
		public UnityEngine.Rect pickerPosition;
		public UnityEditor.SerializedObject serializedObject;

		public WwiseObjectReference currentWwiseObjectReference;
		public UnityEditor.EditorWindow pickedSourceEditorWindow;
		public int pickedSourceControlId = 0;
		private int minPickerWidth = 300;

		internal PickerCreator()
		{
			UnityEditor.EditorApplication.delayCall += DelayCall;
		}

		private void DelayCall()
		{
			if (s_componentPicker != null)
				return;

			s_componentPicker = CreateInstance<AkWwiseComponentPicker>();

			//position the window below the button
			var pos = new UnityEngine.Rect(pickerPosition.x, pickerPosition.yMax, 0, 0);

			//If the window gets out of the screen, we place it on top of the button instead
			if (pickerPosition.yMax > UnityEngine.Screen.currentResolution.height / 2)
				pos.y = pickerPosition.y - UnityEngine.Screen.currentResolution.height / 2;

			//We show a drop down window which is automatically destroyed when focus is lost
			s_componentPicker.ShowAsDropDown(pos,
				new UnityEngine.Vector2(pickerPosition.width >= minPickerWidth ? pickerPosition.width : minPickerWidth,
					UnityEngine.Screen.currentResolution.height / 2));

			s_componentPicker.m_WwiseObjectReference = wwiseObjectReference;
			s_componentPicker.m_serializedObject = serializedObject;
			s_componentPicker.m_type = objectType;
			s_componentPicker.m_CurrentObjectReference = currentWwiseObjectReference;
			s_componentPicker.m_PickedSourceEditorWindow = pickedSourceEditorWindow;
			s_componentPicker.m_ObjectSelectorId = pickedSourceControlId;

			UnityEditor.IMGUI.Controls.TreeViewState treeViewState = new UnityEditor.IMGUI.Controls.TreeViewState();
			s_componentPicker.m_treeView = new AkWwiseTreeView(treeViewState, AkWwiseProjectInfo.GetTreeData(), objectType);
			s_componentPicker.m_treeView.DragDropEnabled = false;
			s_componentPicker.m_treeView.SetDoubleClickFunction(s_componentPicker.SetGuid);

			s_componentPicker.m_SearchField = new UnityEditor.IMGUI.Controls.SearchField();
			s_componentPicker.m_SearchField.downOrUpArrowKeyPressed += s_componentPicker.m_treeView.SetFocusAndEnsureSelectedItem;
			s_componentPicker.m_SearchField.SetFocus();

			var reference = currentWwiseObjectReference;
			if (reference)
			{
				s_componentPicker.m_treeView.dataSource.SelectItem(reference.Guid);
			}
		}
	}

}
#endif
