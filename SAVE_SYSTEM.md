# Save System Technical Documentation

## Overview

The save system provides persistent level storage using JSON serialization. It captures all editor state (blocks, permanent blocks, placeable spaces, Lem placements) and can restore it exactly, even after Unity is closed.

## Architecture

### Components

#### LevelData.cs
Serializable data structure that represents a complete level state.

**Key Classes**:
- `LevelData` - Root container for all level data
- `LevelData.BlockData` - Stores block type and grid position
- `LevelData.LemData` - Stores Lem position and facing direction

**Data Stored**:
- Grid dimensions (width, height, cellSize)
- Block placements (type and index for each block)
- Permanent block placements (type and index for each block)
- Placeable spaces (list of indices where blocks can be placed)
- Lem placements (position and facing direction)
- Inventory entries (block flavors, counts, grouping)
- Key states (location: World, KeyBlock, Lem, or LockBlock)
- Metadata (level name, save timestamp)

#### LevelSaveSystem.cs
Static utility class handling file I/O operations.

**Key Methods**:
- `SaveLevel(levelData, levelName)` - Writes level to JSON file
- `LoadLevel(levelName)` - Reads level from JSON file
- `LevelExists(levelName)` - Checks if save file exists
- `DeleteLevel(levelName)` - Removes save file
- `GetSavePath()` - Returns save directory path

**File Management**:
- Uses `Application.persistentDataPath` for cross-platform compatibility
- Creates `Levels/` subdirectory automatically
- Default filename: `current_level.json`
- Pretty-printed JSON for human readability

#### GridManager.cs (Save/Load Methods)
Integration layer between the save system and game state.

**Key Methods**:
- `CaptureLevelData()` - Creates LevelData from current grid state
- `RestoreLevelData(levelData)` - Applies LevelData to grid
- `SaveLevel(levelName)` - High-level save operation
- `LoadLevel(levelName)` - High-level load operation
- `ClearAllBlocks()` - Removes all placed blocks
- `ClearAllLems()` - Removes all placed Lems
- `ClearPlaceableSpaces()` - Resets placeable space array

## Data Flow

### Saving a Level

```
User presses Ctrl+S
    ↓
EditorController.HandleSaveLoad()
    ↓
GridManager.SaveLevel()
    ↓
GridManager.CaptureLevelData()
    ├─ Capture BlockInventory entries → LevelData.inventoryEntries
    ├─ Iterate permanentBlocks dictionary → LevelData.permanentBlocks
    ├─ Iterate placedBlocks dictionary → LevelData.blocks
    ├─ Iterate placeableSpaces array → LevelData.placeableSpaceIndices
    └─ Iterate originalLemPlacements → LevelData.lems
    ↓
LevelSaveSystem.SaveLevel(levelData)
    ├─ Convert LevelData to JSON with JsonUtility.ToJson()
    ├─ Get file path from Application.persistentDataPath
    └─ Write JSON to file with File.WriteAllText()
    ↓
Success/failure reported to console
```

### Loading a Level

```
User presses Ctrl+L
    ↓
EditorController.HandleSaveLoad()
    ↓
LevelSaveSystem.LevelExists()
    ↓ (if exists)
GridManager.LoadLevel()
    ↓
LevelSaveSystem.LoadLevel()
    ├─ Read JSON from file with File.ReadAllText()
    └─ Convert JSON to LevelData with JsonUtility.FromJson()
    ↓
GridManager.RestoreLevelData(levelData)
    ├─ ClearAllBlocks() - remove existing blocks
    ├─ ClearAllLems() - remove existing Lems
    ├─ ClearPlaceableSpaces() - reset placeable array
    ├─ Apply inventory entries (if present)
    ├─ Check if grid size changed → reinitialize if needed
    ├─ Restore placeableSpaces array from indices
    ├─ PlacePermanentBlock() for each saved permanent block
    ├─ PlaceBlock() for each saved block
    └─ PlaceLem() for each saved Lem
    ↓
Refresh visuals and update cursor state
    ↓
Success/failure reported to console
```

## File Format

### JSON Structure

```json
{
  "gridWidth": 10,
  "gridHeight": 10,
  "cellSize": 1.0,
  "inventoryEntries": [
    {
      "entryId": "Default",
      "blockType": 0,
      "displayName": "Default",
      "inventoryGroupId": "",
      "flavorId": "",
      "routeSteps": null,
      "maxCount": 999,
      "currentCount": 999
    },
    {
      "entryId": "Teleporter_A",
      "blockType": 1,
      "displayName": "Teleporter A",
      "inventoryGroupId": "Teleporter",
      "flavorId": "A",
      "routeSteps": null,
      "maxCount": 2,
      "currentCount": 2
    }
  ],
  "blocks": [
    {
      "blockType": 0,
      "gridIndex": 45,
      "inventoryKey": "Default",
      "flavorId": ""
    },
    {
      "blockType": 1,
      "gridIndex": 46,
      "inventoryKey": "Teleporter",
      "flavorId": "A"
    }
  ],
  "permanentBlocks": [
    {
      "blockType": 2,
      "gridIndex": 12,
      "inventoryKey": "",
      "flavorId": ""
    }
  ],
  "placeableSpaceIndices": [
    44, 45, 46, 54, 55, 56, 64, 65, 66
  ],
  "lems": [
    {
      "gridIndex": 45,
      "facingRight": true
    }
  ],
  "levelName": "current_level",
  "saveTimestamp": "2026-01-25 12:34:56"
}
```

### Field Descriptions

**gridWidth** (int): Number of columns in the grid
**gridHeight** (int): Number of rows in the grid
**cellSize** (float): Size of each grid cell in world units

**inventoryEntries** (array): Inventory configuration for this level
- `entryId` (string): Unique id for the entry
- `blockType` (int): BlockType enum value
- `displayName` (string): Optional UI label
- `inventoryGroupId` (string): Optional shared inventory group id
- `flavorId` (string): Optional flavor identifier (e.g., Teleporter letter)
- `routeSteps` (string[]): Optional transporter route steps
- `maxCount` (int): Total number of blocks for this entry/group
- `currentCount` (int): Current count (reset to max on load)

**blocks** (array): List of all placed blocks
- `blockType` (int): BlockType enum value (0=Default, 1=Teleporter, 2=Crumbler, 3=Transporter, 4=Key, 5=Lock)
- `gridIndex` (int): Linear index in grid (0 to width*height-1)
- `inventoryKey` (string): Inventory group key used for counts
- `flavorId` (string): Flavor identifier (e.g., A, L3,U1)
- `routeSteps` (string[]): Transporter-only route steps (optional)

**permanentBlocks** (array): List of designer-placed permanent blocks
- `blockType` (int): BlockType enum value (0=Default, 1=Teleporter, 2=Crumbler, 3=Transporter, 4=Key, 5=Lock)
- `gridIndex` (int): Linear index in grid (0 to width*height-1)
- `inventoryKey` (string): Inventory group key (optional)
- `flavorId` (string): Flavor identifier (optional)
- `routeSteps` (string[]): Transporter-only route steps (optional)

**keyStates** (array): Tracks the location of each key in the level
- `keyLocation` (enum): Where the key is (0=World, 1=KeyBlock, 2=Lem, 3=LockBlock)
- `sourceKeyBlockIndex` (int): Original KeyBlock grid index
- `lockBlockIndex` (int): If locked, the LockBlock grid index

**placeableSpaceIndices** (array): Grid indices where blocks can be placed during gameplay
- Only stores indices where placeable = true
- Empty array = no spaces are placeable

**lems** (array): List of all placed Lems
- `gridIndex` (int): Starting position in grid
- `facingRight` (bool): Initial facing direction (true = right, false = left)

**levelName** (string): Name of the level (defaults to "current_level")
**saveTimestamp** (string): ISO format timestamp when level was saved

## Save Location

### Platform-Specific Paths

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

### Accessing the Save Directory

At runtime:
```csharp
string savePath = LevelSaveSystem.GetSavePath();
Debug.Log($"Levels saved to: {savePath}");
```

In the editor, press **Ctrl+Shift+S** to log the path to console.

## Implementation Details

### Serialization Strategy

**Why JSON?**
- Human-readable and easily debuggable
- Supported natively by Unity (JsonUtility)
- Can be manually edited if needed
- Compatible with version control systems
- Easy to extend with new fields

**Limitations**:
- JsonUtility doesn't support dictionaries directly
- Must convert to lists/arrays for serialization
- No built-in versioning (must handle manually if format changes)

### Data Optimization

**Placeable Spaces**:
- Stores only indices where `placeable = true`
- Avoids storing large arrays of boolean flags
- Reconstruction: Set all to false, then mark stored indices as true

**Block Data**:
- Stores only placed blocks (not empty grid cells)
- Uses grid index instead of x,y coordinates (more compact)
- Enum stored as integer for compatibility

**Lem Data**:
- Uses `originalLemPlacements` instead of `placedLems`
- Ensures editor state is saved, not runtime state
- Preserves initial facing direction for level design

### Error Handling

**Save Failures**:
- Invalid path permissions → logged to console
- Disk full → logged to console
- Serialization errors → caught and logged
- Returns `false` on failure

**Load Failures**:
- File not found → returns `null`, warning logged
- Invalid JSON format → returns `null`, error logged
- Missing required fields → handled by JsonUtility (uses defaults)
- Returns `null` on failure

**Recovery**:
- Failed loads don't corrupt current level state
- Original level remains intact if load fails
- User can retry or create new level

### Grid Size Changes

When loading a level with different grid dimensions:
1. Update `gridWidth`, `gridHeight`, `cellSize`
2. Recalculate grid origin (recenters around world origin)
3. Reinitialize placeable spaces array
4. Refresh all visualizers
5. Update camera to frame new grid size

Blocks and Lems are validated against new grid size:
- Invalid indices are skipped with a warning
- Only valid placements are restored

### Thread Safety

**Not Thread-Safe**:
- All save/load operations run on main Unity thread
- File I/O is synchronous (blocks until complete)
- No concurrent access protection needed

**Future Consideration**:
- Could be made async with `async/await`
- Would improve responsiveness for large levels
- Requires callback system for completion notification

## Usage Examples

### Basic Save/Load

```csharp
// Save current level
if (GridManager.Instance.SaveLevel())
{
    Debug.Log("Level saved successfully");
}

// Load saved level
if (GridManager.Instance.LoadLevel())
{
    Debug.Log("Level loaded successfully");
}
```

### Named Levels

```csharp
// Save with custom name
GridManager.Instance.SaveLevel("my_puzzle_level");

// Load specific level
GridManager.Instance.LoadLevel("my_puzzle_level");

// Check if level exists
if (LevelSaveSystem.LevelExists("my_puzzle_level"))
{
    Debug.Log("Level exists!");
}
```

### Manual Capture and Restore

```csharp
// Capture level data without saving to file
LevelData data = GridManager.Instance.CaptureLevelData();

// Modify data programmatically
data.levelName = "Generated Level";
data.blocks.Add(new LevelData.BlockData(BlockType.Default, 0));

// Save modified data
LevelSaveSystem.SaveLevel(data, "generated_level");

// Later, load and restore
LevelData loadedData = LevelSaveSystem.LoadLevel("generated_level");
if (loadedData != null)
{
    GridManager.Instance.RestoreLevelData(loadedData);
}
```

### Level Management

```csharp
// Get save directory path
string savePath = LevelSaveSystem.GetSavePath();
Debug.Log($"Levels saved to: {savePath}");

// Delete a level
if (LevelSaveSystem.DeleteLevel("old_level"))
{
    Debug.Log("Level deleted");
}

// List all saved levels (requires custom code)
string[] levelFiles = Directory.GetFiles(
    LevelSaveSystem.GetSavePath(),
    "*.json"
);
foreach (string file in levelFiles)
{
    Debug.Log($"Found level: {Path.GetFileNameWithoutExtension(file)}");
}
```

## Extending the System

### Adding New Data Fields

1. Add field to `LevelData` class:
```csharp
public int levelDifficulty;  // New field
```

2. Update `CaptureLevelData()`:
```csharp
levelData.levelDifficulty = CalculateDifficulty();
```

3. Update `RestoreLevelData()`:
```csharp
// Use loaded difficulty
Debug.Log($"Loading level with difficulty: {levelData.levelDifficulty}");
```

**Versioning Consideration**: Old save files without new field will use default value (0 for int, null for reference types).

### Supporting Multiple Level Slots

Add UI for level slot selection:
```csharp
public void SaveToSlot(int slot)
{
    string levelName = $"level_slot_{slot}";
    GridManager.Instance.SaveLevel(levelName);
}

public void LoadFromSlot(int slot)
{
    string levelName = $"level_slot_{slot}";
    if (LevelSaveSystem.LevelExists(levelName))
    {
        GridManager.Instance.LoadLevel(levelName);
    }
}
```

### Adding Autosave

Implement periodic autosaving:
```csharp
private float autosaveTimer = 0f;
private const float AUTOSAVE_INTERVAL = 300f; // 5 minutes

private void Update()
{
    autosaveTimer += Time.deltaTime;
    if (autosaveTimer >= AUTOSAVE_INTERVAL)
    {
        autosaveTimer = 0f;
        GridManager.Instance.SaveLevel("autosave");
        Debug.Log("Level autosaved");
    }
}
```

### Custom Serialization Format

To use a custom format instead of JSON:

1. Create serialization class:
```csharp
public static class CustomLevelSerializer
{
    public static byte[] SerializeLevel(LevelData data)
    {
        // Custom binary serialization logic
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(data.gridWidth);
            writer.Write(data.gridHeight);
            // ... serialize other fields
            return ms.ToArray();
        }
    }

    public static LevelData DeserializeLevel(byte[] data)
    {
        // Custom deserialization logic
    }
}
```

2. Update `LevelSaveSystem` to use custom serializer instead of JsonUtility.

## Performance Considerations

### Save Performance
- Typical save time: <10ms for standard levels (100 blocks)
- Dominated by file I/O, not serialization
- Can save hundreds of times per second if needed

### Load Performance
- Typical load time: <20ms for standard levels
- Includes clearing old state + placing all blocks/Lems
- Block instantiation is the slowest part

### Memory Usage
- LevelData object: ~1-5 KB for typical levels
- JSON file size: ~2-10 KB for typical levels
- Minimal memory overhead

### Optimization Opportunities
- Use binary format for larger levels (10x smaller files)
- Implement async loading for levels with 500+ blocks
- Pool block GameObjects to avoid instantiation overhead
- Cache last loaded level to avoid redundant file reads

## Troubleshooting

### Common Issues

**Issue**: "Failed to save level" error
**Cause**: Insufficient permissions or disk space
**Fix**: Check that save directory is writable

**Issue**: Level loads but objects are missing
**Cause**: Grid size changed or blocks out of bounds
**Fix**: Check console for "Invalid grid index" warnings

**Issue**: Loaded level looks different from saved version
**Cause**: Colors/visuals changed in code but not in save data
**Fix**: Visual properties aren't saved - only structural data

**Issue**: Cannot load level after Unity update
**Cause**: JsonUtility format may have changed
**Fix**: May need to regenerate level files

### Debugging Tips

**Enable verbose logging**:
```csharp
Debug.Log($"Saving {levelData.blocks.Count} blocks");
Debug.Log($"Placeable spaces: {levelData.placeableSpaceIndices.Count}");
```

**Inspect JSON files directly**:
- Navigate to save directory
- Open .json files in text editor
- Verify data structure matches expectations

**Validate save data**:
```csharp
LevelData data = GridManager.Instance.CaptureLevelData();
Debug.Assert(data.blocks.Count > 0, "No blocks to save!");
Debug.Assert(data.gridWidth > 0, "Invalid grid width!");
```

## Future Enhancements

### Planned Features
- Level metadata (author, description, tags)
- Thumbnail generation (save screenshot with level)
- Compression for large levels
- Level validation (check if solvable)
- Version numbering for format changes

### Potential Improvements
- Cloud save integration
- Level sharing/import/export
- Undo history serialization
- Replay recording
- Level analytics (completion rate, average time)
