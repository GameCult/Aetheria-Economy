using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Random = Unity.Mathematics.Random;

public class NameTools : EditorWindow
{
    public TextAsset[] NameFiles;
    public int NameGeneratorMinLength = 5;
    public int NameGeneratorMaxLength = 10;
    public int NameGeneratorOrder = 4;
    private int minWordLength = 4;
    private TextAsset nameFile;
    private MarkovNameGenerator _nameGenerator;
    private bool _stripNumberTokens;


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
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("NameFiles");

        EditorGUILayout.PropertyField(stringsProperty, true); // True means show children
        so.ApplyModifiedProperties(); // Remember to apply modified properties

        if (GUILayout.Button("Save Name Files"))
        {
            var database = new DatabaseCache();
            foreach (var nameFile in NameFiles)
            {
                var entry = new NameFile();
                entry.Names = nameFile.text.Split('\n');
                database.Add(entry);
            }
            database.Save(Path.Combine(ActionGameManager.GameDataDirectory.FullName, "Names.msgpack"));
        }
        
        nameFile = (TextAsset) EditorGUILayout.ObjectField("Name File", nameFile, typeof(TextAsset), false);
        minWordLength = EditorGUILayout.IntField("Minimum File Word Length", minWordLength);
        NameGeneratorMinLength = EditorGUILayout.IntField("Generated Minimum Word Length", NameGeneratorMinLength);
        NameGeneratorMaxLength = EditorGUILayout.IntField("Generated Maximum Word Length", NameGeneratorMaxLength);
        NameGeneratorOrder = EditorGUILayout.IntField("Generator Order", NameGeneratorOrder);
        _stripNumberTokens = GUILayout.Toggle(_stripNumberTokens, "Strip Number Tokens");

        if (GUILayout.Button("Clean Name File"))
        {
            var lines = nameFile.text.Split('\n');
            using StreamWriter outputFile = new StreamWriter(Path.Combine(Application.dataPath, nameFile.name + ".csv"));
            foreach (var line in lines)
            {
                if (!_stripNumberTokens)
                {
                    if (!string.IsNullOrEmpty(line) && Regex.IsMatch(line, "^[\\x00-\\x7F]+$") && !char.IsDigit(line[0]))
                    {
                        outputFile.WriteLine(line.Trim());
                    }
                }
                else
                {
                    var tokens = line.Split('_', '-', ' ');
                    if(!(tokens.Any(t => t.Length == 2 && char.IsUpper(t[0]) && char.IsUpper(t[1]))))
                    //if (Regex.IsMatch(line, "^[\\x00-\\x7F]+$") && !tokens.Any(t=>int.TryParse(t, out _)) && !tokens.Any(t=>t=="AM"||t=="FM"||t=="TV"))
                    {
                        outputFile.WriteLine(line.Trim());
                    }
                }
            }
        }

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