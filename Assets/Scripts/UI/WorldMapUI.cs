using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// World map UI controller.
/// Displays all available worlds and allows navigation to level selection.
/// </summary>
public class WorldMapUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Container for world button instances")]
    public Transform worldButtonContainer;

    [Tooltip("Prefab for world selection buttons")]
    public GameObject worldButtonPrefab;

    [Tooltip("Button to return to main menu")]
    public Button backButton;

    [Tooltip("Text displaying current progress")]
    public Text progressText;

    [Header("Settings")]
    [Tooltip("Main menu scene name")]
    public string mainMenuSceneName = GameConstants.SceneNames.MainMenu;

    [Tooltip("Level select scene name")]
    public string levelSelectSceneName = GameConstants.SceneNames.LevelSelect;

    private List<WorldButton> worldButtons = new List<WorldButton>();

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBack);
        }

        PopulateWorldButtons();
        UpdateProgressText();

        Debug.Log("[WorldMapUI] World map loaded");
    }

    private void PopulateWorldButtons()
    {
        WorldManager worldManager = WorldManager.Instance;

        if (worldButtonContainer == null)
        {
            Debug.LogError("[WorldMapUI] World button container not assigned!");
            return;
        }

        var worlds = worldManager.Worlds;
        if (worlds == null || worlds.Count == 0)
        {
            Debug.LogWarning("[WorldMapUI] No worlds found to display");
            return;
        }

        // Clear existing buttons
        foreach (var button in worldButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        worldButtons.Clear();

        // Create button for each world
        foreach (WorldData world in worlds)
        {
            GameObject buttonObj = Instantiate(worldButtonPrefab, worldButtonContainer);
            WorldButton worldButton = buttonObj.GetComponent<WorldButton>();

            if (worldButton != null)
            {
                worldButton.Initialize(world, this);
                worldButtons.Add(worldButton);
            }
            else
            {
                Debug.LogWarning($"[WorldMapUI] WorldButton component not found on prefab for {world.worldName}");
            }
        }

        Debug.Log($"[WorldMapUI] Created {worldButtons.Count} world buttons");
    }

    private void UpdateProgressText()
    {
        if (progressText == null) return;
        WorldManager worldManager = WorldManager.Instance;

        var worlds = worldManager.Worlds;
        int totalWorlds = worlds.Count;
        int completedWorlds = 0;

        foreach (WorldData world in worlds)
        {
            if (worldManager.IsWorldComplete(world.worldId))
            {
                completedWorlds++;
            }
        }

        progressText.text = $"Progress: {completedWorlds}/{totalWorlds} Worlds Complete";
    }

    public void OnWorldSelected(WorldData world)
    {
        if (world == null)
        {
            Debug.LogError("[WorldMapUI] Cannot select null world");
            return;
        }

        Debug.Log($"[WorldMapUI] World selected: {world.worldName}");

        // Store selected world ID for level select scene
        PlayerPrefs.SetString(GameConstants.PlayerPrefsKeys.SelectedWorldId, world.worldId);
        PlayerPrefs.Save();

        // Load level select scene
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnBack()
    {
        Debug.Log("[WorldMapUI] Returning to main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
