using System.IO;
using UnityEngine;

/// <summary>
/// Handles saving and loading level data to/from JSON files.
/// Saves to Application.persistentDataPath for cross-platform compatibility.
/// </summary>
public static class LevelSaveSystem
{
    private const string SAVE_FOLDER = "Levels";
    private const string DEFAULT_LEVEL_NAME = "current_level";
    private const string FILE_EXTENSION = ".json";

    /// <summary>
    /// Gets the full path to the save directory.
    /// </summary>
    private static string GetSaveFolderPath()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

        // Create directory if it doesn't exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created save directory: {path}");
        }

        return path;
    }

    /// <summary>
    /// Gets the full path to a level file.
    /// </summary>
    private static string GetLevelFilePath(string levelName)
    {
        string fileName = levelName + FILE_EXTENSION;
        return Path.Combine(GetSaveFolderPath(), fileName);
    }

    /// <summary>
    /// Saves level data to a JSON file.
    /// </summary>
    public static bool SaveLevel(LevelData levelData, string levelName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = DEFAULT_LEVEL_NAME;
            }

            levelData.levelName = levelName;
            string json = JsonUtility.ToJson(levelData, true);
            string filePath = GetLevelFilePath(levelName);

            File.WriteAllText(filePath, json);

            Debug.Log($"Level saved successfully to: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save level: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads level data from a JSON file.
    /// </summary>
    public static LevelData LoadLevel(string levelName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = DEFAULT_LEVEL_NAME;
            }

            string filePath = GetLevelFilePath(levelName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Level file not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            LevelData levelData = JsonUtility.FromJson<LevelData>(json);

            Debug.Log($"Level loaded successfully from: {filePath}");
            return levelData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load level: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if a level save file exists.
    /// </summary>
    public static bool LevelExists(string levelName = null)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            levelName = DEFAULT_LEVEL_NAME;
        }

        string filePath = GetLevelFilePath(levelName);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Deletes a level save file.
    /// </summary>
    public static bool DeleteLevel(string levelName)
    {
        try
        {
            string filePath = GetLevelFilePath(levelName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"Level deleted: {filePath}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Level file not found: {filePath}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete level: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the path where levels are saved (for debugging).
    /// </summary>
    public static string GetSavePath()
    {
        return GetSaveFolderPath();
    }
}
