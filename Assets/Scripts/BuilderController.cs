using UnityEngine;
using UnityEngine.InputSystem;
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
        SetAllLemsFrozen(true);
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
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
        {
            movement.y = 1;
        }
        else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
        {
            movement.y = -1;
        }
        else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
        {
            movement.x = -1;
        }
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
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
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
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
        var kb = Keyboard.current;
        if (kb != null && kb.bKey.wasPressedThisFrame)
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
        var kb = Keyboard.current;
        if (kb != null && (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame))
        {
            int cursorIndex = GridManager.Instance.GetCurrentCursorIndex();

            // Remove block if present
            BaseBlock block = GridManager.Instance.GetBlockAtIndex(cursorIndex);
            if (block != null && (!block.isPermanent || currentMode == GameMode.Designer))
            {
                block.DestroyBlock();
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
        if (blockInventory == null) return;

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

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftBracketKey.wasPressedThisFrame)
        {
            int prevIndex = currentInventoryIndex - 1;
            if (prevIndex < 0) prevIndex = entries.Count - 1;
            SelectInventoryIndex(prevIndex, entries);
        }
        else if (kb.rightBracketKey.wasPressedThisFrame)
        {
            int nextIndex = currentInventoryIndex + 1;
            if (nextIndex >= entries.Count) nextIndex = 0;
            SelectInventoryIndex(nextIndex, entries);
        }
    }

    private int? GetInventoryIndexFromKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return null;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 0;
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 1;
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 2;
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 3;
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return 4;
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) return 5;
        if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) return 6;
        if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) return 7;
        if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) return 8;
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

    private void HandleLemPlacement()
    {
        if (GridManager.Instance == null) return;

        // L key to place Lem or turn it around if already exists
        var kb = Keyboard.current;
        if (kb != null && kb.lKey.wasPressedThisFrame)
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

        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        grid.RestorePlayModeSnapshot();
        SetMode(GameMode.Builder);
    }

    private void HandleModeToggle()
    {
        // E key toggles between Designer and Builder
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.eKey.wasPressedThisFrame)
        {
            if (currentMode == GameMode.Builder) SetMode(GameMode.Designer);
            else if (currentMode == GameMode.Designer) SetMode(GameMode.Builder);
        }

        // P key toggles Play mode
        if (kb.pKey.wasPressedThisFrame)
        {
            if (currentMode == GameMode.Play)
            {
                ExitPlayMode();
            }
            else
            {
                EnterPlayMode();
            }
        }
    }

    private void EnterPlayMode()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return;
        if (grid.HasTransporterConflicts()) return;

        grid.CapturePlayModeSnapshot();
        SetMode(GameMode.Play);
    }

    private void SetMode(GameMode mode)
    {
        if (currentMode == mode) return;

        currentMode = mode;
        if (gameModeManager == null)
        {
            gameModeManager = ServiceRegistry.Get<GameModeManager>();
        }
        if (mode == GameMode.Designer)
        {
            gameModeManager?.SetEditorMode();
        }
        else if (mode == GameMode.Builder)
        {
            gameModeManager?.SetNormalMode();
        }

        SetAllLemsFrozen(mode != GameMode.Play);
        UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        bool showCursor = currentMode != GameMode.Play;
        grid.SetCursorVisible(showCursor);

        PlaceableSpaceVisualizer visualizer = grid.GetComponent<PlaceableSpaceVisualizer>();
        if (visualizer != null)
        {
            visualizer.SetVisible(showCursor);
        }
    }

    private static void SetAllLemsFrozen(bool frozen)
    {
        LemController[] lems = UnityEngine.Object.FindObjectsByType<LemController>(FindObjectsSortMode.None);
        foreach (LemController lem in lems)
        {
            lem.SetFrozen(frozen);
        }
    }

    private void HandlePlaceableSpaceToggle()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        // Space/Enter to mark space as placeable in Level Builder mode
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
        {
            int cursorIndex = grid.GetCurrentCursorIndex();
            if (!grid.IsSpacePlaceable(cursorIndex))
            {
                grid.SetSpacePlaceable(cursorIndex, true);
            }
        }
    }

    private void HandleSaveLoad()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null || currentMode == GameMode.Play) return;

        // Check for Ctrl/Cmd modifier
        var kb = Keyboard.current;
        if (kb == null) return;

        bool ctrlOrCmd = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed ||
                         kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed;

        // Ctrl/Cmd + S to save
        if (ctrlOrCmd && kb.sKey.wasPressedThisFrame)
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager.CurrentLevelDef == null)
            {
                return;
            }

            LevelData levelData = grid.CaptureLevelData(includePlacedBlocks: false, includeKeyStates: false);
            levelManager.CurrentLevelDef.SaveFromLevelData(levelData);
        }
    }
}
