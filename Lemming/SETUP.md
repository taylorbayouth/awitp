# Lemming Setup Guide

## Quick Start

### 1. Create Lemming GameObject

1. In Unity Hierarchy, create new GameObject (`GameObject > Create Empty`)
2. Name it "Lemming"
3. Position it above the grid (e.g., Y = 2)

### 2. Add Components

Add these components in order:

#### CharacterController
1. `Add Component > Character Controller`
2. Settings:
   - **Radius**: 0.25
   - **Height**: 1.0
   - **Center**: (0, 0.5, 0)
   - **Skin Width**: 0.01

#### LemmingController
1. `Add Component > Lemming Controller`
2. Settings:
   - **Walk Speed**: 2.0
   - **Wall Detection Distance**: 0.5
   - **Edge Detection Distance**: 0.5
   - **Ground Check Distance**: 1.5
   - **Detection Mask**: Everything

### 3. Add Visual Representation

#### Option A: Simple Cube (Quick Testing)
1. Create child GameObject: `GameObject > 3D Object > Cube`
2. Scale: (0.4, 0.8, 0.4)
3. Position: (0, 0, 0) - relative to parent

#### Option B: Custom Model
1. Import your lemming model into Assets
2. Drag prefab as child of Lemming GameObject
3. Adjust scale and position as needed

### 4. Configure Block Colliders

Ensure all blocks have colliders:
1. Blocks already have colliders (created as primitives)
2. Make sure blocks are on a layer lemmings can detect
3. Optional: Create "Block" layer for organization

### 5. Test

1. Enter Play mode
2. Lemming should:
   - Walk forward automatically
   - Turn around when reaching grid edge
   - Turn around when hitting walls
   - Stay on top of blocks

## Troubleshooting

### Lemming Falls Through Blocks
- Check CharacterController is touching blocks
- Verify blocks have colliders
- Check detection layer mask includes blocks

### Lemming Doesn't Turn at Edges
- Increase `edgeDetectionDistance`
- Check `groundCheckDistance` is appropriate
- Enable `showDebugRays` to visualize detection

### Lemming Doesn't Detect Walls
- Increase `wallDetectionDistance`
- Verify walls have colliders
- Check detection layer mask

### Lemming Moves Too Fast/Slow
- Adjust `walkSpeed` value
- Typical values: 1-5 units/second

## Advanced Configuration

### Multiple Lemmings

To add more lemmings:
1. Duplicate existing lemming GameObject
2. Position in different location
3. Each lemming operates independently

### Custom Starting Direction

Change initial facing direction:
- Rotate lemming GameObject in Y-axis
- 0° = Forward (+Z)
- 90° = Right (+X)
- 180° = Backward (-Z)
- 270° = Left (-X)

### Layer Setup (Optional)

For better organization:
1. Create layer: `Edit > Project Settings > Tags and Layers`
2. Add "Lemming" layer
3. Add "Blocks" layer
4. Set lemming to Lemming layer
5. Set blocks to Blocks layer
6. Configure detection masks accordingly

## Visual Debugging

Enable debug visualization in Inspector:
- Check `Show Debug Rays` in LemmingController
- Green ray = wall check (clear)
- Red ray = wall check (blocked)
- Yellow ray = edge check (no ground)

## Performance Tips

### Single Lemming
No optimization needed.

### 10-50 Lemmings
- Use object pooling for spawning
- Consider reducing raycast frequency
- Use layer masks efficiently

### 50+ Lemmings
- Implement LOD system
- Update lemmings in batches
- Consider spatial partitioning

## Next Steps

1. Add lemming visual (model/sprite)
2. Implement spawner system
3. Add block-specific behaviors
4. Create win/lose conditions
5. Add sound effects
