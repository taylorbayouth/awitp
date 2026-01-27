# Designer Guide - Level Creation Reference

**Audience**: This guide is for you, Taylor (the game designer/developer), to create puzzle levels.

**What Players See**: Build Mode + Play Mode (they toggle with P)
**What You See**: Build Mode + Designer Mode (you access with E) + Play Mode

---

## Quick Start

1. Open Unity and load the Master.unity scene
2. Press Play to enter Build Mode
3. Press **E** to enter **Designer Mode** - create level structure here
4. Press **P** to test your level in **Play Mode**

## Game Modes

### Build Mode (Player-Facing - Default)
**Purpose**: This is what players experience - placing blocks from inventory to solve puzzles

**Controls**:
- **Arrow Keys / WASD** - Move cursor
- **Space / Enter** - Place block at cursor (from inventory)
- **Delete / Backspace** - Remove block at cursor
- **1-9 Keys** - Switch block entry (first 9 slots)
- **[ / ]** - Cycle block entries
  - 1 = Default (cyan platform)
  - 2 = Teleporter (magenta) - must be placed in pairs
  - 3 = Crumbler (orange)
  - 4 = Transporter (yellow)
  - 5 = Key (gold)
  - 6 = Lock (silver)
- **P** - Toggle Play Mode to test solution

**Note**: Players ONLY have access to Build Mode and Play Mode. They do NOT have the E key (Designer Mode).

---

### Designer Mode (Dev-Only - Press E)
**Purpose**: Create level structure - permanent blocks, placeable spaces, Lems, inventory

**This mode is for you to design levels. Players never see this.**

**Controls**:
- **Arrow Keys / WASD** - Move cursor
- **Space / Enter** - Toggle placeable space (shows black border where players can place blocks)
- **1-9 Keys** - Select block type for permanent blocks
- **[ / ]** - Cycle block types
- **B** - Place permanent block (fixed architecture, cannot be removed by players)
- **L** - Place starting Lem (press again to flip direction)
- **Delete / Backspace** - Remove block or Lem at cursor
- **E** - Return to Build Mode

**What You're Creating**:
1. **Permanent Blocks** (B key) - Fixed level architecture
2. **Placeable Spaces** (Space key) - Black borders showing where players can place blocks
3. **Starting Lems** (L key) - Initial character positions
4. **Inventory** (via LevelBlockInventoryConfig) - What blocks players have access to

---

### Play Mode
**Purpose**: Test your level with active Lem AI (same as player experience)

**Controls**:
- **P** - Exit Play Mode (returns to Build Mode)
- Lems walk automatically and interact with blocks

**What You're Testing**:
- Is the level solvable?
- Do blocks behave correctly?
- Is the difficulty appropriate?
- Do Lems navigate as expected?

---

## Cursor States

The cursor changes color to show what you can do:

### In Build Mode:
- **Red** - Space is not placeable (players cannot place blocks here)
- **Yellow** - Space has a block (you can edit/remove it)
- **Green** - Space is empty and placeable (players CAN place blocks here)

### In Designer Mode:
- **Grey** - Empty space
- **Yellow** - Has block (permanent or placeable)
- **Green** - Placeable space marked (shows black border)

## Saving and Loading

### Save Your Level
Press **Ctrl+S** (Windows/Linux) or **Cmd+S** (Mac)

The level will be saved to a JSON file including:
- All placed blocks (type and position)
- All permanent blocks (type and position)
- Placeable space markings
- Lem placements and facing directions
- Grid settings (width, height, cell size)

### Load Your Level
Press **Ctrl+L** (Windows/Linux) or **Cmd+L** (Mac)

This restores everything exactly as you saved it. If no save file exists, you'll see a warning message.

### Find Save Location
Press **Ctrl+Shift+S** (Windows/Linux) or **Cmd+Shift+S** (Mac)

The console will show you the full path to where levels are saved.

**Default save location**:
- Windows: `C:\Users\[Username]\AppData\LocalLow\[CompanyName]\[ProductName]\Levels\`
- Mac: `~/Library/Application Support/[CompanyName]/[ProductName]/Levels/`
- Linux: `~/.config/unity3d/[CompanyName]/[ProductName]/Levels/`

## Workflow: Creating a Level

**Understanding the Process**:
1. **Prototype** in Build Mode (optional - test player experience)
2. **Design** in Designer Mode (create the puzzle structure)
3. **Test** in Play Mode (verify it works)
4. **Iterate** (repeat until perfect)

---

### Step 1: Enter Designer Mode
1. Start Unity and press Play (you'll be in Build Mode)
2. Press **E** to enter Designer Mode
3. You're now creating the level structure

---

### Step 2: Create Permanent Architecture (Designer Mode)
1. Press **1-9** or **[ ]** to select block type
2. Press **B** to place permanent blocks (fixed architecture)
3. Build the skeleton of your puzzle:
   - Platforms Lems will walk on
   - Key blocks (where keys are located)
   - Lock blocks (where keys must be delivered)
4. Press Delete/Backspace to remove mistakes

**Design Tip**: Permanent blocks form the unchangeable structure. Players cannot remove or add to these.

---

### Step 3: Mark Placeable Spaces (Designer Mode)
1. Still in Designer Mode
2. Move cursor to spaces where players should be able to place blocks
3. Press **Space/Enter** to toggle placeable space marking
   - A **black border** appears - this is where players can place blocks
   - Placeable spaces show a black border
4. To add fixed geometry, press **B** to place a permanent block (uses the selected block type)
   - Permanent blocks are always non-placeable for players
5. Think about:
   - Which spaces create interesting puzzles?
   - Are there multiple solutions?
   - Is the level solvable with the available spaces?

### Step 3: Position Lems (Level Editor Mode)
1. Still in Level Editor Mode
2. Move cursor to where you want a Lem to start
3. Press **L** to place a Lem
   - The Lem appears on top of any block at that position
   - Default facing direction is RIGHT
   - Only one Lem can exist; placing a new one replaces the old
4. Press **L** again to flip the Lem's direction
5. Press Delete/Backspace to remove a Lem

### Step 4: Test Your Level (Play Mode)
1. Press **P** to enter Play Mode
2. Watch the Lems walk and interact with blocks
3. Verify that:
   - Lems can reach the goal
   - The puzzle is challenging but solvable
   - Blocks behave as expected
4. Press **P** to exit Play Mode and return to editing

### Step 5: Save Your Work
1. Press **Ctrl+S** (or **Cmd+S** on Mac)
2. Check the console for "LEVEL SAVED" confirmation
3. Your level persists even after closing Unity

## Tips and Best Practices

### Design Tips
- Start simple: Create a basic path from start to finish
- Add complexity: Introduce puzzles with limited placeable spaces
- Test frequently: Press P often to verify your level works
- Use variety: Mix different block types for interesting interactions

### Block Placement
- Default blocks are your foundation - use them liberally
- Teleporters create shortcuts or tricky paths (must be placed in pairs with matching flavor)
- Crumblers add time pressure and consequences - they darken when stepped on and crumble when exited
- Transporters enable dynamic level changes (route path is blocked for other placements)
  - Inventory preview shows the transporter route shape
- Keys and Locks create puzzle gates - Lem collects key and brings it to matching lock

### Placeable Spaces
- Too few spaces = frustratingly difficult
- Too many spaces = trivially easy
- Sweet spot: Multiple solutions with interesting tradeoffs

### Lem Placement
- Place Lems on stable blocks, not in mid-air
- Consider starting direction - does it create challenge?
- Multiple Lems can create coordination puzzles

### Testing
- Test immediately after major changes
- Try to break your own level
- If you can't solve it, players probably can't either

## Keyboard Shortcuts Reference

### Navigation (All Modes)
- **Arrow Keys / WASD** - Move cursor around grid

### Editor Mode
- **Space / Enter** - Place selected block type
- **Delete / Backspace** - Remove block
- **1** - Select Default block
- **2** - Select Teleporter block
- **3** - Select Crumbler block
- **4** - Select Transporter block
- **5** - Select Key block
- **6** - Select Lock block
- **E** - Switch to Level Editor Mode

### Level Editor Mode
- **Space / Enter** - Toggle placeable space marking
- **1-9** - Select block entry for permanent blocks
- **[ / ]** - Cycle block entries
- **B** - Place permanent block (uses selected block type)
- **L** - Place/flip Lem
- **Delete / Backspace** - Remove block or Lem
- **E** - Return to Editor Mode

### Play Mode
- **P** - Exit Play Mode (returns to Editor Mode)

### Save/Load (All Modes)
- **Ctrl+S / Cmd+S** - Save current level
- **Ctrl+L / Cmd+L** - Load saved level
- **Ctrl+Shift+S / Cmd+Shift+S** - Show save location in console

### Mode Switching
- **E** - Toggle between Editor and Level Editor modes
- **P** - Toggle Play Mode on/off

## Troubleshooting

### "Cannot place block at index X: space is not placeable"
**Solution**: You're in Editor Mode but trying to place a block in a non-placeable space. Either:
- Switch to Level Editor Mode (press E) and mark the space as placeable
- Move to a different space that's already marked as placeable

### "Cannot remove permanent block in Editor mode"
**Cause**: Permanent blocks are locked while in Editor Mode
**Solution**: Press E to switch to Level Editor Mode, then delete the block

### Cursor is red and I can't place anything
**Cause**: The current space is marked as non-placeable
**Solution**: Press E to enter Level Editor Mode, then press Space to mark it as placeable

### "No saved level found"
**Cause**: You haven't saved a level yet, or the save file was deleted
**Solution**: Create and save a level first with Ctrl+S before trying to load

### Lem falls through blocks
**Cause**: Lem was placed in mid-air or on a block that was removed
**Solution**: Place Lems only on solid blocks, and verify blocks exist below them

### Level doesn't load correctly
**Cause**: Save file may be corrupted or from an incompatible version
**Solution**: Check the console for specific error messages. You may need to recreate the level.

### Grid lines are hard to see
**Solution**: Adjust `GRID_LINE_OPACITY` in RenderingConstants.cs (0.0 = transparent, 1.0 = opaque)

## Advanced Features

### Grid Customization
You can modify grid settings in the Inspector (GridManager component):
- **Grid Width** - Number of columns (default: 10)
- **Grid Height** - Number of rows (default: 10)
- **Cell Size** - Size of each grid cell (default: 1.0)

After changing grid settings, the level will automatically recenter.

### Visual Customization
Edit `RenderingConstants.cs` to adjust:
- **Line widths** - Thickness of grid, borders, and cursor
- **Line depths** - Z-position layering
- **Opacity** - Transparency of grid lines

### Inventory Configuration
Add a **LevelBlockInventoryConfig** component to an empty GameObject to define:
- Block entries (type + optional flavor)
- Inventory counts per entry or shared `inventoryGroupId`
- Transporter routes via `routeSteps` (placement is blocked if a route intersects existing blocks)

Block inventory is saved with the level and restored on load.

### Visual Customization
Block visuals are defined by prefabs in `Resources/Blocks/`:
- Each block type has its own prefab (e.g., Block_Key.prefab, Block_Lock.prefab)
- Prefabs can include custom meshes, materials, and child objects
- Fallback colors are defined in `BlockColors.cs` for blocks without prefabs

Edit `BlockColors.cs` to change:
- Fallback block type colors
- Cursor colors (placeable, editable, non-placeable)
- Grid line color
- Placeable border color

## File Format

Levels are saved as JSON files with this structure:
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
    }
  ],
  "blocks": [
    {"blockType": 0, "gridIndex": 45, "inventoryKey": "Default", "flavorId": ""},
    {"blockType": 1, "gridIndex": 46, "inventoryKey": "Teleporter", "flavorId": "A"}
  ],
  "placeableSpaceIndices": [44, 45, 46, 54, 55, 56],
  "lems": [
    {"gridIndex": 45, "facingRight": true}
  ],
  "levelName": "current_level",
  "saveTimestamp": "2026-01-25 12:34:56"
}
```

You can manually edit these files if needed, but be careful with the format!

## Getting Help

Check the console (Unity's Console window) for helpful messages:
- Block placement confirmations
- Mode switch notifications
- Save/load status messages
- Error messages with troubleshooting hints

Most operations provide clear feedback about what's happening.
