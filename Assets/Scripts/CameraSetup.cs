using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public Camera targetCamera;

    [Header("Camera Settings")]
    public float heightOffset = 15f; // How high above the grid center
    public float paddingPercent = 0.1f; // 10% padding around grid edges
    public float minOrthographicSize = 3f;

    [Header("Auto Update")]
    public bool autoUpdateOnGridChange = true;

    private void Awake()
    {
        // Find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }

        // Find grid manager if not assigned
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
    }

    private void Start()
    {
        SetupCamera();
    }

    public void SetupCamera()
    {
        if (targetCamera == null || gridManager == null)
        {
            Debug.LogWarning("CameraSetup: Missing camera or grid manager reference");
            return;
        }

        // Calculate grid center in world space
        Vector3 gridCenter = CalculateGridCenter();

        // Position camera above center looking down
        targetCamera.transform.position = gridCenter + new Vector3(0, heightOffset, 0);
        targetCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Set to orthographic
        targetCamera.orthographic = true;

        // Calculate orthographic size to fit entire grid
        float orthographicSize = CalculateOrthographicSize();
        targetCamera.orthographicSize = orthographicSize;

        Debug.Log($"Camera positioned at {targetCamera.transform.position} with orthographic size {orthographicSize}");
    }

    private Vector3 CalculateGridCenter()
    {
        float centerX = (gridManager.gridWidth * gridManager.cellSize) / 2f;
        float centerZ = (gridManager.gridHeight * gridManager.cellSize) / 2f;

        return gridManager.gridOrigin + new Vector3(centerX, 0, centerZ);
    }

    private float CalculateOrthographicSize()
    {
        // Calculate dimensions needed to see entire grid
        float gridWorldWidth = gridManager.gridWidth * gridManager.cellSize;
        float gridWorldHeight = gridManager.gridHeight * gridManager.cellSize;

        // Get camera aspect ratio
        float aspect = targetCamera.aspect;

        // Orthographic size is half the height of the view
        // We need to fit the larger dimension based on aspect ratio
        float heightNeeded = gridWorldHeight / 2f;
        float widthNeeded = gridWorldWidth / (2f * aspect);

        // Use whichever is larger, add padding
        float size = Mathf.Max(heightNeeded, widthNeeded);
        size *= (1f + paddingPercent);

        // Clamp to minimum size
        size = Mathf.Max(size, minOrthographicSize);

        return size;
    }

    // Call this when grid dimensions change
    public void RefreshCamera()
    {
        SetupCamera();
    }

    // OnValidate removed to prevent errors during initialization

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        if (gridManager == null || targetCamera == null) return;

        // Draw camera view frustum
        Gizmos.color = Color.cyan;
        Vector3 center = CalculateGridCenter();
        Gizmos.DrawWireSphere(center, 0.5f);

        // Draw line from camera to center
        Gizmos.DrawLine(targetCamera.transform.position, center);
    }
    #endregion
}
