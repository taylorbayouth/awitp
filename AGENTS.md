# Block Spatial Detection System

## Overview

Every block in the game has built-in spatial awareness of its 4 surrounding grid spaces (Left, Right, Up, Down). This system runs continuously and allows blocks to detect and react to nearby objects, players, enemies, and other blocks.

## Coordinate System

The game uses the XY plane as the primary playing field:
- **X axis**: Horizontal (left/right)
- **Y axis**: Vertical (up/down) - gravity pulls down
- **Z axis**: Depth (camera at -Z looks toward +Z)

This creates a natural 2D side-scrolling platformer feel while using Unity's 3D physics.

## How It Works

### Detection Grid

Each block monitors 4 surrounding spaces in cardinal directions:

```
      Up
       |
Left - [B] - Right
       |
     Down
```

### Configuration

Detection is configured per-block via public fields:

```csharp
[Header("Detection Settings")]
public float detectionSize = 1f;           // Size of detection box (1 unit cube)
public LayerMask detectionLayerMask;       // What layers to detect
```

### Detection Method

Uses `Physics.OverlapBox` to find all colliders in each direction. Detection runs every frame via `Update()`.

## API

### Two Levels of Access

#### 1. Generic Detection (All Objects)

Returns all colliders on specified layers:

```csharp
List<Collider> objects = block.GetObjectsInDirection(BaseBlock.Direction.Right);
```

**Can detect:**
- Other blocks
- Player character
- Enemies
- Walls
- Triggers/pressure plates
- Any GameObject with a collider on the detection layer

#### 2. Block-Only Detection

Returns only objects with BaseBlock component:

```csharp
List<BaseBlock> blocks = block.GetBlocksInDirection(BaseBlock.Direction.Left);
```

**Use for:**
- Block-to-block interactions
- Chain reactions
- Connectivity checks
- Block-specific logic

### Available Directions

```csharp
public enum Direction
{
    Left,
    Right,
    Up,
    Down
}
```

## Use Cases

### 1. Transporter Block
```csharp
// Check if there's space to push player
List<Collider> objectsAhead = GetObjectsInDirection(Direction.Right);
bool canPush = objectsAhead.Count == 0;
```

### 2. Crumbler Chain Reaction
```csharp
// When crumbling, trigger adjacent crumblers
protected override void OnPlayerReachCenter()
{
    List<BaseBlock> adjacentBlocks = GetBlocksInDirection(Direction.Down);
    foreach (var block in adjacentBlocks)
    {
        if (block.blockType == BlockType.Crumbler)
        {
            block.DestroyBlock();
        }
    }
}
```

### 3. Wall Detection
```csharp
// Check if movement is blocked by walls
List<Collider> objects = GetObjectsInDirection(Direction.Left);
bool blockedByWall = objects.Any(c => c.CompareTag("Wall"));
```

## Implementation Details

### Performance
- Detection runs every frame (`Update()`)
- Uses `Physics.OverlapBox` with layer masking for efficiency
- Results are cached in dictionary until next frame

### Data Structure
```csharp
private Dictionary<Direction, List<Collider>> surroundingObjects;
```

### Detection Size
- Default: 1 unit cube (matches grid cell size)
- Slightly smaller (0.9f) to avoid overlap with self
- Configurable via `detectionSize` field

## Debugging

Visual debugging available in Scene view when block is selected:

```csharp
private void OnDrawGizmosSelected()
{
    // Red wireframes show 4 detection zones
    // Green wireframe shows block itself
}
```

## Layer Mask Configuration

Set `detectionLayerMask` in inspector to control what blocks can "see":

- **Everything**: All layers
- **Blocks Only**: Only block layer
- **Blocks + Player**: Detect blocks and player
- **Custom**: Mix and match for specific behaviors

## Best Practices

1. **Use appropriate detection level**: Generic for mixed objects, Block-only for block logic
2. **Check for null/empty**: Always verify results before using
3. **Configure layers**: Set layer masks appropriately for each block type
4. **Consider performance**: Heavy per-frame logic should be optimized
5. **Test edge cases**: Multiple objects in same direction, etc.
