# Save System Technical Documentation

## Overview

There are three persistence layers in AWITP:

1. **Designer authoring saves (Ctrl/Cmd+S)** - writes the current authored level into a `LevelDefinition` asset (JSON stored in `levelDataJson`).
2. **Player progress saves** - `ProgressManager` stores completion/unlock data in `progress.json`.
3. **Legacy file-based level saves (optional)** - `LevelSaveSystem` writes `LevelData` JSON to disk in `Application.persistentDataPath/Levels/`.

This document describes how each layer works and what data is (and is not) persisted.

---

## Designer Authoring Saves (Ctrl/Cmd+S)

**Purpose**: Save the authored level layout while designing.

**Where it goes**: `LevelDefinition.levelDataJson` (inside the ScriptableObject asset).

**Who uses it**: Designer only. Players never use the save hotkey.

**How it works**:
- `EditorController.HandleSaveLoad()` checks for Ctrl/Cmd+S.
- Requires `LevelManager.CurrentLevelDef` to be assigned.
- Calls `GridManager.CaptureLevelData(includePlacedBlocks: false, includeKeyStates: false)`.
- Writes JSON into the `LevelDefinition` via `SaveFromLevelData()`.
- In editor, Unity saves the asset via `AssetDatabase.SaveAssets()`.

**Saved data**:
- Grid settings (width, height, cellSize)
- Permanent blocks
- Placeable spaces (indices only, excluding cells with permanent blocks)
- Lem placement (grid index + facing + worldPosition)
- Inventory entries (cloned from `BlockInventory`)

**Not saved**:
- Player-placed blocks from Build Mode
- Key/lock runtime state (key pickups, key-in-lock, etc.)
- Runtime block state (crumbler darkening, transporter position, etc.)
- Pair credits (pair inventory resets on load)

**Play Mode rule**: Save/Load is blocked while `GameMode.Play` is active.

---

## Level Loading (Runtime + Editor)

`LevelManager` loads the active `LevelDefinition` on start if `CurrentLevelDef` is assigned.

**Load flow**:

```
LevelManager.Start()
    ↓
LevelDefinition.ToLevelData()
    ↓
GridManager.RestoreLevelData(levelData)
```

**RestoreLevelData behavior**:
- Clears existing blocks, lems, and placeable spaces.
- Loads inventory entries **before** placing blocks (so inventory keys resolve).
- Resizes grid if needed (re-centers and refreshes visuals).
- Restores placeable spaces.
- Places permanent blocks, then placed blocks.
- Places **only the first Lem** from the list (current gameplay supports a single Lem).
- Applies key states (if present).

**Asset defaults**: If the JSON is missing `gridWidth`, `gridHeight`, `cellSize`, or `levelName`, `LevelDefinition.ToLevelData()` fills them from the asset fields.

---

## Play Mode Snapshot

Play mode uses a runtime snapshot to restore the authored layout when exiting Play Mode:

- Enter Play Mode → `GridManager.CapturePlayModeSnapshot()`
- Exit Play Mode → `GridManager.RestorePlayModeSnapshot()`

This snapshot uses **full** `CaptureLevelData()` (includes placed blocks + key states) so runtime changes can be undone.

---

## Player Progress Saves

`ProgressManager` persists player progress to disk:

- File: `Application.persistentDataPath/progress.json`
- Auto-saves on level completion (if `_autoSave`), app pause, and app quit

**Saved data includes**:
- Completed level IDs
- Unlocked world IDs
- Per-level stats (attempts, best time, best blocks)
- Current level/world IDs
- Total play time
- Last save timestamp

---

## Legacy File-Based Level Saves (Optional)

`LevelSaveSystem` can save a `LevelData` JSON file to disk:

- Folder: `Application.persistentDataPath/Levels/`
- File name: `<levelName>.json`
- Used by `GridManager.SaveLevel()` / `GridManager.LoadLevel()`

This is **not** used by the Ctrl/Cmd+S designer workflow, but can be kept for backups or debugging.

---

## File Format (LevelData JSON)

**Example (stored inside a LevelDefinition asset):**

```json
{
  "gridWidth": 10,
  "gridHeight": 10,
  "cellSize": 1.0,
  "inventoryEntries": [
    {
      "entryId": "Teleporter_A",
      "blockType": 1,
      "displayName": "Teleporter A",
      "inventoryGroupId": "",
      "flavorId": "A",
      "routeSteps": null,
      "isPairInventory": true,
      "pairSize": 2,
      "maxCount": 2,
      "currentCount": 2
    }
  ],
  "blocks": [
    {
      "blockType": 0,
      "gridIndex": 45,
      "inventoryKey": "Default",
      "flavorId": "",
      "routeSteps": null
    }
  ],
  "permanentBlocks": [
    {
      "blockType": 2,
      "gridIndex": 12,
      "inventoryKey": "",
      "flavorId": "",
      "routeSteps": null
    }
  ],
  "placeableSpaceIndices": [44, 45, 46, 54, 55, 56],
  "lems": [
    {
      "gridIndex": 45,
      "facingRight": true,
      "worldPosition": {"x": 0.0, "y": 0.5, "z": 0.5},
      "hasWorldPosition": true
    }
  ],
  "keyStates": [
    {
      "sourceKeyBlockIndex": 20,
      "location": 0,
      "targetIndex": -1,
      "worldPosition": {"x": 0.0, "y": 0.0, "z": 0.0},
      "hasWorldPosition": false
    }
  ],
  "levelName": "current_level",
  "saveTimestamp": "2026-01-25 12:34:56"
}
```

### Field Descriptions

**gridWidth / gridHeight / cellSize**: Grid configuration.

**inventoryEntries** (array): Inventory configuration for this level.
- `entryId` (string): Unique ID (auto-generated if missing).
- `blockType` (int): `BlockType` enum value.
- `displayName` (string): Optional UI label.
- `inventoryGroupId` (string): Shared pool id for multiple entries.
- `flavorId` (string): Teleporter/route flavor.
- `routeSteps` (string[]): Transporter steps (L2, U1, etc.).
- `isPairInventory` (bool): If true, `maxCount` is the number of **pairs/units** (not placements).
- `pairSize` (int): Placements per unit (teleporters use 2).
- `maxCount` (int): Total units available for the entry/group.
- `currentCount` (int): Runtime value; **reset to max** on load.

**blocks / permanentBlocks**:
- `blockType`, `gridIndex`, `inventoryKey`, `flavorId`, `routeSteps`.
- `inventoryKey` is used to resolve shared inventory groups.
- Transporter `routeSteps` are normalized on load; entries with the same route are consolidated.

**placeableSpaceIndices**: Indices where `placeable = true` (cells with permanent blocks are excluded).

**lems**:
- `gridIndex`, `facingRight`
- `worldPosition` + `hasWorldPosition` for precise placement (saved for editor consistency)

**keyStates**:
- `sourceKeyBlockIndex`: Original KeyBlock index
- `location`: `KeyBlock`, `LockBlock`, `Lem`, or `World`
- `targetIndex`: Lock/Lem index where applicable
- `worldPosition` + `hasWorldPosition`: Used when a key is in the world

---

## Save Locations

### LevelDefinition asset path (designer saves)
- Press **Ctrl/Cmd + Shift + S** to log the asset path in the console.
- Path is only available in the Unity Editor.

### File-based LevelSaveSystem paths (legacy)

**Windows**:
```
C:\Users\[Username]\AppData\LocalLow\[CompanyName]\[ProductName]\Levels\
```

**macOS**:
```
~/Library/Application Support/[CompanyName]/[ProductName]/Levels/
```

**Linux**:
```
~/.config/unity3d/[CompanyName]/[ProductName]/Levels/
```

**iOS**:
```
Application/[AppGUID]/Documents/Levels/
```

**Android**:
```
/storage/emulated/0/Android/data/[PackageName]/files/Levels/
```

---

## Notes and Gotchas

- **Inventory counts always reset on load**. `currentCount` is serialized but overwritten by `ResetInventory()`.
- **Pair inventory** uses hidden credits. UI shows pair counts, not remaining credits.
- **Only one Lem is restored** even if multiple `lems` entries exist.
- **Placeable space indices** are not saved for cells that have permanent blocks.
- **LevelSaveSystem** is optional; the editor hotkey does not write to disk files.
