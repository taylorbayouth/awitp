# AWITP - Grid-Based Puzzle Game

A Unity 3D puzzle game with Lemmings-inspired mechanics featuring a grid-based block placement system and spatial awareness AI.

## Features

### Grid System
- **Dynamic Grid Manager**: Configurable grid size and cell dimensions
- **Visual Grid Lines**: Double-sided mesh rendering with proper z-ordering
- **Editor Mode**: Define placeable vs. non-placeable spaces
- **Orthographic Camera**: Top-down 2D view in 3D space

### Block System
- **4 Block Types**: Default, Teleporter, Crumbler, Transporter
- **Spatial Awareness**: Blocks detect surrounding objects in 8 directions (see [AGENTS.md](AGENTS.md))
- **Player Detection**: Trigger-based detection when player enters/reaches center
- **Visual Highlighting**: Color-based feedback system

### Inventory System
- **Per-Level Constraints**: Limited quantities for each block type
- **OnGUI Interface**: Real-time display of available/total blocks
- **Color-Coded UI**: Visual distinction between block types
- **Keyboard Selection**: Press 1-4 to select block types

### Visual System
- **Z-Fighting Solution**: Picture-frame mesh borders with proper layering
- **Three Render Layers**:
  - Grid lines (lowest)
  - Placeable/non-placeable borders (middle)
  - Cursor (highest)
- **Centralized Constants**: `BlockColors` and `RenderingConstants` utilities
- **Border Renderer**: Single hollow-square mesh with double-sided geometry

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

### Editor Mode (E key)
- **Mouse**: Hover over spaces to toggle placeable state
- **Left Click**: Toggle space (green cursor = editable)
- **E**: Exit editor mode

### Normal Mode
- **1-4 Keys**: Select block type
- **Mouse**: Hover over grid spaces
- **Left Click**: Place selected block (if available and placeable)
- **Right Click**: Remove block and return to inventory
- **E**: Enter editor mode

## Block Spatial Detection

Each block continuously monitors 8 surrounding grid spaces (N, NE, E, SE, S, SW, W, NW) for:
- Other blocks
- Player character
- Enemies
- Walls
- Any collider-based object

**API:**
```csharp
// Get all objects in a direction
List<Collider> objects = block.GetObjectsInDirection(Direction.North);

// Get only blocks in a direction
List<BaseBlock> blocks = block.GetBlocksInDirection(Direction.East);
```

See [AGENTS.md](AGENTS.md) for detailed documentation on the spatial detection system.

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GridManager.cs
│   │   ├── BaseBlock.cs
│   │   └── BlockType.cs
│   ├── Visualization/
│   │   ├── GridVisualizer.cs
│   │   ├── PlaceableSpaceVisualizer.cs
│   │   ├── BorderRenderer.cs
│   │   └── GridCursor.cs
│   ├── Editor/
│   │   ├── EditorController.cs
│   │   └── EditorModeManager.cs
│   ├── Inventory/
│   │   ├── BlockInventory.cs
│   │   └── InventoryUI.cs
│   ├── Utilities/
│   │   ├── BlockColors.cs
│   │   ├── RenderingConstants.cs
│   │   └── CameraSetup.cs
│   └── Setup/
│       └── GameInitializer.cs
└── Master.unity
```

## Technical Highlights

### Z-Fighting Solution
Complete elimination of z-fighting through:
- Single hollow-square mesh (picture frame style) instead of 4 segments
- `ZWrite Off` on all border materials
- Double-sided geometry with reversed triangle winding
- 100-unit render queue gaps between layers
- Sprites/Default shader for reliable 2D rendering

### Centralized Constants
All magic numbers eliminated via utility classes:
- `BlockColors`: Color definitions and block type utilities
- `RenderingConstants`: Layer heights, render queues, line widths

### Auto-Initialization
`GameInitializer` component ensures proper setup order:
1. GridManager validation
2. Component attachment (GridVisualizer, PlaceableSpaceVisualizer, etc.)
3. EditorModeManager before EditorController (dependency order)
4. InventoryUI setup with references
5. Camera configuration

## Version History

**v0.1-refactor** (Latest)
- Centralized color and rendering constants
- Comprehensive XML documentation
- Complete z-fighting solution
- Working inventory UI
- Stable codebase ready for feature development

## Development

Built with Unity 3D using C# scripting.

### Key Design Decisions
- OnGUI for UI (no external packages needed)
- Mesh-based rendering for all visual elements
- Physics-based spatial detection (OverlapBox)
- Singleton pattern for GridManager
- Factory pattern for block instantiation

## Future Features

Potential expansions:
- More block types (Bridge, Switch, Gate, etc.)
- Player character implementation
- Level system with progression
- Save/load functionality
- Sound effects and music
- Particle effects for block interactions
