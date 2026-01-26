using UnityEngine;
using System.Collections.Generic;

public class EditorController : MonoBehaviour
{
    [Header("Block Placement")]
    public BlockType currentBlockType = BlockType.Default;
    public BlockInventoryEntry currentInventoryEntry;

    [Header("Game Mode")]
    public GameMode currentMode = GameMode.Editor;
    private EditorModeManager editorModeManager;
    private BlockInventory blockInventory;
    private int currentInventoryIndex = 0;

    public int CurrentInventoryIndex => currentInventoryIndex;

    private void Awake()
    {
        // ALWAYS start in Editor mode
        currentMode = GameMode.Editor;

        editorModeManager = GetComponent<EditorModeManager>();
        if (editorModeManager == null)
        {
            editorModeManager = FindObjectOfType<EditorModeManager>();
        }

        // If still not found, add it to this GameObject
        if (editorModeManager == null)
        {
            editorModeManager = gameObject.AddComponent<EditorModeManager>();
        }

        // Find BlockInventory
        blockInventory = GetComponent<BlockInventory>();
        if (blockInventory == null)
        {
            blockInventory = FindObjectOfType<BlockInventory>();
        }

        InitializeInventorySelection();
    }

    private void InitializeInventorySelection()
    {
        if (blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntries();
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
            blockInventory = FindObjectOfType<BlockInventory>();
        }

        if (blockInventory != null)
        {
            InitializeInventorySelection();
        }
    }

    private void EnsureInventorySelectionValid()
    {
        if (blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntries();
        if (entries == null || entries.Count == 0)
        {
            currentInventoryEntry = null;
            return;
        }

        bool found = false;
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == currentInventoryEntry)
            {
                found = true;
                break;
            }
        }

        if (!found)
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

            if (currentMode == GameMode.Editor)
            {
                HandleBlockPlacement();
            }
            else if (currentMode == GameMode.LevelEditor)
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
                Debug.Log($"Placed {currentInventoryEntry.GetDisplayName()} block at index {cursorIndex}");
            }
            else
            {
                GridManager.Instance.PlaceBlock(currentBlockType, cursorIndex);
                Debug.Log($"Placed {currentBlockType} block at index {cursorIndex}");
            }
        }
    }

    private void HandlePermanentBlockPlacement()
    {
        if (GridManager.Instance == null) return;

        // B key places a permanent block in Level Editor mode
        if (Input.GetKeyDown(KeyCode.B))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            if (currentInventoryEntry != null)
            {
                GridManager.Instance.PlacePermanentBlock(currentInventoryEntry, cursorIndex);
                Debug.Log($"Placed permanent {currentInventoryEntry.GetDisplayName()} block at index {cursorIndex}");
            }
            else
            {
                GridManager.Instance.PlacePermanentBlock(currentBlockType, cursorIndex);
                Debug.Log($"Placed permanent {currentBlockType} block at index {cursorIndex}");
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
                if (block.isPermanent && currentMode != GameMode.LevelEditor)
                {
                    Debug.LogWarning("Cannot remove permanent block in Editor mode");
                }
                else
                {
                    block.DestroyBlock();
                    Debug.Log($"Removed block at index {cursorIndex}");
                }
            }

            // Remove Lem if present
            if (GridManager.Instance.HasLemAtIndex(cursorIndex))
            {
                GridManager.Instance.RemoveLem(cursorIndex);
                Debug.Log($"Removed Lem at index {cursorIndex}");
            }

            // Remove placeable space marker in Level Editor mode
            if (currentMode == GameMode.LevelEditor && GridManager.Instance.IsSpacePlaceable(cursorIndex))
            {
                GridManager.Instance.SetSpacePlaceable(cursorIndex, false);
                Debug.Log($"Removed placeable space at index {cursorIndex}");
            }
        }
    }

    private void HandleInventorySelection()
    {
        if (GridManager.Instance == null || blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntries();
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

        if (currentMode == GameMode.Editor && !blockInventory.CanPlaceEntry(entry))
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
            if (existingBlock.isPermanent && currentMode != GameMode.LevelEditor)
            {
                Debug.LogWarning("Cannot change permanent block in Editor mode");
                return;
            }

            ChangeBlockType(existingBlock, entry);
            Debug.Log($"Changed block at index {cursorIndex} to {entry.GetDisplayName()}");
        }
        else
        {
            Debug.Log($"Switched to {entry.GetDisplayName()} block");
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

    private void HandleModeToggle()
    {
        // E key toggles between Level Editor and Editor
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentMode == GameMode.LevelEditor)
            {
                currentMode = GameMode.Editor;
                Debug.Log("=== EDITOR MODE === Place blocks");
                if (editorModeManager != null) editorModeManager.SetNormalMode();
                UpdateCursorVisibility();
            }
            else if (currentMode == GameMode.Editor)
            {
                currentMode = GameMode.LevelEditor;
                Debug.Log("=== LEVEL EDITOR MODE === Place Lem and mark placeable spaces");
                if (editorModeManager != null) editorModeManager.SetEditorMode();
                UpdateCursorVisibility();
            }
            else if (currentMode == GameMode.Play)
            {
                // Can't toggle modes during play
                Debug.Log("Exit Play mode first (press P)");
            }
        }

        // P key toggles Play mode
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentMode == GameMode.Play)
            {
                // Exit play mode, return to Editor
                currentMode = GameMode.Editor;
                Debug.Log("=== EXITED PLAY MODE === Back to Editor");
                // Reset Lems to original Level Editor positions
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.ResetAllLems();
                    GridManager.Instance.RestoreOriginalKeyStates();
                }
                UpdateCursorVisibility();
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
                    GridManager.Instance.CaptureOriginalKeyStates();
                }
                currentMode = GameMode.Play;
                Debug.Log("=== PLAY MODE === Lem is walking!");
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
        LemController[] lems = FindObjectsOfType<LemController>();
        foreach (LemController lem in lems)
        {
            lem.SetFrozen(true);
        }
    }

    private void UnfreezeAllLems()
    {
        LemController[] lems = FindObjectsOfType<LemController>();
        foreach (LemController lem in lems)
        {
            lem.SetFrozen(false);
        }
    }

    private void HandlePlaceableSpaceToggle()
    {
        if (GridManager.Instance == null) return;

        // Space/Enter to mark space as placeable in Level Editor mode
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            if (!GridManager.Instance.IsSpacePlaceable(cursorIndex))
            {
                GridManager.Instance.SetSpacePlaceable(cursorIndex, true);
                Debug.Log($"Marked space at index {cursorIndex} as placeable");
            }
        }
    }

    private void HandleSaveLoad()
    {
        if (GridManager.Instance == null) return;

        // Check for Ctrl/Cmd modifier
        bool ctrlOrCmd = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                         Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        // Ctrl/Cmd + S to save
        if (ctrlOrCmd && Input.GetKeyDown(KeyCode.S))
        {
            if (GridManager.Instance.SaveLevel())
            {
                Debug.Log("=== LEVEL SAVED === Level saved successfully");
            }
            else
            {
                Debug.LogError("Failed to save level");
            }
        }

        // Ctrl/Cmd + L to load
        if (ctrlOrCmd && Input.GetKeyDown(KeyCode.L))
        {
            if (LevelSaveSystem.LevelExists())
            {
                if (GridManager.Instance.LoadLevel())
                {
                    Debug.Log("=== LEVEL LOADED === Level loaded successfully");
                    // Refresh the visualizer after loading
                    PlaceableSpaceVisualizer visualizer = FindObjectOfType<PlaceableSpaceVisualizer>();
                    if (visualizer != null)
                    {
                        visualizer.RefreshVisuals();
                    }
                }
                else
                {
                    Debug.LogError("Failed to load level");
                }
            }
            else
            {
                Debug.LogWarning("No saved level found. Save a level first with Ctrl+S");
            }
        }

        // Show save location with Ctrl/Cmd + Shift + S
        if (ctrlOrCmd && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
        {
            string savePath = LevelSaveSystem.GetSavePath();
            Debug.Log($"Levels are saved to: {savePath}");
        }
    }
}
