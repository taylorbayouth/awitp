using UnityEngine;

/// <summary>
/// Scales and positions a sky quad to always fill the camera view.
///
/// DYNAMIC SYSTEM:
/// - Sky positioned very close to grid (default Z=5)
/// - Sky always rotates to face the camera (perpendicular to camera view)
/// - Sky scales dynamically based on camera FOV and distance
/// - Works with any grid size and extreme camera settings
/// </summary>
public class SkyScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera to frame against (defaults to Main Camera)")]
    public Camera targetCamera;

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
    }

    private void Start()
    {
        UpdateSkyPlane();
    }

    private void LateUpdate()
    {
        if (!autoUpdate || targetCamera == null) return;

        // Check if camera or sky settings changed
        bool settingsChanged =
            !Mathf.Approximately(lastAspect, targetCamera.aspect) ||
            !Mathf.Approximately(lastFOV, targetCamera.fieldOfView) ||
            !Mathf.Approximately(lastDistanceBehindGrid, distanceBehindGrid) ||
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

        // Position sky plane behind the grid at fixed Z
        Vector3 skyPosition = new Vector3(0f, 0f, distanceBehindGrid);
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

        DebugLog.Info($"SkyScaler: Scaled to {viewWidth:F1}x{viewHeight:F1} units at Z={distanceBehindGrid}, " +
                  $"distance from camera={distanceToSky:F1}, FOV={targetCamera.fieldOfView:F2}°");
    }
}
