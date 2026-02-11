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

    [Header("Auto-Update")]
    [Tooltip("Automatically update camera when values change in the Inspector")]
    public bool autoUpdateInEditor = true;

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
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SetupCamera();
        }
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

        targetCamera.fieldOfView = CalculateFieldOfView(distance);
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
