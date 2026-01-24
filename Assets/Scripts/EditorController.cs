using UnityEngine;

public class EditorController : MonoBehaviour
{
    [Header("Block Placement")]
    public BlockType currentBlockType = BlockType.Default;

    private void Update()
    {
        HandleCursorMovement();
        HandleBlockPlacement();
        HandleBlockRemoval();
        HandleBlockTypeSwitch();
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
        // Number keys to switch block type
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            currentBlockType = BlockType.Default;
            Debug.Log("Switched to Default block");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            currentBlockType = BlockType.Teleporter;
            Debug.Log("Switched to Teleporter block");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            currentBlockType = BlockType.Crumbler;
            Debug.Log("Switched to Crumbler block");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            currentBlockType = BlockType.Transporter;
            Debug.Log("Switched to Transporter block");
        }
    }
}
