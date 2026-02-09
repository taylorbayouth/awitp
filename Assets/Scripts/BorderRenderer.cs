using UnityEngine;

/// <summary>
/// Creates a square border using LineRenderer with static color.
/// Used for the grid cursor (red/green/blue states) and other UI highlights.
/// Uses a non-animated shader so cursor/border visuals remain stable.
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
    /// Creates the LineRenderer and configures it with a static, non-animated shader.
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

        // Use non-animated shader for static cursor/grid borders.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

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

}
