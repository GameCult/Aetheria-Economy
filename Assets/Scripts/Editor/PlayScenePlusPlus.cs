using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class PlayScenePlusPlus
{
    static PlayScenePlusPlus()
    {
        if(EditorPrefs.HasKey("PlayScene"))
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorPrefs.GetString("PlayScene"));
        }
    }

    [MenuItem("Edit/Play Scene/Set Static", false)]
    private static void SelectPlayScene()
    {
        EditorSceneManager.playModeStartScene = Selection.activeObject as SceneAsset;
        EditorPrefs.SetString("PlayScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
    }

    [MenuItem("Edit/Play Scene/Set Static", true)]
    private static bool SelectPlaySceneValidate()
    {
        return Selection.objects.Length == 1 && Selection.activeObject is SceneAsset;
    }

    [MenuItem("Edit/Play Scene/Clear", false)]
    private static void ClearPlayScene()
    {
        EditorSceneManager.playModeStartScene = null;
        EditorPrefs.DeleteKey("PlayScene");
    }

    [MenuItem("Edit/Play Scene/Clear", true)]
    private static bool ClearPlaySceneValidate()
    {
        return EditorSceneManager.playModeStartScene;
    }
}