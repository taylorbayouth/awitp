using UnityEngine;
using DG.Tweening;

/// <summary>
/// Subtle idle animation for keys while they sit on a KeyBlock.
/// Uses DOTween with curve-driven easing for bob, spin, and scale pulse.
/// </summary>
[DisallowMultipleComponent]
public class KeyIdleMotion : MonoBehaviour
{
    [Header("Hover Bob")]
    [SerializeField] private float bobDistance = 0.09f;
    [SerializeField] private float bobDuration = 1.35f;
    [SerializeField] private AnimationCurve bobEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.7f),
        new Keyframe(1f, 1f, 1.7f, 0f)
    );

    [Header("Spin")]
    [SerializeField] private float spinDegreesPerSecond = 12f;
    [SerializeField] private AnimationCurve spinEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.3f),
        new Keyframe(1f, 1f, 1.3f, 0f)
    );

    [Header("Scale Pulse")]
    [SerializeField] private float scalePulseAmount = 0.08f;
    [SerializeField] private float scalePulseDuration = 1.5f;
    [SerializeField] private AnimationCurve scalePulseEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.2f),
        new Keyframe(1f, 1f, 1.2f, 0f)
    );

    private KeyItem keyItem;
    private Transform lastParent;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Tween bobTween;
    private Tween spinTween;
    private Tween scaleTween;
    private bool wasAnimating;

    private void Awake()
    {
        keyItem = GetComponent<KeyItem>();
    }

    private void OnEnable()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void LateUpdate()
    {
        bool shouldAnimate = ShouldAnimate();
        if (transform.parent != lastParent || shouldAnimate != wasAnimating)
        {
            RefreshState();
        }
    }

    public void RefreshState()
    {
        KillTweens();
        lastParent = transform.parent;

        bool shouldAnimate = ShouldAnimate();
        wasAnimating = shouldAnimate;
        if (!shouldAnimate)
        {
            return;
        }

        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;

        bobTween = transform
            .DOLocalMoveY(baseLocalPosition.y + bobDistance, Mathf.Max(0.01f, bobDuration))
            .SetEase(bobEase)
            .SetLoops(-1, LoopType.Yoyo);

        float spinDuration = 360f / Mathf.Max(0.1f, spinDegreesPerSecond);
        spinTween = transform
            .DOLocalRotate(new Vector3(0f, 360f, 0f), spinDuration, RotateMode.LocalAxisAdd)
            .SetEase(spinEase)
            .SetLoops(-1, LoopType.Restart);

        float pulseMultiplier = 1f + Mathf.Max(0f, scalePulseAmount);
        scaleTween = transform
            .DOScale(baseLocalScale * pulseMultiplier, Mathf.Max(0.01f, scalePulseDuration))
            .SetEase(scalePulseEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private bool ShouldAnimate()
    {
        if (keyItem == null)
        {
            keyItem = GetComponent<KeyItem>();
            if (keyItem == null) return false;
        }

        if (keyItem.IsHeldByLem || keyItem.IsLockedToBlock)
        {
            return false;
        }

        return GetComponentInParent<KeyBlock>() != null;
    }

    private void KillTweens()
    {
        if (bobTween != null)
        {
            bobTween.Kill();
            bobTween = null;
        }
        if (spinTween != null)
        {
            spinTween.Kill();
            spinTween = null;
        }
        if (scaleTween != null)
        {
            scaleTween.Kill();
            scaleTween = null;
        }
    }
}
