using UnityEngine;

/// <summary>
/// Creates a square border as a single unified mesh (picture frame style).
/// Uses double-sided geometry and ZWrite Off to prevent z-fighting.
/// Render order is controlled via sortingOrder parameter and render queue.
/// </summary>
public class BorderRenderer : MonoBehaviour
{
    public Color color = Color.white;
    public float lineWidth = 0.05f;
    public float size = 1f;
    public float height = 0.5f;
    public int sortingOrder = 0;

    private Material material;
    private GameObject borderMeshObject;

    /// <summary>
    /// Initializes the border with specified parameters.
    /// </summary>
    /// <param name="borderColor">Color of the border</param>
    /// <param name="borderSize">Size of the border square (cell size)</param>
    /// <param name="yHeight">Y position (height) for rendering layer</param>
    /// <param name="sorting">Sorting order (0=grid, 1=borders, 2=cursor)</param>
    public void Initialize(Color borderColor, float borderSize, float yHeight, int sorting)
    {
        color = borderColor;
        size = borderSize;
        height = yHeight;
        sortingOrder = sorting;
        CreateBorder();
    }

    private void CreateBorder()
    {
        // Clean up existing
        if (borderMeshObject != null)
        {
            Destroy(borderMeshObject);
        }

        // Create material using Sprites/Default which handles transparency and ordering well
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        material.renderQueue = 3000 + (sortingOrder * 100);

        // Create single mesh object for entire border
        borderMeshObject = new GameObject("BorderMesh");
        borderMeshObject.transform.parent = transform;
        borderMeshObject.transform.localPosition = Vector3.zero;
        borderMeshObject.transform.localRotation = Quaternion.identity;

        MeshFilter meshFilter = borderMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = borderMeshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshRenderer.sortingOrder = sortingOrder;

        // Create the border mesh as a hollow square (picture frame)
        meshFilter.mesh = CreateHollowSquareMesh();
    }

    /// <summary>
    /// Creates a hollow square mesh (picture frame) with double-sided geometry.
    /// The mesh consists of 4 strips forming the border between outer and inner squares.
    /// </summary>
    private Mesh CreateHollowSquareMesh()
    {
        Mesh mesh = new Mesh();

        float outer = size * 0.5f;
        float inner = outer - lineWidth;
        float y = height;

        // 16 vertices: 8 for top face, 8 for bottom face (double-sided)
        // Outer square: 0-3 (top), 8-11 (bottom)
        // Inner square: 4-7 (top), 12-15 (bottom)
        Vector3[] vertices = new Vector3[16]
        {
            // Top face - outer square (counter-clockwise from above)
            new Vector3(-outer, y, -outer), // 0 - bottom-left outer
            new Vector3(outer, y, -outer),  // 1 - bottom-right outer
            new Vector3(outer, y, outer),   // 2 - top-right outer
            new Vector3(-outer, y, outer),  // 3 - top-left outer
            // Top face - inner square
            new Vector3(-inner, y, -inner), // 4 - bottom-left inner
            new Vector3(inner, y, -inner),  // 5 - bottom-right inner
            new Vector3(inner, y, inner),   // 6 - top-right inner
            new Vector3(-inner, y, inner),  // 7 - top-left inner
            // Bottom face (for double-sided) - outer square
            new Vector3(-outer, y, -outer), // 8
            new Vector3(outer, y, -outer),  // 9
            new Vector3(outer, y, outer),   // 10
            new Vector3(-outer, y, outer),  // 11
            // Bottom face - inner square
            new Vector3(-inner, y, -inner), // 12
            new Vector3(inner, y, -inner),  // 13
            new Vector3(inner, y, inner),   // 14
            new Vector3(-inner, y, inner),  // 15
        };

        // Create triangles for the 4 border strips (top face and bottom face)
        // Each border strip is a quad between outer and inner edges
        int[] triangles = new int[]
        {
            // TOP FACE (viewed from above, counter-clockwise winding)
            // Bottom strip (outer 0,1 to inner 4,5)
            0, 4, 5,
            0, 5, 1,
            // Right strip (outer 1,2 to inner 5,6)
            1, 5, 6,
            1, 6, 2,
            // Top strip (outer 2,3 to inner 6,7)
            2, 6, 7,
            2, 7, 3,
            // Left strip (outer 3,0 to inner 7,4)
            3, 7, 4,
            3, 4, 0,

            // BOTTOM FACE (viewed from below, reversed winding for correct normals)
            // Bottom strip
            8, 13, 12,
            8, 9, 13,
            // Right strip
            9, 14, 13,
            9, 10, 14,
            // Top strip
            10, 15, 14,
            10, 11, 15,
            // Left strip
            11, 12, 15,
            11, 8, 12,
        };

        Vector2[] uvs = new Vector2[16];
        for (int i = 0; i < 16; i++)
        {
            uvs[i] = new Vector2(0.5f, 0.5f); // Simple UV, solid color
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        if (material != null)
        {
            material.color = newColor;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (borderMeshObject != null) Destroy(borderMeshObject);
    }
}
