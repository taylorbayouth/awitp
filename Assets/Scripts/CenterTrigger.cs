using UnityEngine;

/// <summary>
/// Trigger collider that marks the "center" (top plane) of a block for Lem detection.
/// </summary>
public class CenterTrigger : MonoBehaviour
{
    private BaseBlock owner;
    private SphereCollider sphere;
    private bool isActive = false;
    private static BuilderController _cachedBuilderController;

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
        if (other.CompareTag(GameConstants.Tags.Player))
        {
            LogCrumblerSphere("enter", other);
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
        if (other.CompareTag(GameConstants.Tags.Player))
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
        if (other.CompareTag(GameConstants.Tags.Player))
        {
            if (isActive)
            {
                isActive = false;
                owner.NotifyCenterTriggerExit();
            }
            LogCrumblerSphere("exit", other);
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
        if (_cachedBuilderController == null)
        {
            _cachedBuilderController = ServiceRegistry.Get<BuilderController>();
        }

        return _cachedBuilderController != null && _cachedBuilderController.currentMode == GameMode.Play;
    }

    private void LogCrumblerSphere(string phase, Collider other)
    {
        if (owner == null || owner.blockType != BlockType.Crumbler) return;
        SphereCollider sphereCollider = sphere ?? GetComponent<SphereCollider>();
        if (sphereCollider == null) return;

        Vector3 footPoint = GetFootPoint(other);
        float distance = Vector3.Distance(footPoint, transform.position);
        DebugLog.Crumbler($"[CrumblerCenter] Block {owner.gridIndex}: sphere {phase}. Foot distance {distance:0.###}, radius {sphereCollider.radius:0.###}.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (owner == null) owner = GetComponentInParent<BaseBlock>();
        UpdateShape();
    }
#endif
}
