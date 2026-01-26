using UnityEngine;

/// <summary>
/// Crumbler block: darkens when Lem reaches center, then crumbles when Lem exits.
/// </summary>
public class CrumblerBlock : BaseBlock
{
    private Renderer blockRenderer;
    [Header("Crumble Settings")]
    public Color crumbleColor = new Color(0.05f, 0.05f, 0.05f);
    public float crumbleDelay = 0.5f;
    private bool isCrumbing = false;

    protected override void Awake()
    {
        base.Awake();
        blockRenderer = GetComponentInChildren<Renderer>();
    }

    protected override void OnPlayerReachCenter()
    {
        if (blockRenderer != null)
        {
            blockRenderer.material.color = crumbleColor;
        }
    }

    protected override void OnPlayerExit()
    {
        if (isCrumbing) return;
        isCrumbing = true;
        StartCoroutine(CrumbleAndDestroy());
    }

    private System.Collections.IEnumerator CrumbleAndDestroy()
    {
        yield return new WaitForSeconds(crumbleDelay);
        DestroyBlock();
    }
}
