# Level Setup Guide

Complete guide to creating, configuring, and saving levels in the game.

---

## Table of Contents
1. [Level Properties](#level-properties)
2. [Savable Settings](#savable-settings)
3. [Creating a New Level](#creating-a-new-level)
4. [Editing Levels](#editing-levels)
5. [Camera Settings](#camera-settings)
6. [Inventory Configuration](#inventory-configuration)
7. [Workflow Summary](#workflow-summary)

---

## Level Properties

### Metadata Properties
Properties that identify and organize the level:

| Property | Type | Description |
|----------|------|-------------|
| **levelId** | `string` | Unique identifier (e.g., `"tutorial_01"`, `"world1_level3"`) |
| **levelName** | `string` | Display name shown to players (e.g., `"First Steps"`) |
| **worldId** | `string` | ID of the world this level belongs to |
| **orderInWorld** | `int` | 0-based order within the world for progression |

### Grid Configuration
Properties that define the playing field:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **gridWidth** | `int` | 10 | Number of columns in the grid |
| **gridHeight** | `int` | 10 | Number of rows in the grid |
| **cellSize** | `float` | 1.0 | Size of each grid cell in world units |

### Level Data (JSON)
Complete level state stored as JSON, containing:
- Placed blocks
- Permanent blocks (designer-placed)
- Inventory configuration
- Placeable space indices
- Lem placements and states
- Key/lock states
- Camera settings

---

## Savable Settings

### What Gets Saved When You Press Cmd+S

When you press **Cmd+S** (or Ctrl+S on Windows) in Play mode, the following data is captured and saved:

#### 1. Grid Configuration
- Grid dimensions (width × height)
- Cell size
- Level name

#### 2. Permanent Blocks
All blocks placed in **Design mode** are saved as permanent (non-removable) blocks:
- Block type
- Grid position
- Block-specific properties (routes for transporters, flavor IDs for teleporters, etc.)

#### 3. Placeable Spaces
Grid indices where players can place blocks during gameplay.

#### 4. Camera Settings ⭐
Complete camera configuration saved per-level:

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| **verticalOffset** | 10.4 | 0-28 | Camera vertical position from grid center |
| **horizontalOffset** | 0 | -10 to 10 | Camera horizontal position offset |
| **tiltAngle** | 3.7° | -5° to 20° | Camera pitch (negative = look down) |
| **panAngle** | 0° | -15° to 15° | Camera yaw (left/right rotation) |
| **focalLength** | 756mm | 100-1200mm | Lens focal length (higher = telephoto) |
| **distanceMultiplier** | 23.7x | 5-40x | Distance multiplier for perspective flattening |
| **fieldOfView** | 1.82° | (calculated) | Auto-calculated from focal length |
| **nearClipPlane** | 0.24 | 0.01-1 | Near clipping distance |
| **farClipPlane** | 500 | 100-1000 | Far clipping distance |
| **gridMargin** | 1 | 0-3 | Margin around grid for framing |
| **minDistance** | 5 | 1-10 | Minimum camera distance |
| **rollOffset** | 0° | -10° to 10° | Camera roll angle |

#### 5. Inventory Configuration
Block types and quantities available to players:

```csharp
public class BlockInventoryEntry
{
    public BlockType blockType;          // Type of block (Walk, Jump, etc.)
    public int maxCount;                 // Maximum number player can place
    public string displayName;           // Optional custom name
    public string inventoryGroupId;      // For shared inventory pools
    public string flavorId;              // For teleporters/transporters
    public bool isPairInventory;         // For paired teleporter inventory
    public int pairSize;                 // Size of teleporter pairs
    public string[] routeSteps;          // Route steps for transporters (e.g., "U5", "R3")
}
```

#### 6. Lem Placements ✅
Lem starting positions and facing directions ARE saved:
- Place Lems in **Design mode** to set their starting positions
- Facing direction (left/right) is saved
- World position is captured
- Lems are the characters that need to reach the goal - the designer places them, players build paths for them

#### 7. Player-Placed Blocks During Play (NOT saved by Cmd+S)
Blocks placed by the player during Play mode are **NOT** saved by the designer save (Cmd+S). This is intentional - you're designing the level, not solving it. The player places blocks from the inventory to create paths for the Lems to reach the goal.

---

## Creating a New Level

### Method 1: Via Project Window (Recommended)

1. **Create the asset:**
   - Right-click in Project window → **Create → AWITP → Level Definition**
   - Name it descriptively (e.g., `tutorial_01.asset`)

2. **Configure metadata:**
   - Open the asset in Inspector
   - Set `levelId` (unique identifier, e.g., `"tutorial_01"`)
   - Set `levelName` (display name, e.g., `"First Steps"`)
   - Set `worldId` (world identifier, e.g., `"tutorial"`)
   - Set `orderInWorld` (0, 1, 2, etc.)

3. **Configure grid:**
   - Set `gridWidth` and `gridHeight` (default: 10×10)
   - Set `cellSize` (default: 1.0)

4. **Click "Edit Level Visually"** button
   - This loads the Master scene
   - Enters Play mode
   - You can now design the level!

### Method 2: Duplicate Existing Level

1. Duplicate an existing level asset
2. Change the `levelId` and `levelName`
3. Click "Edit Level Visually" to modify

---

## Editing Levels

### Visual Editor Workflow

The primary way to edit levels is through the **Visual Editor**:

1. **Open the level asset** in Inspector
2. **Click "Edit Level Visually"**
   - Loads Master scene
   - Enters Play mode automatically
3. **Design mode controls:**
   - **Arrow Keys**: Move grid cursor
   - **Space/Enter**: Place selected block (permanent)
   - **Tab**: Toggle between Design/Play modes
   - **Cmd+S** (Mac) or **Ctrl+S** (Windows): **Save level to asset**
   - **Cmd+Shift+S**: Show asset path (where it's saved)

4. **Adjust camera settings:**
   - Find `CameraSetup` component in scene
   - Adjust sliders in Inspector while in Play mode
   - Settings update in real-time
   - Press **Cmd+S** to save camera settings to level

5. **Edit inventory:**
   - Either in the level asset Inspector (when not in Play mode)
   - Or use the custom editor UI

### Inspector Editing (Advanced)

For direct editing without entering Play mode:

#### Grid Settings
- Edit `gridWidth`, `gridHeight`, `cellSize` directly
- Changes auto-sync to JSON

#### Inventory Configuration
- Expand **Inventory Configuration** section
- Click **"+ Add Entry"** to add blocks
- Configure each entry:
  - Block type
  - Max count
  - Display name (optional)
  - Advanced settings (routes, flavor IDs, etc.)
- Click **"Save Changes"** to write to JSON

#### JSON Data (Expert Only)
- Expand **"Level Data JSON (Advanced)"**
- Direct JSON editing (not recommended)
- Used for importing/exporting level data

---

## Camera Settings

### Understanding the Camera System

The game uses an **extreme telephoto perspective system** designed to:
- Show consistent perspective across all blocks (they all look the same size)
- Minimize distortion between near and far blocks
- Provide a flat, isometric-like view while using 3D perspective

### Key Camera Parameters

#### Position & Rotation
- **Vertical Offset** (10.4): How high the camera sits above the grid
- **Horizontal Offset** (0): Left/right offset from center
- **Tilt Angle** (3.7°): Slight downward angle to see block tops
- **Pan Angle** (0°): Yaw rotation (usually kept at 0)
- **Roll Angle** (0°): Horizon tilt (usually kept at 0)

#### Perspective Settings
- **Focal Length** (756mm): Telephoto lens simulation
  - Higher = flatter perspective, more "isometric" look
  - 50mm = "normal" human vision
  - 200mm = telephoto
  - 756mm = extreme telephoto (current default)

- **Distance Multiplier** (23.7x): Pulls camera way back
  - Combined with telephoto lens creates ultra-flat perspective
  - Automatically scales based on grid size

#### Advanced Settings
- **FOV**: Auto-calculated from focal length (1.82° for 756mm)
- **Grid Margin**: Padding around grid for framing (1.0 units)
- **Min Distance**: Prevents camera from getting too close (5.0)

### Camera Workflow

#### Adjusting Camera in Play Mode
1. Enter Play mode (via Visual Editor)
2. Select `CameraSetup` GameObject in Hierarchy
3. Adjust settings in Inspector - **changes are live!**
4. Press **Cmd+S** to save camera settings to the level asset

#### Batch Update Camera Settings
Use **Tools → Update Level Camera Settings** to:
- Update camera settings across **all levels**
- Or just update the current scene camera
- Selectively choose which settings to update
- Useful for applying consistent camera settings to multiple levels

---

## Inventory Configuration

### Adding Blocks to Player Inventory

Players can only place blocks that are in the level's inventory. Configure this in the Inspector:

#### Basic Block Entry
```
Block Type: Walk
Max Count: 10
Display Name: (leave empty for default)
```

#### Teleporter Entry
```
Block Type: Teleporter
Max Count: 6
Flavor ID: A  (matches teleporters with same ID)
Is Pair Inventory: true
Pair Size: 2  (places in pairs)
```

#### Transporter Entry
```
Block Type: Transporter
Max Count: 3
Route Steps:
  - U5  (Up 5 cells)
  - R3  (Right 3 cells)
  - D2  (Down 2 cells)
Flavor ID: (optional, for color/style)
```

### Inventory Groups
Use `inventoryGroupId` to share counts between entries:
- All entries with same group ID share one pool
- Example: Different colored blocks but only 10 total allowed

---

## Workflow Summary

### Creating a New Level
1. **Create asset:** Right-click → Create → AWITP → Level Definition
2. **Set metadata:** levelId, levelName, worldId, orderInWorld
3. **Configure grid:** width, height, cell size
4. **Edit visually:** Click "Edit Level Visually" button
5. **Design level:** Place blocks in Design mode
6. **Adjust camera:** Tweak camera settings in Inspector
7. **Set inventory:** Configure which blocks players can use
8. **Save:** Press **Cmd+S** in Play mode
9. **Test:** Switch to Play mode (Tab key) and test the level

### Editing Existing Level
1. **Open asset** in Inspector
2. **Click "Edit Level Visually"**
3. **Make changes** (blocks, camera, inventory)
4. **Save:** Press **Cmd+S**
5. **Exit Play mode**

### Quick Camera Tweaks
1. **Open level** for editing
2. **Select CameraSetup** in Hierarchy
3. **Adjust sliders** (changes are live)
4. **Press Cmd+S** when satisfied

### Batch Operations
- **Tools → Update Level Camera Settings**: Update camera across all levels
- **Inventory editor**: Bulk edit inventory in Inspector
- **Grid settings**: Auto-sync when changed in Inspector

---

## File Locations

### Level Assets
- **Path:** `Assets/Resources/Levels/LevelDefinitions/`
- **Format:** Unity ScriptableObject (.asset)
- **Convention:** `{levelId}.asset` (e.g., `tutorial_01.asset`)

### Where Saves Go
When you press **Cmd+S** in Play mode:
- Data is saved **directly into the LevelDefinition asset**
- Updates the `levelDataJson` field with serialized data
- Unity marks the asset as dirty and saves it
- Changes are immediately reflected in source control

### Debug: View Asset Path
Press **Cmd+Shift+S** in Play mode to see the asset path in console.

---

## Tips & Best Practices

### Level Design (Designer Role)
- ✅ Use **Design mode** to place permanent blocks:
  - Walk blocks (platforms for Lems to stand on)
  - Lock blocks (statues - the goal Lems must reach)
  - Key blocks (trees with apples - keys to unlock locks)
- ✅ Place **Lems** in Design mode at their starting positions
- ✅ Mark placeable spaces where players can build paths
- ✅ Configure inventory with blocks players can use to help Lems reach the goal
- ✅ Test in **Play mode** (Tab key) by placing blocks to create paths for Lems
- ✅ Save often with **Cmd+S**

### Player Role (During Gameplay)
- Player places blocks from the inventory to create paths
- Player does NOT place Lems - Lems are part of the level design
- Goal: Help Lems reach the lock blocks (statues) by building paths

### Camera Settings
- ✅ Start with defaults (756mm focal length, 23.7x distance)
- ✅ Adjust verticalOffset if blocks are cut off
- ✅ Small tilt angles (3-5°) show block tops well
- ✅ Higher focal length = flatter perspective

### Inventory
- ✅ Give just enough blocks to solve the level
- ✅ Extra blocks = more solutions (good for puzzles)
- ✅ Use inventory groups for shared pools
- ✅ Test that the level is solvable!

### Version Control
- ✅ Commit level assets after significant changes
- ✅ Level data is in JSON format (diffable)
- ✅ Camera settings are included in the save
- ✅ Test levels after pulling changes

---

## Troubleshooting

### "No LevelDefinition loaded" when pressing Cmd+S
- Make sure you opened the level via "Edit Level Visually" button
- The level must be loaded through `LevelManager` to save

### Camera settings not saving
- Press **Cmd+S** in Play mode to save
- Make sure you're editing the `CameraSetup` in the scene
- Check that the level asset exists and is not locked

### Blocks not appearing in inventory
- Check the **Inventory Configuration** section in level asset
- Ensure `maxCount` > 0
- Save inventory changes with "Save Changes" button

### Grid size changed but level looks wrong
- Camera distance auto-adjusts based on grid size
- You may need to re-adjust camera settings after grid changes
- Press **Cmd+S** to save new camera position

### Level validation fails
- Check all required fields are filled:
  - levelId (unique)
  - levelName (display name)
  - worldId (world identifier)
  - levelDataJson (generated automatically)

---

## Quick Reference

### Keyboard Shortcuts (Play Mode)
| Key | Action |
|-----|--------|
| **Arrow Keys** | Move grid cursor |
| **Space/Enter** | Place block (Design mode) |
| **Tab** | Toggle Design/Play modes |
| **Cmd+S** (Ctrl+S) | **Save level to asset** |
| **Cmd+Shift+S** | Show asset path |
| **C** | Refresh camera setup |

### Camera Defaults
- Focal Length: **756mm** (extreme telephoto)
- Distance Multiplier: **23.7x** (very far, very flat)
- Vertical Offset: **10.4** units
- Tilt Angle: **3.7°** (slight downward look)
- FOV: **1.82°** (auto-calculated)

### Important Components
- **LevelDefinition**: ScriptableObject asset that stores level data
- **LevelManager**: Runtime singleton that loads/manages levels
- **GridManager**: Manages grid, blocks, and level state
- **CameraSetup**: Controls camera positioning and perspective
- **BuilderController**: Handles player input and design mode

---

## Additional Resources

- **SAVE_SYSTEM.md**: Detailed save system documentation
- **README.md**: Project overview and setup
- **LevelDefinition.cs**: Source code with inline documentation
- **LevelData.cs**: Data structure definitions
- **CameraSetup.cs**: Camera system implementation

---

*Last updated: 2026-02-01*
