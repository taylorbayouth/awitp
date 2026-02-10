using UnityEngine;

/// <summary>
/// Key block: behaves like a normal block, but holds a collectible key.
/// The key floats above the block and attaches to the Lem when reached.
/// </summary>
public class KeyBlock : BaseBlock
{
    [Header("Key Visuals")]
    [Tooltip("Color of the key sphere")]
    [SerializeField] private Color keyColor = Color.green;

    [Tooltip("Optional prefab to use for the key visual (falls back to a sphere if null)")]
    [SerializeField] private GameObject keyVisualPrefab;

    [Tooltip("Optional mesh for key visual when prefab instantiation fails")]
    [SerializeField] private Mesh keyVisualMesh;

    [Tooltip("Optional material for key visual when prefab instantiation fails")]
    [SerializeField] private Material keyVisualMaterial;

    [Tooltip("Key scale relative to grid cell size")]
    [SerializeField] private float keyScale = 0.2f;

    [Tooltip("Local Y offset relative to block scale")]
    [SerializeField] private float keyLocalYOffset = 0.6f;

    [Tooltip("YOffset in world units (relative to cell size) when key follows Lem")]
    [SerializeField] private float keyCarryYOffset = 0.6f;

    private Transform keyTransform;
    private KeyItem keyItem;
    private bool keyClaimed = false;
    private float keyVisualScaleMultiplier = 1f;

    protected override void Start()
    {
        base.Start();
        EnsureKeyVisual();
    }

    protected override void OnPlayerReachCenter()
    {
        if (keyClaimed) return;
        if (currentPlayer == null) return;
        AttachKeyToLem(currentPlayer);
    }

    private void EnsureKeyVisual()
    {
        if (keyTransform != null) return;

        Transform existingKey = transform.Find("Key");
        if (existingKey == null)
        {
            existingKey = transform.Find(GameConstants.ResourcePaths.GreenApplePrefab);
        }
        bool hasEmbeddedKeyVisual = existingKey != null;

        GameObject keyObj = null;
        if (existingKey != null)
        {
            keyObj = existingKey.gameObject;
        }
        else if (keyVisualPrefab != null)
        {
            Object instance = Instantiate((Object)keyVisualPrefab);
            keyObj = instance as GameObject;
            if (keyObj == null && instance is Component component)
            {
                keyObj = component.gameObject;
            }
            if (keyObj == null)
            {
                keyObj = CreateKeyFromMeshOrSphere();
            }
        }
        else
        {
            // Try loading GreenApple prefab from Resources
            GameObject applePrefab = Resources.Load<GameObject>(GameConstants.ResourcePaths.GreenApplePrefab);
            if (applePrefab != null)
            {
                keyObj = Instantiate(applePrefab);
            }
            else
            {
                keyObj = CreateKeyFromMeshOrSphere();
            }
        }

        if (keyObj == null) return;

        if (!hasEmbeddedKeyVisual)
        {
            keyObj.name = "Key";
        }
        foreach (Collider collider in keyObj.GetComponentsInChildren<Collider>())
        {
            Destroy(collider);
        }

        if (!hasEmbeddedKeyVisual)
        {
            keyObj.transform.SetParent(transform, false);
        }

        MeshFilter meshFilter = keyObj.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxSize > 0f)
            {
                keyVisualScaleMultiplier = 1f / maxSize;
            }
        }

        Renderer keyRenderer = keyObj.GetComponentInChildren<Renderer>();
        if (keyRenderer != null && keyVisualPrefab == null && keyVisualMaterial == null)
        {
            keyRenderer.material.color = keyColor;
        }

        keyTransform = keyObj.transform;
        keyItem = keyObj.GetComponent<KeyItem>();
        if (keyItem == null)
        {
            keyItem = keyObj.AddComponent<KeyItem>();
        }
        if (keyObj.GetComponent<KeyIdleMotion>() == null)
        {
            keyObj.AddComponent<KeyIdleMotion>();
        }
        keyItem.ConfigureSource(gridIndex, keyLocalYOffset, keyScale * keyVisualScaleMultiplier, keyCarryYOffset);

        if (hasEmbeddedKeyVisual)
        {
            keyItem.CaptureSourcePoseFromCurrentTransform();
        }
        else
        {
            ApplyKeyTransform(keyTransform, Vector3.up * keyLocalYOffset, GetKeyWorldScale());
            keyItem.CaptureSourcePoseFromCurrentTransform();
        }

        KeyIdleMotion idleMotion = keyObj.GetComponent<KeyIdleMotion>();
        if (idleMotion != null)
        {
            idleMotion.RefreshState();
        }
    }

    private GameObject CreateKeyFromMeshOrSphere()
    {
        if (keyVisualMesh != null)
        {
            GameObject keyObj = new GameObject("Key");
            MeshFilter filter = keyObj.AddComponent<MeshFilter>();
            filter.sharedMesh = keyVisualMesh;

            MeshRenderer renderer = keyObj.AddComponent<MeshRenderer>();
            if (keyVisualMaterial != null)
            {
                renderer.sharedMaterial = keyVisualMaterial;
            }
            else
            {
                renderer.material.color = keyColor;
            }

            return keyObj;
        }

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        return sphere;
    }

    private void AttachKeyToLem(LemController lem)
    {
        if (keyTransform == null || lem == null) return;

        keyClaimed = true;
        float cellSize = GameConstants.Grid.CellSize;
        float carryOffset = cellSize * keyCarryYOffset;
        if (keyItem != null)
        {
            keyItem.AttachToLem(lem, carryOffset, GetKeyWorldScale());
        }
    }

    public void ResetKeyToBlock()
    {
        if (keyItem == null)
        {
            EnsureKeyVisual();
        }

        float cellSize = GameConstants.Grid.CellSize;
        keyClaimed = false;
        keyItem?.AttachToKeyBlock(transform, cellSize);
    }

    public KeyItem GetKeyItem()
    {
        if (keyItem == null)
        {
            EnsureKeyVisual();
        }
        return keyItem;
    }

    private float GetKeyWorldScale()
    {
        float cellSize = GameConstants.Grid.CellSize;
        return cellSize * keyScale * keyVisualScaleMultiplier;
    }

    private static void ApplyKeyTransform(Transform key, Vector3 localPosition, float worldScale)
    {
        if (key == null) return;

        key.localPosition = localPosition;
        key.localRotation = Quaternion.identity;

        float parentScale = key.parent != null ? key.parent.lossyScale.x : 1f;
        float localScale = parentScale > 0f ? worldScale / parentScale : worldScale;
        key.localScale = Vector3.one * localScale;
    }
}
