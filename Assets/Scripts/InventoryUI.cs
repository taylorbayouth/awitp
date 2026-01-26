using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Displays block inventory counts and current selection using OnGUI
/// </summary>
[ExecuteAlways]
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public BlockInventory inventory;
    public EditorController editorController;

    [Header("UI Settings")]
    public float boxSize = 110f;
    public float spacing = 20f;
    public float itemPadding = 6f;
    public float topMargin = 24f;
    public float leftMargin = 24f;

    private GUIStyle normalStyle;
    private GUIStyle selectedStyle;
    private GUIStyle textStyle;
    private GUIStyle labelStyle;
    private GUIStyle subLabelStyle;
    private GUIStyle cornerStyle;
    private GUIStyle winStyle;

    private void Awake()
    {
        // Find references if not assigned
        if (inventory == null)
        {
            inventory = FindObjectOfType<BlockInventory>();
        }

        if (editorController == null)
        {
            editorController = FindObjectOfType<EditorController>();
        }
    }

    private void OnGUI()
    {
        if (inventory == null)
        {
            inventory = FindObjectOfType<BlockInventory>();
        }

        if (Application.isPlaying && editorController == null)
        {
            editorController = FindObjectOfType<EditorController>();
        }

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
            IReadOnlyList<BlockInventoryEntry> entries = inventory.GetEntries();
            int drawIndex = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (ShouldHideFromInventory(entries[i])) continue;
                bool isSelected = editorController != null && editorController.CurrentInventoryIndex == i;
                bool showKeyHint = editorController != null;
                DrawBlockSlot(entries[i], i, drawIndex, isSelected, showKeyHint);
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
            textStyle.fontSize = 18;
            textStyle.fontStyle = FontStyle.Bold;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
        }

        if (subLabelStyle == null)
        {
            subLabelStyle = new GUIStyle(GUI.skin.label);
            subLabelStyle.alignment = TextAnchor.MiddleCenter;
            subLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            subLabelStyle.fontSize = 12;
        }

        if (cornerStyle == null)
        {
            cornerStyle = new GUIStyle(GUI.skin.label);
            cornerStyle.alignment = TextAnchor.UpperRight;
            cornerStyle.normal.textColor = Color.white;
            cornerStyle.fontSize = 16;
            cornerStyle.fontStyle = FontStyle.Bold;
        }

        if (winStyle == null)
        {
            winStyle = new GUIStyle(GUI.skin.label);
            winStyle.alignment = TextAnchor.UpperRight;
            winStyle.normal.textColor = new Color(0.8f, 1f, 0.8f);
            winStyle.fontSize = 18;
            winStyle.fontStyle = FontStyle.Bold;
        }
    }

    private void DrawBlockSlot(BlockInventoryEntry entry, int index, int drawIndex, bool isSelected, bool showKeyHint)
    {
        if (entry == null) return;

        float xPos = GetViewLeft() + leftMargin;
        float yPos = topMargin + (drawIndex * (boxSize + spacing));
        Rect boxRect = new Rect(xPos, yPos, boxSize, boxSize);

        // Draw background box
        GUI.Box(boxRect, "", isSelected ? selectedStyle : normalStyle);

        // Get inventory counts
        int available = inventory.GetDisplayAvailableCount(entry);
        int total = inventory.GetDisplayTotalCount(entry);

        // Draw colored block preview in the center
        Color blockColor = GetColorForBlockType(entry.blockType);
        if (available == 0)
        {
            blockColor.a = 0.3f; // Dim if unavailable
        }

        float previewSize = boxSize * 0.4f;
        float previewX = xPos + (boxSize - previewSize) * 0.5f;
        float previewY = yPos + (boxSize - previewSize) * 0.5f - 4f;
        Rect previewRect = new Rect(previewX, previewY, previewSize, previewSize);

        Color oldColor = GUI.color;
        GUI.color = blockColor;
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw count text at bottom
        Rect countRect = new Rect(xPos, yPos + boxSize - (18f + itemPadding), boxSize, 18f);
        GUI.Label(countRect, $"{available}/{total}", textStyle);

        // Draw block type label at top (height increased to prevent clipping)
        Rect labelRect = new Rect(xPos, yPos + itemPadding, boxSize, 18f);
        string blockName = entry.GetDisplayName();
        GUI.Label(labelRect, blockName, labelStyle);

        // Draw sublabel for pair inventories
        BlockInventoryEntry groupEntry = entry;
        bool isPair = groupEntry != null && groupEntry.isPairInventory;
        if (isPair)
        {
            Rect pairRect = new Rect(xPos, yPos + itemPadding + 18f, boxSize, 16f);
            GUI.Label(pairRect, "(pairs)", subLabelStyle);
        }

        // Draw key hint
        Rect keyRect = new Rect(xPos, yPos + boxSize + 4f, boxSize, 20f);
        string keyHint = showKeyHint && index < 9 ? $"[{index + 1}]" : string.Empty;
        if (!string.IsNullOrEmpty(keyHint))
        {
            GUI.Label(keyRect, keyHint, labelStyle);
        }
    }

    private bool ShouldHideFromInventory(BlockInventoryEntry entry)
    {
        if (entry == null) return true;
        return entry.blockType == BlockType.Key || entry.blockType == BlockType.Lock;
    }

    private void DrawLockStatus()
    {
        LockBlock[] locks = FindObjectsOfType<LockBlock>();
        int totalLocks = locks.Length;
        int lockedCount = 0;
        foreach (LockBlock lockBlock in locks)
        {
            if (lockBlock != null && lockBlock.HasKeyLocked())
            {
                lockedCount++;
            }
        }

        float rightMargin = 24f;
        float topOffset = 20f;
        Rect statusRect = new Rect(0f, topOffset, Screen.width - rightMargin, 24f);
        GUI.Label(statusRect, $"{lockedCount} of {totalLocks}", cornerStyle);

        if (editorController != null && editorController.currentMode == GameMode.Play && totalLocks > 0 && lockedCount >= totalLocks)
        {
            Rect winRect = new Rect(0f, topOffset + 26f, Screen.width - rightMargin, 24f);
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

        Rect rect = new Rect(GetViewLeft() + leftMargin, topMargin, 520f, 24f);
        GUI.Label(rect, text, labelStyle);
    }

    private float GetViewLeft()
    {
        Camera cam = Camera.main;
        if (cam == null) return 0f;
        return cam.ViewportToScreenPoint(new Vector3(0f, 0f, 0f)).x;
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
