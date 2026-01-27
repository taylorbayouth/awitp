using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Displays block inventory counts and current selection using OnGUI
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

    [Header("Scaling")]
    [Tooltip("UI scale multiplier (1.0 = normal, 0.5 = half size)")]
    public float uiScale = 1.0f;

    [Tooltip("Auto-scale based on screen resolution (recommended)")]
    public bool autoScale = true;

    private float _scaleFactor = 1.0f;
    private float _lastScaleFactor = -1f;

    private GUIStyle normalStyle;
    private GUIStyle selectedStyle;
    private GUIStyle textStyle;
    private GUIStyle labelStyle;
    private GUIStyle subLabelStyle;
    private GUIStyle cornerStyle;
    private GUIStyle winStyle;
    private GUIStyle teleporterLabelStyle;
    private readonly Dictionary<string, Texture2D> transporterIconCache = new Dictionary<string, Texture2D>();

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

        // Find references if not assigned
        if (inventory == null)
        {
            inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        }

        if (editorController == null)
        {
            editorController = UnityEngine.Object.FindAnyObjectByType<EditorController>();
        }
    }

    private void OnGUI()
    {
        // In Unity Editor, OnGUI runs for multiple views (Scene, Game, etc.)
        // Only render in the primary display (display 0) to prevent double-rendering
#if UNITY_EDITOR
        if (Event.current.displayIndex != 0)
        {
            return;
        }
#endif

        if (inventory == null)
        {
            inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        }

        if (Application.isPlaying && editorController == null)
        {
            editorController = UnityEngine.Object.FindAnyObjectByType<EditorController>();
        }

        // Calculate scale factor
        if (autoScale)
        {
            // Scale based on screen height (1080p = 1.0x, 720p = 0.67x, 4K = 2.0x)
            _scaleFactor = (Screen.height / 1080f) * uiScale;
        }
        else
        {
            _scaleFactor = uiScale;
        }

        // Snap to quarter steps to keep IMGUI text/layout crisp.
        _scaleFactor = Mathf.Max(0.25f, Mathf.Round(_scaleFactor * 4f) / 4f);

        InitializeStyles();

        if (inventory == null)
        {
            DrawStatusLabel("InventoryUI: No BlockInventory found");
            return;
        }

        if (Application.isPlaying && editorController == null)
        {
            DrawStatusLabel("InventoryUI: No EditorController found");
            return;
        }

        if (Application.isPlaying)
        {
            DrawLockStatus();
        }

        if (!Application.isPlaying || editorController.currentMode != GameMode.Play)
        {
            GameMode mode = editorController != null ? editorController.currentMode : GameMode.Editor;
            IReadOnlyList<BlockInventoryEntry> entries = inventory.GetEntriesForMode(mode);
            bool showInfinite = mode == GameMode.LevelEditor;
            int drawIndex = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (ShouldHideFromInventory(entries[i], mode)) continue;
                bool isSelected = editorController != null && editorController.CurrentInventoryIndex == i;
                bool showKeyHint = editorController != null;
                DrawBlockSlot(entries[i], i, drawIndex, isSelected, showKeyHint, showInfinite);
                drawIndex++;
            }

            if (drawIndex == 0)
            {
                DrawStatusLabel("InventoryUI: No visible inventory entries");
            }
        }

    }


    private void InitializeStyles()
    {
        bool scaleChanged = !Mathf.Approximately(_lastScaleFactor, _scaleFactor);

        if (normalStyle == null)
        {
            normalStyle = new GUIStyle(GUI.skin.box);
            normalStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        }

        if (selectedStyle == null)
        {
            selectedStyle = new GUIStyle(GUI.skin.box);
            selectedStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.9f));            selectedStyle.border = new RectOffset(3, 3, 3, 3);
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;
            textStyle.fontStyle = FontStyle.Bold;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;
        }

        if (subLabelStyle == null)
        {
            subLabelStyle = new GUIStyle(GUI.skin.label);
            subLabelStyle.alignment = TextAnchor.MiddleCenter;
            subLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        }

        if (cornerStyle == null)
        {
            cornerStyle = new GUIStyle(GUI.skin.label);
            cornerStyle.alignment = TextAnchor.UpperRight;
            cornerStyle.normal.textColor = Color.white;
            cornerStyle.fontStyle = FontStyle.Bold;
        }

        if (teleporterLabelStyle == null)
        {
            teleporterLabelStyle = new GUIStyle(GUI.skin.label);
            teleporterLabelStyle.alignment = TextAnchor.MiddleCenter;
            teleporterLabelStyle.normal.textColor = Color.white;
            teleporterLabelStyle.fontStyle = FontStyle.Bold;
        }

        if (winStyle == null)
        {
            winStyle = new GUIStyle(GUI.skin.label);
            winStyle.alignment = TextAnchor.UpperRight;
            winStyle.normal.textColor = new Color(0.8f, 1f, 0.8f);
            winStyle.fontStyle = FontStyle.Bold;
        }

        if (scaleChanged)
        {
            textStyle.fontSize = ScaleFont(20);
            labelStyle.fontSize = ScaleFont(16);
            subLabelStyle.fontSize = ScaleFont(13);
            cornerStyle.fontSize = ScaleFont(18);
            teleporterLabelStyle.fontSize = ScaleFont(56);
            winStyle.fontSize = ScaleFont(20);
            _lastScaleFactor = _scaleFactor;
        }
    }

    private void DrawBlockSlot(BlockInventoryEntry entry, int index, int drawIndex, bool isSelected, bool showKeyHint, bool showInfinite)
    {
        if (entry == null) return;

        float scaledBoxSize = Scale(boxSize);
        float scaledSpacing = Scale(spacing);
        float scaledItemPadding = Scale(itemPadding);
        float scaledTopMargin = Scale(topMargin);
        float scaledLeftMargin = Scale(leftMargin);

        float xPos = Mathf.Round(GetViewLeft() + scaledLeftMargin);
        float yPos = Mathf.Round(scaledTopMargin + (drawIndex * (scaledBoxSize + scaledSpacing)));
        Rect boxRect = new Rect(xPos, yPos, scaledBoxSize, scaledBoxSize);

        // Draw background box
        GUI.Box(boxRect, "", isSelected ? selectedStyle : normalStyle);

        // Get inventory counts
        int available = inventory.GetDisplayAvailableCount(entry);
        int total = inventory.GetDisplayTotalCount(entry);

        // Draw colored block preview in the center
        Color blockColor = GetColorForBlockType(entry.blockType);
        if (!showInfinite && available == 0)
        {
            blockColor.a = 0.3f; // Dim if unavailable
        }

        float previewSize = scaledBoxSize * 0.4f;
        float previewX = xPos + (scaledBoxSize - previewSize) * 0.5f;
        float previewY = yPos + (scaledBoxSize - previewSize) * 0.5f - Scale(4f);
        Rect previewRect = new Rect(previewX, previewY, previewSize, previewSize);

        if (entry.blockType == BlockType.Transporter)
        {
            if (!DrawTransporterPreview(entry, previewRect, blockColor))
            {
                DrawSolidPreview(previewRect, blockColor);
            }
        }
        else
        {
            DrawSolidPreview(previewRect, blockColor);
        }

        // Draw count text at bottom
        Rect countRect = new Rect(xPos, yPos + scaledBoxSize - (Scale(18f) + scaledItemPadding), scaledBoxSize, Scale(18f));
        string countText = showInfinite ? "INF" : $"{available}/{total}";
        GUI.Label(countRect, countText, textStyle);

        // Draw block type label at top (height increased to prevent clipping)
        Rect labelRect = new Rect(xPos, yPos + scaledItemPadding, scaledBoxSize, Scale(18f));
        string blockName = entry.GetDisplayName();
        GUI.Label(labelRect, blockName, labelStyle);

        // Draw sublabel for pair inventories
        BlockInventoryEntry groupEntry = entry;
        bool isPair = groupEntry != null && groupEntry.isPairInventory;
        if (isPair)
        {
            Rect pairRect = new Rect(xPos, yPos + scaledItemPadding + Scale(18f), scaledBoxSize, Scale(16f));
            GUI.Label(pairRect, "(pairs)", subLabelStyle);
        }

        if (entry.blockType == BlockType.Teleporter)
        {
            DrawTeleporterLabel(entry, previewRect);
        }

        // Draw key hint
        Rect keyRect = new Rect(xPos, yPos + scaledBoxSize + Scale(4f), scaledBoxSize, Scale(20f));
        string keyHint = showKeyHint && index < 9 ? $"[{index + 1}]" : string.Empty;
        if (!string.IsNullOrEmpty(keyHint))
        {
            GUI.Label(keyRect, keyHint, labelStyle);
        }
    }

    private bool ShouldHideFromInventory(BlockInventoryEntry entry, GameMode mode)
    {
        if (entry == null) return true;
        if (mode == GameMode.LevelEditor) return false;
        return entry.blockType == BlockType.Key || entry.blockType == BlockType.Lock;
    }

    private void DrawLockStatus()
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

        float rightMargin = Scale(24f);
        float topOffset = Scale(20f);
        Rect statusRect = new Rect(0f, topOffset, Screen.width - rightMargin, Scale(24f));
        GUI.Label(statusRect, $"{lockedCount} of {totalLocks}", cornerStyle);

        if (editorController != null && editorController.currentMode == GameMode.Play && totalLocks > 0 && lockedCount >= totalLocks)
        {
            Rect winRect = new Rect(0f, topOffset + Scale(26f), Screen.width - rightMargin, Scale(24f));
            GUI.Label(winRect, "You win", winStyle);
        }
    }

    private void DrawStatusLabel(string text)
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };
        }

        Rect rect = new Rect(GetViewLeft() + Scale(leftMargin), Scale(topMargin), Scale(520f), Scale(24f));
        GUI.Label(rect, text, labelStyle);
    }

    private float Scale(float value)
    {
        return Mathf.Round(value * _scaleFactor);
    }

    private int ScaleFont(int value)
    {
        return Mathf.Max(1, Mathf.RoundToInt(value * _scaleFactor));
    }

    private void DrawSolidPreview(Rect previewRect, Color blockColor)
    {
        Color oldColor = GUI.color;
        GUI.color = blockColor;
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawTeleporterLabel(BlockInventoryEntry entry, Rect previewRect)
    {
        if (entry == null) return;
        string labelSource = entry.GetResolvedFlavorId();
        if (string.IsNullOrWhiteSpace(labelSource))
        {
            labelSource = entry.displayName;
        }
        if (string.IsNullOrWhiteSpace(labelSource))
        {
            labelSource = entry.GetDisplayName();
        }
        string label = ExtractTeleporterLabel(labelSource);
        if (string.IsNullOrWhiteSpace(label)) return;

        GUI.BeginGroup(previewRect);
        Rect localRect = new Rect(0f, 0f, previewRect.width, previewRect.height);
        GUI.Label(localRect, label.ToUpperInvariant(), teleporterLabelStyle);
        GUI.EndGroup();
    }

    private static string ExtractTeleporterLabel(string labelSource)
    {
        if (string.IsNullOrWhiteSpace(labelSource)) return string.Empty;
        string trimmed = labelSource.Trim();
        string[] parts = trimmed.Split(new[] { ' ', '-', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
        string token = parts.Length > 0 ? parts[parts.Length - 1] : trimmed;
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        return token.Length > 1 ? token.Substring(0, 1) : token;
    }

    private bool DrawTransporterPreview(BlockInventoryEntry entry, Rect previewRect, Color blockColor)
    {
        string[] steps = ResolveRouteSteps(entry);
        if (steps == null || steps.Length == 0) return false;

        int size = Mathf.RoundToInt(Mathf.Min(previewRect.width, previewRect.height));
        if (size <= 0) return false;

        Texture2D icon = GetTransporterRouteTexture(steps, size);
        if (icon == null) return false;

        Color oldColor = GUI.color;
        GUI.color = blockColor;
        GUI.DrawTexture(previewRect, icon);
        GUI.color = oldColor;
        return true;
    }

    private Texture2D GetTransporterRouteTexture(string[] routeSteps, int size)
    {
        string cacheKey = BuildRouteCacheKey(routeSteps, size);
        if (transporterIconCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = BuildTransporterRouteTexture(routeSteps, size);
        if (texture != null)
        {
            transporterIconCache[cacheKey] = texture;
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
        if (transporterIconCache.Count == 0) return;

        foreach (Texture2D texture in transporterIconCache.Values)
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

        transporterIconCache.Clear();
    }

    private float GetViewLeft()
    {
        Camera cam = Camera.main;
        if (cam == null) return 0f;
        return Mathf.Round(cam.ViewportToScreenPoint(new Vector3(0f, 0f, 0f)).x);
    }

    private Color GetColorForBlockType(BlockType blockType)
    {
        return BlockColors.GetColorForBlockType(blockType);
    }

    private string GetBlockTypeName(BlockType blockType)
    {
        return BlockColors.GetBlockTypeName(blockType);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    public void SetVisible(bool visible)
    {
        enabled = visible;
    }
}
