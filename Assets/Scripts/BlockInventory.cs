using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single block entry in the inventory list.
/// Entries can share a group id to draw from the same count.
/// </summary>
[System.Serializable]
public class BlockInventoryEntry
{
    [HideInInspector]
    [Tooltip("Auto-generated unique id used for save/load and selection.")]
    public string entryId;

    [Tooltip("Block behavior type for this entry.")]
    public BlockType blockType;

    [Tooltip("Optional UI label override (e.g., 'Teleporter A', 'L3,U1 Transporter'). Leave blank to auto-label.")]
    public string displayName;

    [Tooltip("Optional shared pool id. Entries with the same group id share counts.")]
    public string inventoryGroupId;

    [Tooltip("Optional flavor id (e.g., 'A', 'B', 'C' for teleporters; 'L3,U1' for transporters).")]
    public string flavorId;

    [Tooltip("Transporter-only route steps like L2, U1. Leave empty for non-transporters.")]
    public string[] routeSteps;

    [Tooltip("If true, each inventory unit represents multiple placements (e.g., teleporter pairs).")]
    public bool isPairInventory = false;

    [Tooltip("Placements per inventory unit when using pairs. Teleporters should be 2.")]
    public int pairSize = 2;

    [Tooltip("Total number of inventory units available for this entry (or shared group).")]
    public int maxCount;

    [HideInInspector]
    [Tooltip("Runtime only: current remaining units. Not designer-editable.")]
    public int currentCount;

    public BlockInventoryEntry() { }

    public BlockInventoryEntry(BlockType type, int max)
    {
        blockType = type;
        maxCount = max;
        currentCount = max;
    }

    public BlockInventoryEntry Clone()
    {
        return new BlockInventoryEntry
        {
            entryId = entryId,
            blockType = blockType,
            displayName = displayName,
            inventoryGroupId = inventoryGroupId,
            flavorId = flavorId,
            routeSteps = routeSteps != null ? (string[])routeSteps.Clone() : null,
            isPairInventory = isPairInventory,
            pairSize = pairSize,
            maxCount = maxCount,
            currentCount = currentCount
        };
    }

    public string GetResolvedFlavorId()
    {
        if (!string.IsNullOrEmpty(flavorId)) return flavorId;
        if (routeSteps != null && routeSteps.Length > 0)
        {
            return string.Join(",", routeSteps);
        }
        return string.Empty;
    }

    public string GetEntryId()
    {
        if (!string.IsNullOrEmpty(entryId)) return entryId;
        string resolvedFlavor = GetResolvedFlavorId();
        return string.IsNullOrEmpty(resolvedFlavor) ? blockType.ToString() : $"{blockType}_{resolvedFlavor}";
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        string baseName = BlockColors.GetBlockTypeName(blockType);
        string resolvedFlavor = GetResolvedFlavorId();
        return string.IsNullOrEmpty(resolvedFlavor) ? baseName : $"{baseName} {resolvedFlavor}";
    }
}

/// <summary>
/// Manages per-level block inventory constraints.
/// Supports multiple flavored entries with shared inventory groups.
/// </summary>
public class BlockInventory : MonoBehaviour
{
    [Header("Inventory Source")]
    public LevelBlockInventoryConfig configSource;
    public bool autoFindConfig = true;

    [Header("Level Block Inventory")]
    public List<BlockInventoryEntry> entries = new List<BlockInventoryEntry>();

    private readonly Dictionary<string, int> pairCredits = new Dictionary<string, int>();

    private void Awake()
    {
        LoadFromConfigIfAvailable();

        if (entries.Count == 0)
        {
            CreateDefaultInventory();
        }

        NormalizeEntries();
        ResetInventory();
    }

    private void Start()
    {
        if (LoadFromConfigIfAvailable())
        {
            NormalizeEntries();
            ResetInventory();
        }
    }

    public IReadOnlyList<BlockInventoryEntry> GetEntries()
    {
        return entries;
    }

    public BlockInventoryEntry GetEntryByIndex(int index)
    {
        if (index < 0 || index >= entries.Count) return null;
        return entries[index];
    }

    public BlockInventoryEntry GetEntryById(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return null;
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;
            if (entry.entryId == entryId || entry.GetEntryId() == entryId)
            {
                return entry;
            }
        }
        return null;
    }

    public BlockInventoryEntry GetDefaultEntryForBlockType(BlockType blockType)
    {
        BlockInventoryEntry fallback = null;
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null || entry.blockType != blockType) continue;

            if (string.IsNullOrEmpty(entry.flavorId) && (entry.routeSteps == null || entry.routeSteps.Length == 0))
            {
                return entry;
            }

            if (fallback == null)
            {
                fallback = entry;
            }
        }
        return fallback;
    }

    public BlockInventoryEntry FindEntry(BlockType blockType, string flavorId, string[] routeSteps, string inventoryKey)
    {
        string resolvedFlavor = !string.IsNullOrEmpty(flavorId)
            ? flavorId
            : (routeSteps != null && routeSteps.Length > 0 ? string.Join(",", routeSteps) : string.Empty);

        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null || entry.blockType != blockType) continue;

            if (!string.IsNullOrEmpty(inventoryKey) && GetInventoryKey(entry) != inventoryKey)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(resolvedFlavor))
            {
                if (entry.GetResolvedFlavorId() != resolvedFlavor) continue;
            }

            return entry;
        }

        return null;
    }

    public string GetInventoryKey(BlockInventoryEntry entry)
    {
        if (entry == null) return string.Empty;
        if (!string.IsNullOrEmpty(entry.inventoryGroupId)) return entry.inventoryGroupId;
        return entry.GetEntryId();
    }

    public bool CanPlaceEntry(BlockInventoryEntry entry)
    {
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(GetInventoryKey(entry));
        if (groupEntry == null) return false;

        if (groupEntry.isPairInventory)
        {
            string key = GetInventoryKey(groupEntry);
            return GetPairCredits(key) > 0 || groupEntry.currentCount > 0;
        }

        return groupEntry.currentCount > 0;
    }

    public bool UseBlock(BlockInventoryEntry entry)
    {
        string key = GetInventoryKey(entry);
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(key);
        if (groupEntry == null)
        {
            Debug.LogWarning("[BlockInventory] Cannot use block - inventory entry not found.");
            return false;
        }

        if (groupEntry.isPairInventory)
        {
            int credits = GetPairCredits(key);
            if (credits > 0)
            {
                SetPairCredits(key, credits - 1);
                Debug.Log($"[BlockInventory] Used {groupEntry.GetDisplayName()} (pair credit). Remaining credits: {credits - 1}");
                return true;
            }

            if (groupEntry.currentCount <= 0)
            {
                Debug.LogWarning($"[BlockInventory] Cannot place {groupEntry.GetDisplayName()} - inventory empty");
                return false;
            }

            int newCount = Mathf.Max(0, groupEntry.currentCount - 1);
            SetGroupCount(key, newCount);
            int newCredits = Mathf.Max(0, groupEntry.pairSize - 1);
            SetPairCredits(key, newCredits);
            Debug.Log($"[BlockInventory] Used {groupEntry.GetDisplayName()} (pair). Remaining pairs: {newCount}/{groupEntry.maxCount}, credits: {newCredits}");
            return true;
        }

        if (groupEntry.currentCount <= 0)
        {
            Debug.LogWarning($"[BlockInventory] Cannot place {groupEntry.GetDisplayName()} - inventory empty");
            return false;
        }

        int remaining = Mathf.Max(0, groupEntry.currentCount - 1);
        SetGroupCount(key, remaining);
        Debug.Log($"[BlockInventory] Used {groupEntry.GetDisplayName()}. Remaining: {remaining}/{groupEntry.maxCount}");
        return true;
    }

    public void ReturnBlock(BlockInventoryEntry entry)
    {
        ReturnBlockByKey(GetInventoryKey(entry), entry != null ? entry.blockType : BlockType.Default);
    }

    public void ReturnBlock(string inventoryKey, BlockType fallbackType)
    {
        ReturnBlockByKey(inventoryKey, fallbackType);
    }

    private void ReturnBlockByKey(string inventoryKey, BlockType fallbackType)
    {
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(inventoryKey);
        if (groupEntry == null)
        {
            groupEntry = GetDefaultEntryForBlockType(fallbackType);
        }

        if (groupEntry == null)
        {
            Debug.LogWarning("[BlockInventory] Cannot return block - inventory entry not found.");
            return;
        }

        string key = GetInventoryKey(groupEntry);
        if (groupEntry.isPairInventory)
        {
            int credits = GetPairCredits(key);
            int maxCredits = Mathf.Max(0, groupEntry.pairSize - 1);
            if (credits < maxCredits)
            {
                SetPairCredits(key, credits + 1);
                Debug.Log($"[BlockInventory] Returned {groupEntry.GetDisplayName()} (pair credit). Credits: {credits + 1}");
                return;
            }

            int newPairCount = Mathf.Min(groupEntry.maxCount, groupEntry.currentCount + 1);
            SetGroupCount(key, newPairCount);
            SetPairCredits(key, 0);
            Debug.Log($"[BlockInventory] Returned {groupEntry.GetDisplayName()} (pair). Available pairs: {newPairCount}/{groupEntry.maxCount}");
            return;
        }

        int newSingleCount = Mathf.Min(groupEntry.maxCount, groupEntry.currentCount + 1);
        SetGroupCount(key, newSingleCount);
        Debug.Log($"[BlockInventory] Returned {groupEntry.GetDisplayName()}. Available: {newSingleCount}/{groupEntry.maxCount}");
    }

    public int GetAvailableCount(BlockInventoryEntry entry)
    {
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(GetInventoryKey(entry));
        if (groupEntry == null) return 0;

        if (groupEntry.isPairInventory)
        {
            int credits = GetPairCredits(GetInventoryKey(groupEntry));
            return Mathf.Max(0, groupEntry.currentCount) * Mathf.Max(1, groupEntry.pairSize) + credits;
        }

        return groupEntry.currentCount;
    }

    public int GetTotalCount(BlockInventoryEntry entry)
    {
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(GetInventoryKey(entry));
        if (groupEntry == null) return 0;

        if (groupEntry.isPairInventory)
        {
            return Mathf.Max(0, groupEntry.maxCount) * Mathf.Max(1, groupEntry.pairSize);
        }

        return groupEntry.maxCount;
    }

    public int GetDisplayAvailableCount(BlockInventoryEntry entry)
    {
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(GetInventoryKey(entry));
        if (groupEntry == null) return 0;

        if (groupEntry.isPairInventory)
        {
            return Mathf.Max(0, groupEntry.currentCount);
        }

        return groupEntry.currentCount;
    }

    public int GetDisplayTotalCount(BlockInventoryEntry entry)
    {
        BlockInventoryEntry groupEntry = GetEntryForInventoryKey(GetInventoryKey(entry));
        if (groupEntry == null) return 0;

        if (groupEntry.isPairInventory)
        {
            return Mathf.Max(0, groupEntry.maxCount);
        }

        return groupEntry.maxCount;
    }

    // --- Backward-compatible methods (block type only) ---

    public bool CanPlaceBlock(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return CanPlaceEntry(entry);
    }

    public bool UseBlock(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return UseBlock(entry);
    }

    public void ReturnBlock(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        ReturnBlock(entry);
    }

    public int GetRemainingCount(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return GetAvailableCount(entry);
    }

    public int GetMaxCount(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return GetTotalCount(entry);
    }

    public int GetAvailableCount(BlockType blockType)
    {
        return GetRemainingCount(blockType);
    }

    public int GetTotalCount(BlockType blockType)
    {
        return GetMaxCount(blockType);
    }

    public void ResetInventory()
    {
        pairCredits.Clear();
        HashSet<string> groups = new HashSet<string>();
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;
            string key = GetInventoryKey(entry);
            if (groups.Add(key))
            {
                SetGroupCount(key, GetGroupMaxCount(key));
            }
        }
        Debug.Log("[BlockInventory] Inventory reset");
    }

    public void ApplyInventoryEntries(List<BlockInventoryEntry> newEntries)
    {
        entries.Clear();
        if (newEntries != null)
        {
            foreach (BlockInventoryEntry entry in newEntries)
            {
                if (entry != null)
                {
                    entries.Add(entry.Clone());
                }
            }
        }
        NormalizeEntries();
        ResetInventory();
    }

    public List<BlockInventoryEntry> GetSerializableEntries()
    {
        if (configSource != null && configSource.entries != null && configSource.entries.Count > 0)
        {
            List<BlockInventoryEntry> configured = new List<BlockInventoryEntry>();
            foreach (BlockInventoryEntry entry in configSource.entries)
            {
                if (entry != null)
                {
                    BlockInventoryEntry clone = entry.Clone();
                    clone.entryId = entry.GetEntryId();
                    clone.currentCount = clone.maxCount;
                    configured.Add(clone);
                }
            }
            return configured;
        }

        List<BlockInventoryEntry> result = new List<BlockInventoryEntry>();
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry != null)
            {
                BlockInventoryEntry clone = entry.Clone();
                clone.entryId = entry.GetEntryId();
                clone.currentCount = clone.maxCount;
                result.Add(clone);
            }
        }
        return result;
    }

    private bool LoadFromConfigIfAvailable()
    {
        if (configSource == null && autoFindConfig)
        {
            LevelBlockInventoryConfig[] configs = FindObjectsOfType<LevelBlockInventoryConfig>(true);
            if (configs != null && configs.Length > 0)
            {
                configSource = configs[0];
            }
        }

        if (configSource != null && configSource.entries != null && configSource.entries.Count > 0)
        {
            entries.Clear();
            foreach (BlockInventoryEntry entry in configSource.entries)
            {
                if (entry != null)
                {
                    entries.Add(entry.Clone());
                }
            }
            return true;
        }
        return false;
    }

    private void CreateDefaultInventory()
    {
        entries.Add(new BlockInventoryEntry(BlockType.Default, 999));
        entries.Add(new BlockInventoryEntry(BlockType.Crumbler, 4));
        entries.Add(new BlockInventoryEntry(BlockType.Teleporter, 5));
        entries.Add(new BlockInventoryEntry(BlockType.Transporter, 1));
        entries.Add(new BlockInventoryEntry(BlockType.Key, 1));
        entries.Add(new BlockInventoryEntry(BlockType.Lock, 1));
    }

    private void NormalizeEntries()
    {
        HashSet<string> usedIds = new HashSet<string>();
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;

            if (string.IsNullOrEmpty(entry.entryId))
            {
                entry.entryId = entry.GetEntryId();
            }

            if (entry.isPairInventory && entry.pairSize < 2)
            {
                entry.pairSize = 2;
            }

            string uniqueId = entry.entryId;
            int suffix = 1;
            while (usedIds.Contains(uniqueId))
            {
                uniqueId = $"{entry.entryId}_{suffix}";
                suffix++;
            }

            if (uniqueId != entry.entryId)
            {
                entry.entryId = uniqueId;
            }

            usedIds.Add(entry.entryId);
        }

        Dictionary<string, int> groupMaxCounts = new Dictionary<string, int>();
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;
            string key = GetInventoryKey(entry);
            if (!groupMaxCounts.ContainsKey(key))
            {
                groupMaxCounts[key] = entry.maxCount;
            }
            else if (groupMaxCounts[key] != entry.maxCount)
            {
                Debug.LogWarning($"[BlockInventory] Inventory group '{key}' has mismatched max counts. Using {groupMaxCounts[key]}.");
            }
        }

        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;
            string key = GetInventoryKey(entry);
            entry.maxCount = groupMaxCounts[key];
        }
    }

    private BlockInventoryEntry GetEntryForInventoryKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;
            if (GetInventoryKey(entry) == key)
            {
                return entry;
            }
        }
        return null;
    }

    private int GetGroupMaxCount(string key)
    {
        BlockInventoryEntry entry = GetEntryForInventoryKey(key);
        return entry != null ? entry.maxCount : 0;
    }

    private void SetGroupCount(string key, int newCount)
    {
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;
            if (GetInventoryKey(entry) == key)
            {
                entry.currentCount = newCount;
            }
        }
    }

    private int GetPairCredits(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        return pairCredits.TryGetValue(key, out int value) ? value : 0;
    }

    private void SetPairCredits(string key, int credits)
    {
        if (string.IsNullOrEmpty(key)) return;
        pairCredits[key] = Mathf.Max(0, credits);
    }
}
