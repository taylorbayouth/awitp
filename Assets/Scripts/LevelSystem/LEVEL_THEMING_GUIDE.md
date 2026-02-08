# Level Theming System Guide

## Overview

The **Level Theming System** provides a modular, Git-friendly way to add visual and audio variety to your levels. Themes are optional ScriptableObject assets that can be reused across multiple levels.

## Architecture

```
LevelDefinition (ScriptableObject)
├── visualTheme (optional) → LevelVisualTheme
└── audioTheme (optional)  → LevelAudioTheme
```

### Why ScriptableObjects?

- **Git-Friendly**: Text-based YAML format, perfect for version control
- **Reusable**: One theme can be shared across many levels
- **Designer-Friendly**: No code required, all settings in Inspector
- **Modular**: Visual and audio are separate, mix and match
- **Optional**: Levels work perfectly fine without any themes

## LevelVisualTheme

Controls the **look and atmosphere** of a level.

### Features

| Feature | Description |
|---------|-------------|
| **Directional Light** | Sun/moon direction, color, intensity, shadows |
| **Ambient Lighting** | Skybox, flat, or trilight ambient modes |
| **Skybox** | Custom sky material |
| **Background Color** | Camera clear color |
| **Fog** | Linear, Exponential, or ExponentialSquared fog |
| **Background Prefab** | Instantiate 3D scenery (mountains, trees, clouds) |
| **Post-Processing** | Volume prefab (future enhancement) |

### Creating a Visual Theme

1. **Right-click** in Project window
2. **Create → AWITP → Level Visual Theme**
3. **Name it** descriptively (e.g., `Theme_Sunset`, `Theme_Nighttime`)
4. **Configure settings** in Inspector
5. **Assign to level**: Drag to LevelDefinition.visualTheme field

### Example Themes

**Sunset Theme:**
```
Light Direction: (50, -30, 0)
Light Color: Orange (#FF8844)
Light Intensity: 1.2
Fog: Enabled, Pink, Exponential, Density 0.01
Background Color: Pink-Orange gradient
```

**Night Theme:**
```
Light Direction: (80, -45, 0)
Light Color: Dark Blue (#334466)
Light Intensity: 0.6
Ambient Mode: Flat
Ambient Color: Dark Blue
Fog: Enabled, Dark Blue-Black
Background Color: Black (#000000)
```

**Overcast Day:**
```
Light Direction: (0, -90, 0) // directly overhead
Light Color: Gray (#CCCCCC)
Light Intensity: 0.8
Ambient Mode: Flat
Ambient Color: Light Gray
Fog: None
Background Color: Gray (#888888)
```

## LevelAudioTheme

Controls the **sound and music** of a level.

### Features

| Feature | Description |
|---------|-------------|
| **Music Tracks** | Override builder/play mode tracks |
| **Music Volume** | Per-level volume multiplier |
| **Ambient Loop** | Continuous background sound (wind, water, etc.) |
| **Random Sounds** | Periodic one-shot sounds (birds, thunder, etc.) |
| **Sound Effects** | Override block placement/removal sounds (future) |

### Creating an Audio Theme

1. **Right-click** in Project window
2. **Create → AWITP → Level Audio Theme**
3. **Name it** descriptively (e.g., `Theme_Peaceful`, `Theme_Intense`)
4. **Assign audio clips** from your project
5. **Assign to level**: Drag to LevelDefinition.audioTheme field

### Integration with GameModeManager

The audio theme system integrates seamlessly with the existing **synchronized dual-track music system**:

- Both builder and play tracks can be overridden per level
- Crossfading behavior is preserved
- Themes add an ambient sound layer on top
- Original tracks restore when level unloads

### Example Themes

**Peaceful Theme:**
```
Builder Track: SoftPiano.mp3
Play Track: SoftPiano_Upbeat.mp3
Music Volume: 0.8
Ambient Loop: BirdChirps.wav (volume 0.3)
Random Sounds: [WindGust.wav, DistantBird.wav]
Interval: 5-15 seconds
```

**Industrial Theme:**
```
Builder Track: ElectronicBeats.mp3
Play Track: ElectronicBeats_Fast.mp3
Music Volume: 1.0
Ambient Loop: MachineHum.wav (volume 0.4)
Random Sounds: [MetalClink.wav, SteamHiss.wav]
Interval: 3-10 seconds
```

**Nature Theme:**
```
Builder Track: Strings.mp3
Play Track: Strings_Orchestral.mp3
Music Volume: 0.9
Ambient Loop: ForestAmbience.wav (volume 0.5, 3D)
Random Sounds: [OwlHoot.wav, RiverRush.wav]
Interval: 10-20 seconds
```

## Workflow for Level Designers

### Quick Start (No Themes)

1. Create LevelDefinition
2. Configure grid, inventory, metadata
3. Edit level visually (blocks, lems, etc.)
4. **Done!** Level uses default lighting/music

### Adding Visual Variety

1. Create or select existing LevelVisualTheme
2. Assign to LevelDefinition.visualTheme
3. Test level in play mode
4. Iterate on theme settings

### Adding Audio Variety

1. Create or select existing LevelAudioTheme
2. Assign to LevelDefinition.audioTheme
3. Test level in play mode
4. Iterate on music selection and ambient sounds

### Reusing Themes

Themes are **designed for reuse**! Create a library:

```
Assets/
└── Data/
    ├── Themes/
    │   ├── Visual/
    │   │   ├── Theme_Day.asset
    │   │   ├── Theme_Night.asset
    │   │   ├── Theme_Sunset.asset
    │   │   ├── Theme_Overcast.asset
    │   │   └── Theme_Industrial.asset
    │   └── Audio/
    │       ├── Theme_Peaceful.asset
    │       ├── Theme_Intense.asset
    │       ├── Theme_Nature.asset
    │       └── Theme_Electronic.asset
    └── Levels/
        └── LevelDefinitions/
            ├── Level_01.asset (uses Theme_Day + Theme_Peaceful)
            ├── Level_02.asset (uses Theme_Day + Theme_Peaceful)  ← Same themes!
            ├── Level_03.asset (uses Theme_Sunset + Theme_Intense)
            └── Level_04.asset (uses Theme_Night + Theme_Nature)
```

## Git Best Practices

### Commit Structure

Themes and levels should be committed separately when possible:

```bash
# Good commit structure
git add Assets/Data/Themes/Visual/Theme_Sunset.asset
git commit -m "Add sunset visual theme"

git add Assets/Data/Levels/LevelDefinitions/Level_05.asset
git commit -m "Add Level 5 with sunset theme"
```

### Merge Conflicts

- **Rare**: ScriptableObjects are text-based and merge cleanly
- **If they occur**: Unity's YAML format is human-readable
- **Strategy**: Use `git diff` to see changes, resolve manually

### Asset References

Themes reference prefabs, materials, and audio clips by GUID, which is stable across:
- File moves
- Renames
- Branch switches

## Performance Considerations

### Visual Themes

- **Minimal overhead**: Applied once at level load
- **Background prefabs**: Ensure they're optimized (low poly, LOD)
- **Fog**: Negligible performance impact on modern hardware
- **Post-processing**: Can be expensive, use wisely

### Audio Themes

- **Ambient loops**: One AudioSource, trivial overhead
- **Random sounds**: One AudioSource, plays occasionally
- **Music overrides**: Same as default system, no extra cost

### Best Practices

1. **Share themes** across levels to reduce asset duplication
2. **Optimize background prefabs** (use LOD groups)
3. **Use compressed audio** (Vorbis for long tracks, ADPCM for short)
4. **Test on target hardware** (mobile, low-end PCs)

## Advanced Techniques

### Layered Backgrounds

Create prefabs with multiple elements:

```
Background_Mountains (Prefab)
├── SkyGradient (Quad with gradient material)
├── DistantMountains (Low-poly mesh)
├── MidMountains (Medium-detail mesh)
└── ForegroundTrees (High-detail, closer to camera)
```

Assign this prefab to visualTheme.backgroundPrefab.

### Dynamic Lighting

Create animated light components:

```
AnimatedSunlight (Prefab)
├── Directional Light
└── SunRotation (Script)  // Rotates light over time
```

Use visualTheme.overrideDirectionalLight = false and instantiate this in backgroundPrefab.

### Parallax Scrolling

For 2D/2.5D levels, create scrolling background layers:

```
ParallaxBackground (Prefab)
├── Layer1_Sky (Scrolls slow)
├── Layer2_Clouds (Scrolls medium)
└── Layer3_Trees (Scrolls fast)
```

### Weather Effects

Instantiate particle systems in backgroundPrefab:

```
RainWeather (Prefab)
├── RainParticles
├── Splashes
└── ThunderLightning
```

### Audio Zones (Future)

For levels with distinct regions, use multiple AudioThemeControllers:

```
Level_10 (Large level)
├── Zone1_Forest (AudioTheme_Nature)
├── Zone2_Cave (AudioTheme_Cave)
└── Zone3_Waterfall (AudioTheme_Water)
```

## Troubleshooting

### Visual Theme Not Applying

1. Check LevelDefinition.visualTheme is assigned
2. Check Console for errors
3. Verify Camera.main exists
4. Ensure level loaded successfully

### Audio Theme Not Playing

1. Check LevelDefinition.audioTheme is assigned
2. Verify audio clips are assigned in theme
3. Check GameModeManager exists in scene
4. Check volume settings (not muted)

### Background Prefab Not Visible

1. Check prefab is assigned
2. Verify position/rotation/scale in theme
3. Check prefab layers match camera culling mask
4. Ensure materials are assigned to meshes

### Music Not Crossfading

1. Audio theme integrates with GameModeManager
2. Ensure GameModeManager.musicFadeDuration > 0
3. Check both tracks are assigned
4. Verify mode switching is working

## Future Enhancements

### Planned Features

- **Post-Processing Volumes**: URP/HDRP volume support
- **Weather System**: Scriptable weather patterns
- **Time of Day**: Animated lighting over level duration
- **Audio Zones**: Position-based audio regions
- **SFX Overrides**: Per-level block sounds
- **Particles**: Theme-based particle effects

### Community Requests

Have ideas? Open an issue or PR!

## Examples Gallery

### Minimal Theme (Tutorial Levels)

```yaml
visualTheme: null
audioTheme: null
```

Simple, distraction-free. Uses defaults.

### Sunset Beach Theme

```yaml
visualTheme: Theme_Sunset
  Light: Orange, 1.2 intensity
  Fog: Pink, exponential
  Background: Beach_Scene.prefab

audioTheme: Theme_Peaceful
  Music: SoftPiano.mp3
  Ambient: Ocean_Waves.wav
  Random: [Seagull.wav]
```

Relaxing, warm, coastal atmosphere.

### Industrial Factory Theme

```yaml
visualTheme: Theme_Industrial
  Light: Cold white, harsh shadows
  Fog: Gray, dense
  Background: Factory_Pipes.prefab

audioTheme: Theme_Industrial
  Music: ElectronicBeats.mp3
  Ambient: MachineHum.wav
  Random: [MetalClank.wav, SteamHiss.wav]
```

Tense, mechanical, challenging atmosphere.

### Night Sky Theme

```yaml
visualTheme: Theme_Night
  Light: Dark blue, low intensity
  Ambient: Flat, dark blue
  Skybox: Starfield.mat
  Background: MoonAndStars.prefab

audioTheme: Theme_Nature
  Music: SlowStrings.mp3
  Ambient: NightCrickets.wav
  Random: [OwlHoot.wav, DistantWolf.wav]
```

Serene, mysterious, contemplative atmosphere.

---

**Happy Theming!** 🎨🎵
