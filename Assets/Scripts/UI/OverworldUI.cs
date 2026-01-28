using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Overworld UI controller.
/// Builds a simple world card grid using WorldManager + ProgressManager.
/// </summary>
public class OverworldUI : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Text subtitleText;
    public RectTransform cardContainer;
    public WorldCardUI cardPrefab;
    public Font defaultFont;

    [Header("Layout")]
    public int columns = 3;
    public Vector2 cardSize = new Vector2(360f, 240f);
    public Vector2 cardSpacing = new Vector2(80f, 60f);

    [Header("Navigation")]
    [Tooltip("Scene to load when a level is selected.")]
    public string gameSceneName = "Master";

    [Header("Font Override")]
    [Tooltip("Force legacy built-in Arial font on all Overworld text.")]
    public bool forceLegacyFont = true;

    [Header("Debug")]
    public bool verboseLogs = false;

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
    }

    private void EnsureManagers()
    {
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

        WorldData[] worlds = WorldManager.Instance != null ? WorldManager.Instance.allWorlds : null;
        if (worlds == null || worlds.Length == 0)
        {
            Debug.LogWarning("[OverworldUI] No worlds found.");
            return;
        }

        int total = worlds.Length;
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

            bool isUnlocked = WorldManager.Instance != null && WorldManager.Instance.IsWorldUnlocked(world.worldId);
            bool isComplete = WorldManager.Instance != null && WorldManager.Instance.IsWorldComplete(world.worldId);

            string statusLabel = isComplete ? "World Complete" : (isUnlocked ? "World In Progress" : "Locked");
            string worldId = world != null ? world.worldId : string.Empty;
            List<LevelDefinition> levels = LoadLevelsForWorld(worldId);
            Debug.Log($"[OverworldUI] World '{worldId}' has {levels.Count} levels from Resources.");

            card.Initialize(world, this, statusLabel, isUnlocked, isComplete);
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

        LevelDefinition[] allLevels = Resources.LoadAll<LevelDefinition>("Levels/LevelDefinitions");
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

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.SetCurrentWorld(world.worldId);
        }

        PlayerPrefs.SetString("SelectedWorldId", world.worldId);
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

        PlayerPrefs.SetString("PendingLevelId", level.levelId);
        PlayerPrefs.Save();

        Debug.Log($"[OverworldUI] Loading level: {level.levelName} ({level.levelId})");
        SceneManager.LoadScene(gameSceneName);
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
