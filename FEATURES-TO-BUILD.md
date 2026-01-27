# Features to Build for Complete Level Creation Experience

**STATUS: ✅ STILL RELEVANT - Future Enhancement Wishlist**

**Note**: These features are separate from the multi-level system (which is now implemented). These are quality-of-life improvements for the Designer Mode level creation workflow.

**What Was Completed (2026-01-27)**:
- ✅ Multi-level system with progression (LevelManager, WorldManager, ProgressManager)
- ✅ Editor tools for creating Level/World assets (LevelDefinitionCreator, WorldDataCreator)
- ✅ UI system for menu navigation

**What This Document Covers**:
- Enhancements to Designer Mode workflow (undo/redo, error messages, etc.)
- Polish features for level editing experience
- Future improvements for content creators

This document tracks features mentioned in [HOW-TO-CREATE-A-LEVEL.md](HOW-TO-CREATE-A-LEVEL.md) that would enhance the level creation workflow but don't currently exist.

## Priority: Critical (Blockers for Good UX)

These features are essential for a smooth level creation experience:

### 1. Undo/Redo System
**Current Issue**: Changes are permanent except via load. One mistake requires reloading entire level.

**Implementation Needed**:
- [ ] Command pattern for all editor actions
- [ ] Action history stack (place block, remove block, mark placeable, etc.)
- [ ] `Ctrl+Z` / `Cmd+Z` for undo
- [ ] `Ctrl+Shift+Z` / `Cmd+Shift+Z` for redo
- [ ] Visual indicator showing undo/redo availability
- [ ] Clear history on level load

**Files to Modify**:
- [EditorController.cs](Assets/Scripts/EditorController.cs) - Add undo/redo input handling
- Create new `EditorCommandSystem.cs` - Command pattern implementation
- [GridManager.cs](Assets/Scripts/GridManager.cs) - Make actions reversible

**Estimated Complexity**: High (requires refactoring existing placement logic)

---

### 2. User-Facing Error Messages
**Current Issue**: Placement errors only show in console. Players don't understand why blocks won't place.

**Implementation Needed**:
- [ ] On-screen error display system
- [ ] Toast/notification UI component
- [ ] Integration with `BaseBlock.GetPlacementErrorMessage()`
- [ ] User-friendly error text for all failure cases
- [ ] Auto-dismiss after 3-5 seconds
- [ ] Queue system for multiple errors

**Files to Create**:
- `Assets/Scripts/UI/ErrorNotificationUI.cs` - Toast message system

**Files to Modify**:
- [EditorController.cs](Assets/Scripts/EditorController.cs) - Call error UI on failed placement
- [BaseBlock.cs](Assets/Scripts/BaseBlock.cs) - Ensure all errors return user-friendly messages

**Estimated Complexity**: Medium

---

### 3. Level Validation Tool
**Current Issue**: No way to verify level is solvable before saving. Players can create broken levels.

**Implementation Needed**:
- [ ] Validation button in UI
- [ ] Check: At least one Lem exists
- [ ] Check: All teleporters have pairs
- [ ] Check: All transporter routes are valid
- [ ] Check: Win condition is defined (if using locks)
- [ ] Check: Placeable spaces exist
- [ ] Report validation results to user
- [ ] Optional: Prevent saving invalid levels

**Files to Create**:
- `Assets/Scripts/Validation/LevelValidator.cs` - Validation logic
- `Assets/Scripts/UI/ValidationResultsUI.cs` - Display validation report

**Files to Modify**:
- [LevelSaveSystem.cs](Assets/Scripts/LevelSaveSystem.cs) - Call validator before save
- [ControlsUI.cs](Assets/Scripts/ControlsUI.cs) - Add validation hotkey display

**Estimated Complexity**: Medium

---

## Priority: High (Major UX Improvements)

These features significantly improve the level creation workflow:

### 4. Multiple Level Save Slots
**Current Issue**: Only one level can be saved at a time (`current_level.json`).

**Implementation Needed**:
- [ ] Level naming system
- [ ] Save/load with level name parameter
- [ ] Level browser UI showing all saved levels
- [ ] Delete level functionality
- [ ] New hotkeys: `Ctrl+Shift+S` for "Save As", `Ctrl+O` for "Open"
- [ ] Default to last opened level on startup

**Files to Create**:
- `Assets/Scripts/UI/LevelBrowserUI.cs` - Grid of saved levels with thumbnails
- `Assets/Scripts/UI/SaveAsDialogUI.cs` - Name entry dialog

**Files to Modify**:
- [LevelSaveSystem.cs](Assets/Scripts/LevelSaveSystem.cs) - Support named files
- [EditorController.cs](Assets/Scripts/EditorController.cs) - Add save as hotkey
- [LevelData.cs](Assets/Scripts/LevelData.cs) - Add `levelName` field (already exists!)

**Estimated Complexity**: Medium

---

### 5. Level Thumbnail Generation
**Current Issue**: Level browser has no visual preview. Hard to identify levels.

**Implementation Needed**:
- [ ] Render level to texture on save
- [ ] Store thumbnail as PNG alongside level JSON
- [ ] Display in level browser
- [ ] Auto-generation (camera snapshot)
- [ ] Fallback placeholder for levels without thumbnails

**Files to Create**:
- `Assets/Scripts/Utilities/ThumbnailGenerator.cs` - Render level to texture

**Files to Modify**:
- [LevelSaveSystem.cs](Assets/Scripts/LevelSaveSystem.cs) - Generate thumbnail on save
- `LevelBrowserUI.cs` (from #4) - Display thumbnails

**Estimated Complexity**: Medium (Unity RenderTexture + file I/O)

---

### 6. Visual Route Editor for Transporters
**Current Issue**: Route configuration requires typing text strings like "L2 U3 R1" in Inspector.

**Implementation Needed**:
- [ ] Custom Inspector for `LevelBlockInventoryConfig`
- [ ] Visual grid for drawing routes
- [ ] Click-and-drag route creation
- [ ] Real-time validation (check for collisions)
- [ ] Display route length and complexity
- [ ] Export to route string format

**Files to Create**:
- `Assets/Scripts/Editor/RouteEditorWindow.cs` - Unity Editor window for route design
- `Assets/Scripts/Editor/LevelBlockInventoryConfigEditor.cs` - Custom Inspector

**Files to Modify**:
- [RouteParser.cs](Assets/Scripts/RouteParser.cs) - Already handles parsing, just needs UI

**Estimated Complexity**: High (Unity Editor GUI programming)

---

### 7. Copy/Paste Block Patterns
**Current Issue**: Repeating patterns requires manual placement. Tedious for large structures.

**Implementation Needed**:
- [ ] Selection box system (drag to select multiple blocks)
- [ ] `Ctrl+C` - Copy selected blocks
- [ ] `Ctrl+V` - Paste at cursor position
- [ ] Clipboard stores block types and relative positions
- [ ] Visual preview of paste location
- [ ] Respect placeable space constraints on paste

**Files to Create**:
- `Assets/Scripts/Editor/ClipboardSystem.cs` - Copy/paste logic
- `Assets/Scripts/Editor/SelectionBox.cs` - Multi-block selection

**Files to Modify**:
- [EditorController.cs](Assets/Scripts/EditorController.cs) - Add copy/paste hotkeys
- [GridCursor.cs](Assets/Scripts/GridCursor.cs) - Show paste preview

**Estimated Complexity**: High

---

## Priority: Medium (Nice-to-Have)

These features add polish but aren't essential:

### 8. In-Game Tutorial System
**Current Issue**: No guided onboarding. New users must read documentation.

**Implementation Needed**:
- [ ] Step-by-step tutorial level
- [ ] Overlay UI with instructions
- [ ] Highlight relevant controls
- [ ] Gated progression (complete step to continue)
- [ ] Skip tutorial option
- [ ] 5-10 minute completion time

**Files to Create**:
- `Assets/Scripts/Tutorial/TutorialManager.cs` - Tutorial state machine
- `Assets/Scripts/Tutorial/TutorialStep.cs` - Individual tutorial steps
- `Assets/Scripts/UI/TutorialOverlayUI.cs` - Instruction display

**Files to Modify**:
- [GameInitializer.cs](Assets/Scripts/GameInitializer.cs) - Launch tutorial on first run

**Estimated Complexity**: High (requires significant content creation)

---

### 9. Level Overview Map
**Current Issue**: Can only see grid one screen at a time. Hard to visualize entire level.

**Implementation Needed**:
- [ ] Minimap UI in corner of screen
- [ ] Zoomed-out view of entire grid
- [ ] Blocks rendered as colored dots
- [ ] Cursor position indicator
- [ ] Click minimap to jump cursor
- [ ] Toggle visibility

**Files to Create**:
- `Assets/Scripts/UI/MinimapUI.cs` - Minimap rendering

**Files to Modify**:
- [GridManager.cs](Assets/Scripts/GridManager.cs) - Provide minimap data
- [ControlsUI.cs](Assets/Scripts/ControlsUI.cs) - Add minimap toggle key

**Estimated Complexity**: Medium

---

### 10. Block Inspector Panel
**Current Issue**: Can't view block properties during editing. Must check Inspector or code.

**Implementation Needed**:
- [ ] UI panel showing selected block details
- [ ] Display block type, flavor, route (for transporters)
- [ ] Show whether block is permanent or placeable
- [ ] Inventory status for that block type
- [ ] Edit properties inline (advanced)

**Files to Create**:
- `Assets/Scripts/UI/BlockInspectorUI.cs` - Inspector panel

**Files to Modify**:
- [GridCursor.cs](Assets/Scripts/GridCursor.cs) - Track selected block
- [BaseBlock.cs](Assets/Scripts/BaseBlock.cs) - Expose properties for inspector

**Estimated Complexity**: Medium

---

### 11. Grid Customization UI
**Current Issue**: Must edit GridManager Inspector to change grid size. Not user-friendly.

**Implementation Needed**:
- [ ] In-game settings menu
- [ ] Sliders for width, height, cell size
- [ ] Real-time preview of grid changes
- [ ] Confirm/cancel dialog (changes can break level)
- [ ] Warning if resizing would delete blocks
- [ ] Save grid settings with level

**Files to Create**:
- `Assets/Scripts/UI/GridSettingsUI.cs` - Settings panel

**Files to Modify**:
- [GridManager.cs](Assets/Scripts/GridManager.cs) - Expose settings API
- [LevelData.cs](Assets/Scripts/LevelData.cs) - Already saves grid settings

**Estimated Complexity**: Low

---

### 12. Example Level Pack
**Current Issue**: No reference levels. Users don't know what "good" looks like.

**Implementation Needed**:
- [ ] 5-10 pre-designed levels
- [ ] Range of difficulties (tutorial → advanced)
- [ ] Demonstrate each block type
- [ ] Show design patterns (resource management, timing, etc.)
- [ ] Save as JSON in `Assets/Levels/Examples/`
- [ ] Load example levels from menu

**Files to Create**:
- `Assets/Levels/Examples/01_Tutorial.json`
- `Assets/Levels/Examples/02_Crumblers.json`
- `Assets/Levels/Examples/03_Transporters.json`
- etc.

**Files to Modify**:
- Create new example level loader (optional, can just use Load functionality)

**Estimated Complexity**: Medium (mostly level design work, not coding)

---

## Priority: Low (Polish & Future)

These features enhance the experience but aren't urgent:

### 13. Sound Effects & Music
**Audio Feedback**: Block placement, Lem walking, crumbler breaking, teleport, key pickup, lock fill

**Implementation Needed**:
- [ ] AudioManager singleton
- [ ] Sound effect library
- [ ] Background music tracks
- [ ] Volume controls
- [ ] Mute toggle

**Estimated Complexity**: Low (Unity audio is straightforward)

---

### 14. Particle Effects
**Visual Feedback**: Block placement poof, crumbler dust, teleport sparkles, key glow

**Implementation Needed**:
- [ ] Particle system prefabs
- [ ] Trigger particles on events
- [ ] Configurable intensity

**Estimated Complexity**: Low

---

### 15. Camera Controls (Pan & Zoom)
**Camera Movement**: Useful for large levels that exceed single screen.

**Implementation Needed**:
- [ ] Mouse drag to pan camera
- [ ] Scroll wheel to zoom
- [ ] Reset camera hotkey
- [ ] Clamp camera to grid bounds
- [ ] Smooth camera movement

**Files to Modify**:
- [CameraSetup.cs](Assets/Scripts/CameraSetup.cs) - Add camera controls

**Estimated Complexity**: Low

---

### 16. Accessibility Features
**Colorblind Mode**: Alternative color schemes for block types
**Control Remapping**: Custom keybindings
**Text Size Options**: Adjustable UI text

**Implementation Needed**:
- [ ] Settings menu with accessibility options
- [ ] Alternative color palettes
- [ ] Keybinding rebind UI
- [ ] Text size slider

**Files to Modify**:
- [BlockColors.cs](Assets/Scripts/BlockColors.cs) - Support multiple color schemes
- Create new `InputManager.cs` for rebindable controls

**Estimated Complexity**: Medium

---

### 17. Difficulty Analysis
**Automated Scoring**: Analyze level complexity and suggest difficulty rating.

**Implementation Needed**:
- [ ] Algorithm to score puzzle complexity
- [ ] Factors: block count, block types, placeable space ratio, etc.
- [ ] Display difficulty rating (1-5 stars)
- [ ] Optional: Suggest improvements

**Files to Create**:
- `Assets/Scripts/Analysis/DifficultyAnalyzer.cs`

**Estimated Complexity**: High (requires puzzle theory research)

---

### 18. Custom Block Behaviors (Scripting API)
**Designer Programming**: Allow level designers to script custom block behaviors.

**Implementation Needed**:
- [ ] Visual scripting interface OR Lua scripting
- [ ] Expose APIs for block events
- [ ] Sandbox security
- [ ] Example custom blocks

**Estimated Complexity**: Very High (major feature)

---

### 19. Multiplayer Level Sharing
**Community Levels**: Upload/download levels from server.

**Implementation Needed**:
- [ ] Backend server for level storage
- [ ] Upload/download UI
- [ ] Level rating system
- [ ] Search/filter by difficulty, author, etc.
- [ ] Moderation system

**Estimated Complexity**: Very High (requires backend infrastructure)

---

## Implementation Priority Recommendation

### Phase 1: Core UX (Critical)
1. Undo/Redo System
2. User-Facing Error Messages
3. Level Validation Tool

**Timeline**: 1-2 weeks
**Impact**: Transforms editor from frustrating to usable

---

### Phase 2: Content Creation (High)
4. Multiple Level Save Slots
5. Level Thumbnail Generation
6. Example Level Pack

**Timeline**: 1-2 weeks
**Impact**: Enables content library, makes level management professional

---

### Phase 3: Advanced Editing (High)
7. Copy/Paste Block Patterns
8. Visual Route Editor

**Timeline**: 2-3 weeks
**Impact**: Speeds up level creation significantly

---

### Phase 4: Polish (Medium)
9. In-Game Tutorial System
10. Level Overview Map
11. Block Inspector Panel
12. Grid Customization UI

**Timeline**: 2-3 weeks
**Impact**: Professional polish, onboarding for new users

---

### Phase 5: Future (Low)
13-19: Sound, particles, camera controls, accessibility, analysis, custom behaviors, multiplayer

**Timeline**: Ongoing
**Impact**: Nice-to-have features, not blockers

---

## Notes for Implementation

### Architecture Considerations

**Undo/Redo**: Consider using the Command pattern with ICommand interface:
```csharp
public interface IEditorCommand {
    void Execute();
    void Undo();
}
```

**Error Messages**: Create a toast notification system that queues messages:
```csharp
ErrorNotificationUI.Show("Cannot place block: Space is not placeable", 3.0f);
```

**Validation**: Make validation rules extensible so new block types can add their own checks:
```csharp
public interface ILevelValidationRule {
    ValidationResult Validate(LevelData level);
}
```

**Save Slots**: Use level name as filename:
```csharp
// Old: current_level.json
// New: my_cool_level.json, tutorial_level.json, etc.
```

---

## Testing Checklist (Per Feature)

When implementing each feature, verify:
- [ ] Works in all three game modes
- [ ] Doesn't break existing functionality
- [ ] Keyboard shortcuts don't conflict
- [ ] UI scales properly on different resolutions
- [ ] Saves/loads correctly
- [ ] Error cases handled gracefully
- [ ] Console has helpful debug logs
- [ ] Documentation updated

---

## Documentation Updates Needed (Per Feature)

When implementing each feature, update:
- [ ] [HOW-TO-CREATE-A-LEVEL.md](HOW-TO-CREATE-A-LEVEL.md) - Usage instructions
- [ ] [LEVEL_EDITOR.md](LEVEL_EDITOR.md) - Control reference
- [ ] [README.md](README.md) - Feature list
- [ ] [PROJECT.md](PROJECT.md) - Technical architecture
- [ ] Code comments and XML summaries

---

## Current Status Summary

**Total Features Identified**: 19
**Critical**: 3
**High**: 6
**Medium**: 4
**Low**: 6

**Estimated Total Implementation Time**: 10-15 weeks for all features
**Recommended Minimum Viable Product (MVP)**: Phase 1 + Phase 2 (3-4 weeks)

---

## How to Use This Document

1. **For Developers**: Pick features from Phase 1 first, implement in order
2. **For Project Managers**: Use this to plan sprints and set milestones
3. **For Designers**: Understand what's possible now vs. future
4. **For Documentation**: Track which features need docs updates

**Last Updated**: 2026-01-26
**Status**: Draft - awaiting prioritization and scheduling
