#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using UnityEditor.IMGUI.Controls;

/// <summary>
/// This class communicates with Wwise Authoring via AkWaapiUtilities to keep track of the Wwise object hierarchy in the project.
/// This hierarchy information is stored in a tree structure and is used by the Wwise Picker when it is in WAAPI mode. 
/// Changes to the project are received via WAAPI subscriptions.
/// </summary>
public class AkWwiseTreeWAAPIDataSource : AkWwiseTreeDataSource
{

	private System.Timers.Timer selectTimer;
	private System.Timers.Timer searchTimer;

	private ReturnOptions waapiWwiseObjectOptions = 
		new ReturnOptions(new string[] { "id", "name", "type", "childrenCount", "path", "workunitType", "parent" });


	public bool AutoSyncSelection;

	public AkWwiseTreeWAAPIDataSource() : base()
	{
		Connect();

		selectTimer = new System.Timers.Timer();
		selectTimer.Interval = 200;
		selectTimer.AutoReset = false;
		selectTimer.Elapsed += FireSelect;

		searchTimer = new System.Timers.Timer();
		searchTimer.Interval = 200;
		searchTimer.AutoReset = false;
		searchTimer.Elapsed += FireSearch;
	}

	public override void FetchData()
	{
		Data.Clear();
		m_MaxID = 0;
		ProjectRoot = CreateProjectRootItem();

		foreach (var type in FolderNames.Keys)
		{
			AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
			{
				AddBaseFolder(AkWaapiUtilities.ParseObjectInfo(items), type);
			};
			AkWaapiUtilities.GetWwiseObjectAndDescendants(FolderNames[type], waapiWwiseObjectOptions, 2, callback);
		}
		Changed();
	}

	public override AkWwiseTreeViewItem GetComponentDataRoot(WwiseObjectType objectType)
	{
		var tempProjectRoot = new AkWwiseTreeViewItem(ProjectRoot);
		if (!wwiseObjectFolders.ContainsKey(objectType))
		{
			return tempProjectRoot;
		}

		tempProjectRoot.AddWwiseItemChild(wwiseObjectFolders[objectType]);
		return tempProjectRoot;
	}

	WwiseObjectType componentObjectType;
	public override void LoadComponentData(WwiseObjectType objectType)
	{
		componentObjectType = objectType;
		LoadComponentDataDelayed();
	}

	public void LoadComponentDataDelayed()
	{
		//Delay call until data has been fetched
		if (!wwiseObjectFolders.ContainsKey(componentObjectType))
		{
			UnityEditor.EditorApplication.delayCall += LoadComponentDataDelayed;
		}
		else
		{
			AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
			{
				AddItems(AkWaapiUtilities.ParseObjectInfo(items));
			};
			AkWaapiUtilities.GetWwiseObjectAndDescendants(wwiseObjectFolders[componentObjectType].objectGuid,
				waapiWwiseObjectOptions, -1, callback);
		}
	}


	private string searchString;
	private WwiseObjectType searchObjectTypeFilter;
	public override void UpdateSearchResults(string searchFilter, WwiseObjectType objectType = WwiseObjectType.None)
	{
		searchTimer.Stop();
		searchString = searchFilter;
		searchObjectTypeFilter = objectType;
		searchTimer.Enabled = true;
		searchTimer.Start();
	}

	private void FireSearch(object sender, System.Timers.ElapsedEventArgs e)
	{
		if (SearchRoot == null)
		{
			SearchRoot = new AkWwiseTreeViewItem(ProjectRoot);
		}

		SearchRoot.children.Clear();
		SearchItems = new List<AkWwiseTreeViewItem>(new[] { SearchRoot });
		TreeUtility.TreeToList(SearchRoot, SearchItems);
		AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
		{
			AddSearchResults(AkWaapiUtilities.ParseObjectInfo(items));
		};
		AkWaapiUtilities.Search(searchString, searchObjectTypeFilter, waapiWwiseObjectOptions, callback);
	}

	public override AkWwiseTreeViewItem GetSearchResults()
	{
		if (SearchRoot == null)
		{
			SearchRoot = new AkWwiseTreeViewItem(ProjectRoot);
		}

		return SearchRoot;
	}

	public void AddSearchResults(IEnumerable<WwiseObjectInfo> matchList)
	{
		try
		{
			foreach (var info in matchList)
			{
				if (!FilterPath(info.path))
				{
					continue;
				}

				var match = Find(SearchItems, info.objectGUID);
				if (match != null)
				{
					continue;
				}

				var matchItem = Find(info.objectGUID);
				if (matchItem == null)
				{
					AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
					{
						AddItemWithAncestorsToSearch(AkWaapiUtilities.ParseObjectInfo(items));
					};
					AkWaapiUtilities.GetWwiseObjectAndAncestors(info.objectGUID, waapiWwiseObjectOptions, callback);
					continue;
				}

				AddItemToSearch(matchItem);
				treeviewCommandQueue.Enqueue(new TreeViewCommand(() => Expand(matchItem.objectGuid, false)));
			}
		}
		catch (System.Exception e)
		{
			UnityEngine.Debug.LogError("Search died");
			UnityEngine.Debug.LogError(e.Message);
		}
	}

	private void AddItemToSearch(AkWwiseTreeViewItem sourceItem)
	{
		var matchItem = new AkWwiseTreeViewItem(sourceItem);
		SearchItems.Add(matchItem);

		var sourceParent = sourceItem.parent as AkWwiseTreeViewItem;
		var parentCopy = Find(SearchItems, sourceParent.objectGuid);
		if (parentCopy != null)
		{
			parentCopy.AddWwiseItemChild(matchItem);
			return;
		}
		else
		{
			parentCopy = new AkWwiseTreeViewItem(sourceParent);
			parentCopy.AddWwiseItemChild(matchItem);
			SearchItems.Add(parentCopy);

			var nextParent = sourceParent.parent as AkWwiseTreeViewItem;
			while (nextParent != null)
			{
				var parentInSearchItems = Find(SearchItems, nextParent.objectGuid);
				if (parentInSearchItems != null)
				{
					parentInSearchItems.AddWwiseItemChild(parentCopy);
					break;
				}
				parentInSearchItems = new AkWwiseTreeViewItem(nextParent);
				parentInSearchItems.AddWwiseItemChild(parentCopy);
				SearchItems.Add(parentInSearchItems);
				parentCopy = parentInSearchItems;
				nextParent = nextParent.parent as AkWwiseTreeViewItem;
			}
		}
	}

	public void AddItemWithAncestors(List<WwiseObjectInfo> infoItems, bool selectAfterCreated = false)
	{
		var parent = ProjectRoot;
		//Items obtained from the WAAPI call are sorted by path so we can simply iterate over them
		foreach (var infoItem in infoItems)
		{
			var newItem = Find(infoItem.objectGUID);
			if (newItem == null)
			{
				newItem = new AkWwiseTreeViewItem(infoItem, GenerateUniqueID(), parent.depth + 1);
				Data.Add(newItem);
				parent.AddWwiseItemChild(newItem);
			}

			if (!CheckIfFullyLoaded(parent))
			{
				System.Guid guid = new System.Guid(parent.objectGuid.ToString());
				AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
				{
					UpdateParentWithLoadedChildren(guid, AkWaapiUtilities.ParseObjectInfo(items));
				};
				AkWaapiUtilities.GetChildren(parent.objectGuid, waapiWwiseObjectOptions, callback);
			}
			parent = newItem;
		}

		if (selectAfterCreated)
		{
			treeviewCommandQueue.Enqueue(new TreeViewCommand(() => Expand(parent.objectGuid, true)));
		}
	}

	public void AddItemWithAncestorsToSearch(List<WwiseObjectInfo> infoItems)
	{
		AddItemWithAncestors(infoItems, false);
		var item = Find(infoItems.Last().objectGUID);
		AddItemToSearch(item);
		treeviewCommandQueue.Enqueue(new TreeViewCommand(() => Expand(item.objectGuid, false)));
	}

	public void AddItems(List<WwiseObjectInfo> infoItems)
	{
		foreach (var infoItem in infoItems)
		{
			if (infoItem.type == WwiseObjectType.None)
			{
				continue;
			}

			var tParent = Find(infoItem.parentID);
			if (tParent == null || tParent == ProjectRoot)
			{
				tParent = ProjectRoot;
			}

			var tChild = Find(infoItem.objectGUID);
			if (tChild == null)
			{
				tChild = new AkWwiseTreeViewItem(infoItem, GenerateUniqueID(), tParent.depth + 1);
				Data.Add(tChild);
				tParent.AddWwiseItemChild(tChild);
			}
		}
	}

	public void AddBaseFolder(List<WwiseObjectInfo> infoItems, WwiseObjectType oType)
	{
		if (infoItems != null && infoItems.Count > 0)
		{
			AddItems(infoItems);
			var folder = Find(infoItems[0].objectGUID);
			wwiseObjectFolders[oType] = folder;
		}
	}

	public void UpdateParentWithLoadedChildren(System.Guid parentGuid, List<WwiseObjectInfo> children)
	{
		if (children == null)
		{
			return;
		}

		var parent = Find(parentGuid);
		if (parent == null)
		{
			return;
		}
		parent.children.Remove(null);

		parent.numChildren = children.Count;
		if (parent.children.Count > children.Count)
		{
			parent.children.Clear();
		}

		foreach (var child in children)
		{
			if (parent.children.Any(c => ((AkWwiseTreeViewItem)c).objectGuid == child.objectGUID))
				continue;
			else
				parent.AddWwiseItemChild(new AkWwiseTreeViewItem(child, GenerateUniqueID(), parent.depth + 1));
		}
	}

	public void SetChildren(System.Guid parentGuid, List<WwiseObjectInfo> children)
	{
		if (children == null)
		{
			return;
		}

		var parent = Find(parentGuid);
		parent.children.Clear();
		parent.children.Remove(null);

		parent.numChildren = children.Count;

		foreach (var child in children)
		{
			parent.AddWwiseItemChild(new AkWwiseTreeViewItem(child, GenerateUniqueID(), parent.depth + 1));
		}
	}

	bool CheckIfFullyLoaded(AkWwiseTreeViewItem item)
	{
		if (item == ProjectRoot)
		{
			return true;
		}
		if (item.objectType == WwiseObjectType.Event)
		{
			return true;
		}
		if (item.numChildren != item.children.Count)
		{
			return false;
		}
		if (item.children.Contains(null))
		{
			return false;
		}
		return true;
	}

	private ReadOnlyDictionary<WwiseObjectType, string> FolderNames = new ReadOnlyDictionary<WwiseObjectType, string>(new Dictionary<WwiseObjectType, string>()
	{
		{ WwiseObjectType.AuxBus ,  @"\Master-Mixer Hierarchy" },
		{ WwiseObjectType.Event ,  @"\Events" },
		{ WwiseObjectType.State, @"\States"},
		{ WwiseObjectType.StateGroup, @"\States"},
		{ WwiseObjectType.Soundbank, @"\SoundBanks"},
		{ WwiseObjectType.Switch, @"\Switches"},
		{ WwiseObjectType.SwitchGroup, @"\Switches"},
		{ WwiseObjectType.AcousticTexture, @"\Virtual Acoustics" },
		{ WwiseObjectType.GameParameter, @"\Game Parameters" },
	 });


	static List<AkWaapiUtilities.SubscriptionInfo> subscriptions = new List<AkWaapiUtilities.SubscriptionInfo>();

	/// <summary>
	/// Subscribes to nameChanged, childAdded, childRemoved, and selectionChanged WAAPI events in order to keep the picker in sync with the Wwise project explorer.
	/// </summary>
	public void SubscribeTopics()
	{
		AkWaapiUtilities.Subscribe(ak.wwise.core.@object.nameChanged, OnWaapiRenamed, SubscriptionHandshake);
		AkWaapiUtilities.Subscribe(ak.wwise.core.@object.childAdded, OnWaapiChildAdded, SubscriptionHandshake);
		AkWaapiUtilities.Subscribe(ak.wwise.core.@object.childRemoved, OnWaapiChildRemoved, SubscriptionHandshake);
		AkWaapiUtilities.Subscribe(ak.wwise.ui.selectionChanged, OnWwiseSelectionChanged, SubscriptionHandshake);
	}

	public void SubscriptionHandshake(AkWaapiUtilities.SubscriptionInfo sub)
	{
		subscriptions.Add(sub);
	}

	/// <summary>
	/// Unsubscribes from currently active subscriptions.
	/// </summary>
	void UnsubscribeTopics()
	{
		var tSubs = subscriptions;
		foreach (var sub in tSubs)
		{
			if (sub.SubscriptionId != 0)
			{
				AkWaapiUtilities.Unsubscribe(sub.SubscriptionId);
			}
		}
		subscriptions.Clear();
	}

	void OnWaapiRenamed(string json)
	{
		var renamedItem = AkWaapiUtilities.ParseRenameObject(json);
		treeviewCommandQueue.Enqueue(new TreeViewCommand(() => Rename(renamedItem.objectInfo.objectGUID, renamedItem.newName)));
	}

	void OnWaapiChildAdded(string json)
	{
		var added = AkWaapiUtilities.ParseChildAddedOrRemoved(json);

		if (added.childInfo.type == WwiseObjectType.None)
			return;

		var parent = Find(added.parentInfo.objectGUID);

		// New object created, but parent is not loaded yet, so we can ignore it
		if (parent == null)
		{
			return;
		}

		var child = Find(added.childInfo.objectGUID);
		if (child == null)
		{
			child = new AkWwiseTreeViewItem(added.childInfo, GenerateUniqueID(), parent.depth + 1);
		}
		else
		{
			child.numChildren = added.childInfo.childrenCount;
			child.displayName = added.childInfo.name;
		}

		parent.AddWwiseItemChild(child);
		Data.Add(child);
		parent.numChildren = added.parentInfo.childrenCount;
		child.depth = parent.depth + 1;

		if (!CheckIfFullyLoaded(parent))
		{
			AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
			{
				UpdateParentWithLoadedChildren(parent.objectGuid, AkWaapiUtilities.ParseObjectInfo(items));
			};
			AkWaapiUtilities.GetChildren(parent.objectGuid, waapiWwiseObjectOptions, callback);
		}

		if (!CheckIfFullyLoaded(child))
		{
			AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
			{
				UpdateParentWithLoadedChildren(child.objectGuid, AkWaapiUtilities.ParseObjectInfo(items));
			};
			AkWaapiUtilities.GetChildren(child.objectGuid, waapiWwiseObjectOptions, callback);
		}
		ScheduleRebuild();
	}

	void OnWaapiChildRemoved(string json)
	{
		var removed = AkWaapiUtilities.ParseChildAddedOrRemoved(json);
		toRequeue.Enqueue(new TreeViewCommand(() => Remove(removed.parentInfo, removed.childInfo)));
	}

	void OnWwiseSelectionChanged(string json)
	{
		if (AutoSyncSelection)
		{
			var objects = AkWaapiUtilities.ParseSelectedObjects(json);
			if (objects.Count > 0)
			{
				if (FilterPath(objects[0].path))
				{
					treeviewCommandQueue.Enqueue(new TreeViewCommand(() => SelectItem(objects[0].objectGUID)));
				}
			}
		}
	}

	public void Rename(System.Guid objectGuid, string newName)
	{
		var item = Find(objectGuid);
		if (item != null)
		{
			item.name = newName;
		}
		else
		{
			toRequeue.Enqueue(new TreeViewCommand(() => Rename(objectGuid, newName)));
		}
	}

	public void Remove(WwiseObjectInfo parentInfo, WwiseObjectInfo childInfo)
	{
		var parent = Find(parentInfo.objectGUID);

		//Object removed, but it was never loaded so we can ignore it
		if (parent == null)
		{
			return;
		}

		parent.numChildren = parentInfo.childrenCount;
		var index = parent.children.FindIndex(el => ((AkWwiseTreeViewItem)el).objectGuid == childInfo.objectGUID);
		if (index != -1)
		{
			parent.children.RemoveAt(index);
		}
	}

	public void Expand(System.Guid objectGuid, bool select)
	{
		if (TreeView == null || !TreeView.ExpandItem(objectGuid, select))
		{
			toRequeue.Enqueue(new TreeViewCommand(() => Expand(objectGuid, select)));
		}
	}

	public override void SelectItem(System.Guid itemGuid)
	{
		if (TreeView == null)
		{
			return;
		}

		if (TreeView.m_storedSearchString != string.Empty)
		{
			return;
		}

		if (!TreeView.ExpandItem(itemGuid, true))
		{
			var item = Find(itemGuid);
			treeviewCommandQueue.Enqueue(new TreeViewCommand(() => Expand(itemGuid, true)));

			if (item == null)
			{
				AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
				{
					AddItemWithAncestors(AkWaapiUtilities.ParseObjectInfo(items));
				};
				AkWaapiUtilities.GetWwiseObjectAndAncestors(itemGuid, waapiWwiseObjectOptions, callback);
			}
		}
	}

	public bool FilterPath(string path)
	{
		var splitpath = path.Split('\\');
		if (splitpath.Length > 1)
		{
			var folder = @"\" + splitpath[1];
			if (FolderNames.Values.Contains(folder) || WaapiKeywords.FolderDisplaynames.Values.Contains(folder))
			{
				return true;
			}
		}
		return false;
	}

	public override void ItemSelected(AkWwiseTreeViewItem item)
	{
		if (AutoSyncSelection)
		{
			SelectObjectInAuthoring(item.objectGuid);
		}
	}

	private System.Guid guidToSelect;
	public override void SelectObjectInAuthoring(System.Guid objectGuid)
	{
		selectTimer.Stop();
		guidToSelect = objectGuid;
		selectTimer.Enabled = true;
		selectTimer.Start();
	}

	private void FireSelect(object sender, System.Timers.ElapsedEventArgs e)
	{
		AkWaapiUtilities.SelectObjectInAuthoring(guidToSelect);
	}

	//Make a single command queue
	private ConcurrentQueue<TreeViewCommand> treeviewCommandQueue = new ConcurrentQueue<TreeViewCommand>();
	private Queue<TreeViewCommand> toRequeue = new Queue<TreeViewCommand>();

	public class TreeViewCommand
	{
		public System.Action payload;

		public TreeViewCommand(System.Action payload)
		{
			this.payload = payload;
		}
		public void Execute()
		{
			payload.Invoke();
		}
	}

	public override void ScheduleRebuild()
	{
		rebuildFlag = true;
	}

	private bool rebuildFlag = false;
	private bool refreshFlag = false;

	public void Update()
	{
		while (treeviewCommandQueue.Count > 0)
		{
			if (treeviewCommandQueue.TryDequeue(out TreeViewCommand cmd))
			{
				cmd.Execute();
				refreshFlag = true;
			}
		}

		while (toRequeue.Count > 0)
		{
			treeviewCommandQueue.Enqueue(toRequeue.Dequeue());
		}

		//Preemptively load items in heirarchy that are close to being exposed ( up to grandchildren of unexpanded items)
		if (rebuildFlag)
		{
			TreeUtility.TreeToList(ProjectRoot, Data);
			if (TreeView != null)
			{
				Preload(ProjectRoot, TreeView.state);
			}
			refreshFlag = true;
			rebuildFlag = false;
		}


		//Updates treeView data and sets repaint flag
		if (refreshFlag)
		{
			Changed();
			refreshFlag = false;
		}
	}

	void Preload(AkWwiseTreeViewItem parent, TreeViewState treeState)
	{
		if (parent == null)
		{
			return;
		}

		if (!CheckIfFullyLoaded(parent))
		{
			AkWaapiUtilities.GetResultListDelegate<WwiseObjectInfoJsonObject> callback = (List<WwiseObjectInfoJsonObject> items) =>
			{
				UpdateParentWithLoadedChildren(parent.objectGuid, AkWaapiUtilities.ParseObjectInfo(items));
			};
			AkWaapiUtilities.GetChildren(parent.objectGuid, waapiWwiseObjectOptions, callback);
		}

		//Preload one level of hidden items. 
		if (IsExpanded(treeState, parent.id) || (parent.parent != null && IsExpanded(treeState, parent.parent.id)) ||
			parent.id == ProjectRoot.id )
		{
			foreach (AkWwiseTreeViewItem childItem in parent.children)
			{

				Preload(childItem, treeState);
			}
		}
	}

	public override void SetExpanded(IEnumerable<System.Guid> ids)
	{
		if (TreeView != null)
		{
			foreach (var id in ids)
			{
				treeviewCommandQueue.Enqueue(new TreeViewCommand(() => Expand(id, false)));
			}
			TreeView.state.expandedIDs.Clear();
		}
	}

	public void Connect()
	{
		AkWaapiUtilities.Connected += OnConnection;
		AkWaapiUtilities.QueueConsumed += ScheduleRebuild;
		AkWaapiUtilities.Disconnecting += Disconnect;
	}

	public void OnConnection()
	{
		SubscribeTopics();
		FetchData();
	}

	public void Disconnect(bool still_connected)
	{
		this.treeviewCommandQueue = new ConcurrentQueue<TreeViewCommand>();
		if (ProjectRoot != null)
			ProjectRoot.children = new List<TreeViewItem>();
		if (still_connected)
		{
			UnsubscribeTopics();
		}
		else
		{
			subscriptions.Clear();
		}
		Changed();
	}


	public void Cleanup()
	{
		subscriptions.Clear();
	}


	~AkWwiseTreeWAAPIDataSource()
	{
		Disconnect(true);
	}

	public override void SaveExpansionStatus(List<int> expandedItems)
	{
		AkWwiseProjectInfo.GetData().ExpandedWaapiItemIds = expandedItems;
	}

	public override List<int> LoadExpansionSatus()
	{
		return AkWwiseProjectInfo.GetData().ExpandedWaapiItemIds;
	}
}
#endif