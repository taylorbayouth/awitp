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
        public float cameraDistance = 150f;
        public float gridMargin = 1f;
        public float nearClipPlane = 0.24f;
        public float farClipPlane = 500f;

        public CameraSettings Clone()
        {
            return new CameraSettings
            {
                cameraDistance = cameraDistance,
                gridMargin = gridMargin,
                nearClipPlane = nearClipPlane,
                farClipPlane = farClipPlane
            };
        }
    }

    // Grid settings (cells are 1.0 world unit each)
    public int gridWidth;
    public int gridHeight;

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

    public LevelData Clone()
    {
        LevelData copy = new LevelData
        {
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            levelName = levelName,
            saveTimestamp = saveTimestamp
        };

        if (cameraSettings != null)
        {
            copy.cameraSettings = cameraSettings.Clone();
        }

        if (blocks != null)
        {
            copy.blocks = new List<BlockData>(blocks.Count);
            foreach (BlockData block in blocks)
            {
                if (block == null) continue;
                BlockData cloned = new BlockData(block.blockType, block.gridIndex)
                {
                    inventoryKey = block.inventoryKey,
                    flavorId = block.flavorId,
                    routeSteps = block.routeSteps != null ? (string[])block.routeSteps.Clone() : null
                };
                copy.blocks.Add(cloned);
            }
        }

        if (permanentBlocks != null)
        {
            copy.permanentBlocks = new List<BlockData>(permanentBlocks.Count);
            foreach (BlockData block in permanentBlocks)
            {
                if (block == null) continue;
                BlockData cloned = new BlockData(block.blockType, block.gridIndex)
                {
                    inventoryKey = block.inventoryKey,
                    flavorId = block.flavorId,
                    routeSteps = block.routeSteps != null ? (string[])block.routeSteps.Clone() : null
                };
                copy.permanentBlocks.Add(cloned);
            }
        }

        if (inventoryEntries != null)
        {
            copy.inventoryEntries = new List<BlockInventoryEntry>(inventoryEntries.Count);
            foreach (BlockInventoryEntry entry in inventoryEntries)
            {
                if (entry == null) continue;
                copy.inventoryEntries.Add(entry.Clone());
            }
        }

        if (placeableSpaceIndices != null)
        {
            copy.placeableSpaceIndices = new List<int>(placeableSpaceIndices);
        }

        if (lems != null)
        {
            copy.lems = new List<LemData>(lems.Count);
            foreach (LemData lem in lems)
            {
                if (lem == null) continue;
                LemData cloned = new LemData(lem.gridIndex, lem.facingRight)
                {
                    worldPosition = lem.worldPosition,
                    hasWorldPosition = lem.hasWorldPosition
                };
                copy.lems.Add(cloned);
            }
        }

        if (keyStates != null)
        {
            copy.keyStates = new List<KeyStateData>(keyStates.Count);
            foreach (KeyStateData keyState in keyStates)
            {
                if (keyState == null) continue;
                KeyStateData cloned = new KeyStateData
                {
                    sourceKeyBlockIndex = keyState.sourceKeyBlockIndex,
                    location = keyState.location,
                    targetIndex = keyState.targetIndex,
                    worldPosition = keyState.worldPosition,
                    hasWorldPosition = keyState.hasWorldPosition
                };
                copy.keyStates.Add(cloned);
            }
        }

        return copy;
    }
}
