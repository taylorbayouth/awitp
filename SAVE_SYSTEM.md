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
- `BuilderController.HandleSaveLoad()` checks for Ctrl/Cmd+S.
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

`ProgressManager` persists player progress to disk using a **JSON-based file persistence** system with **event-driven architecture**.

### Architecture

**Singleton Pattern**:
- Uses `ServiceRegistry` fallback for access
- `DontDestroyOnLoad` for scene persistence
- Prevents duplicate instances

**Save Path**:
- Default: `Application.persistentDataPath/progress.json`
- Fallback to local directory if persistentDataPath unavailable
- Initialized in `Awake()` and cached

**Data Model**: `GameProgressData.cs`
```csharp
- completedLevelIds (List<string>)
- unlockedWorldIds (List<string>)
- currentLevelId (string)
- currentWorldId (string)
- totalPlayTime (float)
- lastSaveTimestamp (string, ISO 8601)
- levelProgressList (List<LevelProgress>)
```

**Per-Level Statistics** (`LevelProgress`):
```csharp
- levelId
- completed (bool)
- attempts (int)
- bestTime (float, seconds)
- bestBlockCount (int)
- completionTimestamp (string)
- lastPlayedTimestamp (string)
```

### Save Triggers (Auto-Save System)

| Event | When | Auto-Save |
|-------|------|-----------|
| Level Completion | `MarkLevelComplete()` | Yes (if enabled) |
| World Unlock | `UnlockWorld()` | Yes (if enabled) |
| Level Attempt | Level load | Yes |
| Application Pause | Mobile background | Yes |
| Application Quit | App closing | Yes |
| Manual Save | `SaveProgress()` called | Yes |

**Auto-Save Feature**:
- Configurable `_autoSave` flag (default: true)
- Reduces manual save requirements
- Prevents progress loss on unexpected closure

### Load Triggers

1. **Manager Awake()**: Immediately loads saved progress
2. **Fresh Start**: Creates new `GameProgressData` if file doesn't exist
3. **Corruption Handling**: Validates JSON, creates fresh data on failure

### Error Recovery

**Corruption Detection**:
```
File not found → Create new progress
Empty file → Create new progress
JSON parse failure → Create new progress
Invalid structure → Create new progress
```

**Static Flag Prevention**:
- `_loggedCorruptProgress` prevents log spam
- Only logs first corruption event per session

**Safety**:
- All corrupted saves replaced, never partially loaded
- Original file exists as implicit backup
- All I/O wrapped in try-catch blocks

### Events System

**ProgressManager Events**:
```csharp
OnLevelFirstComplete  → Fired when level completed for first time
OnWorldUnlocked       → Fired when new world becomes available
OnProgressLoaded      → Fired after progress file loaded
OnProgressSaved       → Fired after save completes
```

**Usage Example**:
```csharp
ProgressManager.Instance.OnLevelFirstComplete += (levelId) => {
    Debug.Log($"First time completing {levelId}!");
};
```

**UI Integration**:
- LevelButton queries `IsLevelComplete()` for checkmarks
- LevelSelectUI shows `"{completedLevels}/{totalLevels}"`
- VictoryScreenUI displays stats on `OnLevelComplete`
- WorldMapUI updates on `OnProgressSaved` event

### Data Flow: Level Completion to Save

```
LevelManager.Update()
  → CheckLevelComplete() [win condition]
    → HandleLevelComplete()
      → ProgressManager.MarkLevelComplete(levelId, time, blocks)
        → GameProgressData.MarkLevelCompleted(levelId)
          → Add to completedLevelIds
          → Set completionTimestamp
        → LevelProgress.RecordCompletion(time, blocks)
          → Update bestTime, bestBlockCount
        → Fire OnLevelFirstComplete event
        → CheckWorldUnlocks() [if previous world complete]
          → WorldManager.UnlockWorld()
            → ProgressManager.UnlockWorld(worldId)
              → Fire OnWorldUnlocked
              → SaveProgress() [if autoSave]
        → SaveProgress() [if autoSave]
          → JsonUtility.ToJson() → File.WriteAllText()
          → Fire OnProgressSaved event
```

### ACID Properties

The save system implements database-like reliability:

- **Atomicity**: `File.WriteAllText` is atomic (complete or fails)
- **Consistency**: `JsonUtility` ensures valid JSON structure
- **Isolation**: Single ProgressManager prevents concurrent access
- **Durability**: File persisted to `persistentDataPath`

**No Risk of**:
- Partial saves (file write is atomic)
- Lost levels (timestamps prevent overwrite of better stats)
- Corruption from duplicate saves (idempotent structure)

### Statistics API

**Reading Progress**:
```csharp
ProgressManager.Instance.IsLevelComplete(levelId);           // bool
ProgressManager.Instance.IsWorldUnlocked(worldId);           // bool
ProgressManager.Instance.GetLevelProgress(levelId);          // LevelProgress
ProgressManager.Instance.CompletedLevelCount;                // int
ProgressManager.Instance.GetOverallCompletionPercent();      // float
ProgressManager.Instance.TotalPlayTime;                      // float
ProgressManager.Instance.GetFormattedPlayTime();             // "Xh Ym"
```

**Updating Progress**:
```csharp
ProgressManager.Instance.MarkLevelComplete(levelId, time, blocks);
ProgressManager.Instance.UnlockWorld(worldId);
ProgressManager.Instance.RecordLevelAttempt(levelId);
ProgressManager.Instance.AddPlayTime(deltaTime);
```

**Save/Load Operations**:
```csharp
ProgressManager.Instance.SaveProgress();          // Manual save
ProgressManager.Instance.LoadProgress();          // Manual load
ProgressManager.Instance.ResetProgress();         // New game
ProgressManager.Instance.ExportProgressJson();    // Debug export
```

### Performance Considerations

**Efficient**:
- Save only on significant events (completion, unlock, lifecycle)
- Level attempt recorded on load (one write per level)
- Short-circuit evaluation in completion checks

**Potential Bottlenecks**:
- `WorldManager.IsWorldComplete()` iterates all levels (O(n))
- LevelSelectUI recalculates progress on scene load
- VictoryScreenUI queries on show

### File Format (progress.json)

```json
{
    "completedLevelIds": ["onboarding_0", "tutorial_01"],
    "unlockedWorldIds": ["onboarding", "tutorial"],
    "currentLevelId": "tutorial_01",
    "currentWorldId": "tutorial",
    "totalPlayTime": 1245.5,
    "lastSaveTimestamp": "2026-02-06 14:30:45",
    "levelProgressList": [
        {
            "levelId": "onboarding_0",
            "completed": true,
            "attempts": 1,
            "bestTime": 45.2,
            "bestBlockCount": 5,
            "completionTimestamp": "2026-02-06 10:15:30",
            "lastPlayedTimestamp": "2026-02-06 10:15:30"
        }
    ]
}
```

**Files**: `ProgressManager.cs`, `GameProgressData.cs`, `LevelManager.cs`, `WorldManager.cs`

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
