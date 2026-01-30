# How to Create a Level in A Walk in the Park

**STATUS: ✅ STILL CURRENT AND RELEVANT**

**IMPORTANT**: This guide is for level designers (you, Taylor), not for players. This explains how to create the puzzles that players will solve.

**Game Structure**:
- **Players** see: Build Mode (place blocks to solve puzzle) + Play Mode (test solution)
- **Designers** see: Build Mode + Level Designer mode (E key - create puzzles) + Play Mode

**NEW: Multi-Level System**
This guide describes the Level Designer mode workflow (still fully functional). The new multi-level system adds:
- **Editor Tools**: Convert saved JSON levels into ScriptableObject assets (see [LEVEL-SYSTEM-SETUP-GUIDE.md](LEVEL-SYSTEM-SETUP-GUIDE.md))
- **World System**: Group levels into worlds with progression
- **UI Navigation**: Menu → World Map → Level Select → Game

**Use this guide for**: Creating and testing individual levels in Level Designer mode
**Then use**: Editor tools to integrate levels into the world system

---

A comprehensive guide for designing and building puzzle levels in AWITP.

## Table of Contents
1. [Introduction](#introduction)
2. [Understanding the Three Modes](#understanding-the-three-modes)
3. [Your First Level: A Complete Tutorial](#your-first-level-a-complete-tutorial)
4. [Block Types Reference](#block-types-reference)
5. [Inventory System & Configuration](#inventory-system--configuration)
6. [Advanced Level Design](#advanced-level-design)
7. [Playtesting & Iteration](#playtesting--iteration)
8. [Best Practices & Design Patterns](#best-practices--design-patterns)
9. [Troubleshooting](#troubleshooting)
10. [Quick Reference](#quick-reference)

---

## Introduction

A Walk in the Park is a puzzle game where players guide "Lems" (lemming-like characters) from start to goal by strategically placing blocks. As a level designer, your job is to create interesting puzzles that challenge players to think creatively about block placement.

### Core Concepts

- **Lems**: Autonomous characters that walk continuously, turn at walls, and fall off edges
- **Blocks**: Platform pieces that Lems walk on (some have special behaviors)
- **Placeable Spaces**: Grid cells where players can place blocks during gameplay
- **Permanent Blocks**: Fixed architecture you place as the designer
- **Inventory**: Limited supply of blocks available to the player

### The Level Design Philosophy

Good levels have:
- **Clear goals**: Players understand what they need to accomplish
- **Interesting constraints**: Limited blocks create meaningful choices
- **Multiple solutions**: Creativity is rewarded
- **Progressive difficulty**: Introduce concepts gradually

---

## Understanding the Three Modes

The editor has three distinct modes you'll switch between while creating levels.

### Build Mode
**Purpose**: Player-facing block placement using the level's inventory

**What you can do**:
- Place blocks on marked placeable spaces (consumes inventory)
- Remove placed blocks (returns to inventory)
- Cycle the inventory entries exactly like a player

**Controls**:
- `Arrow Keys / WASD` - Move cursor
- `Space / Enter` - Place block (from inventory)
- `Delete / Backspace` - Remove placed block
- `1-9` or `[ ]` - Select inventory entry
- `E` - Switch to Level Designer mode

**Use this mode to**: Test the puzzle under real player constraints.

---

### Level Designer mode
**Purpose**: Define the player's puzzle constraints

**What you can do**:
- Mark which spaces players can use (placeable spaces)
- Place permanent blocks (the fixed architecture)
- Position Lems (starting characters)

**Controls**:
- `Arrow Keys / WASD` - Move cursor
- `Space / Enter` - Mark placeable space (adds a border)
- `Delete / Backspace` - Clear placeable space + remove block or Lem
- `B` - Place permanent block
- `L` - Place/flip Lem
- `1-9` or `[ ]` - Select block type for permanent blocks
- `E` - Return to Build Mode

**Note**: Level Designer mode shows all block types with infinite counts for placement.

**Use this mode to**: Transform your prototype into a real puzzle by limiting what players can do.

---

### Play Mode (Black background with skybox)
**Purpose**: Test your level with full Lem AI

**What happens**:
- Lems activate and start walking
- All block behaviors work (crumblers crumble, transporters move, etc.)
- Inventory limits apply
- You experience the level as a player would

**Controls**:
- `P` - Exit play mode (returns to Build Mode)
- All other controls disabled

**Use this mode to**: Verify your level is solvable and fun to play.

---

## Your First Level: A Complete Tutorial

Let's create a simple level from scratch to learn the workflow.

### Step 1: Lay Out the Permanent Structure (Level Designer mode)

1. **Start Unity and press Play** - You'll begin in Build Mode
2. **Press `E`** to enter Level Designer mode
3. **Move the cursor** with arrow keys to position (0, 0) - the bottom-left
4. **Press `1`** to select Default blocks
5. **Create a starting platform**:
   - Press `B` while moving right to place a 5-block permanent platform
6. **Move cursor up and right** to create a gap
7. **Create a landing platform**:
   - Place another 5-block permanent platform at a higher position

At this point you have the fixed structure, but it's not a puzzle yet!

---

### Step 2: Define Placeable Spaces (Level Designer mode)

1. **Mark placeable spaces**:
   - Move cursor to the gap between your two platforms
   - Press `Space / Enter` to mark a placeable space
   - You'll see a border appear - this means players can place blocks here
   - Use **Delete/Backspace** to clear a placeable space
2. **Mark 2-3 spaces** in the gap - enough for players to bridge the gap

**Design Tip**: The number of placeable spaces determines difficulty. Fewer spaces = harder puzzle.

---

### Step 3: Place the Lem (Level Designer mode)
1. **Move cursor** to the leftmost block of your starting platform
2. **Press `L`** to place a Lem
3. **Verify direction**:
   - The Lem appears on top of the block
   - Default facing is RIGHT
   - Press `L` again to flip direction if needed

**Design Tip**: Place Lems on solid ground, never in mid-air!

---

### Step 4: Test Your Level (Play Mode)

1. **Press `P`** to enter Play Mode
2. **Watch the Lem**:
   - Does it walk toward the gap?
   - Can you place blocks to bridge the gap?
   - Does the Lem reach the other side?
3. **Press `P`** to exit Play Mode when done

**Iteration**: If something doesn't work, exit Play Mode and adjust in Level Designer mode.

---

### Step 5: Save Your Level

1. **Press `Ctrl+S`** (Windows/Linux) or `Cmd+S` (Mac)
2. **Check the console** - you should see "LEVEL SAVED" confirmation
3. Your level is now saved to the LevelDefinition asset.

**File Location**: Press `Ctrl+Shift+S` to see the active LevelDefinition asset path.

**Designer-only**: Save/Load is for Taylor while authoring levels. Players do not have a save hotkey. Player progress is saved automatically only when a level is completed (all locks filled), and any in-progress block placements are discarded on restart.
Designer saves persist permanent layout, Lem placement, placeable spaces, grid settings, and inventory config (not in-progress player block placements).

---

### Step 6: Add Complexity (Optional)

Now that you have a basic level, try adding:

- **A crumbler block** (press `3`) - Forces players to time their crossing
- **A second gap** - Requires careful block usage
- **A transporter** (press `4`) - Creates dynamic movement

---

## Block Types Reference

### Walk Block (Cyan) - Type 1
**Behavior**: Solid, permanent platform

**Mechanics**:
- Lems walk on top
- No special interactions
- Foundation of all levels

**Design Use Cases**:
- Starting platforms
- Landing zones
- Structural support
- Safe zones

**Player Strategy**: Core building block for bridges and paths.

---

### Crumbler Block (Orange) - Type 3
**Behavior**: Breaks after a Lem reaches its center and exits

**Mechanics**:
1. Lem enters block (walks onto it)
2. Lem reaches center - block darkens (visual warning)
3. Lem exits block - block crumbles and disappears after 0.1s
4. One-time use only

**Design Use Cases**:
- Create time pressure
- Force commitment to a path
- Prevent backtracking
- Add consequence to movement

**Player Strategy**: Must plan entire path before committing, since return is impossible.

**Example Puzzle**: "Bridge of No Return" - Player must use crumblers to cross, but cannot go back if they make a mistake.

---

### Transporter Block (Yellow) - Type 4
**Behavior**: Moves Lem along a predefined route, then reverses route on next use

**Mechanics**:
1. Lem reaches center - block begins moving along route
2. Route format: `L2 U3 R1` (Left 2, Up 3, Right 1)
3. Lem moves with the block to destination
4. Next Lem triggers reverse route
5. Route path **blocks placement** - cannot place other blocks in the transporter's travel path

**Design Use Cases**:
- Vertical movement (elevators)
- Horizontal conveyors
- Complex paths around obstacles
- Timed sequences

**Configuration**:
- Define routes in the LevelDefinition's inventory entries
- Format: `L` (left), `R` (right), `U` (up), `D` (down) + number of cells
- Example: `U5` moves up 5 cells
- Multiple steps: `L2 U3 R4 D1`

**Player Strategy**: Must understand route pattern and timing to use effectively.

**Example Puzzle**: "Elevator Sequence" - Multiple transporters with different routes create a timing puzzle.

**Technical Note**: The inventory UI shows a route icon for each unique route configuration.

---

### Teleporter Block (Magenta) - Type 2
**Behavior**: Instantly transports Lem to paired teleporter

**Mechanics**:
1. Requires exactly 2 teleporters with matching flavor ID (A, B, C, etc.)
2. Lem reaches center - brief pause (configurable)
3. Instant teleport to pair destination
4. Post-teleport pause prevents instant re-teleport
5. Cooldown system (1.0s) prevents infinite loops

**Design Use Cases**:
- Skip sections of the level
- Create shortcuts vs. safe routes
- Split paths that converge
- Non-linear level structure

**Configuration**:
- Set `flavorId` in inventory config (e.g., "A", "B", "C")
- Each pair must have matching flavor
- Labels show flavor on block in Level Designer mode

**Player Strategy**: Must identify teleporter pairs and plan destination.

**Example Puzzle**: "Three Paths" - Multiple teleporter pairs (A, B, C) lead to different sections, only one leads to goal.

**Validation**: Game prevents placing unpaired teleporters (must have exactly 2).

---

### Key Block (Gold) - Type 5
**Behavior**: Holds a collectible key that attaches to Lem

**Mechanics**:
1. Key floats above key block (visual indicator)
2. Lem reaches center - key attaches to Lem
3. Key follows Lem as it walks
4. Animator parameter `HasKey` updates to true
5. Lem can carry key to Lock block

**Design Use Cases**:
- Create fetch quests (get key, bring to lock)
- Lock sections until key collected
- Multiple keys for multiple locks
- Key as "cost" to proceed

**Visual**: Gold key sprite floats above block.

**Player Strategy**: Must plan path from key to lock, avoiding hazards with key.

---

### Lock Block (Silver/Black) - Type 6
**Behavior**: Accepts keys and anchors them permanently

**Mechanics**:
1. Lem with key reaches lock center
2. Key transfers to lock and becomes anchored
3. Lem continues without key
4. Animator parameter `HasKey` updates to false
5. Filled locks counted for win condition

**Design Use Cases**:
- Goal markers (collect all keys, fill all locks)
- Gates that open when lock filled
- Sequential unlocking puzzles
- Resource management (multiple keys/locks)

**Visual**: Changes appearance when key is inserted.

**Player Strategy**: Determine most efficient path to fill all locks.

**Example Puzzle**: "Keymaster" - 3 keys scattered across level, must fill 3 locks to win. Crumblers prevent backtracking.

---

## Inventory System & Configuration

The inventory system controls what blocks players can place and in what quantities.

### Understanding Inventory Entries

Each inventory slot represents one **inventory entry**:
- **Block Type**: Walk, Teleporter, Crumbler, etc.
- **Display Name** (optional): Label shown in the UI
- **Flavor ID**: For teleporters (A, B, C) or transporter variants
- **Route Steps**: For transporters - defines movement path
- **Inventory Group ID**: Shared pool key for multiple entries
- **Max Count**: Total *units* available (pairs if `isPairInventory` is true)
- **Current Count**: Runtime value; resets to max on load
- **Pair Inventory**: `isPairInventory` + `pairSize` (teleporters use 2 placements per unit)

**Pair inventory behavior**: The UI shows remaining *pairs/units*, not remaining individual placements. After placing the first block of a pair, the count won’t drop until the pair is completed.

### Configuring Inventory in LevelDefinition

Inventory is configured within the **LevelDefinition** asset (Inspector → “Inventory Configuration”, or edit `levelDataJson`). When designing levels:

1. Create or select a **LevelDefinition** asset
2. Edit the `levelDataJson` field to include inventory entries
3. Inventory entries are stored in the `inventoryEntries` array

#### Example: Basic Platform Level
```
Entry 0:
  Display Name: "Platform"
  Block Type: Walk
  Max Count: 10
  Flavor ID: (leave empty)
  Route Steps: (leave empty)
```

#### Example: Teleporter Puzzle
```
Entry 0:
  Display Name: "Teleporter A"
  Block Type: Teleporter
  Flavor ID: "A"
  Max Count: 2
  Is Pair Inventory: true
  Pair Size: 2

Entry 1:
  Display Name: "Teleporter B"
  Block Type: Teleporter
  Flavor ID: "B"
  Max Count: 2
  Is Pair Inventory: true
  Pair Size: 2
```

**Important**: Teleporters use `isPairInventory=true` with `pairSize=2`, meaning each **pair** grants two placements. The pair count drops after the second placement.

#### Example: Transporter with Route
```
Entry 0:
  Display Name: "Elevator"
  Block Type: Transporter
  Max Count: 1
  Route Steps:
    - "U5"
  (or multiple steps):
    - "L2"
    - "U3"
    - "R4"
```

### Inventory Groups (Shared Counts)

Multiple entries can share the same inventory pool:

```
Entry 0:
  Display Name: "Small Platform"
  Block Type: Walk
  Inventory Group ID: "platforms"
  Max Count: 15

Entry 1:
  Display Name: "Large Platform"
  Block Type: Walk
  Inventory Group ID: "platforms"
  Max Count: 15
```

Both entries draw from the same pool of 15 blocks - placing one reduces the count for both.

**Note**: If entries in a group have different `maxCount`, the first value wins and the rest are normalized.

### Level Designer mode (Designer)

When in Level Designer mode:
- All block types show **infinite supply** (INF)
- You're designing the level, not playing it
- Place permanent blocks freely

When in Build Mode or Play Mode:
- Inventory limits apply
- Counts decrement on placement (pairs decrement after both placements)
- Reflects player experience

---

## Advanced Level Design

### Creating Multi-Stage Puzzles

**Concept**: Break levels into distinct sections with different challenges.

**Technique**:
1. Use teleporters to transition between stages
2. Each stage has different block types available
3. Keys/locks gate progression
4. Crumblers prevent returning to previous stage

**Example**: "Three Trials"
- Stage 1: Basic platforming (Default blocks only)
- Teleporter to Stage 2
- Stage 2: Timing challenge (Crumblers)
- Collect key, teleporter to Stage 3
- Stage 3: Vertical puzzle (Transporters)
- Fill lock to win

---

### Resource Management Puzzles

**Concept**: Force players to optimize block usage.

**Technique**:
1. Provide fewer blocks than obvious solution needs
2. Multiple paths, but only one is efficient enough
3. Dead-end paths waste blocks
4. Crumblers consume blocks permanently

**Example**: "The Gauntlet"
- 10 Default blocks available
- Obvious path needs 12 blocks
- Clever path uses crumblers and only needs 8 blocks
- Forces experimentation

---

### Timing and Sequence Puzzles

**Concept**: Use reversing transporters to create timing challenges.

**Technique**:
1. Place multiple transporters with different routes
2. First Lem triggers forward routes
3. Second Lem faces reversed routes
4. Must coordinate timing between multiple Lems

**Example**: "Rush Hour"
- 2 Lems start at different positions
- Transporters create moving platforms
- Must time when each Lem triggers transporter
- Both must reach goal

---

### Choice and Consequence

**Concept**: Give players multiple valid strategies with tradeoffs.

**Technique**:
1. Multiple teleporter pairs lead to different areas
2. Each area offers different resources or challenges
3. Some paths are risky but fast
4. Others are safe but consume more resources

**Example**: "The Shortcut"
- Safe path: Uses 15 blocks, no hazards
- Risky path: Uses 8 blocks, but includes crumblers and precise jumps
- Players choose their preferred risk level

---

### Exploration and Discovery

**Concept**: Hide optimal solutions or bonuses.

**Technique**:
1. Create obvious solution that works but is inefficient
2. Hide better solution behind creative block placement
3. Use keys/locks as optional objectives
4. Reward experimentation with alternate paths

**Example**: "The Hidden Key"
- Level is completable without collecting key
- Key is behind difficult platforming challenge
- Filling the lock reveals congratulations message
- Completionists must find the hidden path

---

## Playtesting & Iteration

### The Iteration Loop

1. **Design**: Create level structure in Level Designer mode
2. **Define**: Set constraints (placeable spaces) in Level Designer mode
3. **Test**: Play in Play Mode - Can you solve it?
4. **Observe**: Watch for issues (too hard? too easy? confusing?)
5. **Adjust**: Return to Level Designer mode and tweak
6. **Repeat**: Iterate until it feels right

### What to Test For

#### Is it solvable?
- Can you complete it yourself?
- Are there enough blocks?
- Are placeable spaces positioned correctly?
- Do transporters/teleporters work as intended?

#### Is it fun?
- Does the solution feel clever?
- Are there multiple valid approaches?
- Is there an "aha!" moment?
- Does it respect the player's time?

#### Is it clear?
- Do players understand the goal?
- Are block mechanics intuitive?
- Does visual feedback help understanding?
- Are special blocks obviously different?

#### Is it balanced?
- Not too easy (trivial placement)
- Not too hard (impossible without pixel-perfect placement)
- Just right (requires thought but achievable)

### Common Pitfalls

**Too Many Placeable Spaces**: If players can place blocks almost anywhere, there's no puzzle - just a construction exercise.

**Solution**: Limit placeable spaces to create meaningful constraints.

---

**Too Few Placeable Spaces**: If only one specific placement works, players resort to trial-and-error rather than strategy.

**Solution**: Allow multiple valid solutions, reward creativity.

---

**Unclear Goals**: Players don't know what they're trying to accomplish.

**Solution**: Make the goal obvious - clear path from start to finish, visible locks that need keys, etc.

---

**Hidden Mechanics**: Players don't understand how special blocks work.

**Solution**: Introduce mechanics gradually. First level with crumblers should make their behavior obvious.

---

**Frustrating Precision**: Levels that require pixel-perfect timing or placement.

**Solution**: Grid-based design naturally prevents this - embrace the grid.

---

**Unsolvable After Mistake**: Player makes one wrong placement and level becomes impossible.

**Solution**: Use the snapshot system - Play Mode preserves the state, so players can reset. Or design more forgiving levels.

---

## Best Practices & Design Patterns

### The "Tutorial Level" Pattern

Introduce one mechanic at a time:

1. **Level 1**: Default blocks only - teach basic platforming
2. **Level 2**: Introduce crumblers - simple one-way path
3. **Level 3**: Introduce transporters - single elevator
4. **Level 4**: Combine crumblers + transporters
5. **Level 5**: Introduce teleporters
6. **Level 6**: Everything together - real puzzle

### The "A-ha Moment" Pattern

Create levels where the solution is non-obvious but feels clever when discovered:

- Obvious approach fails (not enough blocks)
- Hidden approach works (using crumbler as temporary platform)
- Player feels smart for discovering it

### The "Fork in the Road" Pattern

Give players choices:

- Path A: Safe but expensive (uses many blocks)
- Path B: Risky but efficient (fewer blocks, uses crumblers)
- Path C: Creative but requires insight (teleporter shortcut)

### The "Gatekeeper" Pattern

Use keys/locks to control access:

- Early game: Collect key
- Mid game: Solve puzzle to reach lock
- Late game: Fill lock to proceed
- Separates level into before/after sections

### The "Chain Reaction" Pattern

One action triggers multiple consequences:

- Lem walks onto transporter
- Transporter moves, delivering Lem to key
- Lem collects key while transporter reverses
- Next Lem uses reversed route differently
- Creates temporal puzzle

---

## Troubleshooting

### "Cannot place block at index X: space is not placeable"

**Cause**: You're in Build Mode or Play Mode, trying to place a block in a non-placeable space.

**Solution**:
1. Press `E` to enter Level Designer mode
2. Move cursor to the desired space
3. Press `Space` to mark it as placeable (a border appears)
4. Return to Build Mode or Play Mode

---

### "Cannot place Teleporter: No matching pair found"

**Cause**: Teleporter requires exactly 2 blocks with the same flavor ID.

**Solution**:
1. Ensure your inventory config has `maxCount: 2` for that teleporter entry
2. Place the first teleporter
3. Place the second teleporter with matching flavor
4. Both must be placed before validation passes

---

### "Cannot place Transporter: Route path is blocked"

**Cause**: The transporter's route intersects an existing block or another transporter's route.

**Solution**:
1. Check the route path (inventory preview shows route icon)
2. Remove blocking blocks
3. Adjust route in the LevelDefinition's inventory entries to avoid obstacles
4. Place transporter in different location

---

### Cursor shows "not placeable" and won't place blocks

**Cause**: Current space is marked as non-placeable.

**Solution**: See "Cannot place block at index X" above.

---

### Lem falls through blocks

**Cause**: Lem was placed in mid-air or block was removed.

**Solution**:
1. Enter Level Designer mode
2. Press `L` to place Lem on a solid, permanent block
3. Ensure the block beneath is permanent (placed with `B`, not `Space`)

---

### Lem walks in wrong direction

**Cause**: Lem's facing direction wasn't set correctly.

**Solution**:
1. In Level Designer mode, move cursor to Lem
2. Press `L` to flip direction
3. Test in Play Mode

---

### Level won't load

**Cause**: Save file corrupted or incompatible version.

**Solution**:
1. Check console for specific error message
2. Press `Ctrl+Shift+S` to find save location
3. Delete `current_level.json` to start fresh
4. Recreate level from scratch

---

### Overworld shows no levels for a world

**Cause**: The `LevelDefinition.worldId` does not match the `WorldData.worldId`, or the asset is not under `Resources/Levels/LevelDefinitions`.

**Solution**:
1. Open the LevelDefinition asset and verify its `worldId`
2. Confirm a matching `WorldData` asset exists under `Resources/Levels/Worlds`
3. Ensure the LevelDefinition asset is stored under `Resources/Levels/LevelDefinitions`

---

### Overworld level text is missing

**Cause**: UGUI Text components do not have a valid font assigned.

**Solution**:
1. Assign a font on the template Text components (e.g., `LegacyRuntime.ttf`)
2. Enter Play mode to verify the cloned level buttons inherit the font

---

### Blocks disappear after exiting Play Mode

**Cause**: Blocks placed in Play Mode are temporary (player blocks, not permanent).

**Solution**: This is intentional! Play Mode is for testing. Only blocks placed with `B` in Level Designer mode are permanent.

---

### Inventory shows wrong counts

**Cause**: Inventory entries in LevelDefinition might have errors or shared groups misconfigured.

**Solution**:
1. Check the LevelDefinition asset's `levelDataJson` field
2. Verify `maxCount` values in the inventory entries
3. For shared groups, ensure `inventoryGroupId` matches
4. For pairs (teleporters), ensure `isPairInventory: true` and `pairSize: 2`

---

## Quick Reference

### Keyboard Shortcuts

#### Navigation (All Modes)
- `Arrow Keys` or `WASD` - Move cursor

#### Build Mode
- `Space` / `Enter` - Place block
- `Delete` / `Backspace` - Remove block
- `1-9` - Select block entry (first 9)
- `[` / `]` - Cycle block entries
- `E` - Switch to Level Designer mode
- `P` - Enter Play Mode

#### Level Designer mode
- `Space` / `Enter` - Mark placeable space
- `Delete` / `Backspace` - Clear placeable space + remove block or Lem
- `B` - Place permanent block
- `L` - Place/flip Lem
- `1-9` - Select block type
- `[` / `]` - Cycle block types
- `E` - Return to Build Mode
- `P` - Enter Play Mode

#### Play Mode
- `P` - Exit Play Mode

#### Save/Load (All Modes)
- `Ctrl+S` / `Cmd+S` - Save level
- `Ctrl+Shift+S` / `Cmd+Shift+S` - Show active LevelDefinition asset path
  - Designer-only; player progress is auto-saved on completion

---

### Block Type Quick Reference

| Type | Color | Behavior | Use Case |
|------|-------|----------|----------|
| Default | Cyan | Solid platform | Foundation, bridges |
| Crumbler | Orange | Breaks after use | One-way paths, time pressure |
| Transporter | Yellow | Moves along route | Elevators, conveyors |
| Teleporter | Magenta | Instant transport | Shortcuts, non-linear paths |
| Key | Gold | Collectible item | Fetch quests, gates |
| Lock | Silver | Accepts keys | Goals, progression gates |

---

### Inventory Entry Properties

| Property | Purpose | Example |
|----------|---------|---------|
| entryId | Unique id (auto-generated) | "Teleporter_A" |
| displayName | UI label | "Platform" |
| blockType | Block enum | Walk |
| maxCount | Total available | 10 |
| currentCount | Runtime remaining (resets on load) | 10 |
| flavorId | Variant ID | "A" for teleporter A |
| routeSteps | Transporter path | ["U5", "R3"] |
| inventoryGroupId | Shared pool | "platforms" |
| isPairInventory | Pair-based counting | true for teleporters |
| pairSize | Blocks per pair | 2 for teleporters |

---

### Route Format (Transporters)

- `L` + number - Left (e.g., `L3` = 3 cells left)
- `R` + number - Right (e.g., `R2` = 2 cells right)
- `U` + number - Up (e.g., `U5` = 5 cells up)
- `D` + number - Down (e.g., `D1` = 1 cell down)

**Multiple steps**: Separate with spaces or array entries: `["L2", "U3", "R4"]`

**Example**: `U5 R2 D5 L2` creates a rectangular loop.

---

### Visual Feedback Reference

| Element | Color/Indicator | Meaning |
|---------|----------------|---------|
| Cursor | Red | Non-placeable space |
| Cursor | Yellow | Editable (has block) |
| Cursor | Green | Empty placeable space |
| Border | Black | Placeable space marking |
| Border | Grey | Non-placeable space (in Level Editor) |
| Background | Build Mode |
| Background | Level Designer mode |
| Background | Black + skybox | Play Mode |

---

## What's Missing? (For Future Development)

This guide is comprehensive for the **current** system, but here are features that would enhance level creation:

### UI Improvements Needed
- [ ] **In-game tutorial system** - Guided first-level creation
- [ ] **Error message UI** - Show placement errors to user, not just console
- [ ] **Level validation tool** - Check if level is solvable before saving
- [ ] **Block inspector panel** - View block properties during editing
- [ ] **Undo/redo system** - Essential for iterative design
- [ ] **Level overview map** - See entire level layout at once

### Workflow Enhancements Needed
- [ ] **Level templates** - Pre-made starting points (empty, tutorial, etc.)
- [ ] **Copy/paste blocks** - Duplicate sections quickly
- [ ] **Multiple save slots** - More than one level at a time
- [ ] **Level metadata** - Name, author, difficulty, description
- [ ] **Level browser** - In-game level selection UI

### Documentation Improvements Needed
- [ ] **Visual route editor** - GUI for creating transporter routes instead of text
- [ ] **Example level pack** - 5-10 pre-made levels demonstrating concepts
- [ ] **Video tutorials** - Recorded walkthroughs of level creation
- [ ] **Design pattern library** - Documented puzzle structures
- [ ] **Inventory config template** - Copy-paste starting configs

### Technical Features Needed
- [ ] **Grid customization UI** - Change grid size without editing scene
- [ ] **Block variant system** - Multiple visual styles per type
- [ ] **Custom block behaviors** - Scripting API for designers
- [ ] **Difficulty analysis** - Automated puzzle complexity scoring
- [ ] **Accessibility options** - Colorblind mode, control remapping

### Polish Features Needed
- [ ] **Sound effects** - Block placement, Lem walking, crumbler breaking
- [ ] **Particle effects** - Visual feedback for block interactions
- [ ] **Animation polish** - Smoother transitions and movements
- [ ] **Camera controls** - Pan, zoom for large levels
- [ ] **Level thumbnail** - Auto-generated preview image

---

## Conclusion

You now have everything you need to create compelling levels in A Walk in the Park!

**Remember**:
1. Start simple - prototype in Level Designer mode
2. Add constraints - define puzzle in Level Designer mode
3. Test relentlessly - iterate in Play Mode
4. Think like a player - is it fun and fair?
5. Save often - preserve your work

**Best practices**:
- Introduce mechanics gradually
- Create multiple valid solutions
- Respect the player's time
- Make goals obvious
- Test with fresh eyes

**When stuck**:
- Step away and come back later
- Show your level to someone else
- Try breaking your own level
- Simplify first, complexify later

Happy level designing! May your puzzles be clever, your blocks well-placed, and your Lems always find their way home.

---

**Quick Links**:
- [README.md](README.md) - Project overview
- [PROJECT.md](PROJECT.md) - Technical architecture
- [LEVEL_EDITOR.md](LEVEL_EDITOR.md) - User guide for editor controls

**Need Help?**
- Check the console for debug messages
- Review the Troubleshooting section above
- Examine existing levels in the save folder
- Experiment fearlessly - Play Mode is non-destructive!
