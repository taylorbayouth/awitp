using UnityEngine;
using System;

/// <summary>
/// Base class for all block types in the puzzle game.
/// Blocks exist on the XY plane (Z=0) forming a vertical wall that Lems walk on.
///
/// Key Responsibilities:
/// - Manages block identity and grid positioning
/// - Handles player (Lem) detection through collision/trigger events
/// - Provides visual highlighting for editor interactions
/// - Coordinates with CenterTrigger for precise Lem position detection
///
/// Design Pattern: Template Method - Subclasses override OnPlayerEnter/Exit/ReachCenter
/// </summary>
public class BaseBlock : MonoBehaviour
{
    #region Inspector Fields

    [Header("Block Identity")]
    [Tooltip("Globally unique identifier for this block instance")]
    public string uniqueID;

    [Tooltip("Defines behavior type (Default, Teleporter, Crumbler, Transporter)")]
    public BlockType blockType = BlockType.Default;

    [Tooltip("Linear index in GridManager's coordinate system")]
    public int gridIndex = -1;

    [Tooltip("Inventory key used to return blocks to inventory")]
    public string inventoryKey;

    [Tooltip("Optional flavor id for this block (e.g., A, L3,U1)")]
    public string flavorId;

    [Tooltip("If true, block cannot be removed in Editor mode and doesn't return to inventory")]
    public bool isPermanent = false;

    [Header("Detection Settings")]
    [Tooltip("Size of detection area for collision queries")]
    public float detectionSize = 1f;

    [Tooltip("Layer mask for physics detection - defaults to all layers")]
    public LayerMask detectionLayerMask;

    [Header("Highlight Settings")]
    [Tooltip("Color when block is highlighted in editor")]
    public Color highlightColor = Color.black;

    // Cached original color for restoring after highlight
    private Color originalColor;

    [Header("Visuals")]
    [Tooltip("Optional visual root for mesh rendering (created automatically if missing)")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Auto-normalize visual mesh to fit within one grid cell")]
    [SerializeField] private bool autoNormalizeVisualScale = true;

    // Cached renderer component for performance
    private Renderer blockRenderer;

    // Current highlight state
    private bool isHighlighted = false;


    [Header("Editor Visualization")]
    [Tooltip("Show trigger state labels (On/Center/Off) in Scene view during play")]
    public bool showTriggerLabelsInEditor = true;

    [Tooltip("Show wireframe gizmos for trigger zones in Scene view")]
    public bool showTriggerGizmos = true;

    [Tooltip("Size multiplier for the 'On' trigger box visualization")]
    public Vector3 onTriggerBoxSize = Vector3.one;

    [Tooltip("Y offset for 'On' trigger box relative to block center")]
    public float onTriggerYOffset = 1f;

    [Tooltip("Radius of the center detection sphere")]
    public float centerTriggerRadius = 0.08f;

    [Tooltip("Y offset for center trigger relative to block scale")]
    public float centerTriggerYOffset = 0.5f;

    [Tooltip("Additional world-space Y offset for center trigger")]
    public float centerTriggerWorldYOffset = 0f;

    /// <summary>
    /// Represents the current state of player interaction with this block.
    /// Used for editor visualization and debugging.
    /// </summary>
    private enum TriggerState
    {
        None,    // No player interaction
        On,      // Player is on the block
        Center,  // Player reached the center point
        Off      // Player just left the block
    }

    // Tracks most recent trigger state for editor labels
    // Assigned at runtime but only read in editor visualization code
#pragma warning disable 0414
    private TriggerState lastTriggerState = TriggerState.None;
#pragma warning restore 0414

    // Flag indicating if a player is currently on this block
    private bool isPlayerOnBlock = false;

    // Time until which the "Center" label should be displayed
    // Assigned at runtime but only read in editor visualization code
#pragma warning disable 0414
    private float centerLabelUntil = 0f;
#pragma warning restore 0414

    [Header("Trigger Timing")]
    [Tooltip("Duration to show 'Center' label in editor after trigger (seconds)")]
    [SerializeField] private float centerLabelDuration = 0.15f;

    // Reference to the child CenterTrigger component for precise position detection
    private CenterTrigger centerTrigger;

    // Currently interacting player (Lem) - available to subclasses
    protected LemController currentPlayer;

    // Cached reference to BlockInventory to avoid repeated FindObjectOfType calls
    private static BlockInventory _cachedInventory;
    private static EditorController _cachedEditorController;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes block components and generates unique ID if needed.
    /// Caches renderer reference for performance.
    /// </summary>
    protected virtual void Awake()
    {
        // Generate unique ID if not set (important for save/load system)
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
            DebugLog.Info($"[BaseBlock] Generated new unique ID for block at index {gridIndex}: {uniqueID}");
        }

        EnsureVisualRoot();
    }

    /// <summary>
    /// Registers block with GridManager and initializes center trigger.
    /// Called after all Awake() methods have executed.
    /// </summary>
    protected virtual void Start()
    {
        // Register with GridManager if valid grid index is set
        if (gridIndex >= 0)
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.RegisterBlock(this);
                DebugLog.Info($"[BaseBlock] Registered {blockType} block at grid index {gridIndex}");
            }
            else
            {
                Debug.LogError($"[BaseBlock] GridManager.Instance is null! Cannot register block at index {gridIndex}. Ensure GameInitializer runs first.");
            }
        }
        else
        {
            Debug.LogWarning($"[BaseBlock] Block '{gameObject.name}' has invalid grid index ({gridIndex}). It will not be registered with GridManager.");
        }

        // Setup center trigger for precise position detection
        EnsureCenterTrigger();
    }

    #endregion

    #region Instantiation and Destruction

    /// <summary>
    /// Factory method to create a new block of the specified type.
    /// Attempts to load from Resources/Blocks/ prefabs first, falls back to primitive cube.
    ///
    /// Design Pattern: Factory Method
    /// </summary>
    /// <param name="type">The type of block to create</param>
    /// <param name="gridIndex">Grid position index for this block</param>
    /// <returns>The created BaseBlock instance, or null if creation fails</returns>
    public static BaseBlock Instantiate(BlockType type, int gridIndex)
    {
        try
        {
            // Try to load prefab from Resources folder first
            GameObject blockObj = TryInstantiatePrefab(type);
            if (blockObj == null)
            {
                // Fallback: Create primitive cube
                blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DebugLog.Info($"[BaseBlock] Created primitive cube for {type} block (no prefab found)");
            }

            // Set descriptive name for hierarchy
            blockObj.name = $"Block_{type}_{gridIndex}";

            // Get or add appropriate block component
            BaseBlock block = blockObj.GetComponent<BaseBlock>();
            if (block == null || !IsBlockComponentCompatible(block, type))
            {
                if (block != null)
                {
                    Destroy(block);
                }
                block = AddBlockComponent(blockObj, type);
            }

            // Initialize block properties
            block.blockType = type;
            block.gridIndex = gridIndex;
            block.uniqueID = Guid.NewGuid().ToString();

            // Add Rigidbody for collision detection (kinematic = doesn't fall)
            Rigidbody rb = blockObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = blockObj.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;  // Block should not be affected by physics
            rb.useGravity = false;  // No gravity for wall blocks

            // Apply color based on block type
            Renderer renderer = blockObj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Color blockColor = BlockColors.GetColorForBlockType(type);
                renderer.material.color = blockColor;
                block.originalColor = blockColor;
            }
            else
            {
                Debug.LogWarning($"[BaseBlock] No Renderer found on {type} block. Block will be invisible.");
            }

            DebugLog.Info($"[BaseBlock] Successfully instantiated {type} block at grid index {gridIndex}");
            return block;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BaseBlock] Failed to instantiate {type} block at grid index {gridIndex}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Adds the appropriate block component based on block type.
    /// Uses polymorphism to enable different block behaviors.
    /// </summary>
    /// <param name="blockObj">GameObject to add component to</param>
    /// <param name="type">Type of block component to add</param>
    /// <returns>The added BaseBlock component</returns>
    private static BaseBlock AddBlockComponent(GameObject blockObj, BlockType type)
    {
        BaseBlock block;

        switch (type)
        {
            case BlockType.Crumbler:
                block = blockObj.AddComponent<CrumblerBlock>();
                DebugLog.Info($"[BaseBlock] Added CrumblerBlock component to {blockObj.name}");
                break;

            case BlockType.Transporter:
                block = blockObj.AddComponent<TransporterBlock>();
                DebugLog.Info($"[BaseBlock] Added TransporterBlock component to {blockObj.name}");
                break;

            case BlockType.Key:
                block = blockObj.AddComponent<KeyBlock>();
                DebugLog.Info($"[BaseBlock] Added KeyBlock component to {blockObj.name}");
                break;

            case BlockType.Lock:
                block = blockObj.AddComponent<LockBlock>();
                DebugLog.Info($"[BaseBlock] Added LockBlock component to {blockObj.name}");
                break;

            case BlockType.Teleporter:
                block = blockObj.AddComponent<TeleporterBlock>();
                DebugLog.Info($"[BaseBlock] Added TeleporterBlock component to {blockObj.name}");
                break;

            case BlockType.Default:
            default:
                block = blockObj.AddComponent<BaseBlock>();
                DebugLog.Info($"[BaseBlock] Added BaseBlock component to {blockObj.name}");
                break;
        }

        return block;
    }

    private static bool IsBlockComponentCompatible(BaseBlock block, BlockType type)
    {
        if (block == null) return false;

        switch (type)
        {
            case BlockType.Crumbler:
                return block is CrumblerBlock;
            case BlockType.Transporter:
                return block is TransporterBlock;
            case BlockType.Key:
                return block is KeyBlock;
            case BlockType.Lock:
                return block is LockBlock;
            case BlockType.Teleporter:
                return block is TeleporterBlock;
            case BlockType.Default:
            default:
                return block.GetType() == typeof(BaseBlock);
        }
    }

    /// <summary>
    /// Attempts to load and instantiate a block prefab from Resources folder.
    /// Expected location: Resources/Blocks/Block_{BlockType}.prefab
    /// </summary>
    /// <param name="type">Type of block prefab to load</param>
    /// <returns>Instantiated prefab GameObject, or null if not found</returns>
    private static GameObject TryInstantiatePrefab(BlockType type)
    {
        string prefabName = $"Blocks/Block_{type}";
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Blocks/BaseBlock");
            if (prefab == null)
            {
                DebugLog.Info($"[BaseBlock] No prefab found at Resources/{prefabName} or Resources/Blocks/BaseBlock, will use primitive");
                return null;
            }

            DebugLog.Info($"[BaseBlock] Loaded base prefab from Resources/Blocks/BaseBlock for {type}");
            return Instantiate(prefab);
        }

        DebugLog.Info($"[BaseBlock] Loaded prefab from Resources/{prefabName}");
        return Instantiate(prefab);
    }

    private void EnsureVisualRoot()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
        }

        if (visualRoot == null)
        {
            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(transform, false);
            visualRoot = visualObj.transform;
        }

        MeshFilter rootMesh = GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();

        MeshFilter visualMesh = visualRoot.GetComponent<MeshFilter>();
        MeshRenderer visualRenderer = visualRoot.GetComponent<MeshRenderer>();

        if (visualMesh == null && rootMesh != null)
        {
            visualMesh = visualRoot.gameObject.AddComponent<MeshFilter>();
            visualMesh.sharedMesh = rootMesh.sharedMesh;
        }

        if (visualRenderer == null && rootRenderer != null)
        {
            visualRenderer = visualRoot.gameObject.AddComponent<MeshRenderer>();
            visualRenderer.sharedMaterials = rootRenderer.sharedMaterials;
            visualRenderer.enabled = rootRenderer.enabled;
        }

        if (rootRenderer != null && visualRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        if (autoNormalizeVisualScale && visualMesh != null && visualMesh.sharedMesh != null)
        {
            Bounds bounds = visualMesh.sharedMesh.bounds;
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxSize > 0f)
            {
                visualRoot.localScale = Vector3.one * (1f / maxSize);
            }
        }

        blockRenderer = visualRoot.GetComponentInChildren<Renderer>();
        if (blockRenderer == null)
        {
            blockRenderer = GetComponent<Renderer>();
        }

        if (blockRenderer != null)
        {
            originalColor = blockRenderer.material.color;
        }
        else
        {
            Debug.LogWarning($"[BaseBlock] No Renderer component found on block {gameObject.name}. Highlighting will not work.");
        }
    }

    /// <summary>
    /// Destroys this block and returns it to inventory if applicable.
    /// Safe to call multiple times - checks for null references.
    /// </summary>
    public void DestroyBlock()
    {
        try
        {
            // Return block to inventory if it's not permanent
            if (!isPermanent)
            {
                // Use cached inventory reference to avoid expensive FindObjectOfType
                if (_cachedInventory == null)
                {
                    _cachedInventory = UnityEngine.Object.FindObjectOfType<BlockInventory>();
                }

                if (_cachedInventory != null)
                {
                    if (!string.IsNullOrEmpty(inventoryKey))
                    {
                        _cachedInventory.ReturnBlock(inventoryKey, blockType);
                    }
                    else
                    {
                        _cachedInventory.ReturnBlock(blockType);
                    }
                    DebugLog.Info($"[BaseBlock] Returned {blockType} block at index {gridIndex} to inventory");
                }
                else
                {
                    Debug.LogWarning($"[BaseBlock] No BlockInventory found to return {blockType} block");
                }
            }
            else
            {
                DebugLog.Info($"[BaseBlock] Permanent {blockType} block at index {gridIndex} destroyed (not returned to inventory)");
            }

            // Unregister from GridManager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.UnregisterBlock(this);
                DebugLog.Info($"[BaseBlock] Unregistered {blockType} block from GridManager");
            }
            else
            {
                Debug.LogWarning($"[BaseBlock] GridManager.Instance is null, cannot unregister block");
            }

            // Destroy the GameObject
            Destroy(gameObject);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BaseBlock] Error destroying block at index {gridIndex}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// Handles player entering the block's trigger zone.
    /// NOTE: Blocks use BOTH triggers and collisions to ensure reliable detection.
    /// Triggers are used for soft detection, collisions for hard physical contact.
    /// </summary>
    /// <param name="other">The collider that entered</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayModeActive()) return;

        if (other.CompareTag("Player"))
        {
            currentPlayer = other.GetComponent<LemController>();
            isPlayerOnBlock = true;
            lastTriggerState = TriggerState.On;

            OnPlayerEnter();
        }
    }

    /// <summary>
    /// Handles player exiting the block's trigger zone.
    /// Resets player reference and notifies subclasses.
    /// </summary>
    /// <param name="other">The collider that exited</param>
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayModeActive()) return;

        if (other.CompareTag("Player"))
        {
            isPlayerOnBlock = false;
            currentPlayer = null;
            lastTriggerState = TriggerState.Off;

            DebugLog.Info($"[BaseBlock] Player exited trigger on {blockType} block at index {gridIndex}");
            OnPlayerExit();
        }
    }

    /// <summary>
    /// Updates trigger state while player remains in trigger zone.
    /// Used for continuous state tracking in editor visualization.
    /// </summary>
    /// <param name="other">The collider staying in trigger</param>
    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayModeActive()) return;

        if (other.CompareTag("Player"))
        {
            if (!isPlayerOnBlock)
            {
                currentPlayer = other.GetComponent<LemController>();
                isPlayerOnBlock = true;
                OnPlayerEnter();
            }
            lastTriggerState = TriggerState.On;
        }
    }

    /// <summary>
    /// Handles player collision with block (physical contact).
    /// Provides redundant detection alongside triggers for reliability.
    /// </summary>
    /// <param name="collision">Collision data</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsPlayModeActive()) return;

        if (collision.collider.CompareTag("Player"))
        {
            currentPlayer = collision.collider.GetComponent<LemController>();
            isPlayerOnBlock = true;
            lastTriggerState = TriggerState.On;

            OnPlayerEnter();
        }
    }

    /// <summary>
    /// Handles player ending collision with block.
    /// </summary>
    /// <param name="collision">Collision data</param>
    private void OnCollisionExit(Collision collision)
    {
        if (!IsPlayModeActive()) return;

        if (collision.collider.CompareTag("Player"))
        {
            isPlayerOnBlock = false;
            currentPlayer = null;
            lastTriggerState = TriggerState.Off;

            OnPlayerExit();
        }
    }

    /// <summary>
    /// Updates collision state while player remains in contact.
    /// </summary>
    /// <param name="collision">Collision data</param>
    private void OnCollisionStay(Collision collision)
    {
        if (!IsPlayModeActive()) return;

        if (collision.collider.CompareTag("Player"))
        {
            if (!isPlayerOnBlock)
            {
                currentPlayer = collision.collider.GetComponent<LemController>();
                isPlayerOnBlock = true;
                OnPlayerEnter();
            }
            lastTriggerState = TriggerState.On;
        }
    }

    // ===== Template Methods for Subclasses =====
    // Override these in derived classes (CrumblerBlock, TransporterBlock, etc.)

    /// <summary>
    /// Called when player enters this block (either trigger or collision).
    /// Override in subclasses to implement custom behavior.
    /// </summary>
    protected virtual void OnPlayerEnter() { }

    /// <summary>
    /// Called when player exits this block (either trigger or collision).
    /// Override in subclasses to implement custom behavior.
    /// </summary>
    protected virtual void OnPlayerExit() { }

    /// <summary>
    /// Called when player reaches the center point of this block.
    /// Triggered by the CenterTrigger component's sphere collider.
    /// Override in subclasses to implement custom behavior (e.g., teleport, transport).
    /// </summary>
    protected virtual void OnPlayerReachCenter() { }

    #endregion

    #region Highlighting

    /// <summary>
    /// Highlights this block by changing its color.
    /// Used in editor mode for visual feedback when cursor is over block.
    /// </summary>
    public void Highlight()
    {
        if (!isHighlighted && blockRenderer != null)
        {
            isHighlighted = true;
            blockRenderer.material.color = highlightColor;
        }
    }

    /// <summary>
    /// Removes highlight and restores original color.
    /// </summary>
    public void Unhighlight()
    {
        if (isHighlighted && blockRenderer != null)
        {
            isHighlighted = false;
            blockRenderer.material.color = originalColor;
        }
    }

    /// <summary>
    /// Checks if this block is currently highlighted.
    /// </summary>
    /// <returns>True if highlighted, false otherwise</returns>
    public bool IsHighlighted() => isHighlighted;

    #endregion

    #region Grid Position

    /// <summary>
    /// Updates the grid index for this block.
    /// Used when blocks move (e.g., TransporterBlock).
    /// </summary>
    /// <param name="index">New grid index</param>
    public void SetGridIndex(int index) => gridIndex = index;

    /// <summary>
    /// Gets the current grid index.
    /// </summary>
    /// <returns>Linear grid index</returns>
    public int GetGridIndex() => gridIndex;

    /// <summary>
    /// Converts this block's linear grid index to 2D coordinates.
    /// </summary>
    /// <returns>2D grid coordinates (x, y) or Vector2Int.zero if GridManager unavailable</returns>
    public Vector2Int GetGridCoordinates()
    {
        if (GridManager.Instance != null)
        {
            return GridManager.Instance.IndexToCoordinates(gridIndex);
        }

        Debug.LogWarning($"[BaseBlock] Cannot get coordinates: GridManager.Instance is null");
        return Vector2Int.zero;
    }

    #endregion

    #region Placement Validation

    /// <summary>
    /// Checks if this block type can be placed at the specified grid index.
    /// Override in subclasses to implement custom placement rules.
    /// Called by GridManager before placing a block.
    /// </summary>
    /// <param name="targetIndex">The grid index where placement is being attempted</param>
    /// <param name="grid">Reference to the GridManager for coordinate lookups</param>
    /// <returns>True if placement is allowed, false otherwise</returns>
    public virtual bool CanBePlacedAt(int targetIndex, GridManager grid)
    {
        return true; // Default: no restrictions
    }

    /// <summary>
    /// Returns grid indices that this block "claims" and prevents other blocks from occupying.
    /// Override in subclasses that need to reserve multiple grid spaces (e.g., TransporterBlock routes).
    /// Called by GridManager when checking if a space is available.
    /// </summary>
    /// <returns>Array of grid indices blocked by this block (empty by default)</returns>
    public virtual int[] GetBlockedIndices()
    {
        return Array.Empty<int>(); // Default: only occupies own grid index
    }

    /// <summary>
    /// Validates that this block's placement requirements are satisfied after placement.
    /// Override in subclasses that have group requirements (e.g., TeleporterBlock needs a pair).
    /// Called by GridManager after a block is placed to check for incomplete states.
    /// </summary>
    /// <param name="grid">Reference to the GridManager</param>
    /// <returns>True if placement is valid, false if requirements not met</returns>
    public virtual bool ValidateGroupPlacement(GridManager grid)
    {
        return true; // Default: no group requirements
    }

    /// <summary>
    /// Returns a human-readable message explaining why placement failed.
    /// Override alongside CanBePlacedAt to provide helpful feedback.
    /// </summary>
    /// <param name="targetIndex">The grid index where placement was attempted</param>
    /// <param name="grid">Reference to the GridManager</param>
    /// <returns>Error message, or null if placement is allowed</returns>
    public virtual string GetPlacementErrorMessage(int targetIndex, GridManager grid)
    {
        return null; // Default: no error
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showTriggerLabelsInEditor && !showTriggerGizmos) return;

        float halfZ = transform.localScale.z * 0.5f;
        Vector3 frontCenter = transform.position - (transform.forward * halfZ) - (transform.forward * 0.02f);
        Vector3 onTriggerCenter = transform.position + (transform.up * (transform.localScale.y * onTriggerYOffset));
        Vector3 centerTriggerCenter = transform.position + (transform.up * (transform.localScale.y * centerTriggerYOffset + centerTriggerWorldYOffset));

        if (showTriggerGizmos)
        {
            Gizmos.color = Color.cyan;
            Vector3 boxSize = Vector3.Scale(transform.localScale, onTriggerBoxSize);
            Gizmos.DrawWireCube(onTriggerCenter, boxSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerTriggerCenter, centerTriggerRadius);
        }

        if (showTriggerLabelsInEditor)
        {
            UnityEditor.Handles.color = Color.white;
            TriggerState stateToShow = ResolveLabelState(onTriggerCenter, centerTriggerCenter);

            string label = GetLabelForState(stateToShow);
            if (!string.IsNullOrEmpty(label))
            {
                UnityEditor.Handles.Label(frontCenter, label, GetEditorLabelStyle());
            }
        }
    }

    private static GUIStyle _editorLabelStyle;
    private static GUIStyle GetEditorLabelStyle()
    {
        if (_editorLabelStyle != null) return _editorLabelStyle;
        _editorLabelStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            normal = { textColor = Color.black }
        };
        return _editorLabelStyle;
    }

    private static string GetLabelForState(TriggerState state)
    {
        return state switch
        {
            TriggerState.On => "On",
            TriggerState.Center => "Center",
            TriggerState.Off => "Off",
            _ => string.Empty
        };
    }

    private TriggerState ResolveLabelState(Vector3 onTriggerCenter, Vector3 centerTriggerCenter)
    {
        if (Application.isPlaying && Time.timeScale > 0f)
        {
            if (Time.time < centerLabelUntil) return TriggerState.Center;
            if (isPlayerOnBlock) return TriggerState.On;
            return TriggerState.Off;
        }

        Vector3 boxSize = Vector3.Scale(transform.localScale, onTriggerBoxSize);
        Collider[] hits = Physics.OverlapBox(onTriggerCenter, boxSize * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        bool playerInBox = false;
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            playerInBox = true;
            Vector3 footPoint = GetFootPoint(hit);
            if (Vector3.Distance(footPoint, centerTriggerCenter) <= centerTriggerRadius)
            {
                return TriggerState.Center;
            }
        }

        if (playerInBox) return TriggerState.On;
        return TriggerState.Off;
    }

    private static Vector3 GetFootPoint(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        return new Vector3(center.x, bounds.min.y, center.z);
    }
#endif

    #region Center Trigger Management

    /// <summary>
    /// Ensures this block has a CenterTrigger child component.
    /// Creates one automatically if missing.
    /// The CenterTrigger detects when Lem reaches the center/top of the block.
    /// </summary>
    private void EnsureCenterTrigger()
    {
        // First, check if we already have a CenterTrigger component
        if (centerTrigger == null)
        {
            centerTrigger = GetComponentInChildren<CenterTrigger>();
        }

        // If still null, create a new CenterTrigger GameObject
        if (centerTrigger == null)
        {
            GameObject triggerObj = new GameObject("CenterTrigger");
            triggerObj.transform.SetParent(transform);
            centerTrigger = triggerObj.AddComponent<CenterTrigger>();
            centerTrigger.Initialize(this);

            DebugLog.Info($"[BaseBlock] Created CenterTrigger for {blockType} block at index {gridIndex}");
        }

        // Update trigger shape to match block dimensions
        centerTrigger.UpdateShape();
    }

    /// <summary>
    /// Enables or disables the center trigger collider.
    /// Used by subclasses (e.g., TransporterBlock) to control when center detection is active.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable</param>
    protected void SetCenterTriggerEnabled(bool enabled)
    {
        if (centerTrigger == null)
        {
            EnsureCenterTrigger();
        }

        if (centerTrigger != null)
        {
            centerTrigger.SetEnabled(enabled);
            DebugLog.Info($"[BaseBlock] Center trigger {(enabled ? "enabled" : "disabled")} for {blockType} block");
        }
    }

    /// <summary>
    /// Updates the center trigger shape and position.
    /// Call this after modifying trigger parameters in Inspector.
    /// </summary>
    public void UpdateCenterTrigger()
    {
        if (centerTrigger == null)
        {
            EnsureCenterTrigger();
            return;
        }

        centerTrigger.UpdateShape();
    }

    /// <summary>
    /// Called by CenterTrigger when Lem enters the center detection zone.
    /// This is a callback - don't call directly.
    /// </summary>
    /// <param name="lem">The Lem that entered the center</param>
    public void NotifyCenterTriggerEnter(LemController lem)
    {
        if (!IsPlayModeActive()) return;

        if (lem != null)
        {
            currentPlayer = lem;
        }

        lastTriggerState = TriggerState.Center;
        centerLabelUntil = Time.time + centerLabelDuration;

        OnPlayerReachCenter();
    }

    /// <summary>
    /// Called by CenterTrigger when Lem exits the center detection zone.
    /// This is a callback - don't call directly.
    /// </summary>
    public void NotifyCenterTriggerExit()
    {
        if (!IsPlayModeActive()) return;

        if (isPlayerOnBlock)
        {
            lastTriggerState = TriggerState.On;
            DebugLog.Info($"[BaseBlock] Player exited center of {blockType} block");
        }
    }

    private static bool IsPlayModeActive()
    {
        if (_cachedEditorController == null)
        {
            _cachedEditorController = UnityEngine.Object.FindObjectOfType<EditorController>();
        }

        return _cachedEditorController != null && _cachedEditorController.currentMode == GameMode.Play;
    }

    #endregion
}
