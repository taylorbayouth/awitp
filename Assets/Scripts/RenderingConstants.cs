using UnityEngine;

/// <summary>
/// Centralized constants for rendering layers and visual depths.
/// Grid is on XY plane. Camera is at -Z looking toward +Z.
/// Lower Z = closer to camera.
/// </summary>
public static class RenderingConstants
{
    // Visual layer depths (Z-axis position)
    // Camera is at -Z looking toward +Z, so LOWER Z = closer to camera
    // Using larger gaps to prevent z-fighting and sub-pixel rendering issues
    public const float CURSOR_DEPTH = -.001f;            // Cursor (closest to camera)
    public const float BORDER_DEPTH = -.001f;            // Placeable borders (in front of grid)
    public const float BLOCK_DEPTH = 0f;                // Blocks render at Z=0
    public const float GRID_DEPTH = 0f;                 // Grid lines (furthest from camera)

    // Sorting order for LineRenderer (higher = rendered on top)
    public const int GRID_SORTING = 0;
    public const int BORDER_SORTING = 1;
    public const int CURSOR_SORTING = 2;

    // Line widths
    public const float GRID_LINE_WIDTH = 0.01f;         // Thin grid
    public const float BORDER_LINE_WIDTH = 0.037f;       // Thicker borders
    public const float CURSOR_LINE_WIDTH = 0.037f;        // Thickest cursor

    // Opacity values (0.0 = fully transparent, 1.0 = fully opaque)
    public const float GRID_LINE_OPACITY = 0.2f;        // Grid line opacity
}