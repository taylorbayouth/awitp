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

    [Header("Camera Mode")]
    [Tooltip("Use orthographic (true) or perspective (false) projection")]
    public bool useOrthographic = false;

    [Header("Perspective Settings")]
    [Tooltip("Field of view for perspective camera (in degrees)")]
    [Range(20f, 120f)]
    public float fieldOfView = 60f;

    [Tooltip("Near clipping plane distance")]
    [Range(0.01f, 10f)]
    public float nearClipPlane = 0.3f;

    [Tooltip("Far clipping plane distance")]
    [Range(10f, 1000f)]
    public float farClipPlane = 100f;

    [Header("Camera Position")]
    [Tooltip("Distance from grid along -Z axis")]
    [Range(5f, 50f)]
    public float distanceFromGrid = 15f;

    [Tooltip("Vertical offset (moves camera up/down)")]
    [Range(-20f, 20f)]
    public float topMarginOffset = 0f;

    [Tooltip("Horizontal offset (moves camera left/right)")]
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
    [Tooltip("Extra space around grid as percentage (0.0 = tight fit, 1.0 = 2x grid size)")]
    [Range(0f, 2f)]
    public float paddingPercent = 0.15f;

    [Tooltip("Minimum orthographic size (prevents over-zooming on small grids)")]
    [Range(1f, 10f)]
    public float minOrthographicSize = 3f;

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

        // Set camera position with offsets
        targetCamera.transform.position = new Vector3(horizontalOffset, topMarginOffset, -distanceFromGrid);

        // Apply rotation (tilt and pan)
        targetCamera.transform.rotation = Quaternion.Euler(tiltAngle, panAngle, 0f);

        // Apply camera mode
        targetCamera.orthographic = useOrthographic;

        // Apply clipping planes
        targetCamera.nearClipPlane = nearClipPlane;
        targetCamera.farClipPlane = farClipPlane;

        if (useOrthographic)
        {
            targetCamera.orthographicSize = CalculateOrthographicSize();
            DebugLog.Info($"CameraSetup: Orthographic camera at {targetCamera.transform.position}, " +
                      $"size={targetCamera.orthographicSize:F2}, padding={paddingPercent:F2}");
        }
        else
        {
            targetCamera.fieldOfView = fieldOfView;
            DebugLog.Info($"CameraSetup: Perspective camera at {targetCamera.transform.position}, " +
                      $"rotation=({tiltAngle:F1}°, {panAngle:F1}°, 0°), distance={distanceFromGrid:F2}, FOV={fieldOfView:F1}°");
        }
    }

    private Vector3 CalculateGridCenter()
    {
        // Grid is now auto-centered at world origin (0,0,0)
        return Vector3.zero;
    }

    /// <summary>
    /// Calculates the orthographic size needed to fit the entire grid with padding.
    /// Accounts for aspect ratio to ensure grid fits both horizontally and vertically.
    /// </summary>
    /// <returns>Orthographic size value (half the vertical view height)</returns>
    private float CalculateOrthographicSize()
    {
        float gridWorldWidth = gridManager.gridWidth * gridManager.cellSize;
        float gridWorldHeight = gridManager.gridHeight * gridManager.cellSize;

        float aspect = targetCamera.aspect;

        // Validate aspect ratio to prevent division by zero or invalid values
        if (aspect <= 0 || float.IsNaN(aspect) || float.IsInfinity(aspect))
        {
            Debug.LogWarning("Invalid camera aspect ratio, using fallback 16:9");
            aspect = 16f / 9f;
        }

        // Orthographic size is half the vertical view height
        // We need to fit the grid width OR height, whichever requires more zoom
        float heightNeeded = gridWorldHeight / 2f;
        float widthNeeded = gridWorldWidth / (2f * aspect);

        // Use whichever dimension requires more space
        float size = Mathf.Max(heightNeeded, widthNeeded);

        // Apply padding as a multiplier (e.g., 0.15 = 15% extra space)
        size *= (1f + paddingPercent);

        // Enforce minimum size to prevent extreme zoom on tiny grids
        size = Mathf.Max(size, minOrthographicSize);

        DebugLog.Info($"CameraSetup: Grid {gridManager.gridWidth}x{gridManager.gridHeight}, " +
                  $"aspect={aspect:F2}, heightNeeded={heightNeeded:F2}, widthNeeded={widthNeeded:F2}, " +
                  $"padding={paddingPercent:F2}, final size={size:F2}");

        return size;
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
            useOrthographic = useOrthographic,
            fieldOfView = fieldOfView,
            nearClipPlane = nearClipPlane,
            farClipPlane = farClipPlane,
            distanceFromGrid = distanceFromGrid,
            topMarginOffset = topMarginOffset,
            horizontalOffset = horizontalOffset,
            tiltAngle = tiltAngle,
            panAngle = panAngle,
            paddingPercent = paddingPercent,
            minOrthographicSize = minOrthographicSize
        };
    }

    /// <summary>
    /// Imports camera settings from a CameraSettings data object.
    /// Used when loading levels.
    /// </summary>
    public void ImportSettings(LevelData.CameraSettings settings)
    {
        if (settings == null) return;

        useOrthographic = settings.useOrthographic;
        fieldOfView = settings.fieldOfView;
        nearClipPlane = settings.nearClipPlane;
        farClipPlane = settings.farClipPlane;
        distanceFromGrid = settings.distanceFromGrid;
        topMarginOffset = settings.topMarginOffset;
        horizontalOffset = settings.horizontalOffset;
        tiltAngle = settings.tiltAngle;
        panAngle = settings.panAngle;
        paddingPercent = settings.paddingPercent;
        minOrthographicSize = settings.minOrthographicSize;

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
