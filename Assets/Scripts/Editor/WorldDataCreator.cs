using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility to create WorldData ScriptableObject assets.
/// Menu: Tools > AWITP > Create World Data
/// </summary>
public class WorldDataCreator : EditorWindow
{
    private string worldId = "";
    private string worldName = "";
    private string description = "";
    private int orderInGame = 0;
    private Color themeColor = Color.cyan;

    private List<LevelDefinition> levels = new List<LevelDefinition>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/AWITP/Create World Data")]
    public static void ShowWindow()
    {
        GetWindow<WorldDataCreator>("Create World Data");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create WorldData Asset", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        worldId = EditorGUILayout.TextField("World ID", worldId);
        worldName = EditorGUILayout.TextField("World Name", worldName);
        description = EditorGUILayout.TextField("Description", description, GUILayout.Height(60));
        orderInGame = EditorGUILayout.IntField("Order in Game", orderInGame);
        themeColor = EditorGUILayout.ColorField("Theme Color", themeColor);

        EditorGUILayout.Space();
        GUILayout.Label("Levels in this World", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        for (int i = 0; i < levels.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            levels[i] = (LevelDefinition)EditorGUILayout.ObjectField($"Level {i}", levels[i], typeof(LevelDefinition), false);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                levels.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Level Slot"))
        {
            levels.Add(null);
        }

        EditorGUILayout.Space();

        GUI.enabled = !string.IsNullOrEmpty(worldId) && !string.IsNullOrEmpty(worldName) && levels.Count > 0;
        if (GUILayout.Button("Create WorldData Asset", GUILayout.Height(30)))
        {
            CreateWorldDataAsset();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create a WorldData ScriptableObject asset that groups levels together.", MessageType.Info);
    }

    private void CreateWorldDataAsset()
    {
        // Validate levels
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] == null)
            {
                EditorUtility.DisplayDialog("Error", $"Level slot {i} is empty. Please assign all levels.", "OK");
                return;
            }
        }

        // Create the WorldData asset
        WorldData worldData = ScriptableObject.CreateInstance<WorldData>();

        worldData.worldId = worldId;
        worldData.worldName = worldName;
        worldData.description = description;
        worldData.orderInGame = orderInGame;
        worldData.themeColor = themeColor;
        worldData.levels = levels.ToArray();

        // Save the asset
        string path = $"Assets/Resources/Levels/Worlds/{worldId}.asset";
        AssetDatabase.CreateAsset(worldData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = worldData;

        Debug.Log($"Created WorldData asset at {path}");
        EditorUtility.DisplayDialog("Success", $"Created WorldData: {worldName}", "OK");
    }
}
