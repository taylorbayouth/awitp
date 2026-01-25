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
/// - Half-scale (0.5x) to fit grid better
///
/// DESIGN NOTES:
/// - Raycasts use QueryTriggerInteraction.Ignore to avoid detecting block trigger zones
/// - Ground check starts slightly above center to avoid missing ground
/// - Wall/ground check distances are tuned for half-scale Lem
/// </summary>
public class LemController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units per second")]
    [SerializeField] private float walkSpeed = 1f;

    [Header("Detection")]
    [Tooltip("Raycast distance for wall detection (tuned for half-scale Lem)")]
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
    private Transform hatTransform;

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

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Create physics material to prevent sticking
        PhysicMaterial physicsMaterial = new PhysicMaterial("LemPhysics");
        physicsMaterial.dynamicFriction = 0f;
        physicsMaterial.staticFriction = 0f;
        physicsMaterial.bounciness = 0f;
        physicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        physicsMaterial.bounceCombine = PhysicMaterialCombine.Minimum;

        CapsuleCollider collider = GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            collider.material = physicsMaterial;
        }

        hatTransform = transform.Find("Hat");
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
        Vector3 newVelocity = new Vector3(direction * walkSpeed, rb.velocity.y, 0);
        rb.velocity = newVelocity;
    }

    private void CheckGround()
    {
        // Start raycast from center of Lem (not bottom) to avoid missing ground
        // Lem is scaled 0.5x, so everything is half-sized
        Vector3 origin = transform.position + Vector3.up * 0.1f; // Start slightly above center
        RaycastHit hit;
        // IMPORTANT: Use QueryTriggerInteraction.Ignore to ignore trigger colliders (like BaseBlock detection zones)
        isGrounded = Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance, solidLayerMask, QueryTriggerInteraction.Ignore);
    }

    private void CheckWallAhead()
    {
        // Check at center height - adjusted for smaller Lem
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        Vector3 direction = facingRight ? Vector3.right : Vector3.left;

        // Ignore triggers to avoid detecting BaseBlock detection zones
        if (Physics.Raycast(origin, direction, wallCheckDistance, solidLayerMask, QueryTriggerInteraction.Ignore))
        {
            TurnAround();
        }
    }

    private void CheckCliffAhead()
    {
        // Check slightly ahead for ground - adjusted for smaller Lem
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
            rb.velocity = Vector3.zero; // Stop movement when frozen
        }
    }

    private void UpdateFacingVisuals()
    {
        float yRotation = facingRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        rb.velocity = Vector3.zero;
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Creates a Lem character at the specified position.
    /// Lem is half the size (scale 0.5) to fit better in the grid.
    /// </summary>
    public static GameObject CreateLem(Vector3 position)
    {
        GameObject lem = new GameObject("Lem");
        lem.transform.position = position;
        lem.transform.localScale = Vector3.one * 0.5f; // Half size!
        lem.tag = "Player";

        // Body (capsule)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(lem.transform);
        body.transform.localPosition = Vector3.up * 0.5f;
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Destroy(body.GetComponent<Collider>());

        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = Color.red; // Red body
        }

        // Hat (cube) - points in walking direction
        GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hat.name = "Hat";
        hat.transform.SetParent(lem.transform);
        hat.transform.localPosition = new Vector3(0.35f, 1.15f, 0);
        hat.transform.localScale = new Vector3(0.5f, 0.12f, 0.35f);
        hat.transform.localRotation = Quaternion.Euler(0, 0, -90f);
        Destroy(hat.GetComponent<Collider>());

        Renderer hatRenderer = hat.GetComponent<Renderer>();
        if (hatRenderer != null)
        {
            hatRenderer.material.color = new Color(0.1f, 0.1f, 0.1f); // Dark hat
        }

        // Collider on parent - smaller and centered
        CapsuleCollider collider = lem.AddComponent<CapsuleCollider>();
        collider.height = 1f;
        collider.radius = 0.2f; // Slightly smaller radius to avoid getting stuck
        collider.center = Vector3.up * 0.5f;

        // Controller
        LemController controller = lem.AddComponent<LemController>();
        controller.isFrozen = true; // Start frozen in editor
        controller.UpdateFacingVisuals();

        return lem;
    }

    #region Debug

    private void OnDrawGizmos()
    {
        if (!isAlive) return;

        float direction = facingRight ? 1f : -1f;

        // Ground check - starts from 0.1 above center
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 groundOrigin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundCheckDistance);

        // Wall check - starts from 0.25 above center
        Gizmos.color = Color.yellow;
        Vector3 wallStart = transform.position + Vector3.up * 0.25f;
        Gizmos.DrawLine(wallStart, wallStart + Vector3.right * direction * wallCheckDistance);

        // Cliff check - starts from 0.3 ahead + 0.1 up
        Gizmos.color = Color.cyan;
        Vector3 cliffStart = transform.position + Vector3.right * direction * 0.3f + Vector3.up * 0.1f;
        Gizmos.DrawLine(cliffStart, cliffStart + Vector3.down * groundCheckDistance);
    }

    #endregion
}
