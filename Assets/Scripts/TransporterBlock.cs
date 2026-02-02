using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transporter block: moves along a designer-defined route when Lem reaches center.
/// Moves in grid units only (no diagonals), and reverses the route on next trigger.
/// </summary>
public class TransporterBlock : BaseBlock
{
    [Header("Transport Route")]
    [Tooltip("Route steps like L2, U1, R3. One token per element.")]
    public string[] routeSteps = new string[] { "L2", "U1" };

    [Header("Transport Settings")]
    public float unitsPerSecond = 2f;
    public float speedMultiplier = 0.25f;
    public bool debugLogs = false;

    [Header("Route Preview")]
    public bool showRoutePreview = true;

    // Transport state machine
    private bool isTransporting = false;      // Currently moving along the route
    private bool isForward = true;            // Direction of next transport (alternates each ride)
    private bool isArmed = true;              // Ready to accept a new transport trigger
    private bool waitingForExit = false;      // Waiting for Lem to leave before re-arming

    // Path visualization (stays stationary in world space while block moves)
    private LineRenderer pathLineRenderer;    // Bezier curved line showing the transport path
    private GameObject arrowheadObject;       // Cone at the end showing transport direction

    // Utility state
    private int previewOriginIndex = -1;      // Starting grid index for preview generation
    private bool hasSnappedThisRide = false;  // Prevents multiple snaps during one transport

    protected override void OnPlayerReachCenter()
    {
        // Ignore trigger if already transporting or waiting to re-arm
        if (isTransporting || !isArmed) return;

        // Find Lem (the player character)
        LemController lem = currentPlayer != null ? currentPlayer : UnityEngine.Object.FindAnyObjectByType<LemController>();
        if (lem == null)
        {
            Debug.LogError("Cannot transport - Lem not found");
            return;
        }

        // Parse route definition (e.g., ["L2", "U1", "R3"])
        List<Vector2Int> steps = BuildSteps();
        if (steps.Count == 0) return;

        // Reverse route if we're going backwards (alternates each ride)
        if (!isForward)
        {
            steps = RouteParser.ReverseSteps(steps);
        }

        // Disarm trigger and start transport
        isArmed = false;
        SetCenterTriggerEnabled(false);
        if (debugLogs) Debug.Log($"TransporterBlock: start transport (forward={isForward})", this);
        StartCoroutine(TransportRoutine(lem, steps));
    }

    protected override void Start()
    {
        base.Start();
        previewOriginIndex = gridIndex;
        BuildPreview();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        ClearPreview();

        if (isTransporting && currentPlayer != null)
        {
            currentPlayer.transform.SetParent(null, true);
            Rigidbody lemRb = currentPlayer.GetComponent<Rigidbody>();
            if (lemRb != null)
            {
                lemRb.isKinematic = false;
                lemRb.useGravity = true;
            }
            currentPlayer.SetFrozen(false);
        }
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isTransporting = false;
        isForward = true;
        isArmed = true;
        waitingForExit = false;
        hasSnappedThisRide = false;
        SetCenterTriggerEnabled(true);
        UpdateArrowheadDirection();
    }

    /// <summary>
    /// Animates the block along its route, carrying Lem with it.
    /// Lem is parented to the block so they move together.
    /// </summary>
    private IEnumerator TransportRoutine(LemController lem, List<Vector2Int> steps)
    {
        isTransporting = true;
        hasSnappedThisRide = false;

        // Freeze Lem's physics - save previous state to restore later
        Rigidbody lemRb = lem.GetComponent<Rigidbody>();
        bool prevKinematic = false;
        bool prevUseGravity = false;
        if (lemRb != null)
        {
            prevKinematic = lemRb.isKinematic;
            prevUseGravity = lemRb.useGravity;
            if (!lemRb.isKinematic) lemRb.linearVelocity = Vector3.zero;
            lemRb.isKinematic = true;
            lemRb.useGravity = false;
        }
        lem.SetFrozen(true);
        if (debugLogs) Debug.Log("TransporterBlock: Lem frozen and parented", this);

        // Parent Lem to the block so they move together
        lem.transform.SetParent(transform, true);

        GridManager grid = GetGridManager();
        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit
        Vector2Int currentCoords = grid != null ? grid.IndexToCoordinates(gridIndex) : Vector2Int.zero;

        // Snap Lem to the block's center if they're slightly misaligned
        TrySnapLemToBlockCenter(lem);

        // Execute each step of the route
        foreach (Vector2Int step in steps)
        {
            Vector2Int nextCoords = currentCoords + step;
            Vector3 startPos = transform.position;
            Vector3 targetPos = grid != null ? grid.CoordinatesToWorldPosition(nextCoords) : (transform.position + new Vector3(step.x * cellSize, step.y * cellSize, 0f));
            targetPos.z += cellSize * 0.5f;  // Offset to render properly on XY plane

            // Calculate movement duration based on distance and speed
            float distance = Vector3.Distance(startPos, targetPos);
            float speed = Mathf.Max(0.01f, unitsPerSecond * speedMultiplier);
            float duration = Mathf.Max(0.01f, distance / speed);

            // Lerp to the target position over the calculated duration
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t));
                yield return null;
            }

            currentCoords = nextCoords;
        }

        // Update grid registration with the new position
        if (grid != null)
        {
            grid.UnregisterBlock(this);
            gridIndex = grid.CoordinatesToIndex(currentCoords);
            grid.RegisterBlock(this);
        }

        // Unparent Lem and restore physics
        lem.transform.SetParent(null, true);
        if (lemRb != null)
        {
            lemRb.isKinematic = prevKinematic;
            lemRb.useGravity = prevUseGravity;
        }
        lem.SetFrozen(false);
        if (debugLogs) Debug.Log("TransporterBlock: Lem unfrozen and detached", this);

        // Flip direction for next ride and update arrow
        isForward = !isForward;
        UpdateArrowheadDirection();

        // Wait for Lem to exit before re-arming
        isTransporting = false;
        waitingForExit = true;
        if (debugLogs) Debug.Log("TransporterBlock: transport complete, waiting for exit", this);
    }

    protected override void OnPlayerExit()
    {
        if (isTransporting) return;
        waitingForExit = true;
        if (debugLogs) Debug.Log("TransporterBlock: OnPlayerExit -> waiting for exit", this);
    }

    private List<Vector2Int> BuildSteps()
    {
        return RouteParser.ParseRouteSteps(routeSteps);
    }

    #region Placement Validation Overrides

    /// <summary>
    /// Returns the grid indices this block will block (its transport path).
    /// Excludes the starting position since the block itself occupies that.
    /// Used to prevent other blocks from being placed along this block's route.
    /// </summary>
    public override int[] GetBlockedIndices()
    {
        List<int> pathIndices = GetRoutePathIndices();

        // Remove the starting position (block's own position)
        if (pathIndices.Count > 0 && pathIndices[0] == gridIndex)
        {
            pathIndices.RemoveAt(0);
        }

        return pathIndices.ToArray();
    }

    /// <summary>
    /// Validates if this block can be placed at the target position.
    /// Checks that the entire transport route is clear of obstacles.
    /// </summary>
    public override bool CanBePlacedAt(int targetIndex, GridManager grid)
    {
        if (grid == null) return true;

        Vector2Int current = grid.IndexToCoordinates(targetIndex);
        List<Vector2Int> steps = BuildSteps();

        // Check each position along the route
        foreach (Vector2Int step in steps)
        {
            current += step;
            if (!grid.IsValidCoordinates(current.x, current.y)) continue;

            int idx = grid.CoordinatesToIndex(current);

            // Block is obstructed if another block is there
            if (grid.HasBlockAtIndex(idx)) return false;

            // Block is obstructed if it intersects another transporter's route
            if (grid.IsIndexBlockedByAnyBlock(idx, this)) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a user-friendly error message explaining why placement failed.
    /// </summary>
    public override string GetPlacementErrorMessage(int targetIndex, GridManager grid)
    {
        if (grid == null) return null;

        Vector2Int current = grid.IndexToCoordinates(targetIndex);
        List<Vector2Int> steps = BuildSteps();

        // Check each position along the route for conflicts
        foreach (Vector2Int step in steps)
        {
            current += step;
            if (!grid.IsValidCoordinates(current.x, current.y)) continue;

            int idx = grid.CoordinatesToIndex(current);

            if (grid.HasBlockAtIndex(idx))
                return $"Route path blocked by existing block at index {idx}";

            if (grid.IsIndexBlockedByAnyBlock(idx, this))
                return $"Route path conflicts with another transporter's route at index {idx}";
        }

        return null;
    }

    #endregion

    public List<int> GetRoutePathIndices()
    {
        List<int> indices = new List<int>();
        GridManager grid = GetGridManager();
        if (grid == null) return indices;

        int originIndex = previewOriginIndex >= 0 ? previewOriginIndex : gridIndex;

        // Safety check: originIndex must be valid before converting to coordinates
        if (originIndex < 0 || !grid.IsValidIndex(originIndex))
        {
            return indices;  // Return empty list if no valid origin
        }

        Vector2Int current = grid.IndexToCoordinates(originIndex);
        List<Vector2Int> steps = BuildSteps();

        HashSet<int> unique = new HashSet<int>();
        if (grid.IsValidIndex(originIndex)) unique.Add(originIndex);

        foreach (Vector2Int step in steps)
        {
            current += step;
            if (grid.IsValidCoordinates(current.x, current.y))
            {
                int idx = grid.CoordinatesToIndex(current);
                if (unique.Add(idx)) indices.Add(idx);
            }
        }

        if (unique.Contains(originIndex)) indices.Insert(0, originIndex);
        return indices;
    }

    /// <summary>
    /// Creates a visual preview of the transport path using a bezier-curved line and arrowhead.
    /// The preview stays stationary in world space (not parented to the block).
    /// </summary>
    private void BuildPreview()
    {
        ClearPreview();
        if (!showRoutePreview) return;

        GridManager grid = GetGridManager();
        if (grid == null) return;

        if (previewOriginIndex < 0) previewOriginIndex = gridIndex;

        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count == 0) return;

        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit

        // Convert grid indices to world positions (waypoints)
        List<Vector3> waypoints = new List<Vector3>();
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;
            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;  // Offset for XY plane rendering
            waypoints.Add(pos);
        }

        if (waypoints.Count < 2) return;

        // Create LineRenderer in world space (not parented to block)
        GameObject lineObj = new GameObject("TransportPathLine");
        pathLineRenderer = lineObj.AddComponent<LineRenderer>();

        // Generate smooth bezier curve between waypoints (10 segments per curve)
        List<Vector3> smoothPath = GenerateBezierPath(waypoints, 10);

        // Configure LineRenderer appearance
        pathLineRenderer.positionCount = smoothPath.Count;
        pathLineRenderer.SetPositions(smoothPath.ToArray());
        pathLineRenderer.startWidth = 0.05f;
        pathLineRenderer.endWidth = 0.05f;
        pathLineRenderer.material = GetLineMaterial();
        pathLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathLineRenderer.receiveShadows = false;
        pathLineRenderer.useWorldSpace = true;

        // Create directional arrowhead at the end of the path
        CreateArrowhead(waypoints[waypoints.Count - 1], waypoints[waypoints.Count - 2]);
    }

    private void ClearPreview()
    {
        if (pathLineRenderer != null)
        {
            if (pathLineRenderer.gameObject != null)
                Destroy(pathLineRenderer.gameObject);
            pathLineRenderer = null;
        }

        if (arrowheadObject != null)
        {
            Destroy(arrowheadObject);
            arrowheadObject = null;
        }
    }

    private Material GetLineMaterial()
    {
        Material lineMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        lineMat.color = Color.white;
        return lineMat;
    }

    private static GridManager GetGridManager()
    {
        if (GridManager.Instance != null) return GridManager.Instance;
        return Object.FindAnyObjectByType<GridManager>();
    }

    /// <summary>
    /// Generates smooth bezier curves between waypoints to create a curved path.
    /// Adjacent segments are smoothed at their connection points for continuity.
    /// </summary>
    /// <param name="segmentsPerCurve">Number of line segments per bezier curve (higher = smoother)</param>
    private List<Vector3> GenerateBezierPath(List<Vector3> waypoints, int segmentsPerCurve)
    {
        List<Vector3> smoothPath = new List<Vector3>();

        if (waypoints.Count < 2)
            return waypoints;

        // Generate a bezier curve for each segment between waypoints
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[i];      // Start point
            Vector3 p3 = waypoints[i + 1];  // End point

            // Calculate control points for cubic bezier curve
            Vector3 direction = (p3 - p0);
            float distance = direction.magnitude;

            // Default control points at 1/3 and 2/3 along the segment
            Vector3 p1 = p0 + direction * 0.33f;
            Vector3 p2 = p0 + direction * 0.66f;

            // Adjust control points for smooth transitions between curves
            if (i > 0)
            {
                // Average the current and previous directions for smooth entry
                Vector3 prevDirection = (p0 - waypoints[i - 1]).normalized;
                p1 = p0 + (direction.normalized + prevDirection).normalized * (distance * 0.33f);
            }

            if (i < waypoints.Count - 2)
            {
                // Average the current and next directions for smooth exit
                Vector3 nextDirection = (waypoints[i + 2] - p3).normalized;
                p2 = p3 - (direction.normalized + nextDirection).normalized * (distance * 0.33f);
            }

            // Sample points along the bezier curve
            for (int j = 0; j < segmentsPerCurve; j++)
            {
                float t = j / (float)segmentsPerCurve;
                Vector3 point = CalculateBezierPoint(t, p0, p1, p2, p3);
                smoothPath.Add(point);
            }
        }

        // Add the final waypoint
        smoothPath.Add(waypoints[waypoints.Count - 1]);

        return smoothPath;
    }

    /// <summary>
    /// Calculates a point on a cubic bezier curve using the standard formula:
    /// B(t) = (1-t)³P0 + 3(1-t)²tP1 + 3(1-t)t²P2 + t³P3
    /// </summary>
    /// <param name="t">Position along curve (0 to 1)</param>
    /// <param name="p0">Start point</param>
    /// <param name="p1">First control point</param>
    /// <param name="p2">Second control point</param>
    /// <param name="p3">End point</param>
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * p0;           // (1-t)³ * P0
        point += 3 * uu * t * p1;           // 3(1-t)² * t * P1
        point += 3 * u * tt * p2;           // 3(1-t) * t² * P2
        point += ttt * p3;                  // t³ * P3

        return point;
    }

    /// <summary>
    /// Creates a cone-shaped arrowhead at the end of the path to show transport direction.
    /// Not parented to the block so it stays stationary in world space.
    /// </summary>
    private void CreateArrowhead(Vector3 endPosition, Vector3 previousPosition)
    {
        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit

        arrowheadObject = new GameObject("TransportArrowhead");
        arrowheadObject.transform.position = endPosition;

        // Point the arrow in the direction of travel
        Vector3 direction = (endPosition - previousPosition).normalized;
        if (direction != Vector3.zero)
        {
            arrowheadObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Build cone mesh and renderer
        MeshFilter meshFilter = arrowheadObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = arrowheadObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateConeMesh();
        meshRenderer.material = GetLineMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // Scale relative to grid cell size
        arrowheadObject.transform.localScale = Vector3.one * (cellSize * 0.21f);
    }

    /// <summary>
    /// Updates the arrowhead position and rotation when the transport direction changes.
    /// Called after each transport to flip the arrow to the opposite end.
    /// </summary>
    private void UpdateArrowheadDirection()
    {
        if (arrowheadObject == null) return;

        GridManager grid = GetGridManager();
        if (grid == null) return;

        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count < 2) return;

        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit

        // Convert path indices to world positions
        List<Vector3> waypoints = new List<Vector3>();
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;
            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;
            waypoints.Add(pos);
        }

        if (waypoints.Count < 2) return;

        // Place arrow at the appropriate end based on transport direction
        Vector3 endPosition;
        Vector3 previousPosition;

        if (isForward)
        {
            // Forward: arrow at last waypoint
            endPosition = waypoints[waypoints.Count - 1];
            previousPosition = waypoints[waypoints.Count - 2];
        }
        else
        {
            // Backward: arrow at first waypoint
            endPosition = waypoints[0];
            previousPosition = waypoints[1];
        }

        // Update arrowhead transform
        arrowheadObject.transform.position = endPosition;
        Vector3 direction = (endPosition - previousPosition).normalized;
        if (direction != Vector3.zero)
        {
            arrowheadObject.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// Procedurally generates a cone mesh for the arrowhead.
    /// The cone points along the +Z axis with a circular base at the origin.
    /// </summary>
    private Mesh CreateConeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowheadCone";

        int segments = 12;  // Circle segments for the base
        Vector3[] vertices = new Vector3[segments + 2];  // Tip + base center + base circle
        int[] triangles = new int[segments * 6];  // 2 triangles per segment (sides + base)

        // Vertex 0: Tip of the cone (points forward along +Z)
        vertices[0] = new Vector3(0, 0, 1f);

        // Vertex 1: Center of the base circle
        vertices[1] = Vector3.zero;

        // Vertices 2+: Base circle (radius 0.5 at z=0)
        float radius = 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

        // Build triangles
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            // Side triangle (tip to base edge)
            triangles[i * 6 + 0] = 0;
            triangles[i * 6 + 1] = i + 2;
            triangles[i * 6 + 2] = next + 2;

            // Base triangle (center to base edge)
            triangles[i * 6 + 3] = 1;
            triangles[i * 6 + 4] = next + 2;
            triangles[i * 6 + 5] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }


    /// <summary>
    /// Waits for Lem to exit the block after transport, then re-arms the trigger.
    /// Prevents immediate re-triggering when the block alternates direction.
    /// </summary>
    private void Update()
    {
        if (!waitingForExit || isTransporting) return;

        // Check if Lem has left the block
        if (!IsPlayerOverBlock())
        {
            // Re-arm the trigger for the next transport
            waitingForExit = false;
            isArmed = true;
            SetCenterTriggerEnabled(true);
            if (debugLogs) Debug.Log("TransporterBlock: Lem cleared block, re-armed", this);
        }
    }

    /// <summary>
    /// Snaps Lem to the block's center position to prevent visual misalignment during transport.
    /// Only snaps once per ride to avoid fighting with other positioning logic.
    /// </summary>
    private void TrySnapLemToBlockCenter(LemController lem)
    {
        if (lem == null || hasSnappedThisRide) return;

        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit
        float topPlaneY = transform.position.y + (cellSize * 0.5f);
        Vector3 pos = lem.transform.position;
        Collider lemCollider = lem.GetComponent<Collider>();

        if (lemCollider != null)
        {
            // Use collider bounds to position Lem's feet at the block's top surface
            float footY = lemCollider.bounds.min.y;
            float deltaY = topPlaneY - footY;
            if (Mathf.Abs(deltaY) <= 0.02f) return;  // Already close enough
            pos = lem.transform.position + new Vector3(0f, deltaY, 0f);
        }
        else
        {
            // Fallback: place at block center if no collider
            pos = new Vector3(transform.position.x, topPlaneY, transform.position.z);
        }

        // Lock to block's X and Z position
        pos.x = transform.position.x;
        pos.z = transform.position.z;
        lem.transform.position = pos;
        hasSnappedThisRide = true;
    }

    /// <summary>
    /// Checks if Lem is still physically overlapping this block.
    /// Used to detect when Lem has exited after transport.
    /// </summary>
    private bool IsPlayerOverBlock()
    {
        Collider blockCollider = GetComponent<Collider>();
        if (blockCollider == null) return false;

        // Check for overlapping colliders in the block's bounds
        Vector3 center = blockCollider.bounds.center;
        Vector3 halfExtents = blockCollider.bounds.extents;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player")) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
        BuildPreview();
    }
#endif
}
