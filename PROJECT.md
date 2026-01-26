# A Walk in the Park (AWITP) - Project Documentation

## Overview

A Walk in the Park is a 2D grid-based puzzle game built in Unity where players control "Lems" (lemming-like characters) that walk on blocks placed on a vertical wall. The game features a comprehensive level editor that allows designers to create, edit, and test levels in real-time.

## Project Architecture

### Core Systems

#### Grid System
- **GridManager** - Central singleton managing the grid, blocks, and Lems
- **GridVisualizer** - Renders grid lines on the XY plane
- **PlaceableSpaceVisualizer** - Shows which grid spaces can have blocks placed
- **GridCursor** - Interactive cursor for navigating the grid

#### Block System
- **BaseBlock** - Base class for all block types with collision detection
- **BlockType** enum - Defines available block types (Default, Teleporter, Crumbler, Transporter)
- **BlockColors** - Centralized color management for all visual elements
- **BlockInventory** - Manages block counts per entry (supports flavors and shared groups)
- **LevelBlockInventoryConfig** - Optional scene config for inventory entries
- **Transporter Validation** - Prevents placement if route intersects existing blocks

#### Character System
- **LemController** - Controls Lem movement, physics, and AI
- **LemSpawner** - Handles Lem spawning at specified grid positions

#### Editor System
- **EditorController** - Handles all editor input and mode switching
- **EditorModeManager** - Manages transitions between Editor, Level Editor, and Play modes
- **GameMode** enum - Defines the three game modes

#### Rendering System
- **RenderingConstants** - Centralized constants for depths, sorting, line widths, and opacity
- **BorderRenderer** - Reusable component for drawing square borders with LineRenderer

#### Save/Load System
- **LevelData** - Serializable data structure for level state
- **LevelSaveSystem** - Handles file I/O for saving/loading levels to JSON

### Game Modes

The game has three distinct modes:

1. **Editor Mode** - Place and edit blocks on the grid
2. **Level Editor Mode** - Define placeable spaces and place Lems
3. **Play Mode** - Test the level with active Lem AI

### Coordinate System

The game uses a 2D grid on the XY plane:
- **X axis** - Horizontal (left/right)
- **Y axis** - Vertical (up/down)
- **Z axis** - Depth (camera looks from -Z toward +Z)
- **Grid Origin** - Auto-calculated to center the grid at world origin (0,0,0)

## File Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GridManager.cs           # Grid state and block/Lem management
│   │   ├── GridVisualizer.cs        # Grid line rendering
│   │   ├── GridCursor.cs            # Interactive cursor
│   │   └── PlaceableSpaceVisualizer.cs
│   ├── Blocks/
│   │   ├── BaseBlock.cs             # Base block class
│   │   ├── BlockColors.cs           # Color definitions
│   │   └── BlockInventory.cs        # Block count management
│   ├── Characters/
│   │   ├── LemController.cs         # Lem AI and movement
│   │   └── LemSpawner.cs            # Lem spawning logic
│   ├── Editor/
│   │   ├── EditorController.cs      # Editor input handling
│   │   ├── EditorModeManager.cs     # Mode management
│   │   └── GameMode.cs              # Game mode enum
│   ├── Rendering/
│   │   ├── RenderingConstants.cs    # Rendering constants
│   │   └── BorderRenderer.cs        # Border rendering utility
│   ├── SaveSystem/
│   │   ├── LevelData.cs             # Level data structure
│   │   └── LevelSaveSystem.cs       # Save/load operations
│   └── Utilities/
│       ├── CameraSetup.cs           # Camera positioning
│       └── GameInitializer.cs       # Game initialization
└── Master.unity                      # Main scene
```

## Key Features

### Level Editor
- Real-time block placement and editing
- Visual feedback with color-coded cursor states
- Lem placement and orientation control
- Placeable space marking system
- Save/load levels to persistent storage

### Block Types
- **Default** - Standard platform blocks (cyan)
- **Teleporter** - Transport blocks (magenta)
- **Crumbler** - Breakable blocks (orange)
- **Transporter** - Moving blocks (yellow)
  - Placement is blocked if the route path intersects existing blocks

### Rendering Features
- Configurable line widths for grid, borders, and cursor
- Adjustable grid line opacity
- Depth-based layering with sorting orders
- Alpha blended rendering for smooth lines

## Technical Details

### Rendering Pipeline
- Uses Unity's LineRenderer with "Legacy Shaders/Particles/Alpha Blended"
- Depth ordering: Cursor (-0.015) > Borders (-0.01) > Blocks (0) >= Grid (0)
- Sorting orders: Grid (0) < Borders (1) < Cursor (2)
- Configurable line widths and opacity in RenderingConstants.cs

### Save System
- Saves to `Application.persistentDataPath/Levels/`
- JSON format for easy editing and debugging
- Stores: grid settings, block placements, placeable spaces, Lem placements
- Automatic timestamping for save files

### Grid System
- Automatic centering around world origin
- Index-based grid addressing (0 to width*height-1)
- Coordinate conversion utilities (index ↔ coordinates ↔ world position)
- Validation for all grid operations

## Development Guidelines

### Code Organization
- Keep related functionality together in regions
- Use summary comments for all public methods
- Centralize constants in dedicated classes (RenderingConstants, BlockColors)
- Follow Unity naming conventions (PascalCase for public, camelCase for private)

### Adding New Block Types
1. Add new entry to BlockType enum
2. Add color to BlockColors.GetColorForBlockType()
3. Optionally extend BaseBlock with specialized behavior
4. Add to EditorController.HandleBlockTypeSwitch() if needed

### Modifying Rendering
- Update RenderingConstants.cs for depth, sorting, or line width changes
- Opacity changes are applied through Color.a in the rendering components
- Maintain depth separation to avoid z-fighting

## Testing

### Editor Testing
1. Enter Play mode in Unity
2. Use Editor Mode (default) to place blocks
3. Press E to enter Level Editor Mode
4. Mark placeable spaces and place Lems
5. Press P to test in Play Mode
6. Press P again to return to Editor Mode

### Save/Load Testing
1. Create a level in the editor
2. Press Ctrl+S (Cmd+S on Mac) to save
3. Make changes or clear the level
4. Press Ctrl+L (Cmd+L on Mac) to restore

## Known Considerations

### Rendering
- Line widths below 0.01f may appear inconsistent due to sub-pixel rendering
- Use thicker lines (0.03-0.05f) for interactive elements (cursor, borders)
- Grid can use thinner lines (0.01f) as it's a background reference

### Performance
- Grid system is optimized for up to 100x100 grids
- LineRenderer instances are minimal (grid lines, placeable borders, cursor)
- Block detection uses Unity's Physics.OverlapBox (cached per frame)

## Future Enhancements

### Planned Features
- Multiple level save slots
- Level naming and metadata
- Undo/redo system for editor operations
- Copy/paste for block patterns
- Level validation (ensuring levels are solvable)

### Potential Improvements
- Custom level file format with compression
- Level thumbnail generation
- In-game level browser
- Multiplayer level sharing

## Credits

Built with Unity 2022+
Uses Unity's built-in rendering and physics systems
