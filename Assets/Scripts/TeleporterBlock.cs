using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Teleporter block: teleports Lem to matching paired teleporter with same flavor.
/// Teleporters are interchangeable (no in/out distinction).
/// </summary>
public class TeleporterBlock : BaseBlock
{
    [Header("Teleport Settings")]
    [Tooltip("Seconds to ignore teleporter triggers after teleport")] 
    public float teleportCooldownSeconds = 2f;

    [Tooltip("Seconds the Lem pauses before teleporting")]
    public float preTeleportPauseSeconds = 2f;

    [Tooltip("Seconds the Lem pauses after teleporting")]
    public float postTeleportPauseSeconds = 2f;

    [Tooltip("Extra vertical offset above the destination block")]
    public float destinationYOffset = 0.1f;

    [Header("Label Settings")]
    public bool showTeleportLabel = true;
    public Color labelColor = new Color(0f, 0f, 0f, 123f/255f);
    public float labelScale = 0.28f;
    public float labelZOffset = 0.01f;

    private TextMesh labelMesh;
    private Transform labelTransform;

    private static readonly Dictionary<LemController, float> LemCooldownUntil = new Dictionary<LemController, float>();

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
        LemController lem = currentPlayer != null ? currentPlayer : UnityEngine.Object.FindAnyObjectByType<LemController>();
        if (lem == null) return;

        if (IsOnCooldown(lem))
        {
            return;
        }

        TeleporterBlock destination = FindMatchingTeleporter();
        if (destination == null)
        {
            Debug.LogWarning($"[TeleporterBlock] No matching teleporter found for flavor '{GetTeleportKey()}'", this);
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
        int matchCount = 0;

        foreach (TeleporterBlock teleporter in teleporters)
        {
            if (teleporter == null || teleporter == this) continue;
            if (teleporter.GetTeleportKey() != key) continue;

            matchCount++;
            float dist = Vector3.Distance(transform.position, teleporter.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                best = teleporter;
            }
        }

        if (matchCount > 1)
        {
            Debug.LogWarning($"[TeleporterBlock] Multiple matching teleporters found for flavor '{key}'. Using closest.", this);
        }

        return best;
    }

    private string GetTeleportKey()
    {
        if (!string.IsNullOrEmpty(flavorId)) return flavorId;

        if (!string.IsNullOrEmpty(inventoryKey))
        {
            int underscore = inventoryKey.LastIndexOf('_');
            if (underscore >= 0 && underscore < inventoryKey.Length - 1)
            {
                return inventoryKey.Substring(underscore + 1);
            }
            return inventoryKey;
        }

        return "Default";
    }

    private Vector3 GetTeleportLandingPosition(float yOffset)
    {
        float cellSize = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        Vector3 target = transform.position;
        target.y += cellSize * 0.5f + yOffset;
        return target;
    }

    private IEnumerator TeleportSequence(LemController lem, TeleporterBlock destination, bool facingRight)
    {
        if (lem == null || destination == null) yield break;

        Rigidbody lemRb = lem.GetComponent<Rigidbody>();
        if (lemRb != null)
        {
            lemRb.linearVelocity = Vector3.zero;
        }

        lem.SetFrozen(true);
        SetCooldown(lem);

        float preDelay = Mathf.Max(0f, preTeleportPauseSeconds);
        if (preDelay > 0f)
        {
            yield return new WaitForSeconds(preDelay);
        }

        Vector3 targetPosition = destination.GetTeleportLandingPosition(destinationYOffset);
        lem.transform.position = targetPosition;

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

        DebugLog.Info($"[TeleporterBlock] Teleported Lem to '{destination.name}' (flavor '{GetTeleportKey()}')", this);
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

    private void SetCooldown(LemController lem)
    {
        if (lem == null) return;
        float cooldown = Mathf.Max(teleportCooldownSeconds, preTeleportPauseSeconds + postTeleportPauseSeconds);
        LemCooldownUntil[lem] = Time.time + cooldown;
    }

    #region Placement Validation Overrides

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

        string key = GetTeleportKey();
        string text = key == "Default" ? "?" : key;
        labelMesh.text = text;
        labelMesh.anchor = TextAnchor.MiddleCenter;
        labelMesh.alignment = TextAlignment.Center;
        labelMesh.fontStyle = FontStyle.Bold;
        labelMesh.fontSize = 200;
        labelMesh.characterSize = 0.17f;
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

        BlockInventory inventory = UnityEngine.Object.FindAnyObjectByType<BlockInventory>();
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
        UpdateLabel();
        UpdateLabelTransform();
    }
#endif
}
