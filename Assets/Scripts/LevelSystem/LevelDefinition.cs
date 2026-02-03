using UnityEngine;

/// <summary>
/// ScriptableObject that defines a single level in the game.
/// Contains metadata, grid configuration, and serialized LevelData.
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

    [Header("Grid Visuals")]
    [Tooltip("Thickness of grid lines")]
    public float gridLineWidth = RenderingConstants.GRID_LINE_WIDTH;

    [Tooltip("Grid line color (alpha included)")]
    public Color gridLineColor = new Color(BlockColors.GridLine.r, BlockColors.GridLine.g, BlockColors.GridLine.b, 0.2f);

    [Header("Cursor Colors")]
    public Color cursorPlaceableColor = BlockColors.CursorPlaceable;
    public Color cursorEditableColor = BlockColors.CursorEditable;
    public Color cursorNonPlaceableColor = BlockColors.CursorNonPlaceable;

    [Header("Level Data")]
    [SerializeField, HideInInspector] private LevelData levelData = new LevelData();

    public LevelData GetLevelData()
    {
        if (levelData == null)
        {
            levelData = new LevelData();
        }
        if (levelData.gridWidth <= 0) levelData.gridWidth = 10;
        if (levelData.gridHeight <= 0) levelData.gridHeight = 10;
        return levelData;
    }

    public LevelData GetRuntimeLevelData()
    {
        LevelData data = GetLevelData().Clone();
        if (data.gridWidth <= 0) data.gridWidth = 10;
        if (data.gridHeight <= 0) data.gridHeight = 10;
        if (string.IsNullOrEmpty(data.levelName)) data.levelName = levelName;
        return data;
    }

    public void SetLevelData(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("[LevelDefinition] Cannot assign null LevelData");
            return;
        }
        data.levelName = levelName;
        levelData = data;
    }

    public bool SaveFromLevelData(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("[LevelDefinition] Cannot save from null LevelData");
            return false;
        }

        data.levelName = levelName;
        levelData = data.Clone();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>✓ SAVED '{levelName}' ({levelId})</color>");
        Debug.Log($"  Grid: {data.gridWidth}x{data.gridHeight} (1.0 unit cells)");
        Debug.Log($"  Blocks: {data.blocks?.Count ?? 0} placed, {data.permanentBlocks?.Count ?? 0} permanent");
        Debug.Log($"  Inventory: {data.inventoryEntries?.Count ?? 0} entries");
        Debug.Log($"  Lems: {data.lems?.Count ?? 0}");
        Debug.Log($"  Placeable Spaces: {data.placeableSpaceIndices?.Count ?? 0}");
        Debug.Log($"  Key States: {data.keyStates?.Count ?? 0}");

        if (data.cameraSettings != null)
        {
            var cam = data.cameraSettings;
            Debug.Log($"  Camera: Dist={cam.cameraDistance:F1}, Margin={cam.gridMargin:F2}");
            Debug.Log($"  Camera Clip: Near={cam.nearClipPlane:F2}, Far={cam.farClipPlane:F0}");
        }
        else
        {
            Debug.LogWarning("  Camera: No camera settings saved");
        }
#endif

        return true;
    }

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

        LevelData data = GetLevelData();
        if (data == null)
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has no LevelData");
            isValid = false;
        }
        else if (data.gridWidth <= 0 || data.gridHeight <= 0)
        {
            Debug.LogWarning($"[LevelDefinition] '{name}' has invalid grid dimensions");
            isValid = false;
        }

        return isValid;
    }
}
