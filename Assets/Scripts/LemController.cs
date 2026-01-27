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
/// - Height is 95% of grid cell size (leaving 5% headroom)
///
/// DESIGN NOTES:
/// - Raycasts use QueryTriggerInteraction.Ignore to avoid detecting block trigger zones
/// - Ground check starts slightly above center to avoid missing ground
/// - Wall/ground check distances are tuned for half-scale Lem
/// </summary>
public class LemController : MonoBehaviour
{
    [Header("Glitch Visuals")]
    [Tooltip("Local rotation offset (degrees) to align Glitch with +X movement")]
    [SerializeField] private Vector3 glitchRotationOffset = new Vector3(0f, 90f, 0f);

    #region Inspector Fields

    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units per second")]
    [SerializeField] private float walkSpeed = 1f;

    [Header("Detection")]
    [Tooltip("Raycast distance for wall detection")]
    [SerializeField] private float wallCheckDistance = 0.3f;

    [Tooltip("Raycast distance for ground detection")]
    [SerializeField] private float groundCheckDistance = 1.0f;

    [Tooltip("Layer mask for solid objects (blocks). Default: everything")]
    [SerializeField] private LayerMask solidLayerMask = ~0;

    [Header("State")]
    [Tooltip("True if facing right, false if facing left")]
    [SerializeField] private bool facingRight = true;

    [Tooltip("True if Lem is standing on solid ground")]
    [SerializeField] private bool isGrounded = false;

    [Tooltip("False if Lem has died (fell off screen)")]
    [SerializeField] private bool isAlive = true;

    [Tooltip("True = no movement (editor mode), False = active walking (play mode)")]
    [SerializeField] private bool isFrozen = true;

    // Cached component references
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform hatTransform;
    private Transform footPoint;
    private Animator glitchAnimator;
    private bool hasAnimator;
    private bool hasSpeedParam;
    private bool hasIsWalkingParam;
    private bool hasHasKeyParam;
    private Transform glitchVisual;
    private Transform glitchPivot;
    private Vector3 lastGlitchRotationOffset;

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

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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

        footPoint = transform.Find("FootPoint");
        if (footPoint == null)
        {
            footPoint = transform;
        }
        hatTransform = transform.Find("Hat");
        glitchPivot = transform.Find("GlitchPivot");
        glitchVisual = transform.Find("Glitch");
        CacheAnimator();
        ApplyGlitchVisualRotation();
    }

    void FixedUpdate()
    {
        if (!isAlive) return;
        if (isFrozen) return;

        CheckGround();

        if (isGrounded)
        {
            CheckWallAhead();
            // Don't check for cliffs - let Lem walk off edges and fall like in Lemmings!
            Walk();
        }
    }

    void Update()
    {
        // Die if fallen outside camera bounds
        if (IsOutsideCameraBounds())
        {
            Die();
        }

        if (glitchRotationOffset != lastGlitchRotationOffset)
        {
            ApplyGlitchVisualRotation();
        }

        UpdateAnimator();
    }

    private bool IsOutsideCameraBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;

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

            // Add some buffer so Lem disappears slightly after leaving view
            float buffer = 2f;

            Vector3 pos = transform.position;
            return pos.x < left - buffer ||
                   pos.x > right + buffer ||
                   pos.y < bottom - buffer ||
                   pos.y > top + buffer;
        }

        return false;
    }

    private void Walk()
    {
        float direction = facingRight ? 1f : -1f;
        Vector3 newVelocity = new Vector3(direction * walkSpeed, rb.linearVelocity.y, 0);
        rb.linearVelocity = newVelocity;
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

    private void CheckCliffAhead()
    {
        // Check slightly ahead for ground
        Vector3 origin = transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.3f;
        origin.y += 0.1f; // Start from same height as ground check

        // Ignore triggers to avoid detecting BaseBlock detection zones
        if (!Physics.Raycast(origin, Vector3.down, groundCheckDistance, solidLayerMask, QueryTriggerInteraction.Ignore))
        {
            TurnAround();
        }
    }

    public void TurnAround()
    {
        facingRight = !facingRight;
        UpdateFacingVisuals();
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
        if (rb != null && frozen && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero; // Stop movement when frozen
        }

    }

    private void UpdateFacingVisuals()
    {
        float yRotation = facingRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        ApplyGlitchVisualRotation();
    }


    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        rb.linearVelocity = Vector3.zero;

        // Exit play mode when Lem dies
        EditorController editorController = FindAnyObjectByType<EditorController>();
        if (editorController != null)
        {
            editorController.ExitPlayMode();
        }

        Destroy(gameObject, 0.1f);
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


    /// <summary>
    /// Creates a Lem character with its foot point placed at the specified position.
    /// Tries to load from Resources/Characters/Lem prefab first, falls back to programmatic creation.
    /// Lem height is 95% of the current grid cell size (leaving 5% headroom).
    /// </summary>
    public static GameObject CreateLem(Vector3 position)
    {
        // PREFAB-BASED INSTANTIATION (Primary method)
        GameObject lemPrefab = Resources.Load<GameObject>("Characters/Lem");
        if (lemPrefab != null)
        {
            GameObject lem = Instantiate(lemPrefab, position, Quaternion.identity);
            LemController controller = lem.GetComponent<LemController>();
            if (controller != null)
            {
                controller.isFrozen = true;
                controller.UpdateFacingVisuals();
            }
            return lem;
        }

        Debug.LogWarning("[LemController] Lem prefab not found at Resources/Characters/Lem. Using programmatic creation as fallback.");

        // FALLBACK: Programmatic creation
        return CreateLemProgrammatically(position);
    }

    /// <summary>
    /// Programmatic Lem creation (fallback when prefab doesn't exist).
    /// </summary>
    private static GameObject CreateLemProgrammatically(Vector3 position)
    {
        GameObject lem = new GameObject("Lem");
        lem.transform.position = position;
        lem.transform.localScale = Vector3.one;
        lem.tag = "Player";

        float cellSize = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        float lemHeight = cellSize * 0.95f;
        float lemRadius = lemHeight * 0.25f;

        GameObject foot = new GameObject("FootPoint");
        foot.transform.SetParent(lem.transform);
        foot.transform.localPosition = Vector3.zero;

        bool attachedGlitch = AttachGlitchVisual(lem.transform, lemHeight);
        if (!attachedGlitch)
        {
            CreateFallbackBody(lem.transform, lemHeight);
        }

        // Collider on parent - smaller and centered
        CapsuleCollider collider = lem.AddComponent<CapsuleCollider>();
        collider.height = lemHeight;
        collider.radius = lemRadius;
        collider.center = Vector3.up * (lemHeight * 0.5f);

        // Controller
        LemController controller = lem.AddComponent<LemController>();
        controller.isFrozen = true;
        controller.UpdateFacingVisuals();
        controller.glitchVisual = lem.transform.Find("Glitch");
        controller.ApplyGlitchVisualRotation();

        return lem;
    }

    private static bool AttachGlitchVisual(Transform parent, float targetHeight)
    {
        if (parent == null) return false;

        GameObject prefab = LoadGlitchPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[LemController] Glitch prefab not found. Using fallback body.");
            return false;
        }

        Transform pivot = new GameObject("GlitchPivot").transform;
        pivot.SetParent(parent, false);

        GameObject visual = Instantiate(prefab, pivot);
        visual.name = "Glitch";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        Material builtInMaterial = LoadGlitchMaterial();
        if (builtInMaterial != null)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = builtInMaterial;
                }
                else
                {
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = builtInMaterial;
                    }
                    renderer.sharedMaterials = materials;
                }
            }
        }

        if (!TryGetBoundsInParentSpace(visual.transform, parent, out Bounds bounds))
        {
            return true;
        }

        float height = bounds.size.y;
        if (height > 0.0001f)
        {
            float scale = targetHeight / height;
            visual.transform.localScale = Vector3.one * scale;
            if (TryGetBoundsInParentSpace(visual.transform, parent, out bounds))
            {
                // Center on X and ground on Y; keep Z centered on the block to avoid front-edge drift.
                Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, 0f);
                pivot.localPosition = offset;
            }
        }

        return true;
    }

    private void ApplyGlitchVisualRotation()
    {
        if (glitchPivot == null)
        {
            glitchPivot = transform.Find("GlitchPivot");
        }

        if (glitchPivot == null) return;

        glitchPivot.localRotation = Quaternion.Euler(glitchRotationOffset);
        lastGlitchRotationOffset = glitchRotationOffset;
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

    private static GameObject LoadGlitchPrefab()
    {
        GameObject prefab = null;
#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Glitch/Prefabs/PF_Glitch.prefab");
#endif
        return prefab;
    }

    private static Material LoadGlitchMaterial()
    {
        Material mat = null;
#if UNITY_EDITOR
        mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Glitch/Materials/Built-In/M_Glitch.mat");
#endif
        return mat;
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
    }

    #endregion

    private void CacheAnimator()
    {
        glitchAnimator = GetComponentInChildren<Animator>();
        if (glitchAnimator == null)
        {
            hasAnimator = false;
            return;
        }

        hasAnimator = true;
        hasSpeedParam = false;
        hasIsWalkingParam = false;
        hasHasKeyParam = false;

        foreach (AnimatorControllerParameter param in glitchAnimator.parameters)
        {
            if (param.name == "Speed") hasSpeedParam = true;
            if (param.name == "IsWalking") hasIsWalkingParam = true;
            if (param.name == "HasKey") hasHasKeyParam = true;
        }
    }

    private void UpdateAnimator()
    {
        if (!hasAnimator || glitchAnimator == null) return;

        float horizontalSpeed = Mathf.Abs(rb != null ? rb.linearVelocity.x : 0f);
        if (isFrozen)
        {
            horizontalSpeed = 0f;
        }
        bool isWalking = !isFrozen && horizontalSpeed > 0.01f;

        if (hasSpeedParam)
        {
            glitchAnimator.SetFloat("Speed", horizontalSpeed);
        }

        if (hasIsWalkingParam)
        {
            glitchAnimator.SetBool("IsWalking", isWalking);
        }

        if (hasHasKeyParam)
        {
            bool hasKey = KeyItem.FindHeldKey(this) != null;
            glitchAnimator.SetBool("HasKey", hasKey);
        }

    }
}
