# Level Editor Quick Reference

Quick cheat sheet for level creation and editing.

---

## Creating a Level

```
1. Right-click in Project → Create → AWITP → Level Definition
2. Set: levelId, levelName, worldId, orderInWorld
3. Configure: gridWidth, gridHeight, cellSize
4. Click "Edit Level Visually" button
```

---

## Keyboard Shortcuts (Play Mode)

| Shortcut | Action |
|----------|--------|
| **↑ ↓ ← →** | Move cursor |
| **Space** or **Enter** | Place block (Design mode) |
| **Tab** | Switch Design ↔ Play mode |
| **Cmd+S** / **Ctrl+S** | **💾 SAVE LEVEL** |
| **Cmd+Shift+S** | Show asset path |
| **C** | Refresh camera |

---

## What Gets Saved (Cmd+S)

✅ **SAVED:**
- Permanent blocks (walk blocks, lock blocks, key blocks)
- **Lem starting positions** (placed in Design mode)
- Placeable spaces (where players can build)
- Camera settings
- Inventory configuration (what blocks players can use)
- Grid dimensions

❌ **NOT SAVED:**
- Blocks placed by **player** in Play mode (their solution)
- Runtime key states
- Player progress

---

## Camera Settings (Defaults)

| Setting | Default | Description |
|---------|---------|-------------|
| **Focal Length** | 756mm | Extreme telephoto lens |
| **Distance Multiplier** | 23.7x | Very far camera = flat perspective |
| **Vertical Offset** | 10.4 | Height above grid |
| **Tilt Angle** | 3.7° | Slight downward angle |
| **FOV** | 1.82° | (auto-calculated) |

**To adjust camera:**
1. Select `CameraSetup` in Hierarchy (Play mode)
2. Adjust sliders in Inspector (live preview!)
3. Press **Cmd+S** to save

---

## Inventory Configuration

**Basic block:**
```
Block Type: Walk
Max Count: 10
```

**Teleporter:**
```
Block Type: Teleporter
Max Count: 6
Flavor ID: A
Is Pair Inventory: ✓
Pair Size: 2
```

**Transporter:**
```
Block Type: Transporter
Max Count: 3
Route Steps:
  - U5  (Up 5)
  - R3  (Right 3)
  - D2  (Down 2)
```

---

## Batch Operations

**Update camera across all levels:**
```
Tools → Update Level Camera Settings
- Choose which settings to update
- Click "Update All Levels"
```

**Update current scene camera only:**
```
Tools → Update Level Camera Settings
- Adjust settings
- Click "Update Scene Camera"
```

---

## Workflow Checklist

**New Level:**
- [ ] Create level asset
- [ ] Set metadata (levelId, levelName, worldId)
- [ ] Configure grid size
- [ ] Click "Edit Level Visually"
- [ ] **Design mode:** Place permanent blocks
  - Walk blocks (starting platforms)
  - Lock blocks (statues = goals)
  - Key blocks (trees with apples)
- [ ] **Design mode:** Place Lems at starting positions
- [ ] Mark placeable spaces (where players can build)
- [ ] Configure inventory (blocks players can use)
- [ ] Adjust camera settings
- [ ] Test in Play mode (Tab) - place blocks to help Lems
- [ ] **Press Cmd+S to save**
- [ ] Exit Play mode

**Edit Existing:**
- [ ] Open asset
- [ ] Click "Edit Level Visually"
- [ ] Make changes
- [ ] **Press Cmd+S**
- [ ] Exit Play mode

---

## Common Issues

**"No LevelDefinition loaded" error:**
→ Use "Edit Level Visually" button to open the level

**Camera settings not saving:**
→ Press **Cmd+S** in Play mode

**Blocks not in inventory:**
→ Check Inventory Configuration, ensure maxCount > 0

**Grid size changed, camera broken:**
→ Camera auto-adjusts, but may need manual tweaking

---

## File Locations

**Level Assets:**
```
Assets/Resources/Levels/LevelDefinitions/{levelId}.asset
```

**Saves go to:**
- Directly into the LevelDefinition asset
- Updates `levelDataJson` field
- Committed to version control

---

## Camera Ranges

| Setting | Min | Default | Max |
|---------|-----|---------|-----|
| Vertical Offset | 0 | 10.4 | 28 |
| Horizontal Offset | -10 | 0 | 10 |
| Tilt Angle | -5° | 3.7° | 20° |
| Pan Angle | -15° | 0° | 15° |
| Focal Length | 100mm | 756mm | 1200mm |
| Distance Multiplier | 5x | 23.7x | 40x |

---

## Block Types

- **Walk**: Standard walkable block
- **Jump**: Allows Lems to jump
- **Goal**: Level completion trigger
- **Key**: Collectible for locks
- **Lock**: Requires key to open
- **Teleporter**: Instant transport (paired by flavor ID)
- **Transporter**: Animated movement along route
- **Start**: Lem spawn point
- **Spawner**: Continuous Lem spawning

---

## Tips

💡 **Save often** - Press Cmd+S after any significant change

💡 **Test in Play mode** - Tab to switch modes, test your level

💡 **Start simple** - Begin with smaller grids (8x8 or 10x10)

💡 **Camera first** - Get camera settings right before detailed design

💡 **Inventory counts** - Give just enough blocks to solve, or extras for variety

💡 **Use defaults** - 756mm focal length + 23.7x distance = perfect flat perspective

---

## Need More Info?

See **LEVEL_SETUP_GUIDE.md** for comprehensive documentation.

---

*Quick Reference v1.0*
