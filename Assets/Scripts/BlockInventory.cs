using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single block type's inventory data.
/// </summary>
[System.Serializable]
public class BlockInventoryItem
{
    public BlockType blockType;
    public int maxCount;
    public int currentCount;

    public BlockInventoryItem(BlockType type, int max)
    {
        blockType = type;
        maxCount = max;
        currentCount = max;
    }

    public bool CanPlace()
    {
        return currentCount > 0;
    }

    public void UseBlock()
    {
        if (currentCount > 0)
        {
            currentCount--;
        }
    }

    public void ReturnBlock()
    {
        if (currentCount < maxCount)
        {
            currentCount++;
        }
    }
}

/// <summary>
/// Manages per-level block inventory constraints.
/// Tracks how many of each block type are available and enforces limits.
/// </summary>
public class BlockInventory : MonoBehaviour
{
    [Header("Level Block Limits")]
    public List<BlockInventoryItem> inventory = new List<BlockInventoryItem>();

    private void Awake()
    {
        // Default inventory for testing - can be configured per level
        if (inventory.Count == 0)
        {
            inventory.Add(new BlockInventoryItem(BlockType.Default, 999)); // Unlimited default
            inventory.Add(new BlockInventoryItem(BlockType.Crumbler, 4));
            inventory.Add(new BlockInventoryItem(BlockType.Teleporter, 5));
            inventory.Add(new BlockInventoryItem(BlockType.Transporter, 1));
        }
    }

    public bool CanPlaceBlock(BlockType blockType)
    {
        BlockInventoryItem item = GetInventoryItem(blockType);
        return item != null && item.CanPlace();
    }

    public bool UseBlock(BlockType blockType)
    {
        BlockInventoryItem item = GetInventoryItem(blockType);
        if (item != null && item.CanPlace())
        {
            item.UseBlock();
            Debug.Log($"Used {blockType} block. Remaining: {item.currentCount}/{item.maxCount}");
            return true;
        }
        Debug.LogWarning($"Cannot place {blockType} block - inventory empty or unavailable");
        return false;
    }

    public void ReturnBlock(BlockType blockType)
    {
        BlockInventoryItem item = GetInventoryItem(blockType);
        if (item != null)
        {
            item.ReturnBlock();
            Debug.Log($"Returned {blockType} block. Available: {item.currentCount}/{item.maxCount}");
        }
    }

    public int GetRemainingCount(BlockType blockType)
    {
        BlockInventoryItem item = GetInventoryItem(blockType);
        return item != null ? item.currentCount : 0;
    }

    public int GetMaxCount(BlockType blockType)
    {
        BlockInventoryItem item = GetInventoryItem(blockType);
        return item != null ? item.maxCount : 0;
    }

    /// <summary>
    /// Gets the number of blocks currently available to place.
    /// Alias for GetRemainingCount() with more intuitive naming.
    /// </summary>
    public int GetAvailableCount(BlockType blockType)
    {
        return GetRemainingCount(blockType);
    }

    /// <summary>
    /// Gets the total number of blocks of this type for the level.
    /// Alias for GetMaxCount() with more intuitive naming.
    /// </summary>
    public int GetTotalCount(BlockType blockType)
    {
        return GetMaxCount(blockType);
    }

    private BlockInventoryItem GetInventoryItem(BlockType blockType)
    {
        return inventory.Find(item => item.blockType == blockType);
    }

    public void ResetInventory()
    {
        foreach (var item in inventory)
        {
            item.currentCount = item.maxCount;
        }
        Debug.Log("Block inventory reset");
    }
}
