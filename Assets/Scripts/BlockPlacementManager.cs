using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages block placement, validation, and lifecycle for the grid system.
///
/// RESPONSIBILITIES:
/// - Block placement (normal and permanent)
/// - Block registration and tracking
/// - Placement validation (space availability, transporter conflicts)
/// - Block queries (existence, occupation checks)
/// - Serialization helpers for save/load
///
/// DESIGN NOTE: This manager owns the placed/permanent block dictionaries
/// but references the placeableSpaces array from GridManager since that
/// data is tightly coupled with editor tools.
///
/// DEPENDENCIES:
/// - GridCoordinateSystem for coordinate conversions
/// - BlockInventory for inventory management
/// - Reference to placeableSpaces array for validation
/// </summary>
public class BlockPlacementManager : MonoBehaviour
{
    private GridCoordinateSystem coordinateSystem;
    private bool[] placeableSpaces;

    // Block tracking
    private Dictionary<int, BaseBlock> placedBlocks = new Dictionary<int, BaseBlock>();
    private Dictionary<int, BaseBlock> permanentBlocks = new Dictionary<int, BaseBlock>();

    // Callback for cursor state updates (optional)
    private Action onBlockChanged;

    /// <summary>
    /// Initializes the block placement manager.
    /// </summary>
    /// <param name="coordinateSystem">Grid coordinate system for position calculations</param>
    /// <param name="placeableSpaces">Reference to placeable spaces array</param>
    /// <param name="onBlockChanged">Optional callback when blocks change (for cursor updates)</param>
    public void Initialize(
        GridCoordinateSystem coordinateSystem,
        bool[] placeableSpaces,
        Action onBlockChanged = null)
    {
        this.coordinateSystem = coordinateSystem;
        this.placeableSpaces = placeableSpaces;
        this.onBlockChanged = onBlockChanged;

        DebugLog.Info("[BlockPlacementManager] Initialized");
    }

    #region Block Placement

    /// <summary>
    /// Places a block on the grid at the specified index.
    ///
    /// Why extensive validation: Prevents invalid game states that could cause crashes
    /// or exploits. Better to reject early than deal with corrupt state later.
    ///
    /// Validation order optimized for performance:
    /// 1. Quick checks first (index validity, transporter conflicts)
    /// 2. Heavier checks later (inventory queries)
    /// </summary>
    /// <param name="blockType">Type of block to place</param>
    /// <param name="gridIndex">Grid index where block should be placed</param>
    /// <param name="entry">Optional inventory entry (auto-resolved if null)</param>
    /// <returns>The placed block, or null if placement failed</returns>
    public BaseBlock PlaceBlock(BlockType blockType, int gridIndex, BlockInventoryEntry entry = null)
    {
        try
        {
            // === VALIDATION CHECKS (ordered by cost: cheap → expensive) ===

            // Check 1: Valid grid index (instant)
            if (!coordinateSystem.IsValidIndex(gridIndex))
            {
                DebugLog.LogWarning($"[BlockPlacementManager] Cannot place block: Invalid grid index {gridIndex}");
                return null;
            }

            // Check 2: Transporter path conflict (fast dictionary lookup)
            if (IsIndexBlockedByTransporterPath(gridIndex))
            {
                DebugLog.LogWarning($"[BlockPlacementManager] Cannot place {blockType} at index {gridIndex}: Transporter path reserved");
                return null;
            }

            // Check 2b: Transporter-specific placement constraints
            if (blockType == BlockType.Transporter)
            {
                if (IsGridSpaceOccupied(gridIndex))
                {
                    DebugLog.LogWarning($"[BlockPlacementManager] Cannot place Transporter at index {gridIndex}: Space already occupied");
                    return null;
                }

                // Resolve route for validation
                BlockInventoryEntry resolvedForRoute = entry ?? GetDefaultInventoryEntry(blockType);
                string[] routeSteps = resolvedForRoute?.routeSteps;

                // Check if route goes outside grid bounds
                if (!IsTransporterRouteWithinBounds(gridIndex, routeSteps))
                {
                    DebugLog.LogWarning($"[BlockPlacementManager] Cannot place Transporter at index {gridIndex}: Route path goes outside grid bounds");
                    return null;
                }

                // Check if route path is blocked by existing blocks
                List<int> routeIndices = BuildTransporterPathIndices(gridIndex, routeSteps);
                if (routeIndices.Count > 0 && HasBlocksOnIndices(routeIndices))
                {
                    DebugLog.LogWarning($"[BlockPlacementManager] Cannot place Transporter at index {gridIndex}: Route path blocked by existing block");
                    return null;
                }
            }

            // Check 3: Space must be marked as placeable
            if (!IsSpacePlaceable(gridIndex))
            {
                DebugLog.LogWarning($"[BlockPlacementManager] Cannot place {blockType} at index {gridIndex}: Space not marked as placeable");
                return null;
            }

            // Check 4: No permanent blocks
            if (IsPermanentBlockAtIndex(gridIndex))
            {
                DebugLog.LogWarning($"[BlockPlacementManager] Cannot place {blockType} at index {gridIndex}: Permanent block present");
                return null;
            }

            // Check 5: Inventory availability (most expensive - queries external system)
            BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
            BlockInventoryEntry resolvedEntry = entry ?? GetDefaultInventoryEntry(blockType);

            if (inventory != null && resolvedEntry == null)
            {
                DebugLog.LogWarning($"[BlockPlacementManager] Cannot place {blockType}: No inventory entry configured");
                return null;
            }

            if (inventory != null && resolvedEntry != null && !inventory.CanPlaceEntry(resolvedEntry))
            {
                DebugLog.LogWarning($"[BlockPlacementManager] Cannot place {resolvedEntry.GetDisplayName()}: No blocks remaining in inventory");
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
                Debug.LogError($"[BlockPlacementManager] Failed to instantiate {blockType} block at index {gridIndex}");

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
            onBlockChanged?.Invoke();

            DebugLog.Info($"[BlockPlacementManager] Placed {blockType} at index {gridIndex}");
            return newBlock;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BlockPlacementManager] Error placing {blockType} block at index {gridIndex}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Places a block using an inventory entry.
    /// </summary>
    public BaseBlock PlaceBlock(BlockInventoryEntry entry, int gridIndex)
    {
        if (entry == null)
        {
            DebugLog.LogWarning("[BlockPlacementManager] Cannot place block: inventory entry is null");
            return null;
        }

        return PlaceBlock(entry.blockType, gridIndex, entry);
    }

    /// <summary>
    /// Places a permanent block that cannot be removed by the player.
    /// Permanent blocks mark their grid space as non-placeable.
    /// </summary>
    public BaseBlock PlacePermanentBlock(BlockType blockType, int gridIndex, BlockInventoryEntry entry = null)
    {
        if (!coordinateSystem.IsValidIndex(gridIndex))
        {
            DebugLog.LogWarning($"[BlockPlacementManager] Invalid grid index: {gridIndex}");
            return null;
        }

        if (IsIndexBlockedByTransporterPath(gridIndex))
        {
            DebugLog.LogWarning($"[BlockPlacementManager] Cannot place permanent block at index {gridIndex}: transporter path reserved");
            return null;
        }

        // Remove any existing block at this location
        BaseBlock existingBlock = GetBlockAtIndex(gridIndex);
        if (existingBlock != null)
        {
            existingBlock.DestroyBlock();
        }

        // Create and position permanent block
        BaseBlock newBlock = BaseBlock.Instantiate(blockType, gridIndex);
        newBlock.isPermanent = true;

        BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        ApplyEntryMetadata(newBlock, entry, inventory);
        PositionBlock(newBlock, gridIndex);

        // Register as permanent
        permanentBlocks[gridIndex] = newBlock;

        // Mark space as non-placeable (permanent blocks block placement)
        SetSpacePlaceable(gridIndex, false);

        DebugLog.Info($"[BlockPlacementManager] Placed permanent {blockType} at index {gridIndex}");
        return newBlock;
    }

    /// <summary>
    /// Places a permanent block using an inventory entry.
    /// </summary>
    public BaseBlock PlacePermanentBlock(BlockInventoryEntry entry, int gridIndex)
    {
        if (entry == null)
        {
            DebugLog.LogWarning("[BlockPlacementManager] Cannot place permanent block: inventory entry is null");
            return null;
        }

        return PlacePermanentBlock(entry.blockType, gridIndex, entry);
    }

    #endregion

    #region Block Registration

    /// <summary>
    /// Registers a block with the manager (called by blocks on creation).
    /// </summary>
    public void RegisterBlock(BaseBlock block)
    {
        if (!coordinateSystem.IsValidIndex(block.gridIndex))
        {
            return;
        }

        if (block.isPermanent)
        {
            permanentBlocks[block.gridIndex] = block;
        }
        else
        {
            placedBlocks[block.gridIndex] = block;
        }

        onBlockChanged?.Invoke();
    }

    /// <summary>
    /// Unregisters a block (called by blocks on destruction).
    /// </summary>
    public void UnregisterBlock(BaseBlock block)
    {
        placedBlocks.Remove(block.gridIndex);
        permanentBlocks.Remove(block.gridIndex);
        onBlockChanged?.Invoke();
    }

    #endregion

    #region Block Queries

    /// <summary>
    /// Gets the block at the specified grid index.
    /// Checks permanent blocks first, then placed blocks.
    /// </summary>
    public BaseBlock GetBlockAtIndex(int index)
    {
        if (permanentBlocks.TryGetValue(index, out BaseBlock permanentBlock))
        {
            return permanentBlock;
        }
        return placedBlocks.TryGetValue(index, out BaseBlock block) ? block : null;
    }

    /// <summary>
    /// Checks if a permanent block exists at the index.
    /// </summary>
    public bool IsPermanentBlockAtIndex(int index)
    {
        return permanentBlocks.ContainsKey(index);
    }

    /// <summary>
    /// Checks if any block (placed or permanent) occupies the grid space.
    /// </summary>
    public bool IsGridSpaceOccupied(int index)
    {
        return placedBlocks.ContainsKey(index) || permanentBlocks.ContainsKey(index);
    }

    /// <summary>
    /// Checks if there is any block at the given index.
    /// </summary>
    public bool HasBlockAtIndex(int index)
    {
        return GetBlockAtIndex(index) != null;
    }

    /// <summary>
    /// Gets all placed blocks (non-permanent).
    /// </summary>
    public Dictionary<int, BaseBlock> GetAllPlacedBlocks()
    {
        return new Dictionary<int, BaseBlock>(placedBlocks);
    }

    /// <summary>
    /// Gets all permanent blocks.
    /// </summary>
    public Dictionary<int, BaseBlock> GetAllPermanentBlocks()
    {
        return new Dictionary<int, BaseBlock>(permanentBlocks);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates if a block can be placed at the given index using the block's own rules.
    /// </summary>
    public bool ValidateBlockPlacement(BaseBlock block, int targetIndex)
    {
        if (block == null) return false;

        // Use GridManager singleton for validation context
        // Why: Block validation needs full grid context which GridManager provides
        GridManager gridManager = GridManager.Instance;
        if (gridManager == null) return false;

        return block.CanBePlacedAt(targetIndex, gridManager);
    }

    /// <summary>
    /// Checks if any existing transporter's path blocks this index.
    /// Legacy method maintained for compatibility.
    /// </summary>
    private bool IsIndexBlockedByTransporterPath(int index)
    {
        return IsIndexBlockedByAnyBlock(index, null);
    }

    /// <summary>
    /// Checks if any placed block claims this index via GetBlockedIndices().
    ///
    /// Why: Some blocks (like Transporters) reserve grid spaces they don't physically occupy.
    /// Rationale: Prevents overlapping paths and ensures gameplay clarity.
    /// </summary>
    /// <param name="index">Grid index to check</param>
    /// <param name="excludeBlock">Optional block to exclude from check</param>
    /// <returns>True if any block claims this index</returns>
    public bool IsIndexBlockedByAnyBlock(int index, BaseBlock excludeBlock)
    {
        // Check all placed blocks
        foreach (var kvp in placedBlocks)
        {
            BaseBlock block = kvp.Value;
            if (block == null || block == excludeBlock) continue;

            int[] blockedIndices = block.GetBlockedIndices();
            if (blockedIndices != null && Array.IndexOf(blockedIndices, index) >= 0)
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
            if (blockedIndices != null && Array.IndexOf(blockedIndices, index) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if any transporter path conflicts exist in the grid.
    /// </summary>
    public bool HasTransporterConflicts()
    {
        TransporterBlock[] transporters = UnityEngine.Object.FindObjectsByType<TransporterBlock>(FindObjectsSortMode.None);
        foreach (TransporterBlock transporter in transporters)
        {
            if (transporter == null) continue;

            List<int> pathIndices = transporter.GetRoutePathIndices();
            foreach (int index in pathIndices)
            {
                if (!coordinateSystem.IsValidIndex(index)) continue;

                BaseBlock block = GetBlockAtIndex(index);
                if (block != null && block != transporter)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if any blocks exist on the given indices.
    /// </summary>
    private bool HasBlocksOnIndices(List<int> indices)
    {
        foreach (int index in indices)
        {
            if (!coordinateSystem.IsValidIndex(index)) continue;
            if (GetBlockAtIndex(index) != null)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Builds the list of grid indices a transporter path would traverse.
    /// </summary>
    private List<int> BuildTransporterPathIndices(int originIndex, string[] routeSteps)
    {
        List<int> indices = new List<int>();
        if (!coordinateSystem.IsValidIndex(originIndex))
        {
            return indices;
        }

        Vector2Int current = coordinateSystem.IndexToCoordinates(originIndex);
        List<Vector2Int> steps = RouteParser.ParseRouteSteps(routeSteps);
        HashSet<int> unique = new HashSet<int>();

        unique.Add(originIndex);
        foreach (Vector2Int step in steps)
        {
            current += step;
            if (coordinateSystem.IsValidCoordinates(current.x, current.y))
            {
                int idx = coordinateSystem.CoordinatesToIndex(current);
                if (unique.Add(idx))
                {
                    indices.Add(idx);
                }
            }
        }

        indices.Insert(0, originIndex);
        return indices;
    }

    /// <summary>
    /// Checks if a transporter route stays within grid bounds for all steps.
    /// </summary>
    private bool IsTransporterRouteWithinBounds(int originIndex, string[] routeSteps)
    {
        if (!coordinateSystem.IsValidIndex(originIndex))
        {
            return false;
        }

        if (routeSteps == null || routeSteps.Length == 0)
        {
            return true; // Empty route is valid
        }

        Vector2Int current = coordinateSystem.IndexToCoordinates(originIndex);
        List<Vector2Int> steps = RouteParser.ParseRouteSteps(routeSteps);

        foreach (Vector2Int step in steps)
        {
            current += step;
            if (!coordinateSystem.IsValidCoordinates(current.x, current.y))
            {
                return false; // Step goes outside grid bounds
            }
        }

        return true; // All steps stay within bounds
    }

    #endregion

    #region Clear Operations

    /// <summary>
    /// Clears all placed and permanent blocks from the grid.
    /// </summary>
    public void ClearAll()
    {
        // Create a copy of keys to avoid modification during iteration
        var indices = new List<int>(placedBlocks.Keys);
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

        DebugLog.Info("[BlockPlacementManager] Cleared all blocks");
    }

    #endregion

    #region Serialization Helpers

    /// <summary>
    /// Creates serializable block data for saving.
    /// </summary>
    public static LevelData.BlockData CreateBlockData(BaseBlock block, int index)
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

    /// <summary>
    /// Applies saved block data when loading.
    /// </summary>
    public static void ApplyBlockData(BaseBlock block, LevelData.BlockData data, BlockInventory inventory)
    {
        if (block == null || data == null) return;

        block.flavorId = data.flavorId;

        TransporterBlock transporter = block as TransporterBlock;
        if (transporter != null && data.routeSteps != null)
        {
            transporter.routeSteps = (string[])data.routeSteps.Clone();
        }

        // Resolve inventory key for transporter blocks
        if (block.blockType == BlockType.Transporter && inventory != null)
        {
            BlockInventoryEntry entry = inventory.FindEntry(
                block.blockType,
                block.flavorId,
                transporter != null ? transporter.routeSteps : data.routeSteps,
                data.inventoryKey);

            if (entry != null)
            {
                block.inventoryKey = inventory.GetInventoryKey(entry);
                return;
            }
        }

        block.inventoryKey = data.inventoryKey;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Positions a block at the specified grid index.
    /// </summary>
    private void PositionBlock(BaseBlock block, int gridIndex)
    {
        Vector3 position = coordinateSystem.IndexToWorldPosition(gridIndex);
        position.z += coordinateSystem.CellSize * 0.5f; // Center block so front face is flush with grid (z=0)

        block.transform.position = position;
        block.transform.localScale = Vector3.one * coordinateSystem.CellSize;
    }

    /// <summary>
    /// Applies inventory metadata to a block.
    /// </summary>
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

    /// <summary>
    /// Gets the default inventory entry for a block type.
    /// </summary>
    private static BlockInventoryEntry GetDefaultInventoryEntry(BlockType blockType)
    {
        BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        return inventory?.GetDefaultEntryForBlockType(blockType);
    }

    /// <summary>
    /// Checks if a space is marked as placeable.
    /// </summary>
    private bool IsSpacePlaceable(int index)
    {
        if (!coordinateSystem.IsValidIndex(index)) return false;
        if (IsPermanentBlockAtIndex(index)) return false;
        if (placeableSpaces == null || index >= placeableSpaces.Length) return false;
        return placeableSpaces[index];
    }

    /// <summary>
    /// Sets whether a space is placeable (used by permanent block placement).
    /// </summary>
    private void SetSpacePlaceable(int index, bool placeable)
    {
        if (placeableSpaces != null && index >= 0 && index < placeableSpaces.Length)
        {
            placeableSpaces[index] = placeable;
        }
    }

    #endregion

    private void OnDestroy()
    {
        ClearAll();
        DebugLog.Info("[BlockPlacementManager] Destroyed");
    }
}
