using UnityEngine;

/// <summary>
/// Crumbler block: darkens when Lem reaches center, then crumbles when Lem exits.
/// </summary>
public class CrumblerBlock : BaseBlock
{
    private Renderer[] blockRenderers;
    [Header("Crumble Settings")]
    public Color crumbleColor = new Color(0.4f, 0.4f, 0.4f);
    public float crumbleDelay = 0.2f;
    [SerializeField] private bool debugCrumbleLogs = true;
    private bool isCrumbing = false;
    // (unused) kept for backward compatibility if serialized
#pragma warning disable CS0414
    [SerializeField] private bool isCracked = false;
#pragma warning restore CS0414
    private static Material crackMaterial;
    private bool loggedEnter = false;
    private bool loggedCenter = false;

    protected override void Awake()
    {
        base.Awake();
        CacheRenderers();
    }

    protected override void OnPlayerReachCenter()
    {
        // We might be on a prefab with a disabled root renderer and a visible child (Visual).
        // Cache all renderers so we can update the ones that actually draw.
        if (blockRenderers == null || blockRenderers.Length == 0)
        {
            CacheRenderers();
        }
        if (blockRenderers == null || blockRenderers.Length == 0)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: missing renderer, cannot apply crack color.");
            return;
        }

        if (crackMaterial == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            crackMaterial = new Material(shader);
            // Set non-metallic properties
            crackMaterial.SetFloat("_Metallic", 0f);
            crackMaterial.SetFloat("_Glossiness", 0f);
        }

        crackMaterial.color = crumbleColor;
        // Apply to all enabled renderers to hit the visible mesh, not the disabled root.
        int appliedCount = 0;
        foreach (Renderer renderer in blockRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            renderer.material = crackMaterial;
            appliedCount++;
        }
        isCracked = true;
        loggedCenter = true;
        LogCrumble($"[Crumbler] Block {gridIndex}: reached center, applied crack color {crumbleColor} to {appliedCount} renderer(s).");
    }

    protected override void OnPlayerExit()
    {
        if (isCrumbing) return;
        isCrumbing = true;
        loggedEnter = false;
        loggedCenter = false;
        LogCrumble($"[Crumbler] Block {gridIndex}: player exited, crumbling in {crumbleDelay:0.00}s.");
        StartCoroutine(CrumbleAndDestroy());
    }

    private System.Collections.IEnumerator CrumbleAndDestroy()
    {
        yield return new WaitForSeconds(crumbleDelay);
        LogCrumble($"[Crumbler] Block {gridIndex}: destroy now.");
        DestroyBlock();
    }

    protected override void OnPlayerEnter()
    {
        if (loggedEnter) return;
        loggedEnter = true;

        Collider playerCollider = currentPlayer != null ? currentPlayer.GetComponent<Collider>() : null;
        CenterTrigger centerTrigger = GetComponentInChildren<CenterTrigger>();
        SphereCollider sphere = centerTrigger != null ? centerTrigger.GetComponent<SphereCollider>() : null;

        if (playerCollider == null || sphere == null)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: entered. Missing {(playerCollider == null ? "player collider" : "center trigger collider")}.");
            return;
        }

        Vector3 footPoint = GetFootPoint(playerCollider);
        float distance = Vector3.Distance(footPoint, sphere.transform.position);
        LogCrumble($"[Crumbler] Block {gridIndex}: entered. Foot→center distance {distance:0.###}, radius {sphere.radius:0.###}. Renderers {GetRendererSummary()}");
        if (!loggedCenter)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: waiting for center reach...");
        }
    }

    private void CacheRenderers()
    {
        blockRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private string GetRendererSummary()
    {
        if (blockRenderers == null || blockRenderers.Length == 0) return "0";
        int enabledCount = 0;
        foreach (Renderer renderer in blockRenderers)
        {
            if (renderer != null && renderer.enabled) enabledCount++;
        }
        return $"{enabledCount}/{blockRenderers.Length}";
    }

    private void LogCrumble(string message)
    {
        if (!debugCrumbleLogs) return;
        DebugLog.Crumbler(message, this);
    }

    private static Vector3 GetFootPoint(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        return new Vector3(center.x, bounds.min.y, center.z);
    }
}
