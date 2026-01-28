using UnityEngine;

/// <summary>
/// ScriptableObject that defines a single level in the game.
/// Contains metadata, grid configuration, and the JSON-serialized level data.
///
/// USAGE:
/// - Create in Unity: Right-click in Project window -> Create -> AWITP -> Level Definition
/// - Configure in Inspector: Set levelId, levelName, worldId, orderInWorld
/// - Paste level JSON data into levelDataJson field
/// - Reference in WorldData to include in a world
///
/// RUNTIME:
/// - Call ToLevelData() to get the deserialized LevelData for instantiation
/// - LevelManager uses this to load and instantiate levels
/// </summary>
[CreateAssetMenu(fileName = "Level", menuName = "AWITP/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Metadata")]
    [Tooltip("Unique identifier for this level (e.g., 'tutorial_01', 'world1_level3')")]
    public string levelId;

    [Tooltip("Display name shown to players (e.g., 'First Steps', 'Bridge Builder')")]
    public string levelName;

    [Tooltip("ID of the world this level belongs to")]
    public string worldId;

    [Tooltip("Order of this level within its world (0-based)")]
    public int orderInWorld;

    [Header("Grid Configuration")]
    [Tooltip("Number of columns in the grid")]
    public int gridWidth = 10;

    [Tooltip("Number of rows in the grid")]
    public int gridHeight = 10;

    [Tooltip("Size of each grid cell in world units")]
    public float cellSize = 1.0f;

    [Header("Level Data")]
    [Tooltip("JSON string containing the complete level data (blocks, lems, inventory, etc.)")]
    [TextArea(5, 20)]
    public string levelDataJson;

    /// <summary>
    /// Converts the JSON level data string to a LevelData object for runtime use.
    /// </summary>
    /// <returns>The deserialized LevelData, or null if parsing fails</returns>
    public LevelData ToLevelData()
    {
        if (string.IsNullOrEmpty(levelDataJson))
        {
            Debug.LogError($"[LevelDefinition] Level '{levelId}' has no JSON data!");
            return null;
        }

        try
        {
            LevelData data = JsonUtility.FromJson<LevelData>(levelDataJson);

            // Apply asset defaults only when JSON is missing/invalid.
            if (data != null)
            {
                if (data.gridWidth <= 0) data.gridWidth = gridWidth;
                if (data.gridHeight <= 0) data.gridHeight = gridHeight;
                if (data.cellSize <= 0f) data.cellSize = cellSize;
                if (string.IsNullOrEmpty(data.levelName)) data.levelName = levelName;
            }

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelDefinition] Failed to parse level data for '{levelId}': {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Populates this LevelDefinition from a LevelData object.
    /// Useful for saving levels from the designer to assets.
    /// </summary>
    /// <param name="data">The LevelData to serialize</param>
    public void FromLevelData(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("[LevelDefinition] Cannot populate from null LevelData");
            return;
        }

        gridWidth = data.gridWidth;
        gridHeight = data.gridHeight;
        cellSize = data.cellSize;
        levelDataJson = JsonUtility.ToJson(data, true);

        if (!string.IsNullOrEmpty(data.levelName))
        {
            levelName = data.levelName;
        }
    }

    /// <summary>
    /// Saves the provided LevelData into this asset and persists it in the Unity Editor.
    /// In builds, this updates the in-memory asset only (designer workflow is editor-only).
    /// </summary>
    /// <param name="data">The LevelData to serialize</param>
    /// <returns>True if data was applied; false if data was null</returns>
    public bool SaveFromLevelData(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("[LevelDefinition] Cannot save from null LevelData");
            return false;
        }

        FromLevelData(data);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        return true;
    }

    /// <summary>
    /// Validates this level definition has all required data.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate()
    {
        bool isValid = true;

        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has no levelId");
            isValid = false;
        }

        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has no levelName");
            isValid = false;
        }

        if (string.IsNullOrEmpty(worldId))
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has no worldId");
            isValid = false;
        }

        if (string.IsNullOrEmpty(levelDataJson))
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has no level data JSON");
            isValid = false;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has invalid grid dimensions");
            isValid = false;
        }

        return isValid;
    }
}
