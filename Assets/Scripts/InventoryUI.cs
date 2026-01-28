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
    public EditorController editorController;

    [Header("UI Settings")]
    public float boxSize = 110f;
    public float spacing = 20f;
    public float itemPadding = 6f;
    public float topMargin = 24f;
    public float leftMargin = 24f;
    public float keyHintHeight = 20f;
    public float keyHintSpacing = 4f;

    [Header("UGUI Settings")]
    public Font font;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 1f;

    [Header("Text Sizes")]
    public int labelFontSize = 16;
    public int subLabelFontSize = 13;
    public int countFontSize = 20;
    public int teleporterFontSize = 56;
    public int cornerFontSize = 18;

    [Header("Colors")]
    public Color normalBackground = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedBackground = new Color(0f, 0f, 0f, 0.9f);
    public Color subLabelColor = new Color(1f, 1f, 1f, 0.75f);

    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _panel;
    private VerticalLayoutGroup _layout;
    private ContentSizeFitter _fitter;
    private Text _statusText;
    private Text _lockStatusText;
    private Text _winText;
    private float _nextReferenceRefreshTime;
    private float _nextLockStatusRefreshTime;
    private int _cachedTotalLocks;
    private int _cachedLockedCount;

    private readonly List<SlotUI> _slots = new List<SlotUI>();
    private readonly Dictionary<string, Texture2D> _transporterIconCache = new Dictionary<string, Texture2D>();

    private class SlotUI
    {
        public GameObject root;
        public Image background;
        public Image previewImage;
        public RawImage previewRaw;
        public Text label;
        public Text subLabel;
        public Text count;
        public Text teleporterLabel;
        public Text keyHint;
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

        TryResolveReferences(true);

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        EnsureCanvas();
        BuildStaticUI();
    }

    private void OnEnable()
    {
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        EnsureCanvas();
        BuildStaticUI();
    }

    private void Update()
    {
        if (inventory == null || (Application.isPlaying && editorController == null))
        {
            TryResolveReferences(false);
        }

        UpdateUI();
    }

    private void TryResolveReferences(bool force)
    {
        float now = Time.realtimeSinceStartup;
        if (!force && now < _nextReferenceRefreshTime) return;
        _nextReferenceRefreshTime = now + 1f;

        if (inventory == null)
        {
            inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        }

        if (editorController == null)
        {
            editorController = UnityEngine.Object.FindAnyObjectByType<EditorController>();
        }
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

    private void BuildStaticUI()
    {
        if (_panel != null) return;

        GameObject panelObj = new GameObject("InventoryPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObj.transform.SetParent(_root, false);
        _panel = panelObj.GetComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _panel.anchoredPosition = new Vector2(leftMargin, -topMargin);

        _layout = panelObj.GetComponent<VerticalLayoutGroup>();
        _layout.spacing = spacing;
        _layout.childAlignment = TextAnchor.UpperLeft;
        _layout.childControlWidth = false;
        _layout.childControlHeight = false;
        _layout.childForceExpandWidth = false;
        _layout.childForceExpandHeight = false;

        _fitter = panelObj.GetComponent<ContentSizeFitter>();
        _fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _statusText = CreateText("Status", _root, labelFontSize, TextAnchor.UpperLeft, Color.yellow);
        RectTransform statusRect = _statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(leftMargin, -topMargin);
        _statusText.gameObject.SetActive(false);

        _lockStatusText = CreateText("LockStatus", _root, cornerFontSize, TextAnchor.UpperRight, Color.white);
        RectTransform lockRect = _lockStatusText.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(1f, 1f);
        lockRect.anchorMax = new Vector2(1f, 1f);
        lockRect.pivot = new Vector2(1f, 1f);
        lockRect.anchoredPosition = new Vector2(-24f, -20f);

        _winText = CreateText("WinText", _root, cornerFontSize, TextAnchor.UpperRight, new Color(0.8f, 1f, 0.8f));
        RectTransform winRect = _winText.GetComponent<RectTransform>();
        winRect.anchorMin = new Vector2(1f, 1f);
        winRect.anchorMax = new Vector2(1f, 1f);
        winRect.pivot = new Vector2(1f, 1f);
        winRect.anchoredPosition = new Vector2(-24f, -46f);
        _winText.text = string.Empty;
    }

    private void UpdateUI()
    {
        if (_panel == null) return;

        GameMode mode = editorController != null ? editorController.currentMode : GameMode.Editor;
        bool showInventory = !Application.isPlaying || mode != GameMode.Play;
        _panel.gameObject.SetActive(showInventory);

        if (inventory == null)
        {
            ShowStatus("InventoryUI: No BlockInventory found");
            return;
        }

        if (Application.isPlaying && editorController == null)
        {
            ShowStatus("InventoryUI: No EditorController found");
            return;
        }

        HideStatus();

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
        bool showInfinite = mode == GameMode.LevelEditor;

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

        if (drawIndex == 0)
        {
            ShowStatus("InventoryUI: No visible inventory entries");
        }
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
        layout.preferredHeight = boxSize + keyHintSpacing + keyHintHeight;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(boxSize, layout.preferredHeight);

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 1f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(boxSize, boxSize);
        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = normalBackground;

        Text label = CreateText("Label", bgRect, labelFontSize, TextAnchor.UpperCenter, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -itemPadding);
        labelRect.sizeDelta = new Vector2(0f, 18f);

        Text subLabel = CreateText("SubLabel", bgRect, subLabelFontSize, TextAnchor.UpperCenter, subLabelColor);
        RectTransform subRect = subLabel.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -(itemPadding + 18f));
        subRect.sizeDelta = new Vector2(0f, 16f);

        GameObject previewObj = new GameObject("Preview", typeof(RectTransform), typeof(Image));
        previewObj.transform.SetParent(bgRect, false);
        RectTransform previewRect = previewObj.GetComponent<RectTransform>();
        float previewSize = boxSize * 0.4f;
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(0f, -4f);
        previewRect.sizeDelta = new Vector2(previewSize, previewSize);
        Image previewImage = previewObj.GetComponent<Image>();

        GameObject previewRawObj = new GameObject("PreviewRaw", typeof(RectTransform), typeof(RawImage));
        previewRawObj.transform.SetParent(previewRect, false);
        RectTransform rawRect = previewRawObj.GetComponent<RectTransform>();
        rawRect.anchorMin = new Vector2(0f, 0f);
        rawRect.anchorMax = new Vector2(1f, 1f);
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;
        RawImage previewRaw = previewRawObj.GetComponent<RawImage>();
        previewRaw.gameObject.SetActive(false);

        Text teleporterLabel = CreateText("TeleporterLabel", previewRect, teleporterFontSize, TextAnchor.MiddleCenter, Color.white);

        Text count = CreateText("Count", bgRect, countFontSize, TextAnchor.LowerCenter, Color.white);
        RectTransform countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(0.5f, 0f);
        countRect.anchoredPosition = new Vector2(0f, itemPadding);
        countRect.sizeDelta = new Vector2(0f, 18f);

        Text keyHint = CreateText("KeyHint", rootRect, labelFontSize, TextAnchor.UpperCenter, Color.white);
        RectTransform keyRect = keyHint.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0f);
        keyRect.anchorMax = new Vector2(1f, 0f);
        keyRect.pivot = new Vector2(0.5f, 0f);
        keyRect.anchoredPosition = new Vector2(0f, 0f);
        keyRect.sizeDelta = new Vector2(0f, keyHintHeight);

        return new SlotUI
        {
            root = root,
            background = bgImage,
            previewImage = previewImage,
            previewRaw = previewRaw,
            label = label,
            subLabel = subLabel,
            count = count,
            teleporterLabel = teleporterLabel,
            keyHint = keyHint
        };
    }

    private void UpdateSlot(SlotUI slot, bool showInfinite)
    {
        BlockInventoryEntry entry = slot.entry;
        if (entry == null) return;

        bool isSelected = editorController != null && editorController.CurrentInventoryIndex == slot.index;
        slot.background.color = isSelected ? selectedBackground : normalBackground;

        string blockName = entry.GetDisplayName();
        slot.label.text = blockName;

        slot.subLabel.text = entry.isPairInventory ? "(pairs)" : string.Empty;

        int available = inventory.GetDisplayAvailableCount(entry);
        int total = inventory.GetDisplayTotalCount(entry);
        slot.count.text = showInfinite ? "INF" : $"{available}/{total}";

        string keyHint = slot.index < 9 ? $"[{slot.index + 1}]" : string.Empty;
        slot.keyHint.text = keyHint;

        Color blockColor = GetColorForBlockType(entry.blockType);
        if (!showInfinite && available == 0)
        {
            blockColor.a = 0.3f;
        }

        if (entry.blockType == BlockType.Transporter)
        {
            if (TrySetTransporterPreview(entry, slot.previewRaw, blockColor, slot.previewImage.rectTransform.rect.size))
            {
                slot.previewImage.enabled = false;
            }
            else
            {
                slot.previewImage.enabled = true;
                slot.previewImage.color = blockColor;
                slot.previewRaw.gameObject.SetActive(false);
            }
        }
        else
        {
            slot.previewImage.enabled = true;
            slot.previewImage.color = blockColor;
            slot.previewRaw.gameObject.SetActive(false);
        }

        if (entry.blockType == BlockType.Teleporter)
        {
            slot.teleporterLabel.text = ExtractTeleporterLabel(entry);
        }
        else
        {
            slot.teleporterLabel.text = string.Empty;
        }
    }

    private bool TrySetTransporterPreview(BlockInventoryEntry entry, RawImage rawImage, Color blockColor, Vector2 size)
    {
        string[] steps = ResolveRouteSteps(entry);
        if (steps == null || steps.Length == 0) return false;

        int textureSize = Mathf.RoundToInt(Mathf.Min(size.x, size.y));
        if (textureSize <= 0) return false;

        Texture2D icon = GetTransporterRouteTexture(steps, textureSize);
        if (icon == null) return false;

        rawImage.texture = icon;
        rawImage.color = blockColor;
        rawImage.gameObject.SetActive(true);
        return true;
    }

    private void UpdateLockStatus()
    {
        float now = Time.realtimeSinceStartup;
        if (now >= _nextLockStatusRefreshTime)
        {
            _nextLockStatusRefreshTime = now + 0.2f;
            LockBlock[] locks = UnityEngine.Object.FindObjectsByType<LockBlock>(FindObjectsSortMode.None);
            _cachedTotalLocks = locks.Length;
            _cachedLockedCount = 0;
            foreach (LockBlock lockBlock in locks)
            {
                if (lockBlock != null && lockBlock.HasKeyLocked())
                {
                    _cachedLockedCount++;
                }
            }
        }

        _lockStatusText.text = _cachedTotalLocks > 0 ? $"{_cachedLockedCount} of {_cachedTotalLocks}" : string.Empty;

        if (editorController != null && editorController.currentMode == GameMode.Play && _cachedTotalLocks > 0 && _cachedLockedCount >= _cachedTotalLocks)
        {
            _winText.text = "You win";
        }
        else
        {
            _winText.text = string.Empty;
        }
    }

    private void ShowStatus(string text)
    {
        if (_statusText == null) return;
        _statusText.text = text;
        _statusText.gameObject.SetActive(true);
    }

    private void HideStatus()
    {
        if (_statusText == null) return;
        _statusText.gameObject.SetActive(false);
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

    private bool ShouldHideFromInventory(BlockInventoryEntry entry, GameMode mode)
    {
        if (entry == null) return true;
        if (mode == GameMode.LevelEditor) return false;
        return entry.blockType == BlockType.Key || entry.blockType == BlockType.Lock;
    }

    private Color GetColorForBlockType(BlockType blockType)
    {
        return BlockColors.GetColorForBlockType(blockType);
    }

    private static string ExtractTeleporterLabel(BlockInventoryEntry entry)
    {
        string labelSource = entry.GetResolvedFlavorId();
        if (string.IsNullOrWhiteSpace(labelSource))
        {
            labelSource = entry.displayName;
        }
        if (string.IsNullOrWhiteSpace(labelSource))
        {
            labelSource = entry.GetDisplayName();
        }
        if (string.IsNullOrWhiteSpace(labelSource)) return string.Empty;
        string trimmed = labelSource.Trim();
        string[] parts = trimmed.Split(new[] { ' ', '-', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
        string token = parts.Length > 0 ? parts[parts.Length - 1] : trimmed;
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        return token.Length > 1 ? token.Substring(0, 1) : token;
    }

    private Texture2D GetTransporterRouteTexture(string[] routeSteps, int size)
    {
        string cacheKey = BuildRouteCacheKey(routeSteps, size);
        if (_transporterIconCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = BuildTransporterRouteTexture(routeSteps, size);
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

    private static Texture2D BuildTransporterRouteTexture(string[] routeSteps, int size)
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
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            int localX = pos.x - minX;
            int localY = pos.y - minY;
            int pixelX = offsetX + (localX * cellSize);
            int pixelY = offsetY + (localY * cellSize);

            for (int y = 0; y < cellSize; y++)
            {
                int py = pixelY + y;
                if (py < 0 || py >= size) continue;
                int row = py * size;
                for (int x = 0; x < cellSize; x++)
                {
                    int px = pixelX + x;
                    if (px < 0 || px >= size) continue;
                    pixels[row + px] = fill;
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
