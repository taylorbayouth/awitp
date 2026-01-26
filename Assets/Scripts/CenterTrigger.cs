using UnityEngine;

/// <summary>
/// Trigger collider that marks the "center" (top plane) of a block for Lem detection.
/// </summary>
public class CenterTrigger : MonoBehaviour
{
    private BaseBlock owner;
    private SphereCollider sphere;
    private bool isActive = false;
    private static EditorController _cachedEditorController;

    public void Initialize(BaseBlock baseBlock)
    {
        owner = baseBlock;
        sphere = GetComponent<SphereCollider>();
        if (sphere == null)
        {
            sphere = gameObject.AddComponent<SphereCollider>();
        }
        sphere.isTrigger = true;
        UpdateShape();
    }

    public void UpdateShape()
    {
        if (owner == null) owner = GetComponentInParent<BaseBlock>();
        if (sphere == null) sphere = GetComponent<SphereCollider>();
        if (owner == null || sphere == null) return;

        transform.localPosition = Vector3.up * (owner.centerTriggerYOffset * owner.transform.localScale.y + owner.centerTriggerWorldYOffset);
        sphere.radius = owner.centerTriggerRadius;
    }

    public void SetEnabled(bool enabled)
    {
        if (sphere == null) sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.enabled = enabled;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        if (!IsPlayModeActive())
        {
            isActive = false;
            return;
        }
        if (other.CompareTag("Player"))
        {
            UpdateCenterState(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner == null) return;
        if (!IsPlayModeActive())
        {
            isActive = false;
            return;
        }
        if (other.CompareTag("Player"))
        {
            UpdateCenterState(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner == null) return;
        if (!IsPlayModeActive())
        {
            isActive = false;
            return;
        }
        if (other.CompareTag("Player"))
        {
            if (isActive)
            {
                isActive = false;
                owner.NotifyCenterTriggerExit();
            }
        }
    }

    private void UpdateCenterState(Collider other)
    {
        Vector3 footPoint = GetFootPoint(other);
        float distance = Vector3.Distance(footPoint, transform.position);
        bool inside = distance <= sphere.radius;

        if (inside && !isActive)
        {
            isActive = true;
            owner.NotifyCenterTriggerEnter(other.GetComponent<LemController>());
        }
        else if (!inside && isActive)
        {
            isActive = false;
            owner.NotifyCenterTriggerExit();
        }
    }

    private static Vector3 GetFootPoint(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        return new Vector3(center.x, bounds.min.y, center.z);
    }

    private static bool IsPlayModeActive()
    {
        if (_cachedEditorController == null)
        {
            _cachedEditorController = UnityEngine.Object.FindObjectOfType<EditorController>();
        }

        return _cachedEditorController != null && _cachedEditorController.currentMode == GameMode.Play;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (owner == null) owner = GetComponentInParent<BaseBlock>();
        UpdateShape();
    }
#endif
}
