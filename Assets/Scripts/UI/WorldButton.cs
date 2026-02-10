using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Individual world button component for the world map.
/// Displays world info and handles selection.
/// </summary>
[RequireComponent(typeof(Button))]
public class WorldButton : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text showing world name")]
    public Text worldNameText;

    [Tooltip("Text showing world description")]
    public Text descriptionText;

    [Tooltip("Image showing completion status")]
    public Image completionIcon;

    [Tooltip("Image showing lock icon when world is locked")]
    public Image lockIcon;

    [Tooltip("Background image that can be tinted")]
    public Image backgroundImage;

    [Header("Visual States")]
    [Tooltip("Color for locked worlds")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Tooltip("Color for unlocked worlds")]
    public Color unlockedColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Color for completed worlds")]
    public Color completedColor = new Color(0.5f, 1f, 0.5f, 1f);

    private WorldData worldData;
    private WorldMapUI worldMapUI;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Initialize(WorldData world, WorldMapUI mapUI)
    {
        worldData = world;
        worldMapUI = mapUI;

        if (world == null)
        {
            Debug.LogError("[WorldButton] Cannot initialize with null WorldData");
            return;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (worldData == null) return;

        // Set world name
        if (worldNameText != null)
        {
            worldNameText.text = worldData.worldName;
        }

        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = worldData.description;
        }

        // Check unlock/completion status
        bool isUnlocked = WorldManager.Instance != null && WorldManager.Instance.IsWorldUnlocked(worldData.worldId);
        bool isCompleted = WorldManager.Instance != null && WorldManager.Instance.IsWorldComplete(worldData.worldId);

        // Update button interactability
        button.interactable = isUnlocked;

        // Update lock icon
        if (lockIcon != null)
        {
            lockIcon.enabled = !isUnlocked;
        }

        // Update completion icon
        if (completionIcon != null)
        {
            completionIcon.enabled = isCompleted;
        }

        // Update background color
        if (backgroundImage != null)
        {
            if (!isUnlocked)
            {
                backgroundImage.color = lockedColor;
            }
            else if (isCompleted)
            {
                backgroundImage.color = completedColor;
            }
            else
            {
                // Use world's theme color if available
                backgroundImage.color = worldData.themeColor != Color.clear ? worldData.themeColor : unlockedColor;
            }
        }

    }

    private void OnClick()
    {
        if (worldData == null || worldMapUI == null)
        {
            Debug.LogError("[WorldButton] Cannot handle click - missing data");
            return;
        }

        worldMapUI.OnWorldSelected(worldData);
    }
}
