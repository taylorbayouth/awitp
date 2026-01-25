using UnityEngine;

/// <summary>
/// Visualizes the grid with lines on the XY plane using LineRenderer.
/// </summary>
[RequireComponent(typeof(GridManager))]
public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Line Settings")]
    public float lineWidth = RenderingConstants.GRID_LINE_WIDTH;
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

        Color gridColor = BlockColors.GridLine;
        gridColor.a = RenderingConstants.GRID_LINE_OPACITY;
        float z = RenderingConstants.GRID_DEPTH;

        // Horizontal lines (along X axis, at each Y level)
        for (int y = 0; y <= gridManager.gridHeight; y++)
        {
            Vector3 start = gridManager.gridOrigin + new Vector3(0, y * gridManager.cellSize, z);
            Vector3 end = gridManager.gridOrigin + new Vector3(gridManager.gridWidth * gridManager.cellSize, y * gridManager.cellSize, z);
            CreateGridLine($"HorizontalLine_{y}", start, end, gridColor);
        }

        // Vertical lines (along Y axis, at each X level)
        for (int x = 0; x <= gridManager.gridWidth; x++)
        {
            Vector3 start = gridManager.gridOrigin + new Vector3(x * gridManager.cellSize, 0, z);
            Vector3 end = gridManager.gridOrigin + new Vector3(x * gridManager.cellSize, gridManager.gridHeight * gridManager.cellSize, z);
            CreateGridLine($"VerticalLine_{x}", start, end, gridColor);
        }

        Debug.Log($"Grid visualization created: {gridManager.gridWidth}x{gridManager.gridHeight}, line color = {gridColor}");
    }

    private void CreateGridLine(string name, Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.parent = gridLinesParent.transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        // Create a unique material instance using Legacy Shaders/Particles/Alpha Blended
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
        {
            Debug.LogError("Could not find shader!");
            shader = Shader.Find("Sprites/Default");
        }

        Material lineMaterial = new Material(shader);
        lineMaterial.name = name + "_Material";

        // ONLY set vertex colors, don't set material.color
        lr.sharedMaterial = lineMaterial;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.sortingOrder = RenderingConstants.GRID_SORTING;
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
