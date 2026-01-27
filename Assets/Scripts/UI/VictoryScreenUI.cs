using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Victory screen UI overlay shown when a level is completed.
/// Provides options to continue, retry, or return to menu.
/// </summary>
public class VictoryScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel for victory screen (to show/hide)")]
    public GameObject victoryPanel;

    [Tooltip("Text showing completion message")]
    public Text titleText = null;

    [Tooltip("Text showing level stats")]
    public Text statsText;

    [Tooltip("Button to load next level")]
    public Button nextLevelButton;

    [Tooltip("Button to retry current level")]
    public Button retryButton;

    [Tooltip("Button to return to level select")]
    public Button levelSelectButton;

    [Tooltip("Button to return to world map")]
    public Button worldMapButton;

    [Header("Settings")]
    [Tooltip("Level select scene name")]
    public string levelSelectSceneName = "LevelSelect";

    [Tooltip("World map scene name")]
    public string worldMapSceneName = "WorldMap";

    [Tooltip("Particle effect for celebration (optional)")]
    public ParticleSystem celebrationParticles;

    private LevelDefinition currentLevel;
    private LevelDefinition nextLevel;

    private void Start()
    {
        // Hide victory panel initially
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // Setup button listeners
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevel);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetry);
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.AddListener(OnLevelSelect);
        }

        if (worldMapButton != null)
        {
            worldMapButton.onClick.AddListener(OnWorldMap);
        }

        // Subscribe to level completion event
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete += Show;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete -= Show;
        }
    }

    public void Show()
    {
        if (victoryPanel == null)
        {
            Debug.LogError("[VictoryScreenUI] Victory panel not assigned!");
            return;
        }

        if (LevelManager.Instance == null || LevelManager.Instance.currentLevelDef == null)
        {
            Debug.LogError("[VictoryScreenUI] No current level found!");
            return;
        }

        currentLevel = LevelManager.Instance.currentLevelDef;

        // Show victory panel
        victoryPanel.SetActive(true);

        // Update title
        if (titleText != null)
        {
            titleText.text = $"{currentLevel.levelName}\nComplete!";
        }

        // Update stats
        UpdateStatsDisplay();

        // Check for next level
        nextLevel = GetNextLevel(currentLevel);

        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = (nextLevel != null);

            // Update button text if it has a text component
            Text buttonText = nextLevelButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = nextLevel != null ? "Next Level" : "No More Levels";
            }
        }

        // Play celebration effect
        if (celebrationParticles != null)
        {
            celebrationParticles.Play();
        }

        Debug.Log($"[VictoryScreenUI] Showing victory screen for {currentLevel.levelName}");
    }

    public void Hide()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    private void UpdateStatsDisplay()
    {
        if (statsText == null) return;
        if (ProgressManager.Instance == null || currentLevel == null) return;

        LevelProgress progress = ProgressManager.Instance.GetLevelProgress(currentLevel.levelId);

        if (progress != null)
        {
            string timeStr = progress.bestTime > 0 ? $"{progress.bestTime:F1}s" : "--";
            string blocksStr = progress.bestBlockCount > 0 ? $"{progress.bestBlockCount}" : "--";
            string attemptsStr = progress.attempts > 0 ? $"{progress.attempts}" : "1";

            statsText.text = $"Time: {timeStr}\nBlocks Used: {blocksStr}\nAttempts: {attemptsStr}";
        }
        else
        {
            statsText.text = "First completion!";
        }
    }

    private LevelDefinition GetNextLevel(LevelDefinition currentLevel)
    {
        if (WorldManager.Instance == null || currentLevel == null)
        {
            return null;
        }

        // Find the world containing this level
        foreach (WorldData world in WorldManager.Instance.allWorlds)
        {
            LevelDefinition foundLevel = world.GetNextLevel(currentLevel.levelId);
            if (foundLevel != null)
            {
                return foundLevel;
            }
        }

        return null;
    }

    private void OnNextLevel()
    {
        if (nextLevel == null)
        {
            Debug.LogWarning("[VictoryScreenUI] No next level available");
            return;
        }

        Debug.Log($"[VictoryScreenUI] Loading next level: {nextLevel.levelName}");

        Hide();

        // Load the next level
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(nextLevel);
        }
    }

    private void OnRetry()
    {
        Debug.Log("[VictoryScreenUI] Retrying level");

        Hide();

        // Restart current level
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartLevel();
        }
    }

    private void OnLevelSelect()
    {
        Debug.Log("[VictoryScreenUI] Returning to level select");
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnWorldMap()
    {
        Debug.Log("[VictoryScreenUI] Returning to world map");
        SceneManager.LoadScene(worldMapSceneName);
    }
}
