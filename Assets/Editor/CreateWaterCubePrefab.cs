using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to assemble the Water Cube prefab from existing assets.
/// Menu: Tools > Create Water Cube Prefab
/// </summary>
public static class CreateWaterCubePrefab
{
    [MenuItem("Tools/Create Water Cube Prefab")]
    public static void Create()
    {
        // Load the rounded water cube mesh from Resources
        Mesh mesh = LoadWaterCubeMesh();
        if (mesh == null)
        {
            Debug.LogError("[CreateWaterCubePrefab] Could not load roundedWaterCube mesh from Resources. " +
                "Make sure Assets/Resources/roundedWaterCube.upp exists.");
            return;
        }

        // Load the water material
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WaterCube.mat");
        if (material == null)
        {
            Debug.LogWarning("[CreateWaterCubePrefab] Mat_WaterCube.mat not found. Creating a default material.");
            Shader shader = Shader.Find("Custom/WaterCube");
            if (shader == null)
            {
                Debug.LogError("[CreateWaterCubePrefab] Custom/WaterCube shader not found. Import the shader first.");
                return;
            }
            material = new Material(shader);
            material.name = "Mat_WaterCube";

            string matDir = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(matDir))
                AssetDatabase.CreateFolder("Assets", "Materials");

            AssetDatabase.CreateAsset(material, matDir + "/Mat_WaterCube.mat");
        }

        // Build the prefab hierarchy
        GameObject root = new GameObject("Block_Water");

        // Visual child with mesh + renderer
        GameObject visual = new GameObject("WaterVisual");
        visual.transform.SetParent(root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;

        // Physics on root
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.size = Vector3.one;
        collider.center = Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Water controller on root (finds renderer in children)
        root.AddComponent<WaterCubeController>();

        // Save as prefab
        string prefabDir = "Assets/Resources/Blocks";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Blocks");
        }

        string prefabPath = prefabDir + "/Block_Water.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        // Clean up the scene object
        Object.DestroyImmediate(root);

        if (prefab != null)
        {
            Debug.Log("[CreateWaterCubePrefab] Created prefab at " + prefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogError("[CreateWaterCubePrefab] Failed to save prefab at " + prefabPath);
        }
    }

    private static Mesh LoadWaterCubeMesh()
    {
        // Try loading from Resources (Unity strips the extension for .upp assets)
        // UModeler Pro meshes are imported as sub-assets
        string[] guids = AssetDatabase.FindAssets("roundedWaterCube", new[] { "Assets/Resources" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".upp")) continue;

            // Load all sub-assets — the mesh is typically the first (or only) sub-asset
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in subAssets)
            {
                if (obj is Mesh mesh)
                    return mesh;
            }

            // Fallback: try loading main asset as mesh
            Mesh mainMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mainMesh != null)
                return mainMesh;
        }

        return null;
    }
}
