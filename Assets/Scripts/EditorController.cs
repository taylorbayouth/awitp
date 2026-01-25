using UnityEngine;

public class EditorController : MonoBehaviour
{
    [Header("Block Placement")]
    public BlockType currentBlockType = BlockType.Default;

    [Header("Editor Mode")]
    public bool isEditingPlaceableSpaces = false;
    private EditorModeManager editorModeManager;
    private BlockInventory blockInventory;

    private void Awake()
    {
        editorModeManager = GetComponent<EditorModeManager>();
        if (editorModeManager == null)
        {
            editorModeManager = FindObjectOfType<EditorModeManager>();
        }

        // If still not found, add it to this GameObject
        if (editorModeManager == null)
        {
            Debug.LogWarning("EditorController: EditorModeManager not found, adding it now!");
            editorModeManager = gameObject.AddComponent<EditorModeManager>();
        }

        // Find BlockInventory
        blockInventory = GetComponent<BlockInventory>();
        if (blockInventory == null)
        {
            blockInventory = FindObjectOfType<BlockInventory>();
        }

        Debug.Log($"EditorController: EditorModeManager reference set: {editorModeManager != null}");
        Debug.Log($"EditorController: BlockInventory reference set: {blockInventory != null}");
    }

    private void Update()
    {
        HandleCursorMovement();
        HandleBlockPlacement();
        HandleBlockRemoval();
        HandleBlockTypeSwitch();
        HandlePlaceableSpaceToggle();
        HandleEditorModeToggle();
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
        if (GridManager.Instance == null || isEditingPlaceableSpaces) return;

        // Space or Enter to place block
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            GridManager.Instance.PlaceBlock(currentBlockType, cursorIndex);
            Debug.Log($"Placed {currentBlockType} block at index {cursorIndex}");
        }
    }

    private void HandleBlockRemoval()
    {
        if (GridManager.Instance == null) return;

        // Delete or Backspace to remove block
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
        {
            int cursorIndex = GridManager.Instance.currentCursorIndex;
            BaseBlock block = GridManager.Instance.GetBlockAtIndex(cursorIndex);

            if (block != null)
            {
                block.DestroyBlock();
                Debug.Log($"Removed block at index {cursorIndex}");
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
            // Check if this block type is available in inventory
            if (blockInventory != null && !blockInventory.CanPlaceBlock(newBlockType.Value))
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

        // Remove old block
        block.DestroyBlock();

        // Place new block of different type at same position
        GridManager.Instance.PlaceBlock(newType, gridIndex);
    }

    private void HandleEditorModeToggle()
    {
        // E key to toggle editor mode (Tab can be problematic in Unity)
        if (Input.GetKeyDown(KeyCode.E))
        {
            isEditingPlaceableSpaces = !isEditingPlaceableSpaces;
            Debug.Log($"=== E KEY PRESSED === Editor mode toggled to: {isEditingPlaceableSpaces}");
            Debug.Log($"Editor mode: {(isEditingPlaceableSpaces ? "Editing placeable spaces" : "Placing blocks")}");

            // Directly notify EditorModeManager
            if (editorModeManager != null)
            {
                if (isEditingPlaceableSpaces)
                {
                    editorModeManager.SetEditorMode();
                }
                else
                {
                    editorModeManager.SetNormalMode();
                }
            }
            else
            {
                Debug.LogWarning("EditorController: EditorModeManager not found!");
            }
        }
    }

    private void HandlePlaceableSpaceToggle()
    {
        if (GridManager.Instance == null) return;

        // When in editor mode for placeable spaces
        if (isEditingPlaceableSpaces)
        {
            // Space/Enter to toggle placeable state
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                int cursorIndex = GridManager.Instance.currentCursorIndex;
                GridManager.Instance.ToggleSpacePlaceable(cursorIndex);

                bool isPlaceable = GridManager.Instance.IsSpacePlaceable(cursorIndex);
                Debug.Log($"Space at index {cursorIndex} is now {(isPlaceable ? "placeable" : "non-placeable")}");
            }
        }
    }
}
