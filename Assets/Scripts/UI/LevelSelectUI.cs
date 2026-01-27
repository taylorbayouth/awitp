using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Level selection UI controller.
/// Displays all levels in a selected world.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Container for level button instances")]
    public Transform levelButtonContainer;

    [Tooltip("Prefab for level selection buttons")]
    public GameObject levelButtonPrefab;

    [Tooltip("Text displaying world name")]
    public Text worldNameText;

    [Tooltip("Text displaying world description")]
    public Text worldDescriptionText;

    [Tooltip("Button to return to world map")]
    public Button backButton;

    [Tooltip("Text showing progress in this world")]
    public Text progressText;

    [Header("Settings")]
    [Tooltip("World map scene name")]
    public string worldMapSceneName = "WorldMap";

    [Tooltip("Game scene name")]
    public string gameSceneName = "Game";

    private WorldData currentWorld;
    private List<LevelButton> levelButtons = new List<LevelButton>();

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBack);
        }

        LoadSelectedWorld();
        PopulateLevelButtons();
        UpdateProgressText();

        Debug.Log("[LevelSelectUI] Level select loaded");
    }

    private void LoadSelectedWorld()
    {
        string worldId = PlayerPrefs.GetString("SelectedWorldId", "");

        if (string.IsNullOrEmpty(worldId))
        {
            Debug.LogError("[LevelSelectUI] No world ID stored! Returning to world map.");
            SceneManager.LoadScene(worldMapSceneName);
            return;
        }

        if (WorldManager.Instance == null)
        {
            Debug.LogError("[LevelSelectUI] WorldManager not found!");
            return;
        }

        currentWorld = WorldManager.Instance.GetWorld(worldId);

        if (currentWorld == null)
        {
            Debug.LogError($"[LevelSelectUI] World not found: {worldId}");
            return;
        }

        // Update world info display
        if (worldNameText != null)
        {
            worldNameText.text = currentWorld.worldName;
        }

        if (worldDescriptionText != null)
        {
            worldDescriptionText.text = currentWorld.description;
        }

        Debug.Log($"[LevelSelectUI] Loaded world: {currentWorld.worldName}");
    }

    private void PopulateLevelButtons()
    {
        if (currentWorld == null)
        {
            Debug.LogError("[LevelSelectUI] Cannot populate levels - no world loaded");
            return;
        }

        if (levelButtonContainer == null)
        {
            Debug.LogError("[LevelSelectUI] Level button container not assigned!");
            return;
        }

        // Clear existing buttons
        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        levelButtons.Clear();

        // Create button for each level
        LevelDefinition[] levels = currentWorld.levels;

        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning($"[LevelSelectUI] No levels found in world {currentWorld.worldName}");
            return;
        }

        foreach (LevelDefinition level in levels)
        {
            if (level == null)
            {
                Debug.LogWarning("[LevelSelectUI] Null level definition found, skipping");
                continue;
            }

            GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            LevelButton levelButton = buttonObj.GetComponent<LevelButton>();

            if (levelButton != null)
            {
                levelButton.Initialize(level, this);
                levelButtons.Add(levelButton);
            }
            else
            {
                Debug.LogWarning($"[LevelSelectUI] LevelButton component not found on prefab for {level.levelName}");
            }
        }

        Debug.Log($"[LevelSelectUI] Created {levelButtons.Count} level buttons");
    }

    private void UpdateProgressText()
    {
        if (progressText == null) return;
        if (currentWorld == null) return;
        if (ProgressManager.Instance == null) return;

        int totalLevels = currentWorld.levels.Length;
        int completedLevels = 0;

        foreach (LevelDefinition level in currentWorld.levels)
        {
            if (level != null && ProgressManager.Instance.IsLevelComplete(level.levelId))
            {
                completedLevels++;
            }
        }

        progressText.text = $"{completedLevels}/{totalLevels} Levels Complete";
    }

    public void OnLevelSelected(LevelDefinition level)
    {
        if (level == null)
        {
            Debug.LogError("[LevelSelectUI] Cannot select null level");
            return;
        }

        Debug.Log($"[LevelSelectUI] Level selected: {level.levelName}");

        // Store selected level ID
        PlayerPrefs.SetString("SelectedLevelId", level.levelId);
        PlayerPrefs.Save();

        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnBack()
    {
        Debug.Log("[LevelSelectUI] Returning to world map");
        SceneManager.LoadScene(worldMapSceneName);
    }
}
