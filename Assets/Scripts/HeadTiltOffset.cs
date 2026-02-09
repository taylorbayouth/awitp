using UnityEngine;

[DisallowMultipleComponent]
public class HeadTiltOffset : MonoBehaviour
{
    [SerializeField] private string headBoneName = "mixamorig:Head";
    [SerializeField] private Vector3 localEulerOffset = new Vector3(-5f, 0f, 0f);

    private Transform headBone;
    private Quaternion baseLocalRotation;

    private void Awake()
    {
        CacheHeadBone();
    }

    private void LateUpdate()
    {
        if (headBone == null)
        {
            CacheHeadBone();
            if (headBone == null) return;
        }

        headBone.localRotation = baseLocalRotation * Quaternion.Euler(localEulerOffset);
    }

    private void CacheHeadBone()
    {
        Transform[] bones = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            if (bone != null && bone.name == headBoneName)
            {
                headBone = bone;
                baseLocalRotation = bone.localRotation;
                return;
            }
        }
    }
}
