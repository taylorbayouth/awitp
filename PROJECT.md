# A Walk in the Park (AWITP) - Project Documentation

## Overview

A Walk in the Park is a 2D grid-based puzzle game built in Unity where players control "Lems" (lemming-like characters) that walk on blocks placed on a vertical wall. The game features a comprehensive level editor that allows designers to create, edit, and test levels in real-time.

## Project Architecture

### Core Systems

#### Service Locator Pattern
- **ServiceRegistry** - Centralized service locator providing access to core game systems
  - Eliminates expensive FindObjectOfType calls
  - Provides type-safe access to singleton managers
  - Improves performance and maintainability

#### Grid System
- **GridManager** - Central singleton managing the grid with delegated subsystems:
  - **BlockPlacementManager** - Handles all block placement and removal operations
  - **LemPlacementManager** - Manages Lem tracking, spawning, and cleanup
  - **GridCursorManager** - Handles cursor state and visual feedback
  - **GridCoordinateSystem** - Pure coordinate math and grid-to-world conversions
- **GridVisualizer** - Renders grid lines on the XY plane
- **PlaceableSpaceVisualizer** - Shows which grid spaces can have blocks placed
- **GridCursor** - Interactive cursor for navigating the grid

#### Block System
- **BaseBlock** - Base class for all block types with collision detection and placement validation
- **BlockType** enum - Defines available block types (Walk, Teleporter, Crumbler, Transporter, Key, Lock)
- **BlockInventory** - Manages block counts per entry (supports flavors and shared groups, loaded from LevelDefinition)
- **RouteParser** - Canonical route utility for parsing, validating, and normalizing transporter routes
- **Placement Validation** - Self-contained rules via virtual methods:
  - `CanBePlacedAt(index, grid)` - Check if placement is allowed
  - `GetBlockedIndices()` - Return grid spaces this block reserves
  - `ValidateGroupPlacement(grid)` - Validate group requirements (e.g., teleporter pairs)

#### Character System
- **LemController** - Controls Lem movement, physics, and AI
- **LemSpawner** - Handles Lem spawning at specified grid positions

#### Builder System
- **BuilderController** - Handles all builder input and mode switching
- **GameModeManager** - Manages transitions between Build Mode, Level Designer mode, and Play Mode
  - **Synchronized Dual-Track Music System** - Both Build and Play soundtracks play continuously from start with volume crossfading
- **GameMode** enum - Defines the three game modes (Build, LevelDesigner, Play)

#### Input System
- **PointerInput** - Unified abstraction layer for mouse and touch input
  - Prioritizes touch over mouse for mobile compatibility
  - Uses `TouchPhase.Began` for precise tap detection
  - Integrates with UGUI EventSystem for UI interaction detection
- **Screen-to-Grid Raycasting** - Converts 2D screen positions to 3D grid coordinates
  - Camera.ScreenPointToRay for ray generation
  - Plane intersection on XY grid plane
  - Bounds validation and 1D index conversion
- **IPointerClickHandler** - UGUI interface for inventory slot selection
  - Automatically supports both mouse and touch
  - No platform-specific code needed

#### Rendering System
- **RenderingConstants** - Centralized constants for depths, sorting, line widths, and opacity
- **BorderRenderer** - Reusable component for drawing square borders with LineRenderer

#### Save/Load System
- **LevelData** - Serializable data structure for level state (grid, blocks, lems, inventory, camera)
- **LevelDefinition** - ScriptableObject that stores LevelData and optional themes
- **GameProgressData** - Player save data (completed levels, unlocked worlds, statistics)
- **ProgressManager** - Singleton managing player progress persistence (JSON to disk)
- **LevelManager** - Loads/instantiates levels, applies themes, tracks completion
- **WorldManager** - Manages world collections and progression

#### Level Theming System (Visual & Audio Variety)
- **LevelVisualTheme** - ScriptableObject for lighting, sky, fog, background elements
  - Directional light (color, direction, intensity, shadows)
  - Ambient lighting (skybox, flat, trilight modes)
  - Sky & background (custom skybox, camera color, prefab scenery)
  - Fog (linear, exponential, exponential-squared)
  - Optional post-processing volumes
- **LevelAudioTheme** - ScriptableObject for music and ambient sounds
  - Music track overrides (builder/play modes)
  - Ambient sound loops (wind, water, birds, etc.)
  - Random one-shot sounds (thunder, calls, etc.)
  - Integrates with GameModeManager's dual-track system
- **ThemePresets** - Quick-start presets (Day, Sunset, Night, Overcast, Industrial)
- **LevelVarietyUtils** - Color palettes, lighting calculations, atmosphere generators
- **Git-Friendly**: All themes are ScriptableObjects (text-based YAML)
- **Modular & Reusable**: One theme can be shared across many levels
- **Optional**: Levels work without themes (use defaults)

### Game Modes

The game has three distinct modes:

1. **Build Mode** - Player-facing mode where blocks are placed from inventory (default)
2. **Level Designer mode** - Dev-only mode to define placeable spaces and place Lems (press E)
3. **Play Mode** - Test the level with active Lem AI (press P)

### Coordinate System

The game uses a 2D grid on the XY plane:
- **X axis** - Horizontal (left/right)
- **Y axis** - Vertical (up/down)
- **Z axis** - Depth (camera looks from -Z toward +Z)
- **Grid Origin** - Auto-calculated to center the grid at world origin (0,0,0)

## File Structure

The project uses a **flat file structure** in Assets/Scripts/ for simplicity and ease of navigation. All scripts are in the root Scripts directory rather than nested folders.

```
Assets/
├── Scripts/                          # Flat structure - all scripts here
│   ├── ServiceRegistry.cs           # Service locator pattern
│   ├── GridManager.cs               # Grid state and delegated management
│   ├── BlockPlacementManager.cs     # Block operations subsystem
│   ├── LemPlacementManager.cs       # Lem tracking subsystem
│   ├── GridCursorManager.cs         # Cursor state subsystem
│   ├── GridCoordinateSystem.cs      # Pure coordinate math
│   ├── GridVisualizer.cs            # Grid line rendering
│   ├── GridCursor.cs                # Interactive cursor
│   ├── PlaceableSpaceVisualizer.cs  # Placeable space visuals
│   ├── BaseBlock.cs                 # Base block class
│   ├── BlockInventory.cs            # Block count management
│   ├── BlockType.cs                 # Block type enum
│   ├── BlockColors.cs               # Block color definitions
│   ├── WalkBlock.cs                 # Simple platform block
│   ├── CrumblerBlock.cs             # Breaking block
│   ├── TeleporterBlock.cs           # Paired teleport block
│   ├── TransporterBlock.cs          # Moving platform block
│   ├── KeyBlock.cs                  # Collectible key block
│   ├── LockBlock.cs                 # Goal lock block
│   ├── CenterTrigger.cs             # Center detection system
│   ├── RouteParser.cs               # Transporter route parsing
│   ├── LemController.cs             # Lem AI and movement
│   ├── LemSpawner.cs                # Lem spawning logic
│   ├── BuilderController.cs         # Builder input handling
│   ├── GameModeManager.cs           # Mode management
│   ├── GameMode.cs                  # Game mode enum
│   ├── RenderingConstants.cs        # Rendering constants
│   ├── BorderRenderer.cs            # Border rendering utility
│   ├── DebugLog.cs                  # Controlled logging utility
│   ├── LevelData.cs                 # Level data structure
│   ├── LevelDefinition.cs           # ScriptableObject level definition
│   ├── WorldData.cs                 # ScriptableObject world definition
│   ├── LevelSaveSystem.cs           # Save/load operations
│   ├── LevelManager.cs              # Level loading and management
│   ├── WorldManager.cs              # World progression system
│   ├── ProgressManager.cs           # Player progress tracking
│   ├── GameProgressData.cs          # Progress data structure
│   ├── CameraSetup.cs               # Camera positioning
│   ├── GameInitializer.cs           # Game initialization
│   ├── GameSceneInitializer.cs      # Scene-specific initialization
│   ├── InventoryUI.cs               # Inventory display
│   ├── ControlsUI.cs                # Controls help display
│   ├── MainMenuUI.cs                # Main menu UI controller
│   ├── WorldMapUI.cs                # World map UI controller
│   ├── LevelSelectUI.cs             # Level select UI controller
│   ├── VictoryScreenUI.cs           # Victory screen UI controller
│   └── Editor/                      # Editor-only scripts
│       ├── LevelDefinitionCreator.cs
│       └── WorldDataCreator.cs
└── Master.unity                      # Main game scene
```

## Key Features

### Level Editor
- Real-time block placement and editing
- Visual feedback for cursor states
- Lem placement and orientation control
- Placeable space marking system
- Save/load levels to persistent storage

### Block Types
- **Default** - Standard platform blocks
- **Teleporter** - Paired teleport blocks - requires exactly one matching pair
- **Crumbler** - Breakable blocks that crumble when Lem exits
- **Transporter** - Moving platform blocks with configurable routes
  - Placement blocked if route path intersects existing blocks or other transporter routes
  - Inventory preview renders a route icon for each unique route entry
- **Key** - Blocks holding collectible keys
- **Lock** - Blocks that accept keys to unlock

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
- Designer saves write LevelData JSON into `LevelDefinition` assets (`levelDataJson`)
- Optional file saves use `Application.persistentDataPath/Levels/`
- JSON format for easy editing and debugging
- Stores: grid settings, block placements, placeable spaces, Lem placements, inventory entries
- Automatic timestamping for LevelData snapshots

### Grid System
- Automatic centering around world origin
- Index-based grid addressing (0 to width*height-1)
- Coordinate conversion utilities (index ↔ coordinates ↔ world position)
- Validation for all grid operations

### Route System
- **Single source of truth**: `RouteParser.ParseRoute()` validates and expands route steps for transporters.
- **Normalization**: `RouteParser.NormalizeRouteSteps()` and `RouteParser.NormalizeRouteKey()` ensure consistent keys and display tokens.
- **Consumers**: Transporter movement, placement validation, inventory keys, and inventory UI route icons all use `RouteParser`.

## Development Guidelines

### Code Organization
- Flat file structure in Assets/Scripts/ for easy navigation
- Keep related functionality together in regions
- Use summary comments for all public methods
- Centralize constants in dedicated classes (RenderingConstants)
- Follow Unity naming conventions (PascalCase for public, camelCase for private)

### Service Access Pattern
- Use ServiceRegistry to access core systems instead of FindObjectOfType
- Example: `ServiceRegistry.Get<GridManager>()` instead of `FindObjectOfType<GridManager>()`
- Provides better performance and type safety
- Initialize services in ServiceRegistry.Awake()

### Manager Decomposition
- GridManager delegates to specialized subsystems:
  - BlockPlacementManager: Block operations
  - LemPlacementManager: Lem tracking
  - GridCursorManager: Cursor state
  - GridCoordinateSystem: Pure math
- This separation improves maintainability and testability

### Adding New Block Types
1. Add new entry to BlockType enum
2. Create a new class extending BaseBlock with specialized behavior
4. Override template methods as needed:
   - `OnPlayerEnter()` - Called when Lem enters block
   - `OnPlayerExit()` - Called when Lem exits block
   - `OnPlayerReachCenter()` - Called when Lem reaches block center
5. Optionally override placement validation methods:
   - `CanBePlacedAt(index, grid)` - Custom placement rules
   - `GetBlockedIndices()` - Reserve grid spaces
   - `ValidateGroupPlacement(grid)` - Group requirements (e.g., pairs)
   - `GetPlacementErrorMessage(index, grid)` - User-friendly error messages
6. Add case to BaseBlock.AddBlockComponent() factory method
7. Create prefab in Resources/Blocks/Block_{TypeName}.prefab

### Modifying Rendering
- Update RenderingConstants.cs for depth, sorting, or line width changes
- Opacity changes are applied through alpha values in the rendering components
- Maintain depth separation to avoid z-fighting

## Testing

### Editor Testing
1. Enter Play mode in Unity
2. Use Designer mode (default) to place blocks
3. Press E to enter Level Designer mode
4. Mark placeable spaces and place Lems
5. Press P to test in Play Mode
6. Press P again to return to Designer mode

### Save/Load Testing
1. Create a level in the editor
2. Press Ctrl+S (Cmd+S on Mac) to save
3. Make changes or clear the level
4. Restart Play (auto-loads the assigned LevelDefinition)

## Known Considerations

### Rendering
- Line widths below 0.01f may appear inconsistent due to sub-pixel rendering
- Use thicker lines (0.03-0.05f) for interactive elements (cursor, borders)
- Grid can use thinner lines (0.01f) as it's a background reference

### Overworld UI
- Overworld builds world cards and level buttons from `WorldData` and `LevelDefinition` assets under `Resources`.
- `LevelDefinition.worldId` must match `WorldData.worldId` or levels will not appear under the world.
- Level launch uses a pending level id (stored before loading `Master`), so ensure the Master scene loads the pending id on start.
- UGUI `Text` requires a valid font assignment; use `LegacyRuntime.ttf` for built-in fonts to avoid missing mesh errors.
- Level button positions are controlled by the UI script/layout at runtime, so the template's manual position will be overridden.

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

---

## Technical Deep Dives

### Inventory UI System

The **InventoryUI** is a sophisticated UGUI-based display system that manages real-time visualization of block inventory, pairs, selection states, and lock/win conditions.

#### Architecture
- **Event-Driven Dirty Flags**: Two separate flags minimize per-frame rebuilds
  - `_isDirty` - Full UI rebuild needed (inventory/mode/selection changes)
  - `_lockDirty` - Lock status update only (triggered by LockBlock events)
- **Slot Pool Pattern**: Reuses SlotUI objects instead of create/destroy
  - `EnsureSlot()` activates existing or creates new slots
  - `TrimSlots()` deactivates excess slots
  - Eliminates GC pressure from frequent UI updates
- **SlotUI Inner Class**: Caches all UI elements for a block entry
  - previewImage, previewRaw, count badge, teleporter label
  - clickHandler, outlines, shadows

#### Display Logic
- **Per-BlockType Rendering**:
  - Walk/Crumbler: Sprite-based with color fading for unavailable
  - Transporter: Dynamic route visualization texture (cached)
  - Teleporter: Flavor ID text overlay (A/B/C...)
  - Key/Lock: Sprite-based with availability states
- **Pair Inventory System**: Special handling for `isPairInventory=true`
  - Teleporters show pair count, not placement count
  - Pair credits track partial placements (1 of 2 required)
  - Display count stays steady until pair completes
- **Selection Outline**: UGUI Outline component highlights active entry
  - Thickness and color configurable in Inspector
  - Applied to both sprite and RawImage previews

#### Performance
- **UpdateLockStatus() Concern**: Calls `FindObjectsByType<LockBlock>` O(n) search
  - Mitigated: Only on lock state changes (event-driven, not per-frame)
  - Triggered by `LockBlock.OnLockStateChanged` event
  - Still expensive in complex levels with many locks
- **Transporter Route Cache**: Dictionary-based texture caching
  - Key: route steps + size + color hash
  - Pixel-by-pixel route visualization only built once per unique route
- **Sprite Loading**: Asset + Resources path fallback with caching
  - Checks existing sprite before reloading

#### Integration Points
- **BlockInventory**: `OnInventoryChanged` event triggers `_isDirty=true`
- **BuilderController**: `currentInventoryEntry` determines selection highlight
- **LockBlock**: `OnLockStateChanged` triggers `_lockDirty=true`

**Files**: [InventoryUI.cs](Assets/Scripts/InventoryUI.cs) (1,427 lines), [BlockInventory.cs](Assets/Scripts/BlockInventory.cs), [InventorySlotClickHandler.cs](Assets/Scripts/UI/InventorySlotClickHandler.cs)

---

### Input System (Mouse + Touch)

The game implements a **unified pointer abstraction** that seamlessly handles both mouse clicks and touch input without platform-specific code.

#### Layer 1: PointerInput Abstraction
```csharp
public static class PointerInput
{
    public static bool TryGetPrimaryPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        // Priority: Touch input (first finger)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)  // Only initial contact
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;       // Unique per finger
                return true;
            }
        }

        // Fallback: Mouse input (left button)
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            pointerId = -1;  // Constant for mouse
            return true;
        }

        screenPosition = Vector2.zero;
        pointerId = -1;
        return false;
    }

    public static bool IsPointerOverUI(Vector2 screenPosition, int pointerId)
    {
        // Uses UGUI EventSystem.RaycastAll() to detect UI hits
        // Prevents world interactions when clicking UI elements
    }
}
```

#### Layer 2: BuilderController Input Orchestration
**HandlePointerCursorMovement()** (lines 205-236):
1. **Get Primary Pointer**: Call `TryGetPrimaryPointerDown()` for unified input
2. **UI Check**: Call `IsPointerOverUI()` to block world interaction on UI clicks
3. **Screen-to-Grid Conversion**: `TryGetGridIndexFromScreen()` raycasts to grid plane
4. **Action Execution**: Either move cursor or place block based on target cell

**Screen-to-Grid Raycasting** (lines 447-479):
```csharp
Ray ray = cam.ScreenPointToRay(screenPosition);
Plane gridPlane = new Plane(Vector3.forward, Vector3.zero);  // XY plane at Z=0
if (gridPlane.Raycast(ray, out float enter))
{
    Vector3 worldPos = ray.GetPoint(enter);
    Vector3 localPos = worldPos - grid.gridOrigin;
    int x = Mathf.FloorToInt(localPos.x);
    int y = Mathf.FloorToInt(localPos.y);
    index = grid.CoordinatesToIndex(x, y);
}
```

#### Layer 3: UGUI Event System
**InventorySlotClickHandler**: Implements `IPointerClickHandler`
- UGUI automatically invokes `OnPointerClick()` for both mouse and touch
- `PointerEventData` contains button type, position, pointer ID
- No platform detection needed - UGUI handles translation

**EventSystem Auto-Initialization**:
- `StandaloneInputModule` component handles mouse/touch conversion
- Created automatically by InventoryUI if missing

#### Input Flow
```
USER INPUT (Click/Tap)
    ↓
PointerInput.TryGetPrimaryPointerDown()
  ├─ Touch: Input.touchCount > 0 → TouchPhase.Began
  └─ Mouse: Input.GetMouseButtonDown(0)
    ↓
IsPointerOverUI() → EventSystem.RaycastAll()
  ├─ If UI hit: Stop (UI handles it)
  └─ If world: Continue to grid raycasting
    ↓
Camera.ScreenPointToRay() → Plane.Raycast()
    ↓
Convert to grid coordinates (x, y)
    ↓
SetCursorIndex() or TryPlaceBlockAt()
```

**Design Choice**: Uses legacy Input Manager + UGUI EventSystem instead of new Input System package
- Pragmatic for 2D puzzle game without rebinding/controller needs
- Lightweight abstraction via PointerInput helper class

**Files**: [PointerInput.cs](Assets/Scripts/Input/PointerInput.cs), [BuilderController.cs](Assets/Scripts/BuilderController.cs), [InventorySlotClickHandler.cs](Assets/Scripts/UI/InventorySlotClickHandler.cs)

---

### Crumbler Block System

The **CrumblerBlock** is a sophisticated system balancing **visual chaos with gameplay determinism** through separation of debris physics and gameplay surfaces.

#### Four-Stage Lifecycle

**Stage 1: Entry Detection** (`OnPlayerEnter`)
- Plays entry particle system (`FallingStoneFragments`)
- Plays `entrySfx` audio ("smallRocksFalling2.mp3")
- Block remains solid and playable

**Stage 2: Center Trigger** (`OnPlayerReachCenter`)
- Detected when Lem reaches precise center point
- Plays center particle system (`FallingStoneFragments-2`)
- Plays `centerSfx` audio ("smallRocksFalling.mp3")
- **Initiates Center Jiggle Effect**:
  - Duration: 0.12 seconds (configurable)
  - Amplitude: 0.01 units (subtle tremor)
  - Frequency: 28 Hz (rapid vibration)
  - Per-brick sinusoidal motion with phase offsets

**Stage 3: Exit Trigger** (`OnPlayerExit`)
- Sets `collapseStarted = true`
- Initiates `CrumbleSequence()` coroutine

**Stage 4: Collapse & Debris** (`CrumbleSequence`)
- Optional delay (`fallDelaySeconds`, default 0s)
- Plays `collapseSfx` audio ("bigRocksFalling.mp3")
- Disables all colliders on main block
- **Spawns support cube immediately via raycast**
- Releases bricks as physics debris
- Destroys visual root

#### Debris System & Physics

**Debris Creation** (`ReleaseBricksAsDebris`):
- Finds all brick visual elements under `CrumbleBlocks/Bricks`
- Reparents to runtime root (`__CrumbleRuntime`)
- Applies physics via `EnsureDebrisPhysics()`

**Physics Configuration**:
```csharp
Rigidbody:
  - Mass: 0.2 (configurable)
  - Collision Detection: ContinuousSpeculative
  - useGravity: true
  - Initial impulse: Random up to 0.05 units
BoxCollider: Added if missing
Collision Filtering:
  - Ignores Lem colliders (prevents blocking player)
  - Ignores support cube collider (passes through)
```

**Impact Classification** (`DebrisImpactRelay`):
1. **Any Impact** (`HandleDebrisImpact`):
   - Fires once on first contact
   - Plays `impactSfx` ("bigRocksFallToGround.mp3")
   - Anchored to debris position

2. **Significant Impact** (`HandleSignificantDebrisImpact`):
   - Threshold: `relativeVelocity.magnitude >= impactMinSpeed`
   - Normal check: `contact.normal.y >= impactMinUpNormal`
   - Spawns dust puff particles at contact point

#### Support Cube Mechanic (Key Innovation)

**Problem**: Debris physics is chaotic and unpredictable, unsuitable for deterministic puzzle gameplay.

**Solution**: Invisible solid physics object maintains grid alignment while bricks tumble.

**Spawn Methods** (dual approach for robustness):

1. **Raycast Method** (`TrySpawnSupportCubeViaRaycast`):
   - Fires downward from block center
   - Finds first solid collider below
   - If hit is BaseBlock: uses `hitBlock.position + Vector3.up`
   - Fallback: converts hit point to grid coordinates
   - Runs immediately at collapse start

2. **Collision Method** (debris impact trigger):
   - Triggers on debris impact with valid downward contact
   - Only if raycast method didn't spawn cube
   - Validates upward normal

**Support Cube Properties**:
```csharp
Size: Configurable Vector3 (default 1x1x1)
Collider: BoxCollider (NOT a trigger - solid surface)
Position: Grid-aligned, one unit above supporting surface
Visualization: Optional semi-transparent red cube (alpha 0.35)
Marker: SupportCubeMarker component for cleanup tracking
Lifetime: Persists until block destruction
```

**Design Benefit**: Visual chaos (debris) + Gameplay certainty (support cube) = Impressive yet reliable puzzle mechanic.

#### Audio System

**Four-Stage Sound Design**:
| Stage | Clip | Purpose |
|-------|------|---------|
| Entry | smallRocksFalling2.mp3 | Warning - destabilizing |
| Center | smallRocksFalling.mp3 | Confirmation - rumble intensifies |
| Collapse | bigRocksFalling.mp3 | Critical - collapsing |
| Impact | bigRocksFallToGround.mp3 | Final - debris settles |

**PlayOneShotAtPosition()**: Creates temporary GameObject with AudioSource
- Auto-destroyed after clip duration + 0.1s fade
- Parented to `__CrumbleSfxRuntime` for centralized cleanup

#### Advanced Features

- **Jiggle Animation**: Per-element oscillation with phase offsets and smooth settle
- **Runtime Cleanup**: Static `CleanupRuntime()` method destroys all debris and support cubes
- **Debug Logging**: Controlled by `debugCrumbleLogs` inspector toggle
- **Template Method Pattern**: Overrides `OnPlayerEnter()`, `OnPlayerReachCenter()`, `OnPlayerExit()` from BaseBlock

**Files**: [CrumblerBlock.cs](Assets/Scripts/CrumblerBlock.cs) (1,000+ lines), [BaseBlock.cs](Assets/Scripts/BaseBlock.cs)

---

### Synchronized Music System

**GameModeManager** implements a **dual-track synchronized music system** where both Build and Play soundtracks play continuously with volume-only crossfading.

#### Architecture
```csharp
// Both tracks always playing, only volumes change
private AudioSource _musicA;  // Builder/Designer soundtrack
private AudioSource _musicB;  // Play mode soundtrack
private AudioSource _activeMusic;  // Reference to current full-volume track
```

#### Initialization (`UpdateMusicImmediate`)
1. Loads both clips: `SoundtrackBuild.mp3` and `SoundtrackPlay.mp3`
2. Starts both playing simultaneously at time 0
3. Sets initial volumes based on current mode:
   - Build/Designer mode: `_musicA.volume = musicVolume`, `_musicB.volume = 0`
   - Play mode: `_musicA.volume = 0`, `_musicB.volume = musicVolume`
4. Both tracks loop continuously (`loop = true`)

#### Mode Switching (`UpdateMusic`)
- Detects mode changes in `Update()` loop
- Triggers `CrossfadeVolumes()` coroutine
- **Never stops or restarts tracks** - only adjusts volumes

#### Crossfade (`CrossfadeVolumes`)
```csharp
while (elapsed < duration)
{
    float t = Mathf.Clamp01(elapsed / duration);
    from.volume = Mathf.Lerp(startFromVolume, 0f, t);  // Fade out
    to.volume = Mathf.Lerp(startToVolume, musicVolume, t);  // Fade in
    yield return null;
}
```

- Default duration: 0.35 seconds (configurable)
- Smooth interpolation using `Mathf.Lerp`
- Both tracks remain playing throughout
- No audio glitches or restarts

#### Benefits
- **Perfect Synchronization**: Tracks started together remain in sync
- **Seamless Transitions**: No jarring restarts when switching modes
- **Musical Continuity**: Maintains beat/measure alignment
- **Simple Implementation**: Volume crossfading is CPU-efficient

**Files**: [GameModeManager.cs](Assets/Scripts/GameModeManager.cs) (lines 21-270)

---

## Credits

Built with Unity 2022+
Uses Unity's built-in rendering and physics systems
