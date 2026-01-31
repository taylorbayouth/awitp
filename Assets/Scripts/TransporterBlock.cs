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

    private bool isTransporting = false;
    private bool isForward = true;
    private bool isArmed = true;
    private bool waitingForExit = false;
    private LineRenderer pathLineRenderer;
    private GameObject arrowheadObject;
    private int previewOriginIndex = -1;
    private bool hasSnappedThisRide = false;

    protected override void OnPlayerReachCenter()
    {
        if (isTransporting || !isArmed) return;
        LemController lem = currentPlayer != null ? currentPlayer : UnityEngine.Object.FindAnyObjectByType<LemController>();
        if (lem == null)
        {
            Debug.LogError("Cannot transport - Lem not found");
            return;
        }

        List<Vector2Int> steps = BuildSteps();
        if (steps.Count == 0) return;

        if (!isForward)
        {
            steps = RouteParser.ReverseSteps(steps);
        }

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

    private IEnumerator TransportRoutine(LemController lem, List<Vector2Int> steps)
    {
        isTransporting = true;
        hasSnappedThisRide = false;

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

        lem.transform.SetParent(transform, true);

        GridManager grid = GetGridManager();
        float cellSize = grid != null ? grid.cellSize : 1f;
        Vector2Int currentCoords = grid != null ? grid.IndexToCoordinates(gridIndex) : Vector2Int.zero;

        TrySnapLemToBlockCenter(lem, cellSize);

        foreach (Vector2Int step in steps)
        {
            Vector2Int nextCoords = currentCoords + step;
            Vector3 startPos = transform.position;
            Vector3 targetPos = grid != null ? grid.CoordinatesToWorldPosition(nextCoords) : (transform.position + new Vector3(step.x * cellSize, step.y * cellSize, 0f));
            targetPos.z += cellSize * 0.5f;

            float distance = Vector3.Distance(startPos, targetPos);
            float speed = Mathf.Max(0.01f, unitsPerSecond * speedMultiplier);
            float duration = Mathf.Max(0.01f, distance / speed);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t));
                yield return null;
            }

            currentCoords = nextCoords;
        }

        if (grid != null)
        {
            grid.UnregisterBlock(this);
            gridIndex = grid.CoordinatesToIndex(currentCoords);
            grid.RegisterBlock(this);
        }

        lem.transform.SetParent(null, true);
        if (lemRb != null)
        {
            lemRb.isKinematic = prevKinematic;
            lemRb.useGravity = prevUseGravity;
        }
        lem.SetFrozen(false);
        if (debugLogs) Debug.Log("TransporterBlock: Lem unfrozen and detached", this);

        isForward = !isForward;
        UpdateArrowheadDirection();
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

    public override int[] GetBlockedIndices()
    {
        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count > 0 && pathIndices[0] == gridIndex)
        {
            pathIndices.RemoveAt(0);
        }
        return pathIndices.ToArray();
    }

    public override bool CanBePlacedAt(int targetIndex, GridManager grid)
    {
        if (grid == null) return true;

        Vector2Int current = grid.IndexToCoordinates(targetIndex);
        List<Vector2Int> steps = BuildSteps();

        foreach (Vector2Int step in steps)
        {
            current += step;
            if (!grid.IsValidCoordinates(current.x, current.y)) continue;

            int idx = grid.CoordinatesToIndex(current);

            if (grid.HasBlockAtIndex(idx)) return false;
            if (grid.IsIndexBlockedByAnyBlock(idx, this)) return false;
        }

        return true;
    }

    public override string GetPlacementErrorMessage(int targetIndex, GridManager grid)
    {
        if (grid == null) return null;

        Vector2Int current = grid.IndexToCoordinates(targetIndex);
        List<Vector2Int> steps = BuildSteps();

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

    private void BuildPreview()
    {
        ClearPreview();
        if (!showRoutePreview) return;

        GridManager grid = GetGridManager();
        if (grid == null) return;

        if (previewOriginIndex < 0) previewOriginIndex = gridIndex;

        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count == 0) return;

        float cellSize = grid.cellSize;

        // Collect waypoints
        List<Vector3> waypoints = new List<Vector3>();
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;
            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;
            waypoints.Add(pos);
        }

        if (waypoints.Count < 2) return;

        // Create LineRenderer
        GameObject lineObj = new GameObject("TransportPathLine");
        // Don't parent to the block - keep it in world space so it doesn't move with the block
        pathLineRenderer = lineObj.AddComponent<LineRenderer>();

        // Generate smooth bezier curve points
        List<Vector3> smoothPath = GenerateBezierPath(waypoints, 10);

        // Configure LineRenderer
        pathLineRenderer.positionCount = smoothPath.Count;
        pathLineRenderer.SetPositions(smoothPath.ToArray());
        pathLineRenderer.startWidth = 0.05f;
        pathLineRenderer.endWidth = 0.05f;
        pathLineRenderer.material = GetLineMaterial();
        pathLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathLineRenderer.receiveShadows = false;
        pathLineRenderer.useWorldSpace = true;

        // Create arrowhead at the end
        CreateArrowhead(waypoints[waypoints.Count - 1], waypoints[waypoints.Count - 2], cellSize);
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

    private List<Vector3> GenerateBezierPath(List<Vector3> waypoints, int segmentsPerCurve)
    {
        List<Vector3> smoothPath = new List<Vector3>();

        if (waypoints.Count < 2)
            return waypoints;

        // For each segment between waypoints, create a bezier curve
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[i];
            Vector3 p3 = waypoints[i + 1];

            // Calculate control points for bezier curve
            Vector3 direction = (p3 - p0);
            float distance = direction.magnitude;

            // Control points offset by 1/3 of the distance
            Vector3 p1 = p0 + direction * 0.33f;
            Vector3 p2 = p0 + direction * 0.66f;

            // If we have adjacent segments, smooth the transition
            if (i > 0)
            {
                Vector3 prevDirection = (p0 - waypoints[i - 1]).normalized;
                p1 = p0 + (direction.normalized + prevDirection).normalized * (distance * 0.33f);
            }

            if (i < waypoints.Count - 2)
            {
                Vector3 nextDirection = (waypoints[i + 2] - p3).normalized;
                p2 = p3 - (direction.normalized + nextDirection).normalized * (distance * 0.33f);
            }

            // Generate points along the bezier curve
            for (int j = 0; j < segmentsPerCurve; j++)
            {
                float t = j / (float)segmentsPerCurve;
                Vector3 point = CalculateBezierPoint(t, p0, p1, p2, p3);
                smoothPath.Add(point);
            }
        }

        // Add the final point
        smoothPath.Add(waypoints[waypoints.Count - 1]);

        return smoothPath;
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * p0;
        point += 3 * uu * t * p1;
        point += 3 * u * tt * p2;
        point += ttt * p3;

        return point;
    }

    private void CreateArrowhead(Vector3 endPosition, Vector3 previousPosition, float cellSize)
    {
        arrowheadObject = new GameObject("TransportArrowhead");
        // Don't parent to the block - keep it in world space so it doesn't move with the block
        arrowheadObject.transform.position = endPosition;

        // Calculate direction for arrow rotation
        Vector3 direction = (endPosition - previousPosition).normalized;
        if (direction != Vector3.zero)
        {
            arrowheadObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Create a simple cone mesh for the arrowhead
        MeshFilter meshFilter = arrowheadObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = arrowheadObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateConeMesh();
        meshRenderer.material = GetLineMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // Scale the arrowhead
        arrowheadObject.transform.localScale = Vector3.one * (cellSize * 0.21f);
    }

    private void UpdateArrowheadDirection()
    {
        if (arrowheadObject == null) return;

        GridManager grid = GetGridManager();
        if (grid == null) return;

        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count < 2) return;

        float cellSize = grid.cellSize;

        // Get waypoints
        List<Vector3> waypoints = new List<Vector3>();
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;
            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;
            waypoints.Add(pos);
        }

        if (waypoints.Count < 2) return;

        // Determine which end gets the arrow based on isForward
        Vector3 endPosition;
        Vector3 previousPosition;

        if (isForward)
        {
            // Arrow points to the last waypoint
            endPosition = waypoints[waypoints.Count - 1];
            previousPosition = waypoints[waypoints.Count - 2];
        }
        else
        {
            // Arrow points to the first waypoint (going backward)
            endPosition = waypoints[0];
            previousPosition = waypoints[1];
        }

        // Update arrowhead position and rotation
        arrowheadObject.transform.position = endPosition;
        Vector3 direction = (endPosition - previousPosition).normalized;
        if (direction != Vector3.zero)
        {
            arrowheadObject.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private Mesh CreateConeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowheadCone";

        int segments = 12;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        // Tip of the cone (pointing forward)
        vertices[0] = new Vector3(0, 0, 1f);

        // Base center
        vertices[1] = Vector3.zero;

        // Base circle
        float radius = 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

        // Side triangles (cone sides)
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            triangles[i * 6] = 0;
            triangles[i * 6 + 1] = i + 2;
            triangles[i * 6 + 2] = next + 2;

            // Base triangles
            triangles[i * 6 + 3] = 1;
            triangles[i * 6 + 4] = next + 2;
            triangles[i * 6 + 5] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }


    private void Update()
    {
        if (!waitingForExit || isTransporting) return;
        if (!IsPlayerOverBlock())
        {
            waitingForExit = false;
            isArmed = true;
            SetCenterTriggerEnabled(true);
            if (debugLogs) Debug.Log("TransporterBlock: Lem cleared block, re-armed", this);
        }
    }

    private void TrySnapLemToBlockCenter(LemController lem, float cellSize)
    {
        if (lem == null || hasSnappedThisRide) return;

        float topPlaneY = transform.position.y + (cellSize * 0.5f);
        Vector3 pos = lem.transform.position;
        Collider lemCollider = lem.GetComponent<Collider>();

        if (lemCollider != null)
        {
            float footY = lemCollider.bounds.min.y;
            float deltaY = topPlaneY - footY;
            if (Mathf.Abs(deltaY) <= 0.02f) return;
            pos = lem.transform.position + new Vector3(0f, deltaY, 0f);
        }
        else
        {
            pos = new Vector3(transform.position.x, topPlaneY, transform.position.z);
        }

        pos.x = transform.position.x;
        pos.z = transform.position.z;
        lem.transform.position = pos;
        hasSnappedThisRide = true;
    }

    private bool IsPlayerOverBlock()
    {
        Collider blockCollider = GetComponent<Collider>();
        if (blockCollider == null) return false;

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
