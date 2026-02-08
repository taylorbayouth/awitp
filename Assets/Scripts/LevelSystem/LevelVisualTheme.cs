using UnityEngine;

/// <summary>
/// ScriptableObject that defines visual atmosphere for a level.
///
/// PURPOSE:
/// - Encapsulates lighting, sky, fog, and post-processing settings
/// - Reusable across multiple levels (e.g., "Sunset Theme", "Nighttime Theme")
/// - Git-friendly and designer-friendly
/// - Applied automatically when level loads
///
/// USAGE:
/// 1. Create: Right-click in Project → Create → AWITP → Level Visual Theme
/// 2. Configure lighting, sky, fog settings
/// 3. Assign to LevelDefinition.visualTheme
/// 4. LevelManager applies theme automatically on level load
///
/// EXAMPLES:
/// - "Theme_Sunset" - warm orange lighting, pink sky, light fog
/// - "Theme_Night" - dark blue lighting, starry sky, heavy fog
/// - "Theme_Overcast" - gray lighting, cloudy sky, no fog
/// </summary>
[CreateAssetMenu(fileName = "VisualTheme", menuName = "AWITP/Level Visual Theme")]
public class LevelVisualTheme : ScriptableObject
{
    [Header("Theme Info")]
    [Tooltip("Display name for this theme (e.g., 'Sunset', 'Nighttime')")]
    public string themeName;

    [Tooltip("Optional description")]
    [TextArea(2, 3)]
    public string description;

    [Header("Lighting")]
    [Tooltip("Enable custom directional light settings")]
    public bool overrideDirectionalLight = false;

    [Tooltip("Direction of main light (sun/moon). Use when overrideDirectionalLight is true.")]
    public Vector3 lightDirection = new Vector3(50f, -30f, 0f);

    [Tooltip("Color of main directional light")]
    public Color lightColor = Color.white;

    [Tooltip("Intensity of main directional light")]
    [Range(0f, 3f)]
    public float lightIntensity = 1f;

    [Tooltip("Enable shadows from main light")]
    public bool castShadows = true;

    [Header("Ambient Lighting")]
    [Tooltip("Ambient light source mode")]
    public UnityEngine.Rendering.AmbientMode ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;

    [Tooltip("Ambient light color (used when ambientMode is Flat)")]
    public Color ambientColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("Sky color (used when ambientMode is Trilight)")]
    public Color ambientSkyColor = new Color(0.5f, 0.6f, 0.7f, 1f);

    [Tooltip("Equator color (used when ambientMode is Trilight)")]
    public Color ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("Ground color (used when ambientMode is Trilight)")]
    public Color ambientGroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Tooltip("Ambient light intensity multiplier")]
    [Range(0f, 2f)]
    public float ambientIntensity = 1f;

    [Header("Sky & Background")]
    [Tooltip("Override the sky material")]
    public bool overrideSkybox = false;

    [Tooltip("Custom skybox material (null = use default)")]
    public Material skyboxMaterial;

    [Tooltip("Background color (used when no skybox or camera clear color)")]
    public Color backgroundColor = new Color(0.5f, 0.7f, 0.9f, 1f);

    [Header("Fog")]
    [Tooltip("Enable fog")]
    public bool enableFog = false;

    [Tooltip("Fog color")]
    public Color fogColor = Color.gray;

    [Tooltip("Fog mode (Linear, Exponential, ExponentialSquared)")]
    public FogMode fogMode = FogMode.Exponential;

    [Tooltip("Fog density (for Exponential modes)")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;

    [Tooltip("Fog start distance (for Linear mode)")]
    public float fogStartDistance = 10f;

    [Tooltip("Fog end distance (for Linear mode)")]
    public float fogEndDistance = 50f;

    [Header("Post-Processing (Future)")]
    [Tooltip("Post-processing profile (if using URP/HDRP volumes)")]
    public GameObject postProcessingVolumePrefab;

    [Header("Background Elements")]
    [Tooltip("Optional background prefab to instantiate (e.g., trees, mountains, clouds)")]
    public GameObject backgroundPrefab;

    [Tooltip("Position offset for background prefab")]
    public Vector3 backgroundPosition = Vector3.zero;

    [Tooltip("Rotation for background prefab")]
    public Vector3 backgroundRotation = Vector3.zero;

    [Tooltip("Scale for background prefab")]
    public Vector3 backgroundScale = Vector3.one;

    /// <summary>
    /// Applies this visual theme to the current scene.
    /// Called by LevelManager when level loads.
    /// </summary>
    /// <param name="mainCamera">The main camera to apply background color to</param>
    /// <returns>The instantiated background GameObject (if any)</returns>
    public GameObject Apply(Camera mainCamera = null)
    {
        // Apply directional light settings
        if (overrideDirectionalLight)
        {
            Light directionalLight = GetOrCreateDirectionalLight();
            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(lightDirection);
                directionalLight.color = lightColor;
                directionalLight.intensity = lightIntensity;
                directionalLight.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
            }
        }

        // Apply ambient lighting
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        // Apply skybox
        if (overrideSkybox)
        {
            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }

        // Apply camera background color
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = backgroundColor;
        }

        // Apply fog
        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
        }

        // Instantiate background prefab
        GameObject backgroundInstance = null;
        if (backgroundPrefab != null)
        {
            backgroundInstance = Instantiate(backgroundPrefab);
            backgroundInstance.name = $"Background_{themeName}";
            backgroundInstance.transform.position = backgroundPosition;
            backgroundInstance.transform.rotation = Quaternion.Euler(backgroundRotation);
            backgroundInstance.transform.localScale = backgroundScale;
        }

        // Instantiate post-processing volume (future enhancement)
        if (postProcessingVolumePrefab != null)
        {
            GameObject volumeInstance = Instantiate(postProcessingVolumePrefab);
            volumeInstance.name = $"PostProcessing_{themeName}";
        }

        Debug.Log($"[LevelVisualTheme] Applied theme: {themeName}");
        return backgroundInstance;
    }

    /// <summary>
    /// Finds or creates the main directional light in the scene.
    /// </summary>
    private Light GetOrCreateDirectionalLight()
    {
        // Find existing directional light
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                return light;
            }
        }

        // Create new directional light if none exists
        GameObject lightObj = new GameObject("Directional Light");
        Light directionalLight = lightObj.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        return directionalLight;
    }

    /// <summary>
    /// Resets scene to default lighting (useful for cleanup).
    /// </summary>
    public static void ResetToDefaults()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = false;
        RenderSettings.skybox = null;

        Debug.Log("[LevelVisualTheme] Reset to default lighting");
    }
}
