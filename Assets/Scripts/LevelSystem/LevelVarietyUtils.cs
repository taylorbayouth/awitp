using UnityEngine;

/// <summary>
/// Utility class for adding variety and visual interest to levels.
/// Provides helper methods for common level design patterns.
///
/// PURPOSE:
/// - Make it easy to add visual variety without deep Unity knowledge
/// - Provide common patterns that work well in puzzle games
/// - Reduce repetitive setup code
///
/// USAGE:
/// Call from your editor scripts or runtime level generation code.
/// </summary>
public static class LevelVarietyUtils
{
    #region Color Variety

    /// <summary>
    /// Generates a harmonious color palette for level theming.
    /// Uses color theory to create pleasing combinations.
    /// </summary>
    /// <param name="baseHue">Starting hue (0-1)</param>
    /// <param name="count">Number of colors to generate</param>
    /// <returns>Array of colors in the palette</returns>
    public static Color[] GenerateColorPalette(float baseHue, int count = 5)
    {
        Color[] palette = new Color[count];

        for (int i = 0; i < count; i++)
        {
            // Analogous color scheme (nearby hues)
            float hue = (baseHue + (i * 0.1f)) % 1f;
            float saturation = 0.6f + (i * 0.08f);
            float value = 0.7f + (i * 0.06f);

            palette[i] = Color.HSVToRGB(hue, saturation, value);
        }

        return palette;
    }

    /// <summary>
    /// Creates a complementary color pair (opposite on color wheel).
    /// Great for accent colors and visual contrast.
    /// </summary>
    public static (Color primary, Color complement) GetComplementaryColors(float baseHue)
    {
        Color primary = Color.HSVToRGB(baseHue, 0.7f, 0.8f);
        Color complement = Color.HSVToRGB((baseHue + 0.5f) % 1f, 0.7f, 0.8f);

        return (primary, complement);
    }

    /// <summary>
    /// Converts time of day (0-24 hours) to appropriate lighting color.
    /// Useful for time-based level themes.
    /// </summary>
    public static Color GetLightColorForTimeOfDay(float hour)
    {
        // Clamp to 0-24 range
        hour = Mathf.Clamp(hour, 0f, 24f);

        if (hour < 6f) // Night (0-6 AM)
        {
            return new Color(0.3f, 0.4f, 0.6f); // Blue moonlight
        }
        else if (hour < 8f) // Dawn (6-8 AM)
        {
            float t = (hour - 6f) / 2f;
            return Color.Lerp(
                new Color(0.3f, 0.4f, 0.6f), // Night
                new Color(1f, 0.8f, 0.6f),   // Dawn
                t
            );
        }
        else if (hour < 18f) // Day (8 AM - 6 PM)
        {
            return new Color(1f, 0.96f, 0.84f); // Warm daylight
        }
        else if (hour < 20f) // Sunset (6-8 PM)
        {
            float t = (hour - 18f) / 2f;
            return Color.Lerp(
                new Color(1f, 0.96f, 0.84f), // Day
                new Color(1f, 0.5f, 0.3f),   // Sunset
                t
            );
        }
        else // Evening/Night (8 PM - midnight)
        {
            float t = (hour - 20f) / 4f;
            return Color.Lerp(
                new Color(1f, 0.5f, 0.3f),   // Sunset
                new Color(0.3f, 0.4f, 0.6f), // Night
                t
            );
        }
    }

    #endregion

    #region Lighting Variety

    /// <summary>
    /// Calculates appropriate light direction for a given time of day.
    /// Creates realistic sun/moon positions.
    /// </summary>
    public static Vector3 GetLightDirectionForTimeOfDay(float hour)
    {
        // Clamp to 0-24 range
        hour = Mathf.Clamp(hour, 0f, 24f);

        // Sunrise at 6 AM (90° from horizon), sunset at 6 PM (90° from horizon)
        // Noon at 12 PM (directly overhead)

        float normalizedTime = (hour - 6f) / 12f; // Map 6 AM-6 PM to 0-1
        float angle = Mathf.Lerp(90f, -90f, normalizedTime); // 90° to -90° (right to left)
        float elevation = Mathf.Sin(normalizedTime * Mathf.PI) * -90f; // Arc across sky

        return new Vector3(angle, elevation, 0f);
    }

    /// <summary>
    /// Generates a random but aesthetically pleasing light direction.
    /// Ensures light comes from above (not below horizon).
    /// </summary>
    public static Vector3 GetRandomLightDirection()
    {
        float horizontalAngle = Random.Range(-60f, 60f);
        float verticalAngle = Random.Range(-60f, -15f); // Always from above

        return new Vector3(horizontalAngle, verticalAngle, 0f);
    }

    #endregion

    #region Fog Variety

    /// <summary>
    /// Calculates appropriate fog density for a given visibility distance.
    /// Makes it easy to specify "I want fog that's thick at 50 units".
    /// </summary>
    public static float CalculateFogDensity(float visibilityDistance, FogMode mode = FogMode.Exponential)
    {
        if (mode == FogMode.Exponential)
        {
            // Formula: density = -ln(0.01) / distance
            // At this density, object at 'distance' will be 99% obscured
            return -Mathf.Log(0.01f) / visibilityDistance;
        }
        else if (mode == FogMode.ExponentialSquared)
        {
            // Denser fog, use sqrt for more gradual falloff
            return Mathf.Sqrt(-Mathf.Log(0.01f)) / visibilityDistance;
        }

        return 0.01f; // Default fallback
    }

    /// <summary>
    /// Creates fog settings that match the lighting color.
    /// Produces a harmonious, unified atmosphere.
    /// </summary>
    public static (Color fogColor, float fogDensity) GetMatchingFog(Color lightColor, float thickness = 0.015f)
    {
        // Fog color slightly darker and more saturated than light
        Color.RGBToHSV(lightColor, out float h, out float s, out float v);

        s = Mathf.Min(s * 1.2f, 1f); // More saturated
        v *= 0.8f; // Darker

        Color fogColor = Color.HSVToRGB(h, s, v);

        return (fogColor, thickness);
    }

    #endregion

    #region Sky & Background Variety

    /// <summary>
    /// Generates a gradient color pair for sky rendering.
    /// Returns (top color, horizon color) for vertical gradients.
    /// </summary>
    public static (Color top, Color horizon) GetSkyGradient(float timeOfDay = 12f)
    {
        if (timeOfDay >= 6f && timeOfDay <= 18f) // Daytime
        {
            Color top = new Color(0.3f, 0.5f, 0.9f); // Deep blue
            Color horizon = new Color(0.6f, 0.75f, 0.95f); // Light blue
            return (top, horizon);
        }
        else if (timeOfDay > 18f && timeOfDay < 20f) // Sunset
        {
            Color top = new Color(0.4f, 0.3f, 0.6f); // Purple
            Color horizon = new Color(1f, 0.5f, 0.3f); // Orange
            return (top, horizon);
        }
        else // Night
        {
            Color top = new Color(0.05f, 0.05f, 0.15f); // Dark blue-black
            Color horizon = new Color(0.1f, 0.1f, 0.25f); // Slightly lighter
            return (top, horizon);
        }
    }

    #endregion

    #region Level Naming

    /// <summary>
    /// Generates a themed level name based on difficulty and world theme.
    /// Useful for placeholder names during development.
    /// </summary>
    public static string GenerateLevelName(int levelNumber, string themeKeyword = "")
    {
        string[] adjectives = { "Simple", "Tricky", "Clever", "Cunning", "Mind-Bending", "Devious" };
        string[] nouns = { "Path", "Bridge", "Puzzle", "Challenge", "Maze", "Journey" };

        int adjIndex = Mathf.Clamp(levelNumber / 5, 0, adjectives.Length - 1);
        int nounIndex = levelNumber % nouns.Length;

        string name = $"{adjectives[adjIndex]} {nouns[nounIndex]}";

        if (!string.IsNullOrEmpty(themeKeyword))
        {
            name = $"{themeKeyword} {name}";
        }

        return name;
    }

    /// <summary>
    /// Suggests a level ID based on world and order.
    /// Follows consistent naming convention.
    /// </summary>
    public static string GenerateLevelId(string worldId, int orderInWorld)
    {
        return $"{worldId}_level_{orderInWorld:D2}";
    }

    #endregion

    #region Atmospheric Presets

    /// <summary>
    /// Contains all settings for a complete atmospheric preset.
    /// Can be applied to a LevelVisualTheme in one call.
    /// </summary>
    public struct AtmospherePreset
    {
        public string name;
        public Color lightColor;
        public Vector3 lightDirection;
        public float lightIntensity;
        public Color ambientColor;
        public Color backgroundColor;
        public bool enableFog;
        public Color fogColor;
        public float fogDensity;
    }

    /// <summary>
    /// Gets a complete atmosphere preset by name.
    /// Easier than calling multiple utility methods.
    /// </summary>
    public static AtmospherePreset GetAtmospherePreset(string presetName)
    {
        return presetName.ToLower() switch
        {
            "sunny" => new AtmospherePreset
            {
                name = "Sunny",
                lightColor = new Color(1f, 0.96f, 0.84f),
                lightDirection = new Vector3(50f, -30f, 0f),
                lightIntensity = 1.3f,
                ambientColor = new Color(0.6f, 0.7f, 0.8f),
                backgroundColor = new Color(0.5f, 0.7f, 0.9f),
                enableFog = false
            },
            "moody" => new AtmospherePreset
            {
                name = "Moody",
                lightColor = new Color(0.7f, 0.75f, 0.8f),
                lightDirection = new Vector3(0f, -90f, 0f),
                lightIntensity = 0.8f,
                ambientColor = new Color(0.4f, 0.4f, 0.45f),
                backgroundColor = new Color(0.6f, 0.6f, 0.65f),
                enableFog = true,
                fogColor = new Color(0.5f, 0.5f, 0.55f),
                fogDensity = 0.02f
            },
            "dramatic" => new AtmospherePreset
            {
                name = "Dramatic",
                lightColor = new Color(1f, 0.5f, 0.2f),
                lightDirection = new Vector3(85f, -10f, 0f),
                lightIntensity = 1.8f,
                ambientColor = new Color(0.3f, 0.15f, 0.1f),
                backgroundColor = new Color(0.8f, 0.3f, 0.2f),
                enableFog = true,
                fogColor = new Color(0.9f, 0.5f, 0.3f),
                fogDensity = 0.025f
            },
            "mysterious" => new AtmospherePreset
            {
                name = "Mysterious",
                lightColor = new Color(0.4f, 0.5f, 0.7f),
                lightDirection = new Vector3(90f, -60f, 0f),
                lightIntensity = 0.5f,
                ambientColor = new Color(0.15f, 0.15f, 0.25f),
                backgroundColor = new Color(0.1f, 0.1f, 0.2f),
                enableFog = true,
                fogColor = new Color(0.2f, 0.2f, 0.3f),
                fogDensity = 0.03f
            },
            _ => GetAtmospherePreset("sunny") // Default
        };
    }

    /// <summary>
    /// Applies an atmosphere preset to a LevelVisualTheme.
    /// One-line convenience method for quick setup.
    /// </summary>
    public static void ApplyAtmospherePreset(LevelVisualTheme theme, AtmospherePreset preset)
    {
        theme.overrideDirectionalLight = true;
        theme.lightDirection = preset.lightDirection;
        theme.lightColor = preset.lightColor;
        theme.lightIntensity = preset.lightIntensity;

        theme.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        theme.ambientColor = preset.ambientColor;

        theme.backgroundColor = preset.backgroundColor;

        theme.enableFog = preset.enableFog;
        if (preset.enableFog)
        {
            theme.fogColor = preset.fogColor;
            theme.fogMode = FogMode.Exponential;
            theme.fogDensity = preset.fogDensity;
        }

        Debug.Log($"[LevelVarietyUtils] Applied atmosphere preset: {preset.name}");
    }

    #endregion
}
