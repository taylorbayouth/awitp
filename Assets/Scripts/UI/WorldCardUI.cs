using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple world card UI for the overworld screen.
/// </summary>
public class WorldCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Text titleText;
    public Text statusText;
    public Button button;
    public Transform levelButtonContainer;
    public Button levelButtonPrefab;

    [Header("Theme Colors")]
    public Color lockedColor = Color.black;
    public Color inProgressColor = new Color(1f, 0.55f, 0.1f, 1f);
    public Color unlockedColor = new Color(0.1f, 0.75f, 0.2f, 1f);
    public Color titleColor = Color.white;
    public Color statusColor = Color.white;
    public Color levelTextColor = Color.white; // Changed to white for better visibility

    [Header("Typography")]
    public Font defaultFont;

    [Header("Debug")]
    public bool verboseLogs = false;

    [Header("Layout")]
    public float levelButtonHeight = 32f;
    public float levelButtonSpacing = 6f;
    public bool useManualLayout = true;

    private WorldData _world;
    private OverworldUI _owner;

    public void Initialize(WorldData world, OverworldUI owner, string statusLabel, bool isUnlocked, bool isComplete, bool isInProgress, bool isAvailable)
    {
        _world = world;
        _owner = owner;

        if (backgroundImage == null || (backgroundImage != null && !backgroundImage.transform.IsChildOf(transform)))
        {
            backgroundImage = GetComponent<Image>();
        }

        // Always find titleText fresh - don't trust serialized references from prefab
        titleText = FindTextByName("CardTitle");
        statusText = FindTextByName("CardStatus");

        // Debug
        Debug.Log($"[WorldCardUI] Initialize for world '{(world != null ? world.worldId : "null")}': titleText={(titleText != null ? titleText.name : "NULL")}, statusText={(statusText != null ? statusText.name : "NULL")}");

        if (titleText != null)
        {
            string worldName = world != null && !string.IsNullOrEmpty(world.worldName) ? world.worldName : "World";
            titleText.text = worldName;
            Debug.Log($"[WorldCardUI] Set title to '{worldName}'");
            if (defaultFont != null)
            {
                titleText.font = defaultFont;
            }
            titleText.color = titleColor;
            titleText.enabled = true;
            titleText.gameObject.SetActive(true);
            if (titleText.font != null)
            {
                titleText.font.RequestCharactersInTexture(titleText.text, titleText.fontSize, titleText.fontStyle);
                titleText.SetAllDirty();
            }
        }

        if (statusText != null)
        {
            statusText.text = statusLabel;
            if (defaultFont != null)
            {
                statusText.font = defaultFont;
            }
            statusText.color = statusColor;
            statusText.enabled = true;
            statusText.gameObject.SetActive(true);
            if (statusText.font != null)
            {
                statusText.font.RequestCharactersInTexture(statusText.text, statusText.fontSize, statusText.fontStyle);
                statusText.SetAllDirty();
            }
        }

        if (backgroundImage != null)
        {
            if (!isUnlocked)
            {
                backgroundImage.color = lockedColor;
            }
            else if (isInProgress)
            {
                backgroundImage.color = inProgressColor;
            }
            else
            {
                backgroundImage.color = unlockedColor;
            }
        }

        if (button != null)
        {
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private Text FindTextByName(string targetName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text t = texts[i];
            if (t != null && t.name == targetName)
            {
                return t;
            }
        }

        return null;
    }

    public void SetLevels(System.Collections.Generic.List<LevelDefinition> levels, bool isUnlocked)
    {
        if (levelButtonContainer == null || levelButtonPrefab == null)
        {
            Debug.LogWarning("[WorldCardUI] Missing level button container or prefab.");
            return;
        }

        if (verboseLogs)
        {
            RectTransform containerRect = levelButtonContainer as RectTransform;
            Debug.Log($"[WorldCardUI] SetLevels container '{levelButtonContainer.name}' active={levelButtonContainer.gameObject.activeSelf} childCount={levelButtonContainer.childCount} rect={(containerRect != null ? containerRect.rect.ToString() : "n/a")}");
            Debug.Log($"[WorldCardUI] LevelButtonTemplate active={levelButtonPrefab.gameObject.activeSelf} interactable={levelButtonPrefab.interactable}");
        }

        VerticalLayoutGroup layoutGroup = levelButtonContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = !useManualLayout;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = Mathf.Max(levelButtonSpacing, layoutGroup.spacing);
        }

        RectTransform containerRectLocal = levelButtonContainer as RectTransform;
        float totalHeight = levels != null ? (levels.Count * levelButtonHeight) + Mathf.Max(0, levels.Count - 1) * levelButtonSpacing : 0f;
        if (containerRectLocal != null)
        {
            containerRectLocal.sizeDelta = new Vector2(containerRectLocal.sizeDelta.x, totalHeight);
        }

        for (int i = levelButtonContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = levelButtonContainer.GetChild(i);
            if (child != null && child.gameObject != levelButtonPrefab.gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning($"[WorldCardUI] No levels to display for world '{(_world != null ? _world.worldId : "null")}'. Levels list is {(levels == null ? "null" : "empty")}.");
            levelButtonPrefab.gameObject.SetActive(false);
            return;
        }

        Debug.Log($"[WorldCardUI] Creating {levels.Count} level buttons for world '{(_world != null ? _world.worldId : "null")}', isUnlocked={isUnlocked}");

        for (int i = 0; i < levels.Count; i++)
        {
            LevelDefinition level = levels[i];
            Button buttonInstance = Instantiate(levelButtonPrefab, levelButtonContainer);
            buttonInstance.gameObject.SetActive(true);

            RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, levelButtonHeight);
                if (useManualLayout)
                {
                    float y = -(i * (levelButtonHeight + levelButtonSpacing));
                    buttonRect.anchoredPosition = new Vector2(0f, y);
                }
            }

            LayoutElement layoutElement = buttonInstance.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = buttonInstance.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = levelButtonHeight;
            layoutElement.minHeight = levelButtonHeight;
            layoutElement.flexibleHeight = 0f;

            Text label = buttonInstance.GetComponentInChildren<Text>();
            if (label != null)
            {
                string name = level != null && !string.IsNullOrEmpty(level.levelName) ? level.levelName : "Level";

                // Set font FIRST before other properties
                if (defaultFont != null)
                {
                    label.font = defaultFont;
                }

                // Then set style, text, and color
                label.fontStyle = FontStyle.Bold;
                label.text = name;
                label.color = levelTextColor;
                label.enabled = true;
                label.gameObject.SetActive(true);

                // Request characters in texture with Bold style
                if (label.font != null)
                {
                    label.font.RequestCharactersInTexture(label.text, label.fontSize, FontStyle.Bold);
                }

                label.SetAllDirty();

                if (verboseLogs)
                {
                    RectTransform labelRect = label.GetComponent<RectTransform>();
                    Debug.Log($"[WorldCardUI] Level label '{label.text}' active={label.gameObject.activeSelf} rect={(labelRect != null ? labelRect.rect.ToString() : "n/a")} font={(label.font != null ? label.font.name : "null")}");
                }
            }
            else
            {
                Debug.LogWarning($"[WorldCardUI] Button instance has no Text component in children!");
            }

            buttonInstance.interactable = isUnlocked;
            buttonInstance.onClick.RemoveAllListeners();
            if (isUnlocked && _owner != null && level != null)
            {
                buttonInstance.onClick.AddListener(() => _owner.OnLevelSelected(level));
            }
        }

        if (layoutGroup != null && !useManualLayout)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(levelButtonContainer as RectTransform);
        }

        levelButtonPrefab.gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (_owner != null && _world != null)
        {
            _owner.OnWorldSelected(_world);
        }
    }
}
