using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu UI controller.
/// Provides entry point to the game with options to start, view worlds, or quit.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Button to start the game (loads world map)")]
    public Button startGameButton;

    [Tooltip("Button to quit the application")]
    public Button quitButton;

    [Tooltip("Text displaying game title")]
    public Text titleText;

    [Header("Settings")]
    [Tooltip("Name of the world map scene to load")]
    public string worldMapSceneName = "WorldMap";

    private void Start()
    {
        // Setup button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuit);
        }

        // Initialize managers
        InitializeManagers();

        // Display title
        if (titleText != null)
        {
            titleText.text = "A Walk in the Park";
        }

        Debug.Log("[MainMenuUI] Main menu loaded");
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

    private void OnStartGame()
    {
        Debug.Log("[MainMenuUI] Starting game - loading world map");
        SceneManager.LoadScene(worldMapSceneName);
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
}
