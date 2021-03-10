using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Random = Unity.Mathematics.Random;

public class NameTools : EditorWindow
{
    public int NameGeneratorMinLength = 5;
    public int NameGeneratorMaxLength = 10;
    public int NameGeneratorOrder = 4;
    private int minWordLength = 4;
    private TextAsset nameFile;
    private MarkovNameGenerator _nameGenerator;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Aetheria/Name Tools")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        NameTools window = (NameTools)EditorWindow.GetWindow(typeof(NameTools));
        window.Show();
    }

    void OnGUI()
    {
        nameFile = (TextAsset) EditorGUILayout.ObjectField("Name File", nameFile, typeof(TextAsset), false);
        minWordLength = EditorGUILayout.IntField("Minimum File Word Length", minWordLength);
        NameGeneratorMinLength = EditorGUILayout.IntField("Generated Minimum Word Length", NameGeneratorMinLength);
        NameGeneratorMaxLength = EditorGUILayout.IntField("Generated Maximum Word Length", NameGeneratorMaxLength);
        NameGeneratorOrder = EditorGUILayout.IntField("Generator Order", NameGeneratorOrder);

        if (GUILayout.Button("Process Name File") && nameFile != null)
        {
            var names = new HashSet<string>();
            var lines = nameFile.text.Split('\n');
            foreach (var line in lines)
            {
                foreach(var word in line.ToUpperInvariant().Split(' ', ',', '.', '"'))
                    if (word.Length >= minWordLength && !names.Contains(word))
                        names.Add(word);
            }
            Debug.Log($"Found {lines.Length} lines, with {names.Count} unique names!");
            var random = new Random(1337);
            _nameGenerator = new MarkovNameGenerator(ref random, names, NameGeneratorOrder, NameGeneratorMinLength, NameGeneratorMaxLength);
        }

        if (_nameGenerator != null)
        {
            if (GUILayout.Button("Generate Name"))
            {
                Debug.Log(_nameGenerator.NextName);
            }
        }
    }
}