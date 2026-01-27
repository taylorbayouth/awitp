using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller for the Victory Screen overlay.
///
/// RESPONSIBILITIES:
/// - Display victory message when level is completed
/// - Show completion statistics (time, blocks used)
/// - Provide buttons for Next Level, Replay, and Return to World Map
/// - Handle world unlock notifications
///
/// USAGE:
/// - Can be shown via LevelManager.OnLevelComplete event
/// - Call Show() to display, Hide() to dismiss
/// - Responds to LevelManager events automatically if subscribed
///
/// SETUP:
/// 1. Create Canvas with victory screen UI (panel, buttons, texts)
/// 2. Attach this script to the victory panel
/// 3. Configure button and text references
/// 4. Panel should start disabled (hidden)
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main victory panel")]
    [SerializeField] private GameObject victoryPanel;

    [Tooltip("Text showing 'Level Complete!' message")]
    [SerializeField] private Text victoryTitleText;

    [Tooltip("Text showing level name")]
    [SerializeField] private Text levelNameText;

    [Tooltip("Text showing completion time")]
    [SerializeField] private Text timeText;

    [Tooltip("Text showing blocks used")]
    [SerializeField] private Text blocksText;

    [Tooltip("Text showing new best indicator")]
    [SerializeField] private Text newBestText;

    [Header("Buttons")]
    [Tooltip("Button to proceed to next level")]
    [SerializeField] private Button nextLevelButton;

    [Tooltip("Button to replay current level")]
    [SerializeField] private Button replayButton;

    [Tooltip("Button to return to world map")]
    [SerializeField] private Button worldMapButton;

    [Header("World Unlock")]
    [Tooltip("Panel showing world unlock notification")]
    [SerializeField] private GameObject worldUnlockPanel;

    [Tooltip("Text showing unlocked world name")]
    [SerializeField] private Text worldUnlockText;

    [Header("Scene Names")]
    [Tooltip("Name of the world map scene")]
    [SerializeField] private string worldMapSceneName = "WorldMap";

    // State
    private LevelDefinition _completedLevel;
    private bool _hasNextLevel;

    private void OnEnable()
    {
        // Subscribe to level completion events
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete += OnLevelComplete;
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnWorldUnlocked += OnWorldUnlocked;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete -= OnLevelComplete;
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnWorldUnlocked -= OnWorldUnlocked;
        }
    }

    private void Start()
    {
        SetupButtons();
        Hide();
    }

    private void SetupButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        if (worldMapButton != null)
        {
            worldMapButton.onClick.AddListener(OnWorldMapClicked);
        }
    }

    #region Show/Hide

    /// <summary>
    /// Shows the victory screen with the completed level info.
    /// </summary>
    /// <param name="level">The completed level</param>
    public void Show(LevelDefinition level)
    {
        _completedLevel = level;

        UpdateUI();

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        // Pause the game
        Time.timeScale = 0f;

        Debug.Log("[VictoryScreen] Shown for level: " + (level != null ? level.levelName : "null"));
    }

    /// <summary>
    /// Hides the victory screen.
    /// </summary>
    public void Hide()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (worldUnlockPanel != null)
        {
            worldUnlockPanel.SetActive(false);
        }

        // Resume game
        Time.timeScale = 1f;
    }

    #endregion

    #region UI Update

    /// <summary>
    /// Updates all UI elements with current level data.
    /// </summary>
    private void UpdateUI()
    {
        // Level name
        if (levelNameText != null && _completedLevel != null)
        {
            levelNameText.text = _completedLevel.levelName;
        }

        // Completion time
        if (timeText != null && LevelManager.Instance != null)
        {
            float time = LevelManager.Instance.ElapsedTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        // Blocks used
        if (blocksText != null && LevelManager.Instance != null)
        {
            int blocks = LevelManager.Instance.BlocksPlaced;
            blocksText.text = $"Blocks Used: {blocks}";
        }

        // New best indicator
        if (newBestText != null)
        {
            // Check if this is a new best
            bool isNewBest = CheckIfNewBest();
            newBestText.gameObject.SetActive(isNewBest);
        }

        // Next level button
        UpdateNextLevelButton();
    }

    /// <summary>
    /// Updates the next level button based on availability.
    /// </summary>
    private void UpdateNextLevelButton()
    {
        _hasNextLevel = false;

        if (nextLevelButton == null || _completedLevel == null)
        {
            return;
        }

        // Check if there's a next level
        if (WorldManager.Instance != null)
        {
            WorldData world = WorldManager.Instance.GetWorld(_completedLevel.worldId);
            if (world != null)
            {
                LevelDefinition nextLevel = world.GetNextLevel(_completedLevel.levelId);
                _hasNextLevel = nextLevel != null;
            }
        }

        nextLevelButton.gameObject.SetActive(_hasNextLevel);
    }

    /// <summary>
    /// Checks if the current completion is a new personal best.
    /// </summary>
    private bool CheckIfNewBest()
    {
        if (ProgressManager.Instance == null || _completedLevel == null)
        {
            return false;
        }

        LevelProgress progress = ProgressManager.Instance.GetLevelProgress(_completedLevel.levelId);
        if (progress == null)
        {
            return true; // First completion is always a "best"
        }

        // Check if this is better than previous best time
        float currentTime = LevelManager.Instance != null ? LevelManager.Instance.ElapsedTime : 0f;
        return progress.attempts <= 1 || currentTime < progress.bestTime;
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Called when Next Level button is clicked.
    /// </summary>
    public void OnNextLevelClicked()
    {
        Hide();

        if (LevelManager.Instance != null)
        {
            if (!LevelManager.Instance.LoadNextLevel())
            {
                // No next level, go to world map
                OnWorldMapClicked();
            }
        }

        Debug.Log("[VictoryScreen] Next Level clicked");
    }

    /// <summary>
    /// Called when Replay button is clicked.
    /// </summary>
    public void OnReplayClicked()
    {
        Hide();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartLevel();
        }

        Debug.Log("[VictoryScreen] Replay clicked");
    }

    /// <summary>
    /// Called when World Map button is clicked.
    /// </summary>
    public void OnWorldMapClicked()
    {
        Hide();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UnloadLevel();
        }

        SceneManager.LoadScene(worldMapSceneName);

        Debug.Log("[VictoryScreen] World Map clicked");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when LevelManager fires OnLevelComplete event.
    /// </summary>
    private void OnLevelComplete(LevelDefinition level)
    {
        Show(level);
    }

    /// <summary>
    /// Called when a new world is unlocked.
    /// </summary>
    private void OnWorldUnlocked(WorldData world)
    {
        if (worldUnlockPanel != null && world != null)
        {
            worldUnlockPanel.SetActive(true);

            if (worldUnlockText != null)
            {
                worldUnlockText.text = $"New World Unlocked!\n{world.worldName}";
            }

            Debug.Log($"[VictoryScreen] World unlocked: {world.worldName}");
        }
    }

    #endregion
}
