# Level System Setup Guide

This guide walks you through setting up the complete multi-level system for A Walk in the Park.

## Overview

The level system implementation includes:
- ✅ **Phase 1**: Prefab support (partially complete - need Lem prefab)
- ✅ **Phase 2**: Level system foundation (complete)
- ✅ **Phase 3**: Level content creation tools (complete)
- ✅ **Phase 4**: UI implementation (scripts complete, scenes need setup)

---

## Part 1: Create Level Definition Assets

### Step 1: Import JSON Level Data

Three tutorial level JSON files have been created in:
- `Assets/Resources/Levels/LevelDefinitions/Tutorial_01_FirstSteps.json`
- `Assets/Resources/Levels/LevelDefinitions/Tutorial_02_TheCrumbler.json`
- `Assets/Resources/Levels/LevelDefinitions/Tutorial_03_TeleporterTwins.json`

### Step 2: Convert JSON to LevelDefinition Assets

1. In Unity, go to menu: **Tools > AWITP > Create Level Definition from JSON**
2. A window will open with fields to fill out

3. **For Level 1 (First Steps)**:
   - Drag `Tutorial_01_FirstSteps.json` to "Level JSON File" field
   - Level ID: `tutorial_01_firststeps`
   - Level Name: `First Steps`
   - World ID: `world_tutorial`
   - Order in World: `0`
   - Click **Create LevelDefinition Asset**

4. **For Level 2 (The Crumbler)**:
   - Drag `Tutorial_02_TheCrumbler.json` to "Level JSON File" field
   - Level ID: `tutorial_02_thecrumbler`
   - Level Name: `The Crumbler`
   - World ID: `world_tutorial`
   - Order in World: `1`
   - Click **Create LevelDefinition Asset**

5. **For Level 3 (Teleporter Twins)**:
   - Drag `Tutorial_03_TeleporterTwins.json` to "Level JSON File" field
   - Level ID: `tutorial_03_teleportertwins`
   - Level Name: `Teleporter Twins`
   - World ID: `world_tutorial`
   - Order in World: `2`
   - Click **Create LevelDefinition Asset**

**Result**: You should now have 3 `.asset` files in `Assets/Resources/Levels/LevelDefinitions/`

---

## Part 2: Create World Data Asset

### Step 1: Open World Data Creator

1. Go to menu: **Tools > AWITP > Create World Data**
2. A window will open

### Step 2: Configure Tutorial World

Fill in the fields:
- World ID: `world_tutorial`
- World Name: `Tutorial Island`
- Description: `Learn the basics of A Walk in the Park`
- Order in Game: `0`
- Visual Color: Choose a nice color (e.g., cyan/blue)

### Step 3: Add Level References

1. Click **Add Level Slot** three times (for 3 levels)
2. Drag the three LevelDefinition assets you created into the level slots:
   - Level 0: `tutorial_01_firststeps.asset`
   - Level 1: `tutorial_02_thecrumbler.asset`
   - Level 2: `tutorial_03_teleportertwins.asset`

3. Click **Create WorldData Asset**

**Result**: You should now have `world_tutorial.asset` in `Assets/Resources/Levels/Worlds/`

---

## Part 3: Create MainMenu Scene

### Step 1: Create New Scene

1. File > New Scene
2. Save as `Assets/Scenes/MainMenu.unity`

### Step 2: Create UI Canvas

1. Right-click in Hierarchy > UI > Canvas
2. Set Canvas Scaler to "Scale With Screen Size" (1920x1080 reference)

### Step 3: Create Title Text

1. Right-click Canvas > UI > Text
2. Rename to "TitleText"
3. Text: "A WALK IN THE PARK"
4. Font Size: 72
5. Alignment: Center
6. Anchor to top-center
7. Position: Y = -200

### Step 4: Create Start Button

1. Right-click Canvas > UI > Button
2. Rename to "StartButton"
3. Text child: "START GAME"
4. Font Size: 36
5. Anchor to center
6. Position: Y = 0
7. Size: 300x80

### Step 5: Create Quit Button

1. Right-click Canvas > UI > Button
2. Rename to "QuitButton"
3. Text child: "QUIT"
4. Font Size: 36
5. Anchor to center
6. Position: Y = -100
7. Size: 300x80

### Step 6: Add MainMenuUI Script

1. Create empty GameObject in root: "MainMenuManager"
2. Add Component: `MainMenuUI` script
3. Assign references:
   - Start Game Button: drag StartButton
   - Quit Button: drag QuitButton
   - Title Text: drag TitleText
   - World Map Scene Name: "WorldMap"

**Result**: MainMenu scene is ready!

---

## Part 4: Create WorldMap Scene

### Step 1: Create New Scene

1. File > New Scene
2. Save as `Assets/Scenes/WorldMap.unity`

### Step 2: Create UI Canvas

1. Right-click Hierarchy > UI > Canvas
2. Set Canvas Scaler to "Scale With Screen Size" (1920x1080 reference)

### Step 3: Create Title Text

1. Right-click Canvas > UI > Text
2. Rename to "TitleText"
3. Text: "SELECT WORLD"
4. Font Size: 60
5. Alignment: Center
6. Anchor to top-center
7. Position: Y = -100

### Step 4: Create World Button Container

1. Right-click Canvas > UI > Panel
2. Rename to "WorldButtonContainer"
3. Anchor to center
4. Add Component: `Vertical Layout Group`
   - Spacing: 20
   - Child Alignment: Middle Center
   - Child Force Expand: Width = true

### Step 5: Create World Button Prefab

1. Right-click WorldButtonContainer > UI > Button
2. Rename to "WorldButtonPrefab"
3. Inside button, modify:
   - Add UI > Text (for world name) - rename "WorldNameText"
   - Add UI > Text (for description) - rename "DescriptionText"
   - Add UI > Image (for lock icon) - rename "LockIcon"
   - Add UI > Image (for completion checkmark) - rename "CompletionIcon"
4. Add Component to button: `WorldButton` script
5. Assign references in WorldButton:
   - World Name Text: WorldNameText
   - Description Text: DescriptionText
   - Lock Icon: LockIcon
   - Completion Icon: CompletionIcon
   - Background Image: Button's Image component

6. Drag button to Project window to create prefab at `Assets/Prefabs/UI/WorldButtonPrefab.prefab`
7. Delete the button from hierarchy (we only need the prefab)

### Step 6: Create Back Button

1. Right-click Canvas > UI > Button
2. Rename to "BackButton"
3. Text: "BACK TO MENU"
4. Anchor to bottom-left
5. Position: X = 150, Y = 50

### Step 7: Create Progress Text

1. Right-click Canvas > UI > Text
2. Rename to "ProgressText"
3. Text: "Progress: 0/0 Worlds Complete"
4. Anchor to top-right
5. Position: X = -200, Y = -50

### Step 8: Add WorldMapUI Script

1. Create empty GameObject: "WorldMapManager"
2. Add Component: `WorldMapUI` script
3. Assign references:
   - World Button Container: drag WorldButtonContainer
   - World Button Prefab: drag WorldButtonPrefab from Project
   - Back Button: drag BackButton
   - Progress Text: drag ProgressText
   - Main Menu Scene Name: "MainMenu"
   - Level Select Scene Name: "LevelSelect"

**Result**: WorldMap scene is ready!

---

## Part 5: Create LevelSelect Scene

### Step 1: Create New Scene

1. File > New Scene
2. Save as `Assets/Scenes/LevelSelect.unity`

### Step 2: Create UI Canvas

1. Right-click Hierarchy > UI > Canvas
2. Set Canvas Scaler to "Scale With Screen Size"

### Step 3: Create World Name Text

1. Right-click Canvas > UI > Text
2. Rename to "WorldNameText"
3. Font Size: 60
4. Anchor to top-center
5. Position: Y = -100

### Step 4: Create World Description Text

1. Right-click Canvas > UI > Text
2. Rename to "WorldDescriptionText"
3. Font Size: 30
4. Anchor to top-center
5. Position: Y = -180

### Step 5: Create Level Button Container

1. Right-click Canvas > UI > Panel
2. Rename to "LevelButtonContainer"
3. Anchor to center
4. Add Component: `Grid Layout Group`
   - Cell Size: 300x150
   - Spacing: 20x20
   - Child Alignment: Upper Center

### Step 6: Create Level Button Prefab

1. Right-click LevelButtonContainer > UI > Button
2. Rename to "LevelButtonPrefab"
3. Inside button, add:
   - UI > Text (for level name) - rename "LevelNameText"
   - UI > Text (for level number) - rename "LevelNumberText"
   - UI > Text (for stats) - rename "StatsText"
   - UI > Image (for completion checkmark) - rename "CompletionCheckmark"
4. Add Component to button: `LevelButton` script
5. Assign references
6. Drag to Project: `Assets/Prefabs/UI/LevelButtonPrefab.prefab`
7. Delete from hierarchy

### Step 7: Create Back Button & Progress Text

1. Add Back button (bottom-left)
2. Add Progress text (top-right): "0/0 Levels Complete"

### Step 8: Add LevelSelectUI Script

1. Create empty GameObject: "LevelSelectManager"
2. Add Component: `LevelSelectUI` script
3. Assign all references

**Result**: LevelSelect scene is ready!

---

## Part 6: Update Game Scene with Victory Screen

### Step 1: Open Master.unity (Game Scene)

1. Open `Assets/Scenes/Master.unity`

### Step 2: Create Victory Screen UI

1. Right-click Canvas (or create if doesn't exist) > UI > Panel
2. Rename to "VictoryPanel"
3. Make it full screen (stretch anchor)
4. Background color: semi-transparent black (0, 0, 0, 180)

### Step 3: Add Victory Screen Elements

Inside VictoryPanel, create:
1. **Title Text**: "LEVEL COMPLETE!" (center, large font)
2. **Stats Text**: Shows time/blocks/attempts (below title)
3. **Next Level Button**: "NEXT LEVEL" (center-left)
4. **Retry Button**: "RETRY" (center)
5. **Level Select Button**: "LEVEL SELECT" (center-right)
6. **World Map Button**: "WORLD MAP" (bottom-center)

### Step 4: Add VictoryScreenUI Script

1. Create empty GameObject: "VictoryScreenManager"
2. Add Component: `VictoryScreenUI` script
3. Assign all references:
   - Victory Panel: drag VictoryPanel
   - All buttons and texts
   - Scene names

### Step 5: Set VictoryPanel Inactive

1. Select VictoryPanel in hierarchy
2. Uncheck the checkbox at top of Inspector to disable it
   (It will be shown programmatically when level completes)

**Result**: Game scene now has victory screen!

---

## Part 7: Update Build Settings

### Step 1: Add Scenes to Build

1. File > Build Settings
2. Click "Add Open Scenes" for each scene:
   - MainMenu (index 0)
   - WorldMap (index 1)
   - LevelSelect (index 2)
   - Master/Game (index 3)

3. Ensure MainMenu is at index 0 (drag to reorder if needed)

---

## Part 8: Update LevelManager to Load from PlayerPrefs

The LevelManager needs a way to load the selected level when the Game scene starts.

### Step 1: Create GameSceneInitializer Script

This script will be added to the Game scene to automatically load the selected level:

```csharp
using UnityEngine;

public class GameSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "");

        if (!string.IsNullOrEmpty(levelId) && LevelManager.Instance != null)
        {
            Debug.Log($"[GameSceneInitializer] Loading level: {levelId}");
            LevelManager.Instance.LoadLevel(levelId);
        }
        else
        {
            Debug.LogWarning("[GameSceneInitializer] No level selected!");
        }
    }
}
```

### Step 2: Add to Game Scene

1. Open Master.unity (Game scene)
2. Create empty GameObject: "GameSceneInitializer"
3. Add Component: `GameSceneInitializer` script

**Result**: Game scene will now automatically load the selected level!

---

## Part 9: Create Lem Prefab (Manual Step)

Currently, Lems are created programmatically. To use a prefab:

### Step 1: Create Lem in Scene

1. In Game scene, enter Play mode
2. Press 'E' to enter Designer Mode
3. Press 'L' to place a Lem
4. Exit Play mode

### Step 2: Find Lem in Hierarchy

1. The Lem GameObject should still exist with all its components

### Step 3: Save as Prefab

1. Drag the Lem GameObject from Hierarchy to Project window
2. Save to: `Assets/Resources/Characters/Lem.prefab`
3. Ensure it has:
   - LemController component
   - Rigidbody
   - CapsuleCollider
   - FootPoint child
   - Glitch visual (if using)

**Result**: Lem prefab now exists and will be used automatically!

---

## Part 10: Testing

### Test Flow 1: Basic Navigation

1. Play the game from MainMenu scene
2. Click "START GAME"
3. Should see WorldMap with Tutorial World
4. Click Tutorial World
5. Should see 3 levels
6. Click a level
7. Game scene should load with that level

### Test Flow 2: Level Completion

1. Load a level (e.g., First Steps)
2. Complete the level (place blocks, get Lem to lock)
3. Victory screen should appear
4. Check that level is marked complete in progress
5. Try "Next Level" button
6. Try "Retry" button

### Test Flow 3: Progress Persistence

1. Complete a level
2. Exit to world map
3. Close the game
4. Reopen the game
5. Level should still be marked as complete

---

## Troubleshooting

### "WorldManager instance not found!"

**Solution**: Make sure WorldManager is initialized in MainMenu scene. The MainMenuUI script should create it.

### "No worlds found to display"

**Solution**: Ensure `world_tutorial.asset` exists in `Assets/Resources/Levels/Worlds/` and has levels assigned.

### "Level not found: tutorial_01_firststeps"

**Solution**: Check that LevelDefinition assets were created correctly and have matching levelIds.

### Victory screen doesn't appear

**Solution**:
1. Check that VictoryScreenUI is in Game scene
2. Check that it subscribes to LevelManager.OnLevelComplete event
3. Check that VictoryPanel is assigned

### Level doesn't load in Game scene

**Solution**:
1. Check GameSceneInitializer exists in Game scene
2. Check that SelectedLevelId is being set in PlayerPrefs
3. Check console for errors

---

## Summary

You've now set up:
- ✅ 3 tutorial levels (JSON + ScriptableObject assets)
- ✅ 1 tutorial world grouping the levels
- ✅ MainMenu scene with navigation
- ✅ WorldMap scene showing worlds
- ✅ LevelSelect scene showing levels
- ✅ Victory screen overlay in Game scene
- ✅ Complete level progression system
- ⚠️ Lem prefab (manual step needed)

**Next Steps**:
1. Create more levels using the Designer Mode
2. Design more worlds (e.g., "Advanced Puzzles")
3. Playtest and balance difficulty
4. Add sound effects and polish

---

**Status**: Level system implementation complete!
**Last Updated**: 2026-01-27
