using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// Playables-based animation blending for the Lem character.
/// Replaces the Animator Controller with programmatic blend weight control,
/// enabling smooth skeletal interpolation between animation clips at runtime.
///
/// ARCHITECTURE:
///   AnimationClipPlayable (idle)  ──┐
///   AnimationClipPlayable (walk)  ──┤── AnimationMixerPlayable ── AnimationPlayableOutput ── Animator
///   AnimationClipPlayable (carry) ──┘
///
/// Each clip has a current weight (0..1) that is smoothly interpolated toward
/// a target weight each frame. The mixer blends bone transforms based on these
/// weights, producing natural skeletal transitions between poses.
///
/// USAGE:
///   var playables = new LemAnimationPlayables();
///   playables.Create(animator, ("idle", idleClip), ("walk", walkClip));
///   playables.SetActiveClip("walk");   // set target: walk=1, all others=0
///   playables.Evaluate(Time.deltaTime); // smooth blend + apply
///   playables.Dispose();               // cleanup on destroy
///
/// TO ADD A NEW ANIMATION:
///   1. Drop the FBX into Assets/Resources/Animations/
///   2. Add a constant to GameConstants.AnimationClips
///   3. Load and pass the clip in LemController.SetupVisual()
///   4. Call SetActiveClip() with the new name when appropriate
/// </summary>
public class LemAnimationPlayables : System.IDisposable
{
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private ClipEntry[] entries;

    /// <summary>
    /// Controls how quickly blend weights transition (higher = faster).
    /// A value of 10 means ~95% blended in 0.3 seconds.
    /// </summary>
    public float BlendRate { get; set; } = 10f;

    public bool IsValid => graph.IsValid();

    private struct ClipEntry
    {
        public string name;
        public float currentWeight;
        public float targetWeight;
    }

    /// <summary>
    /// Creates the PlayableGraph with the given named clips bound to an Animator.
    /// The first valid clip starts at full weight; all others start at zero.
    /// </summary>
    /// <param name="animator">The Animator component to drive (must not have a controller assigned).</param>
    /// <param name="clipDefs">Named clip pairs: (identifier, AnimationClip). Null clips are skipped.</param>
    /// <returns>True if at least one valid clip was loaded and the graph was created.</returns>
    public bool Create(Animator animator, params (string name, AnimationClip clip)[] clipDefs)
    {
        if (animator == null || clipDefs == null || clipDefs.Length == 0) return false;

        // Filter out null clips
        var valid = new System.Collections.Generic.List<(string name, AnimationClip clip)>();
        for (int i = 0; i < clipDefs.Length; i++)
        {
            if (clipDefs[i].clip != null)
            {
                valid.Add(clipDefs[i]);
            }
            else
            {
                Debug.LogWarning($"[LemAnimationPlayables] Null clip for '{clipDefs[i].name}', skipping.");
            }
        }

        if (valid.Count == 0) return false;

        graph = PlayableGraph.Create("LemAnimation");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        mixer = AnimationMixerPlayable.Create(graph, valid.Count);
        entries = new ClipEntry[valid.Count];

        for (int i = 0; i < valid.Count; i++)
        {
            var clipPlayable = AnimationClipPlayable.Create(graph, valid[i].clip);
            graph.Connect(clipPlayable, 0, mixer, i);

            float initialWeight = (i == 0) ? 1f : 0f;
            entries[i] = new ClipEntry
            {
                name = valid[i].name,
                currentWeight = initialWeight,
                targetWeight = initialWeight
            };
            mixer.SetInputWeight(i, initialWeight);
        }

        var output = AnimationPlayableOutput.Create(graph, "LemOutput", animator);
        output.SetSourcePlayable(mixer);

        graph.Play();
        return true;
    }

    /// <summary>
    /// Sets one clip to target weight 1 and all others to 0.
    /// The actual transition is smoothed in Evaluate().
    /// </summary>
    public void SetActiveClip(string clipName)
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i].targetWeight = (entries[i].name == clipName) ? 1f : 0f;
        }
    }

    /// <summary>
    /// Sets the target weight for a specific named clip.
    /// Caller is responsible for keeping total weights balanced (sum ~= 1).
    /// Use this for custom multi-clip blends; use SetActiveClip() for simple state switches.
    /// </summary>
    public void SetTargetWeight(string clipName, float weight)
    {
        if (entries == null) return;
        weight = Mathf.Clamp01(weight);
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].name == clipName)
            {
                entries[i].targetWeight = weight;
                return;
            }
        }
    }

    /// <summary>
    /// Smoothly interpolates current weights toward targets and applies them to the mixer.
    /// Call once per frame from Update().
    /// Uses exponential decay for frame-rate-independent damping.
    /// </summary>
    public void Evaluate(float deltaTime)
    {
        if (!graph.IsValid() || entries == null) return;

        float lerpT = 1f - Mathf.Exp(-BlendRate * deltaTime);

        for (int i = 0; i < entries.Length; i++)
        {
            entries[i].currentWeight = Mathf.Lerp(entries[i].currentWeight, entries[i].targetWeight, lerpT);
            mixer.SetInputWeight(i, entries[i].currentWeight);
        }
    }

    /// <summary>
    /// Forces the PlayableGraph to evaluate immediately, updating bone transforms.
    /// Call after Create() to ensure the initial animation pose is applied
    /// before reading SkinnedMeshRenderer bounds.
    /// </summary>
    public void ForceGraphEvaluate()
    {
        if (graph.IsValid())
        {
            graph.Evaluate(0f);
        }
    }

    /// <summary>
    /// Destroys the PlayableGraph and releases resources.
    /// Must be called when the owning MonoBehaviour is destroyed.
    /// </summary>
    public void Dispose()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
        entries = null;
    }
}
