using UnityEngine;

/// <summary>
/// ScriptableObject that defines a world (collection of levels) in the game.
/// Worlds are unlocked sequentially by completing all levels in the previous world.
///
/// USAGE:
/// - Create in Unity: Right-click in Project window -> Create -> AWITP -> World Definition
/// - Configure metadata: worldId, worldName, description, orderInGame
/// - Drag LevelDefinition assets into the levels array
///
/// PROGRESSION:
/// - First world (orderInGame = 0) is always unlocked
/// - Subsequent worlds unlock when all levels in previous world are complete
/// - WorldManager handles unlock logic
/// </summary>
[CreateAssetMenu(fileName = "World", menuName = "AWITP/World Definition")]
public class WorldData : ScriptableObject
{
    [Header("Metadata")]
    [Tooltip("Unique identifier for this world (e.g., 'world_tutorial', 'world_basics')")]
    public string worldId;

    [Tooltip("Display name shown to players (e.g., 'Tutorial Island', 'The Basics')")]
    public string worldName;

    [Tooltip("Description of this world shown in the world select screen")]
    [TextArea(2, 5)]
    public string description;

    [Tooltip("Order of this world in the game progression (0-based). Lower numbers are earlier.")]
    public int orderInGame;

    [Header("Visual")]
    [Tooltip("Optional icon for this world in the world select UI")]
    public Sprite worldIcon;

    [Tooltip("Optional color theme for this world")]
    public Color themeColor = Color.white;

    [Header("Levels")]
    [Tooltip("Levels in this world, in order. Drag LevelDefinition assets here.")]
    public LevelDefinition[] levels;

    /// <summary>
    /// Gets the IDs of all levels in this world.
    /// </summary>
    /// <returns>Array of level IDs</returns>
    public string[] GetLevelIds()
    {
        if (levels == null || levels.Length == 0)
        {
            return new string[0];
        }

        string[] ids = new string[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null)
            {
                ids[i] = levels[i].levelId;
            }
            else
            {
                ids[i] = string.Empty;
                Debug.LogWarning($"[WorldData] World '{worldId}' has null level at index {i}");
            }
        }
        return ids;
    }

    /// <summary>
    /// Gets a level by its index within this world.
    /// </summary>
    /// <param name="index">0-based index</param>
    /// <returns>The LevelDefinition at that index, or null if invalid</returns>
    public LevelDefinition GetLevelAt(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
        {
            return null;
        }
        return levels[index];
    }

    /// <summary>
    /// Gets a level by its ID.
    /// </summary>
    /// <param name="levelId">The level ID to find</param>
    /// <returns>The matching LevelDefinition, or null if not found</returns>
    public LevelDefinition GetLevelById(string levelId)
    {
        if (levels == null || string.IsNullOrEmpty(levelId))
        {
            return null;
        }

        foreach (var level in levels)
        {
            if (level != null && level.levelId == levelId)
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the index of a level within this world.
    /// </summary>
    /// <param name="levelId">The level ID to find</param>
    /// <returns>The index (0-based), or -1 if not found</returns>
    public int GetLevelIndex(string levelId)
    {
        if (levels == null || string.IsNullOrEmpty(levelId))
        {
            return -1;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].levelId == levelId)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the next level after the specified level ID.
    /// </summary>
    /// <param name="currentLevelId">The current level ID</param>
    /// <returns>The next LevelDefinition, or null if at end of world</returns>
    public LevelDefinition GetNextLevel(string currentLevelId)
    {
        int currentIndex = GetLevelIndex(currentLevelId);
        if (currentIndex < 0 || currentIndex >= levels.Length - 1)
        {
            return null;
        }
        return levels[currentIndex + 1];
    }

    /// <summary>
    /// Gets the total number of levels in this world.
    /// </summary>
    public int LevelCount => levels != null ? levels.Length : 0;

    /// <summary>
    /// Validates this world definition.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate()
    {
        bool isValid = true;

        if (string.IsNullOrEmpty(worldId))
        {
            Debug.LogWarning($"[WorldData] '{name}' has no worldId");
            isValid = false;
        }

        if (string.IsNullOrEmpty(worldName))
        {
            Debug.LogWarning($"[WorldData] '{name}' has no worldName");
            isValid = false;
        }

        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning($"[WorldData] '{name}' has no levels");
            isValid = false;
        }
        else
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == null)
                {
                    Debug.LogWarning($"[WorldData] '{name}' has null level at index {i}");
                    isValid = false;
                }
                else if (!levels[i].Validate())
                {
                    isValid = false;
                }
            }
        }

        return isValid;
    }
}
