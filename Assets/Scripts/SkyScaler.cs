using UnityEngine;

/// <summary>
/// Scales and positions a sky quad to always fill the camera view.
///
/// DYNAMIC SYSTEM:
/// - Sky positioned behind grid at fixed Z distance (default Z=5)
/// - Sky aligns with camera's horizontal/vertical offsets to stay centered in view
/// - Sky always rotates to face the camera (perpendicular to camera view)
/// - Sky scales dynamically based on camera FOV and distance
/// - Works with any grid size and extreme camera settings
/// - Fully adapts when grid dimensions or camera settings change
/// </summary>
public class SkyScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera to frame against (defaults to Main Camera)")]
    public Camera targetCamera;

    [Tooltip("The camera setup component for offset values (optional - will auto-find if not set)")]
    public CameraSetup cameraSetup;

    [Header("Positioning")]
    [Tooltip("Distance behind the grid (grid is at Z=0, positive values go behind)")]
    [Range(5f, 50f)]
    public float distanceBehindGrid = 5f;

    [Header("Scaling")]
    [Tooltip("Scale multiplier for full bleed coverage (1.5 = 50% overscan)")]
    [Range(1.2f, 3.0f)]
    public float overscanMultiplier = 1.5f;

    [Header("Auto-Update")]
    [Tooltip("Automatically update when camera settings change")]
    public bool autoUpdate = true;

    private float lastAspect;
    private float lastFOV;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;
    private float lastDistanceBehindGrid;
    private float lastHorizontalOffset;
    private float lastVerticalOffset;

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

        if (cameraSetup == null)
        {
            cameraSetup = ServiceRegistry.Get<CameraSetup>();
        }
    }

    private void Start()
    {
        UpdateSkyPlane();
    }

    private void LateUpdate()
    {
        if (!autoUpdate || targetCamera == null) return;

        // Get current offsets
        float currentHorizontalOffset = cameraSetup != null ? cameraSetup.horizontalOffset : 0f;
        float currentVerticalOffset = cameraSetup != null ? cameraSetup.verticalOffset : 0f;

        // Check if camera or sky settings changed
        bool settingsChanged =
            !Mathf.Approximately(lastAspect, targetCamera.aspect) ||
            !Mathf.Approximately(lastFOV, targetCamera.fieldOfView) ||
            !Mathf.Approximately(lastDistanceBehindGrid, distanceBehindGrid) ||
            !Mathf.Approximately(lastHorizontalOffset, currentHorizontalOffset) ||
            !Mathf.Approximately(lastVerticalOffset, currentVerticalOffset) ||
            lastCameraPosition != targetCamera.transform.position ||
            lastCameraRotation != targetCamera.transform.rotation;

        if (settingsChanged)
        {
            UpdateSkyPlane();
        }
    }

    /// <summary>
    /// Updates the sky plane position and scale to fill camera view.
    /// Sky is positioned close to grid and always faces the camera.
    /// </summary>
    [ContextMenu("Update Sky Plane")]
    public void UpdateSkyPlane()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("SkyScaler: No target camera assigned");
            return;
        }

        // Get camera offsets to align sky with camera's view center
        float horizontalOffset = 0f;
        float verticalOffset = 0f;
        if (cameraSetup != null)
        {
            horizontalOffset = cameraSetup.horizontalOffset;
            verticalOffset = cameraSetup.verticalOffset;
        }

        // Position sky plane behind the grid, aligned with camera's offset position
        // This ensures the sky stays centered in the camera view even with camera offsets
        Vector3 skyPosition = new Vector3(horizontalOffset, verticalOffset, distanceBehindGrid);
        transform.position = skyPosition;

        // Calculate distance from camera to sky
        Vector3 cameraPosition = targetCamera.transform.position;
        float distanceToSky = Vector3.Distance(cameraPosition, skyPosition);

        // Rotate sky to face the camera (perpendicular to view direction)
        // Unity Quad's front face is visible when +Z points AWAY from camera
        // So we point +Z from camera toward sky (opposite direction)
        Vector3 directionToCamera = (cameraPosition - skyPosition).normalized;
        if (directionToCamera.magnitude > 0.001f)
        {
            // Flip direction so front face is visible
            transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        // Calculate visible dimensions at the sky's distance from camera
        float fovRadians = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float viewHeight = 2f * distanceToSky * Mathf.Tan(fovRadians / 2f);
        float viewWidth = viewHeight * targetCamera.aspect;

        // Apply overscan multiplier for full bleed
        viewHeight *= overscanMultiplier;
        viewWidth *= overscanMultiplier;

        // Unity Quad is 1x1 units by default, so scale directly
        transform.localScale = new Vector3(viewWidth, viewHeight, 1f);

        // Cache values for change detection
        lastAspect = targetCamera.aspect;
        lastFOV = targetCamera.fieldOfView;
        lastCameraPosition = cameraPosition;
        lastCameraRotation = targetCamera.transform.rotation;
        lastDistanceBehindGrid = distanceBehindGrid;
        lastHorizontalOffset = horizontalOffset;
        lastVerticalOffset = verticalOffset;

        DebugLog.Info($"SkyScaler: Scaled to {viewWidth:F1}x{viewHeight:F1} units at Z={distanceBehindGrid}, " +
                  $"offset=({horizontalOffset:F1}, {verticalOffset:F1}), distance from camera={distanceToSky:F1}, FOV={targetCamera.fieldOfView:F2}°");
    }
}
