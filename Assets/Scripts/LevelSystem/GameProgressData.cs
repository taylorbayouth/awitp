using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data structure for tracking player progress through the game.
/// Stores completed levels, unlocked worlds, and per-level statistics.
///
/// PERSISTENCE:
/// - Saved as JSON to Application.persistentDataPath/progress.json
/// - ProgressManager handles save/load operations
///
/// NOTE:
/// Uses List instead of Dictionary because Unity's JsonUtility
/// doesn't support Dictionary serialization directly.
/// </summary>
[Serializable]
public class GameProgressData
{
    [Header("Completion Status")]
    [Tooltip("IDs of all completed levels")]
    public List<string> completedLevelIds = new List<string>();

    [Tooltip("IDs of all unlocked worlds")]
    public List<string> unlockedWorldIds = new List<string>();

    [Header("Current State")]
    [Tooltip("ID of the last played level")]
    public string currentLevelId;

    [Tooltip("ID of the current world")]
    public string currentWorldId;

    [Header("Statistics")]
    [Tooltip("Per-level progress data")]
    public List<LevelProgress> levelProgressList = new List<LevelProgress>();

    [Tooltip("Total play time in seconds")]
    public float totalPlayTime;

    [Tooltip("Timestamp when progress was last saved")]
    public string lastSaveTimestamp;

    /// <summary>
    /// Gets progress data for a specific level.
    /// </summary>
    /// <param name="levelId">The level ID to look up</param>
    /// <returns>The LevelProgress for that level, or null if not found</returns>
    public LevelProgress GetProgress(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            return null;
        }

        return levelProgressList.Find(p => p.levelId == levelId);
    }

    /// <summary>
    /// Gets or creates progress data for a specific level.
    /// </summary>
    /// <param name="levelId">The level ID</param>
    /// <returns>The existing or newly created LevelProgress</returns>
    public LevelProgress GetOrCreateProgress(string levelId)
    {
        LevelProgress progress = GetProgress(levelId);
        if (progress == null)
        {
            progress = new LevelProgress { levelId = levelId };
            levelProgressList.Add(progress);
        }
        return progress;
    }

    /// <summary>
    /// Checks if a level has been completed.
    /// </summary>
    /// <param name="levelId">The level ID to check</param>
    /// <returns>True if completed</returns>
    public bool IsLevelCompleted(string levelId)
    {
        return completedLevelIds.Contains(levelId);
    }

    /// <summary>
    /// Checks if a world has been unlocked.
    /// </summary>
    /// <param name="worldId">The world ID to check</param>
    /// <returns>True if unlocked</returns>
    public bool IsWorldUnlocked(string worldId)
    {
        return unlockedWorldIds.Contains(worldId);
    }

    /// <summary>
    /// Marks a level as completed.
    /// </summary>
    /// <param name="levelId">The level ID to mark complete</param>
    public void MarkLevelCompleted(string levelId)
    {
        if (!completedLevelIds.Contains(levelId))
        {
            completedLevelIds.Add(levelId);
        }

        LevelProgress progress = GetOrCreateProgress(levelId);
        progress.completed = true;
        progress.completionTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Unlocks a world.
    /// </summary>
    /// <param name="worldId">The world ID to unlock</param>
    public void UnlockWorld(string worldId)
    {
        if (!unlockedWorldIds.Contains(worldId))
        {
            unlockedWorldIds.Add(worldId);
        }
    }

    /// <summary>
    /// Gets the number of completed levels.
    /// </summary>
    public int CompletedLevelCount => completedLevelIds.Count;

    /// <summary>
    /// Gets the number of unlocked worlds.
    /// </summary>
    public int UnlockedWorldCount => unlockedWorldIds.Count;

    /// <summary>
    /// Resets all progress to initial state.
    /// </summary>
    public void Reset()
    {
        completedLevelIds.Clear();
        unlockedWorldIds.Clear();
        levelProgressList.Clear();
        currentLevelId = string.Empty;
        currentWorldId = string.Empty;
        totalPlayTime = 0f;
        lastSaveTimestamp = string.Empty;
    }

    /// <summary>
    /// Updates the save timestamp to now.
    /// </summary>
    public void UpdateSaveTimestamp()
    {
        lastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

/// <summary>
/// Tracks progress and statistics for a single level.
/// </summary>
[Serializable]
public class LevelProgress
{
    [Tooltip("ID of the level")]
    public string levelId;

    [Tooltip("Whether the level has been completed")]
    public bool completed;

    [Tooltip("Number of times the level has been attempted")]
    public int attempts;

    [Tooltip("Best completion time in seconds (for speedruns)")]
    public float bestTime;

    [Tooltip("Number of blocks used in best solution")]
    public int bestBlockCount;

    [Tooltip("When the level was first completed")]
    public string completionTimestamp;

    [Tooltip("When the level was last played")]
    public string lastPlayedTimestamp;

    /// <summary>
    /// Records an attempt at this level.
    /// </summary>
    public void RecordAttempt()
    {
        attempts++;
        lastPlayedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Records a completion of this level.
    /// </summary>
    /// <param name="time">Time taken to complete (seconds)</param>
    /// <param name="blockCount">Number of blocks used</param>
    public void RecordCompletion(float time, int blockCount)
    {
        completed = true;
        attempts++;
        lastPlayedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (!completed || time < bestTime || bestTime <= 0)
        {
            bestTime = time;
            completionTimestamp = lastPlayedTimestamp;
        }

        if (!completed || blockCount < bestBlockCount || bestBlockCount <= 0)
        {
            bestBlockCount = blockCount;
        }
    }
}
