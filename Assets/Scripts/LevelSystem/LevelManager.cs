using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for loading, instantiating, and managing levels.
///
/// RESPONSIBILITIES:
/// - Load levels from LevelDefinition assets
/// - Instantiate level content (blocks, lems, inventory)
/// - Track current level state
/// - Handle level completion and transitions
/// - Coordinate with GridManager, BlockInventory, and ProgressManager
///
/// USAGE:
/// - LevelManager.Instance.LoadLevel("tutorial_01")
/// - LevelManager.Instance.RestartLevel()
/// - LevelManager.Instance.IsLevelComplete()
///
/// EVENTS:
/// - OnLevelLoaded: Fired when a level finishes loading
/// - OnLevelComplete: Fired when win condition is met
/// - OnLevelFailed: Fired when level fails (all Lems dead, etc.)
/// </summary>
public class LevelManager : MonoBehaviour
{
    #region Singleton

    private static LevelManager _instance;

    /// <summary>
    /// Singleton instance. Persists across scene loads.
    /// </summary>
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("LevelManager");
                    _instance = go.AddComponent<LevelManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Events

    /// <summary>Fired when a level finishes loading.</summary>
    public event Action<LevelDefinition> OnLevelLoaded;

    /// <summary>Fired when the win condition is met.</summary>
    public event Action<LevelDefinition> OnLevelComplete;

    /// <summary>Fired when the level fails (all Lems dead, time out, etc.).</summary>
    public event Action<LevelDefinition> OnLevelFailed;

    /// <summary>Fired when a level is unloaded.</summary>
    public event Action OnLevelUnloaded;

    #endregion

    #region Fields

    [Header("Current Level")]
    [Tooltip("The currently loaded level definition")]
    [SerializeField] private LevelDefinition _currentLevelDef;

    [Tooltip("The deserialized level data for the current level")]
    private LevelData _currentLevelData;

    [Header("References")]
    [Tooltip("Reference to GridManager (auto-found if null)")]
    [SerializeField] private GridManager _gridManager;

    [Tooltip("Reference to BlockInventory (auto-found if null)")]
    [SerializeField] private BlockInventory _blockInventory;

    [Header("State")]
    [Tooltip("Whether a level is currently loaded")]
    [SerializeField] private bool _levelLoaded = false;

    [Tooltip("Whether the level has been completed this session")]
    [SerializeField] private bool _levelCompletedThisSession = false;

    [Tooltip("Time when the level was started (for timing)")]
    private float _levelStartTime;

    [Tooltip("Number of blocks placed by player")]
    private int _blocksPlacedCount;

    // Cache for loaded level definitions
    private Dictionary<string, LevelDefinition> _levelDefCache = new Dictionary<string, LevelDefinition>();

    #endregion

    #region Properties

    /// <summary>
    /// The currently loaded level definition.
    /// </summary>
    public LevelDefinition CurrentLevelDef => _currentLevelDef;

    /// <summary>
    /// The current level's deserialized data.
    /// </summary>
    public LevelData CurrentLevelData => _currentLevelData;

    /// <summary>
    /// Whether a level is currently loaded.
    /// </summary>
    public bool IsLevelLoaded => _levelLoaded;

    /// <summary>
    /// Whether the current level has been completed.
    /// </summary>
    public bool IsCurrentLevelComplete => _levelCompletedThisSession;

    /// <summary>
    /// Time elapsed since level started (seconds).
    /// </summary>
    public float ElapsedTime => _levelLoaded ? Time.time - _levelStartTime : 0f;

    /// <summary>
    /// Number of blocks placed by the player.
    /// </summary>
    public int BlocksPlaced => _blocksPlacedCount;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[LevelManager] Duplicate instance detected, destroying.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-find references if not set
        FindReferences();
    }

    private void Start()
    {
        // Re-find references in case they weren't available in Awake
        FindReferences();

        if (!_levelLoaded && _currentLevelDef != null)
        {
            LoadLevel(_currentLevelDef);
        }
    }

    private void Update()
    {
        // Check win condition in play mode
        if (_levelLoaded && !_levelCompletedThisSession)
        {
            // Only check in play mode
            BuilderController builderController = UnityEngine.Object.FindAnyObjectByType<BuilderController>();
            if (builderController != null && builderController.currentMode == GameMode.Play)
            {
                if (CheckLevelComplete())
                {
                    HandleLevelComplete();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region Level Loading

    /// <summary>
    /// Loads a level by its ID.
    /// </summary>
    /// <param name="levelId">Unique level identifier</param>
    /// <returns>True if level loaded successfully</returns>
    public bool LoadLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogError("[LevelManager] Cannot load level: levelId is null or empty");
            return false;
        }

        // Try to find the LevelDefinition
        LevelDefinition levelDef = FindLevelDefinition(levelId);
        if (levelDef == null)
        {
            Debug.LogError($"[LevelManager] Level not found: {levelId}");
            return false;
        }

        return LoadLevel(levelDef);
    }

    /// <summary>
    /// Loads a level from a LevelDefinition asset.
    /// </summary>
    /// <param name="levelDef">The level definition to load</param>
    /// <returns>True if level loaded successfully</returns>
    public bool LoadLevel(LevelDefinition levelDef)
    {
        if (levelDef == null)
        {
            Debug.LogError("[LevelManager] Cannot load null LevelDefinition");
            return false;
        }

        // Validate the level definition
        if (!levelDef.Validate())
        {
            Debug.LogWarning($"[LevelManager] Level '{levelDef.levelId}' has validation warnings");
        }

        // Parse the level data
        LevelData levelData = levelDef.ToLevelData();
        if (levelData == null)
        {
            Debug.LogError($"[LevelManager] Failed to parse level data for '{levelDef.levelId}'");
            return false;
        }

        // Unload any existing level
        UnloadLevel();

        // Store references
        _currentLevelDef = levelDef;
        _currentLevelData = levelData;

        // Ensure references are valid
        FindReferences();

        // Instantiate the level
        if (!InstantiateLevel(levelData))
        {
            Debug.LogError($"[LevelManager] Failed to instantiate level '{levelDef.levelId}'");
            return false;
        }

        // Mark level as loaded
        _levelLoaded = true;
        _levelCompletedThisSession = false;
        _levelStartTime = Time.time;
        _blocksPlacedCount = 0;

        // Update progress tracking
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.SetCurrentLevel(levelDef.levelId);
            LevelProgress progress = ProgressManager.Instance.GetLevelProgress(levelDef.levelId);
            if (progress != null)
            {
                progress.RecordAttempt();
                ProgressManager.Instance.SaveProgress();
            }
        }

        Debug.Log($"[LevelManager] Level loaded: {levelDef.levelName} ({levelDef.levelId})");

        // Fire event
        OnLevelLoaded?.Invoke(levelDef);

        return true;
    }

    /// <summary>
    /// Instantiates all level content from LevelData.
    /// </summary>
    private bool InstantiateLevel(LevelData data)
    {
        if (_gridManager == null)
        {
            Debug.LogError("[LevelManager] GridManager not found!");
            return false;
        }

        try
        {
            // Restore the level data through GridManager
            _gridManager.RestoreLevelData(data);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LevelManager] Error instantiating level: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Unloads the current level and clears all content.
    /// </summary>
    public void UnloadLevel()
    {
        if (!_levelLoaded)
        {
            return;
        }

        try
        {
            // Clear grid content
            if (_gridManager != null)
            {
                // Use reflection or internal method to clear
                // GridManager doesn't expose ClearAllBlocks publicly, so we'll restore empty data
                LevelData emptyData = new LevelData
                {
                    gridWidth = _gridManager.gridWidth,
                    gridHeight = _gridManager.gridHeight,
                    cellSize = _gridManager.cellSize
                };
                _gridManager.RestoreLevelData(emptyData);
            }

            // Destroy any remaining Lems
            LemController[] lems = UnityEngine.Object.FindObjectsByType<LemController>(FindObjectsSortMode.None);
            foreach (var lem in lems)
            {
                if (lem != null)
                {
                    Destroy(lem.gameObject);
                }
            }

            // Reset inventory
            if (_blockInventory != null)
            {
                _blockInventory.ResetInventory();
            }

            // Clear state
            _currentLevelData = null;
            _currentLevelDef = null;
            _levelLoaded = false;
            _levelCompletedThisSession = false;

            Debug.Log("[LevelManager] Level unloaded");

            // Fire event
            OnLevelUnloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LevelManager] Error unloading level: {ex.Message}");
        }
    }

    /// <summary>
    /// Restarts the current level.
    /// </summary>
    public void RestartLevel()
    {
        if (_currentLevelDef != null)
        {
            Debug.Log($"[LevelManager] Restarting level: {_currentLevelDef.levelName}");
            LoadLevel(_currentLevelDef);
        }
        else
        {
            Debug.LogWarning("[LevelManager] No level to restart");
        }
    }

    #endregion

    #region Level Completion

    /// <summary>
    /// Checks if the level's win condition is met.
    /// Win condition: All locks are filled with keys.
    /// </summary>
    public bool CheckLevelComplete()
    {
        if (!_levelLoaded) return false;

        // Find all locks
        LockBlock[] locks = UnityEngine.Object.FindObjectsByType<LockBlock>(FindObjectsSortMode.None);

        // If no locks, level can't be completed (or is a special case)
        if (locks.Length == 0)
        {
            return false;
        }

        // Check if all locks are filled
        foreach (var lockBlock in locks)
        {
            if (lockBlock == null) continue;
            if (!lockBlock.IsFilled())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Handles level completion: saves progress, fires events.
    /// </summary>
    private void HandleLevelComplete()
    {
        if (_levelCompletedThisSession) return;

        _levelCompletedThisSession = true;
        float completionTime = ElapsedTime;

        Debug.Log($"[LevelManager] Level Complete! Time: {completionTime:F2}s, Blocks: {_blocksPlacedCount}");

        // Save progress
        if (ProgressManager.Instance != null && _currentLevelDef != null)
        {
            ProgressManager.Instance.MarkLevelComplete(_currentLevelDef.levelId, completionTime, _blocksPlacedCount);
        }

        // Fire event
        OnLevelComplete?.Invoke(_currentLevelDef);
    }

    /// <summary>
    /// Manually triggers level completion (for testing or special cases).
    /// </summary>
    public void TriggerLevelComplete()
    {
        HandleLevelComplete();
    }

    /// <summary>
    /// Manually triggers level failure.
    /// </summary>
    public void TriggerLevelFailed()
    {
        if (!_levelLoaded || _levelCompletedThisSession) return;

        Debug.Log("[LevelManager] Level Failed");
        OnLevelFailed?.Invoke(_currentLevelDef);
    }

    /// <summary>
    /// Records that a block was placed by the player.
    /// </summary>
    public void RecordBlockPlaced()
    {
        _blocksPlacedCount++;
    }

    #endregion

    #region Level Navigation

    /// <summary>
    /// Loads the next level in the current world.
    /// </summary>
    /// <returns>True if next level exists and was loaded</returns>
    public bool LoadNextLevel()
    {
        if (_currentLevelDef == null)
        {
            Debug.LogWarning("[LevelManager] No current level to get next from");
            return false;
        }

        // Find the world this level belongs to
        if (WorldManager.Instance != null)
        {
            WorldData world = WorldManager.Instance.GetWorld(_currentLevelDef.worldId);
            if (world != null)
            {
                LevelDefinition nextLevel = world.GetNextLevel(_currentLevelDef.levelId);
                if (nextLevel != null)
                {
                    return LoadLevel(nextLevel);
                }
            }
        }

        Debug.Log("[LevelManager] No next level available");
        return false;
    }

    /// <summary>
    /// Returns to the world map scene.
    /// </summary>
    public void ReturnToWorldMap()
    {
        UnloadLevel();
        SceneManager.LoadScene("WorldMap");
    }

    /// <summary>
    /// Returns to the main menu scene.
    /// </summary>
    public void ReturnToMainMenu()
    {
        UnloadLevel();
        SceneManager.LoadScene("MainMenu");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds a LevelDefinition by its ID.
    /// First checks cache, then Resources.
    /// </summary>
    private LevelDefinition FindLevelDefinition(string levelId)
    {
        // Check cache first
        if (_levelDefCache.TryGetValue(levelId, out LevelDefinition cached))
        {
            return cached;
        }

        // Try to load from Resources
        // Convention: Resources/Levels/LevelDefinitions/{levelId}
        LevelDefinition levelDef = Resources.Load<LevelDefinition>($"Levels/LevelDefinitions/{levelId}");

        if (levelDef == null)
        {
            // Try alternate path without subfolder
            levelDef = Resources.Load<LevelDefinition>($"Levels/{levelId}");
        }

        if (levelDef == null)
        {
            // Search all loaded LevelDefinitions
            LevelDefinition[] allLevels = Resources.LoadAll<LevelDefinition>("Levels");
            foreach (var level in allLevels)
            {
                if (level.levelId == levelId)
                {
                    levelDef = level;
                    break;
                }
            }
        }

        // Cache the result
        if (levelDef != null)
        {
            _levelDefCache[levelId] = levelDef;
        }

        return levelDef;
    }

    /// <summary>
    /// Finds and caches references to required components.
    /// </summary>
    private void FindReferences()
    {
        if (_gridManager == null)
        {
            _gridManager = UnityEngine.Object.FindAnyObjectByType<GridManager>();
        }

        if (_blockInventory == null)
        {
            _blockInventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        }
    }

    /// <summary>
    /// Clears the level definition cache.
    /// </summary>
    public void ClearCache()
    {
        _levelDefCache.Clear();
    }

    /// <summary>
    /// Gets the current level's ID.
    /// </summary>
    public string GetCurrentLevelId()
    {
        return _currentLevelDef != null ? _currentLevelDef.levelId : null;
    }

    /// <summary>
    /// Gets the current level's world ID.
    /// </summary>
    public string GetCurrentWorldId()
    {
        return _currentLevelDef != null ? _currentLevelDef.worldId : null;
    }

    #endregion
}
