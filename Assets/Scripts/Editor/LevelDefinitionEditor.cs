using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[CustomEditor(typeof(LevelDefinition))]
public class LevelDefinitionEditor : Editor
{
    private bool showInventory = true;
    private Vector2 inventoryScrollPos;

    private int previousGridWidth = -1;
    private int previousGridHeight = -1;

    private void OnEnable()
    {
        LevelDefinition levelDef = (LevelDefinition)target;
        LevelData data = levelDef.GetLevelData();
        previousGridWidth = data.gridWidth;
        previousGridHeight = data.gridHeight;
    }

    public override void OnInspectorGUI()
    {
        LevelDefinition levelDef = (LevelDefinition)target;
        LevelData data = levelDef.GetLevelData();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Definition", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        levelDef.levelId = EditorGUILayout.TextField("Level ID", levelDef.levelId);
        levelDef.levelName = EditorGUILayout.TextField("Level Name", levelDef.levelName);
        levelDef.worldId = EditorGUILayout.TextField("World ID", levelDef.worldId);
        levelDef.orderInWorld = EditorGUILayout.IntField("Order In World", levelDef.orderInWorld);
        if (EditorGUI.EndChangeCheck())
        {
            data.levelName = levelDef.levelName;
            MarkDirty(levelDef);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int newWidth = Mathf.Max(1, EditorGUILayout.IntField("Grid Width", data.gridWidth));
        int newHeight = Mathf.Max(1, EditorGUILayout.IntField("Grid Height", data.gridHeight));
        bool gridChanged = EditorGUI.EndChangeCheck();

        if (gridChanged)
        {
            HandleGridResize(data, newWidth, newHeight);
            MarkDirty(levelDef);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Visuals", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        levelDef.gridLineWidth = EditorGUILayout.Slider("Grid Stroke Width", levelDef.gridLineWidth, 0.001f, 0.1f);
        levelDef.gridLineColor = EditorGUILayout.ColorField("Grid Color", levelDef.gridLineColor);
        if (EditorGUI.EndChangeCheck())
        {
            MarkDirty(levelDef);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cursor Colors", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        levelDef.cursorNonPlaceableColor = EditorGUILayout.ColorField("Cursor Nonplaceable", levelDef.cursorNonPlaceableColor);
        levelDef.cursorPlaceableColor = EditorGUILayout.ColorField("Cursor Placeable", levelDef.cursorPlaceableColor);
        levelDef.cursorEditableColor = EditorGUILayout.ColorField("Cursor Editable", levelDef.cursorEditableColor);
        if (EditorGUI.EndChangeCheck())
        {
            MarkDirty(levelDef);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Inventory is editor-defined. Blocks, Lems, and placeable spaces are authored in play mode.", MessageType.Info);

        showInventory = EditorGUILayout.Foldout(showInventory, "Inventory Configuration", true, EditorStyles.foldoutHeader);
        if (showInventory)
        {
            EditorGUI.indentLevel++;
            DrawInventoryEditor(levelDef, data);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit this level visually in the scene. Click 'Edit Level Visually' to load the level and enter play mode.", MessageType.Info);

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("Edit Level Visually", GUILayout.Height(32)))
        {
            EditLevelVisually(levelDef);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        if (GUILayout.Button("Validate Level", GUILayout.Height(28)))
        {
            bool isValid = levelDef.Validate();
            EditorUtility.DisplayDialog(isValid ? "Validation Success" : "Validation Failed",
                isValid ? "Level definition is valid!" : "Check the Console for validation warnings.", "OK");
        }
    }

    private void DrawInventoryEditor(LevelDefinition levelDef, LevelData data)
    {
        if (data.inventoryEntries == null)
        {
            data.inventoryEntries = new List<BlockInventoryEntry>();
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Entry", GUILayout.Width(100)))
        {
            data.inventoryEntries.Add(new BlockInventoryEntry(BlockType.Walk, 10));
            MarkDirty(levelDef);
        }
        if (GUILayout.Button("Clear All", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("Clear Inventory", "Remove all inventory entries?", "Yes", "Cancel"))
            {
                data.inventoryEntries.Clear();
                MarkDirty(levelDef);
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (data.inventoryEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("No inventory entries. Click '+ Add Entry' to add blocks.", MessageType.Warning);
        }
        else
        {
            inventoryScrollPos = EditorGUILayout.BeginScrollView(inventoryScrollPos, GUILayout.MaxHeight(400));

            for (int i = 0; i < data.inventoryEntries.Count; i++)
            {
                DrawInventoryEntry(levelDef, data.inventoryEntries, i);
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawInventoryEntry(LevelDefinition levelDef, List<BlockInventoryEntry> entries, int index)
    {
        BlockInventoryEntry entry = entries[index];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Entry {index}", EditorStyles.boldLabel, GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
        {
            entries.RemoveAt(index);
            MarkDirty(levelDef);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        entry.blockType = (BlockType)EditorGUILayout.EnumPopup("Block Type", entry.blockType);
        entry.displayName = EditorGUILayout.TextField("Display Name (optional)", entry.displayName);
        entry.maxCount = EditorGUILayout.IntField("Max Count", Mathf.Max(0, entry.maxCount));

        if (entry.blockType == BlockType.Teleporter)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Teleporter Settings", EditorStyles.miniBoldLabel);
            entry.flavorId = EditorGUILayout.TextField("Flavor ID (e.g., A, B, C)", entry.flavorId);
            entry.isPairInventory = EditorGUILayout.Toggle("Is Pair Inventory", entry.isPairInventory);
            if (entry.isPairInventory)
            {
                entry.pairSize = EditorGUILayout.IntField("Pair Size", Mathf.Max(2, entry.pairSize));
            }
        }
        else if (entry.blockType == BlockType.Transporter)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Transporter Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Route Steps (e.g., U5, R3, D2):");
            if (entry.routeSteps == null)
            {
                entry.routeSteps = new string[0];
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Step", GUILayout.Width(80)))
            {
                System.Array.Resize(ref entry.routeSteps, entry.routeSteps.Length + 1);
                entry.routeSteps[entry.routeSteps.Length - 1] = "U1";
            }
            if (entry.routeSteps.Length > 0 && GUILayout.Button("- Remove Last", GUILayout.Width(100)))
            {
                System.Array.Resize(ref entry.routeSteps, entry.routeSteps.Length - 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            for (int j = 0; j < entry.routeSteps.Length; j++)
            {
                entry.routeSteps[j] = EditorGUILayout.TextField($"Step {j}", entry.routeSteps[j]);
            }
            EditorGUI.indentLevel--;

            entry.flavorId = EditorGUILayout.TextField("Flavor ID (optional)", entry.flavorId);
        }

        EditorGUILayout.Space(5);
        bool showAdvanced = !string.IsNullOrEmpty(entry.inventoryGroupId);
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            entry.inventoryGroupId = EditorGUILayout.TextField("Inventory Group ID", entry.inventoryGroupId);
            EditorGUILayout.HelpBox("Entries with the same group ID share inventory counts.", MessageType.None);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        MarkDirty(levelDef);
    }

    private void HandleGridResize(LevelData data, int newWidth, int newHeight)
    {
        if (previousGridWidth <= 0 || previousGridHeight <= 0)
        {
            data.gridWidth = newWidth;
            data.gridHeight = newHeight;
            previousGridWidth = newWidth;
            previousGridHeight = newHeight;
            return;
        }

        bool dimensionsChanged = (newWidth != previousGridWidth || newHeight != previousGridHeight);
        if (!dimensionsChanged) return;

        Debug.LogWarning($"[LevelDefinitionEditor] Grid size changed: {previousGridWidth}x{previousGridHeight} → {newWidth}x{newHeight}");
        Debug.LogWarning("[LevelDefinitionEditor] Clearing blocks, Lems, keys, and placeable spaces. Inventory is preserved.");

        data.gridWidth = newWidth;
        data.gridHeight = newHeight;

        data.blocks = new List<LevelData.BlockData>();
        data.permanentBlocks = new List<LevelData.BlockData>();
        data.placeableSpaceIndices = new List<int>();
        data.lems = new List<LevelData.LemData>();
        data.keyStates = new List<LevelData.KeyStateData>();
        data.cameraSettings = new LevelData.CameraSettings();

        previousGridWidth = newWidth;
        previousGridHeight = newHeight;
    }

    private void EditLevelVisually(LevelDefinition levelDef)
    {
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        string[] guids = AssetDatabase.FindAssets("t:Scene Master");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Scene Not Found", "Could not find Master.unity scene. Make sure it exists in your project.", "OK");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes",
                "The current scene has unsaved changes. Do you want to save before switching scenes?",
                "Save", "Don't Save"))
            {
                EditorSceneManager.SaveOpenScenes();
            }
        }

        Scene masterScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!masterScene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Failed to load Master scene.", "OK");
            return;
        }

        GameSceneInitializer initializer = ServiceRegistry.Get<GameSceneInitializer>();
        if (initializer == null)
        {
            GameObject initObj = new GameObject("GameSceneInitializer");
            initializer = initObj.AddComponent<GameSceneInitializer>();
        }

        SerializedObject initializerSO = new SerializedObject(initializer);
        SerializedProperty selectedLevelProp = initializerSO.FindProperty("selectedLevel");
        selectedLevelProp.objectReferenceValue = levelDef;
        initializerSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(masterScene);
        EditorSceneManager.SaveScene(masterScene);

        Debug.Log($"<color=cyan>[Visual Editor]</color> Loading level '{levelDef.levelName}' ({levelDef.levelId}) for visual editing");
        Debug.Log("<color=cyan>[Visual Editor]</color> Use arrow keys to move cursor, Space/Enter to place blocks, Cmd+S to save changes");

        EditorApplication.EnterPlaymode();
    }

    private static void MarkDirty(Object obj)
    {
        if (obj == null) return;
        EditorUtility.SetDirty(obj);
    }
}
