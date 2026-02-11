using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple camera framing for a grid on the XY plane.
/// - Camera sits directly in front of the grid on -Z
/// - Always looks at the grid center
/// - FOV is calculated to fit the full grid at a fixed distance
/// </summary>
public class CameraSetup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The grid manager to frame in the camera view")]
    public GridManager gridManager;

    [Tooltip("The camera to configure (defaults to Main Camera if not set)")]
    public Camera targetCamera;

    [Header("Framing")]
    [Tooltip("Fixed distance from the grid center (large distance = flatter perspective)")]
    [Range(1f, 2000f)]
    public float cameraDistance = 150f;

    [Tooltip("Margin around the grid in world units")]
    [Range(0f, 5f)]
    public float gridMargin = 1f;

    [Header("Clip Planes")]
    [Range(0.01f, 10f)]
    public float nearClipPlane = 0.24f;

    [Range(100f, 5000f)]
    public float farClipPlane = 500f;

    [Header("Zoom Test")]
    [Tooltip("How much to zoom in (0.5 = 50% of full FOV)")]
    [Range(0.1f, 1f)]
    public float zoomFactor = 0.5f;

    [Tooltip("Smooth transition speed (higher = faster)")]
    [Range(1f, 20f)]
    public float zoomSmoothSpeed = 6f;

    [Header("Lem Tracking (active when zoomed)")]
    [Tooltip("Lem's horizontal viewport position behind them (0.2 = 20% from trailing edge)")]
    [Range(0.05f, 0.5f)]
    public float lemViewportX = 0.2f;

    [Tooltip("How fast the camera follows Lem's position")]
    [Range(1f, 20f)]
    public float trackingSmoothSpeed = 5f;

    [Tooltip("How fast the look-ahead flips when Lem changes direction")]
    [Range(0.01f, 10f)]
    public float directionSmoothSpeed = 4f;

    [Header("Zoom UI")]
    [Tooltip("Font for the zoom toggle button. Assign Koulen-Regular or leave empty for fallback.")]
    [SerializeField] private Font zoomButtonFont;

    [Header("Auto-Update")]
    [Tooltip("Automatically update camera when values change in the Inspector")]
    public bool autoUpdateInEditor = true;

    private float baseFOV;
    private float targetFOV;
    private bool isZoomed;
    private LemController trackedLem;
    private float smoothedDirectionSign = 1f; // 1 = facing right, -1 = facing left

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

        // Spawn the zoom toggle button (self-contained overlay)
        GameObject uiObj = new GameObject("ZoomToggleUI");
        uiObj.transform.SetParent(transform, false);
        ZoomToggleUI zoomUI = uiObj.AddComponent<ZoomToggleUI>();
        zoomUI.Initialize(this, zoomButtonFont);
    }

    /// <summary>
    /// Returns true if the camera is currently zoomed in.
    /// </summary>
    public bool IsZoomed => isZoomed;

    /// <summary>
    /// Toggles between zoomed and unzoomed states.
    /// Called by ZoomToggleUI button and Z keyboard shortcut.
    /// </summary>
    public void ToggleZoom()
    {
        isZoomed = !isZoomed;
        targetFOV = isZoomed ? baseFOV * zoomFactor : baseFOV;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SetupCamera();
        }

        // Z toggles zoom
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            ToggleZoom();
        }

        if (targetCamera == null) return;

        // Smooth FOV toward target
        if (!Mathf.Approximately(targetCamera.fieldOfView, targetFOV))
        {
            float t = 1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, t);
        }

        // --- Lem tracking (when zoomed) ---
        Vector3 targetPos;
        float distance = Mathf.Max(cameraDistance, 0.01f);

        if (isZoomed && TryGetLem(out LemController lem))
        {
            // Smoothly interpolate the facing direction sign
            float targetDir = lem.GetFacingRight() ? 1f : -1f;
            float dirT = 1f - Mathf.Exp(-directionSmoothSpeed * Time.deltaTime);
            smoothedDirectionSign = Mathf.Lerp(smoothedDirectionSign, targetDir, dirT);

            // Visible width at the grid plane using current (animated) FOV
            float currentFOV = targetCamera.fieldOfView;
            float halfHeight = distance * Mathf.Tan(currentFOV * 0.5f * Mathf.Deg2Rad);
            float fullWidth = halfHeight * 2f * targetCamera.aspect;

            // Offset so Lem sits at lemViewportX behind them, 80% ahead
            float hOffset = (0.5f - lemViewportX) * fullWidth * smoothedDirectionSign;

            Vector3 lemPos = lem.GetFootPointPosition();
            targetPos = new Vector3(lemPos.x + hOffset, lemPos.y, -distance);
        }
        else
        {
            // Default: center on grid
            Vector3 gridCenter = GetGridCenter();
            targetPos = gridCenter + new Vector3(0f, 0f, -distance);
        }

        float posT = 1f - Mathf.Exp(-trackingSmoothSpeed * Time.deltaTime);
        targetCamera.transform.position = Vector3.Lerp(
            targetCamera.transform.position,
            targetPos,
            posT
        );
    }

    private bool TryGetLem(out LemController lem)
    {
        if (trackedLem != null)
        {
            lem = trackedLem;
            return true;
        }

        var lems = Object.FindObjectsByType<LemController>(FindObjectsSortMode.None);
        if (lems.Length > 0)
        {
            trackedLem = lems[0];
            lem = trackedLem;
            return true;
        }

        lem = null;
        return false;
    }

#if UNITY_EDITOR
    private float lastCameraDistance;
    private float lastGridMargin;
    private float lastNearClipPlane;
    private float lastFarClipPlane;

    private void LateUpdate()
    {
        if (Application.isPlaying && autoUpdateInEditor)
        {
            bool settingsChanged =
                !Mathf.Approximately(lastCameraDistance, cameraDistance) ||
                !Mathf.Approximately(lastGridMargin, gridMargin) ||
                !Mathf.Approximately(lastNearClipPlane, nearClipPlane) ||
                !Mathf.Approximately(lastFarClipPlane, farClipPlane);

            if (settingsChanged)
            {
                SetupCamera();
                lastCameraDistance = cameraDistance;
                lastGridMargin = gridMargin;
                lastNearClipPlane = nearClipPlane;
                lastFarClipPlane = farClipPlane;
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

        targetCamera.orthographic = false;
        targetCamera.nearClipPlane = nearClipPlane;
        targetCamera.farClipPlane = farClipPlane;

        float distance = Mathf.Max(cameraDistance, 0.01f);
        Vector3 gridCenter = GetGridCenter();

        targetCamera.transform.position = gridCenter + new Vector3(0f, 0f, -distance);
        targetCamera.transform.rotation = Quaternion.LookRotation(gridCenter - targetCamera.transform.position, Vector3.up);

        float fov = CalculateFieldOfView(distance);
        targetCamera.fieldOfView = fov;
        baseFOV = fov;
        targetFOV = fov;
        isZoomed = false;
    }

    private float CalculateFieldOfView(float distance)
    {
        float gridWorldWidth = gridManager.gridWidth + (gridMargin * 2f);
        float gridWorldHeight = gridManager.gridHeight + (gridMargin * 2f);

        float aspect = targetCamera.aspect;
        if (aspect <= 0 || float.IsNaN(aspect) || float.IsInfinity(aspect))
        {
            Debug.LogWarning("Invalid camera aspect ratio, using fallback 16:9");
            aspect = 16f / 9f;
        }

        float verticalFovForHeight = 2f * Mathf.Atan((gridWorldHeight / 2f) / distance) * Mathf.Rad2Deg;
        float verticalFovForWidth = 2f * Mathf.Atan((gridWorldWidth / 2f) / (distance * aspect)) * Mathf.Rad2Deg;

        float fov = Mathf.Max(verticalFovForHeight, verticalFovForWidth);
        return Mathf.Clamp(fov, 0.5f, 179f);
    }

    public void RefreshCamera()
    {
        SetupCamera();
    }

    public LevelData.CameraSettings ExportSettings()
    {
        return new LevelData.CameraSettings
        {
            cameraDistance = cameraDistance,
            gridMargin = gridMargin,
            nearClipPlane = nearClipPlane,
            farClipPlane = farClipPlane
        };
    }

    public void ImportSettings(LevelData.CameraSettings settings)
    {
        if (settings == null) return;

        cameraDistance = settings.cameraDistance > 0f ? settings.cameraDistance : cameraDistance;
        gridMargin = settings.gridMargin;
        nearClipPlane = settings.nearClipPlane;
        farClipPlane = settings.farClipPlane;

        SetupCamera();
    }

    private Vector3 GetGridCenter()
    {
        if (gridManager == null) return Vector3.zero;
        return gridManager.gridOrigin + new Vector3(gridManager.gridWidth / 2f, gridManager.gridHeight / 2f, 0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (gridManager == null || targetCamera == null) return;

        Vector3 gridCenter = GetGridCenter();

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(gridCenter, 0.5f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(targetCamera.transform.position, gridCenter);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(targetCamera.transform.position, targetCamera.transform.forward * 5f);
    }
#endif
}
