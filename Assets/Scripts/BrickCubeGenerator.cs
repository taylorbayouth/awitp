using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class BrickCubeGenerator : MonoBehaviour
{
    private enum PackingMode
    {
        RelaxedGrid,
        TiledLayers
    }

    [Header("Source")]
    [SerializeField] private GameObject brickPrefab;

    [Header("Packing")]
    [SerializeField] private PackingMode packingMode = PackingMode.RelaxedGrid;

    [Header("Layout")]
    [SerializeField] private Vector3Int counts = new Vector3Int(4, 4, 4);
    [Tooltip("Target cell size (local units) used for packing. Bricks are uniformly scaled to fit within this size.")]
    [SerializeField] private Vector3 brickSize = new Vector3(1f, 0.5f, 0.5f);
    [SerializeField] private float margin = 0.001f;
    [SerializeField] private bool centerOnParent = true;
    [SerializeField] private Vector3 globalOffset = Vector3.zero;

    [Header("Variation")]
    [SerializeField] private Vector3 rotationMax = new Vector3(2f, 2f, 2f);
    [Tooltip("Snap yaw to 0/90/180/270 so bricks stay horizontal but vary direction.")]
    [SerializeField] private bool useCardinalHorizontalRotation = true;
    [Tooltip("Optional extra yaw jitter on top of 90-degree facing.")]
    [SerializeField] private float yawJitterMax = 0f;
    [SerializeField] private Vector3 positionMax = new Vector3(0.001f, 0.001f, 0.001f);
    [SerializeField] private float outwardPushMax = 0.002f;
    [SerializeField] private Vector3 sizeJitter = new Vector3(0.02f, 0.02f, 0.02f);
    [Range(0f, 1f)]
    [Tooltip("Chance a brick is cut to half-length (squashed on its longest local axis).")]
    [SerializeField] private float halfBrickChance = 0f;
    [Range(0.1f, 1f)]
    [SerializeField] private float halfBrickScale = 0.5f;

    [Header("Post Compact")]
    [Tooltip("After applying variation, compact each row/column so bricks are pulled together (reduces gaps from rotation/offset).")]
    [SerializeField] private bool compactAfterGenerate = false;
    [SerializeField] private int compactIterations = 2;
    [Tooltip("Extra gap kept during compaction. Set to 0 for touch-tight packing.")]
    [SerializeField] private float compactMargin = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float compactStrength = 1f;
    [Tooltip("Snap outer bricks so the overall silhouette stays cube-like.")]
    [SerializeField] private bool enforceCubeSides = true;
    [Range(0f, 1f)]
    [SerializeField] private float sideSnapStrength = 1f;
    [SerializeField] private int sideSnapIterations = 2;

    [Header("Color Variation")]
    [SerializeField] private bool varyColor = true;
    [SerializeField] private Color colorMin = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color colorMax = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Vector2 metallicRange = new Vector2(0f, 0.2f);
    [SerializeField] private Vector2 roughnessRange = new Vector2(0.7f, 1f);

    [Header("Random")]
    [SerializeField] private bool useSeed = true;
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool autoRegenerate = false;

    private const string ContainerName = "Bricks";

    // Temporary 3D grid of brick instances used by post-generation compaction/side-snap.
    // Only allocated when RelaxedGrid mode needs post-processing.
    private GameObject[,,] instances;

    /// <summary>
    /// Generates the brick cube using the selected packing mode.
    /// Clears any previous bricks before generating.
    /// </summary>
    public void Generate()
    {
        if (brickPrefab == null)
        {
            Debug.LogWarning("[BrickCubeGenerator] Cannot generate: brickPrefab is not assigned.");
            return;
        }

        var container = GetOrCreateContainer();
        ClearContainer(container);

        var rng = useSeed ? new System.Random(seed) : new System.Random();

        instances = (packingMode == PackingMode.RelaxedGrid) && (compactAfterGenerate || enforceCubeSides)
            ? new GameObject[counts.x, counts.y, counts.z]
            : null;

        var prefabBoundsSize = GetPrefabBoundsSize();
        if (prefabBoundsSize.x <= 0f || prefabBoundsSize.y <= 0f || prefabBoundsSize.z <= 0f)
        {
            // Fallback to no scaling if we can't measure bounds.
            prefabBoundsSize = Vector3.one;
        }

        if (packingMode == PackingMode.TiledLayers)
        {
            GenerateTiledLayers(container, rng, prefabBoundsSize);
            return;
        }

        var maxScaleJitter = Mathf.Max(
            Mathf.Abs(sizeJitter.x),
            Mathf.Abs(sizeJitter.y),
            Mathf.Abs(sizeJitter.z)
        );

        // Uniform scaling only (avoid stretching). Ensure even the largest jitter still fits inside the cell.
        var fitScale = Mathf.Min(
            brickSize.x / prefabBoundsSize.x,
            brickSize.y / prefabBoundsSize.y,
            brickSize.z / prefabBoundsSize.z
        );
        fitScale = Mathf.Max(0.000001f, fitScale / (1f + maxScaleJitter));

        // Conservative shrink to help avoid overlaps when rotation is non-zero.
        var maxRotDeg = Mathf.Max(
            Mathf.Abs(rotationMax.x),
            Mathf.Abs(rotationMax.y),
            Mathf.Abs(rotationMax.z)
        );
        var rotationSafety = Mathf.Cos(maxRotDeg * Mathf.Deg2Rad);
        rotationSafety = Mathf.Clamp(rotationSafety, 0.1f, 1f);
        fitScale *= rotationSafety;

        // Use the brick's scaled bounds as the packing step so `margin=0` packs tightly
        // (even if brickSize doesn't match the prefab's aspect ratio).
        var maxInstanceScale = fitScale * (1f + maxScaleJitter);
        var scaledMaxBoundsSize = Vector3.Scale(prefabBoundsSize, Vector3.one * maxInstanceScale);
        var step = scaledMaxBoundsSize + Vector3.one * margin;

        var size = new Vector3(
            counts.x * step.x,
            counts.y * step.y,
            counts.z * step.z
        );

        var origin = centerOnParent ? -size * 0.5f + step * 0.5f : Vector3.zero;
        var cubeMin = origin - step * 0.5f + globalOffset;
        var cubeMax = origin + new Vector3((counts.x - 1) * step.x, (counts.y - 1) * step.y, (counts.z - 1) * step.z) + step * 0.5f + globalOffset;
        for (var z = 0; z < counts.z; z++)
        {
            for (var y = 0; y < counts.y; y++)
            {
                for (var x = 0; x < counts.x; x++)
                {
                    var basePos = origin + new Vector3(x * step.x, y * step.y, z * step.z);
                    var posJitter = new Vector3(
                        RandomRange(rng, -positionMax.x, positionMax.x),
                        RandomRange(rng, -positionMax.y, positionMax.y),
                        RandomRange(rng, -positionMax.z, positionMax.z)
                    );

                    var outward = Vector3.zero;
                    if (outwardPushMax > 0f)
                    {
                        // Only push outward on the cube surface so we don't shove bricks into each other.
                        var dir = Vector3.zero;
                        if (x == 0) dir += Vector3.left;
                        if (x == counts.x - 1) dir += Vector3.right;
                        if (y == 0) dir += Vector3.down;
                        if (y == counts.y - 1) dir += Vector3.up;
                        if (z == 0) dir += Vector3.back;
                        if (z == counts.z - 1) dir += Vector3.forward;

                        if (dir.sqrMagnitude > 0.000001f)
                        {
                            dir.Normalize();
                            outward = dir * RandomRange(rng, 0f, outwardPushMax);
                        }
                    }

                    var finalPos = basePos + posJitter + outward + globalOffset;

                    var tiltX = RandomRange(rng, -rotationMax.x, rotationMax.x);
                    var tiltZ = RandomRange(rng, -rotationMax.z, rotationMax.z);
                    float yaw;
                    if (useCardinalHorizontalRotation)
                    {
                        yaw = 90f * rng.Next(0, 4) + RandomRange(rng, -yawJitterMax, yawJitterMax);
                    }
                    else
                    {
                        yaw = RandomRange(rng, -rotationMax.y, rotationMax.y);
                    }
                    var rot = Quaternion.Euler(tiltX, yaw, tiltZ);

                    var uniformScaleJitter = RandomRange(rng, -maxScaleJitter, maxScaleJitter);
                    var baseScale = fitScale * (1f + uniformScaleJitter);

                    var instance = InstantiateBrick(container);
                    instance.name = $"Brick_{x}_{y}_{z}";
                    instance.transform.localRotation = rot;

                    var localScale = Vector3.one * baseScale;
                    if (RandomRange(rng, 0f, 1f) < halfBrickChance)
                    {
                        var axis = GetLongestAxis(prefabBoundsSize);
                        if (axis == 0) localScale.x *= halfBrickScale;
                        else if (axis == 1) localScale.y *= halfBrickScale;
                        else localScale.z *= halfBrickScale;
                    }
                    instance.transform.localScale = localScale;
                    instance.transform.localPosition = finalPos;

                    // Align the render bounds center to our target position (prefab pivots are often not centered).
                    var currentBounds = CalculateLocalAabb(instance, container);
                    var centerDelta = finalPos - currentBounds.center;
                    if (centerDelta.sqrMagnitude > 0.000001f)
                    {
                        instance.transform.localPosition += centerDelta;
                    }

                    if (varyColor)
                    {
                        ApplyMaterialVariation(instance, rng);
                    }

                    if (instances != null) instances[x, y, z] = instance;
                }
            }
        }

        if (instances != null)
        {
            var doCompact = compactAfterGenerate;
            var doSides = enforceCubeSides;
            var compactIters = Mathf.Max(1, compactIterations);
            var sidesIters = Mathf.Clamp(sideSnapIterations, 1, 10);
            var cMargin = Mathf.Max(0f, compactMargin);
            var cStrength = Mathf.Clamp01(compactStrength);
            var sStrength = Mathf.Clamp01(sideSnapStrength);

            if (doCompact && doSides)
            {
                // Interleave constraints so we converge to a tight pack *and* cube-like silhouette.
                var solveIters = Mathf.Max(compactIters, sidesIters);
                for (var i = 0; i < solveIters; i++)
                {
                    Compact(instances, container, 1, cMargin, cStrength);
                    SnapSidesToCube(instances, container, cubeMin, cubeMax, sStrength, 1);
                }
            }
            else
            {
                if (doCompact)
                {
                    Compact(instances, container, compactIters, cMargin, cStrength);
                }

                if (doSides)
                {
                    SnapSidesToCube(instances, container, cubeMin, cubeMax, sStrength, sidesIters);
                }
            }
        }
    }

    private void GenerateTiledLayers(Transform container, System.Random rng, Vector3 prefabBoundsSize)
    {
        // Step 1: tile/pack as tightly as possible (no "mess up" yet).
        var fitScale = Mathf.Min(
            brickSize.x / prefabBoundsSize.x,
            brickSize.y / prefabBoundsSize.y,
            brickSize.z / prefabBoundsSize.z
        );
        fitScale = Mathf.Max(0.000001f, fitScale);

        // Packing uses a cube-ish cell based on the long axis, so 90-degree yaw doesn't break the volume.
        var scaledBounds = prefabBoundsSize * fitScale;
        var longXZ = Mathf.Max(scaledBounds.x, scaledBounds.z);
        var cellXZ = longXZ * 0.5f;

        var xCells = counts.x * 2;
        var zCells = counts.z;
        var yCells = counts.y;

        if (xCells <= 0 || yCells <= 0 || zCells <= 0)
        {
            return;
        }

        // Optional: if counts look like an NxNxN cube in half-brick units, make Y match the cube cell size.
        var cellY = scaledBounds.y;
        if (enforceCubeSides && yCells == xCells && zCells == xCells)
        {
            cellY = cellXZ;
        }

        var size = new Vector3(xCells * cellXZ, yCells * cellY, zCells * cellXZ);
        var origin = centerOnParent ? -size * 0.5f : Vector3.zero;
        var cubeMin = origin + globalOffset;
        var cubeMax = origin + size + globalOffset;

        var filled = new bool[xCells * zCells];
        var dominoes = new Domino[xCells * zCells / 2];
        var occupancy = new GameObject[xCells, yCells, zCells];
        var bricks = new List<GameObject>(xCells * zCells / 2 * yCells);

        for (var y = 0; y < yCells; y++)
        {
            Array.Clear(filled, 0, filled.Length);
            var dominoCount = 0;
            if (!TryTileLayer(xCells, zCells, filled, dominoes, ref dominoCount, rng))
            {
                // Fallback: tile everything along X.
                dominoCount = 0;
                for (var z = 0; z < zCells; z++)
                {
                    for (var x = 0; x < xCells; x += 2)
                    {
                        dominoes[dominoCount++] = new Domino(x, z, DominoAxis.X);
                    }
                }
            }

            for (var i = 0; i < dominoCount; i++)
            {
                var d = dominoes[i];
                var makeHalves = halfBrickChance > 0f && RandomRange(rng, 0f, 1f) < halfBrickChance;
                if (makeHalves)
                {
                    SpawnHalfFromDomino(container, rng, prefabBoundsSize, fitScale, origin, cellXZ, cellY, y, d, occupancy, bricks);
                }
                else
                {
                    SpawnFullFromDomino(container, rng, prefabBoundsSize, fitScale, origin, cellXZ, cellY, y, d, occupancy, bricks);
                }
            }
        }

        // Step 2: "mess it up" (noise) — now we apply the Variation settings.
        ApplyTiledNoise(container, rng, bricks, cubeMin, cubeMax, cellXZ, cellY);

        // Step 3: pack again — pull everything together and enforce cube bounds.
        if (compactAfterGenerate)
        {
            CompactOccupancyCube(occupancy, container, cubeMin, cubeMax, compactMargin, compactStrength, compactIterations);
        }
    }

    private readonly struct Domino
    {
        public readonly int x;
        public readonly int z;
        public readonly DominoAxis axis;

        public Domino(int x, int z, DominoAxis axis)
        {
            this.x = x;
            this.z = z;
            this.axis = axis;
        }
    }

    private enum DominoAxis
    {
        X,
        Z
    }

    private static bool TryTileLayer(
        int width,
        int depth,
        bool[] filled,
        Domino[] dominoes,
        ref int dominoCount,
        System.Random rng)
    {
        dominoCount = 0;
        return TileNext(width, depth, filled, dominoes, ref dominoCount, rng);
    }

    private static bool TileNext(
        int width,
        int depth,
        bool[] filled,
        Domino[] dominoes,
        ref int dominoCount,
        System.Random rng)
    {
        var first = -1;
        for (var i = 0; i < filled.Length; i++)
        {
            if (!filled[i])
            {
                first = i;
                break;
            }
        }

        if (first < 0)
        {
            return true;
        }

        var x = first % width;
        var z = first / width;

        Span<DominoAxis> options = stackalloc DominoAxis[2];
        var optCount = 0;
        if (x + 1 < width && !filled[first + 1])
        {
            options[optCount++] = DominoAxis.X;
        }
        if (z + 1 < depth && !filled[first + width])
        {
            options[optCount++] = DominoAxis.Z;
        }

        if (optCount == 0)
        {
            return false;
        }

        if (optCount == 2 && rng.Next(0, 2) == 1)
        {
            (options[0], options[1]) = (options[1], options[0]);
        }

        for (var i = 0; i < optCount; i++)
        {
            var axis = options[i];
            filled[first] = true;
            var other = axis == DominoAxis.X ? first + 1 : first + width;
            filled[other] = true;
            dominoes[dominoCount++] = new Domino(x, z, axis);

            if (TileNext(width, depth, filled, dominoes, ref dominoCount, rng))
            {
                return true;
            }

            dominoCount--;
            filled[first] = false;
            filled[other] = false;
        }

        return false;
    }

    private void SpawnFullFromDomino(
        Transform container,
        System.Random rng,
        Vector3 prefabBoundsSize,
        float fitScale,
        Vector3 origin,
        float cellXZ,
        float cellY,
        int y,
        Domino d,
        GameObject[,,] occupancy,
        List<GameObject> bricks)
    {
        var baseScale = fitScale;

        var yawBase = d.axis == DominoAxis.X ? 0f : 90f;
        yawBase += rng.Next(0, 2) * 180f; // flip for variety
        var yaw = yawBase + RandomRange(rng, -yawJitterMax, yawJitterMax);

        var rot = Quaternion.Euler(
            0f,
            yaw,
            0f
        );

        var centerX = d.axis == DominoAxis.X ? d.x + 1f : d.x + 0.5f;
        var centerZ = d.axis == DominoAxis.X ? d.z + 0.5f : d.z + 1f;
        var finalPos = origin + new Vector3(centerX * cellXZ, (y + 0.5f) * cellY, centerZ * cellXZ) + globalOffset;

        var instance = InstantiateBrick(container);
        instance.name = $"Brick_{d.x}_{y}_{d.z}";
        instance.transform.localRotation = rot;
        instance.transform.localScale = Vector3.one * baseScale;
        instance.transform.localPosition = finalPos;

        // Align the render bounds center to our target position (prefab pivots are often not centered).
        var currentBounds = CalculateLocalAabb(instance, container);
        var centerDelta = finalPos - currentBounds.center;
        if (centerDelta.sqrMagnitude > 0.000001f)
        {
            instance.transform.localPosition += centerDelta;
        }

        if (varyColor)
        {
            ApplyMaterialVariation(instance, rng);
        }

        bricks.Add(instance);
        if (occupancy != null)
        {
            if (d.axis == DominoAxis.X)
            {
                occupancy[d.x, y, d.z] = instance;
                occupancy[d.x + 1, y, d.z] = instance;
            }
            else
            {
                occupancy[d.x, y, d.z] = instance;
                occupancy[d.x, y, d.z + 1] = instance;
            }
        }
    }

    private void SpawnHalfFromDomino(
        Transform container,
        System.Random rng,
        Vector3 prefabBoundsSize,
        float fitScale,
        Vector3 origin,
        float cellXZ,
        float cellY,
        int y,
        Domino d,
        GameObject[,,] occupancy,
        List<GameObject> bricks)
    {
        // Two halves fill the domino area. This keeps the cube packed but introduces smaller "cube" bricks.
        SpawnHalfAtCell(container, rng, prefabBoundsSize, fitScale, origin, cellXZ, cellY, y, d.x, d.z, occupancy, bricks);
        if (d.axis == DominoAxis.X)
        {
            SpawnHalfAtCell(container, rng, prefabBoundsSize, fitScale, origin, cellXZ, cellY, y, d.x + 1, d.z, occupancy, bricks);
        }
        else
        {
            SpawnHalfAtCell(container, rng, prefabBoundsSize, fitScale, origin, cellXZ, cellY, y, d.x, d.z + 1, occupancy, bricks);
        }
    }

    private void SpawnHalfAtCell(
        Transform container,
        System.Random rng,
        Vector3 prefabBoundsSize,
        float fitScale,
        Vector3 origin,
        float cellXZ,
        float cellY,
        int y,
        int cellX,
        int cellZ,
        GameObject[,,] occupancy,
        List<GameObject> bricks)
    {
        var baseScale = fitScale;

        var yaw = 90f * rng.Next(0, 4) + RandomRange(rng, -yawJitterMax, yawJitterMax);
        var rot = Quaternion.Euler(0f, yaw, 0f);

        var finalPos = origin + new Vector3((cellX + 0.5f) * cellXZ, (y + 0.5f) * cellY, (cellZ + 0.5f) * cellXZ) + globalOffset;

        var instance = InstantiateBrick(container);
        instance.name = $"HalfBrick_{cellX}_{y}_{cellZ}";
        instance.transform.localRotation = rot;

        var localScale = Vector3.one * baseScale;
        var axis = GetLongestAxis(prefabBoundsSize);
        if (axis == 0) localScale.x *= halfBrickScale;
        else if (axis == 1) localScale.y *= halfBrickScale;
        else localScale.z *= halfBrickScale;
        instance.transform.localScale = localScale;
        instance.transform.localPosition = finalPos;

        // Align the render bounds center to our target position (prefab pivots are often not centered).
        var currentBounds = CalculateLocalAabb(instance, container);
        var centerDelta = finalPos - currentBounds.center;
        if (centerDelta.sqrMagnitude > 0.000001f)
        {
            instance.transform.localPosition += centerDelta;
        }

        if (varyColor)
        {
            ApplyMaterialVariation(instance, rng);
        }

        bricks.Add(instance);
        if (occupancy != null)
        {
            occupancy[cellX, y, cellZ] = instance;
        }
    }

    private void ApplyTiledNoise(
        Transform container,
        System.Random rng,
        List<GameObject> bricks,
        Vector3 cubeMin,
        Vector3 cubeMax,
        float cellXZ,
        float cellY)
    {
        if (bricks == null || bricks.Count == 0)
        {
            return;
        }

        var maxScaleJitter = Mathf.Max(
            Mathf.Abs(sizeJitter.x),
            Mathf.Abs(sizeJitter.y),
            Mathf.Abs(sizeJitter.z)
        );

        var tol = Mathf.Max(0.0001f, Mathf.Min(cellXZ, cellY) * 0.01f);
        var surfaceMask = new int[bricks.Count];

        // Capture which bricks are on the cube boundary *before* noise so we can do outward push sanely.
        for (var i = 0; i < bricks.Count; i++)
        {
            var go = bricks[i];
            if (go == null) continue;

            var aabb = CalculateLocalAabb(go, container);
            var mask = 0;
            if (aabb.min.x <= cubeMin.x + tol) mask |= 1 << 0;
            if (aabb.max.x >= cubeMax.x - tol) mask |= 1 << 1;
            if (aabb.min.y <= cubeMin.y + tol) mask |= 1 << 2;
            if (aabb.max.y >= cubeMax.y - tol) mask |= 1 << 3;
            if (aabb.min.z <= cubeMin.z + tol) mask |= 1 << 4;
            if (aabb.max.z >= cubeMax.z - tol) mask |= 1 << 5;
            surfaceMask[i] = mask;
        }

        for (var i = 0; i < bricks.Count; i++)
        {
            var go = bricks[i];
            if (go == null) continue;

            // Uniform scale noise (keeps proportions).
            if (maxScaleJitter > 0f)
            {
                var s = 1f + RandomRange(rng, -maxScaleJitter, maxScaleJitter);
                s = Mathf.Max(0.0001f, s);
                go.transform.localScale *= s;
            }

            // Rotation noise: keep horizontal; apply small tilt. Yaw stays cardinal from tiling (unless you disable it).
            var tiltX = RandomRange(rng, -rotationMax.x, rotationMax.x);
            var tiltZ = RandomRange(rng, -rotationMax.z, rotationMax.z);
            var yawExtra = 0f;
            if (!useCardinalHorizontalRotation)
            {
                yawExtra = RandomRange(rng, -rotationMax.y, rotationMax.y);
            }
            var rotJitter = Quaternion.Euler(tiltX, yawExtra, tiltZ);
            go.transform.localRotation = go.transform.localRotation * rotJitter;

            // Position noise.
            var pos = go.transform.localPosition;
            if (positionMax != Vector3.zero)
            {
                pos += new Vector3(
                    RandomRange(rng, -positionMax.x, positionMax.x),
                    RandomRange(rng, -positionMax.y, positionMax.y),
                    RandomRange(rng, -positionMax.z, positionMax.z)
                );
            }

            if (outwardPushMax > 0f)
            {
                var mask = surfaceMask[i];
                var dir = Vector3.zero;
                if ((mask & (1 << 0)) != 0) dir += Vector3.left;
                if ((mask & (1 << 1)) != 0) dir += Vector3.right;
                if ((mask & (1 << 2)) != 0) dir += Vector3.down;
                if ((mask & (1 << 3)) != 0) dir += Vector3.up;
                if ((mask & (1 << 4)) != 0) dir += Vector3.back;
                if ((mask & (1 << 5)) != 0) dir += Vector3.forward;
                if (dir.sqrMagnitude > 0.000001f)
                {
                    dir.Normalize();
                    pos += dir * RandomRange(rng, 0f, outwardPushMax);
                }
            }

            go.transform.localPosition = pos;
        }
    }

    private static void CompactOccupancyCube(
        GameObject[,,] occupancy,
        Transform container,
        Vector3 cubeMin,
        Vector3 cubeMax,
        float baseMargin,
        float strength,
        int iterations)
    {
        if (occupancy == null) return;

        strength = Mathf.Clamp01(strength);
        if (strength <= 0f) return;

        iterations = Mathf.Clamp(iterations, 1, 50);

        var xCells = occupancy.GetLength(0);
        var yCells = occupancy.GetLength(1);
        var zCells = occupancy.GetLength(2);

        var line = new List<GameObject>(Mathf.Max(xCells, Mathf.Max(yCells, zCells)));

        for (var it = 0; it < iterations; it++)
        {
            // X lines (for each y,z)
            for (var y = 0; y < yCells; y++)
            {
                for (var z = 0; z < zCells; z++)
                {
                    BuildLineX(occupancy, xCells, y, z, line);
                    PackLineToBounds(line, container, Axis.X, cubeMin.x, cubeMax.x, baseMargin, strength);
                }
            }

            // Z lines (for each y,x)
            for (var y = 0; y < yCells; y++)
            {
                for (var x = 0; x < xCells; x++)
                {
                    BuildLineZ(occupancy, zCells, x, y, line);
                    PackLineToBounds(line, container, Axis.Z, cubeMin.z, cubeMax.z, baseMargin, strength);
                }
            }

            // Y columns (for each x,z)
            for (var x = 0; x < xCells; x++)
            {
                for (var z = 0; z < zCells; z++)
                {
                    BuildLineY(occupancy, yCells, x, z, line);
                    PackLineToBounds(line, container, Axis.Y, cubeMin.y, cubeMax.y, baseMargin, strength);
                }
            }
        }
    }

    private static void BuildLineX(GameObject[,,] occupancy, int xCells, int y, int z, List<GameObject> line)
    {
        line.Clear();
        GameObject prev = null;
        for (var x = 0; x < xCells; x++)
        {
            var go = occupancy[x, y, z];
            if (go == null || go == prev) continue;
            line.Add(go);
            prev = go;
        }
    }

    private static void BuildLineZ(GameObject[,,] occupancy, int zCells, int x, int y, List<GameObject> line)
    {
        line.Clear();
        GameObject prev = null;
        for (var z = 0; z < zCells; z++)
        {
            var go = occupancy[x, y, z];
            if (go == null || go == prev) continue;
            line.Add(go);
            prev = go;
        }
    }

    private static void BuildLineY(GameObject[,,] occupancy, int yCells, int x, int z, List<GameObject> line)
    {
        line.Clear();
        GameObject prev = null;
        for (var y = 0; y < yCells; y++)
        {
            var go = occupancy[x, y, z];
            if (go == null || go == prev) continue;
            line.Add(go);
            prev = go;
        }
    }

    private static void PackLineToBounds(
        List<GameObject> bricks,
        Transform container,
        Axis axis,
        float minBound,
        float maxBound,
        float baseMargin,
        float strength)
    {
        var count = bricks?.Count ?? 0;
        if (count <= 0) return;
        if (count == 1)
        {
            // Just snap the single brick to the center of the bounds.
            var go = bricks[0];
            if (go == null) return;
            var aabb = CalculateLocalAabb(go, container);
            var pivot = go.transform.localPosition;
            var desiredBoundsCenter = (minBound + maxBound) * 0.5f;
            float offset;
            switch (axis)
            {
                case Axis.X:
                    offset = aabb.center.x - pivot.x;
                    pivot.x = Mathf.Lerp(pivot.x, desiredBoundsCenter - offset, strength);
                    break;
                case Axis.Y:
                    offset = aabb.center.y - pivot.y;
                    pivot.y = Mathf.Lerp(pivot.y, desiredBoundsCenter - offset, strength);
                    break;
                case Axis.Z:
                    offset = aabb.center.z - pivot.z;
                    pivot.z = Mathf.Lerp(pivot.z, desiredBoundsCenter - offset, strength);
                    break;
                default:
                    return;
            }
            go.transform.localPosition = pivot;
            return;
        }

        var half = new float[count];
        var offsetAxis = new float[count];

        var sumLength = 0f;
        for (var i = 0; i < count; i++)
        {
            var go = bricks[i];
            if (go == null) return;
            var aabb = CalculateLocalAabb(go, container);
            var pivot = go.transform.localPosition;
            float h;
            float off;
            switch (axis)
            {
                case Axis.X:
                    h = aabb.extents.x;
                    off = aabb.center.x - pivot.x;
                    break;
                case Axis.Y:
                    h = aabb.extents.y;
                    off = aabb.center.y - pivot.y;
                    break;
                case Axis.Z:
                    h = aabb.extents.z;
                    off = aabb.center.z - pivot.z;
                    break;
                default:
                    return;
            }

            h = Mathf.Max(0.000001f, h);
            half[i] = h;
            offsetAxis[i] = off;
            sumLength += h * 2f;
        }

        var width = maxBound - minBound;
        var gaps = count - 1;
        var leftover = width - sumLength - baseMargin * gaps;
        var actualMargin = baseMargin + leftover / gaps; // can go negative (overlap) to force a tight cube.

        var edge = minBound;
        for (var i = 0; i < count; i++)
        {
            var go = bricks[i];
            if (go == null) continue;

            var desiredBoundsCenter = edge + half[i];
            var desiredPivot = desiredBoundsCenter - offsetAxis[i];

            var p = go.transform.localPosition;
            switch (axis)
            {
                case Axis.X:
                    p.x = Mathf.Lerp(p.x, desiredPivot, strength);
                    break;
                case Axis.Y:
                    p.y = Mathf.Lerp(p.y, desiredPivot, strength);
                    break;
                case Axis.Z:
                    p.z = Mathf.Lerp(p.z, desiredPivot, strength);
                    break;
                default:
                    return;
            }
            go.transform.localPosition = p;

            edge = desiredBoundsCenter + half[i] + actualMargin;
        }
    }

    /// <summary>
    /// Destroys all generated bricks in the container.
    /// </summary>
    public void Clear()
    {
        var container = GetContainer();
        if (container != null)
        {
            ClearContainer(container);
        }
    }

    private Transform GetOrCreateContainer()
    {
        var existing = GetContainer();
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject(ContainerName);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private Transform GetContainer()
    {
        var child = transform.Find(ContainerName);
        return child;
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        for (var i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private GameObject InstantiateBrick(Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(brickPrefab, parent) as GameObject;
            if (instance != null)
            {
                UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Create Brick");
                return instance;
            }
        }
#endif
        var runtimeInstance = Instantiate(brickPrefab, parent);
        return runtimeInstance;
    }

    private void ApplyMaterialVariation(GameObject instance, System.Random rng)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        // Only vary brightness (avoid hue shifts). We keep alpha from colorMin/colorMax but default to 1.
        var minBrightness = Mathf.Min(colorMin.grayscale, colorMax.grayscale);
        var maxBrightness = Mathf.Max(colorMin.grayscale, colorMax.grayscale);
        var brightness = RandomRange(rng, minBrightness, maxBrightness);
        var alpha = RandomRange(rng, Mathf.Min(colorMin.a, colorMax.a), Mathf.Max(colorMin.a, colorMax.a));
        var color = new Color(brightness, brightness, brightness, alpha);

        var metallic = RandomRange(rng, metallicRange.x, metallicRange.y);
        var roughness = RandomRange(rng, roughnessRange.x, roughnessRange.y);

        var block = new MaterialPropertyBlock();
        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(block);

            if (renderer.sharedMaterial != null)
            {
                if (renderer.sharedMaterial.HasProperty("baseColorFactor"))
                {
                    block.SetColor("baseColorFactor", color);
                }
                if (renderer.sharedMaterial.HasProperty("_Color"))
                {
                    block.SetColor("_Color", color);
                }
                if (renderer.sharedMaterial.HasProperty("metallicFactor"))
                {
                    block.SetFloat("metallicFactor", metallic);
                }
                if (renderer.sharedMaterial.HasProperty("_Metallic"))
                {
                    block.SetFloat("_Metallic", metallic);
                }
                if (renderer.sharedMaterial.HasProperty("roughnessFactor"))
                {
                    block.SetFloat("roughnessFactor", roughness);
                }
                if (renderer.sharedMaterial.HasProperty("_Glossiness"))
                {
                    block.SetFloat("_Glossiness", 1f - roughness);
                }
            }

            renderer.SetPropertyBlock(block);
        }
    }

    private static float RandomRange(System.Random rng, float min, float max)
    {
        if (Math.Abs(max - min) < 0.000001f) return min;
        return (float)(min + (max - min) * rng.NextDouble());
    }

    private static int GetLongestAxis(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z) return 0;
        if (size.y >= size.z) return 1;
        return 2;
    }

    private static void Compact(GameObject[,,] bricks, Transform container, int iterations, float margin, float strength)
    {
        if (bricks == null) return;
        if (strength <= 0f) return;

        var nx = bricks.GetLength(0);
        var ny = bricks.GetLength(1);
        var nz = bricks.GetLength(2);

        for (var it = 0; it < iterations; it++)
        {
            // X: for each (y,z) line
            for (var y = 0; y < ny; y++)
            {
                for (var z = 0; z < nz; z++)
                {
                    CompactLine(bricks, container, Axis.X, y, z, margin, strength);
                }
            }

            // Y: for each (x,z) line
            for (var x = 0; x < nx; x++)
            {
                for (var z = 0; z < nz; z++)
                {
                    CompactLine(bricks, container, Axis.Y, x, z, margin, strength);
                }
            }

            // Z: for each (x,y) line
            for (var x = 0; x < nx; x++)
            {
                for (var y = 0; y < ny; y++)
                {
                    CompactLine(bricks, container, Axis.Z, x, y, margin, strength);
                }
            }
        }
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }

    private static void CompactLine(GameObject[,,] bricks, Transform container, Axis axis, int a, int b, float margin, float strength)
    {
        var nx = bricks.GetLength(0);
        var ny = bricks.GetLength(1);
        var nz = bricks.GetLength(2);

        int count;
        Func<int, GameObject> get;
        Func<int, float> getPivot;
        Action<int, float> setPivot;

        switch (axis)
        {
            case Axis.X:
                count = nx;
                get = i => bricks[i, a, b];
                getPivot = i => bricks[i, a, b].transform.localPosition.x;
                setPivot = (i, v) =>
                {
                    var t = bricks[i, a, b].transform;
                    var p = t.localPosition;
                    p.x = Mathf.Lerp(p.x, v, strength);
                    t.localPosition = p;
                };
                break;
            case Axis.Y:
                count = ny;
                get = i => bricks[a, i, b];
                getPivot = i => bricks[a, i, b].transform.localPosition.y;
                setPivot = (i, v) =>
                {
                    var t = bricks[a, i, b].transform;
                    var p = t.localPosition;
                    p.y = Mathf.Lerp(p.y, v, strength);
                    t.localPosition = p;
                };
                break;
            case Axis.Z:
                count = nz;
                get = i => bricks[a, b, i];
                getPivot = i => bricks[a, b, i].transform.localPosition.z;
                setPivot = (i, v) =>
                {
                    var t = bricks[a, b, i].transform;
                    var p = t.localPosition;
                    p.z = Mathf.Lerp(p.z, v, strength);
                    t.localPosition = p;
                };
                break;
            default:
                return;
        }

        if (count <= 1) return;

        // Gather current center + half sizes (in container local space) and pivot→bounds-center offsets.
        var half = new float[count];
        var centerOffset = new float[count];
        var minEdge = float.PositiveInfinity;
        var maxEdge = float.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            var go = get(i);
            if (go == null) return;

            var aabb = CalculateLocalAabb(go, container);
            float h;
            float boundsCenterAxis;
            var pivotAxis = getPivot(i);
            switch (axis)
            {
                case Axis.X:
                    h = aabb.extents.x;
                    boundsCenterAxis = aabb.center.x;
                    minEdge = Mathf.Min(minEdge, aabb.min.x);
                    maxEdge = Mathf.Max(maxEdge, aabb.max.x);
                    break;
                case Axis.Y:
                    h = aabb.extents.y;
                    boundsCenterAxis = aabb.center.y;
                    minEdge = Mathf.Min(minEdge, aabb.min.y);
                    maxEdge = Mathf.Max(maxEdge, aabb.max.y);
                    break;
                case Axis.Z:
                    h = aabb.extents.z;
                    boundsCenterAxis = aabb.center.z;
                    minEdge = Mathf.Min(minEdge, aabb.min.z);
                    maxEdge = Mathf.Max(maxEdge, aabb.max.z);
                    break;
                default:
                    return;
            }
            half[i] = Mathf.Max(0.000001f, h);
            centerOffset[i] = boundsCenterAxis - pivotAxis;
        }

        var currentCenter = (minEdge + maxEdge) * 0.5f;
        var total = 0f;
        for (var i = 0; i < count; i++)
        {
            total += half[i] * 2f;
        }
        total += margin * (count - 1);

        var leftEdge = currentCenter - total * 0.5f;
        for (var i = 0; i < count; i++)
        {
            // We compute desired bounds-center position, then convert to pivot position.
            var desiredBoundsCenter = leftEdge + half[i];
            var desiredPivot = desiredBoundsCenter - centerOffset[i];
            setPivot(i, desiredPivot);
            leftEdge = desiredBoundsCenter + half[i] + margin;
        }
    }

    private static Bounds CalculateLocalAabb(GameObject root, Transform space)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            var p = space.InverseTransformPoint(root.transform.position);
            return new Bounds(p, Vector3.one * 0.0001f);
        }

        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (var i = 0; i < renderers.Length; i++)
        {
            var b = renderers[i].bounds;
            var c = b.center;
            var e = b.extents;

            // 8 corners of world AABB -> then re-AABB in desired space.
            for (var ix = -1; ix <= 1; ix += 2)
            {
                for (var iy = -1; iy <= 1; iy += 2)
                {
                    for (var iz = -1; iz <= 1; iz += 2)
                    {
                        var world = c + Vector3.Scale(e, new Vector3(ix, iy, iz));
                        var local = space.InverseTransformPoint(world);
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }
        }

        var center = (min + max) * 0.5f;
        var size = max - min;
        return new Bounds(center, size);
    }

    private static void SnapSidesToCube(
        GameObject[,,] bricks,
        Transform container,
        Vector3 cubeMin,
        Vector3 cubeMax,
        float strength,
        int iterations)
    {
        if (bricks == null || strength <= 0f) return;

        var nx = bricks.GetLength(0);
        var ny = bricks.GetLength(1);
        var nz = bricks.GetLength(2);

        for (var it = 0; it < iterations; it++)
        {
            for (var z = 0; z < nz; z++)
            {
                for (var y = 0; y < ny; y++)
                {
                    for (var x = 0; x < nx; x++)
                    {
                        var go = bricks[x, y, z];
                        if (go == null) continue;

                        var aabb = CalculateLocalAabb(go, container);
                        var delta = Vector3.zero;

                        if (x == 0) delta.x += cubeMin.x - aabb.min.x;
                        if (x == nx - 1) delta.x += cubeMax.x - aabb.max.x;
                        if (y == 0) delta.y += cubeMin.y - aabb.min.y;
                        if (y == ny - 1) delta.y += cubeMax.y - aabb.max.y;
                        if (z == 0) delta.z += cubeMin.z - aabb.min.z;
                        if (z == nz - 1) delta.z += cubeMax.z - aabb.max.z;

                        if (delta.sqrMagnitude > 0f)
                        {
                            go.transform.localPosition += delta * strength;
                        }
                    }
                }
            }
        }
    }

    private Vector3 GetPrefabBoundsSize()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(brickPrefab);
            if (!string.IsNullOrEmpty(path))
            {
                var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root != null)
                    {
                        var bounds = CalculateWorldBounds(root);
                        return bounds.size;
                    }
                }
                finally
                {
                    if (root != null)
                    {
                        UnityEditor.PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
        }
#endif

        // Runtime/editor fallback: instantiate a temp brick, measure world bounds, then destroy it.
        var temp = Instantiate(brickPrefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        temp.transform.localScale = Vector3.one;

        var measured = CalculateWorldBounds(temp);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(temp);
        }
        else
        {
            Destroy(temp);
        }
#else
        Destroy(temp);
#endif

        return measured.size;
    }

    private static Bounds CalculateWorldBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void OnValidate()
    {
        counts.x = Mathf.Max(1, counts.x);
        counts.y = Mathf.Max(1, counts.y);
        counts.z = Mathf.Max(1, counts.z);

        brickSize.x = Mathf.Max(0.0001f, brickSize.x);
        brickSize.y = Mathf.Max(0.0001f, brickSize.y);
        brickSize.z = Mathf.Max(0.0001f, brickSize.z);

        margin = Mathf.Max(0f, margin);
        outwardPushMax = Mathf.Max(0f, outwardPushMax);
        yawJitterMax = Mathf.Max(0f, yawJitterMax);
        halfBrickScale = Mathf.Clamp(halfBrickScale, 0.1f, 1f);
        halfBrickChance = Mathf.Clamp01(halfBrickChance);
        compactIterations = Mathf.Clamp(compactIterations, 1, 10);
        compactMargin = Mathf.Max(0f, compactMargin);
        sideSnapStrength = Mathf.Clamp01(sideSnapStrength);
        sideSnapIterations = Mathf.Clamp(sideSnapIterations, 1, 10);

        if (autoRegenerate)
        {
            Generate();
        }
    }
}
