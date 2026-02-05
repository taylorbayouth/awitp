using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Displays block inventory counts and current selection using UGUI.
/// </summary>
[ExecuteAlways]
public class InventoryUI : MonoBehaviour
{
    private static InventoryUI _instance;

    [Header("References")]
    public BlockInventory inventory;
    public BuilderController builderController;

    [Header("UI Settings")]
    public float boxSize = 110f;
    [Tooltip("Space between icon blocks")]
    [Range(0f, 60f)]
    public float blockSpacing = 20f;
    public float iconMargin = 5f;
    public float topMargin = 24f;
    public float leftMargin = 24f;

    [Header("UGUI Settings")]
    public Font font;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 1f;

    [Header("Text")]
    [Range(8, 48)]
    public int textSize = 20;
    [Range(-20f, 60f)]
    public float textTopMargin = 0f;
    public Color textColor = Color.white;

    [Header("Corner Text")]
    public int cornerFontSize = 18;

    [Header("Icons")]
    [Tooltip("Optional sprite override for the Walk block inventory preview")]
    public Sprite walkBlockSprite;

    [Tooltip("Resources path fallback for the Walk icon (without extension)")]
    public string walkBlockSpriteResourcePath = "Sprites/inventoryUi/hedgeIcon";

    [Tooltip("Editor-only asset path fallback for the Walk icon")]
    public string walkBlockSpriteAssetPath = "Assets/Sprites/inventoryUi/hedgeIcon.png";

    [Tooltip("Optional texture override for the Transporter inventory preview")]
    public Texture2D transporterIconTexture;

    [Tooltip("Resources path fallback for the Transporter icon (without extension)")]
    public string transporterIconTextureResourcePath = "Sprites/inventoryUi/cloudIcon";

    [Tooltip("Editor-only asset path fallback for the Transporter icon")]
    public string transporterIconTextureAssetPath = "Assets/Sprites/inventoryUi/cloudIcon.png";

    [Header("Font")]
    [Tooltip("Editor-only asset path for the UI font")]
    public string uiFontAssetPath = "Assets/Fonts/Koulen-Regular.ttf";

    [Tooltip("Resources path fallback for the UI font (without extension)")]
    public string uiFontResourcePath = "Fonts/Koulen-Regular";

    private Canvas _canvas;
    private RectTransform _root;
    private GameObject _frameContainer;
    private InventoryPanelFrame _frameComponent;
    private RectTransform _panel;
    private VerticalLayoutGroup _layout;
    private ContentSizeFitter _fitter;
    private Text _lockStatusText;
    private Text _winText;

    private readonly List<SlotUI> _slots = new List<SlotUI>();
    private readonly Dictionary<string, Texture2D> _transporterIconCache = new Dictionary<string, Texture2D>();

    private class SlotUI
    {
        public GameObject root;
        public Image previewImage;
        public RawImage previewRaw;
        public Text count;
        public BlockInventoryEntry entry;
        public int index;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(this);
            }
            return;
        }

        _instance = this;

        LoadUIFont();
        LoadWalkBlockSprite();
        LoadTransporterIconTexture();
        EnsureCanvas();
        BindOrCreateStaticUI();
        RebuildSlotCache();
    }

    private void OnEnable()
    {
        LoadUIFont();
        LoadWalkBlockSprite();
        LoadTransporterIconTexture();
        EnsureCanvas();
        BindOrCreateStaticUI();
        RebuildSlotCache();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (inventory == null)
            {
                inventory = ServiceRegistry.Get<BlockInventory>();
            }

            if (builderController == null)
            {
                builderController = ServiceRegistry.Get<BuilderController>();
            }
        }

        UpdateUI();
    }

    private void EnsureCanvas()
    {
        if (_canvas != null) return;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
        }

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        _root = _canvas.GetComponent<RectTransform>();
    }

    private void BindOrCreateStaticUI()
    {
        if (_root == null)
        {
            EnsureCanvas();
        }

        CleanupLegacyOrDuplicateUI();

        if (_frameContainer != null && _frameComponent != null && _panel != null && _lockStatusText != null && _winText != null)
        {
            return;
        }

        // Frame container with border slices
        if (_frameContainer == null)
        {
            Transform existingFrame = _root != null ? _root.Find("InventoryFrame") : null;
            if (existingFrame != null)
            {
                _frameContainer = existingFrame.gameObject;
            }
        }

        if (_frameContainer == null)
        {
            _frameContainer = new GameObject("InventoryFrame", typeof(RectTransform), typeof(InventoryPanelFrame));
            _frameContainer.transform.SetParent(_root, false);
        }

        RectTransform frameRect = _frameContainer.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 1f);
        frameRect.anchorMax = new Vector2(0f, 1f);
        frameRect.pivot = new Vector2(0f, 1f);
        frameRect.anchoredPosition = new Vector2(leftMargin, -topMargin);

        _frameComponent = _frameContainer.GetComponent<InventoryPanelFrame>();
        if (_frameComponent == null)
        {
            _frameComponent = _frameContainer.AddComponent<InventoryPanelFrame>();
        }
        _frameComponent.backgroundColor = new Color(0.38f, 0.36f, 0.34f, 1f); // #615C57

        // Inventory panel inside the frame's content area
        RectTransform contentArea = _frameComponent.GetContentArea();
        if (_panel == null && contentArea != null)
        {
            Transform existingPanel = contentArea.Find("InventoryPanel");
            if (existingPanel != null)
            {
                _panel = existingPanel.GetComponent<RectTransform>();
            }
        }

        if (_panel == null)
        {
            GameObject panelObj = new GameObject("InventoryPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObj.transform.SetParent(contentArea != null ? contentArea : _root, false);
            _panel = panelObj.GetComponent<RectTransform>();
        }

        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _panel.anchoredPosition = Vector2.zero;

        _layout = _panel.GetComponent<VerticalLayoutGroup>();
        if (_layout == null)
        {
            _layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        _layout.spacing = blockSpacing;
        _layout.childAlignment = TextAnchor.UpperLeft;
        _layout.childControlWidth = false;
        _layout.childControlHeight = false;
        _layout.childForceExpandWidth = false;
        _layout.childForceExpandHeight = false;

        _fitter = _panel.GetComponent<ContentSizeFitter>();
        if (_fitter == null)
        {
            _fitter = _panel.gameObject.AddComponent<ContentSizeFitter>();
        }
        _fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _lockStatusText = FindOrCreateText("LockStatus", _root, cornerFontSize, TextAnchor.UpperRight, Color.white);
        RectTransform lockRect = _lockStatusText != null ? _lockStatusText.GetComponent<RectTransform>() : null;
        lockRect.anchorMin = new Vector2(1f, 1f);
        lockRect.anchorMax = new Vector2(1f, 1f);
        lockRect.pivot = new Vector2(1f, 1f);
        lockRect.anchoredPosition = new Vector2(-24f, -20f);

        _winText = FindOrCreateText("WinText", _root, cornerFontSize, TextAnchor.UpperRight, new Color(0.8f, 1f, 0.8f));
        RectTransform winRect = _winText != null ? _winText.GetComponent<RectTransform>() : null;
        winRect.anchorMin = new Vector2(1f, 1f);
        winRect.anchorMax = new Vector2(1f, 1f);
        winRect.pivot = new Vector2(1f, 1f);
        winRect.anchoredPosition = new Vector2(-24f, -46f);
        _winText.text = string.Empty;
    }

    private void CleanupLegacyOrDuplicateUI()
    {
        if (_root == null) return;

        // InventoryUI previously generated many UI children in edit mode on script reload.
        // Clean up obvious duplicates/legacy names under this UI root.
        string[] keepOneNames = { "InventoryFrame", "LockStatus", "WinText" };
        for (int n = 0; n < keepOneNames.Length; n++)
        {
            string targetName = keepOneNames[n];
            List<Transform> matches = null;
            for (int i = 0; i < _root.childCount; i++)
            {
                Transform child = _root.GetChild(i);
                if (child != null && child.name == targetName)
                {
                    matches ??= new List<Transform>();
                    matches.Add(child);
                }
            }

            if (matches == null || matches.Count <= 1) continue;

            Transform keeper = matches[0];
            if (targetName == "InventoryFrame")
            {
                // Prefer the frame that already contains content (slots, etc.).
                int bestChildren = keeper != null ? keeper.childCount : -1;
                for (int i = 1; i < matches.Count; i++)
                {
                    Transform candidate = matches[i];
                    if (candidate == null) continue;
                    int candidateChildren = candidate.childCount;
                    if (candidateChildren > bestChildren)
                    {
                        keeper = candidate;
                        bestChildren = candidateChildren;
                    }
                }
            }

            for (int i = 0; i < matches.Count; i++)
            {
                Transform duplicate = matches[i];
                if (duplicate == null || duplicate == keeper) continue;
                if (Application.isPlaying)
                {
                    Destroy(duplicate.gameObject);
                }
                else
                {
                    DestroyImmediate(duplicate.gameObject);
                }
            }
        }

        // Legacy name from older UI versions.
        for (int i = _root.childCount - 1; i >= 0; i--)
        {
            Transform child = _root.GetChild(i);
            if (child != null && child.name == "Status")
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private Text FindOrCreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
    {
        if (parent == null) return null;

        Transform existing = parent.Find(name);
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text == null)
        {
            text = CreateText(name, parent, fontSize, anchor, color);
        }
        else
        {
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
        return text;
    }

    private void RebuildSlotCache()
    {
        if (_panel == null) return;

        _slots.Clear();

        for (int i = 0; i < _panel.childCount; i++)
        {
            Transform child = _panel.GetChild(i);
            if (child == null || child.name != "InventorySlot") continue;

            GameObject root = child.gameObject;
            Image previewImage = null;
            RawImage previewRaw = null;
            Text count = null;

            Transform previewTransform = child.Find("Background/Preview");
            if (previewTransform != null)
            {
                previewImage = previewTransform.GetComponent<Image>();
                Transform rawTransform = previewTransform.Find("PreviewRaw");
                if (rawTransform != null)
                {
                    previewRaw = rawTransform.GetComponent<RawImage>();
                }
            }

            Transform countTransform = child.Find("Count");
            if (countTransform != null)
            {
                count = countTransform.GetComponent<Text>();
            }

            _slots.Add(new SlotUI
            {
                root = root,
                previewImage = previewImage,
                previewRaw = previewRaw,
                count = count
            });
        }
    }

    private void UpdateUI()
    {
        if (_panel == null) return;

        GameMode mode = builderController != null ? builderController.currentMode : GameMode.Builder;
        bool showInventory = !Application.isPlaying || mode != GameMode.Play;

        if (_layout != null)
        {
            _layout.spacing = blockSpacing;
        }

        // Show/hide the frame container instead of just the panel
        if (_frameContainer != null)
        {
            _frameContainer.SetActive(showInventory);
        }
        else
        {
            _panel.gameObject.SetActive(showInventory);
        }

        if (inventory == null)
        {
            return;
        }

        if (Application.isPlaying && builderController == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UpdateLockStatus();
        }
        else
        {
            _lockStatusText.text = string.Empty;
            _winText.text = string.Empty;
        }

        if (!showInventory) return;

        IReadOnlyList<BlockInventoryEntry> entries = inventory.GetEntriesForMode(mode);
        bool showInfinite = mode == GameMode.Designer;

        int drawIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (ShouldHideFromInventory(entries[i], mode)) continue;

            SlotUI slot = EnsureSlot(drawIndex);
            slot.entry = entries[i];
            slot.index = i;
            UpdateSlot(slot, showInfinite);
            drawIndex++;
        }

        TrimSlots(drawIndex);

        if (drawIndex == 0) { }
    }

    private SlotUI EnsureSlot(int drawIndex)
    {
        if (drawIndex < _slots.Count)
        {
            _slots[drawIndex].root.SetActive(true);
            return _slots[drawIndex];
        }

        SlotUI slot = CreateSlot();
        _slots.Add(slot);
        return slot;
    }

    private void TrimSlots(int count)
    {
        for (int i = count; i < _slots.Count; i++)
        {
            _slots[i].root.SetActive(false);
        }
    }

    private SlotUI CreateSlot()
    {
        GameObject root = new GameObject("InventorySlot", typeof(RectTransform), typeof(LayoutElement));
        root.transform.SetParent(_panel, false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = boxSize;
        layout.preferredHeight = boxSize + textTopMargin + textSize + 6f;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(boxSize, layout.preferredHeight);

        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 1f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(boxSize, boxSize);

        GameObject previewObj = new GameObject("Preview", typeof(RectTransform), typeof(Image));
        previewObj.transform.SetParent(bgRect, false);
        RectTransform previewRect = previewObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = Vector2.zero;
        float previewSize = Mathf.Max(0f, boxSize - (iconMargin * 2f));
        previewRect.sizeDelta = new Vector2(previewSize, previewSize);
        Image previewImage = previewObj.GetComponent<Image>();
        previewImage.preserveAspect = true;

        GameObject previewRawObj = new GameObject("PreviewRaw", typeof(RectTransform), typeof(RawImage));
        previewRawObj.transform.SetParent(previewRect, false);
        RectTransform rawRect = previewRawObj.GetComponent<RectTransform>();
        rawRect.anchorMin = new Vector2(0f, 0f);
        rawRect.anchorMax = new Vector2(1f, 1f);
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;
        RawImage previewRaw = previewRawObj.GetComponent<RawImage>();
        previewRaw.gameObject.SetActive(false);

        Text count = CreateText("Count", rootRect, textSize, TextAnchor.UpperCenter, textColor);
        RectTransform countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 1f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(0.5f, 1f);
        countRect.anchoredPosition = new Vector2(0f, -boxSize - textTopMargin);
        countRect.sizeDelta = new Vector2(0f, textSize + 6f);

        return new SlotUI
        {
            root = root,
            previewImage = previewImage,
            previewRaw = previewRaw,
            count = count
        };
    }

    private void UpdateSlot(SlotUI slot, bool showInfinite)
    {
        BlockInventoryEntry entry = slot.entry;
        if (entry == null) return;

        RectTransform rootRect = slot.root.GetComponent<RectTransform>();
        LayoutElement layout = slot.root.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.preferredWidth = boxSize;
            layout.preferredHeight = boxSize + textTopMargin + textSize + 6f;
        }
        if (rootRect != null)
        {
            rootRect.sizeDelta = new Vector2(boxSize, boxSize + textTopMargin + textSize + 6f);
        }

        RectTransform previewRect = slot.previewImage != null ? slot.previewImage.rectTransform : null;
        if (previewRect != null)
        {
            RectTransform bgRect = previewRect.parent as RectTransform;
            if (bgRect != null)
            {
                bgRect.sizeDelta = new Vector2(boxSize, boxSize);
            }
            float previewSize = Mathf.Max(0f, boxSize - (iconMargin * 2f));
            previewRect.sizeDelta = new Vector2(previewSize, previewSize);
        }

        int available = inventory.GetDisplayAvailableCount(entry);
        int total = inventory.GetDisplayTotalCount(entry);
        slot.count.text = showInfinite ? "INF" : $"{available} OF {total}";
        slot.count.fontSize = textSize;
        slot.count.color = textColor;
        RectTransform countRect = slot.count.rectTransform;
        countRect.anchoredPosition = new Vector2(0f, -boxSize - textTopMargin);

        Color blockColor = GetColorForBlockType(entry.blockType);
        if (!showInfinite && available == 0)
        {
            blockColor.a = 0.3f;
        }

        if (entry.blockType == BlockType.Walk && walkBlockSprite != null)
        {
            slot.previewImage.enabled = true;
            slot.previewImage.sprite = walkBlockSprite;
            slot.previewImage.color = showInfinite || available > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            slot.previewRaw.gameObject.SetActive(false);
        }
        else if (entry.blockType == BlockType.Transporter)
        {
            if (TrySetTransporterPreview(entry, slot.previewRaw, blockColor, slot.previewImage.rectTransform.rect.size))
            {
                slot.previewImage.enabled = false;
            }
            else
            {
                slot.previewImage.enabled = true;
                slot.previewImage.sprite = null;
                slot.previewImage.color = blockColor;
                slot.previewRaw.gameObject.SetActive(false);
            }
        }
        else
        {
            slot.previewImage.enabled = true;
            slot.previewImage.sprite = null;
            slot.previewImage.color = blockColor;
            slot.previewRaw.gameObject.SetActive(false);
        }

        if (slot.previewImage != null)
        {
            slot.previewImage.preserveAspect = true;
        }
    }

    private bool TrySetTransporterPreview(BlockInventoryEntry entry, RawImage rawImage, Color blockColor, Vector2 size)
    {
        string[] steps = ResolveRouteSteps(entry);
        if (steps == null || steps.Length == 0) return false;

        int textureSize = Mathf.RoundToInt(Mathf.Min(size.x, size.y));
        if (textureSize <= 0) return false;

        LoadTransporterIconTexture();
        Color overlayColor = blockColor;
        if (overlayColor.r >= 0.95f && overlayColor.g >= 0.95f && overlayColor.b >= 0.95f)
        {
            overlayColor = new Color(0.15f, 0.55f, 1f, 1f);
        }
        Texture2D icon = GetTransporterRouteTexture(steps, textureSize, overlayColor);
        if (icon == null) return false;

        rawImage.texture = icon;
        rawImage.color = Color.white;
        rawImage.gameObject.SetActive(true);
        return true;
    }

    private void UpdateLockStatus()
    {
        LockBlock[] locks = UnityEngine.Object.FindObjectsByType<LockBlock>(FindObjectsSortMode.None);
        int totalLocks = locks.Length;
        int lockedCount = 0;
        foreach (LockBlock lockBlock in locks)
        {
            if (lockBlock != null && lockBlock.HasKeyLocked())
            {
                lockedCount++;
            }
        }

        _lockStatusText.text = totalLocks > 0 ? $"{lockedCount} of {totalLocks}" : string.Empty;

        if (builderController != null && builderController.currentMode == GameMode.Play && totalLocks > 0 && lockedCount >= totalLocks)
        {
            _winText.text = "You win";
        }
        else
        {
            _winText.text = string.Empty;
        }
    }

    private Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void LoadUIFont()
    {
        Font loadedFont = null;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(uiFontAssetPath))
        {
            loadedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(uiFontAssetPath);
        }
#endif

        if (loadedFont == null)
        {
            loadedFont = Resources.Load<Font>(uiFontResourcePath);
        }

        if (loadedFont == null)
        {
            loadedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        font = loadedFont;
    }

    private void LoadWalkBlockSprite()
    {
        if (walkBlockSprite != null) return;

        Sprite sprite = Resources.Load<Sprite>(walkBlockSpriteResourcePath);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(walkBlockSpriteResourcePath);
            if (texture != null)
            {
                sprite = CreateSprite(texture);
            }
        }

#if UNITY_EDITOR
        if (sprite == null && !string.IsNullOrEmpty(walkBlockSpriteAssetPath))
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(walkBlockSpriteAssetPath);
            if (sprite == null)
            {
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(walkBlockSpriteAssetPath);
                if (texture != null)
                {
                    sprite = CreateSprite(texture);
                }
            }
        }
#endif

        walkBlockSprite = sprite;
    }

    private void LoadTransporterIconTexture()
    {
        if (transporterIconTexture != null) return;

        Texture2D texture = Resources.Load<Texture2D>(transporterIconTextureResourcePath);

#if UNITY_EDITOR
        if (texture == null && !string.IsNullOrEmpty(transporterIconTextureAssetPath))
        {
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(transporterIconTextureAssetPath);
        }
#endif

        transporterIconTexture = texture;
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private bool ShouldHideFromInventory(BlockInventoryEntry entry, GameMode mode)
    {
        if (entry == null) return true;
        if (mode == GameMode.Designer) return false;
        return entry.blockType == BlockType.Key || entry.blockType == BlockType.Lock;
    }

    private Color GetColorForBlockType(BlockType blockType)
    {
        return BlockColors.GetColorForBlockType(blockType);
    }

    private Texture2D GetTransporterRouteTexture(string[] routeSteps, int size, Color overlayColor)
    {
        string cacheKey = BuildRouteCacheKey(routeSteps, size);
        cacheKey = $"{cacheKey}_{overlayColor.r:F3}_{overlayColor.g:F3}_{overlayColor.b:F3}";
        if (transporterIconTexture != null)
        {
            cacheKey = $"{cacheKey}_{transporterIconTexture.GetInstanceID()}";
        }
        if (_transporterIconCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = BuildTransporterRouteTexture(routeSteps, size, transporterIconTexture, overlayColor);
        if (texture != null)
        {
            _transporterIconCache[cacheKey] = texture;
        }
        return texture;
    }

    private static string BuildRouteCacheKey(string[] routeSteps, int size)
    {
        string key = RouteParser.NormalizeRouteKey(routeSteps);
        if (string.IsNullOrEmpty(key))
        {
            key = "EMPTY";
        }
        return $"{key}_{size}";
    }

    private static Texture2D BuildTransporterRouteTexture(string[] routeSteps, int size, Texture2D baseTexture, Color overlayColor)
    {
        List<Vector2Int> positions = BuildRoutePositions(routeSteps);
        if (positions.Count == 0) return null;

        int minX = positions[0].x;
        int maxX = positions[0].x;
        int minY = positions[0].y;
        int maxY = positions[0].y;

        for (int i = 1; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        int maxDim = Mathf.Max(1, Mathf.Max(width, height));
        int padding = Mathf.Max(1, size / 10);
        int cellSize = Mathf.Max(1, (size - (padding * 2)) / maxDim);

        int usedWidth = width * cellSize;
        int usedHeight = height * cellSize;
        int offsetX = Mathf.Max(0, (size - usedWidth) / 2);
        int offsetY = Mathf.Max(0, (size - usedHeight) / 2);

        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color originalBlock = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0.9f);
        Color previewPath = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0.6f);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }

        if (baseTexture != null && baseTexture.width > 0 && baseTexture.height > 0)
        {
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    pixels[row + x] = baseTexture.GetPixelBilinear(u, v);
                }
            }
        }

        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            int localX = pos.x - minX;
            int localY = pos.y - minY;
            int pixelX = offsetX + (localX * cellSize);
            int pixelY = offsetY + (localY * cellSize);

            // First position is the original block (full opacity), rest are preview path (20% opacity)
            Color fill = (i == 0) ? originalBlock : previewPath;

            for (int y = 0; y < cellSize; y++)
            {
                int py = pixelY + y;
                if (py < 0 || py >= size) continue;
                int row = py * size;
                for (int x = 0; x < cellSize; x++)
                {
                    int px = pixelX + x;
                    if (px < 0 || px >= size) continue;
                    Color baseColor = pixels[row + px];
                    float a = fill.a;
                    pixels[row + px] = new Color(
                        baseColor.r + (fill.r - baseColor.r) * a,
                        baseColor.g + (fill.g - baseColor.g) * a,
                        baseColor.b + (fill.b - baseColor.b) * a,
                        Mathf.Clamp01(baseColor.a + a)
                    );
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static List<Vector2Int> BuildRoutePositions(string[] routeSteps)
    {
        List<Vector2Int> steps = RouteParser.ParseRouteSteps(routeSteps);
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int current = Vector2Int.zero;
        positions.Add(current);
        foreach (Vector2Int step in steps)
        {
            current += step;
            positions.Add(current);
        }
        return positions;
    }

    private static string[] ResolveRouteSteps(BlockInventoryEntry entry)
    {
        if (entry == null) return null;
        RouteParser.RouteData data = RouteParser.ParseRoute(entry.routeSteps, entry.flavorId);
        if (data.normalizedSteps == null || data.normalizedSteps.Length == 0) return null;
        return data.normalizedSteps;
    }

    private void OnDisable()
    {
        ClearTransporterIconCache();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        ClearTransporterIconCache();
    }

    private void ClearTransporterIconCache()
    {
        if (_transporterIconCache.Count == 0) return;

        foreach (Texture2D texture in _transporterIconCache.Values)
        {
            if (texture == null) continue;
            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        _transporterIconCache.Clear();
    }

    public void SetVisible(bool visible)
    {
        if (_canvas != null)
        {
            _canvas.enabled = visible;
        }
    }
}
