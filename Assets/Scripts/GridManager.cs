using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Central manager for the grid system in the puzzle game.
///
/// ARCHITECTURE:
/// - Grid exists on XY plane (vertical wall), viewed from negative Z
/// - X axis = horizontal (left/right)
/// - Y axis = vertical (up/down)
/// - Z axis = depth (toward/away from camera, always 0 for grid)
///
/// RESPONSIBILITIES:
/// - Grid coordinate conversions (index ↔ coordinates ↔ world position)
/// - Block placement and removal
/// - Lem (character) placement and tracking
/// - Placeable space management (which grid cells can have blocks)
/// - Cursor management for editor
/// - Save/load orchestration
/// - Transporter path conflict detection
///
/// DESIGN PATTERN: Singleton (accessed via GridManager.Instance)
///
/// NOTE: This class is large (943 lines) and could benefit from refactoring into:
/// - GridStateManager (coordinates, conversions)
/// - BlockPlacementManager (block operations)
/// - LemManager (Lem tracking)
/// - GridVisualsManager (cursor, visualization)
/// </summary>
public class GridManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance. Initialized in Awake(), destroyed if duplicate exists.
    /// </summary>
    public static GridManager Instance { get; private set; }

    // Cached component references to avoid FindObjectOfType calls
    private static CameraSetup _cachedCameraSetup;
    private static PlaceableSpaceVisualizer _cachedPlaceableVisualizer;
    private static GridVisualizer _cachedGridVisualizer;

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;

    [Header("Grid Origin (Auto-Calculated)")]
    [SerializeField] private Vector3 _gridOrigin = Vector3.zero;

    /// <summary>
    /// The calculated origin point of the grid (bottom-left corner).
    /// Auto-calculated to center grid at world origin.
    /// </summary>
    public Vector3 gridOrigin => _gridOrigin;

    [Header("Cursor Settings")]
    public int currentCursorIndex = 0;
    public GameObject cursorHighlightPrefab;
    private GameObject cursorHighlight;
    private GridCursor gridCursor;

    [Header("Placeable Spaces")]
    private bool[] placeableSpaces;

    private Dictionary<int, BaseBlock> placedBlocks = new Dictionary<int, BaseBlock>();
    private Dictionary<int, BaseBlock> permanentBlocks = new Dictionary<int, BaseBlock>();
    private Dictionary<int, GameObject> placedLems = new Dictionary<int, GameObject>();
    private Dictionary<int, LemPlacementData> originalLemPlacements = new Dictionary<int, LemPlacementData>();

    /// <summary>
    /// Stores original Lem placement data for resetting after Play mode
    /// </summary>
    private class LemPlacementData
    {
        public int gridIndex;
        public bool facingRight;

        public LemPlacementData(int index, bool facing)
        {
            gridIndex = index;
            facingRight = facing;
        }
    }

    /// <summary>
    /// Initializes the singleton instance and sets up the grid system.
    /// Enforces singleton pattern by destroying duplicates.
    /// </summary>
    private void Awake()
    {
        // Enforce singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GridManager] Singleton instance initialized");
        }
        else
        {
            Debug.LogWarning($"[GridManager] Duplicate GridManager detected on '{gameObject.name}'. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        try
        {
            // Calculate grid origin to center grid around world origin (0,0,0)
            CalculateGridOrigin();

            // Initialize the placeable spaces array
            InitializePlaceableSpaces();

            // Create the visual cursor for editor mode
            CreateCursor();

            Debug.Log($"[GridManager] Initialization complete: {gridWidth}x{gridHeight} grid, cell size {cellSize}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Failed to initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Calculates grid origin so the grid is perfectly centered around world origin (0,0,0).
    /// This ensures the camera can be positioned at (0,0,-Z) for perfect centering.
    /// </summary>
    private void CalculateGridOrigin()
    {
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize;

        // Position grid so its center is at world origin
        _gridOrigin = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0);

        Debug.Log($"GridManager: Auto-calculated gridOrigin = {_gridOrigin} for {gridWidth}x{gridHeight} grid");
    }

    /// <summary>
    /// Performs additional setup after all Awake() methods have executed.
    /// Ensures camera is properly centered on the grid.
    /// </summary>
    private void Start()
    {
        try
        {
            // Cache CameraSetup reference to avoid repeated FindObjectOfType calls
            if (_cachedCameraSetup == null)
            {
                _cachedCameraSetup = UnityEngine.Object.FindObjectOfType<CameraSetup>();
            }

            if (_cachedCameraSetup != null)
            {
                _cachedCameraSetup.SetupCamera();
                Debug.Log("[GridManager] Camera setup complete");
            }
            else
            {
                Debug.LogWarning("[GridManager] No CameraSetup found in scene. Camera may not be positioned correctly.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Error in Start(): {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void InitializePlaceableSpaces()
    {
        int totalSpaces = gridWidth * gridHeight;
        placeableSpaces = new bool[totalSpaces];
        // All spaces start as NOT placeable (false is default)
        // Player must explicitly mark spaces as placeable in edit mode
    }

    #region Grid Coordinate Conversion

    /// <summary>
    /// Converts grid index to world position on XY plane.
    /// </summary>
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

    /// <summary>
    /// Converts grid coordinates to world position.
    /// Grid is on XY plane: X = horizontal, Y = vertical, Z = 0
    /// </summary>
    public Vector3 CoordinatesToWorldPosition(Vector2Int coords)
    {
        float halfCell = cellSize * 0.5f;
        return gridOrigin + new Vector3(
            (coords.x * cellSize) + halfCell,  // X = horizontal
            (coords.y * cellSize) + halfCell,  // Y = vertical
            0                                   // Z = 0 (on the wall)
        );
    }

    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < (gridWidth * gridHeight);
    }

    public bool IsValidCoordinates(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// Converts world position to grid index.
    /// </summary>
    public int WorldToGridIndex(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridOrigin;
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.y / cellSize);

        if (!IsValidCoordinates(x, y))
            return -1;

        return CoordinatesToIndex(x, y);
    }

    #endregion

    #region Block Placement and Management

    /// <summary>
    /// Places a block on the grid at the specified index.
    /// Performs extensive validation before placement:
    /// - Checks if index is valid
    /// - Checks if space is placeable
    /// - Checks for transporter path conflicts
    /// - Checks for permanent blocks
    /// - Checks inventory availability
    /// </summary>
    /// <param name="blockType">Type of block to place</param>
    /// <param name="gridIndex">Grid index where block should be placed</param>
    /// <returns>The placed block, or null if placement failed</returns>
    public BaseBlock PlaceBlock(BlockType blockType, int gridIndex)
    {
        try
        {
            // === VALIDATION CHECKS ===

            // Check 1: Valid grid index
            if (!IsValidIndex(gridIndex))
            {
                Debug.LogWarning($"[GridManager] Cannot place block: Invalid grid index {gridIndex}");
                return null;
            }

            // Check 2: Transporter path conflict
            if (IsIndexBlockedByTransporterPath(gridIndex))
            {
                Debug.LogWarning($"[GridManager] Cannot place {blockType} at index {gridIndex}: Transporter path reserved");
                return null;
            }

            // Check 3: Space must be marked as placeable
            if (!IsSpacePlaceable(gridIndex))
            {
                Debug.LogWarning($"[GridManager] Cannot place {blockType} at index {gridIndex}: Space not marked as placeable");
                return null;
            }

            // Check 4: No permanent blocks
            if (IsPermanentBlockAtIndex(gridIndex))
            {
                Debug.LogWarning($"[GridManager] Cannot place {blockType} at index {gridIndex}: Permanent block present");
                return null;
            }

            // Check 5: Inventory availability
            // Use cached inventory reference
            BlockInventory inventory = UnityEngine.Object.FindObjectOfType<BlockInventory>();
            if (inventory != null && !inventory.CanPlaceBlock(blockType))
            {
                Debug.LogWarning($"[GridManager] Cannot place {blockType}: No blocks remaining in inventory");
                return null;
            }

            // === PLACEMENT LOGIC ===

            // If there's already a non-permanent block here, destroy it
            if (placedBlocks.ContainsKey(gridIndex))
            {
                Debug.Log($"[GridManager] Replacing existing block at index {gridIndex}");
                placedBlocks[gridIndex].DestroyBlock();
            }

            // Consume block from inventory
            if (inventory != null)
            {
                inventory.UseBlock(blockType);
            }

            // Create and position the new block
            BaseBlock newBlock = BaseBlock.Instantiate(blockType, gridIndex);
            if (newBlock == null)
            {
                Debug.LogError($"[GridManager] Failed to instantiate {blockType} block at index {gridIndex}");
                // Return the block to inventory since placement failed
                if (inventory != null)
                {
                    inventory.ReturnBlock(blockType);
                }
                return null;
            }

            newBlock.isPermanent = false;
            PositionBlock(newBlock, gridIndex);

            // Register the new block
            placedBlocks[gridIndex] = newBlock;
            UpdateCursorState();

            Debug.Log($"[GridManager] Successfully placed {blockType} block at index {gridIndex}");
            return newBlock;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Error placing {blockType} block at index {gridIndex}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    public BaseBlock PlacePermanentBlock(BlockType blockType, int gridIndex)
    {
        if (!IsValidIndex(gridIndex))
        {
            Debug.LogWarning($"Invalid grid index: {gridIndex}");
            return null;
        }

        if (IsIndexBlockedByTransporterPath(gridIndex))
        {
            Debug.LogWarning($"Cannot place permanent block at index {gridIndex}: transporter path reserved");
            return null;
        }

        BaseBlock existingBlock = GetBlockAtIndex(gridIndex);
        if (existingBlock != null)
        {
            existingBlock.DestroyBlock();
        }

        BaseBlock newBlock = BaseBlock.Instantiate(blockType, gridIndex);
        newBlock.isPermanent = true;
        PositionBlock(newBlock, gridIndex);

        permanentBlocks[gridIndex] = newBlock;
        SetSpacePlaceable(gridIndex, false);

        return newBlock;
    }

    private void PositionBlock(BaseBlock block, int gridIndex)
    {
        Vector3 position = IndexToWorldPosition(gridIndex);
        position.z += cellSize * 0.5f; // Center block so front face is flush with grid (z=0)
        block.transform.position = position;
        block.transform.localScale = Vector3.one * cellSize;
    }

    public void RegisterBlock(BaseBlock block)
    {
        if (IsValidIndex(block.gridIndex))
        {
            if (block.isPermanent)
            {
                permanentBlocks[block.gridIndex] = block;
            }
            else
            {
                placedBlocks[block.gridIndex] = block;
            }
        }
    }

    public void UnregisterBlock(BaseBlock block)
    {
        placedBlocks.Remove(block.gridIndex);
        permanentBlocks.Remove(block.gridIndex);
        UpdateCursorState();
    }

    public BaseBlock GetBlockAtIndex(int index)
    {
        if (permanentBlocks.TryGetValue(index, out BaseBlock permanentBlock))
        {
            return permanentBlock;
        }
        return placedBlocks.TryGetValue(index, out BaseBlock block) ? block : null;
    }

    public bool IsPermanentBlockAtIndex(int index)
    {
        return permanentBlocks.ContainsKey(index);
    }

    public bool IsGridSpaceOccupied(int index)
    {
        return placedBlocks.ContainsKey(index) || permanentBlocks.ContainsKey(index);
    }

    #endregion

    #region Lem Placement

    /// <summary>
    /// Places a Lem at the specified grid index.
    /// If a Lem already exists there, turns it around instead.
    /// </summary>
    public GameObject PlaceLem(int gridIndex)
    {
        if (!IsValidIndex(gridIndex))
        {
            Debug.LogWarning($"Invalid grid index: {gridIndex}");
            return null;
        }

        // If Lem already exists at this position, turn it around
        if (placedLems.TryGetValue(gridIndex, out GameObject existingLem))
        {
            LemController lemController = existingLem.GetComponent<LemController>();
            if (lemController != null)
            {
                lemController.TurnAround();
                // Update original placement data with new facing direction
                originalLemPlacements[gridIndex] = new LemPlacementData(gridIndex, lemController.GetFacingRight());
                Debug.Log($"Turned Lem around at index {gridIndex}");
            }
            return existingLem;
        }

        // Only one Lem allowed - remove any existing Lem first
        if (placedLems.Count > 0)
        {
            ClearAllLems();
        }

        // Create new Lem - place on top of block if one exists, otherwise at grid position
        Vector3 position = IndexToWorldPosition(gridIndex);
        position.z += cellSize * 0.5f;

        // Check if there's a block at this position
        BaseBlock blockBelow = GetBlockAtIndex(gridIndex);
        if (blockBelow != null)
        {
            // Place Lem on top of the block
            // Block center is at blockBelow.transform.position
            // Block extends cellSize/2 above and below center
            // Place Lem slightly above block top to avoid clipping into collider
            position = blockBelow.transform.position;
            position.y += cellSize / 2f + 0.1f; // Block top surface + small offset
        }

        GameObject lem = LemController.CreateLem(position);
        placedLems[gridIndex] = lem;

        // Store original placement data for resetting after Play mode
        LemController controller = lem.GetComponent<LemController>();
        if (controller != null)
        {
            originalLemPlacements[gridIndex] = new LemPlacementData(gridIndex, controller.GetFacingRight());
        }

        return lem;
    }

    /// <summary>
    /// Removes the Lem at the specified grid index.
    /// </summary>
    public void RemoveLem(int gridIndex)
    {
        if (placedLems.TryGetValue(gridIndex, out GameObject lem))
        {
            Destroy(lem);
            placedLems.Remove(gridIndex);
            originalLemPlacements.Remove(gridIndex);
            Debug.Log($"Removed Lem at index {gridIndex}");
        }
    }

    /// <summary>
    /// Resets all Lems to their original placement positions and directions.
    /// Called when exiting Play mode to restore Lems to Level Editor state.
    /// </summary>
    public void ResetAllLems()
    {
        // Destroy all current Lems
        foreach (var lem in placedLems.Values)
        {
            if (lem != null)
            {
                Destroy(lem);
            }
        }
        placedLems.Clear();

        // Recreate Lems at original positions
        foreach (var placementData in originalLemPlacements.Values)
        {
            Vector3 position = IndexToWorldPosition(placementData.gridIndex);
            position.z += cellSize * 0.5f;

            // Check if there's a block at this position
            BaseBlock blockBelow = GetBlockAtIndex(placementData.gridIndex);
            if (blockBelow != null)
            {
                position = blockBelow.transform.position;
                position.y += cellSize / 2f + 0.1f;
            }

            GameObject lem = LemController.CreateLem(position);
            LemController controller = lem.GetComponent<LemController>();
            if (controller != null)
            {
                controller.SetFacingRight(placementData.facingRight);
                controller.SetFrozen(true); // Frozen in editor mode
            }

            placedLems[placementData.gridIndex] = lem;
        }

        Debug.Log($"Reset {originalLemPlacements.Count} Lem(s) to original positions");
    }

    /// <summary>
    /// Checks if there is a Lem at the specified grid index.
    /// </summary>
    public bool HasLemAtIndex(int index)
    {
        return placedLems.ContainsKey(index);
    }

    /// <summary>
    /// Gets the Lem at the specified grid index, if any.
    /// </summary>
    public GameObject GetLemAtIndex(int index)
    {
        return placedLems.TryGetValue(index, out GameObject lem) ? lem : null;
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
            cursorHighlight = new GameObject("GridCursor");
            gridCursor = cursorHighlight.AddComponent<GridCursor>();
        }

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

    public void SetCursorVisible(bool visible)
    {
        if (gridCursor != null)
        {
            gridCursor.SetVisible(visible);
        }
    }

    private void UpdateCursorState()
    {
        if (gridCursor == null) return;

        bool isPlaceable = IsSpacePlaceable(currentCursorIndex);
        bool hasBlock = IsGridSpaceOccupied(currentCursorIndex);

        if (hasBlock)
        {
            gridCursor.SetState(GridCursor.CursorState.Editable);
        }
        else if (!isPlaceable)
        {
            gridCursor.SetState(GridCursor.CursorState.NonPlaceable);
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
        if (IsPermanentBlockAtIndex(index)) return false;
        return placeableSpaces[index];
    }

    public void SetSpacePlaceable(int index, bool placeable)
    {
        if (IsValidIndex(index))
        {
            if (placeable && IsPermanentBlockAtIndex(index))
            {
                Debug.LogWarning($"Cannot mark index {index} as placeable: permanent block present");
                return;
            }
            placeableSpaces[index] = placeable;
            UpdateCursorState();

            PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
            if (visualizer != null)
            {
                visualizer.UpdateMarkerAtIndex(index);
            }
        }
    }

    public void SetSpacePlaceable(int x, int y, bool placeable)
    {
        SetSpacePlaceable(CoordinatesToIndex(x, y), placeable);
    }

    public void ToggleSpacePlaceable(int index)
    {
        if (IsValidIndex(index))
        {
            if (IsPermanentBlockAtIndex(index))
            {
                Debug.LogWarning($"Cannot toggle placeable at index {index}: permanent block present");
                return;
            }
            placeableSpaces[index] = !placeableSpaces[index];
            UpdateCursorState();

            PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
            if (visualizer != null)
            {
                visualizer.UpdateMarkerAtIndex(index);
            }
        }
    }

    #endregion

    #region Grid Refresh

    /// <summary>
    /// Refreshes all grid visualizations and camera positioning.
    /// Call this after changing grid dimensions or structure.
    /// </summary>
    public void RefreshGrid()
    {
        try
        {
            // Cache component references to avoid repeated GetComponent/FindObjectOfType calls
            if (_cachedGridVisualizer == null)
            {
                _cachedGridVisualizer = GetComponent<GridVisualizer>();
            }

            if (_cachedPlaceableVisualizer == null)
            {
                _cachedPlaceableVisualizer = GetComponent<PlaceableSpaceVisualizer>();
            }

            if (_cachedCameraSetup == null)
            {
                _cachedCameraSetup = UnityEngine.Object.FindObjectOfType<CameraSetup>();
            }

            // Refresh grid lines
            if (_cachedGridVisualizer != null)
            {
                _cachedGridVisualizer.RefreshGrid();
            }
            else
            {
                Debug.LogWarning("[GridManager] No GridVisualizer component found");
            }

            // Refresh placeable space markers
            if (_cachedPlaceableVisualizer != null)
            {
                _cachedPlaceableVisualizer.RefreshVisuals();
            }
            else
            {
                Debug.LogWarning("[GridManager] No PlaceableSpaceVisualizer component found");
            }

            // Refresh camera positioning
            if (_cachedCameraSetup != null)
            {
                _cachedCameraSetup.RefreshCamera();
            }
            else
            {
                Debug.LogWarning("[GridManager] No CameraSetup found in scene");
            }

            Debug.Log($"[GridManager] Grid refreshed: {gridWidth}x{gridHeight}, cell size {cellSize}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Error refreshing grid: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    #region Save/Load System

    /// <summary>
    /// Creates a LevelData object from the current grid state.
    /// </summary>
    public LevelData CaptureLevelData()
    {
        LevelData levelData = new LevelData
        {
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            cellSize = cellSize
        };

        // Capture permanent blocks
        foreach (var kvp in permanentBlocks)
        {
            if (kvp.Value != null)
            {
                levelData.permanentBlocks.Add(CreateBlockData(kvp.Value, kvp.Key));
            }
        }

        // Capture all placed blocks
        foreach (var kvp in placedBlocks)
        {
            if (kvp.Value != null)
            {
                levelData.blocks.Add(CreateBlockData(kvp.Value, kvp.Key));
            }
        }

        // Capture placeable spaces (only store indices where placeable = true)
        for (int i = 0; i < placeableSpaces.Length; i++)
        {
            if (placeableSpaces[i] && !IsPermanentBlockAtIndex(i))
            {
                levelData.placeableSpaceIndices.Add(i);
            }
        }

        // Capture Lem placements
        foreach (var kvp in originalLemPlacements)
        {
            levelData.lems.Add(new LevelData.LemData(kvp.Value.gridIndex, kvp.Value.facingRight));
        }

        Debug.Log($"Captured level data: {levelData.permanentBlocks.Count} permanent blocks, {levelData.blocks.Count} blocks, {levelData.placeableSpaceIndices.Count} placeable spaces, {levelData.lems.Count} Lems");
        return levelData;
    }

    /// <summary>
    /// Restores grid state from a LevelData object.
    /// Clears existing state before loading.
    /// </summary>
    public void RestoreLevelData(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("Cannot restore null level data");
            return;
        }

        // Clear existing state
        ClearAllBlocks();
        ClearAllLems();
        ClearPlaceableSpaces();

        // Restore grid settings (if they changed, we need to reinitialize)
        bool gridSizeChanged = (gridWidth != levelData.gridWidth || gridHeight != levelData.gridHeight || cellSize != levelData.cellSize);

        if (gridSizeChanged)
        {
            gridWidth = levelData.gridWidth;
            gridHeight = levelData.gridHeight;
            cellSize = levelData.cellSize;
            CalculateGridOrigin();
            InitializePlaceableSpaces();
            RefreshGrid();
        }

        // Restore placeable spaces
        if (levelData.placeableSpaceIndices != null)
        {
            foreach (int index in levelData.placeableSpaceIndices)
            {
                if (IsValidIndex(index))
                {
                    placeableSpaces[index] = true;
                }
            }
        }

        // Restore permanent blocks
        if (levelData.permanentBlocks != null)
        {
            foreach (var blockData in levelData.permanentBlocks)
            {
                if (IsValidIndex(blockData.gridIndex))
                {
                    BaseBlock block = PlacePermanentBlock(blockData.blockType, blockData.gridIndex);
                    ApplyBlockData(block, blockData);
                }
            }
        }

        // Restore blocks
        if (levelData.blocks != null)
        {
            foreach (var blockData in levelData.blocks)
            {
                if (IsValidIndex(blockData.gridIndex))
                {
                    // Temporarily set space as placeable to allow block placement
                    bool wasPlaceable = placeableSpaces[blockData.gridIndex];
                    placeableSpaces[blockData.gridIndex] = true;

                    BaseBlock block = PlaceBlock(blockData.blockType, blockData.gridIndex);
                    ApplyBlockData(block, blockData);

                    // Restore original placeable state
                    placeableSpaces[blockData.gridIndex] = wasPlaceable;
                }
            }
        }

        // Restore Lems
        if (levelData.lems != null)
        {
            bool lemPlaced = false;
            foreach (var lemData in levelData.lems)
            {
                if (!lemPlaced && IsValidIndex(lemData.gridIndex))
                {
                    GameObject lem = PlaceLem(lemData.gridIndex);
                    if (lem != null)
                    {
                        LemController controller = lem.GetComponent<LemController>();
                        if (controller != null)
                        {
                            controller.SetFacingRight(lemData.facingRight);
                            controller.SetFrozen(true); // Start frozen in editor mode
                        }
                        lemPlaced = true;
                    }
                }
            }
        }

        // Refresh visuals
        PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
        if (visualizer != null)
        {
            visualizer.RefreshVisuals();
        }

        UpdateCursorState();

        Debug.Log($"Restored level data: {levelData.permanentBlocks?.Count ?? 0} permanent blocks, {levelData.blocks?.Count ?? 0} blocks, {levelData.placeableSpaceIndices?.Count ?? 0} placeable spaces, {levelData.lems?.Count ?? 0} Lems");
    }

    /// <summary>
    /// Saves the current level to a file.
    /// </summary>
    public bool SaveLevel(string levelName = null)
    {
        LevelData levelData = CaptureLevelData();
        return LevelSaveSystem.SaveLevel(levelData, levelName);
    }

    /// <summary>
    /// Loads a level from a file.
    /// </summary>
    public bool LoadLevel(string levelName = null)
    {
        LevelData levelData = LevelSaveSystem.LoadLevel(levelName);
        if (levelData != null)
        {
            RestoreLevelData(levelData);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all placed blocks.
    /// </summary>
    private void ClearAllBlocks()
    {
        // Create a copy of the keys to avoid modification during iteration
        var indices = new System.Collections.Generic.List<int>(placedBlocks.Keys);
        indices.AddRange(permanentBlocks.Keys);
        foreach (int index in indices)
        {
            BaseBlock block = GetBlockAtIndex(index);
            if (block != null)
            {
                Destroy(block.gameObject);
            }
        }
        placedBlocks.Clear();
        permanentBlocks.Clear();
    }

    /// <summary>
    /// Clears all placed Lems.
    /// </summary>
    private void ClearAllLems()
    {
        foreach (var lem in placedLems.Values)
        {
            if (lem != null)
            {
                Destroy(lem);
            }
        }
        placedLems.Clear();
        originalLemPlacements.Clear();
    }

    /// <summary>
    /// Clears all placeable space markers.
    /// </summary>
    private void ClearPlaceableSpaces()
    {
        for (int i = 0; i < placeableSpaces.Length; i++)
        {
            placeableSpaces[i] = false;
        }
    }

    #endregion

    public bool HasTransporterConflicts()
    {
        TransporterBlock[] transporters = FindObjectsOfType<TransporterBlock>();
        foreach (TransporterBlock transporter in transporters)
        {
            if (transporter == null) continue;
            List<int> pathIndices = transporter.GetRoutePathIndices();
            foreach (int index in pathIndices)
            {
                if (!IsValidIndex(index)) continue;
                BaseBlock block = GetBlockAtIndex(index);
                if (block != null && block != transporter)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsIndexBlockedByTransporterPath(int index)
    {
        TransporterBlock[] transporters = FindObjectsOfType<TransporterBlock>();
        foreach (TransporterBlock transporter in transporters)
        {
            if (transporter == null) continue;
            List<int> pathIndices = transporter.GetRoutePathIndices();
            if (pathIndices.Contains(index))
            {
                return true;
            }
        }
        return false;
    }

    private static LevelData.BlockData CreateBlockData(BaseBlock block, int index)
    {
        LevelData.BlockData data = new LevelData.BlockData(block.blockType, index);
        TransporterBlock transporter = block as TransporterBlock;
        if (transporter != null && transporter.routeSteps != null)
        {
            data.routeSteps = (string[])transporter.routeSteps.Clone();
        }
        return data;
    }

    private static void ApplyBlockData(BaseBlock block, LevelData.BlockData data)
    {
        if (block == null || data == null) return;
        TransporterBlock transporter = block as TransporterBlock;
        if (transporter != null && data.routeSteps != null)
        {
            transporter.routeSteps = (string[])data.routeSteps.Clone();
        }
    }

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        // Draw horizontal lines (along X axis)
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = gridOrigin + new Vector3(0, y * cellSize, 0);
            Vector3 end = gridOrigin + new Vector3(gridWidth * cellSize, y * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw vertical lines (along Y axis)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0, 0);
            Vector3 end = gridOrigin + new Vector3(x * cellSize, gridHeight * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw cursor position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 cursorPos = IndexToWorldPosition(currentCursorIndex);
            cursorPos.z += cellSize * 0.5f; // Visualize block volume at placement depth
            Gizmos.DrawWireCube(cursorPos, Vector3.one * cellSize);
        }
    }

    #endregion
}
