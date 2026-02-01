using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Custom editor for GameSceneInitializer that shows a dropdown of all available levels.
/// </summary>
[CustomEditor(typeof(GameSceneInitializer))]
public class GameSceneInitializerEditor : Editor
{
    private SerializedProperty selectedLevelProp;
    private LevelDefinition[] allLevels;
    private string[] levelDisplayNames;
    private int selectedIndex = 0;

    private void OnEnable()
    {
        selectedLevelProp = serializedObject.FindProperty("selectedLevel");
        LoadAllLevels();
        UpdateSelectedIndex();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Selection", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select which level to load when playing this scene in the Editor.", MessageType.Info);

        if (allLevels == null || allLevels.Length == 0)
        {
            EditorGUILayout.HelpBox("No level definitions found in Resources/Levels/LevelDefinitions!", MessageType.Warning);

            if (GUILayout.Button("Refresh Level List"))
            {
                LoadAllLevels();
                UpdateSelectedIndex();
            }
        }
        else
        {
            // Show dropdown
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Selected Level", selectedIndex, levelDisplayNames);

            if (EditorGUI.EndChangeCheck())
            {
                selectedLevelProp.objectReferenceValue = allLevels[selectedIndex];
                serializedObject.ApplyModifiedProperties();

                // Mark scene as dirty and save to persist the change
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();

                Debug.Log($"[GameSceneInitializer] Level selection saved: {allLevels[selectedIndex].levelId}");
            }

            // Show level info
            if (selectedLevelProp.objectReferenceValue != null)
            {
                LevelDefinition level = (LevelDefinition)selectedLevelProp.objectReferenceValue;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Level Info", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("ID:", level.levelId);
                EditorGUILayout.LabelField("World:", level.worldId);
                EditorGUILayout.LabelField("Grid:", $"{level.gridWidth}x{level.gridHeight}");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Level List"))
            {
                LoadAllLevels();
                UpdateSelectedIndex();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void LoadAllLevels()
    {
        // Load all LevelDefinition assets from Resources
        allLevels = Resources.LoadAll<LevelDefinition>("Levels/LevelDefinitions");

        if (allLevels == null || allLevels.Length == 0)
        {
            // Try alternate path
            allLevels = Resources.LoadAll<LevelDefinition>("Levels");
        }

        // Sort by world and order
        System.Array.Sort(allLevels, (a, b) =>
        {
            int worldCompare = string.Compare(a.worldId, b.worldId);
            if (worldCompare != 0) return worldCompare;
            return a.orderInWorld.CompareTo(b.orderInWorld);
        });

        // Create display names
        levelDisplayNames = new string[allLevels.Length];
        for (int i = 0; i < allLevels.Length; i++)
        {
            levelDisplayNames[i] = $"{allLevels[i].worldId}/{allLevels[i].levelName} ({allLevels[i].levelId})";
        }
    }

    private void UpdateSelectedIndex()
    {
        if (allLevels == null || allLevels.Length == 0)
        {
            selectedIndex = 0;
            return;
        }

        LevelDefinition currentSelection = (LevelDefinition)selectedLevelProp.objectReferenceValue;
        if (currentSelection == null)
        {
            selectedIndex = 0;
            return;
        }

        // Find the index of the currently selected level
        for (int i = 0; i < allLevels.Length; i++)
        {
            if (allLevels[i] == currentSelection)
            {
                selectedIndex = i;
                return;
            }
        }

        selectedIndex = 0;
    }
}
