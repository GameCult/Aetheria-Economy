#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2020 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

public abstract class AkWwiseTreeDataSource
{
	public List<AkWwiseTreeViewItem> Data { get; protected set; }

	public AkWwiseTreeViewItem ProjectRoot { get; protected set; }
	public Dictionary<WwiseObjectType, AkWwiseTreeViewItem> wwiseObjectFolders;

	public AkWwiseTreeViewItem SearchRoot { get; protected set; }
	public List<AkWwiseTreeViewItem> SearchItems { get; protected set; }

	public AkWwiseTreeView TreeView { protected get; set; }

	public event System.Action modelChanged;

	static readonly List<TreeViewItem> collapsedChildren = new List<TreeViewItem>();
	public static List<TreeViewItem> CreateCollapsedChild()
	{
		// To mark a collapsed parent we use a list with one element that is null.
		// The null element in the children list ensures we show the collapse arrow.
		// Reuse read-only list to prevent allocations.
		if (collapsedChildren.Count != 1 || collapsedChildren[0] != null)
		{
			collapsedChildren.Clear();
			collapsedChildren.Add(null);
		}
		return collapsedChildren;
	}

	public AkWwiseTreeViewItem CreateProjectRootItem()
	{
		return new AkWwiseTreeViewItem(System.IO.Path.GetFileNameWithoutExtension(AkWwiseEditorSettings.Instance.WwiseProjectPath),
			-1, GenerateUniqueID(), System.Guid.Empty, WwiseObjectType.Project);
	}

	protected int m_MaxID;
	public int GenerateUniqueID()
	{
		return ++m_MaxID;
	}

	public AkWwiseTreeDataSource()
	{
		Data = new List<AkWwiseTreeViewItem>(100);
		wwiseObjectFolders = new Dictionary<WwiseObjectType, AkWwiseTreeViewItem>();
		ProjectRoot = CreateProjectRootItem();
	}

	public virtual AkWwiseTreeViewItem Find(IEnumerable<AkWwiseTreeViewItem> data, System.Guid guid)
	{
		return data.FirstOrDefault(element => element.objectGuid == guid);
	}

	public virtual AkWwiseTreeViewItem Find(IEnumerable<AkWwiseTreeViewItem> data, System.Guid guid, string name)
	{
		return data.FirstOrDefault(element => element.objectGuid == guid && element.name ==name);
	}


	public virtual AkWwiseTreeViewItem Find(IEnumerable<AkWwiseTreeViewItem> data, int id)
	{
		return data.FirstOrDefault(element => element.id == id);
	}

	public virtual AkWwiseTreeViewItem Find(int id)
	{
		return Find(Data, id);
	}

	public virtual AkWwiseTreeViewItem Find(System.Guid guid)
	{
		return Find(Data, guid);
	}

	public virtual AkWwiseTreeViewItem Find(System.Guid guid, string name)
	{
		return Find(Data, guid, name);
	}

	public IEnumerable<System.Guid> GetGuidsFromIds(IEnumerable<int> ids)
	{
		if (Data.Count == 0) return new List<System.Guid>();
		return Data.Where(el => ids.Contains(el.id)).Select(el => el.objectGuid);
	}

	public IEnumerable<int> GetIdsFromGuids(IEnumerable<System.Guid> guids)
	{
		if (Data.Count == 0) return new List<int>();
		return Data.Where(el => guids.Contains(el.objectGuid)).Select(el => el.id);
	}

	public IList<int> GetAncestors(int id)
	{
		var parents = new List<int>();
		TreeViewItem el = Find(id);
		if (el != null)
		{
			while (el.parent != null)
			{
				parents.Add(el.parent.id);
				el = el.parent;
			}
		}
		return parents;
	}

	public IList<int> GetDescendantsThatHaveChildren(int id)
	{
		AkWwiseTreeViewItem searchFromThis = Find(id);
		if (searchFromThis != null)
		{
			return GetParentsBelowStackBased(searchFromThis);
		}
		return new List<int>();
	}

	IList<int> GetParentsBelowStackBased(AkWwiseTreeViewItem searchFromThis)
	{
		Stack<AkWwiseTreeViewItem> stack = new Stack<AkWwiseTreeViewItem>();
		stack.Push(searchFromThis);

		var parentsBelow = new List<int>();
		while (stack.Count > 0)
		{
			AkWwiseTreeViewItem current = stack.Pop();
			if (current.hasChildren)
			{
				parentsBelow.Add(current.id);
				foreach (AkWwiseTreeViewItem el in current.children)
				{
					stack.Push(el);
				}
			}
		}
		return parentsBelow;
	}

	abstract public AkWwiseTreeViewItem GetComponentDataRoot(WwiseObjectType objectType);

	protected void Changed()
	{
		if (modelChanged != null)
			modelChanged();
	}

	public bool IsExpanded(TreeViewState state, int id)
	{
		if (ProjectRoot != null && id == ProjectRoot.id)
		{
			return true;
		}
		return state.expandedIDs.BinarySearch(id) >= 0;
	}

	public abstract void FetchData();

	public static int GetDepthFromPath(string path)
	{
		return path.Split('\\').Length - 1;
	}

	public virtual void ScheduleRebuild()
	{
	}

	public abstract void SaveExpansionStatus(List<int> itemIds);
	public abstract List<int> LoadExpansionSatus();

	public string currentSearchString;
	public bool isSearching = false;
	public abstract AkWwiseTreeViewItem GetSearchResults();
	public abstract void UpdateSearchResults(string searchString, WwiseObjectType objectType);
	public virtual void SelectItem(System.Guid itemGuid)
	{
		TreeView.ExpandItem(itemGuid, true);
	}

	public virtual void LoadComponentData(WwiseObjectType objectType) { }
	public virtual void ItemSelected(AkWwiseTreeViewItem itemID) { }
	public virtual void SelectObjectInAuthoring(System.Guid objectGuid) { }
	public abstract void SetExpanded(IEnumerable<System.Guid> ids);
}

#region Utility Functions
public static class TreeUtility
{
	public static void CopyTree(AkWwiseTreeViewItem sourceRoot, AkWwiseTreeViewItem destRoot)
	{
		for (int i = 0; i < sourceRoot.children.Count(); i++)
		{
			var currItem = sourceRoot.children[i];
			var newItem = new AkWwiseTreeViewItem(currItem as AkWwiseTreeViewItem);
			CopyTree(currItem as AkWwiseTreeViewItem, newItem);
			destRoot.AddWwiseItemChild(newItem);
		}
	}

	public static void TreeToList(AkWwiseTreeViewItem root, IList<AkWwiseTreeViewItem> result)
	{
		if (root == null)
			return;

		if (result == null)
			return;

		result.Clear();

		Stack<AkWwiseTreeViewItem> stack = new Stack<AkWwiseTreeViewItem>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			AkWwiseTreeViewItem current = stack.Pop();
			result.Add(current);

			if (current.children != null && current.children.Count > 0)
			{
				for (int i = current.children.Count - 1; i >= 0; i--)
				{
					if (current.children[i] != null)
					{
						stack.Push((AkWwiseTreeViewItem)current.children[i]);
					}
				}
			}
		}
	}
}
#endregion
#endif