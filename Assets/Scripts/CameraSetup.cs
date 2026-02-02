using UnityEngine;

/// <summary>
/// Sets up the camera to view the grid on the XY plane.
///
/// SIMPLE POSITIONING SYSTEM:
/// 1. Camera starts centered with grid at (0, 0, -distance)
/// 2. Vertical/Horizontal offsets move the camera position
/// 3. Tilt angles the camera down/up to see block tops
/// 4. Distance is auto-calculated to frame the grid, or can be manually controlled
///
/// All camera settings are saved per-level via the CameraSettings class in LevelData.
/// </summary>
public class CameraSetup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The grid manager to frame in the camera view")]
    public GridManager gridManager;

    [Tooltip("The camera to configure (defaults to Main Camera if not set)")]
    public Camera targetCamera;

    [Header("Camera Position")]
    [Tooltip("Vertical position offset - moves camera up (positive) or down (negative) from grid center")]
    [Range(0f, 28f)]
    public float verticalOffset = 10.4f;

    [Tooltip("Horizontal position offset - moves camera left (negative) or right (positive) from grid center")]
    [Range(-10f, 10f)]
    public float horizontalOffset = 0f;

    [Header("Camera Rotation")]
    [Tooltip("Tilt/pitch angle - negative looks down (to see block tops), positive looks up")]
    [Range(-5f, 20f)]
    public float tiltAngle = 3.7f;

    [Tooltip("Pan/yaw angle - rotates camera left/right")]
    [Range(-15f, 15f)]
    public float panAngle = 0f;

    [Tooltip("Roll angle - tilts the horizon (usually keep at 0)")]
    [Range(-10f, 10f)]
    public float rollAngle = 0f;

    [Header("Perspective Settings")]
    [Tooltip("Focal length in mm (like camera lenses). Higher = telephoto (flatter perspective), Lower = wide angle (more distortion). 50mm is 'normal'.")]
    [Range(100f, 1200f)]
    public float focalLength = 756f;

    [Tooltip("Field of view (auto-calculated from focal length). Shown for reference.")]
    [Range(1f, 30f)]
    public float fieldOfView = 1.82f;

    [Tooltip("Near clipping plane distance")]
    [Range(0.01f, 1f)]
    public float nearClipPlane = 0.24f;

    [Tooltip("Far clipping plane distance")]
    [Range(100f, 1000f)]
    public float farClipPlane = 500f;

    [Header("Distance Settings")]
    [Tooltip("Multiplier for camera distance. Higher values = flatter perspective (less distortion between blocks).")]
    [Range(5f, 40f)]
    public float distanceMultiplier = 23.7f;

    [Tooltip("Minimum distance from grid (prevents camera getting too close)")]
    [Range(1f, 10f)]
    public float minDistance = 5f;

    [Tooltip("Fixed margin around grid in world units for framing calculations")]
    [Range(0f, 3f)]
    public float gridMargin = 1f;

    [Header("Auto-Update")]
    [Tooltip("Automatically update camera when values change in the Inspector")]
    public bool autoUpdateInEditor = true;

    // Legacy fields for backwards compatibility with old save data
    [HideInInspector] public float viewAngle = 0f;
    [HideInInspector] public float orbitAngle = 0f;
    [HideInInspector] public float tiltOffset = 0f;
    [HideInInspector] public float rollOffset = 0f;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = ServiceRegistry.Get<Camera>();
            }
        }

        if (gridManager == null)
        {
            gridManager = ServiceRegistry.Get<GridManager>();
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

    #if UNITY_EDITOR
    private float lastFocalLength;
    private float lastGridMargin;
    private float lastVerticalOffset;
    private float lastHorizontalOffset;
    private float lastDistanceMultiplier;
    private float lastMinDistance;
    private float lastTiltAngle;
    private float lastPanAngle;
    private float lastRollAngle;

    private void LateUpdate()
    {
        // In editor play mode, detect changes and auto-refresh
        if (Application.isPlaying && autoUpdateInEditor)
        {
            bool settingsChanged =
                lastFocalLength != focalLength ||
                lastGridMargin != gridMargin ||
                lastVerticalOffset != verticalOffset ||
                lastHorizontalOffset != horizontalOffset ||
                lastDistanceMultiplier != distanceMultiplier ||
                lastMinDistance != minDistance ||
                lastTiltAngle != tiltAngle ||
                lastPanAngle != panAngle ||
                lastRollAngle != rollAngle;

            if (settingsChanged)
            {
                SetupCamera();
                lastFocalLength = focalLength;
                lastGridMargin = gridMargin;
                lastVerticalOffset = verticalOffset;
                lastHorizontalOffset = horizontalOffset;
                lastDistanceMultiplier = distanceMultiplier;
                lastMinDistance = minDistance;
                lastTiltAngle = tiltAngle;
                lastPanAngle = panAngle;
                lastRollAngle = rollAngle;
            }
        }
    }
    #endif

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoUpdateInEditor && Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) SetupCamera();
            };
        }
    }
    #endif

    [ContextMenu("Refresh Camera Setup")]
    private void RefreshCameraSetup()
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

        // Always use perspective
        targetCamera.orthographic = false;

        // Convert focal length to FOV (assuming 35mm full-frame equivalent)
        // FOV = 2 * atan(sensorHeight / (2 * focalLength))
        // For 35mm film, sensor height is 24mm
        const float sensorHeight = 24f;
        fieldOfView = 2f * Mathf.Atan(sensorHeight / (2f * focalLength)) * Mathf.Rad2Deg;
        targetCamera.fieldOfView = fieldOfView;

        // Apply clipping planes
        targetCamera.nearClipPlane = nearClipPlane;
        targetCamera.farClipPlane = farClipPlane;

        // Calculate distance to frame grid
        float distance = CalculateCameraDistance();

        // Position camera: centered with grid, offset by user values
        // Camera looks along -Z axis, so position is at negative Z
        Vector3 cameraPosition = new Vector3(horizontalOffset, verticalOffset, -distance);
        targetCamera.transform.position = cameraPosition;

        // Apply rotation: tilt (pitch), pan (yaw), roll
        // Euler angles: (X=pitch/tilt, Y=yaw/pan, Z=roll)
        targetCamera.transform.rotation = Quaternion.Euler(tiltAngle, panAngle, rollAngle);
    }

    /// <summary>
    /// Calculates the camera distance needed to fit the entire grid with padding.
    /// Distance is calculated at a FIXED reference FOV (50mm equivalent) so that
    /// focal length can be changed independently to control zoom without affecting distance.
    /// Each grid cell is 1.0 world unit.
    /// </summary>
    private float CalculateCameraDistance()
    {
        float gridWorldWidth = gridManager.gridWidth;
        float gridWorldHeight = gridManager.gridHeight;

        // Add margins
        gridWorldWidth += (gridMargin * 2f);
        gridWorldHeight += (gridMargin * 2f);

        float aspect = targetCamera.aspect;
        if (aspect <= 0 || float.IsNaN(aspect) || float.IsInfinity(aspect))
        {
            Debug.LogWarning("Invalid camera aspect ratio, using fallback 16:9");
            aspect = 16f / 9f;
        }

        // Use a FIXED reference FOV for distance calculation (50mm = ~47°)
        // This decouples distance from focal length, allowing independent control
        const float referenceFOV = 47f;
        float refFovRadians = referenceFOV * Mathf.Deg2Rad;

        // Distance needed to fit grid height at reference FOV
        float distanceForHeight = (gridWorldHeight / 2f) / Mathf.Tan(refFovRadians / 2f);

        // Distance needed to fit grid width at reference FOV
        float horizontalFovRadians = 2f * Mathf.Atan(Mathf.Tan(refFovRadians / 2f) * aspect);
        float distanceForWidth = (gridWorldWidth / 2f) / Mathf.Tan(horizontalFovRadians / 2f);

        // Use larger distance to ensure both dimensions fit
        float distance = Mathf.Max(distanceForHeight, distanceForWidth);

        // Apply multiplier - this is what controls perspective flattening
        distance *= distanceMultiplier;

        // Enforce minimum
        distance = Mathf.Max(distance, minDistance);

        return distance;
    }

    public void RefreshCamera()
    {
        SetupCamera();
    }

    /// <summary>
    /// Exports current camera settings to a CameraSettings data object.
    /// </summary>
    public LevelData.CameraSettings ExportSettings()
    {
        // Calculate FOV from focal length for storage
        const float sensorHeight = 24f;
        float fov = 2f * Mathf.Atan(sensorHeight / (2f * focalLength)) * Mathf.Rad2Deg;

        return new LevelData.CameraSettings
        {
            fieldOfView = fov,  // Store FOV (calculated from focal length)
            nearClipPlane = nearClipPlane,
            farClipPlane = farClipPlane,
            verticalOffset = verticalOffset,
            horizontalOffset = horizontalOffset,
            tiltAngle = tiltAngle,
            panAngle = panAngle,
            gridMargin = gridMargin,
            minDistance = minDistance,
            distanceMultiplier = distanceMultiplier,
            rollOffset = rollAngle,
            tiltOffset = focalLength  // Repurpose tiltOffset to store focal length
        };
    }

    /// <summary>
    /// Imports camera settings from a CameraSettings data object.
    /// </summary>
    public void ImportSettings(LevelData.CameraSettings settings)
    {
        if (settings == null) return;

        nearClipPlane = settings.nearClipPlane;
        farClipPlane = settings.farClipPlane;
        verticalOffset = settings.verticalOffset;
        horizontalOffset = settings.horizontalOffset;
        tiltAngle = settings.tiltAngle;
        panAngle = settings.panAngle;
        gridMargin = settings.gridMargin;
        minDistance = settings.minDistance;
        distanceMultiplier = settings.distanceMultiplier > 0 ? settings.distanceMultiplier : 1.0f;
        rollAngle = settings.rollOffset;

        // Check if focal length was saved (stored in tiltOffset field)
        // If > 0, use it; otherwise convert from FOV
        if (settings.tiltOffset > 5f)  // Reasonable focal length range
        {
            focalLength = settings.tiltOffset;
        }
        else
        {
            // Convert FOV to focal length for old saves
            const float sensorHeight = 24f;
            float fovRadians = settings.fieldOfView * Mathf.Deg2Rad;
            focalLength = sensorHeight / (2f * Mathf.Tan(fovRadians / 2f));
            focalLength = Mathf.Clamp(focalLength, 10f, 200f);
        }

        SetupCamera();
    }

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        if (gridManager == null || targetCamera == null) return;

        Vector3 gridCenter = Vector3.zero;

        // Draw grid center
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(gridCenter, 0.5f);

        // Draw line from camera to grid center
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(targetCamera.transform.position, gridCenter);

        // Draw camera forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(targetCamera.transform.position, targetCamera.transform.forward * 5f);
    }
    #endregion
}
