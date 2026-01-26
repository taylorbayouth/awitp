using UnityEngine;

/// <summary>
/// Lock block: accepts any key carried by a Lem and permanently anchors it.
/// </summary>
public class LockBlock : BaseBlock
{
    [Header("Lock Visuals")]
    [Tooltip("Color of the lock cube")]
    [SerializeField] private Color lockColor = new Color(0.9f, 0.75f, 0.2f);

    [Tooltip("Lock cube scale relative to grid cell size")]
    [SerializeField] private float lockScale = 0.2f;

    [Tooltip("Local Y offset for the lock cube")]
    [SerializeField] private float lockLocalYOffset = 0.55f;

    [Tooltip("Local Y offset for the key when locked")]
    [SerializeField] private float keyLockYOffset = 0.75f;

    private Transform lockTransform;

    public bool HasKeyLocked()
    {
        return KeyItem.IsKeyLockedOn(transform);
    }

    public void AttachKeyFromState(KeyItem key)
    {
        if (key == null || key.IsLocked) return;
        if (HasKeyLocked()) return;

        float cellSize = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        float keyWorldScale = cellSize * 0.2f;
        float keyLocalOffset = cellSize * keyLockYOffset;
        key.AttachToLock(transform, keyLocalOffset, keyWorldScale);
    }

    protected override void Start()
    {
        base.Start();
        EnsureLockVisual();
    }

    protected override void OnPlayerReachCenter()
    {
        if (currentPlayer == null) return;

        KeyItem key = KeyItem.FindHeldKey(currentPlayer);
        if (key == null || key.IsLocked) return;
        if (HasKeyLocked()) return;

        float cellSize = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        float keyWorldScale = cellSize * 0.2f;
        float keyLocalOffset = cellSize * keyLockYOffset;
        key.AttachToLock(transform, keyLocalOffset, keyWorldScale);
    }

    private void EnsureLockVisual()
    {
        if (lockTransform != null) return;

        GameObject lockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lockObj.name = "Lock";
        Destroy(lockObj.GetComponent<Collider>());
        lockObj.transform.SetParent(transform, false);

        Renderer renderer = lockObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = lockColor;
        }

        lockTransform = lockObj.transform;
        float cellSize = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        float worldScale = cellSize * lockScale;
        ApplyLockTransform(lockTransform, Vector3.up * lockLocalYOffset * cellSize, worldScale);
    }

    private static void ApplyLockTransform(Transform lockTr, Vector3 localPosition, float worldScale)
    {
        if (lockTr == null) return;
        lockTr.localPosition = localPosition;
        lockTr.localRotation = Quaternion.identity;

        float parentScale = lockTr.parent != null ? lockTr.parent.lossyScale.x : 1f;
        float localScale = parentScale > 0f ? worldScale / parentScale : worldScale;
        lockTr.localScale = Vector3.one * localScale;
    }
}
