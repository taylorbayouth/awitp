using UnityEngine;
using System;
using System.IO;

/// <summary>
/// Singleton manager for tracking and persisting player progress.
///
/// RESPONSIBILITIES:
/// - Track completed levels
/// - Track unlocked worlds
/// - Track per-level statistics (attempts, best time, etc.)
/// - Save/load progress to disk
/// - Coordinate with LevelManager and WorldManager
///
/// PERSISTENCE:
/// - Progress saved as JSON to Application.persistentDataPath/progress.json
/// - Auto-saves after significant changes
/// - Supports manual save/load
///
/// USAGE:
/// - ProgressManager.Instance.IsLevelComplete("tutorial_01")
/// - ProgressManager.Instance.MarkLevelComplete("tutorial_01", time, blocks)
/// - ProgressManager.Instance.SaveProgress()
/// </summary>
public class ProgressManager : MonoBehaviour
{
    #region Singleton

    private static ProgressManager _instance;

    /// <summary>
    /// Singleton instance. Persists across scene loads.
    /// </summary>
    public static ProgressManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<ProgressManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ProgressManager");
                    _instance = go.AddComponent<ProgressManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Events

    /// <summary>Fired when a level is completed for the first time.</summary>
    public event Action<string> OnLevelFirstComplete;

    /// <summary>Fired when a world is unlocked.</summary>
    public event Action<string> OnWorldUnlocked;

    /// <summary>Fired when progress is loaded.</summary>
    public event Action OnProgressLoaded;

    /// <summary>Fired when progress is saved.</summary>
    public event Action OnProgressSaved;

    #endregion

    #region Fields

    [Header("Progress Data")]
    [SerializeField] private GameProgressData _progressData;

    [Header("Settings")]
    [Tooltip("Auto-save after each level completion")]
    [SerializeField] private bool _autoSave = true;

    [Tooltip("File name for progress save")]
    [SerializeField] private string _saveFileName = "progress.json";

    // Save path
    private string _savePath;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current progress data.
    /// </summary>
    public GameProgressData ProgressData => _progressData;

    /// <summary>
    /// Gets the number of completed levels.
    /// </summary>
    public int CompletedLevelCount => _progressData?.CompletedLevelCount ?? 0;

    /// <summary>
    /// Gets the number of unlocked worlds.
    /// </summary>
    public int UnlockedWorldCount => _progressData?.UnlockedWorldCount ?? 0;

    /// <summary>
    /// Gets the total play time in seconds.
    /// </summary>
    public float TotalPlayTime => _progressData?.totalPlayTime ?? 0f;

    /// <summary>
    /// Gets the current level ID.
    /// </summary>
    public string CurrentLevelId => _progressData?.currentLevelId;

    /// <summary>
    /// Gets the current world ID.
    /// </summary>
    public string CurrentWorldId => _progressData?.currentWorldId;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[ProgressManager] Duplicate instance detected, destroying.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize save path
        _savePath = Path.Combine(Application.persistentDataPath, _saveFileName);

        // Load existing progress
        LoadProgress();
    }

    private void OnApplicationQuit()
    {
        // Save progress when quitting
        SaveProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Save on mobile when app goes to background
        if (pauseStatus)
        {
            SaveProgress();
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

    #region Level Progress

    /// <summary>
    /// Checks if a level has been completed.
    /// </summary>
    /// <param name="levelId">The level ID to check</param>
    /// <returns>True if completed</returns>
    public bool IsLevelComplete(string levelId)
    {
        return _progressData != null && _progressData.IsLevelCompleted(levelId);
    }

    /// <summary>
    /// Marks a level as completed.
    /// </summary>
    /// <param name="levelId">The level ID</param>
    /// <param name="completionTime">Time taken to complete (seconds)</param>
    /// <param name="blocksUsed">Number of blocks used</param>
    public void MarkLevelComplete(string levelId, float completionTime = 0f, int blocksUsed = 0)
    {
        if (_progressData == null || string.IsNullOrEmpty(levelId))
        {
            return;
        }

        bool wasComplete = _progressData.IsLevelCompleted(levelId);

        // Update progress
        _progressData.MarkLevelCompleted(levelId);

        // Update level-specific stats
        LevelProgress levelProgress = _progressData.GetOrCreateProgress(levelId);
        levelProgress.RecordCompletion(completionTime, blocksUsed);

        Debug.Log($"[ProgressManager] Level completed: {levelId} (Time: {completionTime:F2}s, Blocks: {blocksUsed})");

        // Fire first completion event
        if (!wasComplete)
        {
            OnLevelFirstComplete?.Invoke(levelId);

            // Check if this unlocks a new world
            CheckWorldUnlocks();
        }

        // Auto-save
        if (_autoSave)
        {
            SaveProgress();
        }
    }

    /// <summary>
    /// Gets progress data for a specific level.
    /// </summary>
    /// <param name="levelId">The level ID</param>
    /// <returns>The LevelProgress, or null if not found</returns>
    public LevelProgress GetLevelProgress(string levelId)
    {
        return _progressData?.GetProgress(levelId);
    }

    /// <summary>
    /// Records a level attempt (without completion).
    /// </summary>
    /// <param name="levelId">The level ID</param>
    public void RecordLevelAttempt(string levelId)
    {
        if (_progressData == null || string.IsNullOrEmpty(levelId))
        {
            return;
        }

        LevelProgress progress = _progressData.GetOrCreateProgress(levelId);
        progress.RecordAttempt();
    }

    /// <summary>
    /// Sets the current level being played.
    /// </summary>
    /// <param name="levelId">The level ID</param>
    public void SetCurrentLevel(string levelId)
    {
        if (_progressData != null)
        {
            _progressData.currentLevelId = levelId;
        }
    }

    #endregion

    #region World Progress

    /// <summary>
    /// Checks if a world is unlocked.
    /// </summary>
    /// <param name="worldId">The world ID to check</param>
    /// <returns>True if unlocked</returns>
    public bool IsWorldUnlocked(string worldId)
    {
        return _progressData != null && _progressData.IsWorldUnlocked(worldId);
    }

    /// <summary>
    /// Unlocks a world.
    /// </summary>
    /// <param name="worldId">The world ID to unlock</param>
    public void UnlockWorld(string worldId)
    {
        if (_progressData == null || string.IsNullOrEmpty(worldId))
        {
            return;
        }

        if (!_progressData.IsWorldUnlocked(worldId))
        {
            _progressData.UnlockWorld(worldId);
            Debug.Log($"[ProgressManager] World unlocked: {worldId}");

            OnWorldUnlocked?.Invoke(worldId);

            if (_autoSave)
            {
                SaveProgress();
            }
        }
    }

    /// <summary>
    /// Sets the current world being played.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    public void SetCurrentWorld(string worldId)
    {
        if (_progressData != null)
        {
            _progressData.currentWorldId = worldId;
        }
    }

    /// <summary>
    /// Checks if completing levels should unlock new worlds.
    /// </summary>
    private void CheckWorldUnlocks()
    {
        if (WorldManager.Instance == null)
        {
            return;
        }

        foreach (var world in WorldManager.Instance.Worlds)
        {
            if (world.orderInGame == 0) continue; // First world is always unlocked

            if (_progressData.IsWorldUnlocked(world.worldId)) continue;

            // Check if previous world is complete
            WorldData previousWorld = WorldManager.Instance.GetWorldByOrder(world.orderInGame - 1);
            if (previousWorld != null && WorldManager.Instance.IsWorldComplete(previousWorld.worldId))
            {
                UnlockWorld(world.worldId);
            }
        }
    }

    #endregion

    #region Play Time

    /// <summary>
    /// Adds time to the total play time counter.
    /// </summary>
    /// <param name="deltaTime">Time to add (seconds)</param>
    public void AddPlayTime(float deltaTime)
    {
        if (_progressData != null)
        {
            _progressData.totalPlayTime += deltaTime;
        }
    }

    /// <summary>
    /// Gets formatted total play time.
    /// </summary>
    /// <returns>Formatted string (e.g., "2h 15m")</returns>
    public string GetFormattedPlayTime()
    {
        float seconds = TotalPlayTime;
        int hours = Mathf.FloorToInt(seconds / 3600);
        int minutes = Mathf.FloorToInt((seconds % 3600) / 60);

        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }
        else
        {
            return $"{minutes}m";
        }
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Saves progress to disk.
    /// </summary>
    public void SaveProgress()
    {
        if (_progressData == null)
        {
            Debug.LogWarning("[ProgressManager] No progress data to save");
            return;
        }

        try
        {
            _progressData.UpdateSaveTimestamp();
            string json = JsonUtility.ToJson(_progressData, true);
            File.WriteAllText(_savePath, json);

            Debug.Log($"[ProgressManager] Progress saved to: {_savePath}");
            OnProgressSaved?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressManager] Failed to save progress: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads progress from disk.
    /// </summary>
    public void LoadProgress()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                _progressData = JsonUtility.FromJson<GameProgressData>(json);

                Debug.Log($"[ProgressManager] Progress loaded: {_progressData.CompletedLevelCount} levels complete");
            }
            else
            {
                // Create new progress data
                _progressData = new GameProgressData();

                // Unlock first world by default (tutorial world)
                _progressData.UnlockWorld(GetDefaultWorldId());

                Debug.Log("[ProgressManager] Created new progress data");
            }

            OnProgressLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressManager] Failed to load progress: {ex.Message}");

            // Create fresh progress on error
            _progressData = new GameProgressData();
            _progressData.UnlockWorld(GetDefaultWorldId());
        }
    }

    /// <summary>
    /// Resets all progress (for debugging or new game).
    /// </summary>
    public void ResetProgress()
    {
        Debug.Log("[ProgressManager] Resetting all progress");

        _progressData = new GameProgressData();
        _progressData.UnlockWorld(GetDefaultWorldId());

        // Delete save file
        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }

        SaveProgress();
    }

    /// <summary>
    /// Gets the save file path.
    /// </summary>
    public string GetSavePath()
    {
        return _savePath;
    }

    private string GetDefaultWorldId()
    {
        if (WorldManager.Instance != null)
        {
            WorldData firstWorld = WorldManager.Instance.GetFirstWorld();
            if (firstWorld != null && !string.IsNullOrEmpty(firstWorld.worldId))
            {
                return firstWorld.worldId;
            }
        }

        return "onboarding";
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets overall game completion percentage.
    /// </summary>
    public float GetOverallCompletionPercent()
    {
        if (WorldManager.Instance == null)
        {
            return 0f;
        }

        int totalLevels = WorldManager.Instance.GetTotalLevelCount();
        if (totalLevels == 0) return 0f;

        int completedLevels = WorldManager.Instance.GetTotalCompletedLevelCount();
        return (completedLevels / (float)totalLevels) * 100f;
    }

    /// <summary>
    /// Gets total number of attempts across all levels.
    /// </summary>
    public int GetTotalAttempts()
    {
        if (_progressData == null) return 0;

        int total = 0;
        foreach (var progress in _progressData.levelProgressList)
        {
            total += progress.attempts;
        }
        return total;
    }

    /// <summary>
    /// Exports progress data as JSON string (for debugging).
    /// </summary>
    public string ExportProgressJson()
    {
        if (_progressData == null) return "{}";
        return JsonUtility.ToJson(_progressData, true);
    }

    #endregion
}
