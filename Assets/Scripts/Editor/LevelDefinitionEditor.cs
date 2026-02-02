using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[CustomEditor(typeof(LevelDefinition))]
public class LevelDefinitionEditor : Editor
{
    private SerializedProperty levelIdProp;
    private SerializedProperty levelNameProp;
    private SerializedProperty worldIdProp;
    private SerializedProperty orderInWorldProp;
    private SerializedProperty gridWidthProp;
    private SerializedProperty gridHeightProp;
    private SerializedProperty levelDataJsonProp;

    private List<BlockInventoryEntry> inventoryEntries = new List<BlockInventoryEntry>();
    private bool showInventory = true;
    private bool showJsonData = false;
    private Vector2 inventoryScrollPos;

    // Track previous grid dimensions to detect actual changes
    private int previousGridWidth = -1;
    private int previousGridHeight = -1;

    private void OnEnable()
    {
        levelIdProp = serializedObject.FindProperty("levelId");
        levelNameProp = serializedObject.FindProperty("levelName");
        worldIdProp = serializedObject.FindProperty("worldId");
        orderInWorldProp = serializedObject.FindProperty("orderInWorld");
        gridWidthProp = serializedObject.FindProperty("gridWidth");
        gridHeightProp = serializedObject.FindProperty("gridHeight");
        levelDataJsonProp = serializedObject.FindProperty("levelDataJson");

        LoadInventoryFromJson();

        // Initialize previous grid dimensions
        previousGridWidth = gridWidthProp.intValue;
        previousGridHeight = gridHeightProp.intValue;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        LevelDefinition levelDef = (LevelDefinition)target;

        // Header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Definition", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Basic metadata
        EditorGUILayout.PropertyField(levelIdProp, new GUIContent("Level ID", "Unique identifier (e.g., 'tutorial_01')"));
        EditorGUILayout.PropertyField(levelNameProp, new GUIContent("Level Name", "Display name shown to players"));
        EditorGUILayout.PropertyField(worldIdProp, new GUIContent("World ID", "World this level belongs to"));
        EditorGUILayout.PropertyField(orderInWorldProp, new GUIContent("Order in World", "0-based order within the world"));

        EditorGUILayout.Space();

        // Visual Editor Section
        EditorGUILayout.LabelField("Visual Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit this level visually in the scene. Click 'Edit Level Visually' to load the level and enter play mode.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();

        // Main edit button
        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("Edit Level Visually", GUILayout.Height(35)))
        {
            EditLevelVisually();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Grid configuration
        EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);

        // Track if grid settings changed
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(gridWidthProp, new GUIContent("Grid Width", "Number of columns"));
        EditorGUILayout.PropertyField(gridHeightProp, new GUIContent("Grid Height", "Number of rows (cells are 1.0 world unit)"));
        bool gridSettingsChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space();

        // Inventory section
        showInventory = EditorGUILayout.Foldout(showInventory, "Inventory Configuration", true, EditorStyles.foldoutHeader);
        if (showInventory)
        {
            EditorGUI.indentLevel++;
            DrawInventoryEditor();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // JSON data section (collapsible)
        showJsonData = EditorGUILayout.Foldout(showJsonData, "Level Data JSON (Advanced)", true, EditorStyles.foldoutHeader);
        if (showJsonData)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("This contains the complete level data. Normally you don't need to edit this directly - use Cmd+S in play mode to save your level design.", MessageType.Info);
            EditorGUILayout.PropertyField(levelDataJsonProp, GUIContent.none, GUILayout.Height(100));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Level", GUILayout.Height(30)))
        {
            bool isValid = levelDef.Validate();
            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation Success", "Level definition is valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Failed", "Check the Console for validation warnings.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        // Auto-sync grid settings to JSON when they change
        if (gridSettingsChanged)
        {
            SaveGridSettingsToJson();
        }
    }

    private void DrawInventoryEditor()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Info text
        EditorGUILayout.HelpBox("Configure which blocks players can place and how many. Changes are saved to the JSON automatically.", MessageType.Info);

        EditorGUILayout.Space();

        // Add/Clear buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Entry", GUILayout.Width(100)))
        {
            inventoryEntries.Add(new BlockInventoryEntry(BlockType.Walk, 10));
        }
        if (GUILayout.Button("Clear All", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("Clear Inventory", "Remove all inventory entries?", "Yes", "Cancel"))
            {
                inventoryEntries.Clear();
                SaveInventoryToJson();
            }
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save Changes", GUILayout.Width(120)))
        {
            SaveInventoryToJson();
            EditorUtility.DisplayDialog("Saved", "Inventory saved to level data JSON.", "OK");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Inventory list
        if (inventoryEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("No inventory entries. Click '+ Add Entry' to add blocks.", MessageType.Warning);
        }
        else
        {
            inventoryScrollPos = EditorGUILayout.BeginScrollView(inventoryScrollPos, GUILayout.MaxHeight(400));

            for (int i = 0; i < inventoryEntries.Count; i++)
            {
                DrawInventoryEntry(i);
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawInventoryEntry(int index)
    {
        BlockInventoryEntry entry = inventoryEntries[index];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        // Entry header with delete button
        EditorGUILayout.LabelField($"Entry {index}", EditorStyles.boldLabel, GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
        {
            inventoryEntries.RemoveAt(index);
            SaveInventoryToJson();
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Block type
        entry.blockType = (BlockType)EditorGUILayout.EnumPopup("Block Type", entry.blockType);

        // Display name (optional)
        entry.displayName = EditorGUILayout.TextField("Display Name (optional)", entry.displayName);

        // Max count
        entry.maxCount = EditorGUILayout.IntField("Max Count", Mathf.Max(0, entry.maxCount));

        // Show advanced options based on block type
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

            // Route steps
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

        // Inventory group (advanced)
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
    }

    private void LoadInventoryFromJson()
    {
        inventoryEntries.Clear();

        string json = levelDataJsonProp.stringValue;
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            LevelData levelData = JsonUtility.FromJson<LevelData>(json);
            if (levelData != null && levelData.inventoryEntries != null)
            {
                foreach (var entry in levelData.inventoryEntries)
                {
                    inventoryEntries.Add(entry.Clone());
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LevelDefinitionEditor] Could not parse existing inventory: {e.Message}");
        }
    }

    private void SaveInventoryToJson()
    {
        string json = levelDataJsonProp.stringValue;
        LevelData levelData;

        // Parse existing or create new
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                levelData = JsonUtility.FromJson<LevelData>(json);
                if (levelData == null)
                {
                    levelData = new LevelData();
                }
            }
            catch
            {
                levelData = new LevelData();
            }
        }
        else
        {
            levelData = new LevelData();
        }

        // Ensure all required lists are initialized
        if (levelData.blocks == null) levelData.blocks = new List<LevelData.BlockData>();
        if (levelData.permanentBlocks == null) levelData.permanentBlocks = new List<LevelData.BlockData>();
        if (levelData.placeableSpaceIndices == null) levelData.placeableSpaceIndices = new List<int>();
        if (levelData.lems == null) levelData.lems = new List<LevelData.LemData>();
        if (levelData.keyStates == null) levelData.keyStates = new List<LevelData.KeyStateData>();

        // Update inventory
        levelData.inventoryEntries = new List<BlockInventoryEntry>();
        foreach (var entry in inventoryEntries)
        {
            levelData.inventoryEntries.Add(entry.Clone());
        }

        // Update grid settings from properties
        levelData.gridWidth = gridWidthProp.intValue;
        levelData.gridHeight = gridHeightProp.intValue;
        levelData.levelName = levelNameProp.stringValue;

        // Serialize back
        string newJson = JsonUtility.ToJson(levelData, true);
        levelDataJsonProp.stringValue = newJson;

        // Force Unity to save the changes
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LevelDefinitionEditor] Saved {inventoryEntries.Count} inventory entries to {levelNameProp.stringValue}");
    }

    private void SaveGridSettingsToJson()
    {
        int newWidth = gridWidthProp.intValue;
        int newHeight = gridHeightProp.intValue;

        // Check if grid dimensions actually changed
        bool dimensionsChanged = (newWidth != previousGridWidth || newHeight != previousGridHeight);

        if (dimensionsChanged && previousGridWidth > 0 && previousGridHeight > 0)
        {
            // Grid size changed - reset level data
            Debug.LogWarning($"[LevelDefinitionEditor] Grid size changed for '{levelNameProp.stringValue}': " +
                           $"{previousGridWidth}x{previousGridHeight} → {newWidth}x{newHeight}");
            Debug.LogWarning($"[LevelDefinitionEditor] Resetting level data (blocks, Lems, keys cleared; inventory and camera preserved)");

            ResetLevelDataForGridChange(newWidth, newHeight);

            // Update tracked dimensions
            previousGridWidth = newWidth;
            previousGridHeight = newHeight;

            // Delete runtime save file if it exists
            string levelName = levelNameProp.stringValue;
            if (!string.IsNullOrEmpty(levelName) && LevelSaveSystem.LevelExists(levelName))
            {
                bool deleted = LevelSaveSystem.DeleteLevel(levelName);
                if (deleted)
                {
                    Debug.Log($"<color=yellow>✓ Deleted runtime save file for '{levelName}'</color>");
                }
            }
        }
        else
        {
            // Just update the grid dimensions without resetting
            string json = levelDataJsonProp.stringValue;
            LevelData levelData;

            // Parse existing or create new
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    levelData = JsonUtility.FromJson<LevelData>(json);
                    if (levelData == null)
                    {
                        levelData = new LevelData();
                    }
                }
                catch
                {
                    levelData = new LevelData();
                }
            }
            else
            {
                levelData = new LevelData();
            }

            // Ensure all required lists are initialized
            if (levelData.blocks == null) levelData.blocks = new List<LevelData.BlockData>();
            if (levelData.permanentBlocks == null) levelData.permanentBlocks = new List<LevelData.BlockData>();
            if (levelData.placeableSpaceIndices == null) levelData.placeableSpaceIndices = new List<int>();
            if (levelData.lems == null) levelData.lems = new List<LevelData.LemData>();
            if (levelData.keyStates == null) levelData.keyStates = new List<LevelData.KeyStateData>();
            if (levelData.inventoryEntries == null) levelData.inventoryEntries = new List<BlockInventoryEntry>();

            // Update grid settings from properties
            levelData.gridWidth = newWidth;
            levelData.gridHeight = newHeight;
            levelData.levelName = levelNameProp.stringValue;

            // Serialize back
            string newJson = JsonUtility.ToJson(levelData, true);
            levelDataJsonProp.stringValue = newJson;

            // Force Unity to save the changes
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LevelDefinitionEditor] Auto-synced grid settings: {levelData.gridWidth}x{levelData.gridHeight} (1.0 unit cells)");

            // Update tracked dimensions
            previousGridWidth = newWidth;
            previousGridHeight = newHeight;
        }
    }

    private void ResetLevelDataForGridChange(int newWidth, int newHeight)
    {
        // Preserve inventory only (reset camera to defaults for new grid size)
        List<BlockInventoryEntry> preservedInventory = new List<BlockInventoryEntry>();

        string json = levelDataJsonProp.stringValue;
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                LevelData existingData = JsonUtility.FromJson<LevelData>(json);
                if (existingData != null && existingData.inventoryEntries != null)
                {
                    preservedInventory = existingData.inventoryEntries;
                }
            }
            catch
            {
                // If parsing fails, just start fresh
            }
        }

        // Create fresh level data with new dimensions and default camera settings
        LevelData newData = new LevelData
        {
            gridWidth = newWidth,
            gridHeight = newHeight,
            levelName = levelNameProp.stringValue,
            cameraSettings = new LevelData.CameraSettings(), // Fresh camera settings for new grid
            inventoryEntries = preservedInventory,
            // All other lists remain empty (blocks, permanentBlocks, lems, keyStates, placeableSpaceIndices)
            blocks = new List<LevelData.BlockData>(),
            permanentBlocks = new List<LevelData.BlockData>(),
            placeableSpaceIndices = new List<int>(),
            lems = new List<LevelData.LemData>(),
            keyStates = new List<LevelData.KeyStateData>()
        };

        // Update the JSON
        levelDataJsonProp.stringValue = JsonUtility.ToJson(newData, true);

        // Force Unity to save the changes
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=orange>✓ Reset level data for '{levelNameProp.stringValue}' to {newWidth}x{newHeight} grid</color>");
        Debug.Log($"  • Cleared: blocks, permanent blocks, Lems, keys, placeable spaces, camera settings");
        Debug.Log($"  • Preserved: inventory ({preservedInventory.Count} entries)");
    }

    private void EditLevelVisually()
    {
        LevelDefinition levelDef = (LevelDefinition)target;

        // Save any pending changes first
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        // Find the Master scene
        string[] guids = AssetDatabase.FindAssets("t:Scene Master");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Scene Not Found", "Could not find Master.unity scene. Make sure it exists in your project.", "OK");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

        // Check if we need to save current scene
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (!EditorUtility.DisplayDialog("Unsaved Changes",
                "The current scene has unsaved changes. Do you want to save before switching scenes?",
                "Save", "Don't Save"))
            {
                // User chose not to save, but we still need to continue
            }
            else
            {
                EditorSceneManager.SaveOpenScenes();
            }
        }

        // Load Master scene
        Scene masterScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!masterScene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Failed to load Master scene.", "OK");
            return;
        }

        // Find or create GameSceneInitializer
        GameSceneInitializer initializer = FindAnyObjectByType<GameSceneInitializer>();

        if (initializer == null)
        {
            // Create a new GameObject with GameSceneInitializer
            GameObject initObj = new GameObject("GameSceneInitializer");
            initializer = initObj.AddComponent<GameSceneInitializer>();
            Debug.Log("[LevelDefinitionEditor] Created new GameSceneInitializer in scene");
        }

        // Set the selected level
        SerializedObject initializerSO = new SerializedObject(initializer);
        SerializedProperty selectedLevelProp = initializerSO.FindProperty("selectedLevel");
        selectedLevelProp.objectReferenceValue = levelDef;
        initializerSO.ApplyModifiedProperties();

        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(masterScene);
        EditorSceneManager.SaveScene(masterScene);

        // Enter play mode
        Debug.Log($"<color=cyan>[Visual Editor]</color> Loading level '{levelDef.levelName}' ({levelDef.levelId}) for visual editing");
        Debug.Log($"<color=cyan>[Visual Editor]</color> Use arrow keys to move cursor, Space/Enter to place blocks, Cmd+S to save changes");

        EditorApplication.EnterPlaymode();
    }
}
