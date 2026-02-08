using UnityEngine;

/// <summary>
/// Static utility class providing quick access to common theme presets.
/// Useful for rapid prototyping and testing different atmospheres.
///
/// USAGE:
/// - Call methods from editor scripts or runtime code
/// - Returns configured ScriptableObject instances (not saved to disk)
/// - Great for A/B testing different atmospheres
///
/// EXAMPLE:
/// ```csharp
/// LevelVisualTheme sunset = ThemePresets.CreateSunsetTheme();
/// sunset.Apply(Camera.main);
/// ```
/// </summary>
public static class ThemePresets
{
    #region Visual Theme Presets

    /// <summary>
    /// Creates a bright, cheerful daytime theme.
    /// Perfect for tutorial and early levels.
    /// </summary>
    public static LevelVisualTheme CreateDayTheme()
    {
        LevelVisualTheme theme = ScriptableObject.CreateInstance<LevelVisualTheme>();
        theme.name = "Day";
        theme.themeName = "Day";
        theme.description = "Bright, cheerful daytime atmosphere";

        theme.overrideDirectionalLight = true;
        theme.lightDirection = new Vector3(50f, -30f, 0f);
        theme.lightColor = new Color(1f, 0.96f, 0.84f); // Warm white
        theme.lightIntensity = 1.2f;
        theme.castShadows = true;

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        theme.ambientIntensity = 1f;

        theme.backgroundColor = new Color(0.5f, 0.7f, 0.9f); // Sky blue
        theme.enableFog = false;

        return theme;
    }

    /// <summary>
    /// Creates a warm, orange sunset theme.
    /// Great for dramatic or romantic levels.
    /// </summary>
    public static LevelVisualTheme CreateSunsetTheme()
    {
        LevelVisualTheme theme = ScriptableObject.CreateInstance<LevelVisualTheme>();
        theme.name = "Sunset";
        theme.themeName = "Sunset";
        theme.description = "Warm, orange sunset atmosphere";

        theme.overrideDirectionalLight = true;
        theme.lightDirection = new Vector3(80f, -15f, 0f); // Low angle
        theme.lightColor = new Color(1f, 0.6f, 0.3f); // Orange
        theme.lightIntensity = 1.5f;
        theme.castShadows = true;

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        theme.ambientColor = new Color(0.8f, 0.5f, 0.3f); // Orange ambient
        theme.ambientIntensity = 0.8f;

        theme.backgroundColor = new Color(1f, 0.5f, 0.3f); // Orange-pink
        theme.enableFog = true;
        theme.fogColor = new Color(0.9f, 0.6f, 0.4f);
        theme.fogMode = FogMode.Exponential;
        theme.fogDensity = 0.015f;

        return theme;
    }

    /// <summary>
    /// Creates a mysterious nighttime theme.
    /// Ideal for challenge levels or spooky atmospheres.
    /// </summary>
    public static LevelVisualTheme CreateNightTheme()
    {
        LevelVisualTheme theme = ScriptableObject.CreateInstance<LevelVisualTheme>();
        theme.name = "Night";
        theme.themeName = "Night";
        theme.description = "Dark, mysterious nighttime atmosphere";

        theme.overrideDirectionalLight = true;
        theme.lightDirection = new Vector3(90f, -45f, 0f); // Moonlight
        theme.lightColor = new Color(0.5f, 0.6f, 0.8f); // Blue moonlight
        theme.lightIntensity = 0.4f;
        theme.castShadows = false; // Soft night lighting

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        theme.ambientColor = new Color(0.1f, 0.1f, 0.2f); // Dark blue
        theme.ambientIntensity = 0.5f;

        theme.backgroundColor = new Color(0.05f, 0.05f, 0.1f); // Almost black
        theme.enableFog = true;
        theme.fogColor = new Color(0.1f, 0.1f, 0.15f);
        theme.fogMode = FogMode.Exponential;
        theme.fogDensity = 0.02f;

        return theme;
    }

    /// <summary>
    /// Creates an overcast, gray theme.
    /// Good for somber or contemplative levels.
    /// </summary>
    public static LevelVisualTheme CreateOvercastTheme()
    {
        LevelVisualTheme theme = ScriptableObject.CreateInstance<LevelVisualTheme>();
        theme.name = "Overcast";
        theme.themeName = "Overcast";
        theme.description = "Gray, overcast atmosphere";

        theme.overrideDirectionalLight = true;
        theme.lightDirection = new Vector3(0f, -90f, 0f); // Overhead diffuse
        theme.lightColor = new Color(0.8f, 0.8f, 0.8f); // Gray
        theme.lightIntensity = 0.9f;
        theme.castShadows = false; // Diffuse light, no hard shadows

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        theme.ambientColor = new Color(0.6f, 0.6f, 0.6f); // Medium gray
        theme.ambientIntensity = 1f;

        theme.backgroundColor = new Color(0.7f, 0.7f, 0.7f); // Light gray
        theme.enableFog = false;

        return theme;
    }

    /// <summary>
    /// Creates an industrial, metallic theme.
    /// Perfect for factory or mechanical levels.
    /// </summary>
    public static LevelVisualTheme CreateIndustrialTheme()
    {
        LevelVisualTheme theme = ScriptableObject.CreateInstance<LevelVisualTheme>();
        theme.name = "Industrial";
        theme.themeName = "Industrial";
        theme.description = "Cold, industrial atmosphere";

        theme.overrideDirectionalLight = true;
        theme.lightDirection = new Vector3(45f, -60f, 0f);
        theme.lightColor = new Color(0.9f, 0.95f, 1f); // Cold white
        theme.lightIntensity = 1.1f;
        theme.castShadows = true;

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        theme.ambientColor = new Color(0.3f, 0.35f, 0.4f); // Steel blue
        theme.ambientIntensity = 0.7f;

        theme.backgroundColor = new Color(0.4f, 0.45f, 0.5f); // Gray-blue
        theme.enableFog = true;
        theme.fogColor = new Color(0.5f, 0.5f, 0.5f);
        theme.fogMode = FogMode.Linear;
        theme.fogStartDistance = 20f;
        theme.fogEndDistance = 80f;

        return theme;
    }

    #endregion

    #region Audio Theme Presets

    /// <summary>
    /// Creates a peaceful, relaxing audio theme.
    /// Perfect for calm, meditative levels.
    /// </summary>
    public static LevelAudioTheme CreatePeacefulTheme()
    {
        LevelAudioTheme theme = ScriptableObject.CreateInstance<LevelAudioTheme>();
        theme.name = "Peaceful";
        theme.themeName = "Peaceful";
        theme.description = "Calm, relaxing audio atmosphere";

        theme.musicVolume = 0.8f;
        theme.musicFadeInDuration = 3f;
        theme.ambientVolume = 0.3f;
        theme.randomSoundMinInterval = 8f;
        theme.randomSoundMaxInterval = 20f;
        theme.randomSoundVolume = 0.4f;

        // Note: Audio clips must be assigned by user
        Debug.Log("[ThemePresets] Created Peaceful audio theme (clips must be assigned manually)");

        return theme;
    }

    /// <summary>
    /// Creates an intense, energetic audio theme.
    /// Great for action-packed or challenging levels.
    /// </summary>
    public static LevelAudioTheme CreateIntenseTheme()
    {
        LevelAudioTheme theme = ScriptableObject.CreateInstance<LevelAudioTheme>();
        theme.name = "Intense";
        theme.themeName = "Intense";
        theme.description = "Energetic, intense audio atmosphere";

        theme.musicVolume = 1f;
        theme.musicFadeInDuration = 1f;
        theme.ambientVolume = 0.5f;
        theme.randomSoundMinInterval = 3f;
        theme.randomSoundMaxInterval = 8f;
        theme.randomSoundVolume = 0.6f;

        // Note: Audio clips must be assigned by user
        Debug.Log("[ThemePresets] Created Intense audio theme (clips must be assigned manually)");

        return theme;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Creates a theme with random variations for testing.
    /// Useful for rapid experimentation.
    /// </summary>
    public static LevelVisualTheme CreateRandomTheme()
    {
        LevelVisualTheme theme = ScriptableObject.CreateInstance<LevelVisualTheme>();
        theme.name = "Random";
        theme.themeName = "Random Test";
        theme.description = "Randomly generated theme for testing";

        theme.overrideDirectionalLight = true;
        theme.lightDirection = new Vector3(
            Random.Range(0f, 90f),
            Random.Range(-90f, -10f),
            0f
        );
        theme.lightColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.7f, 1f);
        theme.lightIntensity = Random.Range(0.5f, 1.5f);
        theme.castShadows = Random.value > 0.5f;

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        theme.ambientColor = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.5f, 0.9f);
        theme.ambientIntensity = Random.Range(0.5f, 1.2f);

        theme.backgroundColor = Random.ColorHSV(0f, 1f, 0.4f, 0.8f, 0.6f, 1f);
        theme.enableFog = Random.value > 0.5f;
        if (theme.enableFog)
        {
            theme.fogColor = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.5f, 0.9f);
            theme.fogMode = Random.value > 0.5f ? FogMode.Exponential : FogMode.Linear;
            theme.fogDensity = Random.Range(0.005f, 0.03f);
        }

        Debug.Log("[ThemePresets] Created random theme for experimentation");

        return theme;
    }

    /// <summary>
    /// Gets all preset theme names.
    /// Useful for dropdowns or selection UIs.
    /// </summary>
    public static string[] GetVisualThemePresetNames()
    {
        return new string[]
        {
            "Day",
            "Sunset",
            "Night",
            "Overcast",
            "Industrial"
        };
    }

    /// <summary>
    /// Gets a visual theme preset by name.
    /// </summary>
    public static LevelVisualTheme GetVisualThemePreset(string presetName)
    {
        return presetName switch
        {
            "Day" => CreateDayTheme(),
            "Sunset" => CreateSunsetTheme(),
            "Night" => CreateNightTheme(),
            "Overcast" => CreateOvercastTheme(),
            "Industrial" => CreateIndustrialTheme(),
            _ => CreateDayTheme() // Default
        };
    }

    #endregion
}
