using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crumbler block: cracks on center reach, then falls apart after Lem steps off.
/// </summary>
public class CrumblerBlock : BaseBlock
{
    [Header("Crumble Visuals")]
    [SerializeField] private Color crumbleColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private string crumbleRootName = "CrumbleBlocks";
    [SerializeField] private string particleSystemName = "FallingStoneFragments";

    [Header("Crumble Timing")]
    [SerializeField] private float breakDelaySeconds = 2f;
    [SerializeField] private float debrisLifetimeSeconds = 5f;

    [Header("Debris Physics")]
    [SerializeField] private float debrisMass = 0.2f;
    [SerializeField] private float debrisKick = 0.05f;
    [SerializeField] private bool addBoxColliderIfMissing = true;

    [Header("Debug")]
    [SerializeField] private bool debugCrumbleLogs = true;

    private Renderer[] blockRenderers;
    private ParticleSystem fallingStoneFragments;
    private Transform crumbleRoot;
    private readonly List<GameObject> looseBricks = new List<GameObject>(256);

    private bool hasTriggeredCenter;
    private bool collapseStarted;

    private static Material crackMaterial;

    protected override void Awake()
    {
        base.Awake();
        CacheRenderers();
        CacheSceneReferences();
        StopParticlesAtStartup();
    }

    protected override void OnPlayerReachCenter()
    {
        if (hasTriggeredCenter) return;
        hasTriggeredCenter = true;

        ApplyCrackVisual();
        PlayCenterParticles();
    }

    protected override void OnPlayerExit()
    {
        if (collapseStarted) return;
        collapseStarted = true;

        LogCrumble($"[Crumbler] Block {gridIndex}: player exited, break in {breakDelaySeconds:0.00}s then despawn debris in {debrisLifetimeSeconds:0.00}s.");
        StartCoroutine(CrumbleSequence());
    }

    private IEnumerator CrumbleSequence()
    {
        yield return new WaitForSeconds(breakDelaySeconds);

        // Move bricks out from under this block so we can destroy the block later without deleting debris early.
        ReleaseBricksAsDebris();

        // The block should no longer be interactable once it starts collapsing.
        DisableBlockInteraction();

        // Remove the holder object after bricks are detached.
        if (crumbleRoot != null)
        {
            Destroy(crumbleRoot.gameObject);
            crumbleRoot = null;
        }

        yield return new WaitForSeconds(debrisLifetimeSeconds);

        DespawnLooseBricks();
        DestroyBlock();
    }

    private void ApplyCrackVisual()
    {
        if (blockRenderers == null || blockRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (blockRenderers == null || blockRenderers.Length == 0)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: no renderer found for crack visual.");
            return;
        }

        if (crackMaterial == null)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            crackMaterial = new Material(shader);
            crackMaterial.SetFloat("_Metallic", 0f);
            crackMaterial.SetFloat("_Glossiness", 0f);
        }

        crackMaterial.color = crumbleColor;

        var appliedCount = 0;
        foreach (var renderer in blockRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            if (renderer is ParticleSystemRenderer) continue;
            renderer.material = crackMaterial;
            appliedCount++;
        }

        LogCrumble($"[Crumbler] Block {gridIndex}: crack visual applied to {appliedCount} renderer(s).");
    }

    private void PlayCenterParticles()
    {
        if (fallingStoneFragments == null)
        {
            CacheSceneReferences();
        }

        if (fallingStoneFragments == null)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: particle system '{particleSystemName}' not found.");
            return;
        }

        var emission = fallingStoneFragments.emission;
        emission.enabled = true;
        fallingStoneFragments.Play(true);
        LogCrumble($"[Crumbler] Block {gridIndex}: started particle effect '{particleSystemName}'.");
    }

    private void StopParticlesAtStartup()
    {
        if (fallingStoneFragments == null) return;
        fallingStoneFragments.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void ReleaseBricksAsDebris()
    {
        if (crumbleRoot == null)
        {
            CacheSceneReferences();
        }

        if (crumbleRoot == null)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: crumble root '{crumbleRootName}' not found.");
            return;
        }

        looseBricks.Clear();

        var brickContainer = crumbleRoot.Find("Bricks");
        var roots = new HashSet<Transform>();

        if (brickContainer != null)
        {
            for (var i = 0; i < brickContainer.childCount; i++)
            {
                var child = brickContainer.GetChild(i);
                if (child != null)
                {
                    roots.Add(child);
                }
            }
        }
        else
        {
            var renderers = crumbleRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (renderer.GetComponentInParent<ParticleSystem>() != null) continue;

                var root = GetRootUnder(renderer.transform, crumbleRoot);
                if (root != null)
                {
                    roots.Add(root);
                }
            }
        }

        foreach (var root in roots)
        {
            if (root == null) continue;

            var brick = root.gameObject;
            looseBricks.Add(brick);

            brick.transform.SetParent(transform.parent, true);
            EnsureDebrisPhysics(brick);
        }

        LogCrumble($"[Crumbler] Block {gridIndex}: released {looseBricks.Count} brick(s) as debris.");
    }

    private void EnsureDebrisPhysics(GameObject brick)
    {
        if (brick == null) return;

        var rb = brick.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = brick.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = Mathf.Max(0.01f, debrisMass);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var colliders = brick.GetComponentsInChildren<Collider>(true);
        if ((colliders == null || colliders.Length == 0) && addBoxColliderIfMissing)
        {
            brick.AddComponent<BoxCollider>();
        }

        if (debrisKick > 0f)
        {
            var kick = new Vector3(
                Random.Range(-debrisKick, debrisKick),
                Random.Range(0f, debrisKick),
                Random.Range(-debrisKick, debrisKick)
            );
            rb.AddForce(kick, ForceMode.Impulse);
        }
    }

    private void DisableBlockInteraction()
    {
        var colliders = GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        SetCenterTriggerEnabled(false);
    }

    private void DespawnLooseBricks()
    {
        for (var i = 0; i < looseBricks.Count; i++)
        {
            var brick = looseBricks[i];
            if (brick != null)
            {
                Destroy(brick);
            }
        }

        looseBricks.Clear();
        LogCrumble($"[Crumbler] Block {gridIndex}: despawned loose debris.");
    }

    private void CacheRenderers()
    {
        blockRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CacheSceneReferences()
    {
        crumbleRoot = FindChildRecursive(transform, crumbleRootName);

        if (crumbleRoot != null)
        {
            var particleRoot = FindChildRecursive(crumbleRoot, particleSystemName);
            if (particleRoot != null)
            {
                fallingStoneFragments = particleRoot.GetComponent<ParticleSystem>();
            }
        }

        if (fallingStoneFragments == null)
        {
            var directParticle = FindChildRecursive(transform, particleSystemName);
            if (directParticle != null)
            {
                fallingStoneFragments = directParticle.GetComponent<ParticleSystem>();
            }
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        if (root.name == childName) return root;

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var match = FindChildRecursive(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Transform GetRootUnder(Transform leaf, Transform ancestor)
    {
        if (leaf == null || ancestor == null) return null;

        var current = leaf;
        while (current != null && current.parent != ancestor)
        {
            current = current.parent;
        }

        return current != null && current.parent == ancestor ? current : null;
    }

    private void LogCrumble(string message)
    {
        if (!debugCrumbleLogs) return;
        DebugLog.Crumbler(message, this);
    }
}
