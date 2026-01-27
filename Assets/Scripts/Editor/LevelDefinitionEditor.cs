using UnityEditor;
using UnityEngine;
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
    private SerializedProperty cellSizeProp;
    private SerializedProperty levelDataJsonProp;

    private List<BlockInventoryEntry> inventoryEntries = new List<BlockInventoryEntry>();
    private bool showInventory = true;
    private bool showJsonData = false;
    private Vector2 inventoryScrollPos;

    private void OnEnable()
    {
        levelIdProp = serializedObject.FindProperty("levelId");
        levelNameProp = serializedObject.FindProperty("levelName");
        worldIdProp = serializedObject.FindProperty("worldId");
        orderInWorldProp = serializedObject.FindProperty("orderInWorld");
        gridWidthProp = serializedObject.FindProperty("gridWidth");
        gridHeightProp = serializedObject.FindProperty("gridHeight");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        levelDataJsonProp = serializedObject.FindProperty("levelDataJson");

        LoadInventoryFromJson();
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

        // Grid configuration
        EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(gridWidthProp, new GUIContent("Grid Width", "Number of columns"));
        EditorGUILayout.PropertyField(gridHeightProp, new GUIContent("Grid Height", "Number of rows"));
        EditorGUILayout.PropertyField(cellSizeProp, new GUIContent("Cell Size", "Size of each cell in world units"));

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
            inventoryEntries.Add(new BlockInventoryEntry(BlockType.Default, 10));
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
        levelData.cellSize = cellSizeProp.floatValue;
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
}
