using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Base class for all block types in the puzzle game.
/// Blocks exist on the XY plane (Z=0).
/// </summary>
public class BaseBlock : MonoBehaviour
{
    [Header("Block Identity")]
    public string uniqueID;
    public BlockType blockType = BlockType.Default;
    public int gridIndex = -1;
    public bool isPermanent = false;

    [Header("Detection Settings")]
    public float detectionSize = 1f;
    public LayerMask detectionLayerMask;

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    private Color originalColor;
    private Renderer blockRenderer;
    private bool isHighlighted = false;

    private bool playerAtCenter = false;

    public enum Direction { Left, Right, Up, Down }

    private Dictionary<Direction, List<Collider>> surroundingObjects = new Dictionary<Direction, List<Collider>>();

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }

        blockRenderer = GetComponent<Renderer>();
        if (blockRenderer != null)
        {
            originalColor = blockRenderer.material.color;
        }

        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            surroundingObjects[dir] = new List<Collider>();
        }
    }

    private void Start()
    {
        if (gridIndex >= 0 && GridManager.Instance != null)
        {
            GridManager.Instance.RegisterBlock(this);
        }
    }

    private void Update()
    {
        DetectSurroundingSpaces();
    }

    #region Instantiation and Destruction

    public static BaseBlock Instantiate(BlockType type, int gridIndex)
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.name = $"Block_{type}_{gridIndex}";

        BaseBlock block = blockObj.AddComponent<BaseBlock>();
        block.blockType = type;
        block.gridIndex = gridIndex;
        block.uniqueID = Guid.NewGuid().ToString();

        // Blocks are kinematic (don't fall)
        Rigidbody rb = blockObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer renderer = blockObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color blockColor = BlockColors.GetColorForBlockType(type);
            renderer.material.color = blockColor;
            block.originalColor = blockColor;
        }

        return block;
    }

    public void DestroyBlock()
    {
        BlockInventory inventory = FindObjectOfType<BlockInventory>();
        if (!isPermanent && inventory != null)
        {
            inventory.ReturnBlock(blockType);
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterBlock(this);
        }

        Destroy(gameObject);
    }

    #endregion

    #region Spatial Detection

    private void DetectSurroundingSpaces()
    {
        foreach (var list in surroundingObjects.Values)
        {
            list.Clear();
        }

        // Check 4 directions on the XY plane
        CheckSpace(Direction.Left, Vector3.left);
        CheckSpace(Direction.Right, Vector3.right);
        CheckSpace(Direction.Up, Vector3.up);
        CheckSpace(Direction.Down, Vector3.down);
    }

    private void CheckSpace(Direction direction, Vector3 offset)
    {
        Vector3 checkPosition = transform.position + (offset * detectionSize);
        Vector3 boxSize = Vector3.one * (detectionSize * 0.9f);

        Collider[] hits = Physics.OverlapBox(checkPosition, boxSize / 2f, Quaternion.identity, detectionLayerMask);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject != gameObject)
            {
                surroundingObjects[direction].Add(hit);
            }
        }
    }

    public List<Collider> GetObjectsInDirection(Direction direction)
    {
        return surroundingObjects[direction];
    }

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
            float distanceToCenter = Vector3.Distance(
                new Vector3(other.transform.position.x, other.transform.position.y, transform.position.z),
                transform.position
            );

            if (distanceToCenter < 0.2f && !playerAtCenter)
            {
                playerAtCenter = true;
                OnPlayerReachCenter();
            }
        }
    }

    protected virtual void OnPlayerEnter() { }
    protected virtual void OnPlayerExit() { }
    protected virtual void OnPlayerReachCenter() { }

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

    public bool IsHighlighted() => isHighlighted;

    #endregion

    #region Grid Position

    public void SetGridIndex(int index) => gridIndex = index;
    public int GetGridIndex() => gridIndex;

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
        Gizmos.color = Color.red;
        float size = detectionSize;

        // Draw 4 detection spaces
        Vector3[] offsets = { Vector3.left, Vector3.right, Vector3.up, Vector3.down };

        foreach (Vector3 offset in offsets)
        {
            Vector3 pos = transform.position + (offset * size);
            Gizmos.DrawWireCube(pos, Vector3.one * size * 0.9f);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * size);
    }

    #endregion
}
