using UnityEngine;

/// <summary>
/// Simple AI controller for lemming character.
/// Walks forward continuously, turns around when hitting walls or edges.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class LemmingController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float turnSpeed = 180f;
    public bool facingRight = true; // Start facing right (+X direction)

    [Header("Detection")]
    public float wallDetectionDistance = 0.5f;
    public float edgeDetectionDistance = 0.5f;
    public float groundCheckDistance = 0.3f; // Check slightly below
    public LayerMask detectionMask = ~0; // Detect everything by default

    [Header("Debug")]
    public bool showDebugRays = true;

    private CharacterController controller;
    private Vector3 moveDirection;
    private bool isTurning = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Set initial direction based on facingRight
        // Right = +X, Left = -X (walking on XZ plane, Y is up)
        transform.rotation = Quaternion.Euler(0, facingRight ? 90 : -90, 0);
        moveDirection = transform.right; // Walk along X axis
    }

    private void Update()
    {
        if (isTurning)
        {
            // Skip movement during turn
            return;
        }

        // Apply gravity
        if (!controller.isGrounded)
        {
            moveDirection.y -= 9.81f * Time.deltaTime;
        }
        else
        {
            moveDirection.y = -0.5f; // Small downward force to stay grounded
        }

        // Check for obstacles ahead
        if (DetectWall() || DetectEdge())
        {
            TurnAround();
            return;
        }

        // Move along X axis (right or left)
        Vector3 motion = transform.right * walkSpeed * Time.deltaTime;
        motion.y = moveDirection.y * Time.deltaTime;
        controller.Move(motion);
    }

    /// <summary>
    /// Detects walls in front of the lemming using raycast.
    /// </summary>
    private bool DetectWall()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Check at waist height
        Vector3 rayDirection = transform.right; // Walk direction (along X axis)

        bool hitWall = Physics.Raycast(rayOrigin, rayDirection, wallDetectionDistance, detectionMask);

        if (showDebugRays)
        {
            Debug.DrawRay(rayOrigin, rayDirection * wallDetectionDistance, hitWall ? Color.red : Color.green);
        }

        return hitWall;
    }

    /// <summary>
    /// Detects edges/gaps ahead of the lemming using downward raycast.
    /// </summary>
    private bool DetectEdge()
    {
        // Check ahead in walk direction (along X axis)
        Vector3 rayOrigin = transform.position + transform.right * edgeDetectionDistance + Vector3.up * 0.1f;
        Vector3 rayDirection = Vector3.down;

        bool hasGround = Physics.Raycast(rayOrigin, rayDirection, groundCheckDistance, detectionMask);

        if (showDebugRays)
        {
            Debug.DrawRay(rayOrigin, rayDirection * groundCheckDistance, hasGround ? Color.green : Color.yellow);
        }

        return !hasGround; // Return true if NO ground (edge detected)
    }

    /// <summary>
    /// Turns the lemming around 180 degrees.
    /// </summary>
    private void TurnAround()
    {
        if (isTurning) return;

        isTurning = true;
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
        moveDirection = transform.right;

        // Small delay before resuming movement
        Invoke(nameof(ResumMovement), 0.1f);

        Debug.Log($"Lemming {gameObject.name} turned around (now facing {(facingRight ? "right" : "left")})");
    }

    private void ResumMovement()
    {
        isTurning = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Additional collision handling if needed
        // This triggers when CharacterController physically collides with something

        if (hit.gameObject.CompareTag("Wall"))
        {
            TurnAround();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw walk direction (along X axis)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 1f);

        // Draw detection areas
        Gizmos.color = Color.red;
        Vector3 wallCheck = transform.position + Vector3.up * 0.5f + transform.right * wallDetectionDistance;
        Gizmos.DrawWireSphere(wallCheck, 0.1f);

        Gizmos.color = Color.yellow;
        Vector3 edgeCheck = transform.position + transform.right * edgeDetectionDistance;
        Gizmos.DrawWireSphere(edgeCheck, 0.1f);
    }

    /// <summary>
    /// Public method to set initial direction.
    /// </summary>
    public void SetDirection(bool right)
    {
        facingRight = right;
        transform.rotation = Quaternion.Euler(0, right ? 90 : -90, 0);
    }
}
