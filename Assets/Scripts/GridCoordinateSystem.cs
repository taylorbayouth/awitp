using UnityEngine;

/// <summary>
/// Pure coordinate system for grid-based gameplay.
/// Handles all mathematical conversions between indices, coordinates, and world positions.
///
/// ARCHITECTURE:
/// - Grid exists on XY plane (vertical wall), viewed from negative Z
/// - X axis = horizontal (left/right)
/// - Y axis = vertical (up/down)
/// - Z axis = depth (always 0 for grid positions)
///
/// DESIGN: Pure C# class with no MonoBehaviour dependencies for easy unit testing.
/// Only uses UnityEngine for Vector2Int and Vector3 types.
/// </summary>
public class GridCoordinateSystem
{
    // Grid dimensions
    private readonly int gridWidth;
    private readonly int gridHeight;
    private readonly float cellSize;

    // Calculated origin point (bottom-left corner of grid)
    private Vector3 gridOrigin;

    /// <summary>
    /// Gets the grid width (number of cells horizontally).
    /// </summary>
    public int GridWidth => gridWidth;

    /// <summary>
    /// Gets the grid height (number of cells vertically).
    /// </summary>
    public int GridHeight => gridHeight;

    /// <summary>
    /// Gets the size of each grid cell in world units.
    /// </summary>
    public float CellSize => cellSize;

    /// <summary>
    /// Gets the calculated grid origin (bottom-left corner in world space).
    /// </summary>
    public Vector3 GridOrigin => gridOrigin;

    /// <summary>
    /// Gets the total number of cells in the grid.
    /// </summary>
    public int TotalCells => gridWidth * gridHeight;

    /// <summary>
    /// Creates a new grid coordinate system.
    /// </summary>
    /// <param name="width">Grid width in cells</param>
    /// <param name="height">Grid height in cells</param>
    /// <param name="cellSize">Size of each cell in world units</param>
    public GridCoordinateSystem(int width, int height, float cellSize)
    {
        this.gridWidth = width;
        this.gridHeight = height;
        this.cellSize = cellSize;

        CalculateGridOrigin();
    }

    /// <summary>
    /// Calculates the grid origin to center the grid at world origin.
    ///
    /// Why: Unity uses bottom-left origin (0,0) by default, but we want centered grid.
    /// Rationale: Makes level design more intuitive and symmetric layouts easier.
    /// </summary>
    private void CalculateGridOrigin()
    {
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize;

        // Position grid so its center is at world origin (0, 0, 0)
        gridOrigin = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0);
    }

    /// <summary>
    /// Updates grid dimensions and recalculates origin.
    /// Used when loading levels with different grid sizes.
    /// </summary>
    public void UpdateDimensions(int newWidth, int newHeight, float newCellSize)
    {
        // Note: Using reflection to set readonly fields
        // This is safe because we control when this is called (level load only)
        typeof(GridCoordinateSystem).GetField("gridWidth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, newWidth);

        typeof(GridCoordinateSystem).GetField("gridHeight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, newHeight);

        typeof(GridCoordinateSystem).GetField("cellSize",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, newCellSize);

        CalculateGridOrigin();
    }

    #region Index ↔ Coordinates Conversion

    /// <summary>
    /// Converts a flat grid index to 2D coordinates.
    /// </summary>
    /// <param name="index">Flat index (0 to gridWidth * gridHeight - 1)</param>
    /// <returns>2D coordinates (x, y)</returns>
    public Vector2Int IndexToCoordinates(int index)
    {
        int x = index % gridWidth;
        int y = index / gridWidth;
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts 2D coordinates to a flat grid index.
    /// </summary>
    /// <param name="coords">Grid coordinates (x, y)</param>
    /// <returns>Flat index</returns>
    public int CoordinatesToIndex(Vector2Int coords)
    {
        return coords.y * gridWidth + coords.x;
    }

    /// <summary>
    /// Converts 2D coordinates to a flat grid index.
    /// </summary>
    /// <param name="x">X coordinate (horizontal)</param>
    /// <param name="y">Y coordinate (vertical)</param>
    /// <returns>Flat index</returns>
    public int CoordinatesToIndex(int x, int y)
    {
        return y * gridWidth + x;
    }

    #endregion

    #region Coordinates ↔ World Position Conversion

    /// <summary>
    /// Converts grid coordinates to world position (center of cell).
    /// Grid is on XY plane: X = horizontal, Y = vertical, Z = 0.
    /// </summary>
    /// <param name="coords">Grid coordinates</param>
    /// <returns>World position at center of cell</returns>
    public Vector3 CoordinatesToWorldPosition(Vector2Int coords)
    {
        float halfCell = cellSize * 0.5f;
        return gridOrigin + new Vector3(
            (coords.x * cellSize) + halfCell,  // X = horizontal
            (coords.y * cellSize) + halfCell,  // Y = vertical
            0                                   // Z = 0 (on the wall)
        );
    }

    /// <summary>
    /// Converts grid coordinates to world position (center of cell).
    /// </summary>
    /// <param name="x">X coordinate (horizontal)</param>
    /// <param name="y">Y coordinate (vertical)</param>
    /// <returns>World position at center of cell</returns>
    public Vector3 CoordinatesToWorldPosition(int x, int y)
    {
        return CoordinatesToWorldPosition(new Vector2Int(x, y));
    }

    /// <summary>
    /// Converts world position to grid coordinates.
    /// </summary>
    /// <param name="worldPos">World position</param>
    /// <returns>Grid coordinates (may be outside valid range)</returns>
    public Vector2Int WorldPositionToCoordinates(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridOrigin;
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    #endregion

    #region Index ↔ World Position Conversion

    /// <summary>
    /// Converts grid index to world position (center of cell).
    /// </summary>
    /// <param name="index">Flat grid index</param>
    /// <returns>World position at center of cell</returns>
    public Vector3 IndexToWorldPosition(int index)
    {
        Vector2Int coords = IndexToCoordinates(index);
        return CoordinatesToWorldPosition(coords);
    }

    /// <summary>
    /// Converts world position to grid index.
    /// </summary>
    /// <param name="worldPos">World position</param>
    /// <returns>Flat grid index (may be invalid, use IsValidIndex to check)</returns>
    public int WorldPositionToGridIndex(Vector3 worldPos)
    {
        Vector2Int coords = WorldPositionToCoordinates(worldPos);
        return CoordinatesToIndex(coords);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a grid index is within valid range.
    /// </summary>
    /// <param name="index">Grid index to validate</param>
    /// <returns>True if index is valid (0 to gridWidth * gridHeight - 1)</returns>
    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < TotalCells;
    }

    /// <summary>
    /// Checks if coordinates are within grid bounds.
    /// </summary>
    /// <param name="coords">Coordinates to validate</param>
    /// <returns>True if coordinates are within bounds</returns>
    public bool IsValidCoordinates(Vector2Int coords)
    {
        return coords.x >= 0 && coords.x < gridWidth &&
               coords.y >= 0 && coords.y < gridHeight;
    }

    /// <summary>
    /// Checks if coordinates are within grid bounds.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>True if coordinates are within bounds</returns>
    public bool IsValidCoordinates(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Gets the bounds of the grid in world space.
    /// </summary>
    /// <returns>Bounds struct representing grid area</returns>
    public Bounds GetWorldBounds()
    {
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize;

        // Center is at world origin since grid is centered
        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(totalWidth, totalHeight, 0);

        return new Bounds(center, size);
    }

    /// <summary>
    /// Clamps coordinates to grid bounds.
    /// </summary>
    /// <param name="coords">Coordinates to clamp</param>
    /// <returns>Clamped coordinates</returns>
    public Vector2Int ClampCoordinates(Vector2Int coords)
    {
        int x = Mathf.Clamp(coords.x, 0, gridWidth - 1);
        int y = Mathf.Clamp(coords.y, 0, gridHeight - 1);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Clamps index to valid range.
    /// </summary>
    /// <param name="index">Index to clamp</param>
    /// <returns>Clamped index</returns>
    public int ClampIndex(int index)
    {
        return Mathf.Clamp(index, 0, TotalCells - 1);
    }

    #endregion
}
