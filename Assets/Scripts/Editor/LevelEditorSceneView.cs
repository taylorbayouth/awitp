using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the level grid in Scene view for visual level editing.
/// Shows grid boundaries, cell coordinates, and placeable spaces.
/// </summary>
[InitializeOnLoad]
public static class LevelEditorSceneView
{
    private static bool _enabled = true;
    private static Color _gridColor = new Color(0.5f, 0.8f, 1f, 0.3f);
    private static Color _boundaryColor = new Color(0.5f, 0.8f, 1f, 0.8f);

    static LevelEditorSceneView()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_enabled) return;

        // Find active GridManager in the scene
        GridManager gridManager = ServiceRegistry.Get<GridManager>(logIfMissing: false);
        if (gridManager == null) return;

        DrawGrid(gridManager);
    }

    private static void DrawGrid(GridManager gridManager)
    {
        int width = gridManager.gridWidth;
        int height = gridManager.gridHeight;
        // Grid cells are normalized to 1.0 world unit

        Color gridColor = _gridColor;
        LevelDefinition levelDef = gridManager.CurrentLevelDefinition;
        if (levelDef != null)
        {
            gridColor = levelDef.gridLineColor;
        }

        // Grid is centered at world origin
        Vector3 gridCenter = Vector3.zero;
        Vector3 gridStart = gridCenter - new Vector3(width * 0.5f, height * 0.5f, 0);

        // Draw grid lines
        Handles.color = gridColor;

        // Vertical lines
        for (int x = 0; x <= width; x++)
        {
            Vector3 start = gridStart + new Vector3(x, 0, 0);
            Vector3 end = start + new Vector3(0, height, 0);
            Handles.DrawLine(start, end);
        }

        // Horizontal lines
        for (int y = 0; y <= height; y++)
        {
            Vector3 start = gridStart + new Vector3(0, y, 0);
            Vector3 end = start + new Vector3(width, 0, 0);
            Handles.DrawLine(start, end);
        }

        // Draw boundary (thicker)
        Handles.color = _boundaryColor;
        Vector3 topLeft = gridStart;
        Vector3 topRight = gridStart + new Vector3(width, 0, 0);
        Vector3 bottomLeft = gridStart + new Vector3(0, height, 0);
        Vector3 bottomRight = gridStart + new Vector3(width, height, 0);

        Handles.DrawLine(topLeft, topRight, 3f);
        Handles.DrawLine(topRight, bottomRight, 3f);
        Handles.DrawLine(bottomRight, bottomLeft, 3f);
        Handles.DrawLine(bottomLeft, topLeft, 3f);

        // Draw grid info
        Handles.BeginGUI();
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = Color.white;
        style.fontSize = 14;

        Vector3 labelPos = gridStart - new Vector3(0.5f, 0.5f, 0);
        Vector3 screenPos = SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(labelPos);
        screenPos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - screenPos.y;

        if (screenPos.z > 0)
        {
            Handles.Label(labelPos, $"{width}×{height}", style);
        }

        Handles.EndGUI();
    }

    /// <summary>
    /// Toggles grid visualization on/off.
    /// </summary>
    [MenuItem("Tools/Level Editor/Toggle Grid Visualization")]
    private static void ToggleGridVisualization()
    {
        _enabled = !_enabled;
        SceneView.RepaintAll();
        Debug.Log($"Level Editor Grid: {(_enabled ? "Enabled" : "Disabled")}");
    }
}
