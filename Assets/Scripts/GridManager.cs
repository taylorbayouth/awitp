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

        // Create cursor highlight if prefab is assigned
        if (cursorHighlightPrefab != null)
        {
            cursorHighlight = Instantiate(cursorHighlightPrefab);
            UpdateCursorPosition();
        }
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
        return gridOrigin + new Vector3(coords.x * cellSize, 0, coords.y * cellSize);
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

        // Remove existing block at this position if any
        if (placedBlocks.ContainsKey(gridIndex))
        {
            placedBlocks[gridIndex].DestroyBlock();
        }

        // Create new block
        BaseBlock newBlock = BaseBlock.Instantiate(blockType, gridIndex);
        newBlock.transform.position = IndexToWorldPosition(gridIndex);

        // Register the block
        placedBlocks[gridIndex] = newBlock;

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

    public void MoveCursor(Vector2Int direction)
    {
        Vector2Int currentCoords = IndexToCoordinates(currentCursorIndex);
        Vector2Int newCoords = currentCoords + direction;

        // Clamp to grid bounds
        newCoords.x = Mathf.Clamp(newCoords.x, 0, gridWidth - 1);
        newCoords.y = Mathf.Clamp(newCoords.y, 0, gridHeight - 1);

        currentCursorIndex = CoordinatesToIndex(newCoords);
        UpdateCursorPosition();
        HighlightCurrentCursor();
    }

    public void SetCursorIndex(int index)
    {
        if (IsValidIndex(index))
        {
            currentCursorIndex = index;
            UpdateCursorPosition();
            HighlightCurrentCursor();
        }
    }

    private void UpdateCursorPosition()
    {
        if (cursorHighlight != null)
        {
            cursorHighlight.transform.position = IndexToWorldPosition(currentCursorIndex);
        }
    }

    private void HighlightCurrentCursor()
    {
        // Unhighlight all blocks first
        foreach (var block in placedBlocks.Values)
        {
            block.Unhighlight();
        }

        // Highlight block at cursor position if exists
        BaseBlock currentBlock = GetBlockAtIndex(currentCursorIndex);
        if (currentBlock != null)
        {
            currentBlock.Highlight();
        }
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
