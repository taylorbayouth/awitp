using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data structure for saving/loading level state.
/// Stores all information needed to recreate a level: blocks, placeable spaces, and Lem placements.
/// </summary>
[Serializable]
public class LevelData
{
    [Serializable]
    public class BlockData
    {
        public BlockType blockType;
        public int gridIndex;
        public string inventoryKey;
        public string flavorId;
        public string[] routeSteps;

        public BlockData(BlockType type, int index)
        {
            blockType = type;
            gridIndex = index;
        }
    }

    [Serializable]
    public class LemData
    {
        public int gridIndex;
        public bool facingRight;
        public Vector3 worldPosition;
        public bool hasWorldPosition;

        public LemData(int index, bool facing)
        {
            gridIndex = index;
            facingRight = facing;
        }
    }

    public enum KeyLocation
    {
        KeyBlock,
        LockBlock,
        Lem,
        World
    }

    [Serializable]
    public class KeyStateData
    {
        public int sourceKeyBlockIndex;
        public KeyLocation location;
        public int targetIndex;
        public Vector3 worldPosition;
        public bool hasWorldPosition;
    }

    [Serializable]
    public class CameraSettings
    {
        // Perspective settings
        public float fieldOfView = 45f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 100f;

        // Camera offsets
        public float verticalOffset = 0f;
        public float horizontalOffset = 0f;

        // Camera rotation
        public float tiltAngle = 0f;
        public float panAngle = 0f;

        // Framing settings
        public float gridMargin = 1.0f;  // Fixed margin around grid in world units
        public float minDistance = 8f;
    }

    // Grid settings
    public int gridWidth;
    public int gridHeight;
    public float cellSize;

    // Camera settings
    public CameraSettings cameraSettings;

    // Placed blocks (player-placeable spaces)
    public List<BlockData> blocks = new List<BlockData>();

    // Permanent blocks (designer-placed, non-placeable)
    public List<BlockData> permanentBlocks = new List<BlockData>();

    // Inventory configuration for this level
    public List<BlockInventoryEntry> inventoryEntries = new List<BlockInventoryEntry>();

    // Placeable spaces (stored as indices where placeable = true)
    public List<int> placeableSpaceIndices = new List<int>();

    // Placed Lems
    public List<LemData> lems = new List<LemData>();

    // Key states (source block + current location)
    public List<KeyStateData> keyStates = new List<KeyStateData>();

    // Metadata
    public string levelName;
    public string saveTimestamp;

    public LevelData()
    {
        saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
