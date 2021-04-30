#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Linq;
using System.Collections.Generic;

public class AkWwiseTreeProjectDataSource : AkWwiseTreeDataSource
{

	public AkWwiseTreeProjectDataSource() : base()
	{
	}

	public override void FetchData()
	{
		Data.Clear();
		m_MaxID = 0;
		InitializeMinimal();
		Changed();
	}

	protected void InitializeMinimal()
	{
		ProjectRoot = CreateProjectRootItem();

		ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(WwiseObjectType.Event));
		ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(WwiseObjectType.Switch));
		ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(WwiseObjectType.State));
		ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(WwiseObjectType.Soundbank));
		ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(WwiseObjectType.AuxBus));
		ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(WwiseObjectType.AcousticTexture));

		TreeUtility.TreeToList(ProjectRoot, Data);
	}

	public override AkWwiseTreeViewItem GetComponentDataRoot(WwiseObjectType objectType)
	{
		if (!wwiseObjectFolders.ContainsKey(objectType))
			ProjectRoot.AddWwiseItemChild(BuildObjectTypeTree(objectType));

		var tempProjectRoot = new AkWwiseTreeViewItem(ProjectRoot);
		tempProjectRoot.AddWwiseItemChild(wwiseObjectFolders[objectType]);
		return tempProjectRoot;
	}

	protected AkWwiseTreeViewItem BuildObjectTypeTree(WwiseObjectType objectType)
	{
		var rootElement = new AkWwiseTreeViewItem();
		switch (objectType)
		{
			case WwiseObjectType.AuxBus:
				rootElement = BuildTree("Master-Mixer Hierarchy", AkWwiseProjectInfo.GetData().AuxBusWwu);
				break;

			case WwiseObjectType.Event:
				rootElement = BuildTree("Events", AkWwiseProjectInfo.GetData().EventWwu);
				break;

			case WwiseObjectType.Soundbank:
				rootElement = BuildTree("SoundBanks", AkWwiseProjectInfo.GetData().BankWwu);
				break;

			case WwiseObjectType.State:
				rootElement = BuildTree("States", AkWwiseProjectInfo.GetData().StateWwu);
				break;

			case WwiseObjectType.Switch:
			case WwiseObjectType.SwitchGroup:
				rootElement = BuildTree("Switches", AkWwiseProjectInfo.GetData().SwitchWwu);
				break;

			case WwiseObjectType.GameParameter:
				rootElement = BuildTree("Game Parameters", AkWwiseProjectInfo.GetData().RtpcWwu);
				break;

			case WwiseObjectType.Trigger:
				rootElement = BuildTree("Triggers", AkWwiseProjectInfo.GetData().TriggerWwu);
				break;

			case WwiseObjectType.AcousticTexture:
				rootElement = BuildTree("Virtual Acoustics", AkWwiseProjectInfo.GetData().AcousticTextureWwu);
				break;
		}
		wwiseObjectFolders[objectType] = rootElement;
		return rootElement;
	}

	public AkWwiseTreeViewItem BuildTree(string name,
	List<AkWwiseProjectData.EventWorkUnit> Events)
	{
		var akInfoWwu = new List<AkWwiseProjectData.AkInfoWorkUnit>(Events.Count);
		for (var i = 0; i < Events.Count; i++)
		{
			akInfoWwu.Add(new AkWwiseProjectData.AkInfoWorkUnit());
			akInfoWwu[i].PhysicalPath = Events[i].PhysicalPath;
			akInfoWwu[i].ParentPath = Events[i].ParentPath;
			akInfoWwu[i].PathAndIcons = Events[i].PathAndIcons;
			akInfoWwu[i].Guid = Events[i].Guid;
			akInfoWwu[i].List = Events[i].List.ConvertAll(x => (AkWwiseProjectData.AkInformation)x);
		}
		return BuildTree(name, akInfoWwu);
	}

	public AkWwiseTreeViewItem BuildTree(string name, List<AkWwiseProjectData.GroupValWorkUnit> workUnits)
	{
		var rootFolder = new AkWwiseTreeViewItem(name, 1, GenerateUniqueID(), System.Guid.Empty, WwiseObjectType.PhysicalFolder);
		foreach (var wwu in workUnits)
		{
			var wwuItem = AddTreeItem(rootFolder, wwu.PathAndIcons);

			foreach (var group in wwu.List)
			{
				var groupElement = AddTreeItem(wwuItem, group.PathAndIcons);

				foreach (var child in group.values)
				{
					AddTreeItem(groupElement, child.PathAndIcons);
				}
			}
		}
		return rootFolder;
	}

	private AkWwiseTreeViewItem BuildTree(string name, List<AkWwiseProjectData.AkInfoWorkUnit> workUnits)
	{
		var rootFolder = new AkWwiseTreeViewItem(name, 1, GenerateUniqueID(), System.Guid.Empty, WwiseObjectType.PhysicalFolder);

		foreach (var wwu in workUnits)
		{
			var wwuElement = AddTreeItem(rootFolder, wwu.PathAndIcons);
			if (wwu.List.Count > 0)
			{
				foreach (var akInfo in wwu.List)
				{
					AddTreeItem(wwuElement, akInfo.PathAndIcons);
				}
			}
		}
		return rootFolder;
	}

	private AkWwiseTreeViewItem AddTreeItem(AkWwiseTreeViewItem parentWorkUnit, List<AkWwiseProjectData.PathElement> pathAndIcons)
	{
		var pathDepth = pathAndIcons.Count;
		var treeDepth = pathDepth + 1;
		AkWwiseTreeViewItem newItem;
		AkWwiseProjectData.PathElement pathElem;
		var parent = parentWorkUnit;

		if (pathDepth > parentWorkUnit.depth)
		{
			var unaccountedDepth = pathDepth - parentWorkUnit.depth;
			for (; unaccountedDepth > 0; unaccountedDepth--)
			{
				var pathIndex = pathAndIcons.Count - unaccountedDepth;
				pathElem = pathAndIcons[pathIndex];
				if (pathElem.ObjectGuid == System.Guid.Empty)
				{
					var path = AkWwiseProjectData.PathElement.GetProjectPathString(pathAndIcons, pathIndex);
					newItem = Find(pathElem.ObjectGuid, pathElem.ElementName, path);
				}
				else
					newItem = Find(pathElem.ObjectGuid, pathElem.ElementName);

				if (newItem == null)
				{
					newItem = new AkWwiseTreeViewItem(pathElem.ElementName, treeDepth - unaccountedDepth, GenerateUniqueID(), pathElem.ObjectGuid, pathElem.ObjectType);
					parent.AddWwiseItemChild(newItem);
					Data.Add(newItem);
				}
				parent = newItem;

			}
		}

		pathElem = pathAndIcons.Last();
		newItem = Find(pathElem.ObjectGuid);

		if (newItem == null)
		{
			newItem = new AkWwiseTreeViewItem(pathElem.ElementName, treeDepth, GenerateUniqueID(), pathElem.ObjectGuid, pathElem.ObjectType);
			parent.AddWwiseItemChild(newItem);
			Data.Add(newItem);
		}
		return newItem;
	}

	public virtual AkWwiseTreeViewItem Find(System.Guid guid, string name, string path)
	{
		var results = Data.FindAll(element => element.objectGuid == guid && element.name == name);

		foreach (var r in results)
		{
			var itemPath = GetProjectPath(r,"");
			if (itemPath == path)
			{
				return r;
			}
		}

		return null;
	}

	public string GetProjectPath(AkWwiseTreeViewItem item, string currentpath)
	{
		currentpath = $"/{item.name}{currentpath}";
		if (item.parent == null || item.parent == ProjectRoot)
		{
			return currentpath;
		}

		return GetProjectPath(item.parent as AkWwiseTreeViewItem, currentpath);
	}


	public override AkWwiseTreeViewItem GetSearchResults()
	{
		return SearchRoot;
	}

	public override void UpdateSearchResults(string searchString, WwiseObjectType objectType)
	{
		SearchRoot = new AkWwiseTreeViewItem(ProjectRoot);
		if (objectType != WwiseObjectType.None)
		{
			SearchRoot = new AkWwiseTreeViewItem(ProjectRoot);
			var objectRoot = new AkWwiseTreeViewItem(wwiseObjectFolders[objectType]);
			TreeUtility.CopyTree(wwiseObjectFolders[objectType], objectRoot);
			SearchRoot.AddWwiseItemChild(objectRoot);
		}
		else
		{
			TreeUtility.CopyTree(ProjectRoot, SearchRoot);
		}
		FilterTree(SearchRoot, searchString);
	}

	void FilterTree(AkWwiseTreeViewItem treeElement, string searchFilter)
	{
		var ItemsToRemove = new List<AkWwiseTreeViewItem>();
		for (int i = 0; i < treeElement.children.Count(); i++)
		{
			var current = treeElement.children[i] as AkWwiseTreeViewItem;
			FilterTree(current, searchFilter);

			if (current.name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) == -1 && current.children.Count == 0)
			{
				ItemsToRemove.Add(current);
			}
		}

		for (int i = 0; i < ItemsToRemove.Count(); i++)
		{
			treeElement.children.Remove(ItemsToRemove[i]);
		}
	}

	public override void SetExpanded(IEnumerable<System.Guid> ids)
	{
		if (TreeView != null)
		{
			TreeView.state.expandedIDs = GetIdsFromGuids(ids).ToList();
		}
		Changed();
	}

	public override void SaveExpansionStatus(List<int> expandedItems )
	{
		AkWwiseProjectInfo.GetData().ExpandedFileSystemItemIds = expandedItems;
	}

	public override List<int> LoadExpansionSatus()
	{
		return AkWwiseProjectInfo.GetData().ExpandedFileSystemItemIds;
	}
}
#endif