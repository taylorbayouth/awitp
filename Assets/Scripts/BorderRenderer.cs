using UnityEngine;

/// <summary>
/// Creates a square border using LineRenderer.
/// Always faces the camera automatically.
/// </summary>
public class BorderRenderer : MonoBehaviour
{
    public Color color = Color.white;
    public float lineWidth = 0.05f;
    public float size = 1f;
    public float depth = 0f;
    public int sortingOrder = 0;

    private LineRenderer lineRenderer;
    private Material material;

    public void Initialize(Color borderColor, float borderSize, float zDepth, int sorting, float width = 0.05f)
    {
        color = borderColor;
        size = borderSize;
        depth = zDepth;
        sortingOrder = sorting;
        lineWidth = width;
        CreateBorder();
    }

    private void CreateBorder()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Create unique material instance
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        material = new Material(shader);
        material.name = gameObject.name + "_Material";

        // ONLY set vertex colors, don't set material.color
        lineRenderer.sharedMaterial = material;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.sortingOrder = sortingOrder;

        // Create square border (4 corners)
        float half = size * 0.5f;
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(-half, -half, depth),
            new Vector3(half, -half, depth),
            new Vector3(half, half, depth),
            new Vector3(-half, half, depth)
        };

        lineRenderer.positionCount = 4;
        lineRenderer.SetPositions(corners);
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
        // Don't set material.color - LineRenderer uses vertex colors only
    }

    public void SetVisible(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
