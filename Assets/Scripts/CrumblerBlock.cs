using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crumbler block: emits dust/debris FX and drops a visual rubble pile, while spawning a deterministic
/// invisible support cube so Lem gameplay stays grid-aligned and predictable.
/// </summary>
public class CrumblerBlock : BaseBlock
{
    private const string RuntimeRootName = "__CrumbleRuntime";

    [Header("Crumble Visuals")]
    [SerializeField] private string crumbleRootName = "CrumbleBlocks";
    [SerializeField] private string particleSystemName = "FallingStoneFragments";
    [SerializeField] private string impactParticleSystemName = "PuffOfDust";

    [Header("Crumble Timing")]
    [SerializeField] private float fallDelaySeconds = 0f;
    [SerializeField] private float supportCubeMaxWaitSeconds = 3f;

    [Header("Debris Physics")]
    [SerializeField] private float debrisMass = 0.2f;
    [SerializeField] private float debrisKick = 0.05f;
    [SerializeField] private bool addBoxColliderIfMissing = true;
    [SerializeField] private float impactMinSpeed = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float impactMinUpNormal = 0.35f;
    [SerializeField] private float impactParticleLifetime = 3f;

    [Header("Gameplay Support Cube")]
    [SerializeField] private bool spawnSupportCubeOnFirstBelowImpact = true;
    [SerializeField] private Vector3 supportCubeSize = Vector3.one;
    [SerializeField] private Vector3 supportCubeCenterOffset = Vector3.zero;
    [SerializeField] private bool showSupportCubeVisual = true;
    [SerializeField] private Color supportCubeTint = new Color(1f, 0.2f, 0.2f, 0.35f);

    [Header("Debug")]
    [SerializeField] private bool debugCrumbleLogs = true;

    private ParticleSystem fallingStoneFragments;
    private ParticleSystem puffOfDustTemplate;
    private Transform crumbleRoot;
    private readonly List<GameObject> looseBricks = new List<GameObject>(256);
    private GameObject supportCube;
    private BoxCollider supportCubeCollider;
    private bool supportCubeSpawned;
    private static Mesh supportCubeMesh;
    private static Material supportCubeMaterial;
    private static readonly List<Collider> playerColliders = new List<Collider>(4);
    private static Transform runtimeRoot;

    private bool hasTriggeredCenter;
    private bool collapseStarted;

    protected override void Awake()
    {
        base.Awake();
        CacheSceneReferences();
        StopParticlesAtStartup();
    }

    protected override void OnPlayerReachCenter()
    {
        if (hasTriggeredCenter) return;
        hasTriggeredCenter = true;

        PlayCenterParticles();
    }

    protected override void OnPlayerExit()
    {
        if (collapseStarted) return;
        collapseStarted = true;

        LogCrumble($"[Crumbler] Block {gridIndex}: player exited, collapse in {fallDelaySeconds:0.00}s.");
        StartCoroutine(CrumbleSequence());
    }

    private IEnumerator CrumbleSequence()
    {
        if (fallDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(fallDelaySeconds);
        }

        // Stop gameplay interaction immediately; visuals continue as debris.
        DisableBlockInteraction();

        // Spawn the gameplay support cube immediately based on what's below.
        // This avoids timing issues where Lem falls before debris makes first contact.
        TrySpawnSupportCubeViaRaycast();

        ReleaseBricksAsDebris();

        // Remove the holder object after bricks are detached.
        if (crumbleRoot != null)
        {
            Destroy(crumbleRoot.gameObject);
            crumbleRoot = null;
        }

        yield return WaitForSupportCubeOrTimeout();
        DestroyBlock();
    }

    private void TrySpawnSupportCubeViaRaycast()
    {
        if (!spawnSupportCubeOnFirstBelowImpact || supportCubeSpawned) return;

        var origin = transform.position;
        var hits = Physics.RaycastAll(origin, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));
        RaycastHit hit = default;
        var found = false;
        for (var i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.collider == null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<DebrisImpactRelay>() != null) continue;
            hit = h;
            found = true;
            break;
        }

        if (!found || hit.collider == null) return;

        var hitBlock = hit.collider.GetComponentInParent<BaseBlock>();
        var layer = hit.collider.gameObject.layer;
        var worldPos = hitBlock != null
            ? hitBlock.transform.position + Vector3.up
            : hit.collider.bounds.center + Vector3.up;
        worldPos += supportCubeCenterOffset;

        supportCubeSpawned = true;
        SpawnSupportCube(worldPos, layer);
    }

    private IEnumerator WaitForSupportCubeOrTimeout()
    {
        if (supportCubeSpawned)
        {
            yield break;
        }

        var remaining = Mathf.Max(0f, supportCubeMaxWaitSeconds);
        while (!supportCubeSpawned && remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }
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
        if (fallingStoneFragments != null)
        {
            fallingStoneFragments.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (puffOfDustTemplate != null)
        {
            puffOfDustTemplate.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
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
        RefreshPlayerColliders();

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

            brick.transform.SetParent(GetOrCreateRuntimeRoot(), true);
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
            colliders = brick.GetComponentsInChildren<Collider>(true);
        }

        IgnorePlayerCollisions(colliders);

        if (debrisKick > 0f)
        {
            var kick = new Vector3(
                Random.Range(-debrisKick, debrisKick),
                Random.Range(0f, debrisKick),
                Random.Range(-debrisKick, debrisKick)
            );
            rb.AddForce(kick, ForceMode.Impulse);
        }

        var relay = brick.GetComponent<DebrisImpactRelay>();
        if (relay == null)
        {
            relay = brick.AddComponent<DebrisImpactRelay>();
        }
        relay.Initialize(this, impactMinSpeed, impactMinUpNormal);

        IgnoreSupportCubeCollisions(colliders);
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

        if (puffOfDustTemplate == null)
        {
            var impactParticle = FindChildRecursive(transform, impactParticleSystemName);
            if (impactParticle != null)
            {
                puffOfDustTemplate = impactParticle.GetComponent<ParticleSystem>();
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

    private void HandleDebrisImpact(Collision collision)
    {
        if (collision == null) return;
        if (collision.contactCount <= 0) return;

        if (puffOfDustTemplate == null) return;

        var contact = collision.GetContact(0);
        SpawnImpactParticles(contact.point);
    }

    private void TrySpawnSupportCube(Collision collision)
    {
        if (!spawnSupportCubeOnFirstBelowImpact || supportCubeSpawned) return;
        if (!TryGetSupportCubeWorldPosition(collision, out var worldPos, out var layer)) return;

        supportCubeSpawned = true;
        SpawnSupportCube(worldPos, layer);
    }

    private bool TryGetSupportCubeWorldPosition(Collision collision, out Vector3 worldPos, out int layer)
    {
        worldPos = transform.position;
        layer = gameObject.layer;

        if (collision == null || collision.contactCount <= 0) return false;

        var contact = collision.GetContact(0);
        if (contact.normal.y < impactMinUpNormal) return false;

        if (collision.collider != null)
        {
            layer = collision.collider.gameObject.layer;
        }

        // Preferred path: use the grid-aligned block transform (handles Z offset correctly).
        var hitBlock = collision.collider != null ? collision.collider.GetComponentInParent<BaseBlock>() : null;
        if (hitBlock != null)
        {
            worldPos = hitBlock.transform.position + Vector3.up + supportCubeCenterOffset;
            return true;
        }

        // Fallback: compute from contact point using grid math.
        var grid = GridManager.Instance;
        if (grid == null) return false;

        var sample = contact.point - contact.normal * 0.02f;
        var hitIndex = grid.WorldToGridIndex(sample);
        if (!grid.IsValidIndex(hitIndex)) return false;

        var aboveIndex = hitIndex + grid.gridWidth;
        if (!grid.IsValidIndex(aboveIndex)) return false;

        worldPos = grid.IndexToWorldPosition(aboveIndex);
        worldPos.z = collision.collider != null ? collision.collider.transform.position.z : 0.5f;
        worldPos += supportCubeCenterOffset;
        return true;
    }

    private void SpawnSupportCube(Vector3 worldPos, int layer)
    {
        if (supportCube != null) return;

        supportCube = new GameObject($"CrumbleSupportCube_{gridIndex}");
        supportCube.layer = layer;
        supportCube.transform.SetParent(GetOrCreateRuntimeRoot(), true);
        supportCube.transform.rotation = Quaternion.identity;
        supportCube.transform.localScale = Vector3.one;

        supportCube.transform.position = worldPos;

        supportCube.AddComponent<SupportCubeMarker>();

        supportCubeCollider = supportCube.AddComponent<BoxCollider>();
        supportCubeCollider.isTrigger = false;
        supportCubeCollider.size = supportCubeSize;

        IgnoreSupportCubeCollisionsForAllDebris();

        if (showSupportCubeVisual)
        {
            EnsureSupportCubeVisual();
        }

        LogCrumble($"[Crumbler] Block {gridIndex}: spawned persistent support cube at {supportCube.transform.position}.");
    }

    private void EnsureSupportCubeVisual()
    {
        if (supportCube == null) return;

        if (supportCubeMesh == null)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            supportCubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }

        if (supportCubeMaterial == null)
        {
            var shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            supportCubeMaterial = new Material(shader);
            supportCubeMaterial.SetFloat("_Metallic", 0f);
            supportCubeMaterial.SetFloat("_Glossiness", 0f);
            ForceMaterialTransparent(supportCubeMaterial);
        }

        supportCubeMaterial.color = supportCubeTint;

        var visual = new GameObject("SupportCubeVisual");
        visual.transform.SetParent(supportCube.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = supportCubeSize;

        var filter = visual.AddComponent<MeshFilter>();
        filter.sharedMesh = supportCubeMesh;

        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = supportCubeMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void IgnorePlayerCollisions(Collider[] debrisColliders)
    {
        if (debrisColliders == null || debrisColliders.Length == 0) return;

        if (playerColliders.Count == 0)
        {
            RefreshPlayerColliders();
        }

        if (playerColliders.Count == 0) return;

        foreach (var debrisCollider in debrisColliders)
        {
            if (debrisCollider == null) continue;
            foreach (var playerCollider in playerColliders)
            {
                if (playerCollider == null) continue;
                Physics.IgnoreCollision(debrisCollider, playerCollider, true);
            }
        }
    }

    private static void RefreshPlayerColliders()
    {
        playerColliders.Clear();
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            if (player == null) continue;
            var collider = player.GetComponent<Collider>();
            if (collider != null)
            {
                playerColliders.Add(collider);
            }
        }
    }

    private void IgnoreSupportCubeCollisions(Collider[] debrisColliders)
    {
        if (supportCubeCollider == null) return;
        if (debrisColliders == null || debrisColliders.Length == 0) return;

        foreach (var debrisCollider in debrisColliders)
        {
            if (debrisCollider == null) continue;
            Physics.IgnoreCollision(debrisCollider, supportCubeCollider, true);
        }
    }

    private void IgnoreSupportCubeCollisionsForAllDebris()
    {
        if (supportCubeCollider == null) return;

        for (var i = 0; i < looseBricks.Count; i++)
        {
            var brick = looseBricks[i];
            if (brick == null) continue;

            var colliders = brick.GetComponentsInChildren<Collider>(true);
            IgnoreSupportCubeCollisions(colliders);
        }
    }

    private static void ForceMaterialTransparent(Material material)
    {
        if (material == null) return;
        if (!material.HasProperty("_Mode")) return;

        // Standard shader transparency setup.
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    private void SpawnImpactParticles(Vector3 worldPosition)
    {
        if (puffOfDustTemplate == null)
        {
            CacheSceneReferences();
        }

        if (puffOfDustTemplate == null)
        {
            return;
        }

        var dust = Instantiate(puffOfDustTemplate, worldPosition, puffOfDustTemplate.transform.rotation, GetOrCreateRuntimeRoot());
        dust.gameObject.SetActive(true);
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        dust.Play(true);
        Destroy(dust.gameObject, Mathf.Max(0.2f, impactParticleLifetime));
    }

    public static void CleanupRuntime()
    {
        // Prefer destroying the root (everything runtime hangs off of it).
        var rootGo = runtimeRoot != null ? runtimeRoot.gameObject : GameObject.Find(RuntimeRootName);
        if (rootGo != null)
        {
            DestroyRuntimeObject(rootGo);
            runtimeRoot = null;
            return;
        }

        // Fallback cleanup for older builds (or if something got unparented).
        var debris = Object.FindObjectsByType<DebrisImpactRelay>(FindObjectsSortMode.None);
        foreach (var d in debris)
        {
            if (d != null)
            {
                DestroyRuntimeObject(d.gameObject);
            }
        }

        var supports = Object.FindObjectsByType<SupportCubeMarker>(FindObjectsSortMode.None);
        foreach (var s in supports)
        {
            if (s != null)
            {
                DestroyRuntimeObject(s.gameObject);
            }
        }
    }

    private static void DestroyRuntimeObject(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(go);
            return;
        }
#endif
        Object.Destroy(go);
    }

    private static Transform GetOrCreateRuntimeRoot()
    {
        if (runtimeRoot != null) return runtimeRoot;

        var existing = GameObject.Find(RuntimeRootName);
        if (existing != null)
        {
            runtimeRoot = existing.transform;
            return runtimeRoot;
        }

        var root = new GameObject(RuntimeRootName);
        var grid = GridManager.Instance;
        if (grid != null)
        {
            root.transform.SetParent(grid.transform, false);
        }
        runtimeRoot = root.transform;
        return runtimeRoot;
    }

    private sealed class DebrisImpactRelay : MonoBehaviour
    {
        private CrumblerBlock owner;
        private float minSpeed;
        private float minUpNormal;
        private bool hasFired;

        public void Initialize(CrumblerBlock block, float minimumSpeed, float minimumUpNormal)
        {
            owner = block;
            minSpeed = Mathf.Max(0f, minimumSpeed);
            minUpNormal = Mathf.Clamp01(minimumUpNormal);
            hasFired = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (owner == null || collision == null) return;

            // Support cube should spawn immediately on first valid hit with the block below.
            owner.TrySpawnSupportCube(collision);

            // Dust puff is once-per-brick and filtered for meaningful impacts.
            if (hasFired) return;
            if (collision.relativeVelocity.magnitude < minSpeed) return;
            if (collision.contactCount <= 0) return;

            var contact = collision.GetContact(0);
            if (contact.normal.y < minUpNormal) return;

            hasFired = true;
            owner.HandleDebrisImpact(collision);
        }
    }

    private sealed class SupportCubeMarker : MonoBehaviour { }
}
