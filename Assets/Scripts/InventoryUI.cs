using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    [Tooltip("Optional sprite override for the Crumbler block inventory preview")]
    public Sprite crumblerBlockSprite;

    [Tooltip("Resources path fallback for the Crumbler icon (without extension)")]
    public string crumblerBlockSpriteResourcePath = "Sprites/inventoryUi/rocksIcon";

    [Tooltip("Editor-only asset path fallback for the Crumbler icon")]
    public string crumblerBlockSpriteAssetPath = "Assets/Sprites/inventoryUi/rocksIcon.png";

    [Tooltip("Optional sprite override for the Teleporter block inventory preview")]
    public Sprite teleporterBlockSprite;

    [Tooltip("Resources path fallback for the Teleporter icon (without extension)")]
    public string teleporterBlockSpriteResourcePath = "Sprites/inventoryUi/teleporterIcon";

    [Tooltip("Editor-only asset path fallback for the Teleporter icon")]
    public string teleporterBlockSpriteAssetPath = "Assets/Sprites/inventoryUi/teleporterIcon.png";

    [Tooltip("Optional sprite override for the Key block inventory preview")]
    public Sprite keyBlockSprite;

    [Tooltip("Resources path fallback for the Key icon (without extension)")]
    public string keyBlockSpriteResourcePath = "Sprites/inventoryUi/greenApple";

    [Tooltip("Editor-only asset path fallback for the Key icon")]
    public string keyBlockSpriteAssetPath = "Assets/Sprites/inventoryUi/greenApple.png";

    [Tooltip("Optional sprite override for the Lock block inventory preview")]
    public Sprite lockBlockSprite;

    [Tooltip("Resources path fallback for the Lock icon (without extension)")]
    public string lockBlockSpriteResourcePath = "Sprites/inventoryUi/marbleHand";

    [Tooltip("Editor-only asset path fallback for the Lock icon")]
    public string lockBlockSpriteAssetPath = "Assets/Sprites/inventoryUi/marbleHand.png";

    [Tooltip("Optional texture override for the Transporter inventory preview")]
    public Texture2D transporterIconTexture;

    [Tooltip("Resources path fallback for the Transporter icon (without extension)")]
    public string transporterIconTextureResourcePath = "Sprites/inventoryUi/cloudIcon";

    [Tooltip("Editor-only asset path fallback for the Transporter icon")]
    public string transporterIconTextureAssetPath = "Assets/Sprites/inventoryUi/cloudIcon.png";

    [Header("Icon Shadow")]
    [Range(0f, 1f)]
    public float iconShadowOpacity = 0.07f;
    public Color iconShadowColor = new Color(0.192f, 0.290f, 0.306f, 1f);
    [Range(0f, 12f)]
    public float iconShadowBlur = 2f;

    [Header("Selection Border")]
    [Range(0f, 8f)]
    public float selectionBorderThickness = 3f;
    public Color selectionBorderColor = new Color(0.192f, 0.290f, 0.306f, 1f);

    [Header("Font")]
    [Tooltip("Editor-only asset path for the UI font")]
    public string uiFontAssetPath = "Assets/Fonts/Koulen-Regular.ttf";

    [Tooltip("Resources path fallback for the UI font (without extension)")]
    public string uiFontResourcePath = "Fonts/Koulen-Regular";

    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _panel;
    private VerticalLayoutGroup _layout;
    private ContentSizeFitter _fitter;
    private Text _lockStatusText;
    private Text _winText;

    private readonly List<SlotUI> _slots = new List<SlotUI>();
    private readonly Dictionary<string, Texture2D> _transporterIconCache = new Dictionary<string, Texture2D>();
    private static readonly Color DepletedPreviewColor = new Color(1f, 1f, 1f, 0.3f);

    // Dirty-flag tracking for event-driven rebuild
    private bool _isDirty = true;
    private bool _lockDirty = true;
    private GameMode _lastMode;
    private BlockInventoryEntry _lastSelectedEntry;

    private class SlotUI
    {
        public GameObject root;
        public InventorySlotClickHandler clickHandler;
        public Image previewImage;
        public RawImage previewRaw;
        public Shadow previewShadow;
        public Shadow previewRawShadow;
        public Outline previewOutline;
        public Outline previewRawOutline;
        public Image countBadge;
        public Text count;
        public Text teleporterLabel;
        public Text blockLabel;
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
        RefreshStaticUI();
    }

    private void OnEnable()
    {
        RefreshStaticUI();
        SubscribeToEvents();
        _isDirty = true;
        _lockDirty = true;
    }

    private void RefreshStaticUI()
    {
        LoadUIFont();
        LoadWalkBlockSprite();
        LoadCrumblerBlockSprite();
        LoadTeleporterBlockSprite();
        LoadKeyBlockSprite();
        LoadLockBlockSprite();
        LoadTransporterIconTexture();
        EnsureCanvas();
        EnsureEventSystem();
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
                SubscribeToInventory();
            }

            if (builderController == null)
            {
                builderController = ServiceRegistry.Get<BuilderController>();
            }
        }

        // In edit mode, always update (no event sources available)
        if (!Application.isPlaying)
        {
            UpdateUI();
            return;
        }

        // Detect mode or selection changes
        GameMode currentMode = builderController != null ? builderController.currentMode : GameMode.Builder;
        BlockInventoryEntry currentEntry = builderController != null ? builderController.currentInventoryEntry : null;
        if (currentMode != _lastMode || currentEntry != _lastSelectedEntry)
        {
            _lastMode = currentMode;
            _lastSelectedEntry = currentEntry;
            _isDirty = true;
        }

        if (_isDirty)
        {
            _isDirty = false;
            _lockDirty = false;
            UpdateUI();
        }
        else if (_lockDirty)
        {
            _lockDirty = false;
            UpdateLockStatus();
        }
    }

    private void SubscribeToEvents()
    {
        LockBlock.OnLockStateChanged += HandleLockStateChanged;
        SubscribeToInventory();
    }

    private void UnsubscribeFromEvents()
    {
        LockBlock.OnLockStateChanged -= HandleLockStateChanged;
        UnsubscribeFromInventory();
    }

    private void SubscribeToInventory()
    {
        if (inventory == null) return;
        inventory.OnInventoryChanged -= HandleInventoryChanged;
        inventory.OnInventoryChanged += HandleInventoryChanged;
    }

    private void UnsubscribeFromInventory()
    {
        if (inventory == null) return;
        inventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        _isDirty = true;
    }

    private void HandleLockStateChanged()
    {
        _lockDirty = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _isDirty = true;
    }
#endif

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

        if (_panel != null && _lockStatusText != null && _winText != null)
        {
            return;
        }

        if (_panel == null)
        {
            Transform existingPanel = _root != null ? _root.Find("InventoryPanel") : null;
            if (existingPanel != null)
            {
                _panel = existingPanel.GetComponent<RectTransform>();
            }
        }

        if (_panel == null)
        {
            GameObject panelObj = new GameObject("InventoryPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObj.transform.SetParent(_root, false);
            _panel = panelObj.GetComponent<RectTransform>();
        }

        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _panel.anchoredPosition = new Vector2(leftMargin, -topMargin);

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
        if (lockRect != null)
        {
            lockRect.anchorMin = new Vector2(1f, 1f);
            lockRect.anchorMax = new Vector2(1f, 1f);
            lockRect.pivot = new Vector2(1f, 1f);
            lockRect.anchoredPosition = new Vector2(-24f, -20f);
        }

        _winText = FindOrCreateText("WinText", _root, cornerFontSize, TextAnchor.UpperRight, new Color(0.8f, 1f, 0.8f));
        RectTransform winRect = _winText != null ? _winText.GetComponent<RectTransform>() : null;
        if (winRect != null)
        {
            winRect.anchorMin = new Vector2(1f, 1f);
            winRect.anchorMax = new Vector2(1f, 1f);
            winRect.pivot = new Vector2(1f, 1f);
            winRect.anchoredPosition = new Vector2(-24f, -46f);
        }
        if (_winText != null) _winText.text = string.Empty;
    }

    private void EnsureEventSystem()
    {
        if (!Application.isPlaying) return;
        if (EventSystem.current != null) return;

        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void CleanupLegacyOrDuplicateUI()
    {
        if (_root == null) return;

        // InventoryUI previously generated many UI children in edit mode on script reload.
        // Clean up obvious duplicates/legacy names under this UI root.
        string[] keepOneNames = { "InventoryPanel", "LockStatus", "WinText" };
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
            if (child != null && (child.name == "Status" || child.name == "InventoryFrame"))
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
            InventorySlotClickHandler clickHandler = EnsureClickHandler(root);
            Image previewImage = null;
            RawImage previewRaw = null;
            Text count = null;
            Text teleporterLabel = null;
            Text blockLabel = null;

            Transform previewTransform = child.Find("Background/Preview");
            if (previewTransform != null)
            {
                previewImage = previewTransform.GetComponent<Image>();
                Transform rawTransform = previewTransform.Find("PreviewRaw");
                if (rawTransform != null)
                {
                    previewRaw = rawTransform.GetComponent<RawImage>();
                }

                Transform labelTransform = previewTransform.Find("TeleporterLabel");
                if (labelTransform != null)
                {
                    teleporterLabel = labelTransform.GetComponent<Text>();
                }

                Transform blockLabelTransform = previewTransform.Find("BlockLabel");
                if (blockLabelTransform != null)
                {
                    blockLabel = blockLabelTransform.GetComponent<Text>();
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
                clickHandler = clickHandler,
                previewImage = previewImage,
                previewRaw = previewRaw,
                previewShadow = EnsureShadow(previewImage),
                previewRawShadow = EnsureShadow(previewRaw),
                previewOutline = EnsureOutline(previewImage),
                previewRawOutline = EnsureOutline(previewRaw),
                count = count,
                teleporterLabel = teleporterLabel,
                blockLabel = blockLabel
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

        _panel.gameObject.SetActive(showInventory);

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
            if (_lockStatusText != null) _lockStatusText.text = string.Empty;
            if (_winText != null) _winText.text = string.Empty;
        }

        if (!showInventory) return;

        IReadOnlyList<BlockInventoryEntry> entries = inventory.GetEntriesForMode(mode);
        List<BlockInventoryEntry> orderedEntries = OrderEntriesForInventory(entries);
        bool showInfinite = mode == GameMode.Designer;

        int drawIndex = 0;
        for (int i = 0; i < orderedEntries.Count; i++)
        {
            if (ShouldHideFromInventory(orderedEntries[i])) continue;

            SlotUI slot = EnsureSlot(drawIndex);
            slot.entry = orderedEntries[i];
            slot.index = i;
            UpdateSlot(slot, showInfinite);
            drawIndex++;
        }

        TrimSlots(drawIndex);
        FitPanelToViewport(drawIndex);
    }

    private List<BlockInventoryEntry> OrderEntriesForInventory(IReadOnlyList<BlockInventoryEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return new List<BlockInventoryEntry>();
        }

        // Preserve inspector order, but group identical block types together (e.g. Teleporter A/B).
        Dictionary<BlockType, List<BlockInventoryEntry>> grouped = new Dictionary<BlockType, List<BlockInventoryEntry>>();
        List<BlockType> order = new List<BlockType>();

        for (int i = 0; i < entries.Count; i++)
        {
            BlockInventoryEntry entry = entries[i];
            if (entry == null) continue;
            if (ShouldHideFromInventory(entry)) continue;

            if (!grouped.TryGetValue(entry.blockType, out List<BlockInventoryEntry> list))
            {
                list = new List<BlockInventoryEntry>();
                grouped[entry.blockType] = list;
                order.Add(entry.blockType);
            }
            list.Add(entry);
        }

        List<BlockInventoryEntry> result = new List<BlockInventoryEntry>(entries.Count);
        for (int i = 0; i < order.Count; i++)
        {
            BlockType type = order[i];
            if (grouped.TryGetValue(type, out List<BlockInventoryEntry> list))
            {
                result.AddRange(list);
            }
        }
        return result;
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
        layout.preferredHeight = boxSize;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(boxSize, layout.preferredHeight);

        InventorySlotClickHandler clickHandler = EnsureClickHandler(root);

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
        Shadow previewShadow = EnsureShadow(previewImage);
        Outline previewOutline = EnsureOutline(previewImage);

        GameObject badgeObj = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
        badgeObj.transform.SetParent(previewRect, false);
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(2f, -2f);
        Image badgeImage = badgeObj.GetComponent<Image>();
        badgeImage.color = Color.black;

        GameObject previewRawObj = new GameObject("PreviewRaw", typeof(RectTransform), typeof(RawImage));
        previewRawObj.transform.SetParent(previewRect, false);
        RectTransform rawRect = previewRawObj.GetComponent<RectTransform>();
        rawRect.anchorMin = new Vector2(0f, 0f);
        rawRect.anchorMax = new Vector2(1f, 1f);
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;
        RawImage previewRaw = previewRawObj.GetComponent<RawImage>();
        previewRaw.gameObject.SetActive(false);
        Shadow previewRawShadow = EnsureShadow(previewRaw);
        Outline previewRawOutline = EnsureOutline(previewRaw);

        Text teleporterLabel = CreateText("TeleporterLabel", previewRect, textSize, TextAnchor.MiddleCenter, Color.black);
        RectTransform labelRect = teleporterLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        teleporterLabel.raycastTarget = false;

        Text blockLabel = CreateText("BlockLabel", previewRect, textSize, TextAnchor.LowerCenter, Color.black);
        RectTransform blockLabelRect = blockLabel.GetComponent<RectTransform>();
        blockLabelRect.anchorMin = new Vector2(0f, 0f);
        blockLabelRect.anchorMax = new Vector2(1f, 0f);
        blockLabelRect.pivot = new Vector2(0.5f, 0f);
        blockLabelRect.anchoredPosition = new Vector2(0f, 2f);
        blockLabelRect.sizeDelta = new Vector2(0f, Mathf.Max(12f, textSize * 0.9f));
        blockLabel.raycastTarget = false;

        Text count = CreateText("Count", badgeRect, textSize * 3, TextAnchor.MiddleCenter, Color.white);
        RectTransform countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 0.5f);
        countRect.anchorMax = new Vector2(0.5f, 0.5f);
        countRect.pivot = new Vector2(0.5f, 0.5f);
        countRect.anchoredPosition = Vector2.zero;

        return new SlotUI
        {
            root = root,
            clickHandler = clickHandler,
            previewImage = previewImage,
            previewRaw = previewRaw,
            previewShadow = previewShadow,
            previewRawShadow = previewRawShadow,
            previewOutline = previewOutline,
            previewRawOutline = previewRawOutline,
            countBadge = badgeImage,
            count = count,
            teleporterLabel = teleporterLabel,
            blockLabel = blockLabel
        };
    }

    private void UpdateSlot(SlotUI slot, bool showInfinite)
    {
        BlockInventoryEntry entry = slot.entry;
        if (slot.clickHandler != null)
        {
            slot.clickHandler.Bind(builderController, entry);
        }

        if (entry == null) return;

        float previewSize = Mathf.Max(0f, boxSize - (iconMargin * 2f));
        RectTransform rootRect = slot.root.GetComponent<RectTransform>();
        LayoutElement layout = slot.root.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.preferredWidth = boxSize;
            layout.preferredHeight = boxSize;
        }
        if (rootRect != null)
        {
            rootRect.sizeDelta = new Vector2(boxSize, boxSize);
        }

        RectTransform previewRect = slot.previewImage != null ? slot.previewImage.rectTransform : null;
        if (previewRect != null)
        {
            RectTransform bgRect = previewRect.parent as RectTransform;
            if (bgRect != null)
            {
                bgRect.sizeDelta = new Vector2(boxSize, boxSize);
            }
            previewRect.sizeDelta = new Vector2(previewSize, previewSize);
        }

        int available = inventory.GetDisplayAvailableCount(entry);
        UpdateCountBadge(slot, available, showInfinite, previewSize);

        Color blockColor = GetColorForBlockType(entry.blockType);
        if (!showInfinite && available == 0)
        {
            blockColor.a = 0.3f;
        }

        if (TryGetSpritePreview(entry.blockType, out Sprite sprite))
        {
            SetSpritePreview(slot, sprite, blockColor, showInfinite, available);
        }
        else if (entry.blockType == BlockType.Transporter)
        {
            Vector2 previewSizeForRaw = slot.previewImage != null ? slot.previewImage.rectTransform.rect.size : Vector2.zero;
            if (TrySetTransporterPreview(entry, slot.previewRaw, blockColor, previewSizeForRaw))
            {
                if (slot.previewImage != null)
                {
                    slot.previewImage.enabled = false;
                }
            }
            else
            {
                SetSpritePreview(slot, null, blockColor, showInfinite, available);
            }
        }
        else
        {
            SetSpritePreview(slot, null, blockColor, showInfinite, available);
        }

        if (slot.previewImage != null)
        {
            slot.previewImage.preserveAspect = true;
        }

        UpdateTeleporterLabel(slot);
        UpdateBlockLabel(slot);
        ApplyIconShadow(slot.previewShadow);
        ApplyIconShadow(slot.previewRawShadow);
        bool isSelected = builderController != null && builderController.currentInventoryEntry == entry;
        ApplySelectionOutline(slot.previewOutline, isSelected);
        ApplySelectionOutline(slot.previewRawOutline, isSelected);
    }

    private bool TryGetSpritePreview(BlockType blockType, out Sprite sprite)
    {
        sprite = null;
        switch (blockType)
        {
            case BlockType.Walk:
                if (walkBlockSprite == null) LoadWalkBlockSprite();
                sprite = walkBlockSprite;
                return true;
            case BlockType.Crumbler:
                if (crumblerBlockSprite == null) LoadCrumblerBlockSprite();
                sprite = crumblerBlockSprite;
                return true;
            case BlockType.Teleporter:
                if (teleporterBlockSprite == null) LoadTeleporterBlockSprite();
                sprite = teleporterBlockSprite;
                return true;
            case BlockType.Key:
                if (keyBlockSprite == null) LoadKeyBlockSprite();
                sprite = keyBlockSprite;
                return true;
            case BlockType.Lock:
                if (lockBlockSprite == null) LoadLockBlockSprite();
                sprite = lockBlockSprite;
                return true;
            default:
                return false;
        }
    }

    private void SetSpritePreview(SlotUI slot, Sprite sprite, Color fallbackColor, bool showInfinite, int available)
    {
        if (slot.previewImage != null)
        {
            slot.previewImage.enabled = true;
            slot.previewImage.sprite = sprite;
            slot.previewImage.color = sprite != null
                ? (showInfinite || available > 0 ? Color.white : DepletedPreviewColor)
                : fallbackColor;
        }

        SetRawPreviewVisible(slot.previewRaw, false);
    }

    private static void SetRawPreviewVisible(RawImage rawImage, bool visible)
    {
        if (rawImage == null) return;
        rawImage.gameObject.SetActive(visible);
    }

    private Shadow EnsureShadow(Graphic graphic)
    {
        if (graphic == null) return null;
        Shadow shadow = graphic.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = graphic.gameObject.AddComponent<Shadow>();
        }
        shadow.useGraphicAlpha = false;
        return shadow;
    }

    private Outline EnsureOutline(Graphic graphic)
    {
        if (graphic == null) return null;
        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null)
        {
            outline = graphic.gameObject.AddComponent<Outline>();
        }
        outline.useGraphicAlpha = false;
        outline.enabled = false;
        return outline;
    }

    private void ApplyIconShadow(Shadow shadow)
    {
        if (shadow == null) return;

        if (iconShadowOpacity <= 0f)
        {
            shadow.enabled = false;
            return;
        }

        Color shadowColor = iconShadowColor;
        shadowColor.a = iconShadowOpacity;
        shadow.effectColor = shadowColor;
        float blur = Mathf.Max(0f, iconShadowBlur);
        shadow.effectDistance = new Vector2(blur, -blur);
        shadow.enabled = true;
    }

    private void ApplySelectionOutline(Outline outline, bool enabled)
    {
        if (outline == null) return;

        if (!enabled || selectionBorderThickness <= 0f)
        {
            outline.enabled = false;
            return;
        }

        outline.effectColor = selectionBorderColor;
        outline.effectDistance = new Vector2(selectionBorderThickness, selectionBorderThickness);
        outline.enabled = true;
    }

    private void UpdateCountBadge(SlotUI slot, int available, bool showInfinite, float previewSize)
    {
        if (slot.count == null) return;

        slot.count.text = showInfinite ? "INF" : $"{available}";
        slot.count.color = Color.white;

        if (slot.countBadge == null) return;
        slot.countBadge.transform.SetAsLastSibling();

        float badgeHeight = Mathf.Max(0f, previewSize * 0.25f);
        float padding = Mathf.Clamp(badgeHeight * 0.1f, 1f, 3f);
        int fontSize = Mathf.Max(1, Mathf.RoundToInt(badgeHeight * 0.7f));
        slot.count.fontSize = fontSize;

        RectTransform badgeRect = slot.countBadge.rectTransform;
        RectTransform countRect = slot.count.rectTransform;
        float width = Mathf.Ceil(slot.count.preferredWidth) + (padding * 2f);
        if (width < badgeHeight) width = badgeHeight;
        badgeRect.sizeDelta = new Vector2(width, badgeHeight);
        countRect.sizeDelta = new Vector2(width - (padding * 2f), badgeHeight - (padding * 2f));
    }

    private InventorySlotClickHandler EnsureClickHandler(GameObject root)
    {
        if (root == null) return null;

        Image clickTarget = root.GetComponent<Image>();
        if (clickTarget == null)
        {
            clickTarget = root.AddComponent<Image>();
        }
        clickTarget.color = new Color(1f, 1f, 1f, 0f);
        clickTarget.raycastTarget = true;

        InventorySlotClickHandler handler = root.GetComponent<InventorySlotClickHandler>();
        if (handler == null)
        {
            handler = root.AddComponent<InventorySlotClickHandler>();
        }
        return handler;
    }

    private void FitPanelToViewport(int visibleSlots)
    {
        if (_panel == null || _root == null) return;
        if (visibleSlots <= 0)
        {
            _panel.localScale = Vector3.one;
            return;
        }

        float contentHeight = (visibleSlots * boxSize) + Mathf.Max(0f, visibleSlots - 1) * blockSpacing;
        float availableHeight = _root.rect.height - (topMargin * 2f);
        if (availableHeight <= 0f || contentHeight <= 0f)
        {
            _panel.localScale = Vector3.one;
            return;
        }

        float scale = availableHeight / contentHeight;
        scale = Mathf.Max(0.01f, scale);
        _panel.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdateTeleporterLabel(SlotUI slot)
    {
        if (slot.teleporterLabel == null) return;

        BlockInventoryEntry entry = slot.entry;
        bool isTeleporter = entry != null && entry.blockType == BlockType.Teleporter;
        string label = isTeleporter ? entry.flavorId : null;

        if (string.IsNullOrEmpty(label))
        {
            slot.teleporterLabel.text = string.Empty;
            slot.teleporterLabel.enabled = false;
            return;
        }

        RectTransform previewRect = slot.previewImage != null ? slot.previewImage.rectTransform : null;
        RectTransform labelRect = slot.teleporterLabel.rectTransform;
        if (previewRect != null && labelRect != null)
        {
            float raise = previewRect.rect.height * 0.06f;
            labelRect.anchoredPosition = new Vector2(0f, raise);
        }

        slot.teleporterLabel.text = label;
        slot.teleporterLabel.font = font;
        slot.teleporterLabel.fontSize = Mathf.Max(1, Mathf.RoundToInt(textSize * 3f));
        slot.teleporterLabel.color = new Color(0.275f, 0.325f, 0.584f, 1f);
        slot.teleporterLabel.alignment = TextAnchor.MiddleCenter;
        slot.teleporterLabel.enabled = true;
    }

    private void UpdateBlockLabel(SlotUI slot)
    {
        if (slot.blockLabel == null) return;
        BlockInventoryEntry entry = slot.entry;
        if (entry == null)
        {
            slot.blockLabel.text = string.Empty;
            slot.blockLabel.enabled = false;
            return;
        }

        if (entry.blockType == BlockType.Key || entry.blockType == BlockType.Lock)
        {
            slot.blockLabel.text = string.Empty;
            slot.blockLabel.enabled = false;
            return;
        }

        string label = BlockColors.GetBlockTypeName(entry.blockType);
        if (string.IsNullOrEmpty(label))
        {
            slot.blockLabel.text = string.Empty;
            slot.blockLabel.enabled = false;
            return;
        }

        slot.blockLabel.text = label.ToUpperInvariant();
        slot.blockLabel.font = font;
        slot.blockLabel.fontSize = Mathf.Max(1, Mathf.RoundToInt(textSize * 0.55f));
        slot.blockLabel.color = new Color(0f, 0f, 0f, 0.75f);
        slot.blockLabel.alignment = TextAnchor.LowerCenter;
        slot.blockLabel.enabled = true;
    }

    private bool TrySetTransporterPreview(BlockInventoryEntry entry, RawImage rawImage, Color blockColor, Vector2 size)
    {
        if (rawImage == null) return false;

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

    /// <summary>
    /// Queries lock/key state and updates the corner status text.
    /// Only called when LockBlock.OnLockStateChanged fires (event-driven).
    /// </summary>
    private void UpdateLockStatus()
    {
        if (_lockStatusText == null || _winText == null) return;

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
        walkBlockSprite = LoadSpriteFromPaths(walkBlockSprite, walkBlockSpriteAssetPath, walkBlockSpriteResourcePath);
    }

    private void LoadCrumblerBlockSprite()
    {
        crumblerBlockSprite = LoadSpriteFromPaths(crumblerBlockSprite, crumblerBlockSpriteAssetPath, crumblerBlockSpriteResourcePath);
    }

    private void LoadTeleporterBlockSprite()
    {
        teleporterBlockSprite = LoadSpriteFromPaths(teleporterBlockSprite, teleporterBlockSpriteAssetPath, teleporterBlockSpriteResourcePath);
    }

    private void LoadKeyBlockSprite()
    {
        keyBlockSprite = LoadSpriteFromPaths(keyBlockSprite, keyBlockSpriteAssetPath, keyBlockSpriteResourcePath);
    }

    private void LoadLockBlockSprite()
    {
        lockBlockSprite = LoadSpriteFromPaths(lockBlockSprite, lockBlockSpriteAssetPath, lockBlockSpriteResourcePath);
    }

    /// <summary>
    /// Loads a sprite using editor asset path (preferred) or Resources path (fallback).
    /// Re-creates the sprite if the source rect doesn't cover the full texture (atlas slicing fix).
    /// </summary>
    private Sprite LoadSpriteFromPaths(Sprite existing, string assetPath, string resourcePath)
    {
        Sprite sprite = null;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(assetPath))
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    sprite = CreateSprite(texture);
                }
            }
        }
#endif

        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(resourcePath);
        }
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = CreateSprite(texture);
            }
        }

        // Re-create if sprite rect doesn't cover the full texture (e.g., atlas-sliced import).
        if (sprite != null && sprite.texture != null &&
            (Mathf.Abs(sprite.rect.width - sprite.texture.width) > 0.01f ||
             Mathf.Abs(sprite.rect.height - sprite.texture.height) > 0.01f))
        {
            sprite = CreateSprite(sprite.texture);
        }

        return sprite;
    }

    private void LoadTransporterIconTexture()
    {
        if (transporterIconTexture != null)
        {
#if UNITY_EDITOR
            EnsureReadableTextureAtPath(transporterIconTextureAssetPath);
#endif
            return;
        }

#if UNITY_EDITOR
        EnsureReadableTextureAtPath(transporterIconTextureAssetPath);
#endif

        Texture2D texture = Resources.Load<Texture2D>(transporterIconTextureResourcePath);

#if UNITY_EDITOR
        if (texture == null && !string.IsNullOrEmpty(transporterIconTextureAssetPath))
        {
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(transporterIconTextureAssetPath);
        }
#endif

        transporterIconTexture = texture;
    }

#if UNITY_EDITOR
    private static void EnsureReadableTextureAtPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return;
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
        if (importer == null) return;
        if (importer.isReadable) return;
        importer.isReadable = true;
        importer.SaveAndReimport();
    }
#endif

    private Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool ShouldHideFromInventory(BlockInventoryEntry entry)
    {
        return entry == null;
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
        const float previewScale = 0.35f;
        float baseCellSize = (size - (padding * 2)) / (float)maxDim;
        int cellSize = Mathf.Max(1, Mathf.RoundToInt(baseCellSize * previewScale));

        int usedWidth = width * cellSize;
        int usedHeight = height * cellSize;
        int offsetX = Mathf.Max(0, (size - usedWidth) / 2);
        int offsetY = Mathf.Max(0, (size - usedHeight) / 2);

        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color originalBlock = new Color(0f, 0f, 0f, 1f);
        Color previewPath = new Color(0.376f, 0.451f, 0.816f, 1f);
        int raisePixels = Mathf.RoundToInt(size * 0.15f);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }

        if (baseTexture != null && baseTexture.width > 0 && baseTexture.height > 0 && baseTexture.isReadable)
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
            int pixelY = offsetY + (localY * cellSize) + raisePixels;

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
        UnsubscribeFromEvents();
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

    /// <summary>
    /// Shows or hides the entire inventory canvas.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_canvas != null)
        {
            _canvas.enabled = visible;
        }
    }
}
