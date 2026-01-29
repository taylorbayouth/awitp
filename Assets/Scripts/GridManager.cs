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
    private List<LevelData.KeyStateData> originalKeyStates = new List<LevelData.KeyStateData>();
    private LevelData playModeSnapshot;

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
            DebugLog.Info("[GridManager] Singleton instance initialized");
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

            DebugLog.Info($"[GridManager] Initialization complete: {gridWidth}x{gridHeight} grid, cell size {cellSize}");
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

        DebugLog.Info($"GridManager: Auto-calculated gridOrigin = {_gridOrigin} for {gridWidth}x{gridHeight} grid");
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
                _cachedCameraSetup = UnityEngine.Object.FindAnyObjectByType<CameraSetup>();
            }

            if (_cachedCameraSetup != null)
            {
                _cachedCameraSetup.SetupCamera();
                DebugLog.Info("[GridManager] Camera setup complete");
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
    public BaseBlock PlaceBlock(BlockType blockType, int gridIndex, BlockInventoryEntry entry = null)
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

            // Check 2b: Transporter-specific placement constraints
            if (blockType == BlockType.Transporter)
            {
                if (IsGridSpaceOccupied(gridIndex))
                {
                    Debug.LogWarning($"[GridManager] Cannot place Transporter at index {gridIndex}: Space already occupied");
                    return null;
                }

                BlockInventoryEntry resolvedForRoute = entry;
                if (resolvedForRoute == null)
                {
                    BlockInventory routeInventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
                    if (routeInventory != null)
                    {
                        resolvedForRoute = routeInventory.GetDefaultEntryForBlockType(blockType);
                    }
                }

                string[] routeSteps = resolvedForRoute != null ? resolvedForRoute.routeSteps : null;

                // Check if route goes outside grid bounds
                if (!IsTransporterRouteWithinBounds(gridIndex, routeSteps))
                {
                    Debug.LogWarning($"[GridManager] Cannot place Transporter at index {gridIndex}: Route path goes outside grid bounds");
                    return null;
                }

                List<int> routeIndices = BuildTransporterPathIndices(gridIndex, routeSteps);
                if (routeIndices.Count > 0 && HasBlocksOnIndices(routeIndices))
                {
                    Debug.LogWarning($"[GridManager] Cannot place Transporter at index {gridIndex}: Route path blocked by existing block");
                    return null;
                }
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
            BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
            BlockInventoryEntry resolvedEntry = entry;
            if (inventory != null && resolvedEntry == null)
            {
                resolvedEntry = inventory.GetDefaultEntryForBlockType(blockType);
            }

            if (inventory != null && resolvedEntry == null)
            {
                Debug.LogWarning($"[GridManager] Cannot place {blockType}: No inventory entry configured");
                return null;
            }

            if (inventory != null && resolvedEntry != null && !inventory.CanPlaceEntry(resolvedEntry))
            {
                Debug.LogWarning($"[GridManager] Cannot place {resolvedEntry.GetDisplayName()}: No blocks remaining in inventory");
                return null;
            }

            // === PLACEMENT LOGIC ===

            // If there's already a non-permanent block here, destroy it
            if (placedBlocks.ContainsKey(gridIndex))
            {
                placedBlocks[gridIndex].DestroyBlock();
            }

            // Consume block from inventory
            if (inventory != null)
            {
                if (resolvedEntry != null)
                {
                    inventory.UseBlock(resolvedEntry);
                }
                else
                {
                    inventory.UseBlock(blockType);
                }
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
            ApplyEntryMetadata(newBlock, resolvedEntry, inventory);
            PositionBlock(newBlock, gridIndex);

            // Register the new block
            placedBlocks[gridIndex] = newBlock;
            UpdateCursorState();

            return newBlock;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Error placing {blockType} block at index {gridIndex}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    public BaseBlock PlaceBlock(BlockInventoryEntry entry, int gridIndex)
    {
        if (entry == null)
        {
            Debug.LogWarning("[GridManager] Cannot place block: inventory entry is null");
            return null;
        }

        return PlaceBlock(entry.blockType, gridIndex, entry);
    }

    public BaseBlock PlacePermanentBlock(BlockType blockType, int gridIndex, BlockInventoryEntry entry = null)
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
        ApplyEntryMetadata(newBlock, entry, UnityEngine.Object.FindAnyObjectByType<BlockInventory>());
        PositionBlock(newBlock, gridIndex);

        permanentBlocks[gridIndex] = newBlock;
        SetSpacePlaceable(gridIndex, false);

        return newBlock;
    }

    public BaseBlock PlacePermanentBlock(BlockInventoryEntry entry, int gridIndex)
    {
        if (entry == null)
        {
            Debug.LogWarning("[GridManager] Cannot place permanent block: inventory entry is null");
            return null;
        }

        return PlacePermanentBlock(entry.blockType, gridIndex, entry);
    }

    private void PositionBlock(BaseBlock block, int gridIndex)
    {
        Vector3 position = IndexToWorldPosition(gridIndex);
        position.z += cellSize * 0.5f; // Center block so front face is flush with grid (z=0)
        block.transform.position = position;
        block.transform.localScale = Vector3.one * cellSize;
    }

    private static void ApplyEntryMetadata(BaseBlock block, BlockInventoryEntry entry, BlockInventory inventory)
    {
        if (block == null || entry == null) return;

        string inventoryKey = inventory != null ? inventory.GetInventoryKey(entry) : entry.GetEntryId();
        block.inventoryKey = inventoryKey;
        block.flavorId = entry.GetResolvedFlavorId();

        TransporterBlock transporter = block as TransporterBlock;
        if (transporter != null && entry.routeSteps != null && entry.routeSteps.Length > 0)
        {
            transporter.routeSteps = (string[])entry.routeSteps.Clone();
        }
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
            }
            return existingLem;
        }

        // Only one Lem allowed - remove any existing Lem first
        if (placedLems.Count > 0)
        {
            ClearAllLems();
        }

        Vector3 footPosition = GetLemFootPositionForIndex(gridIndex);

        GameObject lem = LemController.CreateLem(footPosition);
        placedLems[gridIndex] = lem;

        // Store original placement data for resetting after Play mode
        LemController controller = lem.GetComponent<LemController>();
        if (controller != null)
        {
            controller.SetFootPointPosition(footPosition);
            controller.SetFrozen(true);
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
            Vector3 footPosition = GetLemFootPositionForIndex(placementData.gridIndex);

            GameObject lem = LemController.CreateLem(footPosition);
            LemController controller = lem.GetComponent<LemController>();
            if (controller != null)
            {
                controller.SetFootPointPosition(footPosition);
                controller.SetFacingRight(placementData.facingRight);
                controller.SetFrozen(true); // Frozen in editor mode
            }

            placedLems[placementData.gridIndex] = lem;
        }

        DebugLog.Info($"Reset {originalLemPlacements.Count} Lem(s) to original positions");
    }

    public void CaptureOriginalKeyStates()
    {
        originalKeyStates = CaptureKeyStates();
    }

    public void RestoreOriginalKeyStates()
    {
        ApplyKeyStates(originalKeyStates);
    }

    public void CapturePlayModeSnapshot()
    {
        playModeSnapshot = CaptureLevelData();
    }

    public void RestorePlayModeSnapshot()
    {
        if (playModeSnapshot == null)
        {
            Debug.LogWarning("[GridManager] No play mode snapshot found to restore.");
            return;
        }

        RestoreLevelData(playModeSnapshot);
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

    private Vector3 GetLemFootPositionForIndex(int gridIndex)
    {
        Vector2Int coords = IndexToCoordinates(gridIndex);
        float halfCell = cellSize * 0.5f;
        return gridOrigin + new Vector3(
            (coords.x * cellSize) + halfCell,
            coords.y * cellSize,
            0.5f
        );
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
        EnsurePlaceableSpacesSized();
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
            EnsurePlaceableSpacesSized();

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
                _cachedCameraSetup = UnityEngine.Object.FindAnyObjectByType<CameraSetup>();
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

            DebugLog.Info($"[GridManager] Grid refreshed: {gridWidth}x{gridHeight}, cell size {cellSize}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Error refreshing grid: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void EnsurePlaceableSpacesSized()
    {
        int expected = gridWidth * gridHeight;
        if (expected <= 0) return;
        if (placeableSpaces == null || placeableSpaces.Length != expected)
        {
            Debug.LogWarning("[GridManager] placeableSpaces size mismatch. Reinitializing to match grid dimensions.");
            InitializePlaceableSpaces();
        }
    }

    #endregion

    #region Save/Load System

    /// <summary>
    /// Creates a LevelData object from the current grid state.
    /// </summary>
    public LevelData CaptureLevelData(bool includePlacedBlocks = true, bool includeKeyStates = true)
    {
        LevelData levelData = new LevelData
        {
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            cellSize = cellSize
        };

        BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        if (inventory != null)
        {
            // Persist per-level inventory configuration so editor saves restore the same entries.
            levelData.inventoryEntries = new List<BlockInventoryEntry>();
            IReadOnlyList<BlockInventoryEntry> inventoryEntries = inventory.GetEntries();
            foreach (BlockInventoryEntry entry in inventoryEntries)
            {
                if (entry != null)
                {
                    levelData.inventoryEntries.Add(entry.Clone());
                }
            }
        }

        // Capture permanent blocks
        foreach (var kvp in permanentBlocks)
        {
            if (kvp.Value != null)
            {
                levelData.permanentBlocks.Add(CreateBlockData(kvp.Value, kvp.Key));
            }
        }

        if (includePlacedBlocks)
        {
            // Capture all placed blocks
            foreach (var kvp in placedBlocks)
            {
                if (kvp.Value != null)
                {
                    levelData.blocks.Add(CreateBlockData(kvp.Value, kvp.Key));
                }
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
            LevelData.LemData lemData = new LevelData.LemData(kvp.Value.gridIndex, kvp.Value.facingRight);
            if (placedLems.TryGetValue(kvp.Key, out GameObject lem) && lem != null)
            {
                lemData.worldPosition = lem.transform.position;
                lemData.hasWorldPosition = true;
            }
            else
            {
                lemData.worldPosition = IndexToWorldPosition(kvp.Value.gridIndex);
                lemData.hasWorldPosition = true;
            }
            levelData.lems.Add(lemData);
        }

        // Capture key states (runtime-only; skipped for designer saves)
        if (includeKeyStates)
        {
            levelData.keyStates = CaptureKeyStates();
        }

        // Capture camera settings
        CameraSetup cameraSetup = UnityEngine.Object.FindAnyObjectByType<CameraSetup>();
        if (cameraSetup != null)
        {
            levelData.cameraSettings = cameraSetup.ExportSettings();
        }

        DebugLog.Info($"Captured level data: {levelData.permanentBlocks.Count} permanent blocks, {levelData.blocks.Count} blocks, {levelData.placeableSpaceIndices.Count} placeable spaces, {levelData.lems.Count} Lems");
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

        BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        if (inventory != null)
        {
            if (levelData.inventoryEntries != null && levelData.inventoryEntries.Count > 0)
            {
                // Restore inventory entries before placing blocks so inventory keys resolve correctly.
                inventory.LoadInventoryEntries(levelData.inventoryEntries);
            }
            else
            {
                inventory.ResetInventory();
            }
        }

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
        else
        {
            EnsurePlaceableSpacesSized();
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
                    BlockInventoryEntry entry = inventory != null
                        ? inventory.FindEntry(blockData.blockType, blockData.flavorId, blockData.routeSteps, blockData.inventoryKey)
                        : null;
                    BaseBlock block = entry != null
                        ? PlacePermanentBlock(entry, blockData.gridIndex)
                        : PlacePermanentBlock(blockData.blockType, blockData.gridIndex);
                    ApplyBlockData(block, blockData, inventory);
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

                    BlockInventoryEntry entry = inventory != null
                        ? inventory.FindEntry(blockData.blockType, blockData.flavorId, blockData.routeSteps, blockData.inventoryKey)
                        : null;
                    BaseBlock block = entry != null
                        ? PlaceBlock(entry, blockData.gridIndex)
                        : PlaceBlock(blockData.blockType, blockData.gridIndex);
                    ApplyBlockData(block, blockData, inventory);

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
                        if (lemData.hasWorldPosition)
                        {
                            lem.transform.position = lemData.worldPosition;
                        }
                        lemPlaced = true;
                    }
                }
            }
        }

        // Restore key states
        ApplyKeyStates(levelData.keyStates);

        // Refresh visuals
        PlaceableSpaceVisualizer visualizer = GetComponent<PlaceableSpaceVisualizer>();
        if (visualizer != null)
        {
            visualizer.RefreshVisuals();
        }

        // Restore camera settings
        CameraSetup cameraSetup = UnityEngine.Object.FindAnyObjectByType<CameraSetup>();
        if (cameraSetup != null && levelData.cameraSettings != null)
        {
            cameraSetup.ImportSettings(levelData.cameraSettings);
        }
        else if (cameraSetup != null)
        {
            // No saved camera settings, refresh camera with current settings
            cameraSetup.RefreshCamera();
        }

        UpdateCursorState();

        DebugLog.Info($"Restored level data: {levelData.permanentBlocks?.Count ?? 0} permanent blocks, {levelData.blocks?.Count ?? 0} blocks, {levelData.placeableSpaceIndices?.Count ?? 0} placeable spaces, {levelData.lems?.Count ?? 0} Lems");
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
        TransporterBlock[] transporters = UnityEngine.Object.FindObjectsByType<TransporterBlock>(FindObjectsSortMode.None);
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
        // Delegate to the generic method for backward compatibility
        return IsIndexBlockedByAnyBlock(index, null);
    }

    /// <summary>
    /// Checks if any placed block claims this index via GetBlockedIndices().
    /// Used for placement validation - blocks can reserve grid spaces they don't occupy.
    /// </summary>
    /// <param name="index">Grid index to check</param>
    /// <param name="excludeBlock">Optional block to exclude from the check (e.g., the block being placed)</param>
    /// <returns>True if any block claims this index</returns>
    public bool IsIndexBlockedByAnyBlock(int index, BaseBlock excludeBlock)
    {
        // Check all placed blocks
        foreach (var kvp in placedBlocks)
        {
            BaseBlock block = kvp.Value;
            if (block == null || block == excludeBlock) continue;

            int[] blockedIndices = block.GetBlockedIndices();
            if (blockedIndices != null && System.Array.IndexOf(blockedIndices, index) >= 0)
            {
                return true;
            }
        }

        // Check permanent blocks too
        foreach (var kvp in permanentBlocks)
        {
            BaseBlock block = kvp.Value;
            if (block == null || block == excludeBlock) continue;

            int[] blockedIndices = block.GetBlockedIndices();
            if (blockedIndices != null && System.Array.IndexOf(blockedIndices, index) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if there is any block (placed or permanent) at the given index.
    /// </summary>
    /// <param name="index">Grid index to check</param>
    /// <returns>True if a block exists at this index</returns>
    public bool HasBlockAtIndex(int index)
    {
        return GetBlockAtIndex(index) != null;
    }

    /// <summary>
    /// Validates if a block can be placed at the given index using the block's own rules.
    /// </summary>
    /// <param name="block">The block to validate (or a template block for the type)</param>
    /// <param name="targetIndex">Where to place the block</param>
    /// <returns>True if placement is valid</returns>
    public bool ValidateBlockPlacement(BaseBlock block, int targetIndex)
    {
        if (block == null) return false;
        return block.CanBePlacedAt(targetIndex, this);
    }

    private bool HasBlocksOnIndices(List<int> indices)
    {
        foreach (int index in indices)
        {
            if (!IsValidIndex(index)) continue;
            if (GetBlockAtIndex(index) != null)
            {
                return true;
            }
        }
        return false;
    }

    private List<int> BuildTransporterPathIndices(int originIndex, string[] routeSteps)
    {
        List<int> indices = new List<int>();
        if (!IsValidIndex(originIndex))
        {
            return indices;
        }

        Vector2Int current = IndexToCoordinates(originIndex);
        List<Vector2Int> steps = BuildTransporterSteps(routeSteps);
        HashSet<int> unique = new HashSet<int>();

        unique.Add(originIndex);
        foreach (Vector2Int step in steps)
        {
            current += step;
            if (IsValidCoordinates(current.x, current.y))
            {
                int idx = CoordinatesToIndex(current);
                if (unique.Add(idx))
                {
                    indices.Add(idx);
                }
            }
        }

        indices.Insert(0, originIndex);
        return indices;
    }

    private static List<Vector2Int> BuildTransporterSteps(string[] routeSteps)
    {
        return RouteParser.ParseRouteSteps(routeSteps);
    }

    /// <summary>
    /// Checks if a transporter route stays within grid bounds for all steps.
    /// Returns false if any step would go outside the grid.
    /// </summary>
    private bool IsTransporterRouteWithinBounds(int originIndex, string[] routeSteps)
    {
        if (!IsValidIndex(originIndex))
        {
            return false;
        }

        if (routeSteps == null || routeSteps.Length == 0)
        {
            return true; // Empty route is valid
        }

        Vector2Int current = IndexToCoordinates(originIndex);
        List<Vector2Int> steps = BuildTransporterSteps(routeSteps);

        foreach (Vector2Int step in steps)
        {
            current += step;
            if (!IsValidCoordinates(current.x, current.y))
            {
                return false; // Step goes outside grid bounds
            }
        }

        return true; // All steps stay within bounds
    }

    private static LevelData.BlockData CreateBlockData(BaseBlock block, int index)
    {
        LevelData.BlockData data = new LevelData.BlockData(block.blockType, index);
        data.inventoryKey = block.inventoryKey;
        data.flavorId = block.flavorId;
        TransporterBlock transporter = block as TransporterBlock;
        if (transporter != null && transporter.routeSteps != null)
        {
            data.routeSteps = (string[])transporter.routeSteps.Clone();
        }
        return data;
    }

    private static void ApplyBlockData(BaseBlock block, LevelData.BlockData data, BlockInventory inventory)
    {
        if (block == null || data == null) return;
        block.flavorId = data.flavorId;
        TransporterBlock transporter = block as TransporterBlock;
        if (transporter != null && data.routeSteps != null)
        {
            transporter.routeSteps = (string[])data.routeSteps.Clone();
        }

        if (block.blockType == BlockType.Transporter && inventory != null)
        {
            BlockInventoryEntry entry = inventory.FindEntry(block.blockType, block.flavorId, transporter != null ? transporter.routeSteps : data.routeSteps, data.inventoryKey);
            if (entry != null)
            {
                block.inventoryKey = inventory.GetInventoryKey(entry);
                return;
            }
        }

        block.inventoryKey = data.inventoryKey;
    }

    private List<LevelData.KeyStateData> CaptureKeyStates()
    {
        List<LevelData.KeyStateData> states = new List<LevelData.KeyStateData>();
        KeyItem[] keys = UnityEngine.Object.FindObjectsByType<KeyItem>(FindObjectsSortMode.None);
        foreach (KeyItem key in keys)
        {
            if (key == null) continue;

            LevelData.KeyStateData data = new LevelData.KeyStateData
            {
                sourceKeyBlockIndex = key.SourceKeyBlockIndex,
                location = LevelData.KeyLocation.World
            };

            if (key.IsLockedToBlock)
            {
                LockBlock lockBlock = key.GetComponentInParent<LockBlock>();
                if (lockBlock != null)
                {
                    data.location = LevelData.KeyLocation.LockBlock;
                    data.targetIndex = lockBlock.gridIndex;
                }
            }
            else if (key.IsHeldByLem)
            {
                LemController lem = key.GetComponentInParent<LemController>();
                if (lem != null)
                {
                    int lemIndex = WorldToGridIndex(lem.transform.position);
                    data.location = LevelData.KeyLocation.Lem;
                    data.targetIndex = lemIndex;
                }
            }
            else
            {
                KeyBlock keyBlock = key.GetComponentInParent<KeyBlock>();
                if (keyBlock != null)
                {
                    data.location = LevelData.KeyLocation.KeyBlock;
                    data.targetIndex = keyBlock.gridIndex;
                }
            }

            if (data.location == LevelData.KeyLocation.World)
            {
                data.hasWorldPosition = true;
                data.worldPosition = key.transform.position;
            }

            states.Add(data);
        }
        return states;
    }

    private void ApplyKeyStates(List<LevelData.KeyStateData> states)
    {
        if (states == null) return;

        foreach (LevelData.KeyStateData state in states)
        {
            if (state == null) continue;

            KeyItem key = FindKeyBySourceIndex(state.sourceKeyBlockIndex);
            if (key == null)
            {
                continue;
            }

            float size = cellSize;

            switch (state.location)
            {
                case LevelData.KeyLocation.LockBlock:
                {
                    BaseBlock block = GetBlockAtIndex(state.targetIndex);
                    LockBlock lockBlock = block as LockBlock;
                    if (lockBlock != null)
                    {
                        lockBlock.AttachKeyFromState(key);
                    }
                    break;
                }
                case LevelData.KeyLocation.Lem:
                {
                    GameObject lem = GetLemAtIndex(state.targetIndex);
                    LemController lemController = lem != null ? lem.GetComponent<LemController>() : null;
                    if (lemController != null)
                    {
                        key.AttachToLem(lemController, key.GetCarryYOffset(size), key.GetWorldScale(size));
                    }
                    break;
                }
                case LevelData.KeyLocation.KeyBlock:
                {
                    BaseBlock block = GetBlockAtIndex(state.targetIndex);
                    KeyBlock keyBlock = block as KeyBlock;
                    if (keyBlock != null)
                    {
                        keyBlock.ResetKeyToBlock();
                    }
                    break;
                }
                case LevelData.KeyLocation.World:
                default:
                {
                    if (state.hasWorldPosition)
                    {
                        key.transform.SetParent(null, true);
                        key.transform.position = state.worldPosition;
                    }
                    break;
                }
            }
        }
    }

    private KeyItem FindKeyBySourceIndex(int sourceIndex)
    {
        if (sourceIndex < 0) return null;

        BaseBlock block = GetBlockAtIndex(sourceIndex);
        KeyBlock keyBlock = block as KeyBlock;
        if (keyBlock == null) return null;
        return keyBlock.GetKeyItem();
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
