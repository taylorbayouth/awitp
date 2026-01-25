# A Walk in the Park (AWITP)

A Unity 3D puzzle game where players control "Lems" (lemming-like characters) that walk on blocks placed on a vertical wall. Features a comprehensive level editor with real-time testing and persistent save/load functionality.

## Features

### Level Editor
- **Three Game Modes**:
  - **Editor Mode**: Place and edit blocks
  - **Level Editor Mode**: Define placeable spaces and position Lems
  - **Play Mode**: Test your level with active Lem AI
- **Real-Time Testing**: Switch to Play mode instantly to test your level
- **Persistent Storage**: Save/load levels to JSON files (Ctrl+S / Ctrl+L)
- **Visual Feedback**: Color-coded cursor shows placeable/editable states

### Grid System
- **Dynamic Grid Manager**: Configurable grid size and cell dimensions
- **Auto-Centering**: Grid automatically centers around world origin
- **Visual Grid Lines**: LineRenderer with configurable opacity and width
- **Coordinate Conversion**: Utilities for index ↔ coordinates ↔ world position

### Block System
- **4 Block Types**: Default (cyan), Teleporter (magenta), Crumbler (orange), Transporter (yellow)
- **CenterTrigger System**: Precise detection when Lem reaches center/top of block
- **Player Detection**: Dual trigger/collision detection for reliable Lem interaction
- **Inventory Management**: Per-level constraints on block counts with cached references

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

### Navigation (All Modes)
- **Arrow Keys / WASD**: Move cursor around grid

### Editor Mode (Default)
- **Space / Enter**: Place selected block type
- **Delete / Backspace**: Remove block
- **1-4**: Select block type (1=Default, 2=Teleporter, 3=Crumbler, 4=Transporter)
- **E**: Switch to Level Editor Mode

### Level Editor Mode
- **Space / Enter**: Toggle placeable space marking (black border)
- **L**: Place/flip Lem character
- **Delete / Backspace**: Remove block or Lem
- **E**: Return to Editor Mode

### Play Mode
- **P**: Exit Play Mode (returns to Editor Mode)
- Lems walk automatically

### Save/Load
- **Ctrl+S / Cmd+S**: Save current level
- **Ctrl+L / Cmd+L**: Load saved level
- **Ctrl+Shift+S / Cmd+Shift+S**: Show save location in console

### Mode Switching
- **E**: Toggle between Editor and Level Editor modes
- **P**: Toggle Play Mode on/off

## Documentation

### Quick Start
See [LEVEL_EDITOR.md](LEVEL_EDITOR.md) for a complete user guide on creating levels.

### Technical Documentation
- **[PROJECT.md](PROJECT.md)** - Complete project architecture and technical overview
- **[LEVEL_EDITOR.md](LEVEL_EDITOR.md)** - User guide for the level editor
- **[SAVE_SYSTEM.md](SAVE_SYSTEM.md)** - Technical details on save/load system

### API Examples

**Save/Load:**
```csharp
// Save current level
GridManager.Instance.SaveLevel();

// Load saved level
GridManager.Instance.LoadLevel();

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
│   ├── BaseBlock.cs                # Base block class with error handling
│   ├── CenterTrigger.cs            # Precise center detection system
│   ├── BlockType.cs                # Block type enum
│   ├── BlockColors.cs              # Centralized color definitions
│   ├── BlockInventory.cs           # Block count management
│   ├── CrumblerBlock.cs            # Crumbling block implementation
│   ├── TransporterBlock.cs         # Moving platform block
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

1. Open Unity and load the Master.unity scene
2. Press Play to enter the level editor
3. Use arrow keys or WASD to move the cursor
4. Press 1-4 to select block types and Space to place
5. Press E to enter Level Editor Mode and mark placeable spaces
6. Press L to place Lems
7. Press P to test your level in Play Mode
8. Press Ctrl+S to save your level

See [LEVEL_EDITOR.md](LEVEL_EDITOR.md) for detailed instructions.

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

## Future Enhancements

### Planned Features
- Multiple level save slots
- Level naming and metadata
- Undo/redo system
- Block pattern templates
- Level validation (solvability check)

### Potential Improvements
- Custom level file format with compression
- Level thumbnail generation
- In-game level browser
- Multiplayer level sharing
- More block types (Bridge, Switch, Gate, Portal, etc.)
- Sound effects and music
- Particle effects for block interactions
