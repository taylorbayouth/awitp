using UnityEngine;

/// <summary>
/// Represents a collectible key that can be carried by a Lem or locked into a LockBlock.
/// </summary>
public class KeyItem : MonoBehaviour
{
    public bool IsLocked { get; private set; }
    public bool IsHeldByLem => holderLem != null && !IsLocked;
    public bool IsLockedToBlock => holderLock != null && IsLocked;
    public int SourceKeyBlockIndex { get; private set; } = -1;

    private LemController holderLem;
    private Transform holderLock;
    private float sourceLocalYOffset = 0.6f;
    private float sourceScale = 0.2f;
    private float carryYOffset = 0.6f;

    public void ConfigureSource(int keyBlockIndex, float localYOffset, float scale, float carryOffset)
    {
        SourceKeyBlockIndex = keyBlockIndex;
        sourceLocalYOffset = localYOffset;
        sourceScale = scale;
        carryYOffset = carryOffset;
    }

    public float GetCarryYOffset(float cellSize)
    {
        return cellSize * carryYOffset;
    }

    public float GetWorldScale(float cellSize)
    {
        return cellSize * sourceScale;
    }

    public void AttachToKeyBlock(Transform blockTransform, float cellSize)
    {
        if (blockTransform == null) return;

        IsLocked = false;
        holderLem = null;
        holderLock = null;
        SetParentAndTransform(blockTransform, Vector3.up * sourceLocalYOffset, GetWorldScale(cellSize));
    }

    public void AttachToLem(LemController lem, float localYOffset, float worldScale)
    {
        if (lem == null || IsLocked) return;

        holderLem = lem;
        holderLock = null;
        Transform target = lem.transform;
        SetParentAndTransform(target, Vector3.up * localYOffset, worldScale);
    }

    public void AttachToLock(Transform lockTransform, float localYOffset, float worldScale)
    {
        if (lockTransform == null || IsLocked) return;

        holderLem = null;
        holderLock = lockTransform;
        IsLocked = true;
        SetParentAndTransform(lockTransform, Vector3.up * localYOffset, worldScale);
    }

    public static KeyItem FindHeldKey(LemController lem)
    {
        if (lem == null) return null;
        KeyItem[] items = lem.GetComponentsInChildren<KeyItem>();
        foreach (KeyItem item in items)
        {
            if (item != null && item.IsHeldByLem)
            {
                return item;
            }
        }
        return null;
    }

    public static bool IsKeyLockedOn(Transform lockTransform)
    {
        if (lockTransform == null) return false;
        KeyItem[] items = lockTransform.GetComponentsInChildren<KeyItem>();
        foreach (KeyItem item in items)
        {
            if (item != null && item.IsLockedToBlock)
            {
                return true;
            }
        }
        return false;
    }

    private void SetParentAndTransform(Transform parent, Vector3 localPosition, float worldScale)
    {
        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;

        float parentScale = parent != null ? parent.lossyScale.x : 1f;
        float localScale = parentScale > 0f ? worldScale / parentScale : worldScale;
        transform.localScale = Vector3.one * localScale;
    }
}
