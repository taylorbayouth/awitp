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

    // New architecture: Managers handle specific responsibilities
    private GridCoordinateSystem coordinateSystem;
    private BlockPlacementManager blockPlacementManager;
    private LemPlacementManager lemPlacementManager;
    private GridCursorManager gridCursorManager;

    // Cached component references to avoid FindObjectOfType calls
    private static CameraSetup _cachedCameraSetup;
    private static PlaceableSpaceVisualizer _cachedPlaceableVisualizer;
    private static GridVisualizer _cachedGridVisualizer;
    private LevelDefinition _currentLevelDefinition;

    [Header("Grid Settings (Read-Only - Set by Level Data)")]
    [SerializeField, HideInInspector] private int _gridWidth = 10;
    [SerializeField, HideInInspector] private int _gridHeight = 10;

    /// <summary>Grid width in cells. Set by level data when loading.</summary>
    public int gridWidth => _gridWidth;

    /// <summary>Grid height in cells. Set by level data when loading.</summary>
    public int gridHeight => _gridHeight;

    [Header("Grid Origin (Auto-Calculated)")]
    [SerializeField] private Vector3 _gridOrigin = Vector3.zero;

    /// <summary>
    /// The calculated origin point of the grid (bottom-left corner).
    /// Auto-calculated to center grid at world origin.
    /// </summary>
    public Vector3 gridOrigin => _gridOrigin;
    public LevelDefinition CurrentLevelDefinition => _currentLevelDefinition;

    [Header("Cursor Settings")]
    public GameObject cursorHighlightPrefab;

    [Header("Placeable Spaces")]
    private bool[] placeableSpaces;

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
            // === NEW ARCHITECTURE: Initialize Managers ===

            // 1. Create coordinate system (no dependencies)
            coordinateSystem = new GridCoordinateSystem(gridWidth, gridHeight);
            DebugLog.Info($"[GridManager] GridCoordinateSystem created: {gridWidth}x{gridHeight}");

            // 2. Initialize placeable spaces array
            InitializePlaceableSpaces();

            // 3. Initialize BlockPlacementManager
            blockPlacementManager = gameObject.AddComponent<BlockPlacementManager>();
            blockPlacementManager.Initialize(
                coordinateSystem,
                placeableSpaces,
                onBlockChanged: UpdateCursorState
            );

            // 4. Initialize LemPlacementManager
            lemPlacementManager = gameObject.AddComponent<LemPlacementManager>();
            lemPlacementManager.Initialize(coordinateSystem);

            // 5. Initialize GridCursorManager
            gridCursorManager = gameObject.AddComponent<GridCursorManager>();
            gridCursorManager.Initialize(coordinateSystem, cursorHighlightPrefab, this);

            // 6. Register with ServiceRegistry for easy access across codebase
            ServiceRegistry.Register(this);

            // Legacy grid origin calculation (kept for compatibility)
            // TODO: Remove once all code uses coordinateSystem.GridOrigin
            CalculateGridOrigin();

            DebugLog.Info($"[GridManager] Initialization complete: {gridWidth}x{gridHeight} grid");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridManager] Failed to initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Calculates grid origin so the grid is perfectly centered around world origin (0,0,0).
    /// This ensures the camera can be positioned at (0,0,-Z) for perfect centering.
    /// Each cell is 1.0 world unit.
    /// </summary>
    private void CalculateGridOrigin()
    {
        float totalWidth = gridWidth;
        float totalHeight = gridHeight;

        // Position grid so its center is at world origin
        _gridOrigin = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0);

        DebugLog.Info($"GridManager: Auto-calculated gridOrigin = {_gridOrigin} for {gridWidth}x{gridHeight} grid");
    }

    /// <summary>
    /// Performs additional setup after all Awake() methods have executed.
    /// Ensures camera is properly centered on the grid.
    /// Note: If GameSceneInitializer is present, it will load the level and set up
    /// the camera, so we skip initial camera setup here.
    /// </summary>
    private void Start()
    {
        try
        {
            // Cache CameraSetup reference to avoid repeated FindObjectOfType calls
            if (_cachedCameraSetup == null)
            {
                _cachedCameraSetup = ServiceRegistry.Get<CameraSetup>();
            }

            // Check if a scene initializer is present - if so, let it handle camera setup
            GameSceneInitializer sceneInitializer = ServiceRegistry.Get<GameSceneInitializer>();

            if (sceneInitializer == null && _cachedCameraSetup != null)
            {
                // No level loader present, set up camera with current grid dimensions
                _cachedCameraSetup.SetupCamera();
                DebugLog.Info("[GridManager] Camera setup complete (no level loader detected)");
            }
            else if (_cachedCameraSetup == null)
            {
                Debug.LogWarning("[GridManager] No CameraSetup found in scene. Camera may not be positioned correctly.");
            }
            else
            {
                DebugLog.Info("[GridManager] Level loader detected, skipping initial camera setup (will be done after level loads)");
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

    #region Grid Coordinate Conversion (Delegates to GridCoordinateSystem)

    /// <summary>
    /// Converts grid index to world position on XY plane.
    /// </summary>
    public Vector3 IndexToWorldPosition(int index)
    {
        return coordinateSystem.IndexToWorldPosition(index);
    }

    public Vector2Int IndexToCoordinates(int index)
    {
        if (coordinateSystem == null)
        {
            Debug.LogWarning($"[GridManager] coordinateSystem is null when converting index {index} to coordinates");
            return Vector2Int.zero;
        }
        return coordinateSystem.IndexToCoordinates(index);
    }

    public int CoordinatesToIndex(Vector2Int coords)
    {
        return coordinateSystem.CoordinatesToIndex(coords);
    }

    public int CoordinatesToIndex(int x, int y)
    {
        return coordinateSystem.CoordinatesToIndex(x, y);
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// Grid is on XY plane: X = horizontal, Y = vertical, Z = 0
    /// </summary>
    public Vector3 CoordinatesToWorldPosition(Vector2Int coords)
    {
        return coordinateSystem.CoordinatesToWorldPosition(coords);
    }

    public bool IsValidIndex(int index)
    {
        return coordinateSystem.IsValidIndex(index);
    }

    public bool IsValidCoordinates(int x, int y)
    {
        return coordinateSystem.IsValidCoordinates(x, y);
    }

    /// <summary>
    /// Converts world position to grid index.
    /// </summary>
    public int WorldToGridIndex(Vector3 worldPos)
    {
        return coordinateSystem.WorldPositionToGridIndex(worldPos);
    }

    #endregion

    #region Block Placement and Management (Delegates to BlockPlacementManager)

    /// <summary>
    /// Places a block on the grid at the specified index.
    /// Delegates to BlockPlacementManager for all placement logic.
    /// </summary>
    /// <param name="blockType">Type of block to place</param>
    /// <param name="gridIndex">Grid index where block should be placed</param>
    /// <param name="entry">Optional inventory entry</param>
    /// <returns>The placed block, or null if placement failed</returns>
    public BaseBlock PlaceBlock(BlockType blockType, int gridIndex, BlockInventoryEntry entry = null)
    {
        return blockPlacementManager.PlaceBlock(blockType, gridIndex, entry);
    }

    public BaseBlock PlaceBlock(BlockInventoryEntry entry, int gridIndex)
    {
        return blockPlacementManager.PlaceBlock(entry, gridIndex);
    }

    public BaseBlock PlacePermanentBlock(BlockType blockType, int gridIndex, BlockInventoryEntry entry = null)
    {
        return blockPlacementManager.PlacePermanentBlock(blockType, gridIndex, entry);
    }

    public BaseBlock PlacePermanentBlock(BlockInventoryEntry entry, int gridIndex)
    {
        return blockPlacementManager.PlacePermanentBlock(entry, gridIndex);
    }

    public void RegisterBlock(BaseBlock block)
    {
        blockPlacementManager.RegisterBlock(block);
    }

    public void UnregisterBlock(BaseBlock block)
    {
        blockPlacementManager.UnregisterBlock(block);
    }

    public BaseBlock GetBlockAtIndex(int index)
    {
        return blockPlacementManager.GetBlockAtIndex(index);
    }

    public bool IsPermanentBlockAtIndex(int index)
    {
        return blockPlacementManager.IsPermanentBlockAtIndex(index);
    }

    public bool IsGridSpaceOccupied(int index)
    {
        return blockPlacementManager.IsGridSpaceOccupied(index);
    }

    #endregion

    #region Lem Placement (Delegates to LemPlacementManager)

    /// <summary>
    /// Places a Lem at the specified grid index.
    /// Delegates to LemPlacementManager for all Lem operations.
    /// </summary>
    public GameObject PlaceLem(int gridIndex)
    {
        return lemPlacementManager.PlaceLem(gridIndex);
    }

    /// <summary>
    /// Removes the Lem at the specified grid index.
    /// </summary>
    public void RemoveLem(int gridIndex)
    {
        lemPlacementManager.RemoveLem(gridIndex);
    }

    /// <summary>
    /// Resets all Lems to their original placement positions and directions.
    /// Called when exiting Play mode to restore Lems to Level Editor state.
    /// </summary>
    public void ResetAllLems()
    {
        lemPlacementManager.ResetAllLems();
    }

    public void CaptureOriginalKeyStates()
    {
        List<LevelData.KeyStateData> keyStates = CaptureKeyStates();
        lemPlacementManager.CaptureOriginalKeyStates(keyStates);
    }

    public void RestoreOriginalKeyStates()
    {
        List<LevelData.KeyStateData> keyStates = lemPlacementManager.GetOriginalKeyStates();
        ApplyKeyStates(keyStates);
    }

    public void CapturePlayModeSnapshot()
    {
        // Clear any runtime-only debris/support cubes from previous Play sessions.
        CrumblerBlock.CleanupRuntime();
        LevelData snapshot = CaptureLevelData();
        lemPlacementManager.CapturePlayModeSnapshot(snapshot);
    }

    public void RestorePlayModeSnapshot()
    {
        // Clear any runtime-only debris/support cubes from Play mode before restoring snapshot.
        CrumblerBlock.CleanupRuntime();
        LevelData snapshot = lemPlacementManager.GetPlayModeSnapshot();
        if (snapshot == null)
        {
            Debug.LogWarning("[GridManager] No play mode snapshot found to restore");
            return;
        }

        RestoreLevelData(snapshot);
    }

    /// <summary>
    /// Checks if there is a Lem at the specified grid index.
    /// </summary>
    public bool HasLemAtIndex(int index)
    {
        return lemPlacementManager.HasLemAtIndex(index);
    }

    /// <summary>
    /// Gets the Lem at the specified grid index, if any.
    /// </summary>
    public GameObject GetLemAtIndex(int index)
    {
        return lemPlacementManager.GetLemAtIndex(index);
    }

    #endregion

    #region Cursor Movement (Delegates to GridCursorManager)

    public void MoveCursor(Vector2Int direction)
    {
        gridCursorManager.MoveCursor(direction);
    }

    public void SetCursorIndex(int index)
    {
        gridCursorManager.SetCursorIndex(index);
    }

    public void SetCursorVisible(bool visible)
    {
        gridCursorManager.SetCursorVisible(visible);
    }

    /// <summary>
    /// Updates cursor visual state based on current position.
    /// Called by BlockPlacementManager when blocks change.
    /// </summary>
    private void UpdateCursorState()
    {
        // Early return if managers not initialized
        if (gridCursorManager == null)
        {
            return;
        }

        gridCursorManager.UpdateCursorState();
    }

    /// <summary>
    /// Gets the current cursor grid index.
    /// </summary>
    public int GetCurrentCursorIndex()
    {
        return gridCursorManager.CurrentCursorIndex;
    }

    #endregion

    #region Placeable Space Management

    public bool IsSpacePlaceable(int index)
    {
        // Early return if managers not initialized (can happen in editor OnValidate)
        if (coordinateSystem == null || blockPlacementManager == null || placeableSpaces == null)
        {
            return false;
        }

        EnsurePlaceableSpacesSized();
        if (!IsValidIndex(index)) return false;
        if (IsPermanentBlockAtIndex(index)) return false;
        return placeableSpaces[index];
    }

    public void SetSpacePlaceable(int index, bool placeable)
    {
        // Early return if managers not initialized
        if (coordinateSystem == null || blockPlacementManager == null || placeableSpaces == null)
        {
            return;
        }

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
        // Early return if managers not initialized
        if (coordinateSystem == null || blockPlacementManager == null || placeableSpaces == null)
        {
            return;
        }

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
                _cachedCameraSetup = ServiceRegistry.Get<CameraSetup>();
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

            DebugLog.Info($"[GridManager] Grid refreshed: {gridWidth}x{gridHeight}");
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

            // Update BlockPlacementManager's reference to the new array
            if (blockPlacementManager != null)
            {
                blockPlacementManager.UpdatePlaceableSpacesReference(placeableSpaces);
            }
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
            gridHeight = gridHeight
        };

        BlockInventory inventory = ServiceRegistry.Get<BlockInventory>();
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
        Dictionary<int, BaseBlock> permanentBlocks = blockPlacementManager.GetAllPermanentBlocks();
        foreach (var kvp in permanentBlocks)
        {
            if (kvp.Value != null)
            {
                levelData.permanentBlocks.Add(BlockPlacementManager.CreateBlockData(kvp.Value, kvp.Key));
            }
        }

        if (includePlacedBlocks)
        {
            // Capture all placed blocks
            Dictionary<int, BaseBlock> placedBlocks = blockPlacementManager.GetAllPlacedBlocks();
            foreach (var kvp in placedBlocks)
            {
                if (kvp.Value != null)
                {
                    levelData.blocks.Add(BlockPlacementManager.CreateBlockData(kvp.Value, kvp.Key));
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
        Dictionary<int, GameObject> placedLems = lemPlacementManager.GetAllPlacedLems();
        foreach (var kvp in placedLems)
        {
            GameObject lem = kvp.Value;
            if (lem == null) continue;

            LemController controller = lem.GetComponent<LemController>();
            bool facingRight = controller != null ? controller.GetFacingRight() : true;

            LevelData.LemData lemData = new LevelData.LemData(kvp.Key, facingRight);
            lemData.worldPosition = lem.transform.position;
            lemData.hasWorldPosition = true;

            levelData.lems.Add(lemData);
        }

        // Capture key states (runtime-only; skipped for designer saves)
        if (includeKeyStates)
        {
            levelData.keyStates = CaptureKeyStates();
        }

        // Capture camera settings
        CameraSetup cameraSetup = ServiceRegistry.Get<CameraSetup>();
        if (cameraSetup != null)
        {
            levelData.cameraSettings = cameraSetup.ExportSettings();
        }

        DebugLog.Info($"Captured level data: {levelData.permanentBlocks.Count} permanent blocks, {levelData.blocks.Count} blocks, {levelData.placeableSpaceIndices.Count} placeable spaces, {levelData.lems.Count} Lems, camera settings={(levelData.cameraSettings != null ? "saved" : "none")}");
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
        blockPlacementManager.ClearAll();
        lemPlacementManager.ClearAll();
        ClearPlaceableSpaces();

        BlockInventory inventory = ServiceRegistry.Get<BlockInventory>();
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
        // Validate loaded grid dimensions before applying
        if (levelData.gridWidth <= 0 || levelData.gridHeight <= 0)
        {
            Debug.LogError($"Invalid grid dimensions in level data: {levelData.gridWidth}x{levelData.gridHeight}");
            return;
        }

        bool gridSizeChanged = (gridWidth != levelData.gridWidth || gridHeight != levelData.gridHeight);

        if (gridSizeChanged)
        {
            _gridWidth = levelData.gridWidth;
            _gridHeight = levelData.gridHeight;

            // Update coordinate system with new dimensions
            coordinateSystem.UpdateDimensions(gridWidth, gridHeight);

            CalculateGridOrigin();
            InitializePlaceableSpaces();

            // Update BlockPlacementManager's reference to the new array
            if (blockPlacementManager != null)
            {
                blockPlacementManager.UpdatePlaceableSpacesReference(placeableSpaces);
            }

            RefreshGrid();

            // Cursor automatically handles dimension changes through coordinate system
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
                    BlockPlacementManager.ApplyBlockData(block, blockData, inventory);
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
                    BlockPlacementManager.ApplyBlockData(block, blockData, inventory);

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
                            controller.SetFrozen(true); // Start frozen in Designer mode
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
        CameraSetup cameraSetup = ServiceRegistry.Get<CameraSetup>();
        if (cameraSetup != null && levelData.cameraSettings != null)
        {
            cameraSetup.ImportSettings(levelData.cameraSettings);
        }
        else if (cameraSetup != null)
        {
            // No saved camera settings, refresh camera with current settings
            cameraSetup.RefreshCamera();
        }

        if (gridCursorManager != null)
        {
            gridCursorManager.ResetToUpperLeft();
        }

        UpdateCursorState();

        DebugLog.Info($"Restored level data: {levelData.permanentBlocks?.Count ?? 0} permanent blocks, {levelData.blocks?.Count ?? 0} blocks, {levelData.placeableSpaceIndices?.Count ?? 0} placeable spaces, {levelData.lems?.Count ?? 0} Lems, camera settings={(levelData.cameraSettings != null ? "restored" : "default")}");
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
        return blockPlacementManager.HasTransporterConflicts();
    }

    public void ApplyLevelDefinitionSettings(LevelDefinition levelDef)
    {
        _currentLevelDefinition = levelDef;

        if (_cachedGridVisualizer == null)
        {
            _cachedGridVisualizer = GetComponent<GridVisualizer>();
        }
        if (_cachedGridVisualizer != null && levelDef != null)
        {
            _cachedGridVisualizer.ApplySettings(levelDef);
        }

        if (gridCursorManager != null && levelDef != null)
        {
            gridCursorManager.ApplyCursorColors(
                levelDef.cursorPlaceableColor,
                levelDef.cursorEditableColor,
                levelDef.cursorNonPlaceableColor
            );
        }
    }

    /// <summary>
    /// Checks if any placed block claims this index via GetBlockedIndices().
    /// Used for placement validation - blocks can reserve grid spaces they don't occupy.
    /// </summary>
    public bool IsIndexBlockedByAnyBlock(int index, BaseBlock excludeBlock)
    {
        return blockPlacementManager.IsIndexBlockedByAnyBlock(index, excludeBlock);
    }

    /// <summary>
    /// Checks if there is any block at the given index.
    /// </summary>
    public bool HasBlockAtIndex(int index)
    {
        return blockPlacementManager.HasBlockAtIndex(index);
    }

    /// <summary>
    /// Validates if a block can be placed at the given index using the block's own rules.
    /// </summary>
    public bool ValidateBlockPlacement(BaseBlock block, int targetIndex)
    {
        return blockPlacementManager.ValidateBlockPlacement(block, targetIndex);
    }


    private List<LevelData.KeyStateData> CaptureKeyStates()
    {
        List<LevelData.KeyStateData> states = new List<LevelData.KeyStateData>();
        KeyItem[] keys = UnityEngine.Object.FindObjectsByType<KeyItem>(FindObjectsSortMode.None);

        // Guard against null or empty array
        if (keys == null || keys.Length == 0)
        {
            return states;
        }

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
                        key.AttachToLem(lemController, key.GetCarryYOffset(1.0f), key.GetWorldScale(1.0f));
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
            Vector3 start = gridOrigin + new Vector3(0, y, 0);
            Vector3 end = gridOrigin + new Vector3(gridWidth, y, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw vertical lines (along Y axis)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x, 0, 0);
            Vector3 end = gridOrigin + new Vector3(x, gridHeight, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw cursor position
        if (Application.isPlaying && gridCursorManager != null && coordinateSystem != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 cursorPos = IndexToWorldPosition(gridCursorManager.CurrentCursorIndex);
            cursorPos.z += 0.5f; // Visualize block volume at placement depth
            Gizmos.DrawWireCube(cursorPos, Vector3.one);
        }
    }

    #endregion
}
