using UnityEngine;

/// <summary>
/// Creates a square border using LineRenderer with animated glow effects.
/// Used for the grid cursor (red/green/blue states) and other UI highlights.
/// Applies the CursorGlow shader for emission, wiggle animation, and pulse effects.
/// </summary>
public class BorderRenderer : MonoBehaviour
{
    [Header("Border Appearance")]
    [Tooltip("Base color of the border (red = blocked, green = editable, blue = placeable)")]
    public Color color = Color.white;

    [Tooltip("Width of the border line in world units")]
    public float lineWidth = 0.05f;

    [Tooltip("Size of the square border in world units")]
    public float size = 1f;

    [Tooltip("Z-depth offset for rendering order")]
    public float depth = 0f;

    [Tooltip("Sorting order for 2D rendering")]
    public int sortingOrder = 0;

    [Header("Glow & Animation Settings")]
    [Tooltip("Brightness multiplier for glow effect (1 = normal, 3 = very bright)")]
    [Range(1f, 3f)]
    public float emissionStrength = 1.5f;

    [Tooltip("How much the line wiggles perpendicular to its path")]
    [Range(0f, 0.2f)]
    public float wiggleAmount = 0.03f;

    [Tooltip("Speed of the wiggle animation")]
    [Range(0f, 10f)]
    public float wiggleSpeed = 2.0f;

    [Tooltip("Number of wiggles along the border perimeter")]
    [Range(0f, 30f)]
    public float wiggleFrequency = 10.0f;

    private LineRenderer lineRenderer;
    private Material material;  // Instance material for shader properties

    /// <summary>
    /// Sets up the border with the specified parameters.
    /// Call this once after creating the GameObject.
    /// </summary>
    public void Initialize(Color borderColor, float borderSize, float zDepth, int sorting, float width = 0.05f)
    {
        color = borderColor;
        size = borderSize;
        depth = zDepth;
        sortingOrder = sorting;
        lineWidth = width;
        CreateBorder();
    }

    /// <summary>
    /// Creates the LineRenderer and configures it with the CursorGlow shader.
    /// Builds a square border in local space with the specified properties.
    /// </summary>
    private void CreateBorder()
    {
        // Get or create LineRenderer component
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Try to load CursorGlow shader, with fallbacks if not found
        Shader shader = Shader.Find("Custom/CursorGlow");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        // Create a unique material instance for this border
        material = new Material(shader);
        material.name = gameObject.name + "_Material";

        // Configure LineRenderer (colors are set via vertex colors, not material.color)
        lineRenderer.sharedMaterial = material;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false;  // Use local space
        lineRenderer.loop = true;  // Close the square
        lineRenderer.sortingOrder = sortingOrder;

        // Define square corners in local space
        float half = size * 0.5f;
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(-half, -half, depth),  // Bottom-left
            new Vector3(half, -half, depth),   // Bottom-right
            new Vector3(half, half, depth),    // Top-right
            new Vector3(-half, half, depth)    // Top-left
        };

        lineRenderer.positionCount = 4;
        lineRenderer.SetPositions(corners);

        // Apply animation shader properties
        UpdateShaderProperties();
    }

    /// <summary>
    /// Updates the CursorGlow shader properties with current values.
    /// Safe to call even if the shader doesn't have these properties.
    /// </summary>
    private void UpdateShaderProperties()
    {
        if (material == null) return;

        // Only set properties if they exist in the shader
        if (material.HasProperty("_EmissionStrength"))
            material.SetFloat("_EmissionStrength", emissionStrength);
        if (material.HasProperty("_WiggleAmount"))
            material.SetFloat("_WiggleAmount", wiggleAmount);
        if (material.HasProperty("_WiggleSpeed"))
            material.SetFloat("_WiggleSpeed", wiggleSpeed);
        if (material.HasProperty("_WiggleFrequency"))
            material.SetFloat("_WiggleFrequency", wiggleFrequency);
    }

    /// <summary>
    /// Changes the border color (e.g., red/green/blue for cursor states).
    /// Updates vertex colors, not material color.
    /// </summary>
    public void SetColor(Color newColor)
    {
        color = newColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }

    /// <summary>
    /// Shows or hides the border by enabling/disabling the LineRenderer.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }

    /// <summary>
    /// Cleanup: destroy the material instance to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: update shader properties when values change in the Inspector.
    /// Allows real-time preview of glow and animation settings.
    /// </summary>
    private void OnValidate()
    {
        UpdateShaderProperties();
    }
#endif
}
