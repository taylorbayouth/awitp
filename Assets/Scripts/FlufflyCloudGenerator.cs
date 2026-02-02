using UnityEngine;
using System.Collections.Generic;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Quaternion = UnityEngine.Quaternion;
using System;

public class FlufflyCloudGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct CloudBlob
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
    }

    [Header("Blob Count")]
    [SerializeField, Range(5, 100)] private int blobCount = 25;
    [SerializeField, Range(0, 30)] private int edgeBlobCount = 12;
    [SerializeField, Range(0, 16)] private int cornerBlobCount = 8;

    [Header("Size")]
    [SerializeField, Range(0.15f, 0.6f)] private float minSize = 0.25f;
    [SerializeField, Range(0.2f, 0.8f)] private float maxSize = 0.45f;
    [SerializeField, Range(0.1f, 0.5f)] private float edgeBlobSize = 0.2f;
    [SerializeField, Range(0.1f, 0.4f)] private float cornerBlobSize = 0.18f;

    [Header("Squash/Stretch")]
    [SerializeField, Range(0f, 1f)] private float squashProbability = 0.4f;
    [SerializeField, Range(0.5f, 1f)] private float minSquash = 0.6f;
    [SerializeField, Range(0.5f, 1f)] private float maxSquash = 0.9f;
    [SerializeField, Range(1f, 2f)] private float minStretch = 1f;
    [SerializeField, Range(1f, 1.8f)] private float maxStretch = 1.3f;

    [Header("Rotation")]
    [SerializeField, Range(0f, 1f)] private float rotationRandomness = 0.5f;

    [Header("Distribution")]
    [SerializeField, Range(0f, 0.5f)] private float positionSpread = 0.25f;
    [SerializeField, Range(0f, 0.3f)] private float minBlobSpacing = 0.05f;
    [SerializeField, Range(1, 10)] private int relaxationPasses = 3;

    [Header("Edge/Corner Coverage")]
    [SerializeField, Range(0f, 0.5f)] private float edgeInset = 0.15f;
    [SerializeField, Range(0f, 0.5f)] private float cornerInset = 0.1f;

    [Header("Appearance")]
    [SerializeField] private Material blobMaterial;
    [SerializeField] private Color blobColor = Color.white;
    [SerializeField] private Color shadowColor = new Color(0.7f, 0.7f, 0.8f, 1f);
    [SerializeField, Range(0f, 1f)] private float shadowBlend = 0.2f;

    [Header("Seed")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnStart = false;


    [Header("Softness")]
    [SerializeField, Range(0f, 1f)] private float coreOpacity = 0.95f;
    [SerializeField, Range(0.5f, 5f)] private float edgeFalloff = 2f;
    [SerializeField, Range(0f, 2f)] private float edgeBrightness = 0.3f;

    [Header("Animation")]
    [SerializeField, Range(0f, 2f)] private float waveSpeed = 0.5f;
    [SerializeField, Range(0.5f, 5f)] private float waveScale = 2f;
    [SerializeField, Range(0f, 0.1f)] private float waveAmount = 0.02f;
    [SerializeField, Range(0f, 2f)] private float pulseSpeed = 0.3f;
    [SerializeField, Range(0f, 0.05f)] private float pulseAmount = 0.01f;


    public List<CloudBlob> Blobs { get; private set; } = new List<CloudBlob>();

    private System.Random rng;
    private List<GameObject> spawnedSpheres = new List<GameObject>();
    private Material sharedMaterial;
    private bool needsRegeneration = false;

    void Start()
    {
        if (randomizeSeedOnStart) seed = UnityEngine.Random.Range(0, 999999);
        GenerateBlobs();
        SpawnSpheres();
    }

    void OnValidate()
    {
        minSize = Mathf.Min(minSize, maxSize);
        minSquash = Mathf.Min(minSquash, maxSquash);
        minStretch = Mathf.Min(minStretch, maxStretch);

        if (Application.isPlaying)
        {
            needsRegeneration = true;
        }
    }

    void Update()
    {
        if (needsRegeneration)
        {
            needsRegeneration = false;
            GenerateBlobs();
            SpawnSpheres();
        }

        UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        if (sharedMaterial == null) return;

        sharedMaterial.SetFloat("_CoreOpacity", coreOpacity);
        sharedMaterial.SetFloat("_EdgeFalloff", edgeFalloff);
        sharedMaterial.SetFloat("_EdgeBrightness", edgeBrightness);
        sharedMaterial.SetFloat("_WaveSpeed", waveSpeed);
        sharedMaterial.SetFloat("_WaveScale", waveScale);
        sharedMaterial.SetFloat("_WaveAmount", waveAmount);
        sharedMaterial.SetFloat("_PulseSpeed", pulseSpeed);
        sharedMaterial.SetFloat("_PulseAmount", pulseAmount);
        sharedMaterial.SetColor("_Color", blobColor);
        sharedMaterial.SetColor("_ShadowColor", shadowColor);
        sharedMaterial.SetFloat("_ShadowBlend", shadowBlend);
    }



    [ContextMenu("Regenerate")]
    public void GenerateBlobs()
    {
        rng = new System.Random(seed);
        Blobs.Clear();

        // Core blobs (center volume)
        for (int i = 0; i < blobCount; i++)
        {
            Blobs.Add(CreateCoreBlob());
        }

        // Edge blobs (on cube faces)
        for (int i = 0; i < edgeBlobCount; i++)
        {
            Blobs.Add(CreateEdgeBlob());
        }

        // Corner blobs
        for (int i = 0; i < cornerBlobCount; i++)
        {
            Blobs.Add(CreateCornerBlob());
        }

        // Push blobs apart if they're too close
        if (minBlobSpacing > 0)
        {
            RelaxBlobs();
        }
    }

    private CloudBlob CreateCoreBlob()
    {
        CloudBlob blob = new CloudBlob();

        float baseSize = Mathf.Lerp(minSize, maxSize, (float)rng.NextDouble());
        blob.scale = ApplySquash(Vector3.one * baseSize);

        blob.position = new Vector3(
            RandomRange(-positionSpread, positionSpread),
            RandomRange(-positionSpread, positionSpread),
            RandomRange(-positionSpread, positionSpread)
        );

        blob.rotation = RandomRotation();
        return blob;
    }

    private CloudBlob CreateEdgeBlob()
    {
        CloudBlob blob = new CloudBlob();

        float size = edgeBlobSize * RandomRange(0.8f, 1.2f);
        blob.scale = ApplySquash(Vector3.one * size);

        // Pick a random face and position on it
        float facePos = 0.5f - edgeInset;
        int face = rng.Next(6);
        float u = RandomRange(-0.35f, 0.35f);
        float v = RandomRange(-0.35f, 0.35f);

        blob.position = face switch
        {
            0 => new Vector3(facePos, u, v),
            1 => new Vector3(-facePos, u, v),
            2 => new Vector3(u, facePos, v),
            3 => new Vector3(u, -facePos, v),
            4 => new Vector3(u, v, facePos),
            _ => new Vector3(u, v, -facePos)
        };

        blob.rotation = RandomRotation();
        return blob;
    }

    private CloudBlob CreateCornerBlob()
    {
        CloudBlob blob = new CloudBlob();

        float size = cornerBlobSize * RandomRange(0.8f, 1.2f);
        blob.scale = ApplySquash(Vector3.one * size);

        // Pick a corner
        float cornerPos = 0.5f - cornerInset;
        blob.position = new Vector3(
            cornerPos * (rng.Next(2) == 0 ? 1 : -1),
            cornerPos * (rng.Next(2) == 0 ? 1 : -1),
            cornerPos * (rng.Next(2) == 0 ? 1 : -1)
        );

        // Add some jitter
        blob.position += new Vector3(
            RandomRange(-0.05f, 0.05f),
            RandomRange(-0.05f, 0.05f),
            RandomRange(-0.05f, 0.05f)
        );

        blob.rotation = RandomRotation();
        return blob;
    }

    private Vector3 ApplySquash(Vector3 scale)
    {
        if ((float)rng.NextDouble() < squashProbability)
        {
            int axis = rng.Next(3);
            float squash = Mathf.Lerp(minSquash, maxSquash, (float)rng.NextDouble());
            scale[axis] *= squash;

            int stretchAxis = (axis + 1 + rng.Next(2)) % 3;
            float stretch = Mathf.Lerp(minStretch, maxStretch, (float)rng.NextDouble());
            scale[stretchAxis] *= stretch;
        }
        return scale;
    }

    private Quaternion RandomRotation()
    {
        return Quaternion.Euler(
            RandomRange(0f, 360f) * rotationRandomness,
            RandomRange(0f, 360f) * rotationRandomness,
            RandomRange(0f, 360f) * rotationRandomness
        );
    }

    private void RelaxBlobs()
    {
        // Simple relaxation: push overlapping blobs apart
        for (int pass = 0; pass < relaxationPasses; pass++)
        {
            for (int i = 0; i < Blobs.Count; i++)
            {
                CloudBlob blobA = Blobs[i];
                Vector3 push = Vector3.zero;

                for (int j = 0; j < Blobs.Count; j++)
                {
                    if (i == j) continue;

                    CloudBlob blobB = Blobs[j];
                    Vector3 diff = blobA.position - blobB.position;
                    float dist = diff.magnitude;
                    float minDist = minBlobSpacing + (blobA.scale.magnitude + blobB.scale.magnitude) * 0.3f;

                    if (dist < minDist && dist > 0.001f)
                    {
                        float pushStrength = (minDist - dist) * 0.5f;
                        push += diff.normalized * pushStrength;
                    }
                }

                blobA.position += push;

                // Keep inside bounds (but allow some overflow for bumpy edges)
                float limit = 0.45f;
                blobA.position = new Vector3(
                    Mathf.Clamp(blobA.position.x, -limit, limit),
                    Mathf.Clamp(blobA.position.y, -limit, limit),
                    Mathf.Clamp(blobA.position.z, -limit, limit)
                );

                Blobs[i] = blobA;
            }
        }
    }

    private void SpawnSpheres()
    {
        foreach (var sphere in spawnedSpheres)
        {
            if (sphere != null) Destroy(sphere);
        }
        spawnedSpheres.Clear();

        // Always create fresh material
        if (sharedMaterial != null && blobMaterial == null)
        {
            Destroy(sharedMaterial);
            sharedMaterial = null;
        }

        if (blobMaterial != null)
        {
            sharedMaterial = blobMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Custom/FlufflyCloud");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("FlufflyCloud shader not found or not supported, using Standard");
                shader = Shader.Find("Standard");
            }
            sharedMaterial = new Material(shader);
            sharedMaterial.SetColor("_Color", blobColor);
            sharedMaterial.SetColor("_ShadowColor", shadowColor);
            sharedMaterial.SetFloat("_ShadowBlend", shadowBlend);
        }

        foreach (var blob in Blobs)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = blob.position;
            sphere.transform.localRotation = blob.rotation;
            sphere.transform.localScale = blob.scale;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = sharedMaterial;
            Destroy(sphere.GetComponent<Collider>());
            spawnedSpheres.Add(sphere);
        }
    }

    private float RandomRange(float min, float max)
    {
        return Mathf.Lerp(min, max, (float)rng.NextDouble());
    }

    void OnDestroy()
    {
        foreach (var sphere in spawnedSpheres)
        {
            if (sphere != null) Destroy(sphere);
        }
        if (sharedMaterial != null && blobMaterial == null)
        {
            Destroy(sharedMaterial);
        }
    }
}