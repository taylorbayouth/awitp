using UnityEngine;

/// <summary>
/// Centralized constants for rendering layers and visual heights.
/// Ensures consistent z-ordering across all visual elements.
/// </summary>
public static class RenderingConstants
{
    // Visual layer heights (Y-axis position)
    public const float GRID_HEIGHT = 0.5f;              // Base grid lines
    public const float BORDER_HEIGHT = 0.55f;           // Placeable/non-placeable borders
    public const float CURSOR_HEIGHT = 0.6f;            // Cursor (topmost)

    // Render queue values (higher = rendered later/on top)
    public const int GRID_RENDER_QUEUE = 3000;          // Base layer
    public const int BORDER_RENDER_QUEUE = 3100;        // Middle layer
    public const int CURSOR_RENDER_QUEUE = 3200;        // Top layer

    // Render queue sorting order (used by BorderRenderer)
    public const int GRID_SORTING = 0;
    public const int BORDER_SORTING = 1;
    public const int CURSOR_SORTING = 2;

    // Line widths
    public const float GRID_LINE_WIDTH = 0.02f;
    public const float BORDER_LINE_WIDTH = 0.08f;
    public const float CURSOR_LINE_WIDTH = 0.08f;
}
