using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera system for grid-based puzzle gameplay on the XY plane.
///
/// ARCHITECTURE:
/// - Camera sits on -Z axis looking at the XY grid plane
/// - In non-play modes: static framing of full grid with calculated FOV
/// - In Play mode: tracks Lem with velocity-based look-ahead, dead zone,
///   and critically damped springs
///
/// LAYERED EFFECTS (enable one at a time for tuning):
/// Layer 1: Spring-damped position tracking with dead zone (always on)
/// Layer 2: Velocity-based look-ahead (toggle via enableLookAhead)
/// Layer 3: Auto-zoom based on movement confidence (toggle via enableAutoZoom)
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

    [Header("Layer 1: Position Tracking + Dead Zone")]
    [Tooltip("How fast the camera follows Lem (lower = snappier, higher = dreamier)")]
    [Range(0.1f, 1.5f)]
    public float trackingSpeed = 0.35f;

    [Tooltip("Dead zone size in viewport space (Lem can move this much without camera reacting)")]
    [Range(0f, 0.3f)]
    public float deadZoneSize = 0.08f;

    [Header("Layer 2: Look-Ahead")]
    [Tooltip("Enable velocity-based look-ahead (camera leads in movement direction)")]
    public bool enableLookAhead = false;

    [Tooltip("How far ahead the camera leads based on movement direction")]
    [Range(0f, 0.5f)]
    public float lookAheadAmount = 0.3f;

    [Header("Layer 3: Auto-Zoom")]
    [Tooltip("Enable automatic zoom based on movement confidence")]
    public bool enableAutoZoom = false;

    [Tooltip("How much to zoom in at full confidence (0.5 = 50% of full FOV)")]
    [Range(0.1f, 1f)]
    public float zoomFactor = 0.5f;

    [Header("Auto-Update")]
    [Tooltip("Automatically update camera when values change in the Inspector")]
    public bool autoUpdateInEditor = true;

    // --- Internal tuning constants ---

    // Velocity smoothing: 1.2s filters out ~0.4s bounce cycles
    private const float VelocitySmoothTime = 1.2f;

    // Speed threshold: smoothed velocity below 40% of walk speed = zero confidence
    private const float ConfidenceSpeedThreshold = 0.4f;

    // Confidence timing: slow build, moderate decay
    private const float ConfidenceBuildTime = 3.0f;
    private const float ConfidenceDecayTime = 1.5f;

    // Zoom FOV spring: critically damped, no overshoot
    private const float ZoomResponseTime = 0.8f;

    // Initial zoom delay on play mode entry
    private const float InitialZoomDelay = 1.5f;

    // Position spring: underdamped for visible rubber band feel.
    // At cameraDistance=150, 0.9 overshoot is invisible. 0.8 gives a subtle
    // but perceptible elastic settle that reads well at distance.
    private const float PositionDampingRatio = 0.8f;

    // Dead zone vertical = 60% of horizontal
    private const float DeadZoneVerticalRatio = 0.6f;

    // Core state
    private float baseFOV;
    private LemController trackedLem;

    // Spring-damper systems
    private SecondOrderDynamics springX;
    private SecondOrderDynamics springY;
    private SecondOrderDynamics springFOV;
    private SecondOrderDynamics velocitySmoother;

    // Auto-zoom state
    private float movementConfidence;
    private float playModeElapsed;
    private bool isInPlayMode;

    // Teleportation detection
    private Vector3 lastLemPosition;
    private bool hasLastLemPosition;

    // Play mode detection
    private BuilderController builderController;

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

    /// <summary>
    /// Returns true if the camera is currently in a zoomed-in state.
    /// </summary>
    public bool IsZoomed => isInPlayMode && enableAutoZoom && movementConfidence > 0.3f;

    private void Update()
    {
        // C key: reset camera (debug feature)
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SetupCamera();
        }

        if (targetCamera == null) return;

        float dt = Time.deltaTime;
        float distance = Mathf.Max(cameraDistance, 0.01f);

        // --- Detect play mode transitions ---
        bool wasInPlayMode = isInPlayMode;
        isInPlayMode = CheckPlayMode();

        if (isInPlayMode && !wasInPlayMode)
        {
            OnEnterPlayMode();
        }
        else if (!isInPlayMode && wasInPlayMode)
        {
            OnExitPlayMode();
        }

        // --- Compute target position and FOV ---
        float targetX, targetY;
        float targetFOVValue;

        if (isInPlayMode && TryGetLem(out LemController lem))
        {
            playModeElapsed += dt;
            Vector3 lemPos = lem.GetFootPointPosition();

            // Layer 2: Velocity-based look-ahead (if enabled)
            float lookAheadOffset = 0f;
            float smoothedVelocityX = 0f;

            if (enableLookAhead || enableAutoZoom)
            {
                float rawVelocityX = lem.GetHorizontalVelocity();
                smoothedVelocityX = velocitySmoother.Update(dt, rawVelocityX);
            }

            if (enableLookAhead)
            {
                float walkSpeed = lem.GetWalkSpeed();
                float normalizedVelocity = walkSpeed > 0.001f ? smoothedVelocityX / walkSpeed : 0f;

                // Use baseFOV (not current FOV) to decouple look-ahead from zoom state.
                // Using the live FOV creates a feedback loop: zoom changes FOV → changes
                // look-ahead magnitude → changes position → changes zoom confidence.
                float halfHeight = distance * Mathf.Tan(baseFOV * 0.5f * Mathf.Deg2Rad);
                float fullWidth = halfHeight * 2f * targetCamera.aspect;

                lookAheadOffset = normalizedVelocity * lookAheadAmount * fullWidth;
            }

            // Layer 1: Dead zone (always active)
            float rawTargetX = lemPos.x + lookAheadOffset;
            float rawTargetY = lemPos.y;
            ApplyDeadZone(rawTargetX, rawTargetY, lemPos, distance, out targetX, out targetY);

            // Layer 3: Auto-zoom (if enabled)
            if (enableAutoZoom)
            {
                float walkSpeed = lem.GetWalkSpeed();
                UpdateMovementConfidence(dt, smoothedVelocityX, walkSpeed);

                float zoomProgress = Mathf.Clamp01(playModeElapsed / InitialZoomDelay);
                zoomProgress = zoomProgress * zoomProgress * (3f - 2f * zoomProgress);

                float zoomedFOV = baseFOV * zoomFactor;
                float confidenceTarget = Mathf.Lerp(baseFOV, zoomedFOV, movementConfidence);
                targetFOVValue = Mathf.Lerp(baseFOV, confidenceTarget, zoomProgress);
            }
            else
            {
                // No auto-zoom: stay at full grid FOV
                targetFOVValue = baseFOV;
            }

            // Teleportation detection
            DetectTeleportation(lemPos);
        }
        else
        {
            // Not in play mode or no Lem: center on grid
            Vector3 gridCenter = GetGridCenter();
            targetX = gridCenter.x;
            targetY = gridCenter.y;
            targetFOVValue = baseFOV;
        }

        // --- Apply spring-damped motion ---
        float newX = springX.Update(dt, targetX);
        float newY = springY.Update(dt, targetY);
        float newFOV = springFOV.Update(dt, targetFOVValue);

        targetCamera.transform.position = new Vector3(newX, newY, -distance);
        targetCamera.fieldOfView = Mathf.Clamp(newFOV, 0.5f, 179f);
    }

    #region Play Mode Lifecycle

    private bool CheckPlayMode()
    {
        if (builderController == null)
        {
            builderController = ServiceRegistry.Get<BuilderController>(logIfMissing: false);
        }
        return builderController != null && builderController.currentMode == GameMode.Play;
    }

    private void OnEnterPlayMode()
    {
        playModeElapsed = 0f;
        movementConfidence = 0f;
        hasLastLemPosition = false;

        // Snap springs to grid center (camera starts centered on full grid)
        Vector3 gridCenter = GetGridCenter();
        springX.SnapTo(gridCenter.x);
        springY.SnapTo(gridCenter.y);
        springFOV.SnapTo(baseFOV);
        velocitySmoother.SnapTo(0f);

        // Subscribe to teleportation events
        if (TryGetLem(out LemController lem))
        {
            lem.OnTeleported -= HandleTeleported;
            lem.OnTeleported += HandleTeleported;
        }
    }

    private void OnExitPlayMode()
    {
        if (trackedLem != null)
        {
            trackedLem.OnTeleported -= HandleTeleported;
        }
        trackedLem = null;
    }

    #endregion

    #region Dead Zone

    /// <summary>
    /// Applies dead zone logic. When Lem is within the dead zone rectangle
    /// (viewport-space, centered on camera), the camera holds still. When Lem
    /// exits, the camera tracks to keep Lem at the dead zone edge.
    /// </summary>
    private void ApplyDeadZone(float rawTargetX, float rawTargetY, Vector3 lemWorldPos,
        float cameraDistance, out float targetX, out float targetY)
    {
        float dzHalfW = deadZoneSize;
        float dzHalfH = deadZoneSize * DeadZoneVerticalRatio;

        if (dzHalfW <= 0.001f)
        {
            targetX = rawTargetX;
            targetY = rawTargetY;
            return;
        }

        float camX = springX.Current;
        float camY = springY.Current;

        float currentFOV = targetCamera.fieldOfView;
        float halfHeight = cameraDistance * Mathf.Tan(currentFOV * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * targetCamera.aspect;

        float dzWorldHalfW = dzHalfW * halfWidth * 2f;
        float dzWorldHalfH = dzHalfH * halfHeight * 2f;

        float offsetX = lemWorldPos.x - camX;
        float offsetY = lemWorldPos.y - camY;

        float excessX = 0f;
        float excessY = 0f;

        if (Mathf.Abs(offsetX) > dzWorldHalfW)
        {
            excessX = offsetX - Mathf.Sign(offsetX) * dzWorldHalfW;
        }

        if (Mathf.Abs(offsetY) > dzWorldHalfH)
        {
            excessY = offsetY - Mathf.Sign(offsetY) * dzWorldHalfH;
        }

        // Always apply look-ahead offset, regardless of dead zone state.
        // The previous conditional gating (only when excessX > 0) caused oscillation:
        // look-ahead moves camera ahead → Lem falls inside dead zone → look-ahead
        // disabled → camera drifts back → Lem exits dead zone → look-ahead re-enabled → repeat.
        float lookAheadContribution = rawTargetX - lemWorldPos.x;
        targetX = camX + excessX + lookAheadContribution;
        targetY = camY + excessY;
    }

    #endregion

    #region Auto-Zoom

    /// <summary>
    /// Updates movement confidence. A speed threshold ensures bouncing
    /// (low smoothed velocity) never triggers zoom-in.
    /// </summary>
    private void UpdateMovementConfidence(float dt, float smoothedVelocityX, float walkSpeed)
    {
        float speedRatio = walkSpeed > 0.001f ? Mathf.Abs(smoothedVelocityX) / walkSpeed : 0f;

        float instantConfidence;
        if (speedRatio < ConfidenceSpeedThreshold)
        {
            instantConfidence = 0f;
        }
        else
        {
            instantConfidence = Mathf.Clamp01((speedRatio - ConfidenceSpeedThreshold) / (1f - ConfidenceSpeedThreshold));
        }

        if (instantConfidence > movementConfidence)
        {
            float buildRate = 1f / ConfidenceBuildTime;
            movementConfidence = Mathf.MoveTowards(movementConfidence, instantConfidence, buildRate * dt);
        }
        else
        {
            float decayRate = 1f / ConfidenceDecayTime;
            movementConfidence = Mathf.MoveTowards(movementConfidence, instantConfidence, decayRate * dt);
        }
    }

    #endregion

    #region Teleportation

    /// <summary>
    /// Called when Lem teleports. Instead of snapping the camera, we just
    /// reset the velocity smoother and update the last-known position so the
    /// teleport detection doesn't re-fire. The position springs will naturally
    /// pan the camera to the new location — at cameraDistance=150 this reads
    /// as a smooth, intentional camera move rather than a jarring cut.
    /// </summary>
    private void HandleTeleported(Vector3 newPosition)
    {
        velocitySmoother.SnapTo(0f);
        lastLemPosition = newPosition;
        hasLastLemPosition = true;
        // Position springs NOT snapped — camera pans smoothly to new target
    }

    /// <summary>
    /// Detects large position jumps (>2 world units in one frame) as teleports.
    /// Updates lastLemPosition so the jump doesn't re-trigger next frame.
    /// </summary>
    private void DetectTeleportation(Vector3 lemPos)
    {
        if (hasLastLemPosition)
        {
            float delta = Vector3.Distance(lemPos, lastLemPosition);
            if (delta > 2f)
            {
                HandleTeleported(lemPos);
                return;
            }
        }
        lastLemPosition = lemPos;
        hasLastLemPosition = true;
    }

    #endregion

    #region Lem Lookup

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

    #endregion

    #region Camera Setup

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
        targetCamera.transform.rotation = Quaternion.LookRotation(
            gridCenter - targetCamera.transform.position, Vector3.up);

        float fov = CalculateFieldOfView(distance);
        targetCamera.fieldOfView = fov;
        baseFOV = fov;

        // Reset all tracking state
        isInPlayMode = false;
        movementConfidence = 0f;
        playModeElapsed = 0f;
        hasLastLemPosition = false;
        trackedLem = null;

        // Initialize springs
        springX = new SecondOrderDynamics(trackingSpeed, PositionDampingRatio, gridCenter.x);
        springY = new SecondOrderDynamics(trackingSpeed, PositionDampingRatio, gridCenter.y);
        springFOV = new SecondOrderDynamics(ZoomResponseTime, 1.0f, fov);
        velocitySmoother = new SecondOrderDynamics(VelocitySmoothTime, 1.0f, 0f);
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

    #endregion

    #region Editor

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

    [ContextMenu("Refresh Camera Setup")]
    private void RefreshCameraSetup()
    {
        SetupCamera();
    }

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

        // Play mode: dead zone and confidence visualization
        if (!isInPlayMode) return;

        float dist = Mathf.Max(cameraDistance, 0.01f);
        float currentFOV = targetCamera.fieldOfView;
        float halfH = dist * Mathf.Tan(currentFOV * 0.5f * Mathf.Deg2Rad);
        float halfW = halfH * targetCamera.aspect;

        float dzW = deadZoneSize * halfW * 2f;
        float dzH = deadZoneSize * DeadZoneVerticalRatio * halfH * 2f;
        Vector3 camCenter = new Vector3(springX.Current, springY.Current, 0f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(camCenter, new Vector3(dzW * 2f, dzH * 2f, 0.1f));

        if (enableAutoZoom)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.green, movementConfidence);
            Vector3 barStart = camCenter + Vector3.down * (dzH + 0.5f);
            Vector3 barEnd = barStart + Vector3.right * movementConfidence * 2f;
            Gizmos.DrawLine(barStart, barEnd);
        }
    }
#endif

    #endregion
}
