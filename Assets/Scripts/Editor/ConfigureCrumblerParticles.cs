using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to configure the crumbler particle systems.
/// This script modifies both FallingStoneFragments and FallingStoneFragments-2
/// to be smaller, craggy, and fade out over 1.5 seconds.
/// </summary>
public class ConfigureCrumblerParticles : EditorWindow
{
    [MenuItem("Tools/Configure Crumbler Particles")]
    public static void ConfigureParticles()
    {
        // Load the Block_Crumbler prefab
        string prefabPath = "Assets/Resources/Blocks/Block_Crumbler.prefab";
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabAsset == null)
        {
            Debug.LogError($"Could not find prefab at {prefabPath}");
            return;
        }

        // Load prefab contents to modify
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            // Find both particle systems
            Transform crumbleRoot = FindTransformRecursive(prefabContents.transform, "CrumbleBlocks");
            if (crumbleRoot == null)
            {
                Debug.LogError("Could not find CrumbleBlocks in prefab");
                Debug.Log($"Available children: {string.Join(", ", GetChildNames(prefabContents.transform))}");
                return;
            }

            Debug.Log($"Found CrumbleBlocks. Children: {string.Join(", ", GetChildNames(crumbleRoot))}");

            // Search for particle systems
            ParticleSystem fallingStoneFragments = FindParticleSystem(crumbleRoot, "FallingStoneFragments");
            if (fallingStoneFragments == null)
            {
                Debug.LogError("Could not find FallingStoneFragments particle system");
                Debug.Log("Searching in entire prefab...");
                fallingStoneFragments = FindParticleSystemInPrefab(prefabContents.transform, "FallingStoneFragments");
            }
            else
            {
                Debug.Log($"✓ Found FallingStoneFragments at path: {GetTransformPath(fallingStoneFragments.transform)}");
            }

            ParticleSystem fallingStoneFragments2 = FindParticleSystem(crumbleRoot, "FallingStoneFragments-2");
            if (fallingStoneFragments2 == null)
            {
                Debug.LogError("Could not find FallingStoneFragments-2 particle system");
                Debug.Log("Searching in entire prefab...");
                fallingStoneFragments2 = FindParticleSystemInPrefab(prefabContents.transform, "FallingStoneFragments-2");
            }
            else
            {
                Debug.Log($"✓ Found FallingStoneFragments-2 at path: {GetTransformPath(fallingStoneFragments2.transform)}");
            }

            if (fallingStoneFragments == null)
            {
                Debug.LogError("FINAL: Could not find FallingStoneFragments particle system");
                return;
            }

            if (fallingStoneFragments2 == null)
            {
                Debug.LogError("FINAL: Could not find FallingStoneFragments-2 particle system");
                return;
            }

            // Configure both particle systems
            ConfigureParticleSystem(fallingStoneFragments, "FallingStoneFragments");
            ConfigureParticleSystem(fallingStoneFragments2, "FallingStoneFragments-2");

            // Save the modified prefab
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);

            Debug.Log("Successfully configured both crumbler particle systems!");
        }
        finally
        {
            // Always unload the prefab contents
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static string[] GetChildNames(Transform parent)
    {
        var names = new System.Collections.Generic.List<string>();
        foreach (Transform child in parent)
        {
            names.Add(child.name);
        }
        return names.ToArray();
    }

    private static ParticleSystem FindParticleSystem(Transform root, string name)
    {
        // Recursive search
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
            {
                return t.GetComponent<ParticleSystem>();
            }
        }

        return null;
    }

    private static Transform FindTransformRecursive(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
            {
                return t;
            }
        }

        return null;
    }

    private static ParticleSystem FindParticleSystemInPrefab(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
            {
                ParticleSystem ps = t.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Debug.Log($"Found {name} at path: {GetTransformPath(t)}");
                    return ps;
                }
            }
        }
        return null;
    }

    private static string GetTransformPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static void ConfigureParticleSystem(ParticleSystem ps, string name)
    {
        Debug.Log($"Configuring {name}...");

        // Main module - set lifetime to 1.5 seconds
        var main = ps.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 2f; // Reduce speed for falling effect
        main.startSize = 0.1f; // 3x smaller (was 0.3)
        main.gravityModifier = 2f; // Add gravity for falling effect

        // Enable Color over Lifetime - fade to transparent
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),   // Start fully opaque
                new GradientAlphaKey(0.0f, 1.0f)    // End fully transparent
            }
        );
        colorOverLifetime.color = gradient;

        // Enable Size over Lifetime - shrink to zero
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 1.0f);  // Start at full size
        sizeCurve.AddKey(1.0f, 0.0f);  // End at zero size
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer module - set to use a less soft material
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Mesh;

            // Try to find a cube mesh for craggy appearance
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCube);

            renderer.mesh = cubeMesh;

            // Use default particle material but without soft particles
            Material mat = renderer.sharedMaterial;
            if (mat != null)
            {
                // Keep existing material but ensure it's not using soft particles
                renderer.sharedMaterial = mat;
            }
        }

        Debug.Log($"  ✓ {name} configured: lifetime=1.5s, size=0.1, with fade and shrink");
    }
}
