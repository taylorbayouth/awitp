using UnityEngine;

/// <summary>
/// Sets up the camera to view the grid on the XY plane.
/// Camera looks along -Z axis at the grid.
/// </summary>
public class CameraSetup : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public Camera targetCamera;

    [Header("Camera Settings")]
    public float distanceFromGrid = 15f;
    public float paddingPercent = 0.15f; // Increased padding for better framing
    public float minOrthographicSize = 3f;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
    }

    private void Start()
    {
        SetupCamera();
    }

    private void Update()
    {
        // Press C to force camera setup (for debugging)
        if (Input.GetKeyDown(KeyCode.C))
        {
            SetupCamera();
        }
    }

    public void SetupCamera()
    {
        if (targetCamera == null || gridManager == null)
        {
            Debug.LogWarning("CameraSetup: Missing camera or grid manager reference");
            return;
        }

        // Since grid is now centered at world origin, camera is simply at (0,0,-distanceFromGrid)
        targetCamera.transform.position = new Vector3(0, 0, -distanceFromGrid);
        targetCamera.transform.rotation = Quaternion.identity; // Looking forward along +Z

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = CalculateOrthographicSize();

        Debug.Log($"CameraSetup: Camera positioned at {targetCamera.transform.position}, " +
                  $"orthographicSize={targetCamera.orthographicSize:F2}, looking at world origin");
    }

    private Vector3 CalculateGridCenter()
    {
        // Grid is now auto-centered at world origin (0,0,0)
        return Vector3.zero;
    }

    private float CalculateOrthographicSize()
    {
        float gridWorldWidth = gridManager.gridWidth * gridManager.cellSize;
        float gridWorldHeight = gridManager.gridHeight * gridManager.cellSize;

        float aspect = targetCamera.aspect;

        // Orthographic size is half the vertical view height
        // We need to fit the grid width OR height, whichever requires more zoom
        float heightNeeded = gridWorldHeight / 2f;
        float widthNeeded = gridWorldWidth / (2f * aspect);

        float size = Mathf.Max(heightNeeded, widthNeeded);
        size *= (1f + paddingPercent); // Add padding
        size = Mathf.Max(size, minOrthographicSize);

        Debug.Log($"CameraSetup: Grid size {gridManager.gridWidth}x{gridManager.gridHeight}, " +
                  $"aspect={aspect:F2}, heightNeeded={heightNeeded:F2}, widthNeeded={widthNeeded:F2}, " +
                  $"final ortho size={size:F2}");

        return size;
    }

    public void RefreshCamera()
    {
        SetupCamera();
    }

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        if (gridManager == null || targetCamera == null) return;

        Gizmos.color = Color.cyan;
        Vector3 center = CalculateGridCenter();
        Gizmos.DrawWireSphere(center, 0.5f);
        Gizmos.DrawLine(targetCamera.transform.position, center);
    }
    #endregion
}
