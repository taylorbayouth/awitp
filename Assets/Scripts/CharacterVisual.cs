using UnityEngine;

/// <summary>
/// Manages the character's visual representation: mesh, materials, and animation.
/// Decoupled from game logic (LemController) so the character's appearance can be
/// swapped independently of behavior.
///
/// HIERARCHY (created at runtime by Setup):
///   Lem (root - has LemController + CharacterVisual)
///     └── VisualPivot (rotation offset for model alignment)
///         └── Visual (scaled to fit collider height)
///             └── [Model Instance] (mesh, skeleton, Animator)
///
/// USAGE:
///   LemController calls Setup() during Awake to build the visual hierarchy.
///   LemController drives animation state via SetActiveClip(); this component
///   handles rendering, blending, and visual transforms.
///
/// TO SWAP CHARACTERS:
///   1. Place new model FBX in Resources/Characters/
///   2. Place animations in Resources/Animations/
///   3. Update GameConstants.ResourcePaths.CharacterMeshPrefab
///   4. Update GameConstants.AnimationClips paths
///   5. Adjust visual rotation offset if needed
/// </summary>
[DisallowMultipleComponent]
public class CharacterVisual : MonoBehaviour
{
    [Header("Visual Alignment")]
    [Tooltip("Local rotation offset (degrees) to align visual model with +X movement")]
    [SerializeField] private Vector3 visualRotationOffset = new Vector3(0f, 90f, 0f);

    [Header("Material")]
    [Tooltip("Render queue for character materials (higher = drawn later/on top)")]
    [SerializeField] private int renderQueueOverride = 2450;

    [Header("Animation")]
    [Tooltip("How quickly animation blends transition (higher = faster, 10 ≈ 0.3s)")]
    [SerializeField] private float animationBlendSpeed = 10f;

    private LemAnimationPlayables animPlayables;
    private Transform visualPivot;
    private Vector3 lastVisualRotationOffset;
    private Renderer[] cachedRenderers;
    private bool isTransparentModeEnabled = false;

    /// <summary>True if the Playables animation graph is active and valid.</summary>
    public bool IsAnimationValid => animPlayables != null && animPlayables.IsValid;

    /// <summary>
    /// Builds the visual hierarchy and sets up animations.
    /// Called explicitly by LemController during initialization — not in Awake,
    /// to avoid component ordering issues.
    /// </summary>
    /// <param name="targetHeight">Collider height to scale the visual to.</param>
    /// <param name="controller">LemController for animation event forwarding.</param>
    public void Setup(float targetHeight, LemController controller)
    {
        Cleanup();

        GameObject meshPrefab = Resources.Load<GameObject>(GameConstants.ResourcePaths.CharacterMeshPrefab);
        if (meshPrefab == null)
        {
            Debug.LogWarning("[CharacterVisual] No mesh prefab at Resources/" +
                             GameConstants.ResourcePaths.CharacterMeshPrefab +
                             ". Creating fallback capsule.");
            CreateFallbackBody(transform, targetHeight);
            return;
        }

        // Build hierarchy: Root → VisualPivot → Visual → model instance
        Transform pivot = new GameObject("VisualPivot").transform;
        pivot.SetParent(transform, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        visualPivot = pivot;

        Transform visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(pivot, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        GameObject modelInstance = Instantiate(meshPrefab, visualRoot);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        ApplyRenderQueueOverride(modelInstance);
        SetupAnimation(modelInstance, controller);

        // Scale visual AFTER Playables evaluation so bounds reflect the actual animated pose
        FitVisualToHeight(visualRoot, transform, targetHeight);
        ApplyVisualRotation();
    }

    /// <summary>
    /// Sets the active animation clip by name (e.g. "idle", "walk").
    /// Blend transitions are handled smoothly by LemAnimationPlayables.
    /// </summary>
    public void SetActiveClip(string clipName)
    {
        animPlayables?.SetActiveClip(clipName);
    }

    /// <summary>
    /// Smoothly blends animation weights toward their targets and applies them.
    /// Call once per frame from LemController.Update().
    /// </summary>
    public void EvaluateAnimation(float deltaTime)
    {
        if (animPlayables == null || !animPlayables.IsValid) return;
        animPlayables.BlendRate = animationBlendSpeed;
        animPlayables.Evaluate(deltaTime);
    }

    /// <summary>
    /// Sets the animation playback speed multiplier.
    /// Use this to sync animation with movement speed (e.g., faster walk = faster leg animation).
    /// </summary>
    /// <param name="speed">Speed multiplier (1 = normal, 2 = double speed, 0.5 = half speed)</param>
    public void SetAnimationSpeed(float speed)
    {
        if (animPlayables != null && animPlayables.IsValid)
        {
            animPlayables.Speed = speed;
        }
    }

    /// <summary>
    /// Checks if the visual rotation offset changed in the Inspector and reapplies it.
    /// </summary>
    public void UpdateVisualRotationIfNeeded()
    {
        if (visualRotationOffset != lastVisualRotationOffset)
        {
            ApplyVisualRotation();
        }
    }

    /// <summary>
    /// Reapplies the visual rotation offset. Call after facing direction changes.
    /// </summary>
    public void ApplyVisualRotation()
    {
        if (visualPivot == null)
        {
            visualPivot = transform.Find("VisualPivot");
        }
        if (visualPivot == null) return;

        visualPivot.localRotation = Quaternion.Euler(visualRotationOffset);
        lastVisualRotationOffset = visualRotationOffset;
    }

    /// <summary>
    /// Rescales the visual hierarchy to match a new target height.
    /// </summary>
    public void RefitToHeight(float targetHeight)
    {
        Transform runtimeVisual = transform.Find("VisualPivot/Visual");
        if (runtimeVisual != null)
        {
            FitVisualToHeight(runtimeVisual, transform, targetHeight);
        }
    }

    /// <summary>
    /// Enables transparent rendering mode on all character materials.
    /// Call this before starting a fade animation.
    /// </summary>
    public void EnableTransparency()
    {
        if (isTransparentModeEnabled) return;

        CacheRenderers();
        if (cachedRenderers.Length == 0) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null) continue;

            Material[] mats = renderer.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;
                SetMaterialToTransparent(mats[m]);
            }
            renderer.materials = mats;
        }

        isTransparentModeEnabled = true;
    }

    /// <summary>
    /// Disables transparent rendering mode and restores opaque rendering.
    /// Call this after finishing a fade animation.
    /// </summary>
    public void DisableTransparency()
    {
        if (!isTransparentModeEnabled) return;

        CacheRenderers();
        if (cachedRenderers.Length == 0) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null) continue;

            Material[] mats = renderer.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;
                SetMaterialToOpaque(mats[m]);
            }
            renderer.materials = mats;
        }

        isTransparentModeEnabled = false;
    }

    /// <summary>
    /// Sets the alpha/transparency of all character materials.
    /// Used for fade effects during teleportation.
    /// Note: Call EnableTransparency() first to switch materials to transparent mode.
    /// </summary>
    /// <param name="alpha">Alpha value (0 = invisible, 1 = fully visible)</param>
    public void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        CacheRenderers();
        if (cachedRenderers.Length == 0) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null) continue;

            Material[] mats = renderer.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;

                // Try common color properties (different shaders use different names)
                if (mats[m].HasProperty("_Color"))
                {
                    Color color = mats[m].GetColor("_Color");
                    color.a = alpha;
                    mats[m].SetColor("_Color", color);
                }

                if (mats[m].HasProperty("_BaseColor"))
                {
                    Color baseColor = mats[m].GetColor("_BaseColor");
                    baseColor.a = alpha;
                    mats[m].SetColor("_BaseColor", baseColor);
                }
            }
            renderer.materials = mats;
        }
    }

    private void CacheRenderers()
    {
        if (cachedRenderers != null) return;

        Transform visualRoot = transform.Find("VisualPivot/Visual");
        if (visualRoot != null)
        {
            cachedRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            cachedRenderers = System.Array.Empty<Renderer>();
        }
    }

    private void SetMaterialToTransparent(Material mat)
    {
        // Standard shader transparent mode
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3); // Transparent
        }

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    private void SetMaterialToOpaque(Material mat)
    {
        // Standard shader opaque mode
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 0); // Opaque
        }

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = renderQueueOverride;

        // Ensure alpha is at 1.0 when returning to opaque
        if (mat.HasProperty("_Color"))
        {
            Color color = mat.GetColor("_Color");
            color.a = 1f;
            mat.SetColor("_Color", color);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            Color baseColor = mat.GetColor("_BaseColor");
            baseColor.a = 1f;
            mat.SetColor("_BaseColor", baseColor);
        }
    }

    /// <summary>
    /// Destroys the visual hierarchy and disposes the Playables graph.
    /// </summary>
    public void Cleanup()
    {
        // Ensure materials are restored to opaque before cleanup
        if (isTransparentModeEnabled)
        {
            DisableTransparency();
        }

        animPlayables?.Dispose();
        animPlayables = null;
        cachedRenderers = null;
        DestroyChildByName("VisualPivot");
        DestroyChildByName("Visual");
    }

    void OnDestroy()
    {
        animPlayables?.Dispose();
    }

    #region Animation Setup

    private void SetupAnimation(GameObject modelInstance, LemController controller)
    {
        Animator modelAnimator = modelInstance.GetComponent<Animator>();
        if (modelAnimator == null)
        {
            modelAnimator = modelInstance.GetComponentInChildren<Animator>();
        }
        if (modelAnimator == null) return;

        modelAnimator.runtimeAnimatorController = null; // Playables drives animation
        modelAnimator.applyRootMotion = false;
        modelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Forward animation events from child Animator to LemController
        if (controller != null && modelAnimator.gameObject != gameObject)
        {
            var relay = modelAnimator.gameObject.GetComponent<AnimEventRelay>();
            if (relay == null)
            {
                relay = modelAnimator.gameObject.AddComponent<AnimEventRelay>();
            }
            relay.target = controller;
        }

        // Load animation clips from Resources
        AnimationClip idleClip = Resources.Load<AnimationClip>(GameConstants.AnimationClips.IdlePath);
        AnimationClip walkClip = Resources.Load<AnimationClip>(GameConstants.AnimationClips.WalkPath);
        AnimationClip walkCarryClip = Resources.Load<AnimationClip>(GameConstants.AnimationClips.WalkCarryPath);

        // Fall back to walk clip if no carry animation exists yet
        if (walkCarryClip == null) walkCarryClip = walkClip;

        animPlayables = new LemAnimationPlayables();
        animPlayables.BlendRate = animationBlendSpeed;
        animPlayables.Create(modelAnimator,
            (GameConstants.AnimationClips.Idle, idleClip),
            (GameConstants.AnimationClips.Walk, walkClip),
            (GameConstants.AnimationClips.WalkCarry, walkCarryClip)
        );

        // Force-evaluate so bone transforms reflect the actual animated pose
        // (needed for accurate bounds calculation in FitVisualToHeight)
        animPlayables.ForceGraphEvaluate();
    }

    #endregion

    #region Material

    /// <summary>
    /// Overrides the render queue on all model materials so the character
    /// draws in front of blocks (2000) and debris (1900).
    /// Uses renderer.materials to create per-instance copies.
    /// </summary>
    private void ApplyRenderQueueOverride(GameObject modelInstance)
    {
        if (modelInstance == null) return;
        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            // .materials creates per-instance copies (won't modify shared assets)
            Material[] mats = renderer.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null)
                {
                    mats[m].renderQueue = renderQueueOverride;
                }
            }
            renderer.materials = mats;
        }
    }

    #endregion

    #region Visual Fitting

    /// <summary>
    /// Uniformly scales the visual root so its rendered height matches targetHeight,
    /// then offsets the pivot so the model's feet sit at the owner's origin.
    /// </summary>
    private static void FitVisualToHeight(Transform visualRoot, Transform ownerRoot, float targetHeight)
    {
        if (visualRoot == null || ownerRoot == null) return;
        if (targetHeight <= 0f) return;

        if (!TryGetBoundsInParentSpace(visualRoot, ownerRoot, out Bounds bounds))
        {
            return;
        }

        float currentHeight = bounds.size.y;
        if (currentHeight <= 0.0001f) return;

        float scale = targetHeight / currentHeight;
        visualRoot.localScale = Vector3.one * scale;

        if (!TryGetBoundsInParentSpace(visualRoot, ownerRoot, out bounds))
        {
            return;
        }

        Transform pivot = visualRoot.parent;
        if (pivot != null)
        {
            pivot.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);
        }
    }

    private static bool TryGetBoundsInParentSpace(Transform root, Transform parent, out Bounds bounds)
    {
        bounds = new Bounds();
        if (root == null || parent == null) return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return false;

        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            Bounds rBounds = renderer.bounds;
            Vector3 min = rBounds.min;
            Vector3 max = rBounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 localCorner = parent.InverseTransformPoint(corners[i]);
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
    }

    private static void CreateFallbackBody(Transform parent, float height)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = Vector3.up * (height * 0.5f);
        body.transform.localScale = new Vector3(height * 0.5f, height * 0.5f, height * 0.5f);
        Object.Destroy(body.GetComponent<Collider>());
    }

    #endregion

    private void DestroyChildByName(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }
}
