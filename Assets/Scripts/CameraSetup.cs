using UnityEngine;

/// <summary>
/// Sets up the camera to view the grid on the XY plane.
/// Camera looks along -Z axis at the grid.
/// All settings are configurable in the Inspector and changes apply in real-time.
/// </summary>
public class CameraSetup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The grid manager to frame in the camera view")]
    public GridManager gridManager;

    [Tooltip("The camera to configure (defaults to Main Camera if not set)")]
    public Camera targetCamera;

    [Header("Perspective Settings")]
    [Tooltip("Field of view for perspective camera (in degrees)")]
    [Range(20f, 120f)]
    public float fieldOfView = 45f;

    [Tooltip("Near clipping plane distance")]
    [Range(0.01f, 10f)]
    public float nearClipPlane = 0.3f;

    [Tooltip("Far clipping plane distance")]
    [Range(10f, 1000f)]
    public float farClipPlane = 100f;

    [Header("Camera Offsets")]
    [Tooltip("Vertical offset from grid center (moves camera up/down)")]
    [Range(-20f, 20f)]
    public float verticalOffset = 0f;

    [Tooltip("Horizontal offset from grid center (moves camera left/right)")]
    [Range(-20f, 20f)]
    public float horizontalOffset = 0f;

    [Header("Camera Rotation")]
    [Tooltip("Tilt angle (negative = look down, positive = look up)")]
    [Range(-45f, 45f)]
    public float tiltAngle = 0f;

    [Tooltip("Pan angle (rotate left/right around the grid)")]
    [Range(-45f, 45f)]
    public float panAngle = 0f;

    [Header("Framing Settings")]
    [Tooltip("Fixed margin around grid in world units (e.g., 1.0 = 1 unit of space on all sides)")]
    [Range(0f, 5f)]
    public float gridMargin = 1.0f;

    [Tooltip("Minimum distance from grid (prevents camera getting too close)")]
    [Range(5f, 50f)]
    public float minDistance = 8f;

    [Header("Auto-Update")]
    [Tooltip("Automatically update camera when these values change in the Inspector")]
    public bool autoUpdateInEditor = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }
        }

        if (gridManager == null)
        {
            gridManager = UnityEngine.Object.FindAnyObjectByType<GridManager>();
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
            DebugLog.Info("Camera setup refreshed manually (C key pressed)");
        }
    }

    #if UNITY_EDITOR
    private float lastFieldOfView;
    private float lastGridMargin;
    private float lastVerticalOffset;
    private float lastHorizontalOffset;

    private void LateUpdate()
    {
        // In editor play mode, detect changes and auto-refresh
        if (Application.isPlaying && autoUpdateInEditor)
        {
            bool settingsChanged =
                lastFieldOfView != fieldOfView ||
                lastGridMargin != gridMargin ||
                lastVerticalOffset != verticalOffset ||
                lastHorizontalOffset != horizontalOffset;

            if (settingsChanged)
            {
                SetupCamera();
                lastFieldOfView = fieldOfView;
                lastGridMargin = gridMargin;
                lastVerticalOffset = verticalOffset;
                lastHorizontalOffset = horizontalOffset;
            }
        }
    }
    #endif

    #if UNITY_EDITOR
    /// <summary>
    /// Called when values change in the Inspector (Editor only).
    /// Automatically updates camera if autoUpdateInEditor is enabled.
    /// </summary>
    private void OnValidate()
    {
        if (autoUpdateInEditor && Application.isPlaying)
        {
            // Delay to ensure all serialized values are updated
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) SetupCamera();
            };
        }
    }
    #endif

    /// <summary>
    /// Context menu item to manually refresh camera setup.
    /// Right-click component in Inspector and select "Refresh Camera Setup".
    /// </summary>
    [ContextMenu("Refresh Camera Setup")]
    private void RefreshCameraSetup()
    {
        SetupCamera();
        DebugLog.Info("Camera setup refreshed via context menu");
    }

    public void SetupCamera()
    {
        if (targetCamera == null || gridManager == null)
        {
            Debug.LogWarning("CameraSetup: Missing camera or grid manager reference");
            return;
        }

        // Always use perspective
        targetCamera.orthographic = false;
        targetCamera.fieldOfView = fieldOfView;

        // Apply clipping planes
        targetCamera.nearClipPlane = nearClipPlane;
        targetCamera.farClipPlane = farClipPlane;

        // Calculate optimal distance to fit grid in view
        float distance = CalculateCameraDistance();

        // Set camera position with calculated distance and offsets
        targetCamera.transform.position = new Vector3(horizontalOffset, verticalOffset, -distance);

        // Apply rotation (tilt and pan)
        targetCamera.transform.rotation = Quaternion.Euler(tiltAngle, panAngle, 0f);

        DebugLog.Info($"CameraSetup: Perspective camera at {targetCamera.transform.position}, " +
                  $"FOV={fieldOfView:F1}°, distance={distance:F2}, grid={gridManager.gridWidth}x{gridManager.gridHeight}");
    }

    private Vector3 CalculateGridCenter()
    {
        // Grid is now auto-centered at world origin (0,0,0)
        return Vector3.zero;
    }

    /// <summary>
    /// Calculates the camera distance needed to fit the entire grid with padding in perspective view.
    /// Accounts for aspect ratio and FOV to ensure grid fits both horizontally and vertically.
    /// </summary>
    /// <returns>Distance from grid center along -Z axis</returns>
    private float CalculateCameraDistance()
    {
        float gridWorldWidth = gridManager.gridWidth * gridManager.cellSize;
        float gridWorldHeight = gridManager.gridHeight * gridManager.cellSize;

        // Add fixed margins to grid dimensions (e.g., 1.0 unit margin on each side = +2.0 total)
        gridWorldWidth += (gridMargin * 2f);
        gridWorldHeight += (gridMargin * 2f);

        float aspect = targetCamera.aspect;

        // Validate aspect ratio
        if (aspect <= 0 || float.IsNaN(aspect) || float.IsInfinity(aspect))
        {
            Debug.LogWarning("Invalid camera aspect ratio, using fallback 16:9");
            aspect = 16f / 9f;
        }

        // Convert FOV to radians
        float fovRadians = fieldOfView * Mathf.Deg2Rad;

        // Calculate distance needed to fit grid height
        // tan(fov/2) = (gridHeight/2) / distance
        // distance = (gridHeight/2) / tan(fov/2)
        float distanceForHeight = (gridWorldHeight / 2f) / Mathf.Tan(fovRadians / 2f);

        // Calculate distance needed to fit grid width
        // Account for aspect ratio: horizontal FOV = 2 * atan(tan(verticalFOV/2) * aspect)
        float horizontalFovRadians = 2f * Mathf.Atan(Mathf.Tan(fovRadians / 2f) * aspect);
        float distanceForWidth = (gridWorldWidth / 2f) / Mathf.Tan(horizontalFovRadians / 2f);

        // Use whichever distance is larger (ensures both dimensions fit)
        float distance = Mathf.Max(distanceForHeight, distanceForWidth);

        // Enforce minimum distance
        distance = Mathf.Max(distance, minDistance);

        DebugLog.Info($"CameraSetup: Grid {gridManager.gridWidth}x{gridManager.gridHeight} " +
                  $"({gridWorldWidth:F1}x{gridWorldHeight:F1} world units), " +
                  $"aspect={aspect:F2}, FOV={fieldOfView:F1}°, " +
                  $"distHeight={distanceForHeight:F2}, distWidth={distanceForWidth:F2}, " +
                  $"final distance={distance:F2}");

        return distance;
    }

    public void RefreshCamera()
    {
        SetupCamera();
    }

    /// <summary>
    /// Exports current camera settings to a CameraSettings data object.
    /// Used when saving levels in the editor.
    /// </summary>
    public LevelData.CameraSettings ExportSettings()
    {
        return new LevelData.CameraSettings
        {
            fieldOfView = fieldOfView,
            nearClipPlane = nearClipPlane,
            farClipPlane = farClipPlane,
            verticalOffset = verticalOffset,
            horizontalOffset = horizontalOffset,
            tiltAngle = tiltAngle,
            panAngle = panAngle,
            gridMargin = gridMargin,
            minDistance = minDistance
        };
    }

    /// <summary>
    /// Imports camera settings from a CameraSettings data object.
    /// Used when loading levels.
    /// </summary>
    public void ImportSettings(LevelData.CameraSettings settings)
    {
        if (settings == null) return;

        fieldOfView = settings.fieldOfView;
        nearClipPlane = settings.nearClipPlane;
        farClipPlane = settings.farClipPlane;
        verticalOffset = settings.verticalOffset;
        horizontalOffset = settings.horizontalOffset;
        tiltAngle = settings.tiltAngle;
        panAngle = settings.panAngle;
        gridMargin = settings.gridMargin;
        minDistance = settings.minDistance;

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
