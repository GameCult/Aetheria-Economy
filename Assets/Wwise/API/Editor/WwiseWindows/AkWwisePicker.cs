#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

public class AkWwisePicker : UnityEditor.EditorWindow
{
	[UnityEngine.SerializeField] UnityEditor.IMGUI.Controls.TreeViewState m_treeViewState;

	public static AkWwiseTreeView m_treeView;
	UnityEditor.IMGUI.Controls.SearchField m_SearchField;

	[UnityEditor.MenuItem("Window/Wwise Picker", false, (int)AkWwiseWindowOrder.WwisePicker)]
	public static void InitPickerWindow()
	{
		GetWindow<AkWwisePicker>("Wwise Picker", true,
		   typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow"));
	}

	public void OnEnable()
	{
		if (m_treeViewState == null)
			m_treeViewState = new UnityEditor.IMGUI.Controls.TreeViewState();

		var multiColumnHeaderState = AkWwiseTreeView.CreateDefaultMultiColumnHeaderState();
		var multiColumnHeader = new UnityEditor.IMGUI.Controls.MultiColumnHeader(multiColumnHeaderState);
		m_treeView = new AkWwiseTreeView(m_treeViewState, multiColumnHeader, AkWwiseProjectInfo.GetTreeData());
		m_treeView.SetDoubleClickFunction(PlayPauseItem);

		m_treeView.dirtyDelegate = RequestRepaint;

		m_SearchField = new UnityEditor.IMGUI.Controls.SearchField();
		m_SearchField.downOrUpArrowKeyPressed += m_treeView.SetFocusAndEnsureSelectedItem;
		m_SearchField.SetFocus();
	}

	public static void Refresh(bool ignoreIfWaapi = false)
	{
		if (AkWwiseProjectInfo.GetData().currentDataSource == AkWwiseProjectInfo.DataSourceType.WwiseAuthoring && ignoreIfWaapi)
		{
			return;
		}

		if (m_treeView != null)
		{
			m_treeView.dataSource.FetchData();
		};
	}

	private void PlayPauseItem(AkWwiseTreeViewItem item)
	{
		if (m_treeView != null && m_treeView.CheckWaapi())
			AkWaapiUtilities.TogglePlayEvent(item.objectType, item.objectGuid);
	}

	private bool isDirty;
	public void RequestRepaint()
	{
		isDirty = true;
	}

	void Update()
	{
		if (isDirty)
		{
			Repaint();
			m_treeView.Reload();
			isDirty = false;
		}

		if (AkWwiseEditorSettings.Instance.UseWaapi)
		{
			AkWwiseProjectInfo.WaapiPickerData.Update();
		}
	}

	public void OnGUI()
	{
		AkWwiseProjectInfo.DataSourceType ds;
		var buttonWidth = 150;
		using (new UnityEngine.GUILayout.HorizontalScope("box"))
		{
			ds = (AkWwiseProjectInfo.DataSourceType)UnityEditor.EditorGUILayout.EnumPopup(
				AkWwiseProjectInfo.GetData().currentDataSource, UnityEngine.GUILayout.Width(buttonWidth));
			UnityEngine.GUILayout.Space(5);

			var projectData = AkWwiseProjectInfo.GetData();

			if (ds != projectData.currentDataSource)
			{
				projectData.currentDataSource = ds;
				m_treeView.SetDataSource(AkWwiseProjectInfo.GetTreeData());
			}

			if (ds == AkWwiseProjectInfo.DataSourceType.FileSystem)
			{
				projectData.autoPopulateEnabled =
					UnityEngine.GUILayout.Toggle(projectData.autoPopulateEnabled, "Auto populate");
			}
			else
			{
				projectData.AutoSyncSelection =
					UnityEngine.GUILayout.Toggle(projectData.AutoSyncSelection, "Autosync selection");
				AkWwiseProjectInfo.WaapiPickerData.AutoSyncSelection = projectData.AutoSyncSelection;
			}

			UnityEngine.GUILayout.FlexibleSpace();

			if (UnityEngine.GUILayout.Button("Refresh Project", UnityEngine.GUILayout.Width(buttonWidth)))
			{
				if (ds == AkWwiseProjectInfo.DataSourceType.FileSystem)
				{
					AkWwiseProjectInfo.Populate();
				}
				Refresh();
			}


			if (UnityEngine.GUILayout.Button("Generate SoundBanks", UnityEngine.GUILayout.Width(buttonWidth)))
			{
				if (AkUtilities.IsSoundbankGenerationAvailable())
				{
					AkUtilities.GenerateSoundbanks();
				}
				else
				{
					UnityEngine.Debug.LogError("Access to Wwise is required to generate the SoundBanks. Please go to Edit > Project Settings... and set the Wwise Application Path found in the Wwise Editor view.");
				}
			}

			if (projectData.autoPopulateEnabled && AkUtilities.IsWwiseProjectAvailable)
				AkWwiseWWUBuilder.StartWWUWatcher();
			else
				AkWwiseWWUBuilder.StopWWUWatcher();
		}

		using (new UnityEngine.GUILayout.HorizontalScope("box"))
		{
			var search_width = System.Math.Max(position.width / 3, buttonWidth * 2);

			if (ds == AkWwiseProjectInfo.DataSourceType.FileSystem)
			{
				m_treeView.StoredSearchString = m_SearchField.OnGUI(UnityEngine.GUILayoutUtility.GetRect(search_width, 20), m_treeView.StoredSearchString);
				UnityEngine.GUILayout.FlexibleSpace();

			}

			else
			{
				m_treeView.StoredSearchString = m_SearchField.OnGUI(UnityEngine.GUILayoutUtility.GetRect(search_width, 20), m_treeView.StoredSearchString);
				UnityEngine.GUILayout.FlexibleSpace();

				var labelStyle = new UnityEngine.GUIStyle();
				labelStyle.richText = true;
				UnityEngine.GUILayout.Label(AkWaapiUtilities.GetStatusString(), labelStyle);
			}
		}

		UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);


		UnityEngine.GUILayout.FlexibleSpace();
		UnityEngine.Rect lastRect = UnityEngine.GUILayoutUtility.GetLastRect();
		m_treeView.OnGUI(new UnityEngine.Rect(lastRect.x, lastRect.y, position.width, lastRect.height));

		if (UnityEngine.GUI.changed && AkUtilities.IsWwiseProjectAvailable)
			UnityEditor.EditorUtility.SetDirty(AkWwiseProjectInfo.GetData());
	}

	static void SelectInWwisePicker(System.Guid guid)
	{
		InitPickerWindow();
		m_treeView.SelectItem(guid);
	}

	[UnityEditor.MenuItem("CONTEXT/AkBank/Select in Wwise Picker")]
	[UnityEditor.MenuItem("CONTEXT/AkAmbient/Select in Wwise Picker")]
	[UnityEditor.MenuItem("CONTEXT/AkEvent/Select in Wwise Picker")]
	[UnityEditor.MenuItem("CONTEXT/AkState/Select in Wwise Picker")]
	[UnityEditor.MenuItem("CONTEXT/AkSwitch/Select in Wwise Picker")]
	static void SelectItemInWwisePicker(UnityEditor.MenuCommand command)
	{
		AkTriggerHandler component = (AkTriggerHandler)command.context;
		try
		{
			var data = component.GetType().GetField("data");
			var guid = (data.GetValue(component) as AK.Wwise.BaseType).ObjectReference.Guid;
			SelectInWwisePicker(guid);
		}
		catch { }
	}

	[UnityEditor.MenuItem("CONTEXT/AkRoom/Select Aux Bus in Wwise Picker")]
	static void SelectAkRoomAuxBusInWwisePicker(UnityEditor.MenuCommand command)
	{
		AkRoom component = (AkRoom)command.context;
		SelectInWwisePicker(component.reverbAuxBus.ObjectReference.Guid);
	}

	[UnityEditor.MenuItem("CONTEXT/AkRoom/Select Event in Wwise Picker")]
	static void SelectAkRoomEventInWwisePicker(UnityEditor.MenuCommand command)
	{
		AkRoom component = (AkRoom)command.context;
		SelectInWwisePicker(component.roomToneEvent.ObjectReference.Guid);
	}

	[UnityEditor.MenuItem("CONTEXT/AkSurfaceReflector/Select in Wwise Picker")]
	static void SelectReflectorTextureItemInWwisePicker(UnityEditor.MenuCommand command)
	{
		AkSurfaceReflector component = (AkSurfaceReflector)command.context;
		if (component.AcousticTextures.Length >0 && component.AcousticTextures[0].ObjectReference !=null)
		{
			SelectInWwisePicker(component.AcousticTextures[0].ObjectReference.Guid);
		}
	}

	[UnityEditor.MenuItem("CONTEXT/AkEnvironment/Select in Wwise Picker")]
	static void SelectEnvironmentItemInWwisePicker(UnityEditor.MenuCommand command)
	{
		AkEnvironment component = (AkEnvironment)command.context;
		SelectInWwisePicker(component.data.ObjectReference.Guid);
	}

	[UnityEditor.MenuItem("CONTEXT/AkEarlyReflections/Select in Wwise Picker")]
	static void SelectReflectionsItemInWwisePicker(UnityEditor.MenuCommand command)
	{
		AkEarlyReflections component = (AkEarlyReflections)command.context;
		SelectInWwisePicker(component.reflectionsAuxBus.ObjectReference.Guid);
	}
}
#endif