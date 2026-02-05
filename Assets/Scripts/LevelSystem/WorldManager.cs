using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for organizing and navigating worlds (level collections).
///
/// RESPONSIBILITIES:
/// - Load and cache WorldData assets from Resources
/// - Track world unlock status
/// - Provide world navigation (current world, next world)
/// - Coordinate with ProgressManager for unlock state
///
/// USAGE:
/// - WorldManager.Instance.GetWorld("world_tutorial")
/// - WorldManager.Instance.GetAllWorlds()
/// - WorldManager.Instance.IsWorldUnlocked("world_basics")
///
/// EVENTS:
/// - OnWorldUnlocked: Fired when a new world is unlocked
/// </summary>
public class WorldManager : MonoBehaviour
{
    #region Singleton

    private static WorldManager _instance;

    /// <summary>
    /// Singleton instance. Persists across scene loads.
    /// </summary>
    public static WorldManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = ServiceRegistry.TryGet<WorldManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("WorldManager");
                    _instance = go.AddComponent<WorldManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Events

    /// <summary>Fired when a new world is unlocked.</summary>
    public event Action<WorldData> OnWorldUnlocked;

    #endregion

    #region Fields

    [Header("Worlds")]
    [Tooltip("All worlds in the game, loaded from Resources")]
    [SerializeField] private List<WorldData> _worlds = new List<WorldData>();

    [Header("Current State")]
    [Tooltip("ID of the currently selected world")]
    [SerializeField] private string _currentWorldId;

    // Cache for fast lookup
    private Dictionary<string, WorldData> _worldCache = new Dictionary<string, WorldData>();
    private bool _worldsLoaded = false;

    #endregion

    #region Properties

    /// <summary>
    /// Gets all worlds in order.
    /// </summary>
    public IReadOnlyList<WorldData> Worlds => _worlds;

    /// <summary>
    /// Gets all worlds as an array (for UI iteration).
    /// </summary>
    public WorldData[] allWorlds => _worlds.ToArray();

    /// <summary>
    /// Gets the number of worlds.
    /// </summary>
    public int WorldCount => _worlds.Count;

    /// <summary>
    /// Gets the current world ID.
    /// </summary>
    public string CurrentWorldId => _currentWorldId;

    /// <summary>
    /// Gets the current world.
    /// </summary>
    public WorldData CurrentWorld => GetWorld(_currentWorldId);

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[WorldManager] Duplicate instance detected, destroying.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Load all worlds from Resources
        LoadWorlds();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region World Loading

    /// <summary>
    /// Loads all WorldData assets from Resources.
    /// </summary>
    private void LoadWorlds()
    {
        if (_worldsLoaded)
        {
            return;
        }

        try
        {
            // Clear existing
            _worlds.Clear();
            _worldCache.Clear();

            // Load from Resources/Levels/Worlds
            WorldData[] loadedWorlds = Resources.LoadAll<WorldData>("Levels/Worlds");

            if (loadedWorlds.Length == 0)
            {
                // Try alternate path
                loadedWorlds = Resources.LoadAll<WorldData>("Levels");
            }

            // Sort by orderInGame
            System.Array.Sort(loadedWorlds, (a, b) => a.orderInGame.CompareTo(b.orderInGame));

            // Add to list and cache
            foreach (var world in loadedWorlds)
            {
                if (world != null && !string.IsNullOrEmpty(world.worldId))
                {
                    _worlds.Add(world);
                    _worldCache[world.worldId] = world;
                }
            }

            _worldsLoaded = true;
            Debug.Log($"[WorldManager] Loaded {_worlds.Count} worlds");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WorldManager] Error loading worlds: {ex.Message}");
        }
    }

    /// <summary>
    /// Reloads all worlds from Resources.
    /// </summary>
    public void ReloadWorlds()
    {
        _worldsLoaded = false;
        LoadWorlds();
    }

    #endregion

    #region World Access

    /// <summary>
    /// Gets a world by its ID.
    /// </summary>
    /// <param name="worldId">The world ID to find</param>
    /// <returns>The WorldData, or null if not found</returns>
    public WorldData GetWorld(string worldId)
    {
        if (string.IsNullOrEmpty(worldId))
        {
            return null;
        }

        if (_worldCache.TryGetValue(worldId, out WorldData world))
        {
            return world;
        }

        // Try to find in list (in case cache is stale)
        foreach (var w in _worlds)
        {
            if (w.worldId == worldId)
            {
                _worldCache[worldId] = w;
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a world by its order index.
    /// </summary>
    /// <param name="orderInGame">The 0-based order index</param>
    /// <returns>The WorldData, or null if invalid index</returns>
    public WorldData GetWorldByOrder(int orderInGame)
    {
        foreach (var world in _worlds)
        {
            if (world.orderInGame == orderInGame)
            {
                return world;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all worlds in order.
    /// </summary>
    /// <returns>Array of all WorldData assets</returns>
    public WorldData[] GetAllWorlds()
    {
        return _worlds.ToArray();
    }

    /// <summary>
    /// Gets all unlocked worlds.
    /// </summary>
    /// <returns>Array of unlocked WorldData assets</returns>
    public WorldData[] GetUnlockedWorlds()
    {
        List<WorldData> unlocked = new List<WorldData>();
        foreach (var world in _worlds)
        {
            if (IsWorldUnlocked(world.worldId))
            {
                unlocked.Add(world);
            }
        }
        return unlocked.ToArray();
    }

    #endregion

    #region World Navigation

    /// <summary>
    /// Sets the current world.
    /// </summary>
    /// <param name="worldId">The world ID to set as current</param>
    public void SetCurrentWorld(string worldId)
    {
        if (GetWorld(worldId) != null)
        {
            _currentWorldId = worldId;
            Debug.Log($"[WorldManager] Current world set to: {worldId}");
        }
        else
        {
            Debug.LogWarning($"[WorldManager] World not found: {worldId}");
        }
    }

    /// <summary>
    /// Gets the next world after the specified world.
    /// </summary>
    /// <param name="currentWorldId">The current world ID</param>
    /// <returns>The next WorldData, or null if at end</returns>
    public WorldData GetNextWorld(string currentWorldId)
    {
        WorldData current = GetWorld(currentWorldId);
        if (current == null) return null;

        return GetWorldByOrder(current.orderInGame + 1);
    }

    /// <summary>
    /// Gets the previous world before the specified world.
    /// </summary>
    /// <param name="currentWorldId">The current world ID</param>
    /// <returns>The previous WorldData, or null if at beginning</returns>
    public WorldData GetPreviousWorld(string currentWorldId)
    {
        WorldData current = GetWorld(currentWorldId);
        if (current == null || current.orderInGame == 0) return null;

        return GetWorldByOrder(current.orderInGame - 1);
    }

    /// <summary>
    /// Gets the first world (usually tutorial).
    /// </summary>
    /// <returns>The first WorldData, or null if no worlds</returns>
    public WorldData GetFirstWorld()
    {
        return _worlds.Count > 0 ? _worlds[0] : null;
    }

    /// <summary>
    /// Gets the last world.
    /// </summary>
    /// <returns>The last WorldData, or null if no worlds</returns>
    public WorldData GetLastWorld()
    {
        return _worlds.Count > 0 ? _worlds[_worlds.Count - 1] : null;
    }

    #endregion

    #region World Unlock Status

    /// <summary>
    /// Checks if a world is unlocked.
    /// First world is always unlocked.
    /// Subsequent worlds unlock when all levels in previous world are complete.
    /// </summary>
    /// <param name="worldId">The world ID to check</param>
    /// <returns>True if unlocked</returns>
    public bool IsWorldUnlocked(string worldId)
    {
        WorldData world = GetWorld(worldId);
        if (world == null) return false;

        // First world is always unlocked
        if (world.orderInGame == 0)
        {
            return true;
        }

        // Check if saved as unlocked
        if (ProgressManager.Instance != null &&
            ProgressManager.Instance.IsWorldUnlocked(worldId))
        {
            return true;
        }

        // Check if previous world is complete
        WorldData previousWorld = GetWorldByOrder(world.orderInGame - 1);
        if (previousWorld != null && IsWorldComplete(previousWorld.worldId))
        {
            // Unlock this world
            UnlockWorld(worldId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if all levels in a world are complete.
    /// </summary>
    /// <param name="worldId">The world ID to check</param>
    /// <returns>True if all levels are complete</returns>
    public bool IsWorldComplete(string worldId)
    {
        WorldData world = GetWorld(worldId);
        if (world == null || world.LevelCount == 0) return false;

        if (ProgressManager.Instance == null) return false;

        foreach (var level in world.levels)
        {
            if (level == null) continue;
            if (!ProgressManager.Instance.IsLevelComplete(level.levelId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Unlocks a world and saves progress.
    /// </summary>
    /// <param name="worldId">The world ID to unlock</param>
    public void UnlockWorld(string worldId)
    {
        WorldData world = GetWorld(worldId);
        if (world == null) return;

        // Save to progress
        if (ProgressManager.Instance != null)
        {
            if (!ProgressManager.Instance.IsWorldUnlocked(worldId))
            {
                ProgressManager.Instance.UnlockWorld(worldId);
                Debug.Log($"[WorldManager] World unlocked: {world.worldName}");

                // Fire event
                OnWorldUnlocked?.Invoke(world);
            }
        }
    }

    /// <summary>
    /// Gets the number of completed levels in a world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <returns>Number of completed levels</returns>
    public int GetCompletedLevelCount(string worldId)
    {
        WorldData world = GetWorld(worldId);
        if (world == null || ProgressManager.Instance == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var level in world.levels)
        {
            if (level != null && ProgressManager.Instance.IsLevelComplete(level.levelId))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets the completion percentage for a world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <returns>Percentage (0-100)</returns>
    public float GetWorldCompletionPercent(string worldId)
    {
        WorldData world = GetWorld(worldId);
        if (world == null || world.LevelCount == 0)
        {
            return 0f;
        }

        int completed = GetCompletedLevelCount(worldId);
        return (completed / (float)world.LevelCount) * 100f;
    }

    #endregion

    #region Level Access

    /// <summary>
    /// Gets a level from any world by its ID.
    /// </summary>
    /// <param name="levelId">The level ID to find</param>
    /// <returns>The LevelDefinition, or null if not found</returns>
    public LevelDefinition GetLevel(string levelId)
    {
        foreach (var world in _worlds)
        {
            LevelDefinition level = world.GetLevelById(levelId);
            if (level != null)
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the world that contains a specific level.
    /// </summary>
    /// <param name="levelId">The level ID</param>
    /// <returns>The containing WorldData, or null</returns>
    public WorldData GetWorldForLevel(string levelId)
    {
        foreach (var world in _worlds)
        {
            if (world.GetLevelById(levelId) != null)
            {
                return world;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the total number of levels across all worlds.
    /// </summary>
    public int GetTotalLevelCount()
    {
        int count = 0;
        foreach (var world in _worlds)
        {
            count += world.LevelCount;
        }
        return count;
    }

    /// <summary>
    /// Gets the total number of completed levels across all worlds.
    /// </summary>
    public int GetTotalCompletedLevelCount()
    {
        if (ProgressManager.Instance == null) return 0;

        int count = 0;
        foreach (var world in _worlds)
        {
            count += GetCompletedLevelCount(world.worldId);
        }
        return count;
    }

    #endregion
}
