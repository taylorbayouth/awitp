using UnityEngine;
using System.Collections.Generic;

public class BuilderController : MonoBehaviour
{
    [Header("Block Placement")]
    public BlockType currentBlockType = BlockType.Walk;
    public BlockInventoryEntry currentInventoryEntry;

    [Header("Game Mode")]
    public GameMode currentMode = GameMode.Builder;
    private GameModeManager gameModeManager;
    private BlockInventory blockInventory;
    private int currentInventoryIndex = 0;

    public int CurrentInventoryIndex => currentInventoryIndex;

    private void Awake()
    {
        // ALWAYS start in Builder mode
        currentMode = GameMode.Builder;

        gameModeManager = GetComponent<GameModeManager>();
        if (gameModeManager == null)
        {
            gameModeManager = UnityEngine.Object.FindAnyObjectByType<GameModeManager>();
        }

        // GameModeManager is ensured by GameInitializer; don't add it here.

        // Find BlockInventory
        blockInventory = GetComponent<BlockInventory>();
        if (blockInventory == null)
        {
            blockInventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        }

        InitializeInventorySelection();
        FreezeAllLems();
    }

    private void InitializeInventorySelection()
    {
        if (blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntriesForMode(currentMode);
        if (entries == null || entries.Count == 0) return;

        currentInventoryIndex = Mathf.Clamp(currentInventoryIndex, 0, entries.Count - 1);
        currentInventoryEntry = entries[currentInventoryIndex];
        if (currentInventoryEntry != null)
        {
            currentBlockType = currentInventoryEntry.blockType;
        }
    }

    private void EnsureInventoryReference()
    {
        if (blockInventory != null) return;

        blockInventory = GetComponent<BlockInventory>();
        if (blockInventory == null)
        {
            blockInventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
        }

        if (blockInventory != null)
        {
            InitializeInventorySelection();
        }
    }

    private void EnsureInventorySelectionValid()
    {
        if (blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntriesForMode(currentMode);
        if (entries == null || entries.Count == 0)
        {
            currentInventoryEntry = null;
            return;
        }

        bool found = false;
        int foundIndex = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == currentInventoryEntry)
            {
                found = true;
                foundIndex = i;
                break;
            }
        }

        if (!found && currentInventoryEntry != null)
        {
            // In designer mode the entries list is regenerated, so match by block type instead of reference.
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].blockType == currentInventoryEntry.blockType)
                {
                    foundIndex = i;
                    found = true;
                    break;
                }
            }
        }

        if (found)
        {
            currentInventoryIndex = foundIndex;
            currentInventoryEntry = entries[currentInventoryIndex];
            if (currentInventoryEntry != null)
            {
                currentBlockType = currentInventoryEntry.blockType;
            }
        }
        else
        {
            currentInventoryIndex = Mathf.Clamp(currentInventoryIndex, 0, entries.Count - 1);
            currentInventoryEntry = entries[currentInventoryIndex];
            if (currentInventoryEntry != null)
            {
                currentBlockType = currentInventoryEntry.blockType;
            }
        }
    }

    private void Update()
    {
        // Wait for GridManager to be ready
        if (GridManager.Instance == null) return;

        EnsureInventoryReference();
        EnsureInventorySelectionValid();

        HandleModeToggle();
        HandleSaveLoad();

        // Only handle input when not in Play mode
        if (currentMode != GameMode.Play)
        {
            HandleCursorMovement();
            HandleBlockRemoval();
            HandleInventorySelection();

            if (currentMode == GameMode.Builder)
            {
                HandleBlockPlacement();
            }
            else if (currentMode == GameMode.Designer)
            {
                HandlePlaceableSpaceToggle();
                HandlePermanentBlockPlacement();
                HandleLemPlacement();
            }
        }
    }

    private void HandleCursorMovement()
    {
        if (GridManager.Instance == null) return;

        Vector2Int movement = Vector2Int.zero;

        // Arrow keys or WASD for cursor movement
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            movement.y = 1;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            movement.y = -1;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            movement.x = -1;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            movement.x = 1;
        }

        if (movement != Vector2Int.zero)
        {
            GridManager.Instance.MoveCursor(movement);
        }
    }

    private void HandleBlockPlacement()
    {
        if (GridManager.Instance == null) return;

        // Space or Enter to place block
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            if (currentInventoryEntry != null)
            {
                GridManager.Instance.PlaceBlock(currentInventoryEntry, cursorIndex);
            }
            else
            {
                GridManager.Instance.PlaceBlock(currentBlockType, cursorIndex);
            }
        }
    }

    private void HandlePermanentBlockPlacement()
    {
        if (GridManager.Instance == null) return;

        // B key places a permanent block in Level Builder mode
        if (Input.GetKeyDown(KeyCode.B))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            if (currentInventoryEntry != null)
            {
                GridManager.Instance.PlacePermanentBlock(currentInventoryEntry, cursorIndex);
            }
            else
            {
                GridManager.Instance.PlacePermanentBlock(currentBlockType, cursorIndex);
            }
        }
    }

    private void HandleBlockRemoval()
    {
        if (GridManager.Instance == null) return;

        // Delete or Backspace to remove block AND Lem
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;

            // Remove block if present
            BaseBlock block = GridManager.Instance.GetBlockAtIndex(cursorIndex);
            if (block != null)
            {
                if (block.isPermanent && currentMode != GameMode.Designer)
                {
                    Debug.LogWarning("Cannot remove permanent block in Builder mode");
                }
                else
                {
                    block.DestroyBlock();
                }
            }

            // Remove Lem only in Level Builder mode
            if (currentMode == GameMode.Designer && GridManager.Instance.HasLemAtIndex(cursorIndex))
            {
                GridManager.Instance.RemoveLem(cursorIndex);
            }

            // Remove placeable space marker in Level Builder mode
            if (currentMode == GameMode.Designer && GridManager.Instance.IsSpacePlaceable(cursorIndex))
            {
                GridManager.Instance.SetSpacePlaceable(cursorIndex, false);
            }
        }
    }

    private void HandleInventorySelection()
    {
        if (GridManager.Instance == null || blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntriesForMode(currentMode);
        if (entries == null || entries.Count == 0) return;

        int? requestedIndex = GetInventoryIndexFromKeys();
        if (requestedIndex.HasValue)
        {
            if (requestedIndex.Value >= 0 && requestedIndex.Value < entries.Count)
            {
                SelectInventoryIndex(requestedIndex.Value, entries);
            }
            else
            {
                Debug.LogWarning("No inventory entry assigned to that number key");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            int prevIndex = currentInventoryIndex - 1;
            if (prevIndex < 0) prevIndex = entries.Count - 1;
            SelectInventoryIndex(prevIndex, entries);
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            int nextIndex = currentInventoryIndex + 1;
            if (nextIndex >= entries.Count) nextIndex = 0;
            SelectInventoryIndex(nextIndex, entries);
        }
    }

    private int? GetInventoryIndexFromKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 4;
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) return 5;
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) return 6;
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) return 7;
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) return 8;
        return null;
    }

    private void SelectInventoryIndex(int index, IReadOnlyList<BlockInventoryEntry> entries)
    {
        if (entries == null || entries.Count == 0) return;

        if (index < 0 || index >= entries.Count) return;

        BlockInventoryEntry entry = entries[index];
        if (entry == null) return;

        if (currentMode == GameMode.Builder && !blockInventory.CanPlaceEntry(entry))
        {
            Debug.LogWarning($"Cannot switch to {entry.GetDisplayName()} - no blocks remaining in inventory");
            return;
        }

        currentInventoryIndex = index;
        currentInventoryEntry = entry;
        currentBlockType = entry.blockType;

        // Check if cursor is on an existing block
        int cursorIndex = GridManager.Instance.currentCursorIndex;
        BaseBlock existingBlock = GridManager.Instance.GetBlockAtIndex(cursorIndex);

        if (existingBlock != null)
        {
            if (existingBlock.isPermanent && currentMode != GameMode.Designer)
            {
                Debug.LogWarning("Cannot change permanent block in Builder mode");
                return;
            }

            ChangeBlockType(existingBlock, entry);
        }
    }

    private void ChangeBlockType(BaseBlock block, BlockInventoryEntry entry)
    {
        if (block == null || entry == null) return;

        // Store position and index
        int gridIndex = block.gridIndex;
        bool wasPermanent = block.isPermanent;

        // Remove old block
        block.DestroyBlock();

        // Place new block of different type at same position
        if (wasPermanent)
        {
            GridManager.Instance.PlacePermanentBlock(entry, gridIndex);
        }
        else
        {
            GridManager.Instance.PlaceBlock(entry, gridIndex);
        }
    }

    private void HandleLemPlacement()
    {
        if (GridManager.Instance == null) return;

        // L key to place Lem or turn it around if already exists
        if (Input.GetKeyDown(KeyCode.L))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            GridManager.Instance.PlaceLem(cursorIndex);
        }
    }

    /// <summary>
    /// Exits play mode and restores the level to its pre-play state.
    /// Can be called from external systems (like when Lem dies).
    /// </summary>
    public void ExitPlayMode()
    {
        if (currentMode != GameMode.Play) return;

        currentMode = GameMode.Builder;
        DebugLog.Info("=== EXITED PLAY MODE === Back to Editor");
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RestorePlayModeSnapshot();
        }
        FreezeAllLems();
        UpdateCursorVisibility();
    }

    private void HandleModeToggle()
    {
        // E key toggles between Designer and Editor
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentMode == GameMode.Designer)
            {
                currentMode = GameMode.Builder;
                DebugLog.Info("=== BUILDER MODE === Place blocks");
                if (gameModeManager != null) gameModeManager.SetNormalMode();
                FreezeAllLems();
                UpdateCursorVisibility();
            }
            else if (currentMode == GameMode.Builder)
            {
                currentMode = GameMode.Designer;
                DebugLog.Info("=== LEVEL BUILDER MODE === Place Lem and mark placeable spaces");
                if (gameModeManager != null) gameModeManager.SetEditorMode();
                FreezeAllLems();
                UpdateCursorVisibility();
            }
            else if (currentMode == GameMode.Play)
            {
                // Can't toggle modes during play
                DebugLog.Info("Exit Play mode first (press P)");
            }
        }

        // P key toggles Play mode
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentMode == GameMode.Play)
            {
                ExitPlayMode();
            }
            else
            {
                // Enter play mode from any editor mode
                if (GridManager.Instance != null && GridManager.Instance.HasTransporterConflicts())
                {
                    Debug.LogWarning("Cannot enter Play mode: transporter route blocked by another block.");
                    return;
                }
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.CapturePlayModeSnapshot();
                }
                currentMode = GameMode.Play;
                DebugLog.Info("=== PLAY MODE === Lem is walking!");
                UnfreezeAllLems();
                UpdateCursorVisibility();
            }
        }
    }

    private void UpdateCursorVisibility()
    {
        if (GridManager.Instance == null) return;
        GridManager.Instance.SetCursorVisible(currentMode != GameMode.Play);

        PlaceableSpaceVisualizer visualizer = GridManager.Instance.GetComponent<PlaceableSpaceVisualizer>();
        if (visualizer != null)
        {
            visualizer.SetVisible(currentMode != GameMode.Play);
        }
    }

    private void FreezeAllLems()
    {
        LemController[] lems = UnityEngine.Object.FindObjectsByType<LemController>(FindObjectsSortMode.None);
        foreach (LemController lem in lems)
        {
            lem.SetFrozen(true);
        }
    }

    private void UnfreezeAllLems()
    {
        LemController[] lems = UnityEngine.Object.FindObjectsByType<LemController>(FindObjectsSortMode.None);
        foreach (LemController lem in lems)
        {
            lem.SetFrozen(false);
        }
    }

    private void HandlePlaceableSpaceToggle()
    {
        if (GridManager.Instance == null) return;

        // Space/Enter to mark space as placeable in Level Builder mode
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            if (!GridManager.Instance.IsSpacePlaceable(cursorIndex))
            {
                GridManager.Instance.SetSpacePlaceable(cursorIndex, true);
            }
        }
    }

    private void HandleSaveLoad()
    {
        if (GridManager.Instance == null) return;

        if (currentMode == GameMode.Play)
        {
            Debug.LogWarning("Cannot save/load during Play Mode. Exit Play Mode first.");
            return;
        }

        // Check for Ctrl/Cmd modifier
        bool ctrlOrCmd = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                         Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        // Ctrl/Cmd + S to save
        if (ctrlOrCmd && Input.GetKeyDown(KeyCode.S))
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager == null || levelManager.CurrentLevelDef == null)
            {
                Debug.LogWarning("No LevelDefinition loaded. Load a level via LevelManager before saving.");
                return;
            }

            LevelData levelData = GridManager.Instance.CaptureLevelData(includePlacedBlocks: false, includeKeyStates: false);
            if (levelManager.CurrentLevelDef.SaveFromLevelData(levelData))
            {
                DebugLog.Info("=== LEVEL SAVED === LevelDefinition updated successfully");
            }
            else
            {
                Debug.LogError("Failed to save level to LevelDefinition");
            }
        }

        // Show save location with Ctrl/Cmd + Shift + S
        if (ctrlOrCmd && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager == null || levelManager.CurrentLevelDef == null)
            {
                Debug.LogWarning("No LevelDefinition loaded. Load a level via LevelManager to view its asset path.");
                return;
            }

#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(levelManager.CurrentLevelDef);
            DebugLog.Info($"LevelDefinition asset path: {assetPath}");
#else
            DebugLog.Info("Level data is saved to the LevelDefinition asset (path available in editor only).");
#endif
        }
    }
}
