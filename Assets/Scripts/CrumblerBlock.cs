using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private string centerParticleSystemName = "FallingStoneFragments-2";
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

    [Header("Crumble SFX")]
    [SerializeField] private AudioClip entrySfx;
    [SerializeField] private AudioClip centerSfx;
    [SerializeField] private AudioClip collapseSfx;
    [SerializeField] private AudioClip impactSfx;
    [SerializeField] private string entrySfxAssetPath = "Assets/SFX/smallRocksFalling2.mp3";
    [SerializeField] private string centerSfxAssetPath = "Assets/SFX/smallRocksFalling.mp3";
    [SerializeField] private string collapseSfxAssetPath = "Assets/SFX/bigRocksFalling.mp3";
    [SerializeField] private string impactSfxAssetPath = "Assets/SFX/bigRocksFallToGround.mp3";

    private ParticleSystem fallingStoneFragments;
    private ParticleSystem fallingStoneFragmentsCenter;
    private ParticleSystem puffOfDustTemplate;
    private Transform crumbleRoot;
    private readonly List<GameObject> looseBricks = new List<GameObject>(256);
    private GameObject supportCube;
    private BoxCollider supportCubeCollider;
    private bool supportCubeSpawned;
    private Vector3 predictedImpactPosition;
    private bool hasPredictedImpactPosition;
    private Transform impactTargetTransform;
    private static Mesh supportCubeMesh;
    private static Material supportCubeMaterial;
    private static readonly List<Collider> playerColliders = new List<Collider>(4);
    private static Transform runtimeRoot;
    private static Transform sfxRoot;

    private bool hasTriggeredCenter;
    private bool collapseStarted;
    private bool hasFirstImpactFired;
    private event System.Action<FirstImpactData> OnFirstImpact;

    public struct FirstImpactData
    {
        public Vector3 position;
        public float time;
        public Collider hitCollider;
        public Vector3 contactNormal;
        public float impactSpeed;
    }

    protected override void Awake()
    {
        base.Awake();
        CacheSceneReferences();
        LoadSfxClipsIfNeeded();
        StopParticlesAtStartup();
    }

    protected override void OnPlayerEnter()
    {
        PlayEntryParticles();
        PlaySfx(entrySfx);
    }

    protected override void OnPlayerReachCenter()
    {
        if (hasTriggeredCenter) return;
        hasTriggeredCenter = true;

        PlayCenterParticles();
        PlaySfx(centerSfx);
    }

    protected override void OnPlayerExit()
    {
        if (collapseStarted) return;
        collapseStarted = true;

        hasFirstImpactFired = false;
        OnFirstImpact = null;
        OnFirstImpact += OnFirstImpact_PlaySfx;
        OnFirstImpact += OnFirstImpact_SpawnDust;

        LogCrumble($"[Crumbler] Block {gridIndex}: player exited, collapse in {fallDelaySeconds:0.00}s.");
        StartCoroutine(CrumbleSequence());
    }

    private IEnumerator CrumbleSequence()
    {
        if (fallDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(fallDelaySeconds);
        }

        PlaySfx(collapseSfx);

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

        yield return WaitForFirstImpactOrTimeout();
        DestroyBlock();
    }

    private void LoadSfxClipsIfNeeded()
    {
#if UNITY_EDITOR
        if (entrySfx == null)
        {
            entrySfx = AssetDatabase.LoadAssetAtPath<AudioClip>(entrySfxAssetPath);
        }
        if (centerSfx == null)
        {
            centerSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(centerSfxAssetPath);
        }
        if (collapseSfx == null)
        {
            collapseSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(collapseSfxAssetPath);
        }
        if (impactSfx == null)
        {
            impactSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(impactSfxAssetPath);
        }
#endif
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        PlayOneShotAtPosition(clip, transform.position);
    }

    private static void PlayOneShotAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        var root = GetOrCreateSfxRoot();
        var go = new GameObject("CrumbleSFX");
        if (root != null)
        {
            go.transform.SetParent(root, false);
        }
        go.transform.position = position;

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.clip = clip;
        source.Play();

        Object.Destroy(go, clip.length + 0.1f);
    }

    private static Transform GetOrCreateSfxRoot()
    {
        if (sfxRoot != null) return sfxRoot;

        var root = GameObject.Find("__CrumbleSfxRuntime");
        if (root == null)
        {
            root = new GameObject("__CrumbleSfxRuntime");
        }
        sfxRoot = root.transform;
        return sfxRoot;
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

        // Store the target block and grid-aligned position for dust/effects.
        // This is deterministic — doesn't depend on where bricks physically bounce.
        impactTargetTransform = hitBlock != null ? hitBlock.transform : hit.collider.transform;
        predictedImpactPosition = worldPos;
        hasPredictedImpactPosition = true;

        LogCrumble($"[Crumbler] Block {gridIndex}: predicted impact position {predictedImpactPosition}" +
                   $" (below block: {(hitBlock != null ? hitBlock.name : hit.collider.name)})");

        supportCubeSpawned = true;
        SpawnSupportCube(worldPos, layer);
    }

    private IEnumerator WaitForFirstImpactOrTimeout()
    {
        var remaining = Mathf.Max(0.5f, supportCubeMaxWaitSeconds);
        while (!hasFirstImpactFired && remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (!hasFirstImpactFired)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: timed out waiting for first impact after {supportCubeMaxWaitSeconds:F1}s.");
        }
    }

    private void PlayEntryParticles()
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
        LogCrumble($"[Crumbler] Block {gridIndex}: started entry particle effect '{particleSystemName}'.");
    }

    private void PlayCenterParticles()
    {
        if (fallingStoneFragmentsCenter == null)
        {
            CacheSceneReferences();
        }

        if (fallingStoneFragmentsCenter == null)
        {
            LogCrumble($"[Crumbler] Block {gridIndex}: center particle system '{centerParticleSystemName}' not found.");
            return;
        }

        var emission = fallingStoneFragmentsCenter.emission;
        emission.enabled = true;
        fallingStoneFragmentsCenter.Play(true);
        LogCrumble($"[Crumbler] Block {gridIndex}: started center particle effect '{centerParticleSystemName}'.");
    }

    private void StopParticlesAtStartup()
    {
        if (fallingStoneFragments != null)
        {
            fallingStoneFragments.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (fallingStoneFragmentsCenter != null)
        {
            fallingStoneFragmentsCenter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

            var centerParticleRoot = FindChildRecursive(crumbleRoot, centerParticleSystemName);
            if (centerParticleRoot != null)
            {
                fallingStoneFragmentsCenter = centerParticleRoot.GetComponent<ParticleSystem>();
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

        if (fallingStoneFragmentsCenter == null)
        {
            var directCenterParticle = FindChildRecursive(transform, centerParticleSystemName);
            if (directCenterParticle != null)
            {
                fallingStoneFragmentsCenter = directCenterParticle.GetComponent<ParticleSystem>();
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

    private void ReportDebrisImpact(FirstImpactData data)
    {
        if (hasFirstImpactFired) return;
        hasFirstImpactFired = true;

        LogCrumble($"[Crumbler] Block {gridIndex}: first impact at {data.position} on " +
                   $"{(data.hitCollider != null ? data.hitCollider.name : "None")} v={data.impactSpeed:F2}");

        OnFirstImpact?.Invoke(data);
    }

    private void OnFirstImpact_PlaySfx(FirstImpactData data) => PlaySfx(impactSfx);

    private void OnFirstImpact_SpawnDust(FirstImpactData data)
    {
        var dustPos = hasPredictedImpactPosition ? predictedImpactPosition : data.position;
        SpawnImpactParticles(dustPos);
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

        // Grow over lifetime: start small, expand to full size.
        var sizeOverLifetime = dust.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f));

        // Fade: 50% opacity at birth → fully transparent at death.
        var colorOverLifetime = dust.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var alphaGradient = new Gradient();
        alphaGradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(alphaGradient);

        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        dust.Play(true);
        Destroy(dust.gameObject, Mathf.Max(0.5f, impactParticleLifetime));
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
        private bool hasReported;
        private Rigidbody rb;

        public void Initialize(CrumblerBlock block, float minimumSpeed, float minimumUpNormal)
        {
            owner = block;
            minSpeed = Mathf.Max(0f, minimumSpeed);
            minUpNormal = Mathf.Clamp01(minimumUpNormal);
            hasReported = false;
            rb = GetComponent<Rigidbody>();
        }

        private bool ShouldIgnoreCollider(Collider col)
        {
            if (col == null) return true;
            if (col.GetComponentInParent<DebrisImpactRelay>() != null) return true;
            if (col.GetComponent<SupportCubeMarker>() != null) return true;
            // Only accept hits on the specific block below the crumbler.
            // If no target was identified (crumbler over void), accept any valid hit.
            if (owner.impactTargetTransform != null && !col.transform.IsChildOf(owner.impactTargetTransform)) return true;
            return false;
        }

        private void Report(Vector3 position, Collider hitCollider, Vector3 normal, float speed)
        {
            if (hasReported) return;
            hasReported = true;

            owner.ReportDebrisImpact(new FirstImpactData
            {
                position = position,
                time = Time.time,
                hitCollider = hitCollider,
                contactNormal = normal,
                impactSpeed = speed
            });
        }

        private void FixedUpdate()
        {
            if (owner == null || owner.hasFirstImpactFired || hasReported) return;
            if (rb == null || rb.linearVelocity.y > 0f) return;

            var origin = transform.position;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 0.15f, ~0, QueryTriggerInteraction.Ignore)) return;
            if (ShouldIgnoreCollider(hit.collider)) return;

            Report(hit.point, hit.collider, hit.normal, rb.linearVelocity.magnitude);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (owner == null || owner.hasFirstImpactFired || hasReported) return;
            if (collision == null || collision.contactCount <= 0) return;

            if (ShouldIgnoreCollider(collision.collider)) return;

            var contact = collision.GetContact(0);
            Report(contact.point, collision.collider, contact.normal, collision.relativeVelocity.magnitude);
        }
    }

    private sealed class SupportCubeMarker : MonoBehaviour { }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!hasPredictedImpactPosition) return;

        // Predicted impact position (where dust will spawn).
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(predictedImpactPosition, 0.25f);
        Gizmos.DrawLine(transform.position, predictedImpactPosition);

        // Target block below.
        if (impactTargetTransform != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(impactTargetTransform.position, Vector3.one);

            // Label via Handles.
            UnityEditor.Handles.color = Color.white;
            var label = $"Target: {impactTargetTransform.name}";
            var belowBlock = impactTargetTransform.GetComponent<BaseBlock>();
            if (belowBlock != null)
            {
                label += $"\nID: {belowBlock.gridIndex} Type: {belowBlock.blockType}";
            }
            UnityEditor.Handles.Label(impactTargetTransform.position + Vector3.up * 0.6f, label);
        }

        // Impact state.
        Gizmos.color = hasFirstImpactFired ? Color.green : Color.red;
        Gizmos.DrawWireSphere(predictedImpactPosition, 0.15f);
    }
#endif
}
