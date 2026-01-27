using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Controller for the World Map scene.
///
/// RESPONSIBILITIES:
/// - Display all worlds and their unlock status
/// - Show level selection when a world is chosen
/// - Handle navigation between worlds
/// - Track and display progress per world
///
/// SETUP:
/// 1. Create a scene called "WorldMap"
/// 2. Add Canvas with world selection UI
/// 3. Attach this script to an empty GameObject
/// 4. Configure world button prefab and container
/// </summary>
public class WorldMapController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Container for world buttons")]
    [SerializeField] private Transform worldButtonContainer;

    [Tooltip("Prefab for world selection button")]
    [SerializeField] private GameObject worldButtonPrefab;

    [Tooltip("Back to main menu button")]
    [SerializeField] private Button backButton;

    [Header("Level Selection Panel")]
    [Tooltip("Panel showing levels for selected world")]
    [SerializeField] private GameObject levelSelectPanel;

    [Tooltip("Container for level buttons in the panel")]
    [SerializeField] private Transform levelButtonContainer;

    [Tooltip("Prefab for level selection button")]
    [SerializeField] private GameObject levelButtonPrefab;

    [Tooltip("Text showing selected world name")]
    [SerializeField] private Text worldNameText;

    [Tooltip("Text showing world progress")]
    [SerializeField] private Text worldProgressText;

    [Tooltip("Close level select panel button")]
    [SerializeField] private Button closeLevelSelectButton;

    [Header("Scene Names")]
    [Tooltip("Name of the main menu scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Tooltip("Name of the gameplay scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    // Currently selected world
    private WorldData _selectedWorld;

    // Cached button instances
    private List<GameObject> _worldButtonInstances = new List<GameObject>();
    private List<GameObject> _levelButtonInstances = new List<GameObject>();

    private void Start()
    {
        SetupButtons();
        PopulateWorlds();
        HideLevelSelectPanel();
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (closeLevelSelectButton != null)
        {
            closeLevelSelectButton.onClick.AddListener(OnCloseLevelSelectClicked);
        }
    }

    #region World Display

    /// <summary>
    /// Populates the world selection UI with all worlds.
    /// </summary>
    private void PopulateWorlds()
    {
        // Clear existing buttons
        ClearWorldButtons();

        if (WorldManager.Instance == null)
        {
            Debug.LogWarning("[WorldMap] WorldManager not found");
            return;
        }

        WorldData[] worlds = WorldManager.Instance.GetAllWorlds();

        foreach (var world in worlds)
        {
            CreateWorldButton(world);
        }

        Debug.Log($"[WorldMap] Displayed {worlds.Length} worlds");
    }

    /// <summary>
    /// Creates a button for a world.
    /// </summary>
    private void CreateWorldButton(WorldData world)
    {
        if (world == null || worldButtonContainer == null)
        {
            return;
        }

        GameObject buttonObj;

        if (worldButtonPrefab != null)
        {
            buttonObj = Instantiate(worldButtonPrefab, worldButtonContainer);
        }
        else
        {
            // Create a simple button if no prefab
            buttonObj = new GameObject($"World_{world.worldId}");
            buttonObj.transform.SetParent(worldButtonContainer, false);
            buttonObj.AddComponent<RectTransform>();
            buttonObj.AddComponent<Image>();
            buttonObj.AddComponent<Button>();

            // Add text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = world.worldName;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        _worldButtonInstances.Add(buttonObj);

        // Configure button
        WorldButtonUI buttonUI = buttonObj.GetComponent<WorldButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(world, this);
        }
        else
        {
            // Simple setup without custom component
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                WorldData capturedWorld = world;
                button.onClick.AddListener(() => OnWorldSelected(capturedWorld));

                // Update button appearance based on unlock status
                bool isUnlocked = WorldManager.Instance.IsWorldUnlocked(world.worldId);
                button.interactable = isUnlocked;

                // Update text
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    int completed = WorldManager.Instance.GetCompletedLevelCount(world.worldId);
                    int total = world.LevelCount;
                    string lockStatus = isUnlocked ? "" : " [LOCKED]";
                    buttonText.text = $"{world.worldName}\n{completed}/{total}{lockStatus}";
                }
            }
        }
    }

    /// <summary>
    /// Clears all world button instances.
    /// </summary>
    private void ClearWorldButtons()
    {
        foreach (var button in _worldButtonInstances)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        _worldButtonInstances.Clear();
    }

    #endregion

    #region Level Selection

    /// <summary>
    /// Called when a world is selected.
    /// Shows the level selection panel for that world.
    /// </summary>
    public void OnWorldSelected(WorldData world)
    {
        if (world == null)
        {
            return;
        }

        if (!WorldManager.Instance.IsWorldUnlocked(world.worldId))
        {
            Debug.Log($"[WorldMap] World '{world.worldName}' is locked");
            return;
        }

        _selectedWorld = world;
        ShowLevelSelectPanel();
        PopulateLevels(world);

        Debug.Log($"[WorldMap] Selected world: {world.worldName}");
    }

    /// <summary>
    /// Populates the level selection panel with levels from the selected world.
    /// </summary>
    private void PopulateLevels(WorldData world)
    {
        // Clear existing buttons
        ClearLevelButtons();

        if (world == null || world.levels == null)
        {
            return;
        }

        // Update world info
        if (worldNameText != null)
        {
            worldNameText.text = world.worldName;
        }

        if (worldProgressText != null)
        {
            int completed = WorldManager.Instance.GetCompletedLevelCount(world.worldId);
            int total = world.LevelCount;
            worldProgressText.text = $"{completed}/{total} Complete";
        }

        // Create level buttons
        for (int i = 0; i < world.levels.Length; i++)
        {
            LevelDefinition level = world.levels[i];
            if (level != null)
            {
                CreateLevelButton(level, i);
            }
        }
    }

    /// <summary>
    /// Creates a button for a level.
    /// </summary>
    private void CreateLevelButton(LevelDefinition level, int index)
    {
        if (level == null || levelButtonContainer == null)
        {
            return;
        }

        GameObject buttonObj;

        if (levelButtonPrefab != null)
        {
            buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
        }
        else
        {
            // Create a simple button if no prefab
            buttonObj = new GameObject($"Level_{level.levelId}");
            buttonObj.transform.SetParent(levelButtonContainer, false);
            buttonObj.AddComponent<RectTransform>();
            buttonObj.AddComponent<Image>();
            buttonObj.AddComponent<Button>();

            // Add text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        _levelButtonInstances.Add(buttonObj);

        // Configure button
        LevelButtonUI buttonUI = buttonObj.GetComponent<LevelButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(level, this);
        }
        else
        {
            // Simple setup
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                LevelDefinition capturedLevel = level;
                button.onClick.AddListener(() => OnLevelSelected(capturedLevel));

                // Determine if level is unlocked
                bool isUnlocked = IsLevelUnlocked(level, index);
                button.interactable = isUnlocked;

                // Update text
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    bool isComplete = ProgressManager.Instance != null &&
                                     ProgressManager.Instance.IsLevelComplete(level.levelId);
                    string status = isComplete ? " ★" : (isUnlocked ? "" : " 🔒");
                    buttonText.text = $"{index + 1}. {level.levelName}{status}";
                }
            }
        }
    }

    /// <summary>
    /// Checks if a level is unlocked.
    /// First level of each world is always unlocked if the world is unlocked.
    /// Subsequent levels unlock when the previous level is completed.
    /// </summary>
    private bool IsLevelUnlocked(LevelDefinition level, int index)
    {
        if (level == null || _selectedWorld == null)
        {
            return false;
        }

        // First level is always unlocked if world is unlocked
        if (index == 0)
        {
            return WorldManager.Instance.IsWorldUnlocked(_selectedWorld.worldId);
        }

        // Check if previous level is completed
        if (index > 0 && _selectedWorld.levels != null && index < _selectedWorld.levels.Length)
        {
            LevelDefinition previousLevel = _selectedWorld.levels[index - 1];
            if (previousLevel != null && ProgressManager.Instance != null)
            {
                return ProgressManager.Instance.IsLevelComplete(previousLevel.levelId);
            }
        }

        return false;
    }

    /// <summary>
    /// Called when a level is selected.
    /// Loads the gameplay scene and starts the level.
    /// </summary>
    public void OnLevelSelected(LevelDefinition level)
    {
        if (level == null)
        {
            return;
        }

        Debug.Log($"[WorldMap] Selected level: {level.levelName} ({level.levelId})");

        // Store the level to load
        PlayerPrefs.SetString("PendingLevelId", level.levelId);
        PlayerPrefs.Save();

        // Load gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Clears all level button instances.
    /// </summary>
    private void ClearLevelButtons()
    {
        foreach (var button in _levelButtonInstances)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        _levelButtonInstances.Clear();
    }

    #endregion

    #region Panel Management

    private void ShowLevelSelectPanel()
    {
        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(true);
        }
    }

    private void HideLevelSelectPanel()
    {
        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(false);
        }
        _selectedWorld = null;
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Called when Back button is clicked.
    /// Returns to main menu.
    /// </summary>
    public void OnBackClicked()
    {
        Debug.Log("[WorldMap] Back clicked - returning to main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Called when Close Level Select button is clicked.
    /// Hides the level selection panel.
    /// </summary>
    public void OnCloseLevelSelectClicked()
    {
        HideLevelSelectPanel();
    }

    #endregion

    #region Refresh

    /// <summary>
    /// Refreshes the world map UI.
    /// Call after progress changes.
    /// </summary>
    public void Refresh()
    {
        PopulateWorlds();

        if (_selectedWorld != null)
        {
            PopulateLevels(_selectedWorld);
        }
    }

    #endregion
}

/// <summary>
/// Optional component for world buttons with more detailed UI.
/// </summary>
public class WorldButtonUI : MonoBehaviour
{
    [SerializeField] private Text worldNameText;
    [SerializeField] private Text progressText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button button;

    private WorldData _world;
    private WorldMapController _controller;

    public void Setup(WorldData world, WorldMapController controller)
    {
        _world = world;
        _controller = controller;

        if (worldNameText != null)
        {
            worldNameText.text = world.worldName;
        }

        if (iconImage != null && world.worldIcon != null)
        {
            iconImage.sprite = world.worldIcon;
        }

        bool isUnlocked = WorldManager.Instance != null && WorldManager.Instance.IsWorldUnlocked(world.worldId);

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        if (progressText != null)
        {
            int completed = WorldManager.Instance != null ? WorldManager.Instance.GetCompletedLevelCount(world.worldId) : 0;
            int total = world.LevelCount;
            progressText.text = isUnlocked ? $"{completed}/{total}" : "Locked";
        }

        if (button != null)
        {
            button.interactable = isUnlocked;
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_controller != null && _world != null)
        {
            _controller.OnWorldSelected(_world);
        }
    }
}

/// <summary>
/// Optional component for level buttons with more detailed UI.
/// </summary>
public class LevelButtonUI : MonoBehaviour
{
    [SerializeField] private Text levelNameText;
    [SerializeField] private Text levelNumberText;
    [SerializeField] private GameObject completedIndicator;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button button;

    private LevelDefinition _level;
    private WorldMapController _controller;

    public void Setup(LevelDefinition level, WorldMapController controller)
    {
        _level = level;
        _controller = controller;

        if (levelNameText != null)
        {
            levelNameText.text = level.levelName;
        }

        if (levelNumberText != null)
        {
            levelNumberText.text = (level.orderInWorld + 1).ToString();
        }

        bool isComplete = ProgressManager.Instance != null && ProgressManager.Instance.IsLevelComplete(level.levelId);

        if (completedIndicator != null)
        {
            completedIndicator.SetActive(isComplete);
        }

        // Note: Lock status should be set by the controller
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void SetLocked(bool locked)
    {
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(locked);
        }

        if (button != null)
        {
            button.interactable = !locked;
        }
    }

    private void OnClick()
    {
        if (_controller != null && _level != null)
        {
            _controller.OnLevelSelected(_level);
        }
    }
}
