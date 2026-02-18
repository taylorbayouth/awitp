using UnityEngine;
using System;

/// <summary>
/// Autonomous walking character ("Lem") for side-scrolling puzzle gameplay.
///
/// BEHAVIOR:
/// - Walks continuously in facing direction until hitting wall or edge
/// - Turns around when hitting walls (detected via raycast)
/// - Falls through gaps (no edge detection by design - like classic Lemmings)
/// - Dies when falling outside camera bounds
/// - Can be frozen/unfrozen for editor vs play mode
///
/// PHYSICS:
/// - Uses Rigidbody with continuous collision detection
/// - Custom PhysicMaterial with zero friction to prevent sticking
/// - Constrained to XY plane (Z position and rotation frozen)
/// - Height is scaled to 95% of grid cell size on spawn
///
/// DESIGN NOTES:
/// - Raycasts use QueryTriggerInteraction.Ignore to avoid detecting block trigger zones
/// - Ground check starts slightly above center to avoid missing ground
/// - Wall/ground check distances are tuned for half-scale Lem
/// </summary>
public class LemController : MonoBehaviour
{
    private const float DefaultLemHeight = 0.95f;
    private const float ClockwiseTurnDegrees = -180f;
    private static PhysicsMaterial sharedLemPhysicsMaterial;
    private static GameObject cachedLemPrefab;
    private static bool loggedMissingLemPrefab;

    [System.Serializable]
    public class FootstepSurface
    {
        public BlockType blockType = BlockType.Walk;
        public AudioClip clip;
        [Tooltip("Volume range for slight variation (min..max).")]
        public Vector2 volumeRange = new Vector2(0.9f, 1f);
        [Tooltip("Pitch range for slight variation (min..max).")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    }

    #region Inspector Fields

    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units per second")]
    [SerializeField] private float walkSpeed = .8f;
    [Header("Dimensions")]
    [Tooltip("Overall Lem height in world units (controls collider and visual fit).")]
    [SerializeField] private float lemHeight = DefaultLemHeight;
    [Tooltip("How quickly Lem accelerates toward target horizontal speed")]
    [SerializeField] private float horizontalAcceleration = 7f;
    [Tooltip("How quickly Lem slows when reversing direction or stopping")]
    [SerializeField] private float horizontalDeceleration = 11f;
    [Tooltip("Temporary slowdown after bumping a wall")]
    [SerializeField] [Range(0.1f, 1f)] private float wallBumpSlowdownMultiplier = 0.7f;
    [Tooltip("Slowdown duration after bumping a wall")]
    [SerializeField] private float wallBumpSlowdownDuration = 0.12f;
    [Tooltip("Duration of clockwise turn when Lem changes direction")]
    [SerializeField] private float turnDuration = 0.25f;

    [Header("Detection")]
    [Tooltip("Raycast distance for wall detection")]
    [SerializeField] private float wallCheckDistance = 0.3f;

    [Tooltip("Raycast distance for ground detection")]
    [SerializeField] private float groundCheckDistance = 1.0f;

    [Tooltip("Raycast distance for detecting interactive block centers ahead (teleporter/transporter)")]
    [SerializeField] private float interactionLookAheadDistance = 0.4f;

    [Tooltip("Layer mask for solid objects (blocks). Default: everything")]
    [SerializeField] private LayerMask solidLayerMask = ~0;

    [Header("Fall Arc")]
    [Tooltip("How far forward (X) to move during the fall (0.5 = center of next cell)")]
    [SerializeField] private float fallArcForwardDistance = 0.5f;

    [Tooltip("Horizontal speed during the fall arc (units per second)")]
    [SerializeField] private float fallArcHorizontalSpeed = 0.8f;

    [Tooltip("Time to smoothly ramp up to full speed at start (seconds)")]
    [SerializeField] private float fallArcEaseInTime = 0.08f;

    [Tooltip("Time to smoothly ramp down to zero at handoff (seconds)")]
    [SerializeField] private float fallArcEaseOutTime = 0.08f;

    [Header("State")]
    [Tooltip("True if facing right, false if facing left")]
    [SerializeField] private bool facingRight = true;

    [Tooltip("True if Lem is standing on solid ground")]
    [SerializeField] private bool isGrounded = false;

    [Tooltip("False if Lem has died (fell off screen)")]
    [SerializeField] private bool isAlive = true;

    [Tooltip("True = no movement (Designer mode), False = active walking (play mode)")]
    [SerializeField] private bool isFrozen = true;

    [Tooltip("True when Lem is in the controlled fall arc")]
    [SerializeField] private bool isFallingArc = false;

    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private FootstepSurface[] footstepSurfaces = System.Array.Empty<FootstepSurface>();
    [SerializeField] private AudioClip footstepFallbackClip;
    [SerializeField] private Vector2 footstepFallbackVolumeRange = new Vector2(0.9f, 1f);
    [SerializeField] private Vector2 footstepFallbackPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Interaction Stops")]
    [Tooltip("How long movement takes to tween to zero for transporter/teleporter pauses")]
    [SerializeField] private float interactionStopTweenDuration = 0.2f;

    // Cached component references
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform footPoint;
    private CharacterVisual characterVisual;

    // Fall arc tracking
    private bool wasGroundedLastFrame = true;
    private float fallArcTargetX;
    private float fallArcDirectionX;
    private float fallArcElapsed = 0f;
    private bool fallArcHandoffStarted = false;
    private float fallArcHandoffElapsed = 0f;
    private float wallBumpTimer = 0f;
    private bool isExternalStopActive = false;
    private bool isTurningAround = false;
    private Coroutine turnRoutine;
    private float lastAppliedLemHeight = -1f;

    private bool defaultUseGravity;
    private bool defaultIsKinematic;
    private BaseBlock currentBlock;
    private BlockType currentBlockType = BlockType.Walk;
    private bool hasCurrentBlock;

    #endregion

    void Awake()
    {
        if (gameObject.tag != GameConstants.Tags.Player)
        {
            gameObject.tag = GameConstants.Tags.Player;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        ApplyLemDimensions();

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        defaultUseGravity = rb.useGravity;
        defaultIsKinematic = rb.isKinematic;

        CapsuleCollider collider = GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            collider.material = GetSharedLemPhysicsMaterial();
        }

        // Initialize visual system (mesh, materials, animation)
        characterVisual = GetComponent<CharacterVisual>();
        if (characterVisual == null)
        {
            characterVisual = gameObject.AddComponent<CharacterVisual>();
        }
        float targetHeight = capsuleCollider != null ? capsuleCollider.height : DefaultLemHeight;
        characterVisual.Setup(targetHeight, this);

        footPoint = transform.Find("FootPoint");
        if (footPoint == null)
        {
            footPoint = transform;
        }

        if (footstepSource == null)
        {
            footstepSource = GetComponent<AudioSource>();
            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
            }
        }
        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.spatialBlend = 0f;
    }

    void FixedUpdate()
    {
        if (!isAlive) return;
        if (isFrozen) return;

        if (wallBumpTimer > 0f)
        {
            wallBumpTimer -= Time.fixedDeltaTime;
        }

        // Store previous grounded state before checking
        bool wasGrounded = isGrounded;
        CheckGround();

        // During fall arc, control horizontal movement toward target X
        if (isFallingArc)
        {
            UpdateFallArc();
            wasGroundedLastFrame = isGrounded;
            return;
        }

        // Detect grounded → airborne transition (stepped off an edge)
        if (wasGrounded && !isGrounded && wasGroundedLastFrame)
        {
            StartFallArc();
            wasGroundedLastFrame = false;
            return;
        }

        wasGroundedLastFrame = isGrounded;

        if (isExternalStopActive || isTurningAround)
        {
            return;
        }

        if (isGrounded)
        {
            CheckWallAhead();
            Walk();
        }
    }

    void Update()
    {
        if (!Mathf.Approximately(lemHeight, lastAppliedLemHeight))
        {
            ApplyLemDimensions();
        }

        // Die if fallen outside camera bounds.
        // Skip while frozen — game systems (teleporter, transporter) move Lem
        // programmatically and the camera may not have caught up yet.
        if (!isFrozen && IsOutsideCameraBounds())
        {
            Die();
        }

        if (characterVisual != null)
        {
            characterVisual.UpdateVisualRotationIfNeeded();
        }

        UpdateAnimation();
    }

    private bool IsOutsideCameraBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Vector3 pos = transform.position;
        float buffer = 2f;

        // For orthographic camera on XY plane
        if (mainCamera.orthographic)
        {
            float orthoHeight = mainCamera.orthographicSize;
            float orthoWidth = orthoHeight * mainCamera.aspect;

            // Camera is at (0,0,-Z) looking at (0,0,0), so visible bounds are:
            float left = -orthoWidth;
            float right = orthoWidth;
            float bottom = -orthoHeight;
            float top = orthoHeight;

            return pos.x < left - buffer ||
                   pos.x > right + buffer ||
                   pos.y < bottom - buffer ||
                   pos.y > top + buffer;
        }
        else
        {
            // For perspective camera, convert world position to viewport space
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(pos);

            // Viewport coordinates: (0,0) = bottom-left, (1,1) = top-right
            // Also check if behind camera (z < 0)
            float viewportBuffer = 0.2f; // 20% buffer outside viewport
            return viewportPos.z < 0 ||
                   viewportPos.x < -viewportBuffer ||
                   viewportPos.x > 1f + viewportBuffer ||
                   viewportPos.y < -viewportBuffer ||
                   viewportPos.y > 1f + viewportBuffer;
        }
    }

    private void Walk()
    {
        float direction = facingRight ? 1f : -1f;
        float targetSpeed = walkSpeed * GetWallBumpSpeedMultiplier();
        MoveHorizontalToward(direction * targetSpeed, Time.fixedDeltaTime);
    }

    private void CheckGround()
    {
        // Raycast from just above the foot point for precise ground detection
        Vector3 origin = GetFootPointPosition() + Vector3.up * 0.01f;
        // IMPORTANT: Use QueryTriggerInteraction.Ignore to ignore trigger colliders (like BaseBlock detection zones)
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, solidLayerMask, QueryTriggerInteraction.Ignore);
    }

    private void CheckWallAhead()
    {
        float checkHeight = GetLemHeight() * 0.5f;
        Vector3 origin = GetFootPointPosition() + Vector3.up * checkHeight;
        Vector3 direction = facingRight ? Vector3.right : Vector3.left;

        // Ignore triggers to avoid detecting BaseBlock detection zones
        if (Physics.Raycast(origin, direction, wallCheckDistance, solidLayerMask, QueryTriggerInteraction.Ignore))
        {
            TurnAround();
        }
    }

    /// <summary>
    /// Checks if there's an interactive block center (teleporter/transporter) ahead.
    /// Returns distance to the center, or -1 if none found.
    /// Used to anticipate animation slowdown before reaching the block.
    /// </summary>
    private float CheckInteractiveBlockAhead()
    {
        if (!isGrounded) return -1f;

        Vector3 origin = GetFootPointPosition();
        Vector3 direction = facingRight ? Vector3.right : Vector3.left;

        // This raycast MUST detect triggers to find the block center trigger collider
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, interactionLookAheadDistance, solidLayerMask, QueryTriggerInteraction.Collide))
        {
            // Check if this is a trigger collider (the center sphere)
            if (hit.collider != null && hit.collider.isTrigger)
            {
                // Get the BaseBlock component from the hit collider
                BaseBlock block = hit.collider.GetComponentInParent<BaseBlock>();
                if (block != null)
                {
                    // Only slow for blocks that stop the Lem (teleporter, transporter)
                    BlockType blockType = block.blockType;
                    if (blockType == BlockType.Teleporter || blockType == BlockType.Transporter)
                    {
                        return hit.distance;
                    }
                }
            }
        }

        return -1f;
    }

    /// <summary>
    /// Starts the controlled fall arc when Lem steps off a ledge.
    /// Physics stays active - we only control horizontal velocity.
    /// </summary>
    private void StartFallArc()
    {
        if (isFallingArc) return;

        isFallingArc = true;
        fallArcDirectionX = facingRight ? 1f : -1f;
        fallArcTargetX = transform.position.x + (fallArcForwardDistance * fallArcDirectionX);
        fallArcElapsed = 0f;
        fallArcHandoffStarted = false;
        fallArcHandoffElapsed = 0f;

        // Start with zero horizontal velocity (will ease in)
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    /// <summary>
    /// Updates the fall arc each FixedUpdate.
    /// Eases in at start, maintains speed, then eases out at handoff.
    /// </summary>
    private void UpdateFallArc()
    {
        fallArcElapsed += Time.fixedDeltaTime;

        float currentX = transform.position.x;
        bool reachedTarget = (fallArcDirectionX > 0 && currentX >= fallArcTargetX) ||
                             (fallArcDirectionX < 0 && currentX <= fallArcTargetX);

        // Start handoff when reached target
        if (reachedTarget && !fallArcHandoffStarted)
        {
            fallArcHandoffStarted = true;
            fallArcHandoffElapsed = 0f;
        }

        // During handoff, ease out velocity
        if (fallArcHandoffStarted)
        {
            fallArcHandoffElapsed += Time.fixedDeltaTime;

            if (fallArcHandoffElapsed >= fallArcEaseOutTime)
            {
                // Handoff complete
                EndFallArc();
                return;
            }

            // Ease out: velocity goes from full → 0
            float t = fallArcHandoffElapsed / fallArcEaseOutTime;
            float speed = fallArcHorizontalSpeed * (1f - t) * fallArcDirectionX;
            rb.linearVelocity = new Vector3(speed, rb.linearVelocity.y, 0f);
            return;
        }

        // End immediately if landed before handoff complete
        if (isGrounded)
        {
            EndFallArc();
            return;
        }

        // Calculate horizontal velocity with ease-in at start
        float horizontalSpeed;
        if (fallArcElapsed < fallArcEaseInTime)
        {
            // Ease in: velocity goes from 0 → full
            float t = fallArcElapsed / fallArcEaseInTime;
            horizontalSpeed = fallArcHorizontalSpeed * t;
        }
        else
        {
            // Full speed
            horizontalSpeed = fallArcHorizontalSpeed;
        }

        rb.linearVelocity = new Vector3(
            horizontalSpeed * fallArcDirectionX,
            rb.linearVelocity.y,
            0f
        );
    }

    /// <summary>
    /// Ends the fall arc and zeros horizontal velocity.
    /// </summary>
    private void EndFallArc()
    {
        isFallingArc = false;
        fallArcHandoffStarted = false;

        // Zero horizontal velocity - Lem falls straight down from here
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void TurnAround()
    {
        if (Application.isPlaying && !isFrozen && isAlive && !isFallingArc)
        {
            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
            }
            turnRoutine = StartCoroutine(TurnAroundClockwiseRoutine());
            return;
        }

        TurnAroundImmediate();
    }

    public bool GetFacingRight()
    {
        return facingRight;
    }

    public void SetFacingRight(bool facing)
    {
        facingRight = facing;
        UpdateFacingVisuals();
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (rb == null) return;

        if (frozen)
        {
            isExternalStopActive = false;
            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
                turnRoutine = null;
            }
            isTurningAround = false;

            // Cancel any active fall arc
            isFallingArc = false;
            fallArcHandoffStarted = false;
            wasGroundedLastFrame = true;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero; // Stop movement when frozen
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = defaultIsKinematic;
            rb.useGravity = defaultUseGravity;

            // Sync ground state immediately to prevent spurious fall arc.
            // Without this, stale isGrounded from before freeze combined with
            // a one-frame physics miss triggers StartFallArc() on the first
            // FixedUpdate, giving Lem unexpected horizontal momentum.
            CheckGround();
            wasGroundedLastFrame = isGrounded;
        }
    }

    public float GetInteractionStopTweenDuration()
    {
        return Mathf.Max(0f, interactionStopTweenDuration);
    }

    public System.Collections.IEnumerator TweenHorizontalSpeedToZero(float duration = -1f)
    {
        if (rb == null) yield break;

        float tweenDuration = duration >= 0f ? duration : interactionStopTweenDuration;
        tweenDuration = Mathf.Max(0f, tweenDuration);
        if (tweenDuration <= 0.0001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            yield break;
        }

        isExternalStopActive = true;
        try
        {
            float startSpeed = rb.linearVelocity.x;
            float elapsed = 0f;

            while (elapsed < tweenDuration && rb != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tweenDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 2f);
                float speed = Mathf.Lerp(startSpeed, 0f, easedT);
                rb.linearVelocity = new Vector3(speed, rb.linearVelocity.y, 0f);
                yield return null;
            }

            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
        finally
        {
            isExternalStopActive = false;
        }
    }

    private void UpdateFacingVisuals()
    {
        float yRotation = facingRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        characterVisual?.ApplyVisualRotation();
    }


    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        isExternalStopActive = false;
        if (turnRoutine != null)
        {
            StopCoroutine(turnRoutine);
            turnRoutine = null;
        }
        isTurningAround = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Wait 2 seconds before exiting play mode
        StartCoroutine(DieAfterDelay(2f));
    }

    private System.Collections.IEnumerator DieAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Exit play mode - this will restore the snapshot which resets Lem position
        BuilderController builderController = ServiceRegistry.Get<BuilderController>();
        if (builderController != null)
        {
            builderController.ExitPlayMode();
            // Don't destroy gameObject - RestorePlayModeSnapshot will handle cleanup and restoration
        }
        else
        {
            // Fallback if no editor controller (shouldn't happen in normal gameplay)
            Destroy(gameObject, 0.1f);
        }
    }

    public Vector3 GetFootPointPosition()
    {
        return footPoint != null ? footPoint.position : transform.position;
    }

    public void SetFootPointPosition(Vector3 position)
    {
        if (rb != null && rb.isKinematic == false)
        {
            rb.position = position;
        }
        else
        {
            transform.position = position;
        }
    }

    private float GetLemHeight()
    {
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }

        if (capsuleCollider != null)
        {
            return capsuleCollider.height * transform.localScale.y;
        }

        return 1f;
    }

    public void NotifyEnteredBlock(BaseBlock block)
    {
        if (block == null) return;
        currentBlock = block;
        currentBlockType = block.blockType;
        hasCurrentBlock = true;
    }

    public void NotifyExitedBlock(BaseBlock block)
    {
        if (block == null) return;
        if (currentBlock == block)
        {
            currentBlock = null;
            hasCurrentBlock = false;
        }
    }

    // Animation events (call these from the walk animation)
    public void FootstepLeft()
    {
        PlayFootstep();
    }

    public void FootstepRight()
    {
        PlayFootstep();
    }

    private void PlayFootstep()
    {
        if (!isGrounded) return;
        if (footstepSource == null) return;

        if (hasCurrentBlock && currentBlock == null)
        {
            hasCurrentBlock = false;
            currentBlockType = BlockType.Walk;
        }

        BlockType type = hasCurrentBlock ? currentBlockType : BlockType.Walk;
        AudioClip clip = null;
        Vector2 volumeRange = footstepFallbackVolumeRange;
        Vector2 pitchRange = footstepFallbackPitchRange;

        if (TryGetFootstepSurface(type, out FootstepSurface surface))
        {
            clip = surface.clip;
            volumeRange = surface.volumeRange;
            pitchRange = surface.pitchRange;
        }
        else
        {
            clip = footstepFallbackClip;
        }

        if (clip == null) return;

        float pitchMin = Mathf.Min(pitchRange.x, pitchRange.y);
        float pitchMax = Mathf.Max(pitchRange.x, pitchRange.y);
        float volumeMin = Mathf.Min(volumeRange.x, volumeRange.y);
        float volumeMax = Mathf.Max(volumeRange.x, volumeRange.y);

        footstepSource.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        float volume = UnityEngine.Random.Range(volumeMin, volumeMax);
        footstepSource.PlayOneShot(clip, volume);
    }

    private bool TryGetFootstepSurface(BlockType type, out FootstepSurface surface)
    {
        if (footstepSurfaces == null || footstepSurfaces.Length == 0)
        {
            surface = null;
            return false;
        }

        for (int i = 0; i < footstepSurfaces.Length; i++)
        {
            FootstepSurface entry = footstepSurfaces[i];
            if (entry != null && entry.blockType == type)
            {
                surface = entry;
                return true;
            }
        }

        surface = null;
        return false;
    }


    /// <summary>
    /// Creates a Lem character with its foot point placed at the specified position.
    /// Prefers a prefab at Resources/Characters/Lem for editor-tuned settings.
    /// Falls back to programmatic construction if prefab is missing.
    /// </summary>
    public static GameObject CreateLem(Vector3 position)
    {
        GameObject lemPrefab = LoadLemPrefab();
        if (lemPrefab != null)
        {
            GameObject lemFromPrefab = Instantiate(lemPrefab, position, Quaternion.identity);
            return FinalizeSpawnedLem(lemFromPrefab, position);
        }

        if (!loggedMissingLemPrefab)
        {
            loggedMissingLemPrefab = true;
            DebugLog.Info("[LemController] No Lem prefab found at Resources/Characters/Lem. Using code-built Lem fallback.");
        }

        GameObject lem = new GameObject("Lem");
        return FinalizeSpawnedLem(lem, position);
    }

    private static GameObject FinalizeSpawnedLem(GameObject lem, Vector3 footPosition)
    {
        if (lem == null) return null;

        lem.name = "Lem";
        lem.tag = GameConstants.Tags.Player;
        lem.transform.position = footPosition;
        lem.transform.localScale = Vector3.one;

        EnsureFootPoint(lem.transform);
        EnsurePhysicsComponents(lem);

        LemController controller = lem.GetComponent<LemController>();
        if (controller == null)
        {
            controller = lem.AddComponent<LemController>();
        }

        controller.isFrozen = true;
        controller.UpdateFacingVisuals();
        return lem;
    }

    private static void EnsureFootPoint(Transform lemRoot)
    {
        if (lemRoot == null) return;

        Transform foot = lemRoot.Find("FootPoint");
        if (foot == null)
        {
            foot = new GameObject("FootPoint").transform;
            foot.SetParent(lemRoot, false);
        }

        foot.localPosition = Vector3.zero;
        foot.localRotation = Quaternion.identity;
        foot.localScale = Vector3.one;
    }

    private static void EnsurePhysicsComponents(GameObject lem)
    {
        if (lem == null) return;

        if (lem.GetComponent<Rigidbody>() == null)
        {
            lem.AddComponent<Rigidbody>();
        }

        if (lem.GetComponent<CapsuleCollider>() == null)
        {
            float cellSize = GameConstants.Grid.CellSize;
            float height = cellSize * DefaultLemHeight;
            float lemRadius = height * 0.25f;

            CapsuleCollider collider = lem.AddComponent<CapsuleCollider>();
            collider.height = height;
            collider.radius = lemRadius;
            collider.center = Vector3.up * (height * 0.5f);
        }
    }

    private static GameObject LoadLemPrefab()
    {
        if (cachedLemPrefab != null)
        {
            return cachedLemPrefab;
        }

        cachedLemPrefab = Resources.Load<GameObject>(GameConstants.ResourcePaths.LemPrefab);
        return cachedLemPrefab;
    }

    #region Debug

    private void OnDrawGizmos()
    {
        if (!isAlive) return;

        float direction = facingRight ? 1f : -1f;
        Vector3 foot = GetFootPointPosition();
        float halfHeight = GetLemHeight() * 0.5f;

        // Ground check - starts from 0.1 above center
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 groundOrigin = foot + Vector3.up * 0.01f;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundCheckDistance);

        // Wall check - starts from 0.25 above center
        Gizmos.color = Color.yellow;
        Vector3 wallStart = foot + Vector3.up * halfHeight;
        Gizmos.DrawLine(wallStart, wallStart + Vector3.right * direction * wallCheckDistance);

        // Cliff check - starts from 0.3 ahead + 0.1 up
        Gizmos.color = Color.cyan;
        Vector3 cliffStart = foot + Vector3.right * direction * 0.3f + Vector3.up * 0.1f;
        Gizmos.DrawLine(cliffStart, cliffStart + Vector3.down * groundCheckDistance);

        // Fall arc target X - where Lem will be horizontally centered after stepping off
        Gizmos.color = Color.magenta;
        Vector3 arcTargetX = foot + new Vector3(fallArcForwardDistance * direction, 0f, 0f);
        Gizmos.DrawLine(foot, arcTargetX);
        Gizmos.DrawWireSphere(arcTargetX, 0.08f);
    }

    #endregion

    /// <summary>
    /// Determines which animation clip should be active based on current gameplay state
    /// and delegates to CharacterVisual for blending and rendering.
    /// Game logic (am I moving? carrying?) stays here; visual execution is delegated.
    /// </summary>
    private void UpdateAnimation()
    {
        if (characterVisual == null || !characterVisual.IsAnimationValid) return;

        // Determine effective movement speed for animation decisions
        float speed = 0f;
        if (isAlive && !isFrozen)
        {
            if (isTurningAround)
            {
                // Keep walk animation active during turn (independent of physics slowdown)
                speed = walkSpeed * Mathf.Max(0.1f, wallBumpSlowdownMultiplier);
            }
            else if (!isFallingArc && isGrounded && rb != null)
            {
                speed = Mathf.Abs(rb.linearVelocity.x);
            }
        }

        bool isMoving = speed > 0.1f;

        if (isMoving)
        {
            bool hasKey = KeyItem.FindHeldKey(this) != null;
            characterVisual.SetActiveClip(hasKey
                ? GameConstants.AnimationClips.WalkCarry
                : GameConstants.AnimationClips.Walk);

            // Sync animation speed with movement speed to prevent foot sliding
            // Speed ratio: current speed / target walk speed
            float animSpeed = walkSpeed > 0.001f ? (speed / walkSpeed) : 1f;

            // Check if approaching an interactive block center (teleporter/transporter)
            float distanceToInteraction = CheckInteractiveBlockAhead();
            if (distanceToInteraction >= 0f)
            {
                // Anticipate the stop by slowing animation based on distance
                // Closer = slower animation (smooth ramp down)
                float slowdownFactor = Mathf.Clamp01(distanceToInteraction / interactionLookAheadDistance);
                // Remap: 1.0 at max distance → 0.6 at zero distance (gentler slowdown)
                slowdownFactor = Mathf.Lerp(0.6f, 1f, slowdownFactor);
                animSpeed *= slowdownFactor;
            }
            // If actively slowing for interaction (teleporter, transporter, etc.),
            // make animation respond immediately rather than waiting for velocity to drop
            else if (isExternalStopActive)
            {
                // Animation slows down faster than physics to anticipate the stop
                animSpeed *= 0.5f; // Scale down further during active deceleration
            }

            // Clamp to reasonable range (prevent too slow/too fast animations)
            animSpeed = Mathf.Clamp(animSpeed, 0.3f, 2f);
            characterVisual.SetAnimationSpeed(animSpeed);
        }
        else
        {
            characterVisual.SetActiveClip(GameConstants.AnimationClips.Idle);
            characterVisual.SetAnimationSpeed(1f); // Normal speed for idle
        }

        characterVisual.EvaluateAnimation(Time.deltaTime);
    }

    void OnDestroy()
    {
        if (characterVisual != null)
        {
            characterVisual.Cleanup();
        }
    }

    private float GetWallBumpSpeedMultiplier()
    {
        if (wallBumpTimer <= 0f)
        {
            return 1f;
        }

        float slowDuration = Mathf.Max(0f, wallBumpSlowdownDuration);
        float totalDuration = slowDuration;
        if (totalDuration <= 0.0001f)
        {
            return 1f;
        }

        float elapsed = totalDuration - wallBumpTimer;
        if (slowDuration > 0f && elapsed <= slowDuration)
        {
            float t = Mathf.Clamp01(elapsed / slowDuration);
            return Mathf.Lerp(wallBumpSlowdownMultiplier, 1f, t);
        }

        return 1f;
    }

    private void MoveHorizontalToward(float targetX, float deltaTime)
    {
        if (rb == null) return;

        float currentX = rb.linearVelocity.x;
        bool acceleratingSameDirection = Mathf.Sign(targetX) == Mathf.Sign(currentX) && Mathf.Abs(targetX) >= Mathf.Abs(currentX);
        float response = acceleratingSameDirection ? horizontalAcceleration : horizontalDeceleration;
        float maxDelta = Mathf.Max(0.01f, response) * deltaTime;
        float nextX = Mathf.MoveTowards(currentX, targetX, maxDelta);
        rb.linearVelocity = new Vector3(nextX, rb.linearVelocity.y, 0f);
    }

    private static PhysicsMaterial GetSharedLemPhysicsMaterial()
    {
        if (sharedLemPhysicsMaterial != null)
        {
            return sharedLemPhysicsMaterial;
        }

        sharedLemPhysicsMaterial = new PhysicsMaterial("LemPhysics")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        return sharedLemPhysicsMaterial;
    }

    private void TriggerWallBumpResponse()
    {
        wallBumpTimer = Mathf.Max(0f, wallBumpSlowdownDuration);
    }

    private void TurnAroundImmediate()
    {
        facingRight = !facingRight;
        UpdateFacingVisuals();
    }

    private System.Collections.IEnumerator TurnAroundClockwiseRoutine()
    {
        if (rb == null)
        {
            TurnAroundImmediate();
            turnRoutine = null;
            yield break;
        }

        isTurningAround = true;
        float duration = Mathf.Max(0.01f, turnDuration);
        float elapsed = 0f;
        float startY = transform.eulerAngles.y;
        float targetY = startY + ClockwiseTurnDegrees;
        float startSpeedX = rb.linearVelocity.x;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t); // smoothstep

            float newY = Mathf.Lerp(startY, targetY, eased);
            transform.rotation = Quaternion.Euler(0f, newY, 0f);
            characterVisual?.ApplyVisualRotation();

            float slowedX = Mathf.Lerp(startSpeedX, 0f, eased);
            rb.linearVelocity = new Vector3(slowedX, rb.linearVelocity.y, 0f);
            yield return null;
        }

        facingRight = !facingRight;
        UpdateFacingVisuals();
        TriggerWallBumpResponse();
        float exitSpeed = walkSpeed * Mathf.Max(0.1f, wallBumpSlowdownMultiplier);
        float exitDirection = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector3(exitDirection * exitSpeed, rb.linearVelocity.y, 0f);
        isTurningAround = false;
        turnRoutine = null;
    }

    private void ApplyLemDimensions()
    {
        lemHeight = Mathf.Max(0.1f, lemHeight);

        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }

        if (capsuleCollider != null)
        {
            float radius = lemHeight * 0.25f;
            capsuleCollider.height = lemHeight;
            capsuleCollider.radius = radius;
            capsuleCollider.center = Vector3.up * (lemHeight * 0.5f);
        }

        characterVisual?.RefitToHeight(lemHeight);

        lastAppliedLemHeight = lemHeight;
    }
}

/// <summary>
/// Forwards animation events from a child Animator to the root LemController.
/// Unity's animation events only fire on the Animator's own GameObject via SendMessage,
/// so this relay bridges the gap when the Animator lives on a child object.
/// </summary>
public class AnimEventRelay : MonoBehaviour
{
    [HideInInspector] public LemController target;

    public void FootstepLeft()
    {
        if (target != null) target.FootstepLeft();
    }

    public void FootstepRight()
    {
        if (target != null) target.FootstepRight();
    }
}
