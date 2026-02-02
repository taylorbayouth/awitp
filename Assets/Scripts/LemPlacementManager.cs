using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages Lem (player character) placement and lifecycle for the level editor.
///
/// RESPONSIBILITIES:
/// - Lem placement and removal
/// - Original placement tracking (for reset after Play mode)
/// - Play mode snapshot capture and restoration
/// - Key state management
///
/// DESIGN NOTE: Currently only one Lem per level is supported.
/// If multiple Lems needed in future, remove the single-Lem constraint.
///
/// DEPENDENCIES:
/// - GridCoordinateSystem for position calculations
/// - LemController for Lem behavior
/// </summary>
public class LemPlacementManager : MonoBehaviour
{
    private GridCoordinateSystem coordinateSystem;

    // Current Lems placed in the level
    private Dictionary<int, GameObject> placedLems = new Dictionary<int, GameObject>();

    // Original placements for resetting after Play mode
    private Dictionary<int, LemPlacementData> originalLemPlacements = new Dictionary<int, LemPlacementData>();

    // Snapshot data for Play mode state
    private List<LevelData.KeyStateData> originalKeyStates = new List<LevelData.KeyStateData>();
    private LevelData playModeSnapshot;

    /// <summary>
    /// Stores original Lem placement data for resetting after Play mode.
    /// </summary>
    private class LemPlacementData
    {
        public int gridIndex;
        public bool facingRight;

        public LemPlacementData(int gridIndex, bool facingRight)
        {
            this.gridIndex = gridIndex;
            this.facingRight = facingRight;
        }
    }

    /// <summary>
    /// Initializes the Lem placement manager.
    /// </summary>
    /// <param name="coordinateSystem">Grid coordinate system</param>
    public void Initialize(GridCoordinateSystem coordinateSystem)
    {
        this.coordinateSystem = coordinateSystem;
        DebugLog.Info("[LemPlacementManager] Initialized");
    }

    #region Lem Placement

    /// <summary>
    /// Places a Lem at the specified grid index.
    /// If Lem already exists there, turns it around.
    /// Only one Lem allowed per level - removes existing if placing at new location.
    /// </summary>
    /// <param name="gridIndex">Grid index to place Lem</param>
    /// <returns>Lem GameObject or null if index invalid</returns>
    public GameObject PlaceLem(int gridIndex)
    {
        if (coordinateSystem == null || !coordinateSystem.IsValidIndex(gridIndex))
        {
            Debug.LogWarning($"[LemPlacementManager] Invalid grid index: {gridIndex}");
            return null;
        }

        // If Lem already exists at this position, turn it around
        if (placedLems.TryGetValue(gridIndex, out GameObject existingLem))
        {
            LemController lemController = existingLem.GetComponent<LemController>();
            if (lemController != null)
            {
                lemController.TurnAround();
                // Update original placement data with new facing direction
                originalLemPlacements[gridIndex] = new LemPlacementData(gridIndex, lemController.GetFacingRight());
            }
            return existingLem;
        }

        // Only one Lem allowed - remove any existing Lem first
        if (placedLems.Count > 0)
        {
            ClearAllLems();
        }

        Vector3 footPosition = GetLemFootPositionForIndex(gridIndex);

        GameObject lem = LemController.CreateLem(footPosition);
        placedLems[gridIndex] = lem;

        // Store original placement data for resetting after Play mode
        LemController controller = lem.GetComponent<LemController>();
        if (controller != null)
        {
            controller.SetFootPointPosition(footPosition);
            controller.SetFrozen(true);
            originalLemPlacements[gridIndex] = new LemPlacementData(gridIndex, controller.GetFacingRight());
        }

        DebugLog.Info($"[LemPlacementManager] Placed Lem at index {gridIndex}");
        return lem;
    }

    /// <summary>
    /// Removes the Lem at the specified grid index.
    /// </summary>
    /// <param name="gridIndex">Grid index of Lem to remove</param>
    public void RemoveLem(int gridIndex)
    {
        if (placedLems.TryGetValue(gridIndex, out GameObject lem))
        {
            Destroy(lem);
            placedLems.Remove(gridIndex);
            originalLemPlacements.Remove(gridIndex);
            DebugLog.Info($"[LemPlacementManager] Removed Lem at index {gridIndex}");
        }
    }

    /// <summary>
    /// Checks if there is a Lem at the specified grid index.
    /// </summary>
    /// <param name="index">Grid index to check</param>
    /// <returns>True if Lem exists at index</returns>
    public bool HasLemAtIndex(int index)
    {
        return placedLems.ContainsKey(index);
    }

    /// <summary>
    /// Gets the Lem at the specified grid index, if any.
    /// </summary>
    /// <param name="index">Grid index</param>
    /// <returns>Lem GameObject or null if none exists</returns>
    public GameObject GetLemAtIndex(int index)
    {
        return placedLems.TryGetValue(index, out GameObject lem) ? lem : null;
    }

    /// <summary>
    /// Gets all placed Lems (typically just one).
    /// </summary>
    /// <returns>Dictionary of grid index to Lem GameObject</returns>
    public Dictionary<int, GameObject> GetAllPlacedLems()
    {
        return new Dictionary<int, GameObject>(placedLems);
    }

    #endregion

    #region Reset and Clear

    /// <summary>
    /// Resets all Lems to their original placement positions and directions.
    /// Called when exiting Play mode to restore Lems to Level Editor state.
    /// </summary>
    public void ResetAllLems()
    {
        // Destroy all current Lems
        foreach (var lem in placedLems.Values)
        {
            if (lem != null)
            {
                Destroy(lem);
            }
        }
        placedLems.Clear();

        // Recreate Lems at original positions
        foreach (var placementData in originalLemPlacements.Values)
        {
            Vector3 footPosition = GetLemFootPositionForIndex(placementData.gridIndex);

            GameObject lem = LemController.CreateLem(footPosition);
            LemController controller = lem.GetComponent<LemController>();
            if (controller != null)
            {
                controller.SetFootPointPosition(footPosition);
                controller.SetFacingRight(placementData.facingRight);
                controller.SetFrozen(true); // Frozen in Designer mode
            }

            placedLems[placementData.gridIndex] = lem;
        }

        DebugLog.Info($"[LemPlacementManager] Reset {originalLemPlacements.Count} Lem(s) to original positions");
    }

    /// <summary>
    /// Clears all Lems from the level.
    /// </summary>
    private void ClearAllLems()
    {
        foreach (var lem in placedLems.Values)
        {
            if (lem != null)
            {
                Destroy(lem);
            }
        }
        placedLems.Clear();
        originalLemPlacements.Clear();
    }

    /// <summary>
    /// Public version of clear - removes all Lems and their placement data.
    /// </summary>
    public void ClearAll()
    {
        ClearAllLems();
        DebugLog.Info("[LemPlacementManager] Cleared all Lems");
    }

    #endregion

    #region Snapshot Management

    /// <summary>
    /// Captures current key states for restoration after Play mode.
    /// </summary>
    /// <param name="keyStates">Key state data from GridManager</param>
    public void CaptureOriginalKeyStates(List<LevelData.KeyStateData> keyStates)
    {
        originalKeyStates = new List<LevelData.KeyStateData>(keyStates);
    }

    /// <summary>
    /// Gets the captured original key states.
    /// </summary>
    /// <returns>List of key state data</returns>
    public List<LevelData.KeyStateData> GetOriginalKeyStates()
    {
        return new List<LevelData.KeyStateData>(originalKeyStates);
    }

    /// <summary>
    /// Captures Play mode snapshot for potential restoration.
    /// </summary>
    /// <param name="snapshot">Level data snapshot</param>
    public void CapturePlayModeSnapshot(LevelData snapshot)
    {
        playModeSnapshot = snapshot;
    }

    /// <summary>
    /// Gets the captured Play mode snapshot.
    /// </summary>
    /// <returns>Level data snapshot or null if none captured</returns>
    public LevelData GetPlayModeSnapshot()
    {
        return playModeSnapshot;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculates the foot position for a Lem at the specified grid index.
    /// Lem foot is at the bottom of the grid cell, slightly forward in Z.
    /// </summary>
    /// <param name="gridIndex">Grid index</param>
    /// <returns>World position for Lem foot</returns>
    private Vector3 GetLemFootPositionForIndex(int gridIndex)
    {
        if (coordinateSystem == null) return Vector3.zero;

        Vector2Int coords = coordinateSystem.IndexToCoordinates(gridIndex);
        const float halfCell = 0.5f; // Grid cells are normalized to 1.0 world unit
        Vector3 gridOrigin = coordinateSystem.GridOrigin;

        return gridOrigin + new Vector3(
            coords.x + halfCell,  // Centered horizontally
            coords.y,             // Bottom of cell
            0.5f                  // Slightly forward in Z
        );
    }

    #endregion

    private void OnDestroy()
    {
        ClearAllLems();
        DebugLog.Info("[LemPlacementManager] Destroyed");
    }
}
