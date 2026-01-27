using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller for the Main Menu scene.
///
/// RESPONSIBILITIES:
/// - Handle Play button -> Load WorldMap scene
/// - Handle Continue button -> Resume last played level
/// - Handle Settings button -> Open settings panel
/// - Handle Quit button -> Exit application
/// - Display game title and version
///
/// SETUP:
/// 1. Create a new scene called "MainMenu"
/// 2. Add Canvas with UI elements (buttons, title text)
/// 3. Attach this script to an empty GameObject
/// 4. Drag button references to inspector fields
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Play/New Game button")]
    [SerializeField] private Button playButton;

    [Tooltip("Continue button (resume last level)")]
    [SerializeField] private Button continueButton;

    [Tooltip("Settings button")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Quit button")]
    [SerializeField] private Button quitButton;

    [Header("Panels")]
    [Tooltip("Settings panel (hidden by default)")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Display")]
    [Tooltip("Text showing game title")]
    [SerializeField] private Text titleText;

    [Tooltip("Text showing game version")]
    [SerializeField] private Text versionText;

    [Tooltip("Text showing completion stats")]
    [SerializeField] private Text statsText;

    [Header("Scene Names")]
    [Tooltip("Name of the world map scene")]
    [SerializeField] private string worldMapSceneName = "WorldMap";

    [Tooltip("Name of the gameplay scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private void Start()
    {
        SetupButtons();
        UpdateUI();
        HideSettingsPanel();
    }

    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void UpdateUI()
    {
        // Update version text
        if (versionText != null)
        {
            versionText.text = $"v{Application.version}";
        }

        // Update stats text
        if (statsText != null && ProgressManager.Instance != null)
        {
            int completed = ProgressManager.Instance.CompletedLevelCount;
            int total = WorldManager.Instance != null ? WorldManager.Instance.GetTotalLevelCount() : 0;
            float percent = ProgressManager.Instance.GetOverallCompletionPercent();

            if (total > 0)
            {
                statsText.text = $"{completed}/{total} Levels ({percent:F0}%)";
            }
            else
            {
                statsText.text = "";
            }
        }

        // Update continue button interactability
        if (continueButton != null)
        {
            bool hasProgress = ProgressManager.Instance != null &&
                              !string.IsNullOrEmpty(ProgressManager.Instance.CurrentLevelId);
            continueButton.interactable = hasProgress;
        }
    }

    #region Button Handlers

    /// <summary>
    /// Called when Play button is clicked.
    /// Loads the world map scene.
    /// </summary>
    public void OnPlayClicked()
    {
        Debug.Log("[MainMenu] Play clicked - loading world map");
        SceneManager.LoadScene(worldMapSceneName);
    }

    /// <summary>
    /// Called when Continue button is clicked.
    /// Resumes the last played level.
    /// </summary>
    public void OnContinueClicked()
    {
        if (ProgressManager.Instance == null)
        {
            OnPlayClicked();
            return;
        }

        string lastLevelId = ProgressManager.Instance.CurrentLevelId;
        if (string.IsNullOrEmpty(lastLevelId))
        {
            OnPlayClicked();
            return;
        }

        Debug.Log($"[MainMenu] Continue clicked - resuming level: {lastLevelId}");

        // Load the gameplay scene and then load the level
        SceneManager.LoadScene(gameplaySceneName);

        // Use a callback to load the level after scene loads
        SceneManager.sceneLoaded += OnGameplaySceneLoaded;
    }

    private void OnGameplaySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGameplaySceneLoaded;

        if (scene.name == gameplaySceneName)
        {
            string levelId = ProgressManager.Instance?.CurrentLevelId;
            if (!string.IsNullOrEmpty(levelId) && LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadLevel(levelId);
            }
        }
    }

    /// <summary>
    /// Called when Settings button is clicked.
    /// Shows the settings panel.
    /// </summary>
    public void OnSettingsClicked()
    {
        Debug.Log("[MainMenu] Settings clicked");
        ShowSettingsPanel();
    }

    /// <summary>
    /// Called when Quit button is clicked.
    /// Exits the application.
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quit clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Settings Panel

    public void ShowSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Called when Close button in settings is clicked.
    /// </summary>
    public void OnSettingsCloseClicked()
    {
        HideSettingsPanel();
    }

    /// <summary>
    /// Called when Reset Progress button is clicked.
    /// </summary>
    public void OnResetProgressClicked()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.ResetProgress();
            UpdateUI();
            Debug.Log("[MainMenu] Progress reset");
        }
    }

    #endregion
}
