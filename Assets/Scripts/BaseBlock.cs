using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Base class for all block types in the puzzle game.
/// Handles placement, spatial awareness (detecting surrounding objects),
/// player detection, and visual highlighting.
/// </summary>
public class BaseBlock : MonoBehaviour
{
    [Header("Block Identity")]
    public string uniqueID;
    public BlockType blockType = BlockType.Default;
    public int gridIndex = -1; // -1 means not placed on grid yet

    [Header("Detection Settings")]
    public float detectionSize = 1f; // Size of detection box (1 unit cube)
    public LayerMask detectionLayerMask; // What layers to detect

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    private Color originalColor;
    private Renderer blockRenderer;
    private bool isHighlighted = false;

    [Header("Player Detection")]
    private bool playerAtCenter = false;

    // Store what's in each surrounding space
    private Dictionary<Direction, List<Collider>> surroundingObjects = new Dictionary<Direction, List<Collider>>();

    public enum Direction
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    private void Awake()
    {
        // Generate unique ID if not already set
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }

        // Get renderer for highlighting
        blockRenderer = GetComponent<Renderer>();
        if (blockRenderer != null)
        {
            originalColor = blockRenderer.material.color;
        }

        // Initialize surrounding objects dictionary
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            surroundingObjects[dir] = new List<Collider>();
        }
    }

    private void Start()
    {
        // Register with grid manager if we have a valid grid index
        if (gridIndex >= 0 && GridManager.Instance != null)
        {
            GridManager.Instance.RegisterBlock(this);
        }
    }

    private void Update()
    {
        // Continuously detect surrounding spaces
        DetectSurroundingSpaces();
    }

    #region Instantiation and Destruction

    /// <summary>
    /// Factory method for creating and configuring a new block instance.
    /// Called by GridManager when placing a block.
    /// </summary>
    public static BaseBlock Instantiate(BlockType type, int gridIndex)
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.name = $"Block_{type}_{gridIndex}";

        BaseBlock block = blockObj.AddComponent<BaseBlock>();
        block.blockType = type;
        block.gridIndex = gridIndex;
        block.uniqueID = Guid.NewGuid().ToString();

        // Add Rigidbody
        Rigidbody rb = blockObj.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Blocks don't fall, but can be moved programmatically
        rb.useGravity = false;

        // Set color based on block type
        Renderer renderer = blockObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color blockColor = GetColorForBlockType(type);
            renderer.material.color = blockColor;
            // Update the original color reference so highlighting works correctly
            block.originalColor = blockColor;
        }

        // Position will be set by GridManager

        return block;
    }

    private static Color GetColorForBlockType(BlockType type)
    {
        return BlockColors.GetColorForBlockType(type);
    }

    /// <summary>
    /// Destroys this block and returns it to the inventory.
    /// </summary>
    public void DestroyBlock()
    {
        BlockInventory inventory = FindObjectOfType<BlockInventory>();
        if (inventory != null)
        {
            inventory.ReturnBlock(blockType);
        }

        // Unregister from grid manager
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterBlock(this);
        }

        // Destroy the game object
        Destroy(gameObject);
    }

    #endregion

    #region Spatial Detection

    /// <summary>
    /// Continuously detects objects in the 8 surrounding grid spaces.
    /// Called every frame to maintain awareness of nearby blocks and entities.
    /// </summary>
    private void DetectSurroundingSpaces()
    {
        foreach (var list in surroundingObjects.Values)
        {
            list.Clear();
        }

        // Check each of the 8 surrounding spaces
        CheckSpace(Direction.North, Vector3.forward);
        CheckSpace(Direction.NorthEast, new Vector3(1, 0, 1).normalized);
        CheckSpace(Direction.East, Vector3.right);
        CheckSpace(Direction.SouthEast, new Vector3(1, 0, -1).normalized);
        CheckSpace(Direction.South, Vector3.back);
        CheckSpace(Direction.SouthWest, new Vector3(-1, 0, -1).normalized);
        CheckSpace(Direction.West, Vector3.left);
        CheckSpace(Direction.NorthWest, new Vector3(-1, 0, 1).normalized);
    }

    private void CheckSpace(Direction direction, Vector3 offset)
    {
        Vector3 checkPosition = transform.position + (offset * detectionSize);
        Vector3 boxSize = Vector3.one * (detectionSize * 0.9f); // Slightly smaller to avoid overlap

        Collider[] hits = Physics.OverlapBox(checkPosition, boxSize / 2f, Quaternion.identity, detectionLayerMask);

        foreach (Collider hit in hits)
        {
            // Don't detect self
            if (hit.gameObject != gameObject)
            {
                surroundingObjects[direction].Add(hit);
            }
        }
    }

    /// <summary>
    /// Gets all colliders detected in a specific direction.
    /// </summary>
    public List<Collider> GetObjectsInDirection(Direction direction)
    {
        return surroundingObjects[direction];
    }

    /// <summary>
    /// Gets all blocks detected in a specific direction (filters for BaseBlock components).
    /// </summary>
    public List<BaseBlock> GetBlocksInDirection(Direction direction)
    {
        List<BaseBlock> blocks = new List<BaseBlock>();
        foreach (Collider col in surroundingObjects[direction])
        {
            BaseBlock block = col.GetComponent<BaseBlock>();
            if (block != null)
            {
                blocks.Add(block);
            }
        }
        return blocks;
    }

    #endregion

    #region Player Detection

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerAtCenter = false;
            OnPlayerExit();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if player is at center of block
            float distanceToCenter = Vector3.Distance(
                new Vector3(other.transform.position.x, transform.position.y, other.transform.position.z),
                transform.position
            );

            if (distanceToCenter < 0.2f && !playerAtCenter)
            {
                playerAtCenter = true;
                OnPlayerReachCenter();
            }
        }
    }

    // Virtual methods for child classes to override
    protected virtual void OnPlayerEnter()
    {
        Debug.Log($"Player entered block {uniqueID} ({blockType})");
    }

    protected virtual void OnPlayerExit()
    {
        Debug.Log($"Player exited block {uniqueID} ({blockType})");
    }

    protected virtual void OnPlayerReachCenter()
    {
        Debug.Log($"Player reached center of block {uniqueID} ({blockType})");
    }

    #endregion

    #region Highlighting

    public void Highlight()
    {
        if (!isHighlighted && blockRenderer != null)
        {
            isHighlighted = true;
            blockRenderer.material.color = highlightColor;
        }
    }

    public void Unhighlight()
    {
        if (isHighlighted && blockRenderer != null)
        {
            isHighlighted = false;
            blockRenderer.material.color = originalColor;
        }
    }

    public bool IsHighlighted()
    {
        return isHighlighted;
    }

    #endregion

    #region Grid Position

    public void SetGridIndex(int index)
    {
        gridIndex = index;
    }

    public int GetGridIndex()
    {
        return gridIndex;
    }

    public Vector2Int GetGridCoordinates()
    {
        if (GridManager.Instance != null)
        {
            return GridManager.Instance.IndexToCoordinates(gridIndex);
        }
        return Vector2Int.zero;
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        // Visualize the 8 surrounding detection spaces
        Gizmos.color = Color.red;
        float size = detectionSize;

        Vector3[] offsets = new Vector3[]
        {
            Vector3.forward,                    // North
            new Vector3(1, 0, 1).normalized,    // NorthEast
            Vector3.right,                      // East
            new Vector3(1, 0, -1).normalized,   // SouthEast
            Vector3.back,                       // South
            new Vector3(-1, 0, -1).normalized,  // SouthWest
            Vector3.left,                       // West
            new Vector3(-1, 0, 1).normalized    // NorthWest
        };

        foreach (Vector3 offset in offsets)
        {
            Vector3 pos = transform.position + (offset * size);
            Gizmos.DrawWireCube(pos, Vector3.one * size * 0.9f);
        }

        // Draw self in green
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * size);
    }

    #endregion
}
