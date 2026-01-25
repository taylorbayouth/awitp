using UnityEngine;

/// <summary>
/// Displays block inventory counts and current selection using OnGUI
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public BlockInventory inventory;
    public EditorController editorController;

    [Header("UI Settings")]
    public float boxSize = 160f;  // Doubled from 80f
    public float spacing = 20f;   // Doubled from 10f
    public float topMargin = 40f; // Doubled from 20f
    public float leftMargin = 40f; // Doubled from 20f

    private GUIStyle normalStyle;
    private GUIStyle selectedStyle;
    private GUIStyle textStyle;
    private GUIStyle labelStyle;

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
        if (inventory == null || editorController == null) return;
        if (editorController.currentMode == GameMode.Play) return;

        InitializeStyles();

        BlockType[] blockTypes = new BlockType[]
        {
            BlockType.Default,
            BlockType.Teleporter,
            BlockType.Crumbler,
            BlockType.Transporter
        };

        for (int i = 0; i < blockTypes.Length; i++)
        {
            DrawBlockSlot(blockTypes[i], i);
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
            selectedStyle.normal.background = MakeTex(2, 2, new Color(1f, 1f, 0f, 0.9f));
            selectedStyle.border = new RectOffset(3, 3, 3, 3);
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;
            textStyle.fontSize = 28;  // Doubled from 14
            textStyle.fontStyle = FontStyle.Bold;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 20;  // Doubled from 10
        }
    }

    private void DrawBlockSlot(BlockType blockType, int index)
    {
        float xPos = leftMargin + (index * (boxSize + spacing));
        Rect boxRect = new Rect(xPos, topMargin, boxSize, boxSize);

        // Determine if this block is selected
        bool isSelected = editorController.currentBlockType == blockType;

        // Draw background box
        GUI.Box(boxRect, "", isSelected ? selectedStyle : normalStyle);

        // Get inventory counts
        int available = inventory.GetAvailableCount(blockType);
        int total = inventory.GetTotalCount(blockType);

        // Draw colored block preview in the center
        Color blockColor = GetColorForBlockType(blockType);
        if (available == 0)
        {
            blockColor.a = 0.3f; // Dim if unavailable
        }

        float previewSize = boxSize * 0.4f;
        float previewX = xPos + (boxSize - previewSize) * 0.5f;
        float previewY = topMargin + (boxSize - previewSize) * 0.5f - 5f;
        Rect previewRect = new Rect(previewX, previewY, previewSize, previewSize);

        Color oldColor = GUI.color;
        GUI.color = blockColor;
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw count text at bottom
        Rect countRect = new Rect(xPos, topMargin + boxSize - 25f, boxSize, 20f);
        GUI.Label(countRect, $"{available}/{total}", textStyle);

        // Draw block type label at top (height increased to prevent clipping)
        Rect labelRect = new Rect(xPos, topMargin + 5f, boxSize, 25f);
        string blockName = GetBlockTypeName(blockType);
        GUI.Label(labelRect, blockName, labelStyle);

        // Draw key hint (height increased to prevent clipping)
        Rect keyRect = new Rect(xPos, topMargin + boxSize + 2f, boxSize, 25f);
        GUI.Label(keyRect, $"[{index + 1}]", labelStyle);
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
