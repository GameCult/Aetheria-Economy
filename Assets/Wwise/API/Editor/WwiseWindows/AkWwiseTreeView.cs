#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
public class AkWwiseTreeView : TreeView
{

	public enum PickerMode
	{
		FullPicker,
		ComponentPicker
	}

	private PickerMode m_pickerMode;
	private WwiseObjectType componentObjectType;

	AkWwisePickerIcons icons;
	protected AkWwiseTreeDataSource m_dataSource;
	public AkWwiseTreeDataSource dataSource { get { return m_dataSource; } }
	readonly IList<AkWwiseTreeViewItem> m_Rows = new List<AkWwiseTreeViewItem>(100);

	public event System.Action treeChanged;

	private static Dictionary<WwiseObjectType, UnityEditor.MonoScript> DragDropMonoScriptMap;
	private static Dictionary<System.Type, WwiseObjectType> ScriptTypeMap
		= new Dictionary<System.Type, WwiseObjectType>{
			{ typeof(AkAmbient), WwiseObjectType.Event },
			{ typeof(AkBank), WwiseObjectType.Soundbank },
			{ typeof(AkEnvironment), WwiseObjectType.AuxBus },
			{ typeof(AkState), WwiseObjectType.State },
			{ typeof(AkSurfaceReflector), WwiseObjectType.AcousticTexture },
			{ typeof(AkSwitch), WwiseObjectType.Switch },
		};


	public AkWwiseTreeView(TreeViewState treeViewState,
		MultiColumnHeader multiColumnHeader, AkWwiseTreeDataSource data)
		: base(treeViewState, multiColumnHeader)
	{
		m_pickerMode = PickerMode.FullPicker;
		Initialize(data);
		Reload();
	}

	public AkWwiseTreeView(TreeViewState treeViewState,
		AkWwiseTreeDataSource data, WwiseObjectType componentType)
	: base(treeViewState)

	{
		m_pickerMode = PickerMode.ComponentPicker;
		componentObjectType = componentType;
		Initialize(data);
		data.LoadComponentData(componentObjectType);
		Reload();
	}

	private void Initialize(AkWwiseTreeDataSource data)
	{

		m_dataSource = data;
		m_dataSource.TreeView = this;
		m_dataSource.modelChanged += ModelChanged;
		this.LoadExpansionStatus();

		icons = new AkWwisePickerIcons();
		icons.LoadIcons();

		DragDropEnabled = true;
		extraSpaceBeforeIconAndLabel = AkWwisePickerIcons.kIconWidth;
		StoredSearchString = "";

		if (DragDropMonoScriptMap == null)
		{
			DragDropMonoScriptMap = new Dictionary<WwiseObjectType, UnityEditor.MonoScript>();

			var scripts = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.MonoScript>();
			foreach (var script in scripts)
			{
				WwiseObjectType wwiseObjectType;
				var type = script.GetClass();
				if (type != null && ScriptTypeMap.TryGetValue(type, out wwiseObjectType))
					DragDropMonoScriptMap[wwiseObjectType] = script;
			}
		}

		UnityEditor.EditorApplication.playModeStateChanged += (UnityEditor.PlayModeStateChange playMode) =>
		{
			if (playMode == UnityEditor.PlayModeStateChange.ExitingEditMode)
				SaveExpansionStatus();
		};
		UnityEditor.EditorApplication.quitting += SaveExpansionStatus;
	}

	private bool bSearchStringChanged;
	public string m_storedSearchString;
	public string StoredSearchString
	{
		get { return m_storedSearchString; }
		set
		{
			if (m_storedSearchString != value)
			{
				if (value != string.Empty)
				{
					bSearchStringChanged = true;
					SaveExpansionStatus();
				}
				else
				{
					LoadExpansionStatus();
				}
			}
			m_storedSearchString = value;
			searchString = value;
		}
	}

	public void SaveExpansionStatus()
	{
		//Don't save exansion state when searching
		if (m_storedSearchString != string.Empty) return;
		dataSource.SaveExpansionStatus(new List<int>(state.expandedIDs));
	}

	public void LoadExpansionStatus()
	{
		state.expandedIDs = dataSource.LoadExpansionSatus();
	}

	protected override void ExpandedStateChanged()
	{
		if (this.m_storedSearchString == string.Empty)
		{
			this.m_dataSource.ScheduleRebuild();
		}
	}

	void ModelChanged()
	{
		if (treeChanged != null)
			treeChanged();

		SetDirty();
	}

	public delegate void DirtyDelegate();
	public DirtyDelegate dirtyDelegate;
	public void SetDirty()
	{
		dirtyDelegate?.Invoke();
	}

	public override void OnGUI(UnityEngine.Rect rect)
	{
		if (bSearchStringChanged)
		{
			if (!m_dataSource.isSearching)
			{
				m_dataSource.UpdateSearchResults(searchString, componentObjectType);
				bSearchStringChanged = false;
			}
		}

		base.OnGUI(rect);
	}

	protected override TreeViewItem BuildRoot()
	{ 
		return m_dataSource.CreateProjectRootItem();
	}

	public void RebuildRows()
	{
		BuildRows(new AkWwiseTreeViewItem());
	}

	protected override IList<TreeViewItem> BuildRows(
		TreeViewItem root)
	{
		m_Rows.Clear();

		var dataRoot = m_dataSource.ProjectRoot;

		if (m_pickerMode == PickerMode.ComponentPicker)
		{
			dataRoot = m_dataSource.GetComponentDataRoot(componentObjectType);
		}

		if (!string.IsNullOrEmpty(searchString))
		{
			dataRoot = m_dataSource.GetSearchResults();
		}
		AddChildrenRecursive(dataRoot, m_Rows);
		searchString = "";
		return m_Rows.Cast<TreeViewItem>().ToList();
	}


	private bool TestExpanded(AkWwiseTreeViewItem node)
	{
		if (node.children.Count > 0)
		{
			if (node.depth ==-1)
			{
				return true;
			}
			return IsExpanded(node.id);
		}
		return false;
	}

	void AddChildrenRecursive(AkWwiseTreeViewItem parent, IList<AkWwiseTreeViewItem> newRows)
	{
		if (parent == null)
		{
			return;
		}

		foreach (AkWwiseTreeViewItem child in parent.children)
		{
			var item = new AkWwiseTreeViewItem(child);
			item.parent = parent;
			item.children = child.children;
			newRows.Add(item);

			if (child.children.Count > 0)
			{
				if (TestExpanded(child))
				{
					AddChildrenRecursive(child, newRows);
				}
				else
				{
					item.children = AkWwiseTreeDataSource.CreateCollapsedChild();
				}
			}
		}
	}

	protected override IList<int> GetAncestors(int id)
	{
		return m_dataSource.GetAncestors(id);
	}

	protected override IList<int> GetDescendantsThatHaveChildren(int id)
	{
		return m_dataSource.GetDescendantsThatHaveChildren(id);
	}

	public AkWwiseTreeViewItem GetItemByGuid(System.Guid guid)
	{

		return m_dataSource.Find(m_Rows, guid);
	}

	public void SelectItem(System.Guid guid)
	{
		var item = dataSource.Find(guid);
		if (item == null && AkWwiseProjectInfo.GetData().currentDataSource == AkWwiseProjectInfo.DataSourceType.WwiseAuthoring)
		{
			m_dataSource.SelectItem(guid);
		}
		else
		{
			HighlightItem(item, true);
		}
	}

	public bool ExpandItem(System.Guid guid, bool select)
	{
		var item = GetItemByGuid(guid);
		if (item != null)
		{
			HighlightItem(item, select);
			return true;
		}

		item = m_dataSource.Find(guid);
		if (item != null)
		{
			var parent = item.parent;
			while (parent != null && GetItemByGuid((parent as AkWwiseTreeViewItem).objectGuid) == null)
			{
				parent = parent.parent;
			}
			if (parent != null)
			{
				SetExpandedRecursive(parent.id, true);
			}
		}
		return false;
	}


	public void HighlightItem(AkWwiseTreeViewItem item, bool select)
	{
		if (item != null)
		{
			FrameItem(item.id);
			if (select)
			{
				SetSelection(new List<int>() { item.id });
			}
			SetDirty();
		}
	}

	#region Mulicolumn 
	enum ObjectColumns
	{
		Name,
		Guid,
		Depth,
	}

	public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
	{
		var columns = new[]
		{
				new MultiColumnHeaderState.Column
				{
					headerContent = new UnityEngine.GUIContent("Name"),
					headerTextAlignment = UnityEngine.TextAlignment.Left,
					sortedAscending = true,
					sortingArrowAlignment = UnityEngine.TextAlignment.Center,
					width = 300,
					minWidth = 200,
					autoResize = true,
					allowToggleVisibility = false
				},
			};

		var state = new MultiColumnHeaderState(columns);
		return state;
	}


	public static MultiColumnHeaderState CreateDebug()
	{
		var columns = new[]
		{
				new MultiColumnHeaderState.Column
				{
					headerContent = new UnityEngine.GUIContent("Name"),
					headerTextAlignment = UnityEngine.TextAlignment.Left,
					sortedAscending = true,
					sortingArrowAlignment = UnityEngine.TextAlignment.Center,
					width = 300,
					minWidth = 200,
					autoResize = true,
					allowToggleVisibility = false
				},
				new MultiColumnHeaderState.Column
				{
					headerContent = new UnityEngine.GUIContent("Guid"),
					headerTextAlignment = UnityEngine.TextAlignment.Right,
					sortedAscending = true,
					sortingArrowAlignment = UnityEngine.TextAlignment.Left,
					width = 200,
					minWidth = 60,
					autoResize = true
				},
				new MultiColumnHeaderState.Column
				{
					headerContent = new UnityEngine.GUIContent("depth"),
					headerTextAlignment = UnityEngine.TextAlignment.Right,
					sortedAscending = true,
					sortingArrowAlignment = UnityEngine.TextAlignment.Left,
					width = 200,
					minWidth = 60,
					autoResize = true
				},
			};

		var state = new MultiColumnHeaderState(columns);
		return state;
	}

	#endregion

	#region Search 

	protected void SearchStringChanged(string lastSearch, string newSearch)
	{

	}

	#endregion

	#region Drawing
	protected override void AfterRowsGUI()
	{
		base.AfterRowsGUI();
		this.searchString = StoredSearchString;
	}

	//check here to see if multicolumn or not
	protected override void RowGUI(RowGUIArgs args)
	{
		var evt = UnityEngine.Event.current;
		var item = (AkWwiseTreeViewItem)args.item;
		if (m_pickerMode == PickerMode.ComponentPicker)
		{
			CellGUI(args.rowRect, item, ObjectColumns.Name, ref args);
		}
		else
		{
			for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
			{
				CellGUI(args.GetCellRect(i), item, (ObjectColumns)args.GetColumn(i), ref args);
			}
		}
	}

	void CellGUI(UnityEngine.Rect cellRect, AkWwiseTreeViewItem item, ObjectColumns column, ref RowGUIArgs args)
	{
		// Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
		CenterRectUsingSingleLineHeight(ref cellRect);

		switch (column)
		{
			case ObjectColumns.Name:
				{
					UnityEngine.Rect iconRect = new UnityEngine.Rect(cellRect);
					iconRect.x += GetContentIndent(item);
					iconRect.width = AkWwisePickerIcons.kIconWidth;
					UnityEngine.GUI.DrawTexture(iconRect, icons.GetIcon(item.objectType), UnityEngine.ScaleMode.ScaleToFit);
					//// Default icon and label
					args.rowRect = cellRect;
					base.RowGUI(args);
				}
				break;
			case ObjectColumns.Guid:
				{
					UnityEngine.GUI.Label(cellRect, item.objectGuid.ToString());
				}
				break;
			case ObjectColumns.Depth:
				{
					UnityEngine.GUI.Label(cellRect, item.depth.ToString());
				}
				break;
		}
	}

	public void SetExpandedUpwardsRecursive(TreeViewItem item)
	{
		if (item == null)
		{
			return;
		}
		SetExpanded(item.id, true);
		SetExpandedUpwardsRecursive(item.parent);
	}

	public AkWwiseTreeViewItem Find(int id)
	{
		var result = this.m_Rows.FirstOrDefault(element => element.id == id);
		return result as AkWwiseTreeViewItem;
	}

	#endregion

	#region click and drag/drop
	protected override bool CanMultiSelect(TreeViewItem item)
	{
		return false;
	}

	protected override void SelectionChanged(IList<int> selectedIds)
	{
		dataSource.ItemSelected(Find(selectedIds.Last()));
		base.SelectionChanged(selectedIds);
	}

	public bool CheckWaapi()
	{
		return AkWwiseEditorSettings.Instance.UseWaapi && AkWaapiUtilities.IsConnected() &&
			AkWwiseProjectInfo.GetData().currentDataSource == AkWwiseProjectInfo.DataSourceType.WwiseAuthoring;
	}

	protected override void ContextClickedItem(int id)
	{
		UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu();
		var item = Find(id);
		if (CheckWaapi())
		{
			if (CanPlay(item))
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Play \u2215 Stop _SPACE"), false,
					() => AkWaapiUtilities.TogglePlayEvent(item.objectType, item.objectGuid));
			else
				menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Play \u2215 Stop _Space"));

			menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Stop All"), false,
					() => AkWaapiUtilities.StopAllTransports());

			menu.AddSeparator("");

			if (CanRenameWithLog(item, false))
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Rename _F2"), false,
					() => BeginRename(item));
			else
				menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Rename"));

			if (CanDelete(item, false))
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Delete _Delete"), false,
					() => AkWaapiUtilities.Delete(item.objectGuid));
			else
				menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Delete"));

			menu.AddSeparator("");
			if (item.objectType == WwiseObjectType.Soundbank)
			{
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Open Folder/WorkUnit #O"), false,
					() => AkWaapiUtilities.OpenWorkUnitInExplorer(item.objectGuid));
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Open Folder/SoundBank "), false,
					() => AkWaapiUtilities.OpenSoundBankInExplorer(item.objectGuid));
			}
			else
			{
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Open Containing Folder #O"), false,
					() => AkWaapiUtilities.OpenWorkUnitInExplorer(item.objectGuid));
			}

			menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Find in Project Explorer #F"), false,
				() => m_dataSource.SelectObjectInAuthoring(item.objectGuid));

		}
		else
		{
			if (AkWwiseProjectInfo.GetData().currentDataSource == AkWwiseProjectInfo.DataSourceType.WwiseAuthoring)
			{
				menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Wwise Connection Settings"), false,
					OpenSettings);
				menu.AddSeparator("");
			}

			menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Play \u2215 Stop"));
			menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Stop all"));
			menu.AddSeparator("");
			menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Rename"));
			menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Delete"));
			menu.AddSeparator("");
			menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Open Containing Folder"));
			menu.AddDisabledItem(UnityEditor.EditorGUIUtility.TrTextContent("Find in Project Explorer"));
		}

		menu.AddItem(UnityEditor.EditorGUIUtility.TrTextContent("Find References in Scene #R"), false,
			 () => FindReferencesInScene(item));

		menu.ShowAsContext();
	}

	protected void OpenSettings()
	{
		UnityEditor.SettingsService.OpenProjectSettings("Project/Wwise Editor");
	}

	protected override void KeyEvent()
	{
		var selected = GetSelection();
		if (selected.Count == 0)
		{
			return;
		}
		var item = Find(GetSelection()[0]);
		if (UnityEngine.Event.current.type == UnityEngine.EventType.KeyDown)
		{
			switch (UnityEngine.Event.current.keyCode)
			{
				case UnityEngine.KeyCode.KeypadEnter:
					DoubleClickedItem(item.id);
					UnityEngine.Event.current.Use();
					break;
				case UnityEngine.KeyCode.Space:
					if (CanPlay(item))
						AkWaapiUtilities.TogglePlayEvent(item.objectType, item.objectGuid);
					UnityEngine.Event.current.Use();
					break;
				case UnityEngine.KeyCode.Delete:
					if (CanDelete(item))
						AkWaapiUtilities.Delete(item.objectGuid);
					UnityEngine.Event.current.Use();
					break;
				case UnityEngine.KeyCode.F2:
					if (CanRename(item))
						BeginRename(item);
					UnityEngine.Event.current.Use();
					break;
				case UnityEngine.KeyCode.O:
					if (UnityEngine.Event.current.shift)
					{
						if (CanOpen(item))
							AkWaapiUtilities.OpenWorkUnitInExplorer(item.objectGuid);
						UnityEngine.Event.current.Use();
					}
					break;
				case UnityEngine.KeyCode.F:
					if (UnityEngine.Event.current.shift)
					{
						if (CanSelect(item))
							m_dataSource.SelectObjectInAuthoring(item.objectGuid);
						UnityEngine.Event.current.Use();
					}
					break;
				case UnityEngine.KeyCode.R:
					if (UnityEngine.Event.current.shift)
					{
						FindReferencesInScene(item);
						UnityEngine.Event.current.Use();
					}
					break;
			}
		}
	}

	internal static void FindReferencesInScene(AkWwiseTreeViewItem item)
	{
		var reference = WwiseObjectReference.FindWwiseObject(item.objectType, item.objectGuid);
		var path = UnityEditor.AssetDatabase.GetAssetPath(reference);

		if (path.IndexOf(' ') != -1)
			path = '"' + path + '"';

		if (path == string.Empty)
		{
			UnityEngine.Debug.Log($"No references to {item.displayName} in scene.");
			return;
		}

#if !UNITY_2019_1_OR_NEWER
		//drop "Assets" part of path
		path = string.Join("/", path.Split('/').Skip(1));
#endif

		var searchFilter = "ref:" + path;

		System.Type type = typeof(UnityEditor.SearchableEditorWindow);
		System.Reflection.FieldInfo info = type.GetField("searchableWindows",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		var searchableWindows = info.GetValue(null) as List<UnityEditor.SearchableEditorWindow>;

		foreach (UnityEditor.SearchableEditorWindow sw in searchableWindows)
		{
			info = type.GetField("m_HierarchyType",
				System.Reflection.BindingFlags.NonPublic);
			if (sw.GetType().ToString() == "UnityEditor.SceneHierarchyWindow")
			{
				if (sw.GetType().ToString() == "UnityEditor.SceneHierarchyWindow")
				{
					System.Reflection.MethodInfo setSearchFilter = typeof(UnityEditor.SearchableEditorWindow).GetMethod(
						"SetSearchFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					object[] parameters = new object[] { searchFilter, 0, false, false };

					setSearchFilter.Invoke(sw, parameters);
					sw.Repaint();
				}
			}
		}
	}

	protected override void RenameEnded(RenameEndedArgs args)
	{
		var item = Find(args.itemID);

		if (ValidateNameChange(item, args.newName))
		{
			var name = args.newName.Replace(" ", "_");
			AkWaapiUtilities.Rename(item.objectGuid, name);
			item.displayName = args.newName;
		}
	}
	protected override bool CanRename(TreeViewItem item)
	{
		return CanRenameWithLog(item, true);
	}

	protected bool CanRenameWithLog(TreeViewItem item, bool log)
	{
		if (!CheckWaapi()) return false;

		var wwiseItem = (AkWwiseTreeViewItem)item;
		if (item == null)
		{
			if (log) UnityEngine.Debug.LogWarning("Tree item no longer exists");
			return false;
		}

		if ((wwiseItem.objectType == WwiseObjectType.PhysicalFolder) || (wwiseItem.objectType == WwiseObjectType.WorkUnit))
		{
			if (log) UnityEngine.Debug.LogWarning("You can't change the name of a PhysicalFolder/WorkUnit");
			return false;
		}

		if (item.parent == null)
		{
			if (log) UnityEngine.Debug.LogWarning("A root tree item can not be renamed");
			return false;
		}

		return true;
	}

	protected bool CanPlay(TreeViewItem item)
	{
		if (!CheckWaapi()) return false;

		var wwiseItem = (AkWwiseTreeViewItem)item;
		if (wwiseItem.objectType == WwiseObjectType.Event) return true;

		return false;
	}
	protected bool CanDelete(TreeViewItem item, bool log = true)
	{
		if (!CheckWaapi()) return false;

		var wwiseItem = (AkWwiseTreeViewItem)item;

		if ((wwiseItem.objectType == WwiseObjectType.PhysicalFolder) || (wwiseItem.objectType == WwiseObjectType.WorkUnit)
			|| wwiseItem.WwiseTypeInChildren(WwiseObjectType.WorkUnit))
		{
			if (log) UnityEngine.Debug.LogWarning("You can't delete a PhysicalFolder/WorkUnit from within Unity");
			return false;
		}

		return true;
	}

	protected bool CanSelect(TreeViewItem item)
	{
		if (!CheckWaapi()) return false;
		return true;
	}

	protected bool CanOpen(TreeViewItem item)
	{
		if (!CheckWaapi()) return false;
		return true;
	}

	const int MAX_NAME_LENGTH = 1024;
	bool ValidateNameChange(AkWwiseTreeViewItem item, string newName)
	{
		if (item == null)
		{
			UnityEngine.Debug.LogWarning("Tree item no longer exists");
			return false;
		}

		if (newName.Trim() == System.String.Empty)
		{
			UnityEngine.Debug.LogWarning("Names cannot be left blank");
			return false;
		}

		if (newName.Trim().Length >= MAX_NAME_LENGTH)
		{
			UnityEngine.Debug.LogWarning($"Names must be less than {MAX_NAME_LENGTH} characters long.");
			return false;
		}

		// If the new name is the same as the old name, consider this to be unchanged
		if (item.displayName == newName)
		{
			return false;
		}

		if (newName.Contains('/') || newName.Contains('\\'))
		{
			UnityEngine.Debug.LogWarning("Item names cannot contain / or \\.");
			return false;
		}

		// Validate that an item with this name doesn't exist already
		if (item.parent.children.Find((i) => i.displayName == newName) != null)
		{
			UnityEngine.Debug.LogWarning("An item with this name already exists at this level");
			return false;
		}

		return true;
	}

	public delegate void DoubleClickFunctionDelegate(AkWwiseTreeViewItem element);

	private DoubleClickFunctionDelegate doubleClickExternalFunction;
	public void SetDoubleClickFunction(DoubleClickFunctionDelegate f)
	{
		doubleClickExternalFunction = f;
	}

	protected override void DoubleClickedItem(int id)
	{
		base.DoubleClickedItem(id);
		var doubleClickedElement = m_dataSource.Find(id);
		doubleClickExternalFunction?.Invoke(doubleClickedElement);
	}

	public bool DragDropEnabled;
	protected override bool CanStartDrag(CanStartDragArgs args)
	{
		return DragDropEnabled;
	}

	protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
	{
		UnityEditor.DragAndDrop.PrepareStartDrag();

		var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
		var draggedItem = draggedRows[0] as AkWwiseTreeViewItem;
		if (draggedItem.objectGuid == System.Guid.Empty ||
			draggedItem.objectType == WwiseObjectType.Bus ||
			draggedItem.objectType == WwiseObjectType.PhysicalFolder ||
			draggedItem.objectType == WwiseObjectType.Folder ||
			draggedItem.objectType == WwiseObjectType.WorkUnit ||
			draggedItem.objectType == WwiseObjectType.Project ||
			draggedItem.objectType == WwiseObjectType.StateGroup ||
			draggedItem.objectType == WwiseObjectType.SwitchGroup)
			return;


		var reference = WwiseObjectReference.FindOrCreateWwiseObject(draggedItem.objectType, draggedItem.name, draggedItem.objectGuid);
		if (!reference)
			return;

		var groupReference = reference as WwiseGroupValueObjectReference;
		if (groupReference)
		{
			var parent = draggedItem.parent as AkWwiseTreeViewItem;
			groupReference.SetupGroupObjectReference(parent.name, parent.objectGuid);
		}

		UnityEditor.MonoScript script;
		if (DragDropMonoScriptMap.TryGetValue(reference.WwiseObjectType, out script))
		{
			UnityEngine.GUIUtility.hotControl = 0;
			UnityEditor.DragAndDrop.PrepareStartDrag();
			UnityEditor.DragAndDrop.objectReferences = new UnityEngine.Object[] { script };
			AkWwiseTypes.DragAndDropObjectReference = reference;
			UnityEditor.DragAndDrop.StartDrag("Dragging an AkObject");
		}
	}

	public void SetDataSource(AkWwiseTreeDataSource datasource)
	{
		if (m_dataSource != null)
		{
			m_dataSource.modelChanged -= this.ModelChanged;
			m_dataSource.TreeView = null;
		}
		m_dataSource = datasource;
		m_dataSource.modelChanged += this.ModelChanged;
		m_dataSource.TreeView = this;
		m_dataSource.FetchData();
	}

	~AkWwiseTreeView()
	{
		if (m_pickerMode != PickerMode.ComponentPicker && StoredSearchString == System.String.Empty)
		{
			SaveExpansionStatus();
		}
	}

#endregion
}


#region Icons
public class AkWwisePickerIcons
{
	public const float kIconWidth = 18f;

	private UnityEngine.Texture2D m_textureWwiseAcousticTextureIcon;
	private UnityEngine.Texture2D m_textureWwiseAuxBusIcon;
	private UnityEngine.Texture2D m_textureWwiseBusIcon;
	private UnityEngine.Texture2D m_textureWwiseEventIcon;
	private UnityEngine.Texture2D m_textureWwiseFolderIcon;
	private UnityEngine.Texture2D m_textureWwiseGameParameterIcon;
	private UnityEngine.Texture2D m_textureWwisePhysicalFolderIcon;
	private UnityEngine.Texture2D m_textureWwiseProjectIcon;
	private UnityEngine.Texture2D m_textureWwiseSoundbankIcon;
	private UnityEngine.Texture2D m_textureWwiseStateIcon;
	private UnityEngine.Texture2D m_textureWwiseStateGroupIcon;
	private UnityEngine.Texture2D m_textureWwiseSwitchIcon;
	private UnityEngine.Texture2D m_textureWwiseSwitchGroupIcon;
	private UnityEngine.Texture2D m_textureWwiseWorkUnitIcon;

	protected UnityEngine.Texture2D GetTexture(string texturePath)
	{
		try
		{
			return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texturePath);
		}
		catch (System.Exception ex)
		{
			UnityEngine.Debug.LogError(string.Format("WwiseUnity: Failed to find local texture: {0}", ex));
			return null;
		}
	}

	public void LoadIcons()
	{
		var tempWwisePath = "Assets/Wwise/API/Editor/WwiseWindows/TreeViewIcons/";

		m_textureWwiseAcousticTextureIcon = GetTexture(tempWwisePath + "acoustictexture_nor.png");
		m_textureWwiseAuxBusIcon = GetTexture(tempWwisePath + "auxbus_nor.png");
		m_textureWwiseBusIcon = GetTexture(tempWwisePath + "bus_nor.png");
		m_textureWwiseEventIcon = GetTexture(tempWwisePath + "event_nor.png");
		m_textureWwiseFolderIcon = GetTexture(tempWwisePath + "folder_nor.png");
		m_textureWwiseGameParameterIcon = GetTexture(tempWwisePath + "gameparameter_nor.png");
		m_textureWwisePhysicalFolderIcon = GetTexture(tempWwisePath + "physical_folder_nor.png");
		m_textureWwiseProjectIcon = GetTexture(tempWwisePath + "wproj.png");
		m_textureWwiseSoundbankIcon = GetTexture(tempWwisePath + "soundbank_nor.png");
		m_textureWwiseStateIcon = GetTexture(tempWwisePath + "state_nor.png");
		m_textureWwiseStateGroupIcon = GetTexture(tempWwisePath + "stategroup_nor.png");
		m_textureWwiseSwitchIcon = GetTexture(tempWwisePath + "switch_nor.png");
		m_textureWwiseSwitchGroupIcon = GetTexture(tempWwisePath + "switchgroup_nor.png");
		m_textureWwiseWorkUnitIcon = GetTexture(tempWwisePath + "workunit_nor.png");
	}

	public UnityEngine.Texture2D GetIcon(WwiseObjectType type)
	{
		switch (type)
		{
			case WwiseObjectType.AcousticTexture:
				return m_textureWwiseAcousticTextureIcon;
			case WwiseObjectType.AuxBus:
				return m_textureWwiseAuxBusIcon;
			case WwiseObjectType.Bus:
				return m_textureWwiseBusIcon;
			case WwiseObjectType.Event:
				return m_textureWwiseEventIcon;
			case WwiseObjectType.Folder:
				return m_textureWwiseFolderIcon;
			case WwiseObjectType.GameParameter:
				return m_textureWwiseGameParameterIcon;
			case WwiseObjectType.PhysicalFolder:
				return m_textureWwisePhysicalFolderIcon;
			case WwiseObjectType.Project:
				return m_textureWwiseProjectIcon;
			case WwiseObjectType.Soundbank:
				return m_textureWwiseSoundbankIcon;
			case WwiseObjectType.State:
				return m_textureWwiseStateIcon;
			case WwiseObjectType.StateGroup:
				return m_textureWwiseStateGroupIcon;
			case WwiseObjectType.Switch:
				return m_textureWwiseSwitchIcon;
			case WwiseObjectType.SwitchGroup:
				return m_textureWwiseSwitchGroupIcon;
			case WwiseObjectType.WorkUnit:
				return m_textureWwiseWorkUnitIcon;
			default:
				return m_textureWwisePhysicalFolderIcon;
		}
	}
}
#endregion
#endif
