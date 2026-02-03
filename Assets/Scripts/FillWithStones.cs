using UnityEngine;
using System.Collections.Generic;
using System;

using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;

public class FillWithStones : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("How many stones to generate.")]
    public int stoneCount = 20;

    [Tooltip("Target number of faces. (Default 20 uses an Icosahedron base).")]
    public int facetsPerStone = 20;

    [Tooltip("The size of the area to fill.")]
    public Vector3 spawnVolume = new Vector3(4, 5, 4);

    [Header("Stone Aesthetics")]
    public float minStoneSize = 0.5f;
    public float maxStoneSize = 1.0f;
    [Tooltip("How much random noise to apply to vertex positions.")]
    public float irregularity = 0.3f;
    public Color stoneColor = Color.gray;

    [Header("Controls")]
    [Tooltip("Check this box or press SPACE to drop the walls.")]
    public bool releaseStones = false;

    // Internal references
    private List<GameObject> _walls = new List<GameObject>();
    private Material _stoneMaterial;

    void Start()
    {
        // Create a simple grey material for the stones
        _stoneMaterial = new Material(Shader.Find("Standard"));
        _stoneMaterial.color = stoneColor;
        _stoneMaterial.SetFloat("_Glossiness", 0.0f); // Make it matte

        CreateContainer();
        SpawnStones();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            releaseStones = !releaseStones;
        }

        // Handle the physical toggling of walls
        if (_walls.Count > 0 && _walls[0].activeSelf == releaseStones)
        {
            foreach (var wall in _walls)
            {
                wall.SetActive(!releaseStones);
            }
        }
    }

    void SpawnStones()
    {
        for (int i = 0; i < stoneCount; i++)
        {
            GenerateStone(i);
        }
    }

    void GenerateStone(int index)
    {
        GameObject stone = new GameObject($"Stone_{index}");
        stone.transform.parent = this.transform;

        // Random Position inside bounds
        Vector3 randomPos = transform.position + new Vector3(
            Random.Range(-spawnVolume.x / 2, spawnVolume.x / 2),
            Random.Range(-spawnVolume.y / 2, spawnVolume.y / 2),
            Random.Range(-spawnVolume.z / 2, spawnVolume.z / 2)
        );
        stone.transform.position = randomPos;
        stone.transform.rotation = Random.rotation;

        // Add Components
        MeshFilter mf = stone.AddComponent<MeshFilter>();
        MeshRenderer mr = stone.AddComponent<MeshRenderer>();
        mr.material = _stoneMaterial;

        // Generate the Procedural Mesh
        mf.mesh = GenerateLowPolyMesh();

        // Add Physics
        MeshCollider mc = stone.AddComponent<MeshCollider>();
        mc.convex = true;
        mc.sharedMesh = mf.mesh;

        Rigidbody rb = stone.AddComponent<Rigidbody>();
        rb.mass = Random.Range(1f, 5f);
    }

    Mesh GenerateLowPolyMesh()
    {
        Mesh mesh = new Mesh();

        // Base Geometry: Icosahedron (12 vertices, 20 faces)
        // This creates exactly 20 facets, matching the default requirement.
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        List<Vector3> vertices = new List<Vector3>()
        {
            new Vector3(-1,  t,  0), new Vector3( 1,  t,  0), new Vector3(-1, -t,  0), new Vector3( 1, -t,  0),
            new Vector3( 0, -1,  t), new Vector3( 0,  1,  t), new Vector3( 0, -1, -t), new Vector3( 0,  1, -t),
            new Vector3( t,  0, -1), new Vector3( t,  0,  1), new Vector3(-t,  0, -1), new Vector3(-t,  0,  1)
        };

        List<int> triangles = new List<int>()
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        // 1. Jitter: Randomize vertices slightly to make unique stones
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * irregularity;
            vertices[i] += randomOffset;
        }

        // 2. Flat Shading: We must duplicate vertices for every triangle
        // so that normals are calculated per face, giving the "Low Poly" look.
        Vector3[] flatVertices = new Vector3[triangles.Count];
        int[] flatTriangles = new int[triangles.Count];

        float scale = Random.Range(minStoneSize, maxStoneSize);

        for (int i = 0; i < triangles.Count; i++)
        {
            flatVertices[i] = vertices[triangles[i]] * scale;
            flatTriangles[i] = i;
        }

        mesh.vertices = flatVertices;
        mesh.triangles = flatTriangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void CreateContainer()
    {
        Vector3 size = spawnVolume;
        float halfX = size.x / 2;
        float halfZ = size.z / 2;

        // Floor
        CreateWall(new Vector3(0, -size.y / 2 - 0.5f, 0), new Vector3(size.x, 1, size.z), Quaternion.identity);

        // Walls
        _walls.Add(CreateWall(new Vector3(halfX + 0.5f, 0, 0), new Vector3(1, size.y, size.z), Quaternion.identity)); // Right
        _walls.Add(CreateWall(new Vector3(-halfX - 0.5f, 0, 0), new Vector3(1, size.y, size.z), Quaternion.identity)); // Left
        _walls.Add(CreateWall(new Vector3(0, 0, halfZ + 0.5f), new Vector3(size.x, size.y, 1), Quaternion.identity)); // Front
        _walls.Add(CreateWall(new Vector3(0, 0, -halfZ - 0.5f), new Vector3(size.x, size.y, 1), Quaternion.identity)); // Back
    }

    GameObject CreateWall(Vector3 pos, Vector3 scale, Quaternion rot)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.parent = this.transform;
        wall.transform.localPosition = pos;
        wall.transform.localScale = scale;
        wall.transform.localRotation = rot;

        // Make walls semi-transparent red to indicate they are temporary
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(1, 0, 0, 0.3f);
        wallMat.SetFloat("_Mode", 3); // Transparent

        // Setup renderer for transparency
        wallMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wallMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        wallMat.SetInt("_ZWrite", 0);
        wallMat.DisableKeyword("_ALPHATEST_ON");
        wallMat.EnableKeyword("_ALPHABLEND_ON");
        wallMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        wallMat.renderQueue = 3000;

        wall.GetComponent<Renderer>().material = wallMat;

        return wall;
    }

    // Draw the spawn volume in editor for easy setup
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, spawnVolume);
    }
}