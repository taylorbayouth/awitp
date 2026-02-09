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
            gameModeManager = ServiceRegistry.Get<GameModeManager>();
        }

        // GameModeManager is ensured by GameInitializer; don't add it here.

        // Find BlockInventory
        blockInventory = GetComponent<BlockInventory>();
        if (blockInventory == null)
        {
            blockInventory = ServiceRegistry.Get<BlockInventory>();
        }

        InitializeInventorySelection();
        FreezeAllLems();
    }

    private void InitializeInventorySelection()
    {
        if (blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntriesForMode(currentMode);
        if (entries == null || entries.Count == 0)
        {
            currentInventoryIndex = -1;
            currentInventoryEntry = null;
            return;
        }

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
            blockInventory = ServiceRegistry.Get<BlockInventory>();
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
            // Match by block type AND flavor/entryId so selection remains stable across mode changes.
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null || entries[i].blockType != currentInventoryEntry.blockType) continue;

                // Match by entryId first (most specific)
                if (!string.IsNullOrEmpty(currentInventoryEntry.entryId) &&
                    entries[i].entryId == currentInventoryEntry.entryId)
                {
                    foundIndex = i;
                    found = true;
                    break;
                }

                // Match by flavorId for flavored blocks (teleporters)
                if (!string.IsNullOrEmpty(currentInventoryEntry.flavorId) &&
                    entries[i].flavorId == currentInventoryEntry.flavorId)
                {
                    foundIndex = i;
                    found = true;
                    break;
                }
            }

            // If no flavor match, fall back to first entry of same type (for unflavored blocks)
            if (!found)
            {
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
            // Defensive check: ensure entries is not empty before accessing
            if (entries.Count == 0)
            {
                currentInventoryIndex = -1;
                currentInventoryEntry = null;
                return;
            }

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
            HandlePointerCursorMovement();
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

    private void HandlePointerCursorMovement()
    {
        if (GridManager.Instance == null) return;

        if (!PointerInput.TryGetPrimaryPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            return;
        }

        if (PointerInput.IsPointerOverUI(screenPosition, pointerId))
        {
            return;
        }

        if (!TryGetGridIndexFromScreen(screenPosition, out int index)) return;

        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        int currentIndex = grid.GetCurrentCursorIndex();
        if (index == currentIndex)
        {
            if (currentMode == GameMode.Builder && grid.IsSpacePlaceable(index))
            {
                TryPlaceBlockAt(index);
            }
        }
        else
        {
            grid.SetCursorIndex(index);
        }
    }

    private void HandleBlockPlacement()
    {
        if (GridManager.Instance == null) return;

        // Space or Enter to place block
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int cursorIndex = GridManager.Instance.GetCurrentCursorIndex();
            TryPlaceBlockAt(cursorIndex);
        }
    }

    private void TryPlaceBlockAt(int index)
    {
        if (GridManager.Instance == null) return;

        if (currentInventoryEntry != null)
        {
            GridManager.Instance.PlaceBlock(currentInventoryEntry, index);
        }
        else
        {
            GridManager.Instance.PlaceBlock(currentBlockType, index);
        }
    }

    private void HandlePermanentBlockPlacement()
    {
        if (GridManager.Instance == null) return;

        // B key places a permanent block in Level Builder mode
        if (Input.GetKeyDown(KeyCode.B))
        {
            int cursorIndex = GridManager.Instance.GetCurrentCursorIndex();
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
            int cursorIndex = GridManager.Instance.GetCurrentCursorIndex();

            // Remove block if present
            BaseBlock block = GridManager.Instance.GetBlockAtIndex(cursorIndex);
            if (block != null)
            {
                if (block.isPermanent && currentMode != GameMode.Designer)
                {
                    // Cannot remove permanent block in Builder mode
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
            return;
        }

        currentInventoryIndex = index;
        currentInventoryEntry = entry;
        currentBlockType = entry.blockType;
    }

    public void SelectInventoryEntry(BlockInventoryEntry entry)
    {
        if (entry == null) return;
        if (currentMode == GameMode.Play) return;

        EnsureInventoryReference();
        if (blockInventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = blockInventory.GetEntriesForMode(currentMode);
        if (entries == null || entries.Count == 0) return;

        int index = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == entry)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            string entryId = entry.GetEntryId();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].GetEntryId() == entryId)
                {
                    index = i;
                    break;
                }
            }
        }

        if (index >= 0)
        {
            SelectInventoryIndex(index, entries);
        }
    }

    private bool TryGetGridIndexFromScreen(Vector2 screenPosition, out int index)
    {
        index = -1;
        GridManager grid = GridManager.Instance;
        if (grid == null) return false;

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = ServiceRegistry.Get<Camera>();
        }
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        Plane gridPlane = new Plane(Vector3.forward, Vector3.zero);
        if (!gridPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 worldPos = ray.GetPoint(enter);
        Vector3 localPos = worldPos - grid.gridOrigin;
        int x = Mathf.FloorToInt(localPos.x);
        int y = Mathf.FloorToInt(localPos.y);

        if (x < 0 || x >= grid.gridWidth || y < 0 || y >= grid.gridHeight)
        {
            return false;
        }

        index = grid.CoordinatesToIndex(x, y);
        return true;
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
            int cursorIndex = GridManager.Instance.GetCurrentCursorIndex();
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
                if (gameModeManager != null) gameModeManager.SetNormalMode();
                FreezeAllLems();
                UpdateCursorVisibility();
            }
            else if (currentMode == GameMode.Builder)
            {
                currentMode = GameMode.Designer;
                if (gameModeManager != null) gameModeManager.SetEditorMode();
                FreezeAllLems();
                UpdateCursorVisibility();
            }
            else if (currentMode == GameMode.Play)
            {
                // Can't toggle modes during play
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
                // Enter play mode from any Designer mode
                if (GridManager.Instance != null && GridManager.Instance.HasTransporterConflicts())
                {
                    return;
                }
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.CapturePlayModeSnapshot();
                }
                currentMode = GameMode.Play;
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
            int cursorIndex = GridManager.Instance.GetCurrentCursorIndex();
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
                return;
            }

            LevelData levelData = GridManager.Instance.CaptureLevelData(includePlacedBlocks: false, includeKeyStates: false);
            levelManager.CurrentLevelDef.SaveFromLevelData(levelData);
        }
    }
}
