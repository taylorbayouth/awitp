using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optional scene-level inventory config for quick designer setup.
/// Attach to an empty GameObject and fill entries in the Inspector.
/// </summary>
public class LevelBlockInventoryConfig : MonoBehaviour
{
    public List<BlockInventoryEntry> entries = new List<BlockInventoryEntry>();
}
