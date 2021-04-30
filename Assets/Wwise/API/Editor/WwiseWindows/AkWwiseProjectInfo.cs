#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Runtime.CompilerServices;
using UnityEngine;

public static class AkWwiseProjectInfo
{
	private const string DataFileName = "AkWwiseProjectData.asset";
	private static string WwiseEditorDirectory = System.IO.Path.Combine("Wwise", "Editor");
	private static string DataRelativeDirectory = System.IO.Path.Combine(WwiseEditorDirectory, "ProjectData");
	private static string DataRelativePath = System.IO.Path.Combine(DataRelativeDirectory, DataFileName);
	private static string DataAssetPath = System.IO.Path.Combine("Assets", DataRelativePath);

	public static AkWwiseProjectData m_Data;
	public static AkWwiseTreeWAAPIDataSource m_WaapiPickerData = new AkWwiseTreeWAAPIDataSource();
	public static AkWwiseTreeProjectDataSource m_ProjectPickerData = new AkWwiseTreeProjectDataSource();

	public enum DataSourceType
	{
		WwiseAuthoring,
		FileSystem
	}

	public static AkWwiseTreeWAAPIDataSource WaapiPickerData
	{
		get
		{
			return m_WaapiPickerData;
		}
	}

	public static AkWwiseTreeProjectDataSource ProjectPickerData
	{
		get
		{
			return m_ProjectPickerData;
		}
	}

	public static AkWwiseTreeDataSource GetTreeData()
	{
		AkWwiseTreeDataSource treeData;
		if (GetData().currentDataSource == DataSourceType.WwiseAuthoring)
		{
			treeData = WaapiPickerData;
		}
		else
		{
			treeData = ProjectPickerData;
		}
		return treeData;
	}

	private static bool WwiseFolderExists()
	{
		return System.IO.Directory.Exists(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Wwise"));
	}

	public static AkWwiseProjectData GetData()
	{
		if (m_Data == null && WwiseFolderExists())
		{
			try
			{
				m_Data = UnityEditor.AssetDatabase.LoadAssetAtPath<AkWwiseProjectData>(DataAssetPath);

				if (m_Data == null)
				{
					var dataAbsolutePath = System.IO.Path.Combine(UnityEngine.Application.dataPath, DataRelativePath);
					var dataExists = System.IO.File.Exists(dataAbsolutePath);

					if (!dataExists)
					{
						var dataAbsoluteDirectory = System.IO.Path.Combine(UnityEngine.Application.dataPath, DataRelativeDirectory);
						if (!System.IO.Directory.Exists(dataAbsoluteDirectory))
							System.IO.Directory.CreateDirectory(dataAbsoluteDirectory);
					}

					m_Data = UnityEngine.ScriptableObject.CreateInstance<AkWwiseProjectData>();

					if (dataExists)
						UnityEngine.Debug.LogWarning("WwiseUnity: Unable to load asset at <" + dataAbsolutePath + ">.");
					else
					{
#if UNITY_2019_3_OR_LATER
						if (UnityEditor.EditorSettings.assetPipelineMode == UnityEditor.AssetPipelineMode.Version2)
						{
							UnityEditor.EditorApplication.delayCall += () => UnityEditor.AssetDatabase.CreateAsset(m_Data, DataAssetPath);
						}
						else
#else
						{
							UnityEditor.AssetDatabase.CreateAsset(m_Data, DataAssetPath);
						}
#endif
					}
				}
			}
			catch (System.Exception e)
			{
				UnityEngine.Debug.LogError("WwiseUnity: Unable to load Wwise Data: " + e);
			}
		}

		return m_Data;
	}

	public static bool Populate()
	{
		var bDirty = false;
		if (AkUtilities.IsWwiseProjectAvailable)
		{
			bDirty = AkWwiseWWUBuilder.Populate();
			bDirty |= AkWwiseXMLBuilder.Populate();
			if (bDirty)
			{
				UnityEditor.EditorUtility.SetDirty(GetData());
				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh();
			}
		}

		return bDirty;
	}
}
#endif
