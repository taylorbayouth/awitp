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
    [Range(0f, 1f)]
    public float previewAlpha = 0.5f;

    private bool isTransporting = false;
    private bool isForward = true;
    private bool isArmed = true;
    private bool waitingForExit = false;
    private readonly List<GameObject> previewBlocks = new List<GameObject>();
    private readonly List<Vector3> previewWorldPositions = new List<Vector3>();
    private Material previewMaterialInstance;
    private int previewOriginIndex = -1;
    private bool hasSnappedThisRide = false;

    protected override void OnPlayerReachCenter()
    {
        if (isTransporting || !isArmed) return;
        LemController lem = currentPlayer != null ? currentPlayer : UnityEngine.Object.FindAnyObjectByType<LemController>();
        if (lem == null) return;

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

        if (previewMaterialInstance != null)
        {
            Destroy(previewMaterialInstance);
            previewMaterialInstance = null;
        }

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

    private void OnDrawGizmos()
    {
        if (!showRoutePreview) return;
        GridManager grid = GetGridManager();
        if (grid == null) return;

        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count == 0) return;

        Gizmos.color = new Color(1f, 1f, 1f, previewAlpha);
        float cellSize = grid.cellSize;
        Vector3 size = Vector3.one * cellSize;
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;
            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;
            Gizmos.DrawCube(pos, size);
        }
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

        Material mat = GetPreviewMaterial();
        float cellSize = grid.cellSize;

        previewWorldPositions.Clear();
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;

            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TransportPreview";
            cube.transform.SetParent(transform, false);
            cube.transform.position = pos;
            cube.transform.localScale = Vector3.one * cellSize;

            // Remove collider from preview
            Collider col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = cube.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            previewWorldPositions.Add(pos);
            previewBlocks.Add(cube);
        }
    }

    private void ClearPreview()
    {
        foreach (var block in previewBlocks)
        {
            if (block != null) Destroy(block);
        }
        previewBlocks.Clear();
        previewWorldPositions.Clear();
    }

    private Material GetPreviewMaterial()
    {
        if (previewMaterialInstance == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Transparent/Diffuse") ??
                           Shader.Find("Standard") ??
                           Shader.Find("Unlit/Color");
            previewMaterialInstance = new Material(shader);
        }

        previewMaterialInstance.color = new Color(1f, 1f, 1f, previewAlpha);

        if (previewMaterialInstance.shader.name == "Standard")
        {
            previewMaterialInstance.SetFloat("_Mode", 3f);
            previewMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterialInstance.SetInt("_ZWrite", 1);
            previewMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
            previewMaterialInstance.renderQueue = 3000;
        }

        return previewMaterialInstance;
    }

    private static GridManager GetGridManager()
    {
        if (GridManager.Instance != null) return GridManager.Instance;
        return Object.FindAnyObjectByType<GridManager>();
    }

    private void LateUpdate()
    {
        if (previewBlocks.Count == 0 || previewWorldPositions.Count == 0) return;
        int count = Mathf.Min(previewBlocks.Count, previewWorldPositions.Count);
        for (int i = 0; i < count; i++)
        {
            if (previewBlocks[i] != null)
                previewBlocks[i].transform.position = previewWorldPositions[i];
        }
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
