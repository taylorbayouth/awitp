using UnityEngine;

/// <summary>
/// Manages grid cursor visualization and movement for the level editor.
///
/// RESPONSIBILITIES:
/// - Cursor position tracking and movement
/// - Visual state updates (placeable, non-placeable, editable)
/// - Cursor visibility control
///
/// DEPENDENCIES:
/// - GridCoordinateSystem for position calculations
/// - GridCursor component for visual rendering
/// </summary>
public class GridCursorManager : MonoBehaviour
{
    private GridCoordinateSystem coordinateSystem;
    private GridManager gridManager; // Reference for state queries (placeable spaces, occupied)

    [Header("Cursor State")]
    private int currentCursorIndex = 0;
    private GameObject cursorHighlight;
    private GridCursor gridCursor;

    [Header("Cursor Prefab")]
    private GameObject cursorHighlightPrefab;

    /// <summary>
    /// Gets the current cursor grid index.
    /// </summary>
    public int CurrentCursorIndex => currentCursorIndex;

    /// <summary>
    /// Initializes the cursor manager with coordinate system and optional prefab.
    /// </summary>
    /// <param name="coordinateSystem">Grid coordinate system</param>
    /// <param name="prefab">Optional cursor prefab (creates default if null)</param>
    /// <param name="cellSize">Grid cell size for cursor initialization</param>
    /// <param name="gridManager">Reference to GridManager for state queries</param>
    public void Initialize(GridCoordinateSystem coordinateSystem, GameObject prefab, float cellSize, GridManager gridManager)
    {
        this.coordinateSystem = coordinateSystem;
        this.cursorHighlightPrefab = prefab;
        this.gridManager = gridManager;

        CreateCursor(cellSize);
    }

    /// <summary>
    /// Creates the cursor GameObject and GridCursor component.
    /// </summary>
    private void CreateCursor(float cellSize)
    {
        if (cursorHighlightPrefab != null)
        {
            cursorHighlight = Instantiate(cursorHighlightPrefab);
            gridCursor = cursorHighlight.GetComponent<GridCursor>();
        }
        else
        {
            // Create default cursor if no prefab provided
            cursorHighlight = new GameObject("GridCursor");
            gridCursor = cursorHighlight.AddComponent<GridCursor>();
        }

        if (gridCursor != null)
        {
            gridCursor.Initialize(cellSize);
        }

        UpdateCursorPosition();
        UpdateCursorState();

        DebugLog.Info("[GridCursorManager] Cursor created and initialized");
    }

    /// <summary>
    /// Moves cursor by a direction vector (e.g., Vector2Int.right).
    /// Clamps movement to grid bounds.
    /// </summary>
    /// <param name="direction">Direction to move (-1, 0, 1 for each axis)</param>
    public void MoveCursor(Vector2Int direction)
    {
        if (coordinateSystem == null)
        {
            Debug.LogWarning("[GridCursorManager] Cannot move cursor - coordinate system not initialized");
            return;
        }

        // Validate grid dimensions before cursor movement
        if (coordinateSystem.GridWidth <= 0 || coordinateSystem.GridHeight <= 0)
        {
            Debug.LogWarning("[GridCursorManager] Cannot move cursor - invalid grid dimensions");
            return;
        }

        Vector2Int currentCoords = coordinateSystem.IndexToCoordinates(currentCursorIndex);
        Vector2Int newCoords = currentCoords + direction;

        // Clamp to grid bounds
        newCoords = coordinateSystem.ClampCoordinates(newCoords);

        currentCursorIndex = coordinateSystem.CoordinatesToIndex(newCoords);
        UpdateCursorPosition();
        UpdateCursorState();
    }

    /// <summary>
    /// Sets cursor to a specific grid index.
    /// Only updates if index is valid.
    /// </summary>
    /// <param name="index">Target grid index</param>
    public void SetCursorIndex(int index)
    {
        if (coordinateSystem == null || !coordinateSystem.IsValidIndex(index))
        {
            Debug.LogWarning($"[GridCursorManager] Invalid cursor index: {index}");
            return;
        }

        currentCursorIndex = index;
        UpdateCursorPosition();
        UpdateCursorState();
    }

    /// <summary>
    /// Updates cursor GameObject position to match current grid index.
    /// </summary>
    private void UpdateCursorPosition()
    {
        if (cursorHighlight != null && coordinateSystem != null)
        {
            Vector3 worldPos = coordinateSystem.IndexToWorldPosition(currentCursorIndex);
            cursorHighlight.transform.position = worldPos;
        }
    }

    /// <summary>
    /// Controls cursor visibility.
    /// </summary>
    /// <param name="visible">True to show cursor, false to hide</param>
    public void SetCursorVisible(bool visible)
    {
        if (gridCursor != null)
        {
            gridCursor.SetVisible(visible);
        }
    }

    /// <summary>
    /// Updates cursor visual state based on grid conditions.
    /// Queries GridManager for placeable space and occupation status.
    /// </summary>
    public void UpdateCursorState()
    {
        if (gridCursor == null || gridManager == null) return;

        bool isPlaceable = gridManager.IsSpacePlaceable(currentCursorIndex);
        bool hasBlock = gridManager.IsGridSpaceOccupied(currentCursorIndex);

        // Determine cursor state based on grid conditions
        if (hasBlock)
        {
            // Space has a block - editable
            gridCursor.SetState(GridCursor.CursorState.Editable);
        }
        else if (!isPlaceable)
        {
            // Space marked as non-placeable
            gridCursor.SetState(GridCursor.CursorState.NonPlaceable);
        }
        else
        {
            // Empty and placeable
            gridCursor.SetState(GridCursor.CursorState.Placeable);
        }
    }

    /// <summary>
    /// Gets the world position of the current cursor location.
    /// </summary>
    /// <returns>World position at center of current cell</returns>
    public Vector3 GetCurrentWorldPosition()
    {
        if (coordinateSystem == null) return Vector3.zero;
        return coordinateSystem.IndexToWorldPosition(currentCursorIndex);
    }

    /// <summary>
    /// Gets the grid coordinates of the current cursor location.
    /// </summary>
    /// <returns>Grid coordinates (x, y)</returns>
    public Vector2Int GetCurrentCoordinates()
    {
        if (coordinateSystem == null) return Vector2Int.zero;
        return coordinateSystem.IndexToCoordinates(currentCursorIndex);
    }

    /// <summary>
    /// Refreshes cursor state after external changes (e.g., block placed/removed).
    /// </summary>
    public void RefreshState()
    {
        UpdateCursorState();
    }

    private void OnDestroy()
    {
        if (cursorHighlight != null)
        {
            Destroy(cursorHighlight);
        }

        DebugLog.Info("[GridCursorManager] Destroyed");
    }
}
