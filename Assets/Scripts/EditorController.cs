using UnityEngine;

public class EditorController : MonoBehaviour
{
    [Header("Block Placement")]
    public BlockType currentBlockType = BlockType.Default;

    [Header("Game Mode")]
    public GameMode currentMode = GameMode.Editor;
    private EditorModeManager editorModeManager;
    private BlockInventory blockInventory;

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
    }

    private void Update()
    {
        // Wait for GridManager to be ready
        if (GridManager.Instance == null) return;

        HandleModeToggle();
        HandleSaveLoad();

        // Only handle input when not in Play mode
        if (currentMode != GameMode.Play)
        {
            HandleCursorMovement();
            HandleBlockRemoval();
            HandleBlockTypeSwitch();

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
            GridManager.Instance.PlaceBlock(currentBlockType, cursorIndex);
            Debug.Log($"Placed {currentBlockType} block at index {cursorIndex}");
        }
    }

    private void HandlePermanentBlockPlacement()
    {
        if (GridManager.Instance == null) return;

        // B key places a permanent block in Level Editor mode
        if (Input.GetKeyDown(KeyCode.B))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            GridManager.Instance.PlacePermanentBlock(currentBlockType, cursorIndex);
            Debug.Log($"Placed permanent {currentBlockType} block at index {cursorIndex}");
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
                block.DestroyBlock();
                Debug.Log($"Removed block at index {cursorIndex}");
            }

            // Remove Lem if present
            if (GridManager.Instance.HasLemAtIndex(cursorIndex))
            {
                GridManager.Instance.RemoveLem(cursorIndex);
                Debug.Log($"Removed Lem at index {cursorIndex}");
            }
        }
    }

    private void HandleBlockTypeSwitch()
    {
        if (GridManager.Instance == null) return;

        BlockType? newBlockType = null;

        // Number keys to switch block type
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            newBlockType = BlockType.Default;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            newBlockType = BlockType.Teleporter;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            newBlockType = BlockType.Crumbler;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            newBlockType = BlockType.Transporter;
        }

        if (newBlockType.HasValue)
        {
            // Check if this block type is available in inventory (Editor mode only)
            if (currentMode == GameMode.Editor && blockInventory != null && !blockInventory.CanPlaceBlock(newBlockType.Value))
            {
                Debug.LogWarning($"Cannot switch to {newBlockType.Value} - no blocks remaining in inventory");
                return;
            }

            currentBlockType = newBlockType.Value;

            // Check if cursor is on an existing block
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            BaseBlock existingBlock = GridManager.Instance.GetBlockAtIndex(cursorIndex);

            if (existingBlock != null)
            {
                // Change the block type of existing block
                ChangeBlockType(existingBlock, currentBlockType);
                Debug.Log($"Changed block at index {cursorIndex} to {currentBlockType}");
            }
            else
            {
                Debug.Log($"Switched to {currentBlockType} block");
            }
        }
    }

    private void ChangeBlockType(BaseBlock block, BlockType newType)
    {
        if (block == null) return;

        // Store position and index
        int gridIndex = block.gridIndex;
        bool wasPermanent = block.isPermanent;

        // Remove old block
        block.DestroyBlock();

        // Place new block of different type at same position
        if (wasPermanent)
        {
            GridManager.Instance.PlacePermanentBlock(newType, gridIndex);
        }
        else
        {
            GridManager.Instance.PlaceBlock(newType, gridIndex);
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
                }
                UpdateCursorVisibility();
            }
            else
            {
                // Enter play mode from any editor mode
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

        // Space/Enter to toggle placeable state in Level Editor mode
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            GridManager.Instance.ToggleSpacePlaceable(cursorIndex);

            bool isPlaceable = GridManager.Instance.IsSpacePlaceable(cursorIndex);
            Debug.Log($"Space at index {cursorIndex} is now {(isPlaceable ? "placeable" : "non-placeable")}");
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
