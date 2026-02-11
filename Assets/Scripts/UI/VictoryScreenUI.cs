using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Victory screen UI overlay shown when a level is completed.
/// Provides options to continue, retry, or return to menu.
/// Tracks whether completing this level unlocked a new world.
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

    [Tooltip("Button to return to overworld (primary post-victory navigation)")]
    public Button overworldButton;

    [Header("Settings")]
    [Tooltip("Level select scene name")]
    public string levelSelectSceneName = GameConstants.SceneNames.LevelSelect;

    [Tooltip("World map scene name")]
    public string worldMapSceneName = GameConstants.SceneNames.WorldMap;

    [Tooltip("Overworld scene name")]
    public string overworldSceneName = GameConstants.SceneNames.Overworld;

    [Tooltip("Particle effect for celebration (optional)")]
    public ParticleSystem celebrationParticles;

    private LevelDefinition currentLevel;
    private LevelDefinition nextLevel;

    /// <summary>
    /// Tracks the worldId of a newly unlocked world during this level completion, or null.
    /// </summary>
    private string _newlyUnlockedWorldId;

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

        if (overworldButton != null)
        {
            overworldButton.onClick.AddListener(OnOverworld);
        }

        // Subscribe to level completion event
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete += Show;
        }

        // Subscribe to world unlock events to track newly unlocked worlds
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnWorldUnlocked += OnWorldUnlocked;
        }
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete -= Show;
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnWorldUnlocked -= OnWorldUnlocked;
        }
    }

    /// <summary>
    /// Called when a new world is unlocked (from WorldManager).
    /// Stores the world ID so we can trigger a reveal when returning to the overworld.
    /// </summary>
    private void OnWorldUnlocked(WorldData world)
    {
        if (world != null)
        {
            _newlyUnlockedWorldId = world.worldId;
            Debug.Log($"[VictoryScreenUI] New world unlocked: {world.worldName} ({world.worldId})");
        }
    }

    public void Show(LevelDefinition completedLevel)
    {
        if (victoryPanel == null)
        {
            Debug.LogError("[VictoryScreenUI] Victory panel not assigned!");
            return;
        }

        if (completedLevel == null)
        {
            Debug.LogError("[VictoryScreenUI] No level provided!");
            return;
        }

        currentLevel = completedLevel;

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

        // Update overworld button text to hint at new world if one was unlocked
        if (overworldButton != null)
        {
            Text buttonText = overworldButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = !string.IsNullOrEmpty(_newlyUnlockedWorldId)
                    ? "New World Unlocked!"
                    : "Overworld";
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
        if (currentLevel == null) return;
        ProgressManager progressManager = ProgressManager.Instance;

        LevelProgress progress = progressManager.GetLevelProgress(currentLevel.levelId);

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
        if (currentLevel == null)
        {
            return null;
        }
        WorldManager worldManager = WorldManager.Instance;

        // Find the world containing this level
        foreach (WorldData world in worldManager.Worlds)
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

    /// <summary>
    /// Navigates to the overworld. If a new world was unlocked during this session,
    /// stores a pending reveal flag so the overworld can show a reveal animation.
    /// </summary>
    private void OnOverworld()
    {
        // If a new world was unlocked, store it for the overworld reveal
        if (!string.IsNullOrEmpty(_newlyUnlockedWorldId))
        {
            PlayerPrefs.SetString(GameConstants.PlayerPrefsKeys.PendingWorldReveal, _newlyUnlockedWorldId);
            PlayerPrefs.Save();
            Debug.Log($"[VictoryScreenUI] Stored pending world reveal: {_newlyUnlockedWorldId}");
        }

        Debug.Log("[VictoryScreenUI] Returning to overworld");
        SceneManager.LoadScene(overworldSceneName);
    }
}
