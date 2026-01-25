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

    [Header("Detection")]
    public float wallDetectionDistance = 0.5f;
    public float edgeDetectionDistance = 0.5f;
    public float groundCheckDistance = 1.5f;
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
        // Start moving forward
        moveDirection = transform.forward;
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

        // Move forward
        Vector3 motion = transform.forward * walkSpeed * Time.deltaTime;
        motion.y = moveDirection.y * Time.deltaTime;
        controller.Move(motion);
    }

    /// <summary>
    /// Detects walls in front of the lemming using raycast.
    /// </summary>
    private bool DetectWall()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Check at waist height
        Vector3 rayDirection = transform.forward;

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
        Vector3 rayOrigin = transform.position + transform.forward * edgeDetectionDistance + Vector3.up * 0.1f;
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
        moveDirection = transform.forward;

        // Small delay before resuming movement
        Invoke(nameof(ResumMovement), 0.1f);

        Debug.Log($"Lemming {gameObject.name} turned around");
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

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);

        // Draw detection areas
        Gizmos.color = Color.red;
        Vector3 wallCheck = transform.position + Vector3.up * 0.5f + transform.forward * wallDetectionDistance;
        Gizmos.DrawWireSphere(wallCheck, 0.1f);

        Gizmos.color = Color.yellow;
        Vector3 edgeCheck = transform.position + transform.forward * edgeDetectionDistance;
        Gizmos.DrawWireSphere(edgeCheck, 0.1f);
    }
}
