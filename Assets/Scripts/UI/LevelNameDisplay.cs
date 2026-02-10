using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current level name as an overlay on the screen.
/// Simple, automatic setup - just add to any GameObject in the scene.
/// </summary>
public class LevelNameDisplay : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Text to display when no level is loaded")]
    public string noLevelText = "No Level";

    [Tooltip("Position on screen (0-1 range for both x and y)")]
    public Vector2 screenPosition = new Vector2(0.5f, 0.95f); // Top center

    [Tooltip("Font size")]
    public int fontSize = 24;

    [Tooltip("Text color")]
    public Color textColor = Color.white;

    [Header("Optional - Auto-created if null")]
    [Tooltip("Existing Text component to use (auto-creates if null)")]
    public Text textComponent;

    private Canvas canvas;
    private RectTransform textRect;
    private string lastDisplayedLevelName;

    private void Start()
    {
        SetupUI();
        UpdateLevelName();
    }

    private void Update()
    {
        // Update every frame in case level changes
        UpdateLevelName();
    }

    private void SetupUI()
    {
        // If no text component provided, create one
        if (textComponent == null)
        {
            // Create Canvas if needed
            canvas = ServiceRegistry.Get<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("LevelNameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // On top of everything

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Create Text GameObject
            GameObject textObj = new GameObject("LevelNameText");
            textObj.transform.SetParent(canvas.transform, false);

            textComponent = textObj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAnchor.MiddleCenter;

            // Add shadow for readability
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(2, -2);
            shadow.effectColor = new Color(0, 0, 0, 0.5f);

            // Position the text
            textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = screenPosition;
            textRect.anchorMax = screenPosition;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(800, 50);
        }
        else
        {
            // Use existing text component
            textRect = textComponent.GetComponent<RectTransform>();
        }
    }

    private void UpdateLevelName()
    {
        if (textComponent == null) return;

        string levelName = noLevelText;

        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelDef != null)
        {
            levelName = LevelManager.Instance.CurrentLevelDef.levelName;
        }

        if (lastDisplayedLevelName != levelName)
        {
            textComponent.text = levelName;
            lastDisplayedLevelName = levelName;
        }

        if (textComponent.fontSize != fontSize)
        {
            textComponent.fontSize = fontSize;
        }
        if (textComponent.color != textColor)
        {
            textComponent.color = textColor;
        }

        // Update position if changed
        if (textRect != null)
        {
            textRect.anchorMin = screenPosition;
            textRect.anchorMax = screenPosition;
        }
    }
}
