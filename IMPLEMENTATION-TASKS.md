# Implementation Tasks for Level System

This document provides concrete, actionable tasks for implementing the level system described in [LEVEL-SYSTEM-DESIGN.md](LEVEL-SYSTEM-DESIGN.md).

---

## Phase 1: Prefab Migration (Start Here!)

### Task 1.1: Create Default Block Prefab
**Priority**: Critical
**Estimated Time**: 30 minutes

**Steps**:
1. In Unity, create empty GameObject named "Block_Default"
2. Add SpriteRenderer (or keep existing visual setup)
3. Add BoxCollider with proper size
4. Add `DefaultBlock` script component
5. Create child GameObject "CenterTrigger"
   - Add SphereCollider, set Is Trigger = true
   - Add `CenterTrigger` script component
6. Save as prefab: `Assets/Resources/Blocks/Block_Default.prefab`
7. Test: Load prefab in code and verify it works

**Code Change**:
```csharp
// OLD (in GridManager or similar):
GameObject blockObj = new GameObject("Block");
blockObj.AddComponent<BoxCollider>();
DefaultBlock block = blockObj.AddComponent<DefaultBlock>();

// NEW:
GameObject prefab = Resources.Load<GameObject>("Blocks/Block_Default");
GameObject blockObj = Instantiate(prefab, position, Quaternion.identity);
DefaultBlock block = blockObj.GetComponent<DefaultBlock>();
```

**Verification**:
- [ ] Prefab exists in Resources/Blocks/
- [ ] Can load prefab with Resources.Load()
- [ ] Instantiated block behaves identically to old version
- [ ] CenterTrigger detects Lem correctly

---

### Task 1.2: Create Remaining Block Prefabs
**Priority**: Critical
**Estimated Time**: 2 hours

**Steps** (repeat for each block type):
1. Crumbler: `Block_Crumbler.prefab` with `CrumblerBlock` script
2. Transporter: `Block_Transporter.prefab` with `TransporterBlock` script
3. Teleporter: `Block_Teleporter.prefab` with `TeleporterBlock` script
4. Key: `Block_Key.prefab` with `KeyBlock` script
5. Lock: `Block_Lock.prefab` with `LockBlock` script

**For Each Prefab**:
- Use existing prefabs (Block_Key, Block_Lock) as reference if they exist
- Ensure proper colliders
- Ensure CenterTrigger child
- Assign correct colors via BlockColors
- Test basic behavior

**Verification**:
- [ ] All 6 block prefabs exist
- [ ] Each has correct script component
- [ ] Each has CenterTrigger child
- [ ] Colors match BlockColors definitions

---

### Task 1.3: Create Lem Prefab
**Priority**: Critical
**Estimated Time**: 30 minutes

**Steps**:
1. Find existing Lem GameObject in scene
2. Add SpriteRenderer (or existing visual)
3. Add CapsuleCollider
4. Add Rigidbody with:
   - Mass: 1
   - Gravity: Use Gravity = true
   - Constraints: Freeze Rotation Z, Freeze Position Z
5. Assign PhysicMaterial (zero friction)
6. Add `LemController` script
7. Add Animator (if using animations)
8. Save as prefab: `Assets/Resources/Characters/Lem.prefab`

**Code Change**:
```csharp
// OLD (in LemSpawner):
GameObject lemObj = new GameObject("Lem");
lemObj.AddComponent<CapsuleCollider>();
lemObj.AddComponent<Rigidbody>();
LemController lem = lemObj.AddComponent<LemController>();

// NEW:
GameObject prefab = Resources.Load<GameObject>("Characters/Lem");
GameObject lemObj = Instantiate(prefab, position, Quaternion.identity);
LemController lem = lemObj.GetComponent<LemController>();
```

**Verification**:
- [ ] Prefab exists in Resources/Characters/
- [ ] Lem walks correctly
- [ ] Physics behaves identically
- [ ] Animations work (if applicable)

---

### Task 1.4: Update GridManager to Use Prefabs
**Priority**: Critical
**Estimated Time**: 1 hour

**File**: `Assets/Scripts/GridManager.cs`

**Changes Needed**:
1. Remove factory methods that instantiate blocks via `AddComponent<>()`
2. Add prefab loading method:

```csharp
private GameObject GetBlockPrefab(BlockType type) {
    string prefabName = $"Blocks/Block_{type}";
    GameObject prefab = Resources.Load<GameObject>(prefabName);

    if (prefab == null) {
        Debug.LogError($"Block prefab not found: {prefabName}");
        return null;
    }

    return prefab;
}

public BaseBlock PlaceBlock(int gridIndex, BlockType type, string flavorId = "", string[] routeSteps = null) {
    GameObject prefab = GetBlockPrefab(type);
    if (prefab == null) return null;

    Vector3 pos = GetWorldPosition(gridIndex);
    GameObject blockObj = Instantiate(prefab, pos, Quaternion.identity, transform);
    BaseBlock block = blockObj.GetComponent<BaseBlock>();

    // Configure block (flavor, routes, etc.)
    if (block is TeleporterBlock teleporter && !string.IsNullOrEmpty(flavorId)) {
        teleporter.flavorId = flavorId;
    }
    // ... other configurations

    return block;
}
```

3. Update all calls to block placement to use new method
4. Remove old factory code

**Verification**:
- [ ] All blocks place correctly via prefabs
- [ ] No compilation errors
- [ ] Existing level loads and works
- [ ] No null reference exceptions

---

### Task 1.5: Update LemSpawner to Use Prefab
**Priority**: Critical
**Estimated Time**: 30 minutes

**File**: `Assets/Scripts/LemSpawner.cs`

**Changes Needed**:
1. Add prefab loading:

```csharp
private GameObject lemPrefab;

void Awake() {
    lemPrefab = Resources.Load<GameObject>("Characters/Lem");
    if (lemPrefab == null) {
        Debug.LogError("Lem prefab not found in Resources/Characters/");
    }
}

public LemController SpawnLem(Vector3 position, bool facingRight = true) {
    if (lemPrefab == null) return null;

    GameObject lemObj = Instantiate(lemPrefab, position, Quaternion.identity);
    LemController lem = lemObj.GetComponent<LemController>();

    if (lem != null) {
        lem.facingRight = facingRight;
    }

    return lem;
}
```

2. Remove old instantiation code
3. Update all spawn calls

**Verification**:
- [ ] Lems spawn from prefab
- [ ] Facing direction works
- [ ] No performance regression
- [ ] Animations still work

---

### Task 1.6: Test Prefab Changes
**Priority**: Critical
**Estimated Time**: 1 hour

**Testing Checklist**:
- [ ] Load existing saved level
- [ ] Place all 6 block types
- [ ] Spawn Lem
- [ ] Test Build Mode placement
- [ ] Test Designer Mode placement
- [ ] Test Play Mode behavior
- [ ] Test crumbler breaking
- [ ] Test transporter movement
- [ ] Test teleporter pairs
- [ ] Test key pickup
- [ ] Test lock filling
- [ ] Verify no memory leaks (profile in Unity)

**If Any Test Fails**: Roll back and debug before proceeding

---

## Phase 2: Level System Foundation

### Task 2.1: Create LevelDefinition ScriptableObject
**Priority**: High
**Estimated Time**: 1 hour

**File**: Create `Assets/Scripts/LevelSystem/LevelDefinition.cs`

**Code**:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "AWITP/Level Definition")]
public class LevelDefinition : ScriptableObject {
    [Header("Metadata")]
    public string levelId;
    public string levelName;
    public string worldId;
    public int orderInWorld;

    [Header("Grid Configuration")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f;

    [Header("Level Data")]
    [TextArea(3, 10)]
    public string levelDataJson; // JSON string of LevelData

    // Convert JSON to LevelData at runtime
    public LevelData ToLevelData() {
        if (string.IsNullOrEmpty(levelDataJson)) {
            Debug.LogError($"Level {levelId} has no data!");
            return null;
        }

        try {
            return JsonUtility.FromJson<LevelData>(levelDataJson);
        } catch (System.Exception e) {
            Debug.LogError($"Failed to parse level data for {levelId}: {e.Message}");
            return null;
        }
    }
}
```

**Testing**:
1. Right-click in Project window
2. Create → AWITP → Level Definition
3. Fill in test data
4. Verify it shows in Inspector

**Verification**:
- [ ] Can create LevelDefinition asset
- [ ] Inspector shows all fields
- [ ] ToLevelData() parses JSON correctly

---

### Task 2.2: Create WorldData ScriptableObject
**Priority**: High
**Estimated Time**: 30 minutes

**File**: Create `Assets/Scripts/LevelSystem/WorldData.cs`

**Code**:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "World", menuName = "AWITP/World Definition")]
public class WorldData : ScriptableObject {
    [Header("Metadata")]
    public string worldId;
    public string worldName;
    [TextArea(2, 5)]
    public string description;
    public int orderInGame;

    [Header("Levels")]
    public LevelDefinition[] levels; // Drag LevelDefinition assets here

    // Helper: Get level IDs
    public string[] GetLevelIds() {
        string[] ids = new string[levels.Length];
        for (int i = 0; i < levels.Length; i++) {
            ids[i] = levels[i].levelId;
        }
        return ids;
    }
}
```

**Testing**:
1. Create → AWITP → World Definition
2. Fill in test data
3. Drag LevelDefinition assets into levels array

**Verification**:
- [ ] Can create WorldData asset
- [ ] Can reference LevelDefinition assets
- [ ] GetLevelIds() returns correct IDs

---

### Task 2.3: Create GameProgressData
**Priority**: High
**Estimated Time**: 30 minutes

**File**: Create `Assets/Scripts/LevelSystem/GameProgressData.cs`

**Code**:
```csharp
using System;
using System.Collections.Generic;

[Serializable]
public class GameProgressData {
    public List<string> completedLevelIds = new List<string>();
    public List<string> unlockedWorldIds = new List<string>();
    public string currentLevelId;
    public Dictionary<string, LevelProgress> levelProgress = new Dictionary<string, LevelProgress>();
}

[Serializable]
public class LevelProgress {
    public string levelId;
    public bool completed;
    public int attempts;
    public float bestTime;
}
```

**Note**: Unity's JsonUtility doesn't support Dictionary. Use a wrapper or alternative serialization (like Newtonsoft.Json).

**Alternative** (Unity-compatible):
```csharp
[Serializable]
public class GameProgressData {
    public List<string> completedLevelIds = new List<string>();
    public List<string> unlockedWorldIds = new List<string>();
    public string currentLevelId;
    public List<LevelProgress> levelProgressList = new List<LevelProgress>();

    // Helper to get progress by level ID
    public LevelProgress GetProgress(string levelId) {
        return levelProgressList.Find(p => p.levelId == levelId);
    }
}
```

**Verification**:
- [ ] GameProgressData serializes to JSON
- [ ] Can deserialize back correctly

---

### Task 2.4: Create LevelManager Singleton
**Priority**: Critical
**Estimated Time**: 2 hours

**File**: Create `Assets/Scripts/LevelSystem/LevelManager.cs`

**Code Structure**:
```csharp
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public static LevelManager Instance { get; private set; }

    [Header("Current Level")]
    public LevelDefinition currentLevelDef;
    private LevelData currentLevelData;

    [Header("References")]
    public GridManager gridManager;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadLevel(string levelId) {
        // Find LevelDefinition by ID (load from Resources or reference)
        LevelDefinition levelDef = Resources.Load<LevelDefinition>($"Levels/LevelDefinitions/{levelId}");
        if (levelDef == null) {
            Debug.LogError($"Level not found: {levelId}");
            return;
        }

        LoadLevel(levelDef);
    }

    public void LoadLevel(LevelDefinition levelDef) {
        currentLevelDef = levelDef;
        currentLevelData = levelDef.ToLevelData();

        if (currentLevelData == null) {
            Debug.LogError($"Failed to load level data for {levelDef.levelId}");
            return;
        }

        InstantiateLevel(currentLevelData);
    }

    private void InstantiateLevel(LevelData data) {
        // Clear current level
        UnloadLevel();

        // Configure grid
        gridManager.SetGridSize(data.gridWidth, data.gridHeight, data.cellSize);

        // Place permanent blocks
        foreach (var blockData in data.blocks) {
            gridManager.PlaceBlock(blockData.gridIndex, blockData.blockType, blockData.flavorId, blockData.routeSteps);
        }

        // Mark placeable spaces
        gridManager.SetPlaceableSpaces(data.placeableSpaceIndices);

        // Spawn Lems
        foreach (var lemData in data.lems) {
            Vector3 pos = gridManager.GetWorldPosition(lemData.gridIndex);
            LemSpawner.Instance.SpawnLem(pos, lemData.facingRight);
        }

        // Configure inventory
        BlockInventory.Instance.LoadInventoryEntries(data.inventoryEntries);

        Debug.Log($"Level loaded: {currentLevelDef.levelName}");
    }

    public void UnloadLevel() {
        // Clear grid
        gridManager.ClearAllBlocks();
        gridManager.ClearPlaceableSpaces();

        // Destroy Lems
        LemController[] lems = FindObjectsOfType<LemController>();
        foreach (var lem in lems) {
            Destroy(lem.gameObject);
        }

        currentLevelData = null;
        currentLevelDef = null;
    }

    public bool IsLevelComplete() {
        // Check if all locks are filled
        LockBlock[] locks = FindObjectsOfType<LockBlock>();
        foreach (var lock in locks) {
            if (!lock.IsFilled()) {
                return false;
            }
        }
        return true;
    }

    public void OnLevelComplete() {
        Debug.Log("Level Complete!");
        ProgressManager.Instance.MarkLevelComplete(currentLevelDef.levelId);
        // TODO: Show victory screen
    }

    public void RestartLevel() {
        if (currentLevelDef != null) {
            LoadLevel(currentLevelDef);
        }
    }
}
```

**Verification**:
- [ ] LevelManager loads level from LevelDefinition
- [ ] Grid populates correctly
- [ ] Blocks place correctly
- [ ] Lems spawn correctly
- [ ] Inventory configures correctly
- [ ] UnloadLevel clears everything

---

### Task 2.5: Create WorldManager Singleton
**Priority**: High
**Estimated Time**: 1 hour

**File**: Create `Assets/Scripts/LevelSystem/WorldManager.cs`

**Code**:
```csharp
using UnityEngine;
using System.Linq;

public class WorldManager : MonoBehaviour {
    public static WorldManager Instance { get; private set; }

    [Header("World Configuration")]
    public WorldData[] allWorlds; // Assign in Inspector

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Sort worlds by order
        allWorlds = allWorlds.OrderBy(w => w.orderInGame).ToArray();
    }

    public WorldData GetWorld(string worldId) {
        return System.Array.Find(allWorlds, w => w.worldId == worldId);
    }

    public bool IsWorldUnlocked(string worldId) {
        // First world always unlocked
        if (allWorlds.Length > 0 && allWorlds[0].worldId == worldId) {
            return true;
        }

        return ProgressManager.Instance.IsWorldUnlocked(worldId);
    }

    public void UnlockNextWorld(string currentWorldId) {
        WorldData currentWorld = GetWorld(currentWorldId);
        if (currentWorld == null) return;

        // Find next world
        int nextIndex = currentWorld.orderInGame + 1;
        if (nextIndex < allWorlds.Length) {
            WorldData nextWorld = allWorlds[nextIndex];
            ProgressManager.Instance.UnlockWorld(nextWorld.worldId);
            Debug.Log($"World unlocked: {nextWorld.worldName}");
        }
    }

    public LevelDefinition[] GetLevelsInWorld(string worldId) {
        WorldData world = GetWorld(worldId);
        return world?.levels ?? new LevelDefinition[0];
    }

    public bool IsWorldComplete(string worldId) {
        WorldData world = GetWorld(worldId);
        if (world == null) return false;

        foreach (var level in world.levels) {
            if (!ProgressManager.Instance.IsLevelComplete(level.levelId)) {
                return false;
            }
        }
        return true;
    }
}
```

**Verification**:
- [ ] WorldManager loads worlds
- [ ] IsWorldUnlocked checks first world correctly
- [ ] GetLevelsInWorld returns correct levels
- [ ] IsWorldComplete checks all levels

---

### Task 2.6: Create ProgressManager Singleton
**Priority**: Critical
**Estimated Time**: 1.5 hours

**File**: Create `Assets/Scripts/LevelSystem/ProgressManager.cs`

**Code**:
```csharp
using UnityEngine;
using System.IO;

public class ProgressManager : MonoBehaviour {
    public static ProgressManager Instance { get; private set; }

    private GameProgressData progress;
    private string saveFilePath;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, "progress.json");
        LoadProgress();
    }

    public void MarkLevelComplete(string levelId) {
        if (!progress.completedLevelIds.Contains(levelId)) {
            progress.completedLevelIds.Add(levelId);
        }

        // Update level progress
        LevelProgress levelProg = progress.GetProgress(levelId);
        if (levelProg == null) {
            levelProg = new LevelProgress { levelId = levelId };
            progress.levelProgressList.Add(levelProg);
        }
        levelProg.completed = true;
        levelProg.attempts++;

        SaveProgress();

        // Check if world is now complete
        CheckWorldCompletion(levelId);
    }

    private void CheckWorldCompletion(string levelId) {
        // Find which world this level belongs to
        foreach (var world in WorldManager.Instance.allWorlds) {
            if (System.Array.Exists(world.GetLevelIds(), id => id == levelId)) {
                if (WorldManager.Instance.IsWorldComplete(world.worldId)) {
                    WorldManager.Instance.UnlockNextWorld(world.worldId);
                }
                break;
            }
        }
    }

    public bool IsLevelComplete(string levelId) {
        return progress.completedLevelIds.Contains(levelId);
    }

    public LevelProgress GetLevelProgress(string levelId) {
        return progress.GetProgress(levelId);
    }

    public bool IsWorldUnlocked(string worldId) {
        return progress.unlockedWorldIds.Contains(worldId);
    }

    public void UnlockWorld(string worldId) {
        if (!progress.unlockedWorldIds.Contains(worldId)) {
            progress.unlockedWorldIds.Add(worldId);
            SaveProgress();
        }
    }

    public void SaveProgress() {
        try {
            string json = JsonUtility.ToJson(progress, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"Progress saved to {saveFilePath}");
        } catch (System.Exception e) {
            Debug.LogError($"Failed to save progress: {e.Message}");
        }
    }

    public void LoadProgress() {
        if (File.Exists(saveFilePath)) {
            try {
                string json = File.ReadAllText(saveFilePath);
                progress = JsonUtility.FromJson<GameProgressData>(json);
                Debug.Log("Progress loaded successfully");
            } catch (System.Exception e) {
                Debug.LogError($"Failed to load progress: {e.Message}");
                progress = new GameProgressData();
            }
        } else {
            progress = new GameProgressData();
            // Unlock first world by default
            if (WorldManager.Instance.allWorlds.Length > 0) {
                progress.unlockedWorldIds.Add(WorldManager.Instance.allWorlds[0].worldId);
            }
            Debug.Log("No progress file found, starting fresh");
        }
    }

    public void ResetProgress() {
        progress = new GameProgressData();
        // Unlock first world
        if (WorldManager.Instance.allWorlds.Length > 0) {
            progress.unlockedWorldIds.Add(WorldManager.Instance.allWorlds[0].worldId);
        }
        SaveProgress();
        Debug.Log("Progress reset!");
    }
}
```

**Verification**:
- [ ] Progress saves to JSON
- [ ] Progress loads on startup
- [ ] MarkLevelComplete works
- [ ] World unlocking triggers correctly
- [ ] ResetProgress clears everything

---

## Phase 3: Create First Levels

### Task 3.1: Design Tutorial Level 1 - "First Steps"
**Priority**: High
**Estimated Time**: 1 hour

**Objective**: Teach basic movement and goal (fill lock with key)

**Design Constraints**:
- Small grid (6x6)
- 1 Lem starting on left
- 1 Key on middle platform
- 1 Lock on right platform
- 3 placeable spaces to bridge gaps
- 5 Default blocks in inventory

**Steps**:
1. Enter Designer Mode (E key)
2. Place permanent platforms (B key)
3. Mark 3 placeable spaces (Space key)
4. Place 1 Lem (L key)
5. Configure inventory (1 Default entry, max 5)
6. Save level (Ctrl+S)
7. Export to LevelDefinition asset

**Verification**:
- [ ] Level is solvable with 5 blocks
- [ ] Clear path from start to key to lock
- [ ] Win condition triggers when lock filled

---

### Task 3.2: Design Tutorial Level 2 - "The Crumbler"
**Priority**: High
**Estimated Time**: 1 hour

**Objective**: Introduce crumbler blocks

**Design Constraints**:
- Grid (8x6)
- 1 permanent crumbler in path
- Must use crumbler to cross, cannot go back
- 1 Key before crumbler
- 1 Lock after crumbler
- 4 Default blocks in inventory

**Verification**:
- [ ] Crumbler breaks after Lem crosses
- [ ] Cannot return to start
- [ ] Level solvable

---

### Task 3.3: Design Tutorial Level 3 - "Teleporter Twins"
**Priority**: High
**Estimated Time**: 1 hour

**Objective**: Introduce teleporters

**Design Constraints**:
- Grid (10x8)
- 2 teleporters (flavor A) - permanent
- Lem must use teleporter to reach key
- 1 Key at teleporter destination
- 1 Lock at separate location
- 6 Default blocks in inventory

**Verification**:
- [ ] Teleporter pair works
- [ ] Level solvable
- [ ] Clear which teleporters are paired (matching flavor)

---

### Task 3.4: Design Tutorial Level 4 - "Moving Up"
**Priority**: High
**Estimated Time**: 1 hour

**Objective**: Introduce transporters

**Design Constraints**:
- Grid (10x10)
- 1 permanent transporter (route: U4) - acts as elevator
- Key at top level (reachable via transporter)
- Lock at ground level
- 5 Default blocks in inventory

**Verification**:
- [ ] Transporter moves Lem up
- [ ] Transporter reverses on second use
- [ ] Level solvable

---

### Task 3.5: Design Tutorial Level 5 - "Puzzle Master"
**Priority**: High
**Estimated Time**: 1.5 hours

**Objective**: Combine all mechanics

**Design Constraints**:
- Grid (12x10)
- 2 Keys, 2 Locks
- Combination of: crumblers, teleporters, transporter
- Limited blocks (8 Default)
- Multiple valid solutions

**Verification**:
- [ ] All mechanics work together
- [ ] Level is challenging but solvable
- [ ] Multiple approaches possible

---

### Task 3.6: Create World_Tutorial WorldData Asset
**Priority**: High
**Estimated Time**: 30 minutes

**Steps**:
1. Right-click in Project
2. Create → AWITP → World Definition
3. Set:
   - worldId: "world_tutorial"
   - worldName: "Tutorial Island"
   - description: "Learn the basics of A Walk in the Park"
   - orderInGame: 0
4. Drag 5 tutorial LevelDefinition assets into levels array
5. Save asset

**Verification**:
- [ ] WorldData asset exists
- [ ] All 5 levels referenced
- [ ] WorldManager can load it

---

### Task 3.7: Test Level Progression
**Priority**: Critical
**Estimated Time**: 1 hour

**Testing Steps**:
1. Reset progress (ProgressManager.ResetProgress())
2. Load Level 1
3. Complete Level 1
4. Verify progress saves
5. Load Level 2
6. Complete all 5 levels
7. Verify world marked complete
8. Verify next world unlocked (if exists)

**Verification**:
- [ ] Each level loads correctly
- [ ] Completing level saves progress
- [ ] All levels completable
- [ ] World completion triggers unlock

---

## Phase 4: UI Implementation

### Task 4.1: Create MainMenu Scene
**Priority**: Medium
**Estimated Time**: 1 hour

**Steps**:
1. Create new scene: "MainMenu"
2. Add Canvas
3. Add "Start Game" button → loads WorldMap scene
4. Add "Quit" button → Application.Quit()
5. Add title text
6. Add background

**Verification**:
- [ ] Scene loads correctly
- [ ] Buttons work
- [ ] Transitions to WorldMap

---

### Task 4.2: Create WorldMap Scene with UI
**Priority**: High
**Estimated Time**: 3 hours

**Steps**:
1. Create new scene: "WorldMap"
2. Add Canvas
3. Create WorldMapUI script:
   - Display all worlds
   - Show locked/unlocked state
   - Click world → load LevelSelect scene with worldId
4. Add visual feedback:
   - Locked worlds: greyed out, padlock icon
   - Unlocked worlds: colored, clickable
   - Completed worlds: checkmark

**Code Structure**:
```csharp
public class WorldMapUI : MonoBehaviour {
    public GameObject worldButtonPrefab;
    public Transform worldButtonContainer;

    void Start() {
        PopulateWorldButtons();
    }

    void PopulateWorldButtons() {
        WorldData[] worlds = WorldManager.Instance.allWorlds;
        foreach (var world in worlds) {
            GameObject buttonObj = Instantiate(worldButtonPrefab, worldButtonContainer);
            WorldButton button = buttonObj.GetComponent<WorldButton>();
            button.Initialize(world);
        }
    }
}

public class WorldButton : MonoBehaviour {
    public Text worldNameText;
    public Image lockIcon;
    public Button button;

    private WorldData world;

    public void Initialize(WorldData world) {
        this.world = world;
        worldNameText.text = world.worldName;

        bool unlocked = WorldManager.Instance.IsWorldUnlocked(world.worldId);
        bool completed = WorldManager.Instance.IsWorldComplete(world.worldId);

        button.interactable = unlocked;
        lockIcon.enabled = !unlocked;

        if (completed) {
            // Show checkmark
        }
    }

    public void OnClick() {
        // Load LevelSelect scene with world ID
        PlayerPrefs.SetString("SelectedWorldId", world.worldId);
        SceneManager.LoadScene("LevelSelect");
    }
}
```

**Verification**:
- [ ] All worlds display
- [ ] Locked worlds not clickable
- [ ] Clicking world loads LevelSelect

---

### Task 4.3: Create LevelSelect UI
**Priority**: High
**Estimated Time**: 2 hours

**Steps**:
1. Create LevelSelectUI script for WorldMap scene OR separate scene
2. Display levels in selected world
3. Show completion state for each level
4. Click level → load Game scene with levelId

**Code Structure**:
```csharp
public class LevelSelectUI : MonoBehaviour {
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public Text worldNameText;

    private string worldId;

    void Start() {
        worldId = PlayerPrefs.GetString("SelectedWorldId");
        WorldData world = WorldManager.Instance.GetWorld(worldId);
        worldNameText.text = world.worldName;

        PopulateLevelButtons(world);
    }

    void PopulateLevelButtons(WorldData world) {
        foreach (var level in world.levels) {
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            LevelButton button = buttonObj.GetComponent<LevelButton>();
            button.Initialize(level);
        }
    }
}

public class LevelButton : MonoBehaviour {
    public Text levelNameText;
    public Image completionCheckmark;

    private LevelDefinition level;

    public void Initialize(LevelDefinition level) {
        this.level = level;
        levelNameText.text = level.levelName;

        bool completed = ProgressManager.Instance.IsLevelComplete(level.levelId);
        completionCheckmark.enabled = completed;
    }

    public void OnClick() {
        PlayerPrefs.SetString("SelectedLevelId", level.levelId);
        SceneManager.LoadScene("Game");
    }
}
```

**Verification**:
- [ ] Levels display correctly
- [ ] Completed levels show checkmark
- [ ] Clicking level loads it

---

### Task 4.4: Create Victory Screen UI
**Priority**: Medium
**Estimated Time**: 1.5 hours

**Steps**:
1. Create VictoryScreenUI script
2. Show when level complete
3. Display:
   - "Level Complete!" message
   - Next Level button
   - Retry button
   - World Map button
4. Add confetti particles (optional)

**Code**:
```csharp
public class VictoryScreenUI : MonoBehaviour {
    public GameObject victoryPanel;
    public Button nextLevelButton;
    public Button retryButton;
    public Button worldMapButton;

    void Start() {
        victoryPanel.SetActive(false);
    }

    public void Show() {
        victoryPanel.SetActive(true);

        // Check if there's a next level
        string currentLevelId = LevelManager.Instance.currentLevelDef.levelId;
        LevelDefinition nextLevel = GetNextLevel(currentLevelId);
        nextLevelButton.interactable = (nextLevel != null);
    }

    public void OnNextLevel() {
        string currentLevelId = LevelManager.Instance.currentLevelDef.levelId;
        LevelDefinition nextLevel = GetNextLevel(currentLevelId);
        if (nextLevel != null) {
            LevelManager.Instance.LoadLevel(nextLevel);
            victoryPanel.SetActive(false);
        }
    }

    public void OnRetry() {
        LevelManager.Instance.RestartLevel();
        victoryPanel.SetActive(false);
    }

    public void OnWorldMap() {
        SceneManager.LoadScene("WorldMap");
    }

    private LevelDefinition GetNextLevel(string currentLevelId) {
        // Find current level in world, return next level
        foreach (var world in WorldManager.Instance.allWorlds) {
            for (int i = 0; i < world.levels.Length; i++) {
                if (world.levels[i].levelId == currentLevelId) {
                    if (i + 1 < world.levels.Length) {
                        return world.levels[i + 1];
                    }
                    return null;
                }
            }
        }
        return null;
    }
}
```

**Verification**:
- [ ] Victory screen shows on level complete
- [ ] Next Level button works
- [ ] Retry button works
- [ ] World Map button works

---

### Task 4.5: Connect Victory Screen to Level Completion
**Priority**: High
**Estimated Time**: 30 minutes

**File**: Update `LevelManager.cs`

**Changes**:
```csharp
public class LevelManager : MonoBehaviour {
    public VictoryScreenUI victoryScreenUI;

    void Update() {
        // Check win condition each frame in Play Mode
        if (EditorModeManager.Instance.CurrentMode == GameMode.Play) {
            if (IsLevelComplete() && !levelCompletedThisSession) {
                OnLevelComplete();
                levelCompletedThisSession = true;
            }
        }
    }

    public void OnLevelComplete() {
        Debug.Log("Level Complete!");
        ProgressManager.Instance.MarkLevelComplete(currentLevelDef.levelId);
        victoryScreenUI.Show();
    }
}
```

**Verification**:
- [ ] Victory screen triggers automatically when all locks filled
- [ ] Progress saves
- [ ] Can proceed to next level

---

## Phase 5: Polish & Testing

### Task 5.1: Add Scene Transitions
**Priority**: Low
**Estimated Time**: 1 hour

**Steps**:
1. Create fade-in/fade-out transition
2. Add loading screen for level load
3. Smooth camera transitions

**Verification**:
- [ ] Transitions look smooth
- [ ] No jarring cuts

---

### Task 5.2: Add Sound Effects
**Priority**: Low
**Estimated Time**: 2 hours

**Sounds Needed**:
- Block placement
- Block removal
- Lem walking
- Crumbler breaking
- Teleporter activation
- Key pickup
- Lock fill
- Victory fanfare

**Verification**:
- [ ] All sounds play at correct times
- [ ] Volume balanced

---

### Task 5.3: Add Particle Effects
**Priority**: Low
**Estimated Time**: 1.5 hours

**Effects Needed**:
- Confetti on level complete
- Dust when crumbler breaks
- Sparkles when teleporter activates
- Glow when key picked up

**Verification**:
- [ ] Effects enhance experience
- [ ] Not too distracting

---

### Task 5.4: Comprehensive Playtest
**Priority**: Critical
**Estimated Time**: 3 hours

**Testing Checklist**:
- [ ] Complete all 5 tutorial levels
- [ ] Verify world unlocks after completing all levels
- [ ] Test save/load progress
- [ ] Test level restart
- [ ] Test all block types in each level
- [ ] Test edge cases (Lem falling off, etc.)
- [ ] Performance test (60fps target)
- [ ] Memory leak test (profile in Unity)

**Bug Tracking**: Document all issues

---

### Task 5.5: Balance & Polish
**Priority**: Medium
**Estimated Time**: Variable

**Actions**:
- Adjust difficulty based on playtesting
- Fix any bugs found
- Polish UI visuals
- Add any missing feedback
- Optimize performance

---

## Summary Checklist

### Phase 1: Prefabs ✓
- [ ] All block prefabs created
- [ ] Lem prefab created
- [ ] Code updated to use prefabs
- [ ] Existing level still works

### Phase 2: Foundation ✓
- [ ] LevelDefinition ScriptableObject
- [ ] WorldData ScriptableObject
- [ ] GameProgressData
- [ ] LevelManager implemented
- [ ] WorldManager implemented
- [ ] ProgressManager implemented
- [ ] Save/load progress works

### Phase 3: Levels ✓
- [ ] 5 tutorial levels designed
- [ ] World_Tutorial created
- [ ] All levels tested and solvable
- [ ] Level progression works

### Phase 4: UI ✓
- [ ] MainMenu scene
- [ ] WorldMap scene with UI
- [ ] LevelSelect UI
- [ ] Victory screen
- [ ] Full UI flow works

### Phase 5: Polish ✓
- [ ] Scene transitions
- [ ] Sound effects
- [ ] Particle effects
- [ ] Comprehensive playtest
- [ ] All bugs fixed

---

## Next Steps After Completion

1. **Create More Worlds**: Design "Basics" and "Advanced" worlds
2. **Add More Block Types**: Implement new mechanics
3. **Community Features**: Level sharing, custom levels
4. **Mobile Port**: Touch controls, responsive UI
5. **Achievements**: Track player accomplishments
6. **Leaderboards**: Speedrun times, efficiency scores

---

## Estimated Total Time

| Phase | Time |
|-------|------|
| Phase 1: Prefabs | 6 hours |
| Phase 2: Foundation | 8 hours |
| Phase 3: Levels | 6.5 hours |
| Phase 4: UI | 8 hours |
| Phase 5: Polish | 7.5 hours |
| **Total** | **36 hours** |

**Realistic Timeline**: 5-6 weeks working part-time (8-10 hours/week)

---

## Tips for Success

1. **Commit Often**: Use git to commit after each task
2. **Test Incrementally**: Don't wait until the end to test
3. **Keep Backups**: Save prefabs and assets frequently
4. **Ask for Help**: If stuck, debug systematically
5. **Stay Organized**: Follow the file structure
6. **Document Changes**: Update this file as you complete tasks

---

**Document Status**: Ready for Implementation
**Last Updated**: 2026-01-26
**Target**: Taylor (Designer/Developer)
