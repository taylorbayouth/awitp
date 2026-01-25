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

        public LemData(int index, bool facing)
        {
            gridIndex = index;
            facingRight = facing;
        }
    }

    // Grid settings
    public int gridWidth;
    public int gridHeight;
    public float cellSize;

    // Placed blocks (player-placeable spaces)
    public List<BlockData> blocks = new List<BlockData>();

    // Permanent blocks (designer-placed, non-placeable)
    public List<BlockData> permanentBlocks = new List<BlockData>();

    // Placeable spaces (stored as indices where placeable = true)
    public List<int> placeableSpaceIndices = new List<int>();

    // Placed Lems
    public List<LemData> lems = new List<LemData>();

    // Metadata
    public string levelName;
    public string saveTimestamp;

    public LevelData()
    {
        saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
