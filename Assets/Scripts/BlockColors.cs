using UnityEngine;

/// <summary>
/// Centralized color definitions for all block types.
/// Ensures visual consistency across the game.
/// </summary>
public static class BlockColors
{
    // Block type colors
    public static readonly Color Default = Color.white;
    public static readonly Color Teleporter = new Color(0.5f, 0f, 1f);      // Purple
    public static readonly Color Crumbler = new Color(1f, 0.5f, 0f);        // Orange
    public static readonly Color Transporter = Color.cyan;

    // Grid and UI colors
    public static readonly Color GridLine = new Color(1f, 1f, 1f, 0.3f);    // Transparent white
    public static readonly Color PlaceableBorder = Color.black;
    public static readonly Color NonPlaceableBorder = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark grey

    // Cursor colors
    public static readonly Color CursorPlaceable = new Color(1f, 1f, 1f, 1f);          // White
    public static readonly Color CursorEditable = new Color(0f, 1f, 0f, 1f);           // Green
    public static readonly Color CursorNonPlaceable = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Grey

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
            _ => "UNKNOWN"
        };
    }
}
