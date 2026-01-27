using UnityEngine;

/// <summary>
/// Initializes the game scene by loading the selected level from PlayerPrefs.
/// Should be attached to a GameObject in the Game scene.
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "");

        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("[GameSceneInitializer] No level selected! Use LevelSelect UI to choose a level.");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogError("[GameSceneInitializer] LevelManager instance not found!");
            return;
        }

        Debug.Log($"[GameSceneInitializer] Loading level: {levelId}");
        LevelManager.Instance.LoadLevel(levelId);
    }
}
