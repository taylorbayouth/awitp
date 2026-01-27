# Block Detection and Trigger System

## Overview

The game uses a sophisticated dual-detection system for tracking when Lems (player characters) interact with blocks. This system combines Unity's physics events with a custom CenterTrigger component for precise position detection.

## Architecture

### Dual Detection System

Every block uses **two complementary detection methods**:

1. **Trigger/Collision Events** - Broad detection when Lem is anywhere on/near the block
2. **CenterTrigger Component** - Precise detection when Lem reaches the center/top of the block

This redundancy ensures reliable detection even with fast-moving characters or edge cases.

## Coordinate System

The game uses the XY plane as the primary playing field:
- **X axis**: Horizontal (left/right)
- **Y axis**: Vertical (up/down) - gravity pulls down
- **Z axis**: Depth (camera at -Z looks toward +Z, blocks at Z=0)

Blocks exist on the XY plane forming a vertical wall that Lems walk on.

## Detection Components

### 1. BaseBlock Trigger Detection

BaseBlock implements Unity's physics callbacks to detect broad player presence:

```csharp
// Trigger-based detection (soft detection)
void OnTriggerEnter(Collider other)   // Player enters block area
void OnTriggerStay(Collider other)    // Player remains on block
void OnTriggerExit(Collider other)    // Player leaves block

// Collision-based detection (hard physical contact)
void OnCollisionEnter(Collision collision)   // Player collides with block
void OnCollisionStay(Collision collision)    // Player stays in contact
void OnCollisionExit(Collision collision)    // Player stops touching
```

**Why both triggers and collisions?**
- Triggers provide soft detection for proximity-based logic
- Collisions provide solid physical contact detection
- Redundancy ensures reliable detection in all scenarios

### 2. CenterTrigger System

`CenterTrigger` is a specialized component that detects when a Lem's feet reach the center point of a block's top surface.

#### Component Structure

```csharp
public class CenterTrigger : MonoBehaviour
{
    private BaseBlock owner;              // Parent block
    private SphereCollider sphere;        // Detection collider
    private bool isActive;                // Current trigger state

    public void Initialize(BaseBlock baseBlock);
    public void UpdateShape();
    public void SetEnabled(bool enabled);
}
```

#### How It Works

1. **Automatic Creation**: Each BaseBlock automatically creates a CenterTrigger child GameObject in `Start()`
2. **Sphere Collider**: Uses a small sphere collider positioned at the block's top center
3. **Foot Point Detection**: Tracks the Lem's foot position (bottom of collider bounds)
4. **Precise Triggering**: Only fires when foot point enters the sphere radius

#### Configuration

Configure in Inspector on any BaseBlock:

```csharp
[Header("Editor Visualization")]
public float centerTriggerRadius = 0.02f;           // Sphere radius
public float centerTriggerYOffset = 0.5f;           // Y offset (block scale)
public float centerTriggerWorldYOffset = 0f;        // Additional world offset
```

#### Visual Debugging

In Unity Editor:
- **Yellow wireframe sphere** shows the CenterTrigger detection zone
- **"Center" label** appears when Lem enters the trigger
- Gizmos visible when block is selected or during play

## Template Methods for Subclasses

BaseBlock provides virtual methods that subclasses can override:

### Player Interaction Methods
```csharp
// Called when Lem enters block (trigger or collision)
protected virtual void OnPlayerEnter() { }

// Called when Lem exits block (trigger or collision)
protected virtual void OnPlayerExit() { }

// Called when Lem reaches the center point (CenterTrigger)
protected virtual void OnPlayerReachCenter() { }
```

### Placement Validation Methods
```csharp
// Check if this block can be placed at the target index
public virtual bool CanBePlacedAt(int targetIndex, GridManager grid) { return true; }

// Return grid indices this block reserves (prevents other blocks from occupying)
public virtual int[] GetBlockedIndices() { return Array.Empty<int>(); }

// Validate group requirements after placement (e.g., teleporter pairs)
public virtual bool ValidateGroupPlacement(GridManager grid) { return true; }

// Return user-friendly error message for failed placement
public virtual string GetPlacementErrorMessage(int targetIndex, GridManager grid) { return null; }
```

## Implementation Examples

### Example 1: CrumblerBlock

Crumbles and darkens when player exits:

```csharp
public class CrumblerBlock : BaseBlock
{
    protected override void OnPlayerExit()
    {
        // Darken color
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.Lerp(
                renderer.material.color,
                Color.black,
                0.5f
            );
        }

        // Schedule destruction
        Destroy(gameObject, 1f);
    }
}
```

### Example 2: TransporterBlock

Moves along a route when player reaches center:

```csharp
public class TransporterBlock : BaseBlock
{
    protected override void OnPlayerReachCenter()
    {
        if (isTransporting || !isArmed) return;

        LemController lem = currentPlayer;
        if (lem == null) return;

        // Build movement path
        List<Vector2Int> steps = BuildSteps();

        // Start transport coroutine
        isArmed = false;
        SetCenterTriggerEnabled(false);
        StartCoroutine(TransportRoutine(lem, steps));
    }
}
```

### Example 3: TeleporterBlock (Actual Implementation)

```csharp
public class TeleporterBlock : BaseBlock
{
    // Finds matching teleporter with same flavorId
    protected override void OnPlayerReachCenter()
    {
        LemController lem = currentPlayer;
        if (lem == null || IsOnCooldown(lem)) return;

        TeleporterBlock destination = FindMatchingTeleporter();
        if (destination == null) return;

        StartCoroutine(TeleportSequence(lem, destination));
    }

    // Validates teleporter has exactly one matching pair
    public override bool ValidateGroupPlacement(GridManager grid)
    {
        return HasValidPair();
    }
}
```

### Example 4: KeyBlock and LockBlock

```csharp
public class KeyBlock : BaseBlock
{
    protected override void OnPlayerReachCenter()
    {
        // Lem collects the key when reaching center
        if (currentPlayer != null && keyItem != null)
        {
            currentPlayer.CollectKey(keyItem);
        }
    }
}

public class LockBlock : BaseBlock
{
    protected override void OnPlayerReachCenter()
    {
        // Lem deposits key into lock
        if (currentPlayer != null && currentPlayer.HasKey())
        {
            KeyItem key = currentPlayer.DropKey();
            key.AttachToLock(this);
        }
    }
}
```

## CenterTrigger Management

### Enabling/Disabling

Subclasses can control when center detection is active:

```csharp
// Disable center trigger (e.g., during cooldown)
SetCenterTriggerEnabled(false);

// Re-enable after some condition
SetCenterTriggerEnabled(true);
```

### Updating Shape

If you change trigger parameters at runtime:

```csharp
// Change radius
centerTriggerRadius = 0.05f;

// Update the trigger shape
UpdateCenterTrigger();
```

## Trigger State Visualization

BaseBlock tracks trigger states for editor debugging:

```csharp
private enum TriggerState
{
    None,    // No player interaction
    On,      // Player is on the block
    Center,  // Player reached center point
    Off      // Player just left
}
```

During play mode, labels show current state:
- **"On"** - Player anywhere on block
- **"Center"** - Player at center point (brief flash)
- **"Off"** - Player just exited

## Performance Notes

### Efficient Detection
- Trigger/collision callbacks only fire on state changes (not every frame)
- CenterTrigger uses sphere collider (very efficient)
- Static caching eliminates repeated FindObjectOfType calls

### Layer Masks
Configure colliders to use appropriate layers:
- Blocks should be on "Default" or custom "Blocks" layer
- Lems should be tagged "Player" for CompareTag checks
- Use Physics Layer Collision Matrix to control interactions

## Best Practices

### 1. Always Check for Null
```csharp
protected override void OnPlayerReachCenter()
{
    if (currentPlayer == null) return;

    // Safe to use currentPlayer here
}
```

### 2. Use SetCenterTriggerEnabled for Cooldowns
```csharp
// Disable during special state
SetCenterTriggerEnabled(false);

// Re-enable when ready
yield return new WaitForSeconds(cooldownTime);
SetCenterTriggerEnabled(true);
```

### 3. Leverage State Tracking
```csharp
protected override void OnPlayerEnter()
{
    // currentPlayer is automatically set by BaseBlock
    Debug.Log($"Player entered: {currentPlayer.name}");
}
```

### 4. Consider Edge Cases
- Multiple Lems (game currently limits to one, but architecture supports more)
- Rapid trigger enter/exit cycles
- Lem destroyed while on block

## Debugging Tips

### Enable Visual Gizmos
In Inspector on BaseBlock:
- ☑ Show Trigger Labels In Editor
- ☑ Show Trigger Gizmos

### Console Logging
Recent improvements added comprehensive logging:
```
[BaseBlock] Player entered trigger on Crumbler block at index 42
[BaseBlock] Player reached center of Transporter block at index 15
[BaseBlock] Player exited trigger on Default block at index 8
```

### Scene View Visualization
- Select any block to see trigger zones
- Watch labels update in real-time during play mode
- Yellow sphere = CenterTrigger zone
- Cyan box = OnTrigger zone

## Technical Implementation Details

### Why Dual Detection?
1. **Reliability**: If one system fails, the other catches it
2. **Different Use Cases**: Broad detection vs precise positioning
3. **Physics Edge Cases**: Fast-moving objects might miss one but not both

### Foot Point Calculation
```csharp
private static Vector3 GetFootPoint(Collider collider)
{
    Bounds bounds = collider.bounds;
    Vector3 center = bounds.center;
    return new Vector3(center.x, bounds.min.y, center.z);
}
```
Uses collider bounds minimum Y to find the bottom point.

### Trigger Update Logic
```csharp
private void UpdateCenterState(Collider other)
{
    Vector3 footPoint = GetFootPoint(other);
    float distance = Vector3.Distance(footPoint, transform.position);
    bool inside = distance <= sphere.radius;

    if (inside && !isActive)
    {
        isActive = true;
        owner.NotifyCenterTriggerEnter(other.GetComponent<LemController>());
    }
    else if (!inside && isActive)
    {
        isActive = false;
        owner.NotifyCenterTriggerExit();
    }
}
```

## Future Enhancements

Potential improvements to the detection system:

1. **Direction Detection**: Detect which direction player entered from
2. **Velocity Tracking**: React differently based on player speed
3. **Multi-Point Detection**: Multiple trigger zones (corners, edges, center)
4. **Event System**: Observable events instead of virtual methods
5. **Pooled Triggers**: Reuse CenterTrigger instances for performance

## Related Files

- **BaseBlock.cs** - Main block class with detection logic and placement validation
- **CenterTrigger.cs** - Precise center detection component
- **LemController.cs** - Player character with physics and collision
- **TransporterBlock.cs** - Moving platform using OnPlayerReachCenter and GetBlockedIndices
- **TeleporterBlock.cs** - Paired teleportation with ValidateGroupPlacement
- **CrumblerBlock.cs** - Breakable block using OnPlayerExit
- **KeyBlock.cs** - Collectible key block
- **LockBlock.cs** - Key receiver block
- **RouteParser.cs** - Shared utility for transporter route parsing
