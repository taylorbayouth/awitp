# A Walk in the Park

A Unity 3D puzzle game where players guide "Lems" (lemming-like characters) by strategically placing blocks to navigate through levels. The goal: collect all keys and place them in their corresponding locks.

## Game Overview

**For Players:**
- Solve spatial puzzles by placing blocks from your limited inventory
- Guide autonomous Lems through levels to collect keys and fill locks
- Two modes: **Build Mode** (place blocks) and **Play Mode** (test solution)
- World-based progression: complete all levels in a world to unlock the next

**For Designers:**
- Comprehensive designer tools for creating puzzle levels
- **Three Game Modes**:
  - **Build Mode**: Place blocks from inventory (player experience)
  - **Designer Mode**: Create level structure, mark placeable spaces, configure inventory (dev-only, press E)
  - **Play Mode**: Test levels with active Lem AI
- **Real-Time Testing**: Switch to Play mode instantly to test your level
- **Persistent Storage**: Save levels to JSON files (Ctrl+S)
- **Visual Feedback**: Color-coded cursor shows placeable/editable states

## Core Gameplay

### Player Experience
1. Start with a grid containing permanent blocks and marked placeable spaces (black borders)
2. Use limited block inventory to create paths for Lems
3. Lems walk autonomously - they turn at walls and fall off edges
4. Navigate Lems to collect keys (gold blocks)
5. Bring keys to locks (silver blocks) to fill them
6. **Win Condition**: All locks filled with keys

### Block Types
- **Default (Cyan)**: Solid platforms
- **Crumbler (Orange)**: Breaks after Lem exits - one-time use
- **Transporter (Yellow)**: Moving platforms following predefined routes
- **Teleporter (Magenta)**: Paired instant transport
- **Key (Gold)**: Collectible items that attach to Lems
- **Lock (Silver)**: Goal markers that accept keys

### Grid System
- **Dynamic Grid Manager**: Configurable grid size and cell dimensions
- **Auto-Centering**: Grid automatically centers around world origin
- **Visual Grid Lines**: LineRenderer with configurable opacity and width
- **Coordinate Conversion**: Utilities for index ↔ coordinates ↔ world position

### Block System
- **6 Block Types**: Default (cyan), Teleporter (magenta), Crumbler (orange), Transporter (yellow), Key (gold), Lock (silver)
- **CenterTrigger System**: Precise detection when Lem reaches center/top of block
- **Player Detection**: Dual trigger/collision detection for reliable Lem interaction
- **Inventory Management**: Per-level inventory entries with optional flavors and shared groups
- **Transporter Route Icons**: Inventory preview shows the transporter route shape for each route entry
- **Self-Contained Placement Validation**: Blocks define their own placement rules via virtual methods
  - Transporters block their route path from other placements
  - Teleporters validate they have exactly one matching pair
  - Custom blocks can override `CanBePlacedAt()`, `GetBlockedIndices()`, and `ValidateGroupPlacement()`

### Character System
- **Lem Controller**: Walking AI with gravity, collision detection, and turning logic
- **Frozen/Active States**: Lems freeze in editor modes, walk in Play mode
- **Direction Control**: Flip Lem facing direction during placement
- **Grid-Based Placement**: Lems snap to grid positions on blocks

### Save/Load System
- **Comprehensive State**: Saves blocks, placeable spaces, Lems, and grid settings
- **JSON Format**: Human-readable, debuggable, version-control friendly
- **Cross-Platform**: Uses Application.persistentDataPath for compatibility
- **Auto-Timestamping**: Tracks when levels were saved

### Visual System
- **Depth-Based Layering**: Cursor > Borders > Blocks >= Grid
- **Sorting Orders**: LineRenderer sorting for reliable 2D rendering
- **Configurable Constants**: All rendering values centralized in RenderingConstants.cs
- **Alpha Blending**: Smooth lines with opacity control

## Architecture

### Core Components

**GridManager** - Singleton managing grid state, block placement, and spatial queries
**BaseBlock** - Base class for all blocks with spatial detection and player interaction
**BlockInventory** - Per-level block type constraints and tracking
**EditorController** - Mouse-based placement and editing controls
**EditorModeManager** - Visual mode switching (normal/editor)

### Visualization

**GridVisualizer** - Renders grid lines
**PlaceableSpaceVisualizer** - Shows placeable (black) vs non-placeable (grey) borders
**GridCursor** - Visual cursor with state-based coloring
**InventoryUI** - OnGUI display of block inventory

### Utilities

**BlockColors** - Centralized color definitions for all block types
**RenderingConstants** - Z-layer heights and render queue values
**GameInitializer** - Auto-setup of all game systems

## Controls

### Player Controls (Build Mode + Play Mode)

#### Navigation (All Modes)
- **Arrow Keys / WASD**: Move cursor around grid

#### Build Mode (Player-Facing - Default)
- **Space / Enter**: Place selected block type
- **Delete / Backspace**: Remove block
- **1-9**: Select block entry (first 9 slots)
- **[ / ]**: Cycle block entries
- **P**: Toggle Play Mode to test solution

#### Play Mode
- **P**: Return to Build Mode
- Lems walk automatically

### Designer Controls (Dev-Only)

#### Designer Mode (Press E to access)
- **Space / Enter**: Toggle placeable space marking (black border)
- **B**: Place permanent block
- **L**: Place/flip Lem character
- **Delete / Backspace**: Remove block or Lem
- **1-9**: Select block type for permanent blocks
- **[ / ]**: Cycle block types
- **E**: Return to Build Mode

#### Save/Load (All Modes)
- **Ctrl+S / Cmd+S**: Save current level
- **Ctrl+Shift+S / Cmd+Shift+S**: Show save location in console

#### Mode Switching
- **E**: Toggle Designer Mode on/off (dev-only)
- **P**: Toggle Play Mode on/off

## Documentation

### For Designers
- **[HOW-TO-CREATE-A-LEVEL.md](HOW-TO-CREATE-A-LEVEL.md)** - Complete guide to designing puzzle levels
- **[DESIGNER-GUIDE.md](DESIGNER-GUIDE.md)** - Designer Mode controls and workflow
- **[LEVEL-SYSTEM-DESIGN.md](LEVEL-SYSTEM-DESIGN.md)** - Architecture for multi-level progression system

### For Developers
- **[PROJECT.md](PROJECT.md)** - Complete project architecture and technical overview
- **[IMPLEMENTATION-TASKS.md](IMPLEMENTATION-TASKS.md)** - Roadmap for level system implementation
- **[FEATURES-TO-BUILD.md](FEATURES-TO-BUILD.md)** - Future features and improvements

### API Examples

**Save/Load:**
```csharp
// Save current level
GridManager.Instance.SaveLevel();

// Load saved level
LevelManager.Instance.LoadLevel("tutorial_01");

// Check if save exists
bool exists = LevelSaveSystem.LevelExists();
```

## Project Structure

```
Assets/
├── Scripts/
│   ├── GridManager.cs              # Core grid state and management
│   ├── GridVisualizer.cs           # Grid line rendering
│   ├── GridCursor.cs               # Interactive cursor
│   ├── PlaceableSpaceVisualizer.cs # Placeable space borders
│   ├── BaseBlock.cs                # Base block class with placement validation
│   ├── CenterTrigger.cs            # Precise center detection system
│   ├── BlockType.cs                # Block type enum (6 types)
│   ├── BlockColors.cs              # Centralized color definitions
│   ├── BlockInventory.cs           # Block count management
│   ├── CrumblerBlock.cs            # Crumbling block implementation
│   ├── TransporterBlock.cs         # Moving platform block with route validation
│   ├── TeleporterBlock.cs          # Paired teleport block with cooldowns
│   ├── KeyBlock.cs                 # Block holding a collectible key
│   ├── LockBlock.cs                # Block that accepts keys
│   ├── RouteParser.cs              # Shared transporter route parsing utility
│   ├── LemController.cs            # Lem AI and movement
│   ├── LemSpawner.cs               # Lem spawning utility
│   ├── EditorController.cs         # Editor input handling
│   ├── EditorModeManager.cs        # Mode management
│   ├── GameMode.cs                 # Game mode enum
│   ├── BorderRenderer.cs           # Border rendering utility
│   ├── RenderingConstants.cs       # Rendering constants
│   ├── LevelData.cs                # Level data structure
│   ├── LevelSaveSystem.cs          # Save/load operations
│   ├── CameraSetup.cs              # Camera positioning
│   ├── GameInitializer.cs          # Game initialization
│   ├── InventoryUI.cs              # Inventory display
│   ├── ControlsUI.cs               # Controls help display
│   └── Editor/
│       └── LightmapperFix.cs       # Unity lightmapper fix
├── Resources/
│   └── Blocks/                     # Block prefabs
│       ├── Block_Key.prefab
│       └── Block_Lock.prefab
└── Master.unity                     # Main scene
```

## Technical Highlights

### Rendering System
- **Depth-Based Layering**: Z-position separation prevents conflicts
- **Sorting Orders**: LineRenderer sorting for reliable 2D layering
- **Alpha Blending**: Legacy Shaders/Particles/Alpha Blended for smooth lines
- **Configurable Visuals**: All constants centralized in RenderingConstants.cs

### Save System
- **JSON Serialization**: Unity's JsonUtility for native support
- **Efficient Storage**: Only stores occupied spaces and placeable indices
- **Grid Size Flexibility**: Handles loading levels with different grid dimensions
- **Cross-Platform**: Uses Application.persistentDataPath

### Grid Architecture
- **Auto-Centering**: Grid automatically centers around world origin
- **Index-Based Addressing**: Efficient O(1) lookups for grid operations
- **Coordinate Utilities**: Comprehensive conversion methods
- **Validation**: All grid operations validate bounds

### Character AI
- **Physics-Based Movement**: Uses Rigidbody and gravity
- **Collision Detection**: Ground detection with raycasting
- **State Management**: Frozen/active states for editor/play modes
- **Direction Handling**: Automatic turning at edges and walls

## Quick Start

### Playing a Level
1. Open Unity and load the Master.unity scene
2. Press Play to start
3. Use arrow keys or WASD to move the cursor
4. Press 1-9 to select blocks from inventory and Space to place them in black-bordered spaces
5. Press P to test your solution in Play Mode
6. Collect all keys and fill all locks to win

### Designing a Level
1. Press E to enter Designer Mode (dev-only)
2. Press B to place permanent blocks
3. Press Space to mark placeable spaces (black borders)
4. Press L to place starting Lems
5. Press E to return to Build Mode
6. Press P to test your level
7. Press Ctrl+S to save

See [HOW-TO-CREATE-A-LEVEL.md](HOW-TO-CREATE-A-LEVEL.md) for detailed instructions.

## Development

Built with Unity 2022+ using C# scripting.

### Key Design Decisions
- **LineRenderer** for all visual elements (grid, borders, cursor)
- **JSON** for level serialization (human-readable, debuggable)
- **Physics-based** spatial detection (OverlapBox)
- **Singleton pattern** for GridManager
- **Factory pattern** for block instantiation
- **Region organization** for code clarity

### Architecture Patterns
- **State Management**: GameMode enum with mode-specific behaviors
- **Event-Driven**: Lem placement tracking for reset functionality
- **Separation of Concerns**: Visualization separated from logic
- **Centralized Constants**: BlockColors and RenderingConstants utilities
- **Performance Optimization**: Cached component references to avoid FindObjectOfType
- **Error Handling**: Comprehensive try-catch blocks with detailed logging
- **Self-Contained Validation**: Blocks define their own placement rules via virtual methods
- **Template Method Pattern**: BaseBlock provides hooks (OnPlayerEnter, OnPlayerExit, OnPlayerReachCenter) for subclasses

## Recent Improvements

### Code Quality (v1.1)
- **Comprehensive Comments**: All major classes now have detailed developer comments explaining architecture, responsibilities, and design decisions
- **Error Handling**: Try-catch blocks added to critical operations with detailed error logging
- **Performance Optimization**: Cached component references eliminate expensive FindObjectOfType calls
- **Enhanced Logging**: Debug.Log statements added to track system behavior during development
- **Input Validation**: All public methods validate parameters before execution
- **Code Documentation**: XML summary comments on all public methods and properties

### Bug Fixes
- Fixed FindObjectOfType performance issues by implementing static caching
- Improved error messages to include context (class name, grid index, block type)
- Added null checks throughout GridManager and BaseBlock

## Contributing

When adding new features:
1. Document code with XML summary comments
2. Update relevant .md files
3. Add constants to RenderingConstants.cs or BlockColors.cs
4. Use regions to organize code
5. Add comprehensive error handling with try-catch blocks
6. Include Debug.Log statements for critical operations
7. Cache component references instead of using FindObjectOfType
8. Test in all three game modes

## Current Development Focus

### In Progress
- **Level System Architecture**: World-based progression (Baba Is You style)
  - Multiple levels per world
  - Complete all levels to unlock next world
  - Level selection UI
- **Prefab-Based Architecture**: Centralized block/Lem prefabs for reusability
- **Code Organization**: Consolidate block code, improve maintainability

### Planned Features
- Level progression system (3-5 levels per world)
- World unlock system
- Level selection screen
- Prefab-based blocks and Lems
- Undo/redo system for Build Mode
- Level validation (solvability check)

### Future Improvements
- Level thumbnail generation
- Additional block types (Bridge, Switch, Gate, etc.)
- Sound effects and music
- Particle effects for block interactions
- Level sharing/export system
