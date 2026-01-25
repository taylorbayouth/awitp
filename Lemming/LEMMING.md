# Lemming Character System

## Overview

The lemming is an autonomous character that walks forward on blocks, turns around when hitting walls or edges, and demonstrates simple AI behavior.

## Core Behavior

### Walking
- Constant forward movement at configurable speed
- Walks on top of blocks (rigid colliders)
- Affected by gravity

### Turning
- Detects walls ahead using raycasts
- Detects edges/gaps using ground detection
- Turns 180° when obstacle detected
- Continues walking in opposite direction

### Physics
- Uses CharacterController for movement
- Collision detection with walls and blocks
- Ground detection for edge awareness
- Gravity applied continuously

## Components

### LemmingController
Main controller script handling:
- Forward movement
- Wall detection (forward raycast)
- Edge detection (downward raycast)
- Turn-around logic
- Ground check

### LemmingAnimator (Future)
Animation control for:
- Walk cycle
- Turn animation
- Fall animation
- Idle state

## Configuration

### Inspector Settings
- **walkSpeed**: Movement speed (units/second)
- **turnSpeed**: How fast to rotate when turning
- **wallDetectionDistance**: Raycast distance for wall detection
- **edgeDetectionDistance**: Raycast distance for edge detection
- **groundCheckRadius**: Size of ground detection sphere

### Layer Setup
- Lemming layer: Character layer
- Detection layers: Blocks, walls, ground

## Technical Details

### Movement System
Uses `CharacterController.Move()` for smooth movement:
```csharp
Vector3 moveDirection = transform.forward * walkSpeed;
controller.Move(moveDirection * Time.deltaTime);
```

### Wall Detection
Forward raycast detects obstacles:
```csharp
if (Physics.Raycast(transform.position, transform.forward, wallDetectionDistance))
{
    TurnAround();
}
```

### Edge Detection
Downward raycast detects gaps:
```csharp
Vector3 checkPos = transform.position + transform.forward * edgeDetectionDistance;
if (!Physics.Raycast(checkPos, Vector3.down, groundCheckRadius))
{
    TurnAround(); // No ground ahead, turn around
}
```

### Turn Around
180° rotation:
```csharp
transform.Rotate(0, 180, 0);
```

## Block Interaction

### Walkable Blocks
All block types are currently walkable:
- Collider required on blocks
- Lemming walks on top surface
- Blocks act as ground for lemming

### Future Block Types
Different blocks could have special behaviors:
- **Crumbler**: Collapses when lemming walks on it
- **Teleporter**: Teleports lemming to paired teleporter
- **Transporter**: Pushes lemming in specific direction

## Spawning

### LemmingSpawner (Future)
System to spawn lemmings at designated points:
- Spawn rate configuration
- Maximum lemming count
- Spawn point management

### Manual Spawn
For testing, place lemming in scene:
1. Create empty GameObject
2. Add LemmingController component
3. Add CharacterController component
4. Configure settings
5. Position above blocks

## Debugging

### Visual Debugging
Gizmos show:
- Wall detection ray (red)
- Edge detection ray (yellow)
- Ground check sphere (green)
- Forward direction indicator

### Debug Logs
Optional logging for:
- Turn events
- Wall hits
- Edge detection
- Ground status

## Best Practices

1. **Layer Masks**: Configure detection layers properly
2. **Detection Distance**: Adjust based on movement speed
3. **Ground Check**: Ensure reliable edge detection
4. **Collision**: Use CharacterController for smooth physics
5. **Performance**: Raycast optimization for multiple lemmings

## Known Issues

1. Fast movement may skip thin walls
2. Steep slopes need special handling
3. Multiple lemmings may stack/collide

## Future Enhancements

- Animation system integration
- Different lemming types (roles)
- Path finding for complex navigation
- Group behavior / flocking
- Sound effects (footsteps, collisions)
- Death/respawn system
