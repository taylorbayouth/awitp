using UnityEngine;

/// <summary>
/// Scales and positions a sky quad to always fill the camera view.
/// Designed for 2D/2.5D puzzle games where the camera pans/zooms minimally.
/// Keeps the sky image undistorted and always visible behind the grid.
/// </summary>
public class SkyScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera to frame against (defaults to Main Camera)")]
    public Camera targetCamera;

    [Header("Positioning")]
    [Tooltip("Distance behind the grid (positive Z from grid center at origin)")]
    [Range(5f, 50f)]
    public float distanceBehindGrid = 20f;

    [Header("Scaling")]
    [Tooltip("Scale multiplier for full bleed coverage (1.05 = 5% overscan)")]
    [Range(1.0f, 1.2f)]
    public float overscanMultiplier = 1.05f;

    [Header("Auto-Update")]
    [Tooltip("Automatically update when camera settings change")]
    public bool autoUpdate = true;

    private float lastAspect;
    private float lastFOV;
    private float lastCameraZ;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindAnyObjectByType<Camera>();
            }
        }
    }

    private void Start()
    {
        UpdateSkyPlane();
    }

    private void LateUpdate()
    {
        if (!autoUpdate) return;

        // Check if camera settings changed
        bool cameraChanged =
            !Mathf.Approximately(lastAspect, targetCamera.aspect) ||
            !Mathf.Approximately(lastFOV, targetCamera.fieldOfView) ||
            !Mathf.Approximately(lastCameraZ, targetCamera.transform.position.z);

        if (cameraChanged)
        {
            UpdateSkyPlane();
        }
    }

    /// <summary>
    /// Updates the sky plane position and scale to fill camera view with overscan.
    /// Called automatically when camera settings change.
    /// </summary>
    [ContextMenu("Update Sky Plane")]
    public void UpdateSkyPlane()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("SkyScaler: No target camera assigned");
            return;
        }

        // Position sky plane behind the grid (grid is at world origin Z=0)
        transform.position = new Vector3(0f, 0f, distanceBehindGrid);

        // Calculate distance from camera to sky plane
        float distanceFromCamera = distanceBehindGrid - targetCamera.transform.position.z;

        // Ensure distance is positive
        if (distanceFromCamera <= 0)
        {
            Debug.LogWarning($"SkyScaler: Sky is in front of or at camera position. Adjust distanceBehindGrid. Camera Z={targetCamera.transform.position.z}, Sky Z={distanceBehindGrid}");
            distanceFromCamera = Mathf.Abs(distanceFromCamera) + 1f;
        }

        // Calculate visible dimensions at the sky plane's distance
        float fovRadians = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float viewHeight = 2f * distanceFromCamera * Mathf.Tan(fovRadians / 2f);
        float viewWidth = viewHeight * targetCamera.aspect;

        // Apply overscan multiplier for full bleed
        viewHeight *= overscanMultiplier;
        viewWidth *= overscanMultiplier;

        // Unity Quad is 1x1 units by default, so scale directly
        transform.localScale = new Vector3(viewWidth, viewHeight, 1f);

        // Ensure quad faces the camera (rotation should be identity for XY plane)
        transform.rotation = Quaternion.identity;

        // Cache values for change detection
        lastAspect = targetCamera.aspect;
        lastFOV = targetCamera.fieldOfView;
        lastCameraZ = targetCamera.transform.position.z;

        DebugLog.Info($"SkyScaler: Scaled to {viewWidth:F1}x{viewHeight:F1} units at Z={distanceBehindGrid}, distance from camera={distanceFromCamera:F1}");
    }
}
