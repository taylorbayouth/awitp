using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Cursor Settings")]
    public int currentCursorIndex = 0;
    public GameObject cursorHighlightPrefab;
    private GameObject cursorHighlight;
    private GridCursor gridCursor;

    [Header("Placeable Spaces")]
    private bool[] placeableSpaces; // Track which grid spaces can have blocks

    // Track all placed blocks
    private Dictionary<int, BaseBlock> placedBlocks = new Dictionary<int, BaseBlock>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize placeable spaces array
        InitializePlaceableSpaces();

        // Create cursor highlight
        CreateCursor();
    }

    private void Start()
    {
        // Initialize visualizer after everything is set up
        PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
        if (visualizer != null)
        {
            visualizer.RefreshVisuals();
        }
    }

    private void InitializePlaceableSpaces()
    {
        int totalSpaces = gridWidth * gridHeight;
        placeableSpaces = new bool[totalSpaces];
        // All spaces start as non-placeable (false is default for bool arrays)
    }

    #region Grid Coordinate Conversion

    public Vector3 IndexToWorldPosition(int index)
    {
        Vector2Int coords = IndexToCoordinates(index);
        return CoordinatesToWorldPosition(coords);
    }

    public Vector2Int IndexToCoordinates(int index)
    {
        int x = index % gridWidth;
        int y = index / gridWidth;
        return new Vector2Int(x, y);
    }

    public int CoordinatesToIndex(Vector2Int coords)
    {
        return coords.y * gridWidth + coords.x;
    }

    public int CoordinatesToIndex(int x, int y)
    {
        return y * gridWidth + x;
    }

    public Vector3 CoordinatesToWorldPosition(Vector2Int coords)
    {
        // Add 0.5 offset to center blocks in grid squares
        float halfCell = cellSize * 0.5f;
        return gridOrigin + new Vector3((coords.x * cellSize) + halfCell, 0, (coords.y * cellSize) + halfCell);
    }

    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < (gridWidth * gridHeight);
    }

    public bool IsValidCoordinates(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    #endregion

    #region Block Placement and Management

    public BaseBlock PlaceBlock(BlockType blockType, int gridIndex)
    {
        if (!IsValidIndex(gridIndex))
        {
            Debug.LogWarning($"Invalid grid index: {gridIndex}");
            return null;
        }

        // Check if space is placeable
        if (!IsSpacePlaceable(gridIndex))
        {
            Debug.LogWarning($"Cannot place block at index {gridIndex}: space is not placeable");
            return null;
        }

        // Check block inventory
        BlockInventory inventory = FindObjectOfType<BlockInventory>();
        if (inventory != null && !inventory.CanPlaceBlock(blockType))
        {
            Debug.LogWarning($"Cannot place {blockType} block: no blocks remaining in inventory");
            return null;
        }

        // Remove existing block at this position if any
        if (placedBlocks.ContainsKey(gridIndex))
        {
            placedBlocks[gridIndex].DestroyBlock();
        }

        // Use block from inventory
        if (inventory != null)
        {
            inventory.UseBlock(blockType);
        }

        // Create new block
        BaseBlock newBlock = BaseBlock.Instantiate(blockType, gridIndex);
        newBlock.transform.position = IndexToWorldPosition(gridIndex);

        // Register the block
        placedBlocks[gridIndex] = newBlock;

        // Update cursor state in case cursor is on this space
        UpdateCursorState();

        return newBlock;
    }

    public void RegisterBlock(BaseBlock block)
    {
        if (IsValidIndex(block.gridIndex))
        {
            placedBlocks[block.gridIndex] = block;
        }
    }

    public void UnregisterBlock(BaseBlock block)
    {
        if (placedBlocks.ContainsKey(block.gridIndex))
        {
            placedBlocks.Remove(block.gridIndex);
        }

        // Update cursor state in case cursor is on this space
        UpdateCursorState();
    }

    public BaseBlock GetBlockAtIndex(int index)
    {
        if (placedBlocks.ContainsKey(index))
        {
            return placedBlocks[index];
        }
        return null;
    }

    public bool IsGridSpaceOccupied(int index)
    {
        return placedBlocks.ContainsKey(index);
    }

    #endregion

    #region Cursor Movement

    private void CreateCursor()
    {
        if (cursorHighlightPrefab != null)
        {
            cursorHighlight = Instantiate(cursorHighlightPrefab);
            gridCursor = cursorHighlight.GetComponent<GridCursor>();
        }
        else
        {
            // Create cursor programmatically
            cursorHighlight = new GameObject("GridCursor");
            gridCursor = cursorHighlight.AddComponent<GridCursor>();
        }

        // Initialize the cursor with cell size
        if (gridCursor != null)
        {
            gridCursor.Initialize(cellSize);
        }

        UpdateCursorPosition();
        UpdateCursorState();
    }

    public void MoveCursor(Vector2Int direction)
    {
        Vector2Int currentCoords = IndexToCoordinates(currentCursorIndex);
        Vector2Int newCoords = currentCoords + direction;

        // Clamp to grid bounds
        newCoords.x = Mathf.Clamp(newCoords.x, 0, gridWidth - 1);
        newCoords.y = Mathf.Clamp(newCoords.y, 0, gridHeight - 1);

        currentCursorIndex = CoordinatesToIndex(newCoords);
        UpdateCursorPosition();
        UpdateCursorState();
    }

    public void SetCursorIndex(int index)
    {
        if (IsValidIndex(index))
        {
            currentCursorIndex = index;
            UpdateCursorPosition();
            UpdateCursorState();
        }
    }

    private void UpdateCursorPosition()
    {
        if (cursorHighlight != null)
        {
            cursorHighlight.transform.position = IndexToWorldPosition(currentCursorIndex);
        }
    }

    private void UpdateCursorState()
    {
        if (gridCursor == null) return;

        // Check if current position is placeable
        bool isPlaceable = IsSpacePlaceable(currentCursorIndex);

        // Check if there's a block at current position
        bool hasBlock = IsGridSpaceOccupied(currentCursorIndex);

        // Determine cursor state
        if (!isPlaceable)
        {
            gridCursor.SetState(GridCursor.CursorState.NonPlaceable);
        }
        else if (hasBlock)
        {
            gridCursor.SetState(GridCursor.CursorState.Editable);
        }
        else
        {
            gridCursor.SetState(GridCursor.CursorState.Placeable);
        }
    }

    #endregion

    #region Placeable Space Management

    public bool IsSpacePlaceable(int index)
    {
        if (!IsValidIndex(index)) return false;
        return placeableSpaces[index];
    }

    public void SetSpacePlaceable(int index, bool placeable)
    {
        if (IsValidIndex(index))
        {
            placeableSpaces[index] = placeable;
            UpdateCursorState();

            // Update visualizer
            PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
            if (visualizer != null)
            {
                visualizer.UpdateMarkerAtIndex(index);
            }
        }
    }

    public void SetSpacePlaceable(int x, int y, bool placeable)
    {
        int index = CoordinatesToIndex(x, y);
        SetSpacePlaceable(index, placeable);
    }

    public void ToggleSpacePlaceable(int index)
    {
        if (IsValidIndex(index))
        {
            placeableSpaces[index] = !placeableSpaces[index];
            UpdateCursorState();

            // Update visualizer
            PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
            if (visualizer != null)
            {
                visualizer.UpdateMarkerAtIndex(index);
            }
        }
    }

    #endregion

    #region Grid Refresh

    private void OnValidate()
    {
        // Don't refresh during OnValidate - causes Unity errors
        // Grid will refresh on Start/Play
    }

    public void RefreshGrid()
    {
        // Refresh grid visualization
        GridVisualizer visualizer = GetComponent<GridVisualizer>();
        if (visualizer != null)
        {
            visualizer.RefreshGrid();
        }

        // Refresh placeable space visualization
        PlaceableSpaceVisualizer spaceVisualizer = GetComponent<PlaceableSpaceVisualizer>();
        if (spaceVisualizer != null)
        {
            spaceVisualizer.RefreshVisuals();
        }

        // Refresh camera setup
        CameraSetup cameraSetup = FindObjectOfType<CameraSetup>();
        if (cameraSetup != null)
        {
            cameraSetup.RefreshCamera();
        }

        Debug.Log($"Grid refreshed: {gridWidth}x{gridHeight}");
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        // Draw grid
        Gizmos.color = Color.gray;

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0, y * cellSize);
            Vector3 end = gridOrigin + new Vector3(gridWidth * cellSize, 0, y * cellSize);
            Gizmos.DrawLine(start, end);
        }

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0, 0);
            Vector3 end = gridOrigin + new Vector3(x * cellSize, 0, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw cursor position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 cursorPos = IndexToWorldPosition(currentCursorIndex);
            Gizmos.DrawWireCube(cursorPos, Vector3.one * cellSize);
        }
    }

    #endregion
}
