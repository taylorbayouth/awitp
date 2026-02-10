using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Teleporter block: teleports Lem to matching paired teleporter with same flavor.
/// Teleporters are interchangeable (no in/out distinction).
/// </summary>
public class TeleporterBlock : BaseBlock
{
    public enum TeleporterFlavor
    {
        A,
        B,
        C,
        D
    }

    [Header("Flavor")]
    [Tooltip("Teleporter channel (A-D). Used for pairing and visual palette.")]
    [SerializeField] private TeleporterFlavor teleporterFlavor = TeleporterFlavor.A;

    [Header("Teleport Settings")]
    [Tooltip("Seconds to ignore teleporter triggers after teleport")] 
    public float teleportCooldownSeconds = 2f;

    [Tooltip("Seconds the Lem pauses before teleporting")]
    public float preTeleportPauseSeconds = 0.4f;

    [Tooltip("Seconds the Lem pauses after teleporting")]
    public float postTeleportPauseSeconds = 0.15f;

    [Tooltip("How far the Lem sinks into the teleporter before and after warp")]
    public float sinkDistance = 0.45f;

    [Tooltip("Seconds to sink into the source teleporter")]
    public float sinkDurationSeconds = 0.25f;

    [Tooltip("Seconds to rise out of the destination teleporter")]
    public float riseDurationSeconds = 0.25f;

    [Tooltip("Extra vertical offset above the destination block")]
    public float destinationYOffset = 0.1f;

    [Header("Label Settings")]
    public bool showTeleportLabel = true;
    public Color labelColor = new Color(0f, 0f, 0f, 123f/255f);
    [Tooltip("Font used for the 3D teleporter label text.")]
    public Font teleporterLabelFont;
    public float labelScale = 0.28f;
    public float labelZOffset = 0.01f;

    private TextMesh labelMesh;
    private Transform labelTransform;

    private static readonly Dictionary<LemController, float> LemCooldownUntil = new Dictionary<LemController, float>();
    private readonly HashSet<LemController> blockedUntilTriggerExit = new HashSet<LemController>();

    // Flavor A keeps source material/shader colors.
    private static readonly Color PadPurple = new Color(95f / 255f, 0f, 154f / 255f, 1f);      // #5F009A
    private static readonly Color PadPurpleEmission = new Color(160f / 255f, 76f / 255f, 214f / 255f, 1f);
    private static readonly Color PadMaroon = new Color(0.68f, 0.22f, 0.28f, 1f);
    private static readonly Color PadMaroonEmission = new Color(0.92f, 0.28f, 0.33f, 1f);
    private static readonly Color PadTeal = new Color(0f, 81f / 255f, 73f / 255f, 1f);          // #005149
    private static readonly Color PadTealEmission = new Color(18f / 255f, 160f / 255f, 142f / 255f, 1f);

    private static readonly Color WaterPurpleDeep = new Color(34f / 255f, 0f, 63f / 255f, 1f);
    private static readonly Color WaterPurpleShallow = new Color(95f / 255f, 0f, 154f / 255f, 0.90f);
    private static readonly Color WaterPurpleSss = new Color(159f / 255f, 78f / 255f, 216f / 255f, 1f);
    private static readonly Color WaterMaroonDeep = new Color(0.30f, 0.02f, 0.05f, 1f);
    private static readonly Color WaterMaroonShallow = new Color(0.72f, 0.12f, 0.18f, 0.90f);
    private static readonly Color WaterMaroonSss = new Color(0.92f, 0.24f, 0.30f, 1f);
    private static readonly Color WaterTealDeep = new Color(1f / 255f, 43f / 255f, 39f / 255f, 1f);
    private static readonly Color WaterTealShallow = new Color(0f, 81f / 255f, 73f / 255f, 0.90f);
    private static readonly Color WaterTealSss = new Color(42f / 255f, 138f / 255f, 126f / 255f, 1f);

    private void OnDestroy()
    {
        // Clean up any stale entries referencing destroyed Lems
        CleanupStaleCooldownEntries();
    }

    /// <summary>
    /// Removes cooldown entries for Lems that have been destroyed.
    /// Called on destroy and periodically during cooldown checks.
    /// </summary>
    private static void CleanupStaleCooldownEntries()
    {
        if (LemCooldownUntil.Count == 0) return;

        List<LemController> staleKeys = null;
        foreach (var kvp in LemCooldownUntil)
        {
            if (kvp.Key == null)
            {
                if (staleKeys == null) staleKeys = new List<LemController>();
                staleKeys.Add(kvp.Key);
            }
        }

        if (staleKeys != null)
        {
            foreach (var key in staleKeys)
            {
                LemCooldownUntil.Remove(key);
            }
        }
    }

    protected override void Start()
    {
        base.Start();
        TryResolveFlavorFromInventory();
        SyncFlavor();
        ApplyFlavorVisuals();
        EnsureLabel();
        UpdateLabel();
    }

    private void LateUpdate()
    {
        if (labelTransform == null || !showTeleportLabel) return;
        FaceCamera();
    }

    protected override void OnPlayerReachCenter()
    {
        LemController lem = currentPlayer != null ? currentPlayer : ServiceRegistry.Get<LemController>();
        if (lem == null) return;

        if (IsBlockedUntilTriggerExit(lem))
        {
            return;
        }

        if (IsOnCooldown(lem))
        {
            return;
        }

        TeleporterBlock destination = FindMatchingTeleporter();
        if (destination == null)
        {
            return;
        }

        bool wasFacingRight = lem.GetFacingRight();
        StartCoroutine(TeleportSequence(lem, destination, wasFacingRight));
    }

    private TeleporterBlock FindMatchingTeleporter()
    {
        TeleporterBlock[] teleporters = UnityEngine.Object.FindObjectsByType<TeleporterBlock>(FindObjectsSortMode.None);
        if (teleporters == null || teleporters.Length == 0) return null;

        string key = GetTeleportKey();
        TeleporterBlock best = null;
        float bestDistance = float.MaxValue;

        foreach (TeleporterBlock teleporter in teleporters)
        {
            if (teleporter == null || teleporter == this) continue;
            if (teleporter.GetTeleportKey() != key) continue;

            float dist = Vector3.Distance(transform.position, teleporter.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                best = teleporter;
            }
        }

        return best;
    }

    private string GetTeleportKey()
    {
        if (TryParseFlavor(flavorId, out TeleporterFlavor fromFlavorId))
        {
            return FlavorToId(fromFlavorId);
        }
        if (!string.IsNullOrWhiteSpace(flavorId))
        {
            return flavorId.Trim();
        }

        string keyFromInventory = ExtractFlavorFromInventoryKey(inventoryKey);
        if (TryParseFlavor(keyFromInventory, out TeleporterFlavor fromInventory))
        {
            return FlavorToId(fromInventory);
        }
        if (!string.IsNullOrWhiteSpace(keyFromInventory))
        {
            return keyFromInventory.Trim();
        }

        return FlavorToId(teleporterFlavor);
    }

    private Vector3 GetTeleportLandingPosition(float yOffset)
    {
        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit
        Vector3 target = transform.position;
        target.y += cellSize * 0.5f + yOffset;
        return target;
    }

    private IEnumerator TeleportSequence(LemController lem, TeleporterBlock destination, bool facingRight)
    {
        if (lem == null || destination == null) yield break;

        yield return lem.StartCoroutine(lem.TweenHorizontalSpeedToZero(lem.GetInteractionStopTweenDuration()));

        lem.SetFrozen(true);
        SetCooldown(lem);

        float preDelay = Mathf.Max(0f, preTeleportPauseSeconds);
        if (preDelay > 0f)
        {
            yield return new WaitForSeconds(preDelay);
        }

        float sinkAmount = Mathf.Max(0f, sinkDistance);
        float sinkDuration = Mathf.Max(0f, sinkDurationSeconds);
        float riseDuration = Mathf.Max(0f, riseDurationSeconds);

        if (lem != null && sinkAmount > 0f)
        {
            float sourceSunkenY = lem.transform.position.y - sinkAmount;
            Tween sinkTween = lem.transform.DOMoveY(sourceSunkenY, sinkDuration).SetEase(Ease.InOutSine);
            yield return sinkTween.WaitForCompletion();
        }

        Vector3 targetPosition = destination.GetTeleportLandingPosition(destinationYOffset);
        targetPosition.y -= sinkAmount;
        destination.RequireTriggerExitBeforeReuse(lem);
        lem.transform.position = targetPosition;

        if (lem != null && sinkAmount > 0f)
        {
            float destinationRiseY = targetPosition.y + sinkAmount;
            Tween riseTween = lem.transform.DOMoveY(destinationRiseY, riseDuration).SetEase(Ease.InOutSine);
            yield return riseTween.WaitForCompletion();
        }

        float postDelay = Mathf.Max(0f, postTeleportPauseSeconds);
        if (postDelay > 0f)
        {
            yield return new WaitForSeconds(postDelay);
        }

        if (lem != null)
        {
            lem.SetFacingRight(facingRight);
            lem.SetFrozen(false);
        }
    }

    private bool IsOnCooldown(LemController lem)
    {
        if (lem == null) return true;

        // Periodically clean up stale entries (every 10+ entries)
        if (LemCooldownUntil.Count > 10)
        {
            CleanupStaleCooldownEntries();
        }

        if (LemCooldownUntil.TryGetValue(lem, out float until))
        {
            return Time.time < until;
        }
        return false;
    }

    protected override void OnPlayerTriggerExit(LemController lem)
    {
        if (lem == null) return;
        blockedUntilTriggerExit.Remove(lem);
    }

    private void RequireTriggerExitBeforeReuse(LemController lem)
    {
        if (lem == null) return;
        blockedUntilTriggerExit.Add(lem);
    }

    private bool IsBlockedUntilTriggerExit(LemController lem)
    {
        if (blockedUntilTriggerExit.Count == 0) return false;

        if (blockedUntilTriggerExit.Remove(null) && blockedUntilTriggerExit.Count == 0)
        {
            return false;
        }

        return lem != null && blockedUntilTriggerExit.Contains(lem);
    }

    private void SetCooldown(LemController lem)
    {
        if (lem == null) return;
        float animationTime =
            Mathf.Max(0f, preTeleportPauseSeconds) +
            Mathf.Max(0f, postTeleportPauseSeconds) +
            Mathf.Max(0f, sinkDurationSeconds) +
            Mathf.Max(0f, riseDurationSeconds);
        float cooldown = Mathf.Max(teleportCooldownSeconds, animationTime);
        cooldown += Mathf.Max(0.05f, lem.GetInteractionStopTweenDuration());
        LemCooldownUntil[lem] = Time.time + cooldown;
    }

    #region Placement Validation Overrides

    public static bool TryParseFlavor(string rawFlavor, out TeleporterFlavor parsedFlavor)
    {
        parsedFlavor = TeleporterFlavor.A;
        if (string.IsNullOrWhiteSpace(rawFlavor)) return false;

        string normalized = rawFlavor.Trim().ToUpperInvariant();
        switch (normalized)
        {
            case "A":
                parsedFlavor = TeleporterFlavor.A;
                return true;
            case "B":
                parsedFlavor = TeleporterFlavor.B;
                return true;
            case "C":
                parsedFlavor = TeleporterFlavor.C;
                return true;
            case "D":
                parsedFlavor = TeleporterFlavor.D;
                return true;
            default:
                return false;
        }
    }

    public static TeleporterFlavor ParseFlavorOrDefault(string rawFlavor, TeleporterFlavor fallback = TeleporterFlavor.A)
    {
        return TryParseFlavor(rawFlavor, out TeleporterFlavor parsedFlavor) ? parsedFlavor : fallback;
    }

    public static string FlavorToId(TeleporterFlavor flavor)
    {
        return flavor.ToString();
    }

    /// <summary>
    /// Validates that this teleporter has exactly one matching pair.
    /// Teleporters must come in pairs to function.
    /// </summary>
    public override bool ValidateGroupPlacement(GridManager grid)
    {
        return HasValidPair();
    }

    /// <summary>
    /// Returns error message if teleporter doesn't have a valid pair.
    /// </summary>
    public override string GetPlacementErrorMessage(int targetIndex, GridManager grid)
    {
        if (!HasValidPair())
        {
            return $"Teleporter '{GetTeleportKey()}' needs exactly one matching pair to function";
        }
        return null;
    }

    /// <summary>
    /// Checks if this teleporter has exactly one other teleporter with the same key.
    /// </summary>
    public bool HasValidPair()
    {
        TeleporterBlock[] teleporters = UnityEngine.Object.FindObjectsByType<TeleporterBlock>(FindObjectsSortMode.None);
        if (teleporters == null) return false;

        string key = GetTeleportKey();
        int matchCount = 0;

        foreach (TeleporterBlock teleporter in teleporters)
        {
            if (teleporter == null || teleporter == this) continue;
            if (teleporter.GetTeleportKey() == key)
            {
                matchCount++;
            }
        }

        return matchCount == 1; // Exactly one pair
    }

    /// <summary>
    /// Gets the teleport key for this block. Made public for validation purposes.
    /// </summary>
    public string GetKey() => GetTeleportKey();

    #endregion

    // Destination cooldown handled by Lem-based cooldown.

    private void EnsureLabel()
    {
        if (!showTeleportLabel) return;

        if (labelTransform == null)
        {
            Transform existing = transform.Find("TeleportLabel");
            if (existing != null)
            {
                labelTransform = existing;
                labelMesh = labelTransform.GetComponent<TextMesh>();
            }
        }

        if (labelTransform == null)
        {
            GameObject labelObj = new GameObject("TeleportLabel");
            labelObj.transform.SetParent(transform, false);
            labelTransform = labelObj.transform;
        }

        if (labelMesh == null)
        {
            labelMesh = labelTransform.GetComponent<TextMesh>();
            if (labelMesh == null)
            {
                labelMesh = labelTransform.gameObject.AddComponent<TextMesh>();
            }
        }

        UpdateLabelTransform();
    }

    private void UpdateLabel()
    {
        if (!showTeleportLabel) return;
        EnsureLabel();

        labelMesh.text = GetTeleportKey();
        labelMesh.anchor = TextAnchor.MiddleCenter;
        labelMesh.alignment = TextAlignment.Center;
        labelMesh.fontStyle = FontStyle.Bold;
        labelMesh.fontSize = 200;
        labelMesh.characterSize = 0.17f;
        labelMesh.color = labelColor;
        if (teleporterLabelFont != null)
        {
            labelMesh.font = teleporterLabelFont;
        }

        Renderer labelRenderer = labelMesh.GetComponent<Renderer>();
        if (labelRenderer != null && labelMesh.font != null)
        {
            // TextMesh must use the active font's atlas material to render glyphs.
            labelRenderer.sharedMaterial = labelMesh.font.material;
        }
    }

    private void UpdateLabelTransform()
    {
        if (labelTransform == null) return;
        float halfDepth = 0.5f * transform.localScale.z;
        labelTransform.localPosition = new Vector3(0f, 0f, -halfDepth - labelZOffset);
        labelTransform.localScale = Vector3.one * labelScale;
    }

    private void TryResolveFlavorFromInventory()
    {
        if (!string.IsNullOrEmpty(flavorId)) return;

        BlockInventory inventory = ServiceRegistry.Get<BlockInventory>();
        if (inventory == null) return;

        IReadOnlyList<BlockInventoryEntry> entries = inventory.GetEntries();
        if (entries == null || entries.Count == 0) return;

        BlockInventoryEntry match = null;
        foreach (BlockInventoryEntry entry in entries)
        {
            if (entry == null || entry.blockType != BlockType.Teleporter) continue;
            if (string.IsNullOrEmpty(entry.flavorId)) continue;

            if (!string.IsNullOrEmpty(inventoryKey))
            {
                string key = inventory.GetInventoryKey(entry);
                if (key != inventoryKey && entry.entryId != inventoryKey)
                {
                    continue;
                }
            }

            if (match != null && match.flavorId != entry.flavorId)
            {
                return;
            }

            match = entry;
        }

        if (match == null)
        {
            int teleporterCount = 0;
            foreach (BlockInventoryEntry entry in entries)
            {
                if (entry == null || entry.blockType != BlockType.Teleporter) continue;
                if (string.IsNullOrEmpty(entry.flavorId)) continue;
                teleporterCount++;
                match = entry;
                if (teleporterCount > 1) break;
            }

            if (teleporterCount != 1)
            {
                match = null;
            }
        }

        if (match != null)
        {
            flavorId = match.flavorId;
            UpdateLabel();
        }
    }

    private static string ExtractFlavorFromInventoryKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        int underscore = key.LastIndexOf('_');
        if (underscore >= 0 && underscore < key.Length - 1)
        {
            return key.Substring(underscore + 1);
        }

        return key;
    }

    private void SyncFlavor()
    {
        if (TryParseFlavor(flavorId, out TeleporterFlavor parsedFlavor))
        {
            teleporterFlavor = parsedFlavor;
            flavorId = FlavorToId(teleporterFlavor);
            return;
        }

        if (TryParseFlavor(ExtractFlavorFromInventoryKey(inventoryKey), out parsedFlavor))
        {
            teleporterFlavor = parsedFlavor;
            flavorId = FlavorToId(teleporterFlavor);
            return;
        }

        if (string.IsNullOrWhiteSpace(flavorId))
        {
            flavorId = FlavorToId(teleporterFlavor);
        }
    }

    private void ApplyFlavorVisuals()
    {
        if (!gameObject.scene.IsValid()) return;

        bool useFlavorOverride = teleporterFlavor != TeleporterFlavor.A;
        Color padBaseColor = GetPadBaseColor(teleporterFlavor);
        Color padEmissionColor = GetPadEmissionColor(teleporterFlavor);

        WaterCubeController[] waterControllers = GetComponentsInChildren<WaterCubeController>(true);
        for (int i = 0; i < waterControllers.Length; i++)
        {
            WaterCubeController controller = waterControllers[i];
            if (controller == null) continue;

            if (!useFlavorOverride)
            {
                controller.ResetFlavorPalette();
                continue;
            }

            GetWaterPalette(teleporterFlavor, out Color deep, out Color shallow, out Color sss);
            controller.ApplyFlavorPalette(deep, shallow, sss);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer.GetComponent<TextMesh>() != null) continue;
            if (renderer.GetComponentInParent<WaterCubeController>() != null) continue;
            ApplyRendererPalette(renderer, useFlavorOverride, padBaseColor, padEmissionColor);
        }
    }

    private static Color GetPadBaseColor(TeleporterFlavor flavor)
    {
        return flavor switch
        {
            TeleporterFlavor.B => PadPurple,
            TeleporterFlavor.C => PadMaroon,
            TeleporterFlavor.D => PadTeal,
            _ => Color.white
        };
    }

    private static Color GetPadEmissionColor(TeleporterFlavor flavor)
    {
        return flavor switch
        {
            TeleporterFlavor.B => PadPurpleEmission,
            TeleporterFlavor.C => PadMaroonEmission,
            TeleporterFlavor.D => PadTealEmission,
            _ => Color.white
        };
    }

    private static void GetWaterPalette(TeleporterFlavor flavor, out Color deep, out Color shallow, out Color sss)
    {
        switch (flavor)
        {
            case TeleporterFlavor.B:
                deep = WaterPurpleDeep;
                shallow = WaterPurpleShallow;
                sss = WaterPurpleSss;
                return;
            case TeleporterFlavor.C:
                deep = WaterMaroonDeep;
                shallow = WaterMaroonShallow;
                sss = WaterMaroonSss;
                return;
            case TeleporterFlavor.D:
                deep = WaterTealDeep;
                shallow = WaterTealShallow;
                sss = WaterTealSss;
                return;
            default:
                deep = Color.clear;
                shallow = Color.clear;
                sss = Color.clear;
                return;
        }
    }

    private static void ApplyRendererPalette(Renderer renderer, bool useOverride, Color baseColor, Color emissionColor)
    {
        Material material = renderer.material;
        Material sourceMaterial = renderer.sharedMaterial != null ? renderer.sharedMaterial : material;
        if (material == null || sourceMaterial == null) return;

        ApplyPaletteProperty(material, sourceMaterial, "_Color", useOverride, baseColor);
        ApplyPaletteProperty(material, sourceMaterial, "_BaseColor", useOverride, baseColor);
        ApplyPaletteProperty(material, sourceMaterial, "_EmissionColor", useOverride, emissionColor);
    }

    private static void ApplyPaletteProperty(Material target, Material source, string propertyName, bool useOverride, Color overrideColor)
    {
        if (!target.HasProperty(propertyName) || !source.HasProperty(propertyName)) return;
        target.SetColor(propertyName, useOverride ? overrideColor : source.GetColor(propertyName));
    }

    private void FaceCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || labelTransform == null) return;

        Vector3 forward = labelTransform.position - cam.transform.position;
        if (forward.sqrMagnitude > 0.0001f)
        {
            labelTransform.rotation = Quaternion.LookRotation(forward);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!gameObject.scene.IsValid())
        {
            UpdateLabel();
            UpdateLabelTransform();
            return;
        }

        SyncFlavor();
        UpdateLabel();
        UpdateLabelTransform();
        ApplyFlavorVisuals();
    }
#endif
}
