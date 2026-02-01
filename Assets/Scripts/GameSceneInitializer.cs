using UnityEngine;

/// <summary>
/// Initializes the game scene by loading the selected level.
/// In Editor: Uses the level selected in the Inspector dropdown
/// In Build: Uses PlayerPrefs from the level select UI
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Header("Level Selection")]
    [Tooltip("Select which level to load (Editor only - sets PlayerPrefs)")]
    public LevelDefinition selectedLevel;

    private void Start()
    {
#if UNITY_EDITOR
        // In editor, use the Inspector-selected level
        if (selectedLevel != null)
        {
            PlayerPrefs.SetString("SelectedLevelId", selectedLevel.levelId);
            Debug.Log($"[GameSceneInitializer] Set level from Inspector: {selectedLevel.levelId}");
        }
#endif

        string levelId = PlayerPrefs.GetString("SelectedLevelId", "");

        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("[GameSceneInitializer] No level selected! Use LevelSelect UI or Inspector to choose a level.");
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
