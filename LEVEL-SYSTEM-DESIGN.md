# Level System Design Document

## Overview

This document outlines the architecture needed to transform AWITP from a single-level prototype into a multi-level puzzle game with world-based progression (similar to Baba Is You).

## Current State

**What Exists:**
- Single level save/load system (JSON files)
- Three game modes: Build Mode, Designer Mode (dev-only), Play Mode
- Block/Lem instantiation via code (factory pattern in GridManager)
- Inventory configuration per level (LevelBlockInventoryConfig)

**What's Missing:**
- Multiple level management
- Level selection UI
- World-based progression system
- Prefab-based architecture for blocks and Lems
- Level unlocking mechanics
- Progress persistence

---

## Goals

### 1. World-Based Progression
- Organize levels into "Worlds" (like Baba Is You)
- Each world contains 3-5 levels
- Must complete ALL levels in a world to unlock the next world
- Clear visual indication of locked vs unlocked worlds

### 2. Prefab-Based Architecture
- Blocks as prefabs in `Resources/Blocks/`
- Lems as prefabs in `Resources/Characters/`
- Level data references prefabs by name
- Behavior changes propagate to all levels automatically
- Self-contained block validation

### 3. Extensibility
- Easy to add new levels
- Easy to add new worlds
- Easy to modify block behavior globally
- Easy to test individual levels

---

## Architecture Design

### Level Data Structure

#### Current: LevelData.cs
```csharp
[Serializable]
public class LevelData {
    public int gridWidth;
    public int gridHeight;
    public float cellSize;
    public BlockData[] blocks;          // Permanent blocks
    public int[] placeableSpaceIndices;
    public LemData[] lems;
    public InventoryEntryData[] inventoryEntries;
}
```

#### Proposed: Enhanced LevelData
```csharp
[Serializable]
public class LevelData {
    // Metadata
    public string levelId;              // Unique ID: "world1_level1"
    public string levelName;            // Display name: "First Steps"
    public string worldId;              // Parent world: "world1"
    public int orderInWorld;            // 0-based index in world

    // Grid configuration
    public int gridWidth;
    public int gridHeight;
    public float cellSize;

    // Level content
    public BlockData[] permanentBlocks; // Fixed architecture
    public int[] placeableSpaceIndices; // Where players can place
    public LemData[] startingLems;      // Initial Lem positions
    public InventoryEntryData[] inventoryEntries;

    // Win condition (implicit: fill all locks)
    public int requiredKeyCount;        // Number of key-lock pairs
}
```

---

### World Data Structure

#### New: WorldData.cs
```csharp
[Serializable]
public class WorldData {
    public string worldId;              // Unique ID: "world1"
    public string worldName;            // Display name: "Tutorial Island"
    public string description;          // "Learn the basics"
    public int orderInGame;             // 0-based progression order
    public string[] levelIds;           // Levels in this world
    public bool isUnlocked;             // Unlocked by completing previous world
}
```

---

### Progress Data Structure

#### New: GameProgressData.cs
```csharp
[Serializable]
public class GameProgressData {
    public string[] completedLevelIds;  // All solved levels
    public string[] unlockedWorldIds;   // Unlocked worlds
    public string currentLevelId;       // Last played level
    public Dictionary<string, LevelProgress> levelProgress;
}

[Serializable]
public class LevelProgress {
    public string levelId;
    public bool completed;
    public int attempts;
    public float bestTime;              // Future: speedrun tracking
}
```

---

## File Structure

### Proposed Directory Layout

```
Assets/
├── Resources/
│   ├── Blocks/                      # Block prefabs
│   │   ├── Block_Default.prefab
│   │   ├── Block_Crumbler.prefab
│   │   ├── Block_Transporter.prefab
│   │   ├── Block_Teleporter.prefab
│   │   ├── Block_Key.prefab
│   │   └── Block_Lock.prefab
│   ├── Characters/                  # Character prefabs
│   │   └── Lem.prefab
│   └── Levels/                      # Level data
│       ├── Worlds/
│       │   ├── World_Tutorial.asset
│       │   ├── World_Basics.asset
│       │   └── World_Advanced.asset
│       └── LevelDefinitions/
│           ├── Tutorial/
│           │   ├── Level_FirstSteps.asset
│           │   ├── Level_Crumblers.asset
│           │   └── Level_Teleporters.asset
│           ├── Basics/
│           │   ├── Level_ResourceManagement.asset
│           │   └── Level_Timing.asset
│           └── Advanced/
│               └── (future levels)
│
├── Scripts/
│   ├── Core/
│   │   ├── GridManager.cs
│   │   ├── GameMode.cs
│   │   └── (existing core files)
│   ├── Blocks/
│   │   ├── BaseBlock.cs             # Abstract base
│   │   ├── DefaultBlock.cs          # Concrete implementations
│   │   ├── CrumblerBlock.cs
│   │   ├── TransporterBlock.cs
│   │   ├── TeleporterBlock.cs
│   │   ├── KeyBlock.cs
│   │   ├── LockBlock.cs
│   │   ├── BlockType.cs
│   │   ├── BlockColors.cs
│   │   └── BlockInventory.cs
│   ├── LevelSystem/                 # NEW
│   │   ├── LevelData.cs             # Enhanced level data
│   │   ├── WorldData.cs             # World configuration
│   │   ├── GameProgressData.cs      # Player progress
│   │   ├── LevelManager.cs          # Level loading/switching
│   │   ├── WorldManager.cs          # World progression logic
│   │   ├── ProgressManager.cs       # Save/load progress
│   │   └── LevelDefinition.cs       # ScriptableObject for levels
│   ├── UI/
│   │   ├── WorldMapUI.cs            # NEW: World selection screen
│   │   ├── LevelSelectUI.cs         # NEW: Level selection within world
│   │   ├── InventoryUI.cs
│   │   ├── ControlsUI.cs
│   │   └── WinConditionUI.cs        # NEW: Victory screen
│   └── (other existing scripts)
│
└── Scenes/
    ├── MainMenu.unity               # NEW: Entry point
    ├── WorldMap.unity               # NEW: World selection
    ├── Game.unity                   # Gameplay scene (replaces Master.unity)
    └── Designer.unity               # NEW: Designer-only scene
```

---

## Core Systems

### 1. LevelManager (NEW)

**Responsibility**: Load, instantiate, and manage current level

**Key Methods**:
```csharp
public class LevelManager : MonoBehaviour {
    public static LevelManager Instance { get; private set; }

    public LevelDefinition CurrentLevel { get; private set; }

    // Load a level from its definition
    public void LoadLevel(string levelId);

    // Instantiate level from LevelData
    private void InstantiateLevel(LevelData data);

    // Clear current level
    public void UnloadLevel();

    // Check win condition
    public bool IsLevelComplete();

    // Handle level completion
    public void OnLevelComplete();

    // Restart current level
    public void RestartLevel();
}
```

**Integration Points**:
- Works with GridManager to place blocks
- Loads prefabs from Resources/
- Notifies ProgressManager on completion
- Triggers UI updates

---

### 2. WorldManager (NEW)

**Responsibility**: Manage world progression and unlocking

**Key Methods**:
```csharp
public class WorldManager : MonoBehaviour {
    public static WorldManager Instance { get; private set; }

    public WorldData[] AllWorlds { get; private set; }

    // Get world by ID
    public WorldData GetWorld(string worldId);

    // Check if world is unlocked
    public bool IsWorldUnlocked(string worldId);

    // Unlock next world (called when all levels complete)
    public void UnlockNextWorld(string currentWorldId);

    // Get levels in world
    public LevelDefinition[] GetLevelsInWorld(string worldId);

    // Check if all levels in world are complete
    public bool IsWorldComplete(string worldId);
}
```

---

### 3. ProgressManager (NEW)

**Responsibility**: Track and persist player progress

**Key Methods**:
```csharp
public class ProgressManager : MonoBehaviour {
    public static ProgressManager Instance { get; private set; }

    private GameProgressData progress;

    // Mark level as complete
    public void MarkLevelComplete(string levelId);

    // Check if level is complete
    public bool IsLevelComplete(string levelId);

    // Get progress for level
    public LevelProgress GetLevelProgress(string levelId);

    // Save progress to disk
    public void SaveProgress();

    // Load progress from disk
    public void LoadProgress();

    // Reset all progress (for testing)
    public void ResetProgress();
}
```

**Storage**: JSON file in `Application.persistentDataPath/progress.json`

---

### 4. LevelDefinition (NEW - ScriptableObject)

**Responsibility**: Designer-friendly level definition

```csharp
[CreateAssetMenu(fileName = "Level", menuName = "AWITP/Level Definition")]
public class LevelDefinition : ScriptableObject {
    [Header("Metadata")]
    public string levelId;
    public string levelName;
    public string worldId;
    public int orderInWorld;

    [Header("Grid")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f;

    [Header("Level Content")]
    public LevelData levelData;

    // Optionally reference external JSON file
    public TextAsset levelDataJson;

    // Convert to runtime LevelData
    public LevelData ToLevelData();
}
```

**Why ScriptableObjects?**
- Visible in Unity Editor (easy to create/edit)
- Can reference prefabs directly
- Supports Inspector validation
- Easy to organize in Project window

---

## Prefab Architecture

### Block Prefabs

**Structure**:
```
Block_Default.prefab
├── GameObject "Block"
│   ├── SpriteRenderer (or MeshRenderer)
│   ├── BoxCollider
│   ├── DefaultBlock (script component)
│   └── CenterTrigger (child GameObject)
│       └── SphereCollider (Is Trigger)
```

**BaseBlock.cs Updates**:
```csharp
public abstract class BaseBlock : MonoBehaviour {
    // Self-contained validation
    public virtual bool CanBePlacedAt(int gridIndex, GridManager grid) {
        return true; // Override in subclasses
    }

    public virtual int[] GetBlockedIndices() {
        return new int[0]; // Override for transporters
    }

    public virtual bool ValidateGroupPlacement(GridManager grid) {
        return true; // Override for teleporters
    }

    public virtual string GetPlacementErrorMessage(int gridIndex, GridManager grid) {
        return "Cannot place block here";
    }

    // Lifecycle hooks (already exist)
    protected virtual void OnPlayerEnter(LemController lem) { }
    protected virtual void OnPlayerReachCenter(LemController lem) { }
    protected virtual void OnPlayerExit(LemController lem) { }
}
```

**Factory Pattern Replacement**:

**Old (GridManager)**:
```csharp
// Hardcoded instantiation
GameObject blockObj = new GameObject($"Block_{blockType}");
BaseBlock block = blockObj.AddComponent<DefaultBlock>();
```

**New (LevelManager)**:
```csharp
// Prefab-based instantiation
GameObject prefab = Resources.Load<GameObject>($"Blocks/Block_{blockType}");
GameObject blockObj = Instantiate(prefab, position, Quaternion.identity);
BaseBlock block = blockObj.GetComponent<BaseBlock>();
```

---

### Lem Prefab

**Structure**:
```
Lem.prefab
├── GameObject "Lem"
│   ├── SpriteRenderer (with Animator)
│   ├── CapsuleCollider
│   ├── Rigidbody (with PhysicMaterial)
│   └── LemController (script component)
```

**Benefits**:
- Edit Lem appearance in one place
- Adjust physics properties globally
- Add animations without code changes
- Easy to create Lem variants (future)

---

## UI Flow

### Game Flow Diagram

```
[Main Menu]
    ↓
[World Map] ← Shows all worlds, locked/unlocked
    ↓ (Click world)
[Level Select] ← Shows 3-5 levels in world
    ↓ (Click level)
[Game Scene]
    ├─ Build Mode ← Place blocks
    │   ↓ (Press P)
    ├─ Play Mode ← Test solution
    │   ↓ (Complete level)
    ├─ [Victory Screen]
    │   ├─ Next Level
    │   ├─ Retry
    │   └─ World Map
    ↓
[Back to Level Select or World Map]
```

### Designer Flow (Dev-Only)

```
[Unity Editor]
    ↓
[Designer Scene]
    ├─ Build Mode ← Test player experience
    ├─ Designer Mode (E key) ← Create level
    │   ├─ Place permanent blocks
    │   ├─ Mark placeable spaces
    │   ├─ Configure inventory
    │   └─ Place Lems
    ├─ Play Mode (P key) ← Test level
    │   ↓
    ├─ [Save Level] (Ctrl+S) → Create LevelDefinition asset
    └─ [Export to Build] → Level becomes playable in game
```

---

## Implementation Phases

### Phase 1: Prefab Migration (Week 1)
**Goal**: Convert current code-based blocks to prefabs

**Tasks**:
1. Create block prefabs for all 6 types
2. Create Lem prefab
3. Update GridManager/LevelManager to use `Resources.Load()`
4. Test that existing level still works
5. Verify behavior changes propagate

**Deliverables**:
- [ ] `Resources/Blocks/` with 6 prefabs
- [ ] `Resources/Characters/Lem.prefab`
- [ ] Updated instantiation code
- [ ] Existing level loads correctly

---

### Phase 2: Level System Foundation (Week 2)
**Goal**: Create level management infrastructure

**Tasks**:
1. Create `LevelDefinition` ScriptableObject
2. Create `LevelManager` singleton
3. Create `WorldData` ScriptableObject
4. Create `WorldManager` singleton
5. Create `ProgressManager` singleton
6. Implement save/load for progress

**Deliverables**:
- [ ] `LevelSystem/` scripts folder
- [ ] Basic level loading works
- [ ] Progress saves to JSON

---

### Phase 3: Multiple Levels (Week 3)
**Goal**: Create first world with 3-5 levels

**Tasks**:
1. Design 5 tutorial levels
2. Create LevelDefinition assets for each
3. Group into "Tutorial World"
4. Implement level completion detection
5. Implement world unlock logic
6. Test progression

**Deliverables**:
- [ ] 5 playable levels
- [ ] World progression works
- [ ] Levels unlock correctly

---

### Phase 4: UI Implementation (Week 4)
**Goal**: Create world map and level selection screens

**Tasks**:
1. Create MainMenu scene
2. Create WorldMap scene with UI
3. Create LevelSelect UI
4. Create victory screen
5. Implement navigation between screens
6. Add visual feedback for locked/completed states

**Deliverables**:
- [ ] Full UI flow works
- [ ] Players can select worlds/levels
- [ ] Victory screen triggers correctly

---

### Phase 5: Polish & Testing (Week 5)
**Goal**: Refinement and bug fixes

**Tasks**:
1. Add transitions between scenes
2. Add sound effects
3. Add particle effects for victory
4. Playtest all levels
5. Balance difficulty
6. Fix bugs

**Deliverables**:
- [ ] Polished experience
- [ ] All levels tested
- [ ] No blocking bugs

---

## Technical Considerations

### Prefab Loading Performance
- Use `Resources.Load()` once per level load
- Cache loaded prefabs in LevelManager
- Instantiate from cache for repeated objects
- Unload unused prefabs when changing levels

### Progress Data Integrity
- Save progress immediately on level completion
- Implement backup system (progress.json + progress_backup.json)
- Handle corrupted saves gracefully
- Provide reset option in settings

### Designer Workflow
- Designer Mode remains accessible in build (with #if UNITY_EDITOR checks removed)
- Ctrl+S creates LevelDefinition asset in Editor
- Runtime saves to JSON for testing
- Export tool converts JSON → LevelDefinition asset

### Scene Management
- Use `SceneManager.LoadScene()` for transitions
- Implement loading screen for large levels
- Preserve progress across scene loads
- Handle scene load failures

---

## Migration Strategy

### Approach: Incremental Migration

**Don't**: Delete everything and start over
**Do**: Migrate piece by piece while keeping game playable

### Step-by-Step Migration:

1. **Week 1**: Create prefabs alongside existing code
2. **Week 2**: Add new systems without removing old
3. **Week 3**: Switch to new systems, keep old as fallback
4. **Week 4**: Remove old code after verification
5. **Week 5**: Clean up and document

**Benefits**:
- Always have working version
- Easy to rollback if issues
- Can test new features incrementally

---

## Testing Strategy

### Unit Tests (Optional)
- Test block placement validation
- Test world unlock logic
- Test progress save/load
- Test level completion detection

### Integration Tests
- Load each level and verify it instantiates
- Complete level and verify progress saves
- Unlock world and verify next world accessible
- Restart level and verify state resets

### Playtesting
- Each level should be solvable
- Difficulty progression should feel smooth
- Controls should be intuitive
- Win condition should be clear

---

## Future Enhancements

### Post-Phase 5 Features:
- **Level Editor In-Game**: Allow players to create levels
- **Level Sharing**: Export/import custom levels
- **Time Trials**: Speedrun mode with leaderboards
- **Star Ratings**: 1-3 stars based on block efficiency
- **Hints System**: Optional hints for stuck players
- **Achievements**: Complete X levels, use Y blocks, etc.
- **Custom Worlds**: Player-created world packs
- **Mobile Support**: Touch controls, responsive UI

---

## Questions & Decisions Needed

### Open Questions for Taylor:

1. **Level Count**: How many levels per world? (Recommended: 3-5)
2. **World Count**: How many worlds total? (Recommended: Start with 3, expand later)
3. **Difficulty Curve**: How quickly should difficulty increase?
4. **Designer Mode in Build**: Keep E key accessible, or Editor-only?
5. **Progress Reset**: Should players be able to reset progress?
6. **Level Names**: Descriptive ("Bridge the Gap") or numbered ("Level 1-1")?
7. **Tutorial**: Integrated tutorial or separate mode?
8. **Win Animation**: How elaborate should victory feedback be?

### Design Decisions:

**Recommended Answers** (Taylor can override):
1. 4 levels per world (manageable scope)
2. 3 worlds initially (Tutorial, Basics, Advanced)
3. Gradual curve: easy → medium → hard within each world
4. Designer Mode accessible in build (helpful for debugging)
5. Yes, reset in settings menu
6. Descriptive names with numbers ("1-1: First Steps")
7. Integrated tutorial (first world teaches mechanics)
8. Simple victory screen with confetti particles

---

## Success Criteria

**Phase 1 Success**:
- ✓ All blocks are prefabs
- ✓ Lem is a prefab
- ✓ Existing level still works
- ✓ No regressions in behavior

**Phase 2 Success**:
- ✓ Can load levels from LevelDefinition assets
- ✓ Progress saves and loads correctly
- ✓ Multiple levels can exist simultaneously

**Phase 3 Success**:
- ✓ 5 levels playable
- ✓ Completing levels unlocks progression
- ✓ World system works as designed

**Phase 4 Success**:
- ✓ Players can navigate UI
- ✓ Locked/unlocked states are clear
- ✓ Victory screen feels rewarding

**Phase 5 Success**:
- ✓ Game feels polished
- ✓ No game-breaking bugs
- ✓ Ready for expanded content

---

## Conclusion

This design provides a clear path from single-level prototype to full multi-level puzzle game. The prefab-based architecture ensures maintainability and extensibility, while the world-based progression creates a satisfying player experience.

**Next Steps**:
1. Review this document and approve/modify design decisions
2. Begin Phase 1 (Prefab Migration)
3. Create first tutorial level
4. Iterate based on playtesting

**Estimated Total Time**: 5 weeks for full implementation
**Minimal Viable Product**: End of Phase 3 (3 weeks)

---

**Document Status**: Draft v1.0
**Author**: AI Assistant
**Date**: 2026-01-26
**Target**: Taylor (Designer/Developer)
