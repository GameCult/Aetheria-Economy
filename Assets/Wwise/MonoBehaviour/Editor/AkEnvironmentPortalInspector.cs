#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

[UnityEditor.CustomEditor(typeof(AkEnvironmentPortal))]
public class AkEnvironmentPortalInspector : UnityEditor.Editor
{
	private readonly int[] m_selectedIndex = new int[AkEnvironmentPortal.MAX_ENVIRONMENTS_PER_PORTAL];
	private AkEnvironmentPortal m_envPortal;
	private UnityEditor.SerializedProperty m_environments;
	private UnityEditor.SerializedProperty m_axis;
	private UnityEditor.SerializedProperty m_envList;

	[UnityEditor.MenuItem("GameObject/Wwise/Environment Portal", false, 1)]
	public static void CreatePortal()
	{
		var portal = new UnityEngine.GameObject("EnvironmentPortal");

		UnityEditor.Undo.AddComponent<AkEnvironmentPortal>(portal);
		portal.GetComponent<UnityEngine.Collider>().isTrigger = true;

		UnityEditor.Selection.objects = new UnityEngine.Object[] { portal };
	}

	private void OnEnable()
	{
		m_envPortal = target as AkEnvironmentPortal;
		m_environments = serializedObject.FindProperty("environments");
		m_axis = serializedObject.FindProperty("axis");
		m_envList = serializedObject.FindProperty("envList");

		FindOverlappingEnvironments();
		for (var i = 0; i < AkEnvironmentPortal.MAX_ENVIRONMENTS_PER_PORTAL; i++)
		{
			m_selectedIndex[i] = 0;

			var list = m_envList.GetArrayElementAtIndex(i).FindPropertyRelative("list");
			for (var j = 0; j < list.arraySize; ++j)
			{
				if (list.GetArrayElementAtIndex(j).objectReferenceValue == m_environments.GetArrayElementAtIndex(i).objectReferenceValue)
				{
					m_selectedIndex[i] = j;
					break;
				}
			}
		}
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		using (new UnityEngine.GUILayout.VerticalScope("box"))
		{
			for (var i = 0; i < AkEnvironmentPortal.MAX_ENVIRONMENTS_PER_PORTAL; i++)
			{
				var list = m_envList.GetArrayElementAtIndex(i).FindPropertyRelative("list");
				var labels = new string[list.arraySize];

				for (var j = 0; j < list.arraySize; j++)
				{
					var environment = list.GetArrayElementAtIndex(j).objectReferenceValue as AkEnvironment;
					if (environment != null)
					{
						labels[j] = j + 1 + ". " + GetEnvironmentName(environment) + " (" + environment.name + ")";
					}
					else
					{
						list.DeleteArrayElementAtIndex(j);
					}
				}

				var index = UnityEditor.EditorGUILayout.Popup("Environment #" + (i + 1), m_selectedIndex[i], labels);
				m_environments.GetArrayElementAtIndex(i).objectReferenceValue = index < 0 || index >= list.arraySize ? null : list.GetArrayElementAtIndex(index).objectReferenceValue;
				m_selectedIndex[i] = index;
			}
		}

		UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

		using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
		{
			var axisLabels = new[]{ "X", "Y", "Z" };
			var axes = new[] { UnityEngine.Vector3.right, UnityEngine.Vector3.up, UnityEngine.Vector3.forward };

			var index = 0;
			for (var i = 0; i < 3; i++)
			{
				if (m_axis.vector3Value == axes[i])
				{
					index = i;
					break;
				}
			}

			var newIndex = UnityEditor.EditorGUILayout.Popup("Axis", index, axisLabels);
			m_axis.vector3Value = axes[newIndex];

			if (index != newIndex)
			{
				for (var i = 0; i < AkEnvironmentPortal.MAX_ENVIRONMENTS_PER_PORTAL; i++)
					m_envList.GetArrayElementAtIndex(i).FindPropertyRelative("list").ClearArray();

				FindOverlappingEnvironments();
			}
		}

		serializedObject.ApplyModifiedProperties();

		AkGameObjectInspector.RigidbodyCheck(m_envPortal.gameObject);
	}

	private string GetEnvironmentName(AkEnvironment in_env)
	{
		foreach (var wwu in AkWwiseProjectInfo.GetData().AuxBusWwu)
			foreach (var env in wwu.List)
				if (in_env.data.Id == env.Id)
					return env.Name;

		return string.Empty;
	}

	public void FindOverlappingEnvironments()
	{
		var myCollider = m_envPortal.gameObject.GetComponent<UnityEngine.Collider>();
		if (myCollider == null)
			return;

		var environments = FindObjectsOfType<AkEnvironment>();
		foreach (var environment in environments)
		{
			var otherCollider = environment.gameObject.GetComponent<UnityEngine.Collider>();
			if (otherCollider == null)
				continue;

			if (myCollider.bounds.Intersects(otherCollider.bounds))
			{
				//if index == 0 => the environment is on the negative side of the portal(opposite to the direction of the chosen axis)
				//if index == 1 => the environment is on the positive side of the portal(same direction as the chosen axis) 
				var index = UnityEngine.Vector3.Dot(m_envPortal.transform.rotation * m_axis.vector3Value,
					            environment.transform.position - m_envPortal.transform.position) >= 0 ? 1 : 0;

				var list = m_envList.GetArrayElementAtIndex(index).FindPropertyRelative("list");

				var isFound = false;
				var count = list.arraySize;

				for (var j = 0; j < count; j++)
				{
					if (list.GetArrayElementAtIndex(j).objectReferenceValue == environment)
					{
						isFound = true;
						break;
					}
				}

				if (!isFound)
				{
					list.InsertArrayElementAtIndex(count);
					list.GetArrayElementAtIndex(count).objectReferenceValue = environment;

					var otherList = m_envList.GetArrayElementAtIndex(++index % AkEnvironmentPortal.MAX_ENVIRONMENTS_PER_PORTAL).FindPropertyRelative("list");

					for (var j = 0; j < otherList.arraySize; j++)
					{
						if (otherList.GetArrayElementAtIndex(j).objectReferenceValue == environment)
						{
							otherList.DeleteArrayElementAtIndex(j);
							break;
						}
					}
				}
			}
			else
			{
				for (var i = 0; i < AkEnvironmentPortal.MAX_ENVIRONMENTS_PER_PORTAL; i++)
				{
					var list = m_envList.GetArrayElementAtIndex(i).FindPropertyRelative("list");
					for (var j = 0; j < list.arraySize; j++)
					{
						if (list.GetArrayElementAtIndex(j).objectReferenceValue == environment)
						{
							list.DeleteArrayElementAtIndex(j);
							break;
						}
					}
				}
			}
		}
	}
}
#endif