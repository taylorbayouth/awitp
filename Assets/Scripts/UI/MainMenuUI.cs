using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Start screen UI controller.
/// Provides entry point to load a game or quit.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Button to load the game (preferred)")]
    public Button loadGameButton;

    [Tooltip("Legacy start button (used if loadGameButton is not assigned)")]
    public Button startGameButton;

    [Tooltip("Button to quit the application")]
    public Button quitButton;

    [Tooltip("Button to reset player progress")]
    public Button resetProgressButton;

    [Tooltip("Text displaying game title")]
    public Text titleText;

    [Tooltip("Optional subtitle text under the title")]
    public Text subtitleText;

    [Header("Settings")]
    [Tooltip("Name of the overworld scene to load")]
    public string overworldSceneName = "Overworld";

    private void Start()
    {
        // Make sure the mouse is usable on the start screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Setup button listeners
        Button effectiveLoadButton = loadGameButton != null ? loadGameButton : startGameButton;
        if (effectiveLoadButton != null)
        {
            effectiveLoadButton.onClick.AddListener(OnLoadGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuit);
        }

        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.AddListener(OnResetProgress);
        }

        // Initialize managers
        InitializeManagers();

        // Display title
        if (titleText != null)
        {
            titleText.text = "A Walk in the Park";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "A game by Taylor Bayouth";
        }
        else if (titleText != null)
        {
            titleText.text = "A Walk in the Park\nA game by Taylor Bayouth";
        }

        Debug.Log("[MainMenuUI] Start screen loaded");
    }

    private void InitializeManagers()
    {
        // Ensure singleton managers are initialized
        if (WorldManager.Instance == null)
        {
            GameObject managerObj = new GameObject("WorldManager");
            managerObj.AddComponent<WorldManager>();
            DontDestroyOnLoad(managerObj);
        }

        if (ProgressManager.Instance == null)
        {
            GameObject managerObj = new GameObject("ProgressManager");
            managerObj.AddComponent<ProgressManager>();
            DontDestroyOnLoad(managerObj);
        }

        if (LevelManager.Instance == null)
        {
            GameObject managerObj = new GameObject("LevelManager");
            managerObj.AddComponent<LevelManager>();
            DontDestroyOnLoad(managerObj);
        }
    }

    private void OnLoadGame()
    {
        Debug.Log("[MainMenuUI] Loading game - loading overworld");
        SceneManager.LoadScene(overworldSceneName);
    }

    private void OnQuit()
    {
        Debug.Log("[MainMenuUI] Quitting game");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnResetProgress()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.ResetProgress();
            Debug.Log("[MainMenuUI] Progress reset");
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] ProgressManager not available to reset progress");
        }
    }
}
