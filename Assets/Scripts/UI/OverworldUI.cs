using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Overworld UI controller.
/// Builds a simple world card grid using WorldManager + ProgressManager.
/// Supports a "world reveal" sequence when a new world is unlocked.
/// </summary>
public class OverworldUI : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Text subtitleText;
    public RectTransform cardContainer;
    public WorldCardUI cardPrefab;
    public Font defaultFont;

    [Header("World Reveal Overlay")]
    [Tooltip("Root panel that covers the screen during the world reveal countdown. Created at runtime if null.")]
    public GameObject revealOverlayPanel;

    [Tooltip("Text displaying the countdown or reveal message. Created at runtime if null.")]
    public Text revealText;

    [Tooltip("Duration of the countdown before revealing the new world (seconds)")]
    public float revealCountdownDuration = 3f;

    [Header("Layout")]
    public int columns = 3;
    public Vector2 cardSize = new Vector2(360f, 240f);
    public Vector2 cardSpacing = new Vector2(80f, 60f);

    [Header("Navigation")]
    [Tooltip("Scene to load when a level is selected.")]
    public string gameSceneName = GameConstants.SceneNames.Gameplay;
    [Tooltip("Scene to load when backing out to main menu.")]
    public string mainMenuSceneName = GameConstants.SceneNames.MainMenu;
    [Tooltip("Optional back button to return to main menu.")]
    public Button backButton;

    [Header("Font Override")]
    [Tooltip("Force legacy built-in Arial font on all Overworld text.")]
    public bool forceLegacyFont = true;

    [Header("Debug")]
    public bool verboseLogs = false;

    private bool _revealInProgress = false;

    private void Start()
    {
        EnsureManagers();

        if (forceLegacyFont)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ApplyFontToAllText(defaultFont);
        }

        if (titleText != null)
        {
            titleText.text = "Overworld";
            if (defaultFont != null)
            {
                titleText.font = defaultFont;
            }
            if (titleText.font != null)
            {
                titleText.font.RequestCharactersInTexture(titleText.text, titleText.fontSize, titleText.fontStyle);
                titleText.SetAllDirty();
            }
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Select a world";
            if (defaultFont != null)
            {
                subtitleText.font = defaultFont;
            }
            if (subtitleText.font != null)
            {
                subtitleText.font.RequestCharactersInTexture(subtitleText.text, subtitleText.fontSize, subtitleText.fontStyle);
                subtitleText.SetAllDirty();
            }
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackToMainMenu);
        }

        BuildWorldCards();
        if (forceLegacyFont && defaultFont != null)
        {
            ApplyFontToAllText(defaultFont);
        }
        Canvas.ForceUpdateCanvases();

        if (verboseLogs)
        {
            DumpTextState();
        }

        // Check for a pending world reveal
        CheckPendingWorldReveal();
    }

    private void EnsureManagers()
    {
        _ = WorldManager.Instance;
        _ = ProgressManager.Instance;
        _ = LevelManager.Instance;
    }

    #region World Reveal

    /// <summary>
    /// Checks if there is a pending world reveal (set by VictoryScreenUI when a new world was unlocked).
    /// If so, starts the reveal countdown sequence.
    /// </summary>
    private void CheckPendingWorldReveal()
    {
        string pendingWorldId = PlayerPrefs.GetString(GameConstants.PlayerPrefsKeys.PendingWorldReveal, "");
        if (string.IsNullOrEmpty(pendingWorldId))
        {
            return;
        }

        // Clear the flag immediately so it doesn't re-trigger
        PlayerPrefs.DeleteKey(GameConstants.PlayerPrefsKeys.PendingWorldReveal);
        PlayerPrefs.Save();

        // Find the world data for the reveal
        WorldData revealWorld = WorldManager.Instance != null
            ? WorldManager.Instance.GetWorld(pendingWorldId)
            : null;

        string worldName = revealWorld != null ? revealWorld.worldName : pendingWorldId;
        Debug.Log($"[OverworldUI] Starting world reveal for: {worldName}");

        StartCoroutine(WorldRevealSequence(worldName));
    }

    /// <summary>
    /// Coroutine that shows a countdown overlay, then reveals the new world.
    /// This is a placeholder sequence - replace with a proper cutscene/animation later.
    /// </summary>
    private IEnumerator WorldRevealSequence(string worldName)
    {
        _revealInProgress = true;

        // Ensure we have an overlay panel (create at runtime if not assigned in inspector)
        EnsureRevealOverlay();

        if (revealOverlayPanel != null)
        {
            revealOverlayPanel.SetActive(true);
        }

        // Countdown phase
        float remaining = revealCountdownDuration;
        while (remaining > 0f)
        {
            if (revealText != null)
            {
                int seconds = Mathf.CeilToInt(remaining);
                revealText.text = $"New world discovered...\n\n{seconds}";
            }
            remaining -= Time.deltaTime;
            yield return null;
        }

        // Reveal announcement
        if (revealText != null)
        {
            revealText.text = $"{worldName}\nUnlocked!";
        }

        // Hold the reveal message for a moment
        yield return new WaitForSeconds(2f);

        // Hide overlay
        if (revealOverlayPanel != null)
        {
            revealOverlayPanel.SetActive(false);
        }

        _revealInProgress = false;

        // Rebuild cards so the new world is now visible and interactable
        BuildWorldCards();
        if (forceLegacyFont && defaultFont != null)
        {
            ApplyFontToAllText(defaultFont);
        }
        Canvas.ForceUpdateCanvases();

        Debug.Log($"[OverworldUI] World reveal complete: {worldName}");
    }

    /// <summary>
    /// Creates a fullscreen reveal overlay at runtime if one wasn't assigned in the inspector.
    /// </summary>
    private void EnsureRevealOverlay()
    {
        if (revealOverlayPanel != null && revealText != null) return;

        // Find or create a Canvas to parent our overlay
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null) return;

        // Create overlay panel
        if (revealOverlayPanel == null)
        {
            GameObject panel = new GameObject("WorldRevealOverlay");
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            // Ensure it renders on top
            Canvas overlayCanvas = panel.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 100;
            panel.AddComponent<GraphicRaycaster>();

            revealOverlayPanel = panel;
        }

        // Create text
        if (revealText == null)
        {
            GameObject textObj = new GameObject("RevealText");
            textObj.transform.SetParent(revealOverlayPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.2f);
            textRect.anchorMax = new Vector2(0.9f, 0.8f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            revealText = textObj.AddComponent<Text>();
            revealText.alignment = TextAnchor.MiddleCenter;
            revealText.fontSize = 48;
            revealText.color = Color.white;
            revealText.fontStyle = FontStyle.Bold;

            if (defaultFont != null)
            {
                revealText.font = defaultFont;
            }
            else
            {
                revealText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        revealOverlayPanel.SetActive(false);
    }

    #endregion

    private void BuildWorldCards()
    {
        if (cardContainer == null || cardPrefab == null)
        {
            Debug.LogWarning("[OverworldUI] Missing cardContainer or cardPrefab reference.");
            return;
        }

        // Clear existing (except template)
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = cardContainer.GetChild(i);
            if (child != null && child.gameObject != cardPrefab.gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        WorldManager worldManager = WorldManager.Instance;
        var worlds = worldManager.Worlds;
        if (worlds == null || worlds.Count == 0)
        {
            Debug.LogWarning("[OverworldUI] No worlds found.");
            return;
        }

        int total = worlds.Count;
        int cols = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt(total / (float)cols);

        float totalWidth = cols * cardSize.x + (cols - 1) * cardSpacing.x;
        float totalHeight = rows * cardSize.y + (rows - 1) * cardSpacing.y;

        for (int i = 0; i < total; i++)
        {
            WorldData world = worlds[i];
            WorldCardUI card = Instantiate(cardPrefab, cardContainer);
            card.gameObject.SetActive(true);
            card.defaultFont = defaultFont;

            int col = i % cols;
            int row = i / cols;

            float x = -totalWidth * 0.5f + col * (cardSize.x + cardSpacing.x) + cardSize.x * 0.5f;
            float y = totalHeight * 0.5f - row * (cardSize.y + cardSpacing.y) - cardSize.y * 0.5f;

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = cardSize;
            rect.anchoredPosition = new Vector2(x, y);

            bool isUnlocked = worldManager.IsWorldUnlocked(world.worldId);
            if (!isUnlocked)
            {
                // Locked worlds should not be visible or accessible - destroy the card we just created
                Destroy(card.gameObject);
                continue;
            }

            int completedLevels = worldManager.GetCompletedLevelCount(world.worldId);
            int totalLevels = world != null ? world.LevelCount : 0;
            bool isComplete = totalLevels > 0 && completedLevels >= totalLevels;
            bool hasProgress = completedLevels > 0 && !isComplete;
            bool isAvailable = !hasProgress && !isComplete;

            string statusLabel = isComplete ? "Unlocked" : (hasProgress ? "In Progress" : "Unlocked");
            string worldId = world != null ? world.worldId : string.Empty;
            List<LevelDefinition> levels = LoadLevelsForWorld(worldId);
            if (verboseLogs)
            {
                Debug.Log($"[OverworldUI] World '{worldId}' has {levels.Count} levels from Resources.");
            }

            card.Initialize(world, this, statusLabel, isUnlocked, isComplete, hasProgress, isAvailable);
            card.verboseLogs = verboseLogs;
            card.SetLevels(levels, isUnlocked);
        }

        cardPrefab.gameObject.SetActive(false);
    }

    private List<LevelDefinition> LoadLevelsForWorld(string worldId)
    {
        List<LevelDefinition> results = new List<LevelDefinition>();
        if (string.IsNullOrEmpty(worldId))
        {
            return results;
        }

        LevelDefinition[] allLevels = Resources.LoadAll<LevelDefinition>(GameConstants.ResourcePaths.LevelDefinitionsRoot);
        if (allLevels == null || allLevels.Length == 0)
        {
            Debug.LogWarning("[OverworldUI] No LevelDefinition assets found in Resources/Levels/LevelDefinitions.");
            return results;
        }

        Debug.Log($"[OverworldUI] Loaded {allLevels.Length} LevelDefinition assets from Resources.");
        for (int i = 0; i < allLevels.Length; i++)
        {
            LevelDefinition level = allLevels[i];
            if (level != null && level.worldId == worldId)
            {
                results.Add(level);
            }
        }

        results.Sort((a, b) => a.orderInWorld.CompareTo(b.orderInWorld));
        return results;
    }

    public void OnWorldSelected(WorldData world)
    {
        if (world == null)
        {
            Debug.LogWarning("[OverworldUI] Selected world is null.");
            return;
        }

        WorldManager.Instance.SetCurrentWorld(world.worldId);

        PlayerPrefs.SetString(GameConstants.PlayerPrefsKeys.SelectedWorldId, world.worldId);
        PlayerPrefs.Save();

        Debug.Log($"[OverworldUI] World selected: {world.worldName}");

        // World click doesn't load a scene now; levels handle navigation.
    }

    public void OnLevelSelected(LevelDefinition level)
    {
        if (level == null)
        {
            Debug.LogWarning("[OverworldUI] Selected level is null.");
            return;
        }

        PlayerPrefs.SetString(GameConstants.PlayerPrefsKeys.PendingLevelId, level.levelId);
        PlayerPrefs.Save();

        Debug.Log($"[OverworldUI] Loading level: {level.levelName} ({level.levelId})");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnBackToMainMenu()
    {
        Debug.Log("[OverworldUI] Back to main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ApplyFontToAllText(Font font)
    {
        if (font == null) return;
        Text[] allText = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
        foreach (Text t in allText)
        {
            if (t == null) continue;
            if (t.font == null)
            {
                t.font = font;
            }
            t.SetAllDirty();
        }

        TextMesh[] allTextMeshes = UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
        foreach (TextMesh tm in allTextMeshes)
        {
            if (tm == null) continue;
            if (tm.font == null)
            {
                tm.font = font;
            }
        }
    }

    private void DumpTextState()
    {
        Text[] allText = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
        Debug.Log($"[OverworldUI] Text components found: {allText.Length}");
        foreach (Text t in allText)
        {
            if (t == null) continue;
            RectTransform rect = t.GetComponent<RectTransform>();
            Debug.Log($"[OverworldUI] Text '{t.name}' active={t.gameObject.activeSelf} enabled={t.enabled} text='{t.text}' font={(t.font != null ? t.font.name : "null")} rect={(rect != null ? rect.rect.ToString() : "n/a")}");
        }

        TextMesh[] allTextMeshes = UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
        Debug.Log($"[OverworldUI] TextMesh components found: {allTextMeshes.Length}");
        foreach (TextMesh tm in allTextMeshes)
        {
            if (tm == null) continue;
            Debug.Log($"[OverworldUI] TextMesh '{tm.name}' active={tm.gameObject.activeSelf} text='{tm.text}' font={(tm.font != null ? tm.font.name : "null")}");
        }
    }
}
