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
///
/// INVENTORY SOURCE: LevelDefinition assets only.
/// Inventory entries are loaded via LoadInventoryEntries() when LevelManager loads a level.
/// If no level is loaded, a default inventory is created for testing/editor use.
/// </summary>
[ExecuteAlways]
public class BlockInventory : MonoBehaviour
{
    // Runtime inventory entries - loaded from LevelDefinition via LevelManager
    private List<BlockInventoryEntry> entries = new List<BlockInventoryEntry>();

    private readonly Dictionary<string, int> pairCredits = new Dictionary<string, int>();
    private List<BlockInventoryEntry> designerEntries;

    /// <summary>
    /// Fired whenever inventory counts change (use, return, reset, or load).
    /// </summary>
    public event System.Action OnInventoryChanged;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            InitializeInventory();
        }
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            InitializeInventory();
        }
    }

    private void InitializeInventory()
    {
        // Inventory starts empty - it's loaded from LevelDefinition via LoadInventoryEntries()
        NormalizeEntries();
        ResetInventory();
        designerEntries = null;
    }

    /// <summary>
    /// Returns the current runtime inventory entries (read-only).
    /// </summary>
    public IReadOnlyList<BlockInventoryEntry> GetEntries()
    {
        return entries;
    }

    /// <summary>
    /// Returns entries appropriate for the given game mode. Designer mode gets unlimited entries.
    /// </summary>
    public IReadOnlyList<BlockInventoryEntry> GetEntriesForMode(GameMode mode)
    {
        if (mode == GameMode.Designer)
        {
            return GetDesignerEntries();
        }

        return entries;
    }

    /// <summary>
    /// Gets an entry by its list index. Returns null if out of range.
    /// </summary>
    public BlockInventoryEntry GetEntryByIndex(int index)
    {
        if (index < 0 || index >= entries.Count) return null;
        return entries[index];
    }

    /// <summary>
    /// Finds an entry by its unique entry ID string. Returns null if not found.
    /// </summary>
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

    /// <summary>
    /// Returns the first unflavored entry for a block type, or any entry of that type as fallback.
    /// </summary>
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

    /// <summary>
    /// Finds an entry matching the given block type, flavor, route, and inventory key.
    /// Falls back to any matching transporter entry if exact key match fails.
    /// </summary>
    public BlockInventoryEntry FindEntry(BlockType blockType, string flavorId, string[] routeSteps, string inventoryKey)
    {
        string resolvedFlavor = !string.IsNullOrEmpty(flavorId)
            ? flavorId
            : (routeSteps != null && routeSteps.Length > 0 ? string.Join(",", routeSteps) : string.Empty);

        BlockInventoryEntry fallback = null;
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null || entry.blockType != blockType) continue;

            if (!string.IsNullOrEmpty(resolvedFlavor))
            {
                if (entry.GetResolvedFlavorId() != resolvedFlavor) continue;
            }

            if (!string.IsNullOrEmpty(inventoryKey))
            {
                if (GetInventoryKey(entry) == inventoryKey)
                {
                    return entry;
                }

                if (entry.blockType == BlockType.Transporter && fallback == null)
                {
                    fallback = entry;
                }
                continue;
            }

            return entry;
        }

        return fallback;
    }

    /// <summary>
    /// Returns the inventory pool key for an entry (accounts for transporter routes and shared groups).
    /// </summary>
    public string GetInventoryKey(BlockInventoryEntry entry)
    {
        if (entry == null) return string.Empty;
        if (entry.blockType == BlockType.Transporter)
        {
            return GetTransporterInventoryKey(entry);
        }
        if (!string.IsNullOrEmpty(entry.inventoryGroupId)) return entry.inventoryGroupId;
        return entry.GetEntryId();
    }

    /// <summary>
    /// Returns true if the entry (or its shared group) has remaining stock for placement.
    /// </summary>
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

    /// <summary>
    /// Consumes one unit from inventory. For pair entries, consumes a credit first,
    /// then a full pair unit when credits are exhausted. Returns false if empty.
    /// </summary>
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
                DebugLog.Info($"[BlockInventory] Used {groupEntry.GetDisplayName()} (pair credit). Remaining credits: {credits - 1}");
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
            DebugLog.Info($"[BlockInventory] Used {groupEntry.GetDisplayName()} (pair). Remaining pairs: {newCount}/{groupEntry.maxCount}, credits: {newCredits}");
            return true;
        }

        if (groupEntry.currentCount <= 0)
        {
            Debug.LogWarning($"[BlockInventory] Cannot place {groupEntry.GetDisplayName()} - inventory empty");
            return false;
        }

        int remaining = Mathf.Max(0, groupEntry.currentCount - 1);
        SetGroupCount(key, remaining);
        DebugLog.Info($"[BlockInventory] Used {groupEntry.GetDisplayName()}. Remaining: {remaining}/{groupEntry.maxCount}");
        return true;
    }

    /// <summary>
    /// Returns one unit to inventory for the given entry.
    /// </summary>
    public void ReturnBlock(BlockInventoryEntry entry)
    {
        ReturnBlockByKey(GetInventoryKey(entry), entry != null ? entry.blockType : BlockType.Walk);
    }

    /// <summary>
    /// Returns one unit to inventory by key, with a fallback block type for lookup.
    /// </summary>
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
                DebugLog.Info($"[BlockInventory] Returned {groupEntry.GetDisplayName()} (pair credit). Credits: {credits + 1}");
                return;
            }

            int newPairCount = Mathf.Min(groupEntry.maxCount, groupEntry.currentCount + 1);
            SetGroupCount(key, newPairCount);
            SetPairCredits(key, 0);
            DebugLog.Info($"[BlockInventory] Returned {groupEntry.GetDisplayName()} (pair). Available pairs: {newPairCount}/{groupEntry.maxCount}");
            return;
        }

        int newSingleCount = Mathf.Min(groupEntry.maxCount, groupEntry.currentCount + 1);
        SetGroupCount(key, newSingleCount);
        DebugLog.Info($"[BlockInventory] Returned {groupEntry.GetDisplayName()}. Available: {newSingleCount}/{groupEntry.maxCount}");
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
            int count = Mathf.Max(0, groupEntry.currentCount);
            int credits = GetPairCredits(GetInventoryKey(groupEntry));
            // Keep the visible count steady until the pair is completed.
            if (credits > 0)
            {
                count += 1;
            }
            return count;
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

    // --- BlockType convenience overloads (resolve to default entry) ---

    /// <summary>
    /// Checks if a block of the given type can be placed (uses the default entry).
    /// </summary>
    public bool CanPlaceBlock(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return CanPlaceEntry(entry);
    }

    /// <summary>
    /// Consumes one block of the given type from inventory (uses the default entry).
    /// </summary>
    public bool UseBlock(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return UseBlock(entry);
    }

    /// <summary>
    /// Returns one block of the given type to inventory (uses the default entry).
    /// </summary>
    public void ReturnBlock(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        ReturnBlock(entry);
    }

    /// <summary>
    /// Returns the available count for the default entry of the given block type.
    /// </summary>
    public int GetAvailableCount(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return GetAvailableCount(entry);
    }

    /// <summary>
    /// Returns the total (max) count for the default entry of the given block type.
    /// </summary>
    public int GetTotalCount(BlockType blockType)
    {
        BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
        return GetTotalCount(entry);
    }

    /// <summary>
    /// Resets all entries to their max counts and clears pair credits.
    /// </summary>
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
    }

    /// <summary>
    /// Loads inventory entries from a list (typically from LevelData via LevelManager).
    /// Replaces current entries and resets counts.
    /// This is the ONLY way inventory should be populated at runtime.
    /// </summary>
    /// <param name="newEntries">List of entries to load</param>
    public void LoadInventoryEntries(List<BlockInventoryEntry> newEntries)
    {
        if (newEntries == null || newEntries.Count == 0)
        {
            Debug.LogWarning("[BlockInventory] LoadInventoryEntries called with null or empty list");
            return;
        }

        entries.Clear();
        foreach (BlockInventoryEntry entry in newEntries)
        {
            if (entry != null)
            {
                entries.Add(entry.Clone());
            }
        }

        NormalizeEntries();
        ResetInventory();
        designerEntries = null;

        DebugLog.Info($"[BlockInventory] Loaded {entries.Count} inventory entries from level data");
        OnInventoryChanged?.Invoke();
    }

    private IReadOnlyList<BlockInventoryEntry> GetDesignerEntries()
    {
        if (designerEntries != null) return designerEntries;

        designerEntries = new List<BlockInventoryEntry>();
        foreach (BlockType blockType in System.Enum.GetValues(typeof(BlockType)))
        {
            BlockInventoryEntry entry = GetDefaultEntryForBlockType(blockType);
            BlockInventoryEntry clone = entry != null ? entry.Clone() : new BlockInventoryEntry(blockType, 9999);

            clone.displayName = BlockColors.GetBlockTypeName(blockType);
            clone.inventoryGroupId = string.Empty;
            clone.flavorId = string.Empty;
            clone.routeSteps = null;
            clone.isPairInventory = false;
            clone.pairSize = 1;
            clone.maxCount = 9999;
            clone.currentCount = 9999;
            clone.entryId = clone.GetEntryId();

            designerEntries.Add(clone);
        }

        return designerEntries;
    }

    private void NormalizeEntries()
    {
        if (entries == null || entries.Count == 0) return;

        ConsolidateTransporterEntries();

        HashSet<string> usedIds = new HashSet<string>();
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;

            if (string.IsNullOrEmpty(entry.entryId))
            {
                entry.entryId = entry.GetEntryId();
            }

            if (entry.blockType == BlockType.Transporter)
            {
                ValidateTransporterEntry(entry);
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

        designerEntries = null;

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
        OnInventoryChanged?.Invoke();
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
        OnInventoryChanged?.Invoke();
    }

    private static bool IsTransporterEntry(BlockInventoryEntry entry)
    {
        return entry != null && entry.blockType == BlockType.Transporter;
    }

    private static string GetTransporterRouteKey(BlockInventoryEntry entry)
    {
        if (entry == null) return string.Empty;
        string normalized = NormalizeRouteSteps(entry.routeSteps);
        if (!string.IsNullOrEmpty(normalized)) return normalized;
        return string.IsNullOrEmpty(entry.flavorId) ? string.Empty : entry.flavorId.Trim();
    }

    private static string GetTransporterInventoryKey(BlockInventoryEntry entry)
    {
        string routeKey = GetTransporterRouteKey(entry);
        return string.IsNullOrEmpty(routeKey) ? BlockType.Transporter.ToString() : $"{BlockType.Transporter}_{routeKey}";
    }

    private static string NormalizeRouteSteps(string[] routeSteps)
    {
        string[] normalized = RouteParser.NormalizeRouteSteps(routeSteps);
        return normalized != null && normalized.Length > 0 ? string.Join(",", normalized) : string.Empty;
    }

    private void ConsolidateTransporterEntries()
    {
        Dictionary<string, BlockInventoryEntry> grouped = new Dictionary<string, BlockInventoryEntry>();
        List<BlockInventoryEntry> consolidated = new List<BlockInventoryEntry>(entries.Count);

        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null) continue;

            if (!IsTransporterEntry(entry))
            {
                consolidated.Add(entry);
                continue;
            }

            string key = GetTransporterInventoryKey(entry);
            if (!grouped.TryGetValue(key, out BlockInventoryEntry existing))
            {
                consolidated.Add(entry);
                grouped[key] = entry;
                continue;
            }

            MergeTransporterEntry(existing, entry);
        }

        if (consolidated.Count != entries.Count)
        {
            entries = consolidated;
        }
    }

    private static void MergeTransporterEntry(BlockInventoryEntry target, BlockInventoryEntry source)
    {
        if (target == null || source == null) return;

        int added = Mathf.Max(0, source.maxCount);
        target.maxCount += added;

        if (string.IsNullOrEmpty(target.displayName) && !string.IsNullOrEmpty(source.displayName))
        {
            target.displayName = source.displayName;
        }

        if ((target.routeSteps == null || target.routeSteps.Length == 0) && source.routeSteps != null && source.routeSteps.Length > 0)
        {
            target.routeSteps = (string[])source.routeSteps.Clone();
        }

        if (string.IsNullOrEmpty(target.flavorId) && !string.IsNullOrEmpty(source.flavorId))
        {
            target.flavorId = source.flavorId;
        }

        if (target.isPairInventory != source.isPairInventory || target.pairSize != source.pairSize)
        {
            Debug.LogWarning("[BlockInventory] Transporter entries with matching routes have different pair settings. Using the first entry's values.");
        }
    }

    private static void ValidateTransporterEntry(BlockInventoryEntry entry)
    {
        if (entry == null) return;

        RouteParser.RouteData data = RouteParser.ParseRoute(entry.routeSteps, entry.flavorId);
        if (!data.IsValid && !string.IsNullOrEmpty(data.error))
        {
            Debug.LogWarning($"[BlockInventory] Transporter entry '{entry.GetEntryId()}' has invalid route: {data.error}");
        }

        if (data.normalizedSteps != null && data.normalizedSteps.Length > 0)
        {
            entry.routeSteps = data.normalizedSteps;
        }
    }
}
