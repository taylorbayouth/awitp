using UnityEngine;

/// <summary>
/// Scales and positions a sky quad to always fill the camera view.
///
/// DYNAMIC SYSTEM:
/// - Sky positioned where camera's view axis intersects Z=distanceBehindGrid plane
/// - This ensures sky is perfectly centered in camera view regardless of camera position/rotation
/// - Sky always rotates to face the camera (perpendicular to camera view)
/// - Sky scales dynamically based on camera FOV and distance
/// - Works with any grid size, camera position, tilt, and extreme camera settings
/// - Fully adapts when grid dimensions or camera settings change
/// </summary>
public class SkyScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera to frame against (defaults to Main Camera)")]
    public Camera targetCamera;

    [Header("Positioning")]
    [Tooltip("Distance behind the grid (grid is at Z=0, positive values go behind)")]
    [Range(0.1f, 50f)]
    public float distanceBehindGrid = 1f;

    [Header("Scaling")]
    [Tooltip("Scale multiplier for full bleed coverage (1.5 = 50% overscan)")]
    [Range(1.2f, 3.0f)]
    public float overscanMultiplier = 1.5f;

    [Tooltip("Preserve the sky texture's native aspect ratio (prevents squashing)")]
    public bool preserveTextureAspect = true;

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

        // Calculate where the camera's view axis intersects the plane at Z=distanceBehindGrid
        // This ensures the sky is centered in the camera's view, accounting for camera tilt/position
        Vector3 cameraPos = targetCamera.transform.position;
        Vector3 cameraForward = targetCamera.transform.forward;

        // Find intersection with plane at Z=distanceBehindGrid
        // Ray: P + t*D, where we solve for t when Z = distanceBehindGrid
        float t = (distanceBehindGrid - cameraPos.z) / cameraForward.z;
        Vector3 skyPosition = cameraPos + t * cameraForward;

        // Ensure Z is exactly at distanceBehindGrid (avoid floating point drift)
        skyPosition.z = distanceBehindGrid;
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

        float targetWidth = viewWidth;
        float targetHeight = viewHeight;

        if (preserveTextureAspect)
        {
            float textureAspect = GetMainTextureAspect();
            if (textureAspect > 0f)
            {
                float viewAspect = viewWidth / viewHeight;
                if (textureAspect >= viewAspect)
                {
                    targetHeight = viewHeight;
                    targetWidth = viewHeight * textureAspect;
                }
                else
                {
                    targetWidth = viewWidth;
                    targetHeight = viewWidth / textureAspect;
                }
            }
        }

        // Unity Quad is 1x1 units by default, so scale directly
        transform.localScale = new Vector3(targetWidth, targetHeight, 1f);

        // Cache values for change detection
        lastAspect = targetCamera.aspect;
        lastFOV = targetCamera.fieldOfView;
        lastCameraPosition = targetCamera.transform.position;
        lastCameraRotation = targetCamera.transform.rotation;
        lastDistanceBehindGrid = distanceBehindGrid;

        DebugLog.Info($"SkyScaler: Scaled to {targetWidth:F1}x{targetHeight:F1} units at position ({skyPosition.x:F1}, {skyPosition.y:F1}, {skyPosition.z:F1}), " +
                  $"distance from camera={distanceToSky:F1}, FOV={targetCamera.fieldOfView:F2}°");
    }

    private float GetMainTextureAspect()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return 0f;

        Texture texture = renderer.sharedMaterial != null ? renderer.sharedMaterial.mainTexture : null;
        if (texture == null) return 0f;

        if (texture.width <= 0 || texture.height <= 0) return 0f;
        return (float)texture.width / texture.height;
    }
}
