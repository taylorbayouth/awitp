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
    private const string PreRiggedLemResourcePath = "Characters/LemMeshPreRigged";
    private const string PreRiggedLemTexturePath = "Characters/LemMeshPreRigged_Albedo";
    private const string AnimatorControllerResourcePath = "Animations/Lem";
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");
    private static readonly int HasKeyParam = Animator.StringToHash("HasKey");
    private static readonly int IsFallingParam = Animator.StringToHash("IsFalling");

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

    [Header("Visual")]
    [Tooltip("Local rotation offset (degrees) to align visual with +X movement")]
    [SerializeField] private Vector3 visualRotationOffset = new Vector3(0f, 90f, 0f);
    [Tooltip("Animator speed blend damp time in seconds")]
    [SerializeField] private float speedBlendDampTime = 0.08f;

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
    private Animator visualAnimator;
    private bool hasAnimator;
    private bool hasSpeedParam;
    private bool hasIsWalkingParam;
    private bool hasHasKeyParam;
    private bool hasIsFallingParam;

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

    private Transform visualPivot;
    private Vector3 lastVisualRotationOffset;
    private bool defaultUseGravity;
    private bool defaultIsKinematic;
    private BaseBlock currentBlock;
    private BlockType currentBlockType = BlockType.Walk;
    private bool hasCurrentBlock;

    private enum AnimState
    {
        Dead,
        Frozen,
        Falling,
        Turning,
        Walking
    }

    #endregion

    void Awake()
    {
        if (gameObject.tag != "Player")
        {
            gameObject.tag = "Player";
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        capsuleCollider = GetComponent<CapsuleCollider>();

        ApplyLemDimensions();

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        defaultUseGravity = rb.useGravity;
        defaultIsKinematic = rb.isKinematic;

        // Create physics material to prevent sticking
        PhysicsMaterial physicsMaterial = new PhysicsMaterial("LemPhysics");
        physicsMaterial.dynamicFriction = 0f;
        physicsMaterial.staticFriction = 0f;
        physicsMaterial.bounciness = 0f;
        physicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        physicsMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;

        CapsuleCollider collider = GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            collider.material = physicsMaterial;
        }

        TrySwapToPreRiggedVisual();

        footPoint = transform.Find("FootPoint");
        if (footPoint == null)
        {
            footPoint = transform;
        }
        visualPivot = transform.Find("VisualPivot");
        CacheAnimator();
        ApplyVisualRotation();

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

    private void TrySwapToPreRiggedVisual()
    {
        GameObject preRiggedPrefab = Resources.Load<GameObject>(PreRiggedLemResourcePath);
        if (preRiggedPrefab == null) return;

        RuntimeAnimatorController controllerAsset = null;
        Animator existingAnimator = GetComponentInChildren<Animator>();
        if (existingAnimator != null)
        {
            controllerAsset = existingAnimator.runtimeAnimatorController;
        }
        if (controllerAsset == null)
        {
            controllerAsset = Resources.Load<RuntimeAnimatorController>(AnimatorControllerResourcePath);
        }

        Transform existingPivot = transform.Find("VisualPivot");
        if (existingPivot != null)
        {
            Destroy(existingPivot.gameObject);
        }
        Transform existingVisual = transform.Find("Visual");
        if (existingVisual != null)
        {
            Destroy(existingVisual.gameObject);
        }
        Transform legacyGlitchPivot = transform.Find("GlitchPivot");
        if (legacyGlitchPivot != null)
        {
            Destroy(legacyGlitchPivot.gameObject);
        }
        Transform legacyGlitch = transform.Find("Glitch");
        if (legacyGlitch != null)
        {
            Destroy(legacyGlitch.gameObject);
        }

        Transform pivot = new GameObject("VisualPivot").transform;
        pivot.SetParent(transform, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;

        Transform visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(pivot, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        GameObject modelInstance = Instantiate(preRiggedPrefab, visualRoot);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;
        if (modelInstance.GetComponent<HeadTiltOffset>() == null)
        {
            modelInstance.AddComponent<HeadTiltOffset>();
        }
        ApplyPreRiggedMaterial(modelInstance);
        FitVisualToHeight(visualRoot, transform, capsuleCollider != null ? capsuleCollider.height : DefaultLemHeight);

        Animator modelAnimator = modelInstance.GetComponent<Animator>();
        if (modelAnimator == null)
        {
            modelAnimator = modelInstance.GetComponentInChildren<Animator>();
        }

        if (modelAnimator != null)
        {
            if (controllerAsset != null)
            {
                modelAnimator.runtimeAnimatorController = controllerAsset;
            }
            modelAnimator.applyRootMotion = false;
            modelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            modelAnimator.Rebind();
        }
    }

    private void ApplyPreRiggedMaterial(GameObject modelInstance)
    {
        if (modelInstance == null) return;
        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        Texture2D texture = Resources.Load<Texture2D>(PreRiggedLemTexturePath);
        if (texture == null) return;

        Shader shader = GetPreferredCharacterShader(renderers);
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Material materialToApply = new Material(shader);
        if (materialToApply.HasProperty("_BaseMap")) materialToApply.SetTexture("_BaseMap", texture);
        if (materialToApply.HasProperty("_MainTex")) materialToApply.SetTexture("_MainTex", texture);
        Color darkenTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        if (materialToApply.HasProperty("_BaseColor")) materialToApply.SetColor("_BaseColor", darkenTint);
        if (materialToApply.HasProperty("_Color")) materialToApply.SetColor("_Color", darkenTint);

        // Set render queue higher than default (2000) so Lem renders in front of debris
        // Standard opaque queue is 2000, we use 2450 to render after debris (1900) and blocks (2000)
        materialToApply.renderQueue = 2450;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            Material[] mats = renderer.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                renderer.sharedMaterial = materialToApply;
                continue;
            }

            for (int m = 0; m < mats.Length; m++)
            {
                mats[m] = materialToApply;
            }
            renderer.sharedMaterials = mats;
        }
    }

    private static Shader GetPreferredCharacterShader(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            Material[] mats = renderer.sharedMaterials;
            if (mats == null) continue;
            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat != null && mat.shader != null)
                {
                    return mat.shader;
                }
            }
        }

        return null;
    }

    private static void FitVisualToHeight(Transform visualRoot, Transform lemRoot, float targetHeight)
    {
        if (visualRoot == null || lemRoot == null) return;
        if (targetHeight <= 0f) return;

        if (!TryGetBoundsInParentSpace(visualRoot, lemRoot, out Bounds bounds))
        {
            return;
        }

        float currentHeight = bounds.size.y;
        if (currentHeight <= 0.0001f) return;

        float scale = targetHeight / currentHeight;
        visualRoot.localScale = Vector3.one * scale;

        if (!TryGetBoundsInParentSpace(visualRoot, lemRoot, out bounds))
        {
            return;
        }

        Transform pivot = visualRoot.parent;
        if (pivot != null)
        {
            pivot.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);
        }
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

        // Die if fallen outside camera bounds
        if (IsOutsideCameraBounds())
        {
            Die();
        }

        if (visualRotationOffset != lastVisualRotationOffset)
        {
            ApplyVisualRotation();
        }

        UpdateAnimator();
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
        RaycastHit hit;
        // IMPORTANT: Use QueryTriggerInteraction.Ignore to ignore trigger colliders (like BaseBlock detection zones)
        isGrounded = Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance, solidLayerMask, QueryTriggerInteraction.Ignore);

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
        ApplyVisualRotation();
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
        rb.linearVelocity = Vector3.zero;

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

        footstepSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        float volume = UnityEngine.Random.Range(volumeRange.x, volumeRange.y);
        footstepSource.PlayOneShot(clip, volume);
    }

    private bool TryGetFootstepSurface(BlockType type, out FootstepSurface surface)
    {
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
    /// Uses prefab-based creation from Resources/Characters/Lem when available.
    /// Falls back to programmatic creation if the prefab is missing.
    /// </summary>
    public static GameObject CreateLem(Vector3 position)
    {
        GameObject lemPrefab = Resources.Load<GameObject>("Characters/Lem");
        if (lemPrefab != null)
        {
            GameObject lem = Instantiate(lemPrefab, position, Quaternion.identity);
            LemController controller = lem.GetComponent<LemController>();
            if (controller != null)
            {
                controller.SetFootPointPosition(position);
                controller.SetFrozen(true);
            }
            return lem;
        }

        return CreateLemProgrammatically(position);
    }

    /// <summary>
    /// Programmatic Lem creation.
    /// </summary>
    private static GameObject CreateLemProgrammatically(Vector3 position)
    {
        GameObject lem = new GameObject("Lem");
        lem.transform.position = position;
        lem.transform.localScale = Vector3.one;
        lem.tag = "Player";

        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit
        float height = cellSize * DefaultLemHeight;
        float lemRadius = height * 0.25f;

        GameObject foot = new GameObject("FootPoint");
        foot.transform.SetParent(lem.transform);
        foot.transform.localPosition = Vector3.zero;

        // Collider on parent - smaller and centered
        CapsuleCollider collider = lem.AddComponent<CapsuleCollider>();
        collider.height = height;
        collider.radius = lemRadius;
        collider.center = Vector3.up * (height * 0.5f);

        // Controller
        LemController controller = lem.AddComponent<LemController>();
        controller.isFrozen = true;
        controller.UpdateFacingVisuals();
        controller.ApplyVisualRotation();

        if (lem.GetComponentInChildren<Renderer>() == null)
        {
            CreateFallbackBody(lem.transform, height);
        }

        return lem;
    }
    private void ApplyVisualRotation()
    {
        if (visualPivot == null)
        {
            visualPivot = transform.Find("VisualPivot");
        }

        if (visualPivot == null) return;

        visualPivot.localRotation = Quaternion.Euler(visualRotationOffset);
        lastVisualRotationOffset = visualRotationOffset;
    }

    private static bool TryGetBoundsInParentSpace(Transform root, Transform parent, out Bounds bounds)
    {
        bounds = new Bounds();
        if (root == null || parent == null) return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return false;

        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            Bounds rBounds = renderer.bounds;
            Vector3 min = rBounds.min;
            Vector3 max = rBounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 localCorner = parent.InverseTransformPoint(corners[i]);
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
    }

    private static void CreateFallbackBody(Transform parent, float lemHeight)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = Vector3.up * (lemHeight * 0.5f);
        body.transform.localScale = new Vector3(lemHeight * 0.5f, lemHeight * 0.5f, lemHeight * 0.5f);
        Destroy(body.GetComponent<Collider>());
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

    private void CacheAnimator()
    {
        Transform preferredRoot = transform.Find("VisualPivot");
        if (preferredRoot != null)
        {
            visualAnimator = preferredRoot.GetComponentInChildren<Animator>();
        }
        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<Animator>();
        }
        if (visualAnimator == null)
        {
            hasAnimator = false;
            return;
        }

        hasAnimator = true;
        hasSpeedParam = false;
        hasIsWalkingParam = false;
        hasHasKeyParam = false;
        hasIsFallingParam = false;

        foreach (AnimatorControllerParameter param in visualAnimator.parameters)
        {
            if (param.nameHash == SpeedParam) hasSpeedParam = true;
            if (param.nameHash == IsWalkingParam) hasIsWalkingParam = true;
            if (param.nameHash == HasKeyParam) hasHasKeyParam = true;
            if (param.nameHash == IsFallingParam) hasIsFallingParam = true;
        }
    }

    private void UpdateAnimator()
    {
        if (!hasAnimator || visualAnimator == null) return;

        AnimState state = EvaluateAnimState();
        bool isWalking = state == AnimState.Walking || state == AnimState.Turning;
        bool isFalling = state == AnimState.Falling;
        float horizontalSpeed = GetAnimatorSpeedValue(state);

        if (state == AnimState.Frozen || state == AnimState.Dead)
        {
            horizontalSpeed = 0f;
        }

        if (hasSpeedParam)
        {
            visualAnimator.SetFloat(SpeedParam, horizontalSpeed, speedBlendDampTime, Time.deltaTime);
        }

        if (hasIsWalkingParam)
        {
            visualAnimator.SetBool(IsWalkingParam, isWalking);
        }

        if (hasHasKeyParam)
        {
            bool hasKey = KeyItem.FindHeldKey(this) != null;
            visualAnimator.SetBool(HasKeyParam, hasKey);
        }

        if (hasIsFallingParam)
        {
            visualAnimator.SetBool(IsFallingParam, isFalling);
        }
    }

    private AnimState EvaluateAnimState()
    {
        if (!isAlive) return AnimState.Dead;
        if (isFrozen) return AnimState.Frozen;
        if (isFallingArc || !isGrounded) return AnimState.Falling;
        if (isTurningAround) return AnimState.Turning;
        return AnimState.Walking;
    }

    private float GetAnimatorSpeedValue(AnimState state)
    {
        if (rb == null) return 0f;

        if (state == AnimState.Turning)
        {
            // Turn animation should stay in locomotion space, independent of physics slowdown to zero.
            return walkSpeed * Mathf.Max(0.1f, wallBumpSlowdownMultiplier);
        }

        return Mathf.Abs(rb.linearVelocity.x);
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
            ApplyVisualRotation();

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

        Transform runtimeVisual = transform.Find("VisualPivot/Visual");
        if (runtimeVisual != null)
        {
            FitVisualToHeight(runtimeVisual, transform, lemHeight);
        }

        lastAppliedLemHeight = lemHeight;
    }
}
