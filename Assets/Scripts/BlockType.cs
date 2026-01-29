using UnityEngine;

/// <summary>
/// Defines the available block types in the game.
/// Each type has unique behavior and visual properties defined in BlockColors.
/// </summary>
public enum BlockType
{
    /// <summary>
    /// Walk block - simple walkable platform that Lem can stand on (cyan)
    /// </summary>
    Walk,

    /// <summary>
    /// Teleporter block - transports player to paired teleporter (magenta)
    /// </summary>
    Teleporter,

    /// <summary>
    /// Crumbler block - breaks after being walked on (orange)
    /// </summary>
    Crumbler,

    /// <summary>
    /// Transporter block - moves player or objects (yellow)
    /// </summary>
    Transporter,

    /// <summary>
    /// Key block - holds a collectible key (green)
    /// </summary>
    Key,

    /// <summary>
    /// Lock block - accepts and holds keys (black)
    /// </summary>
    Lock
}
