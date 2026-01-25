using UnityEngine;

/// <summary>
/// Visualizes the grid with horizontal and vertical lines.
/// Lines are rendered as double-sided meshes to ensure visibility from all angles.
/// </summary>
[RequireComponent(typeof(GridManager))]
public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Line Settings")]
    public Color gridColor = BlockColors.GridLine;
    public float lineWidth = RenderingConstants.GRID_LINE_WIDTH;
    public Material lineMaterial;
    public bool showGrid = true;

    private GridManager gridManager;
    private GameObject gridLinesParent;

    private void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    private void Start()
    {
        CreateGridLines();
    }

    public void CreateGridLines()
    {
        if (gridLinesParent != null) Destroy(gridLinesParent);
        if (!showGrid) return;

        gridLinesParent = new GameObject("GridLines");
        gridLinesParent.transform.parent = transform;

        if (lineMaterial == null)
        {
            // Use Sprites/Default for reliable 2D rendering
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.color = gridColor;
            lineMaterial.renderQueue = RenderingConstants.GRID_RENDER_QUEUE;
        }

        // Horizontal lines
        for (int y = 0; y <= gridManager.gridHeight; y++)
        {
            Vector3 start = gridManager.gridOrigin + new Vector3(0, RenderingConstants.GRID_HEIGHT, y * gridManager.cellSize);
            Vector3 end = gridManager.gridOrigin + new Vector3(gridManager.gridWidth * gridManager.cellSize, RenderingConstants.GRID_HEIGHT, y * gridManager.cellSize);
            CreateGridLine($"HorizontalLine_{y}", start, end);
        }

        // Vertical lines
        for (int x = 0; x <= gridManager.gridWidth; x++)
        {
            Vector3 start = gridManager.gridOrigin + new Vector3(x * gridManager.cellSize, RenderingConstants.GRID_HEIGHT, 0);
            Vector3 end = gridManager.gridOrigin + new Vector3(x * gridManager.cellSize, RenderingConstants.GRID_HEIGHT, gridManager.gridHeight * gridManager.cellSize);
            CreateGridLine($"VerticalLine_{x}", start, end);
        }

        Debug.Log($"Grid visualization created: {gridManager.gridWidth}x{gridManager.gridHeight}");
    }

    private void CreateGridLine(string name, Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.parent = gridLinesParent.transform;

        MeshFilter meshFilter = lineObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = lineObj.AddComponent<MeshRenderer>();

        meshRenderer.material = lineMaterial;

        // Create double-sided quad mesh for the line
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * lineWidth * 0.5f;

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[8]
        {
            // Top face
            start - perpendicular,
            start + perpendicular,
            end + perpendicular,
            end - perpendicular,
            // Bottom face (same positions)
            start - perpendicular,
            start + perpendicular,
            end + perpendicular,
            end - perpendicular
        };

        // Double-sided: top face CCW from above, bottom face CW from above (CCW from below)
        int[] triangles = new int[12]
        {
            0, 1, 2, 0, 2, 3,  // Top face
            4, 6, 5, 4, 7, 6   // Bottom face
        };

        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++) uvs[i] = new Vector2(0.5f, 0.5f);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }

    public void ToggleGrid()
    {
        showGrid = !showGrid;
        if (gridLinesParent != null)
        {
            gridLinesParent.SetActive(showGrid);
        }
    }

    public void RefreshGrid()
    {
        CreateGridLines();
    }
}
