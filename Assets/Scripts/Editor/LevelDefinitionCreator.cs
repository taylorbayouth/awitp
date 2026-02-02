using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to create LevelDefinition ScriptableObject assets from JSON files.
/// Menu: Tools > AWITP > Create Level Definition from JSON
/// </summary>
public class LevelDefinitionCreator : EditorWindow
{
    private TextAsset jsonFile;
    private string levelId = "";
    private string levelName = "";
    private string worldId = "world_tutorial";
    private int orderInWorld = 0;

    [MenuItem("Tools/AWITP/Create Level Definition from JSON")]
    public static void ShowWindow()
    {
        GetWindow<LevelDefinitionCreator>("Create Level Definition");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create LevelDefinition from JSON", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        jsonFile = (TextAsset)EditorGUILayout.ObjectField("Level JSON File", jsonFile, typeof(TextAsset), false);
        EditorGUILayout.Space();

        levelId = EditorGUILayout.TextField("Level ID", levelId);
        levelName = EditorGUILayout.TextField("Level Name", levelName);
        worldId = EditorGUILayout.TextField("World ID", worldId);
        orderInWorld = EditorGUILayout.IntField("Order in World", orderInWorld);

        EditorGUILayout.Space();

        GUI.enabled = jsonFile != null && !string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(levelName);
        if (GUILayout.Button("Create LevelDefinition Asset", GUILayout.Height(30)))
        {
            CreateLevelDefinitionAsset();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create a LevelDefinition ScriptableObject asset that can be used with the level system.", MessageType.Info);
    }

    private void CreateLevelDefinitionAsset()
    {
        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a JSON file", "OK");
            return;
        }

        // Create the LevelDefinition asset
        LevelDefinition levelDef = ScriptableObject.CreateInstance<LevelDefinition>();

        // Parse JSON to get grid settings
        try
        {
            LevelData levelData = JsonUtility.FromJson<LevelData>(jsonFile.text);

            levelDef.levelId = levelId;
            levelDef.levelName = levelName;
            levelDef.worldId = worldId;
            levelDef.orderInWorld = orderInWorld;
            levelDef.gridWidth = levelData.gridWidth;
            levelDef.gridHeight = levelData.gridHeight;
            levelDef.levelDataJson = jsonFile.text;
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to parse JSON: {e.Message}", "OK");
            return;
        }

        // Save the asset
        string path = $"Assets/Resources/Levels/LevelDefinitions/{levelId}.asset";
        AssetDatabase.CreateAsset(levelDef, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = levelDef;

        Debug.Log($"Created LevelDefinition asset at {path}");
        EditorUtility.DisplayDialog("Success", $"Created LevelDefinition: {levelName}", "OK");
    }
}
