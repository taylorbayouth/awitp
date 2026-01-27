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
    public Color previewColor = new Color(0f, 1f, 1f, 0.25f);

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
        LemController lem = currentPlayer != null ? currentPlayer : FindObjectOfType<LemController>();
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
        // Stop any running transport coroutine to prevent state corruption
        StopAllCoroutines();

        // Clean up preview material to prevent memory leak
        if (previewMaterialInstance != null)
        {
            Destroy(previewMaterialInstance);
            previewMaterialInstance = null;
        }

        // Clear preview objects
        ClearPreview();

        // Reset state in case coroutine was interrupted mid-transport
        if (isTransporting && currentPlayer != null)
        {
            // Attempt to restore Lem state if we were interrupted mid-transport
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

    /// <summary>
    /// Resets the transporter to its initial state. Useful for level resets.
    /// </summary>
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
            if (!lemRb.isKinematic)
            {
                lemRb.linearVelocity = Vector3.zero;
            }
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

    /// <summary>
    /// Returns all grid indices along this transporter's route.
    /// These positions are blocked for other block placement.
    /// </summary>
    public override int[] GetBlockedIndices()
    {
        List<int> pathIndices = GetRoutePathIndices();
        // Exclude the origin (where this block sits) - it's already occupied by this block
        if (pathIndices.Count > 0 && pathIndices[0] == gridIndex)
        {
            pathIndices.RemoveAt(0);
        }
        return pathIndices.ToArray();
    }

    /// <summary>
    /// Checks if this transporter can be placed at the target index.
    /// Validates that the entire route path is clear of other blocks.
    /// </summary>
    public override bool CanBePlacedAt(int targetIndex, GridManager grid)
    {
        if (grid == null) return true;

        // Calculate what the route would be from this position
        Vector2Int current = grid.IndexToCoordinates(targetIndex);
        List<Vector2Int> steps = BuildSteps();

        foreach (Vector2Int step in steps)
        {
            current += step;
            if (!grid.IsValidCoordinates(current.x, current.y))
            {
                continue; // Out of bounds positions are handled elsewhere
            }

            int idx = grid.CoordinatesToIndex(current);

            // Check if there's already a block at this position
            if (grid.HasBlockAtIndex(idx))
            {
                return false;
            }

            // Check if another transporter's route passes through here
            if (grid.IsIndexBlockedByAnyBlock(idx, this))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns error message explaining why placement failed.
    /// </summary>
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
            {
                return $"Route path blocked by existing block at index {idx}";
            }

            if (grid.IsIndexBlockedByAnyBlock(idx, this))
            {
                return $"Route path conflicts with another transporter's route at index {idx}";
            }
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
        if (grid.IsValidIndex(originIndex))
        {
            unique.Add(originIndex);
        }
        foreach (Vector2Int step in steps)
        {
            current += step;
            if (grid.IsValidCoordinates(current.x, current.y))
            {
                int idx = grid.CoordinatesToIndex(current);
                if (unique.Add(idx))
                {
                    indices.Add(idx);
                }
            }
        }
        if (unique.Contains(originIndex))
        {
            indices.Insert(0, originIndex);
        }
        return indices;
    }

    private void OnDrawGizmos()
    {
        if (!showRoutePreview) return;
        GridManager grid = GetGridManager();
        if (grid == null) return;

        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count == 0) return;

        Gizmos.color = previewColor;
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

        if (previewOriginIndex < 0)
        {
            previewOriginIndex = gridIndex;
        }
        List<int> pathIndices = GetRoutePathIndices();
        if (pathIndices.Count == 0) return;

        Material mat = GetPreviewMaterial(previewColor);
        float cellSize = grid.cellSize;
        Vector3 scale = Vector3.one * cellSize;

        previewWorldPositions.Clear();
        foreach (int index in pathIndices)
        {
            if (!grid.IsValidIndex(index)) continue;

            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "TransportPreview";
            preview.transform.SetParent(transform, false);

            Vector3 pos = grid.IndexToWorldPosition(index);
            pos.z += cellSize * 0.5f;
            preview.transform.position = pos;
            preview.transform.localScale = scale;
            previewWorldPositions.Add(pos);

            Collider col = preview.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Renderer renderer = preview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            previewBlocks.Add(preview);
        }
    }

    private void ClearPreview()
    {
        for (int i = 0; i < previewBlocks.Count; i++)
        {
            if (previewBlocks[i] != null)
            {
                Destroy(previewBlocks[i]);
            }
        }
        previewBlocks.Clear();
        previewWorldPositions.Clear();
    }

    private Material GetPreviewMaterial(Color color)
    {
        if (previewMaterialInstance == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            previewMaterialInstance = new Material(shader);
        }

        previewMaterialInstance.color = color;
        ConfigureMaterialTransparency(previewMaterialInstance);
        return previewMaterialInstance;
    }

    private static void ConfigureMaterialTransparency(Material mat)
    {
        if (mat == null) return;
        if (mat.shader != null && mat.shader.name == "Standard")
        {
            // Set non-metallic properties
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0f);

            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
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
            GameObject preview = previewBlocks[i];
            if (preview == null) continue;
            preview.transform.position = previewWorldPositions[i];
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
        if (lem == null) return;
        if (hasSnappedThisRide) return;
        float topPlaneY = transform.position.y + (cellSize * 0.5f);
        Vector3 pos = lem.transform.position;
        Collider lemCollider = lem.GetComponent<Collider>();
        if (lemCollider != null)
        {
            float footY = lemCollider.bounds.min.y;
            float deltaY = topPlaneY - footY;
            if (Mathf.Abs(deltaY) > 0.02f)
            {
                pos = lem.transform.position + new Vector3(0f, deltaY, 0f);
            }
            else
            {
                return;
            }
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
            if (hit.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            BuildPreview();
        }
    }
#endif
}
