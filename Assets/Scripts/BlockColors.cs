using UnityEngine;

/// <summary>
/// Centralized color definitions for all block types.
/// </summary>
public static class BlockColors
{
    // Block type colors
    public static readonly Color Default = Color.white;
    public static readonly Color Teleporter = new Color(0.5f, 0f, 1f);      // Purple
    public static readonly Color Crumbler = new Color(1f, 0.5f, 0f);        // Orange
    public static readonly Color Transporter = Color.cyan;
    public static readonly Color Key = new Color(0.75f, 0.95f, 0.75f);      // Pale green
    public static readonly Color Lock = Color.black;

    // Grid and UI colors
    public static readonly Color GridLine = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray (solid)
    public static readonly Color PlaceableBorder = Color.black;              // 100% black

    // Cursor colors
    public static readonly Color CursorPlaceable = Color.blue;               // Blue
    public static readonly Color CursorEditable = Color.green;               // Green
    public static readonly Color CursorNonPlaceable = Color.red;             // Red

    /// <summary>
    /// Gets the color for a specific block type.
    /// </summary>
    public static Color GetColorForBlockType(BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Default => Default,
            BlockType.Teleporter => Teleporter,
            BlockType.Crumbler => Crumbler,
            BlockType.Transporter => Transporter,
            BlockType.Key => Key,
            BlockType.Lock => Lock,
            _ => Color.gray
        };
    }

    /// <summary>
    /// Gets the display name for a block type (all caps).
    /// </summary>
    public static string GetBlockTypeName(BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Default => "DEFAULT",
            BlockType.Teleporter => "TELEPORT",
            BlockType.Crumbler => "CRUMBLE",
            BlockType.Transporter => "TRANSPORT",
            BlockType.Key => "KEY",
            BlockType.Lock => "LOCK",
            _ => "UNKNOWN"
        };
    }
}
