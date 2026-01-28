using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Individual level button component for level selection.
/// Displays level info and handles selection.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text showing level name")]
    public Text levelNameText;

    [Tooltip("Text showing level number/order")]
    public Text levelNumberText;

    [Tooltip("Image showing completion status")]
    public Image completionCheckmark;

    [Tooltip("Image showing best stats (time/blocks)")]
    public Text statsText;

    [Tooltip("Background image that can be tinted")]
    public Image backgroundImage;

    [Header("Visual States")]
    [Tooltip("Color for incomplete levels")]
    public Color incompleteColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Tooltip("Color for completed levels")]
    public Color completedColor = new Color(0.5f, 1f, 0.5f, 1f);

    private LevelDefinition levelData;
    private LevelSelectUI levelSelectUI;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Initialize(LevelDefinition level, LevelSelectUI selectUI)
    {
        levelData = level;
        levelSelectUI = selectUI;

        if (level == null)
        {
            Debug.LogError("[LevelButton] Cannot initialize with null LevelDefinition");
            return;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (levelData == null) return;

        // Set level name
        if (levelNameText != null)
        {
            levelNameText.text = levelData.levelName;
        }

        // Set level number
        if (levelNumberText != null)
        {
            levelNumberText.text = $"Level {levelData.orderInWorld + 1}";
        }

        // Check completion status
        bool isCompleted = ProgressManager.Instance != null && ProgressManager.Instance.IsLevelComplete(levelData.levelId);

        // Update completion checkmark
        if (completionCheckmark != null)
        {
            completionCheckmark.enabled = isCompleted;
        }

        // Update stats text
        if (statsText != null && ProgressManager.Instance != null)
        {
            LevelProgress progress = ProgressManager.Instance.GetLevelProgress(levelData.levelId);
            if (progress != null && progress.completed)
            {
                string timeStr = progress.bestTime > 0 ? $"{progress.bestTime:F1}s" : "--";
                string blocksStr = progress.bestBlockCount > 0 ? $"{progress.bestBlockCount} blocks" : "--";
                statsText.text = $"{timeStr} | {blocksStr}";
            }
            else
            {
                statsText.text = "Incomplete";
            }
        }

        // Update background color
        if (backgroundImage != null)
        {
            backgroundImage.color = isCompleted ? completedColor : incompleteColor;
        }

        // Button is always interactable (can replay completed levels)
        button.interactable = true;

        Debug.Log($"[LevelButton] Initialized: {levelData.levelName} (Completed: {isCompleted})");
    }

    private void OnClick()
    {
        if (levelData == null || levelSelectUI == null)
        {
            Debug.LogError("[LevelButton] Cannot handle click - missing data");
            return;
        }

        levelSelectUI.OnLevelSelected(levelData);
    }
}
