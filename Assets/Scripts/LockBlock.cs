using UnityEngine;

/// <summary>
/// Lock block: accepts any key carried by a Lem and permanently anchors it.
/// </summary>
public class LockBlock : BaseBlock
{
    [Header("Lock Visuals")]
    [Tooltip("Statue scale relative to grid cell size (0.8 = 80% of cell, same as Lem)")]
    [SerializeField] private float statueScale = 0.8f;

    [Tooltip("Additional Y offset adjustment to align statue base with block surface")]
    [SerializeField] private float statueYOffset = 0f;

    [Tooltip("Local Y offset for the key when locked")]
    [SerializeField] private float keyLockYOffset = 0.75f;

    private Transform lockTransform;

    public bool HasKeyLocked()
    {
        return KeyItem.IsKeyLockedOn(transform);
    }

    /// <summary>
    /// Returns true if this lock has a key inserted.
    /// Alias for HasKeyLocked() for use with LevelManager win condition checks.
    /// </summary>
    public bool IsFilled()
    {
        return HasKeyLocked();
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
        if (lockTransform == null)
        {
            lockTransform = transform.Find("Statue");
        }
        if (lockTransform == null) return;

        float cellSize = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        float worldScale = cellSize * statueScale;

        // Position statue so base aligns with block surface (mesh pivot is at center, so offset up by half)
        lockTransform.localPosition = Vector3.up * (worldScale * 0.5f + statueYOffset);
        lockTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);

        float parentScale = transform.lossyScale.x;
        float localScale = parentScale > 0f ? worldScale / parentScale : worldScale;
        lockTransform.localScale = Vector3.one * localScale;
    }
}
