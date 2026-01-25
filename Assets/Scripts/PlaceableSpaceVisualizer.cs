using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages visual borders for placeable and non-placeable grid spaces.
/// Placeable spaces get black borders, non-placeable spaces get grey borders.
/// </summary>
public class PlaceableSpaceVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color nonPlaceableColor = BlockColors.NonPlaceableBorder;
    public Color placeableColor = BlockColors.PlaceableBorder;
    public float borderHeight = RenderingConstants.BORDER_HEIGHT;
    public float lineWidth = RenderingConstants.BORDER_LINE_WIDTH;

    private GridManager gridManager;
    private Dictionary<int, GameObject> nonPlaceableMarkers = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> placeableBorders = new Dictionary<int, GameObject>();

    private void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    public void RefreshVisuals()
    {
        // Clear existing markers
        ClearAllMarkers();
        ClearAllBorders();

        int markerCount = 0;
        int borderCount = 0;

        // Create markers for all non-placeable spaces and borders for placeable spaces
        for (int i = 0; i < gridManager.gridWidth * gridManager.gridHeight; i++)
        {
            if (!gridManager.IsSpacePlaceable(i))
            {
                CreateMarkerAtIndex(i);
                markerCount++;
            }
            else
            {
                CreateBorderAtIndex(i);
                borderCount++;
            }
        }

        Debug.Log($"PlaceableSpaceVisualizer: Created {markerCount} non-placeable markers and {borderCount} placeable borders");
    }

    public void UpdateMarkerAtIndex(int index)
    {
        bool isPlaceable = gridManager.IsSpacePlaceable(index);

        if (isPlaceable)
        {
            RemoveMarkerAtIndex(index);
            if (!placeableBorders.ContainsKey(index)) CreateBorderAtIndex(index);
        }
        else
        {
            RemoveBorderAtIndex(index);
            if (!nonPlaceableMarkers.ContainsKey(index)) CreateMarkerAtIndex(index);
        }
    }

    private void CreateMarkerAtIndex(int index)
    {
        if (nonPlaceableMarkers.ContainsKey(index)) return;

        GameObject borderObj = new GameObject($"NonPlaceableBorder_{index}");
        borderObj.transform.parent = transform;
        borderObj.transform.position = gridManager.IndexToWorldPosition(index);

        BorderRenderer border = borderObj.AddComponent<BorderRenderer>();
        border.Initialize(nonPlaceableColor, gridManager.cellSize, borderHeight, RenderingConstants.BORDER_SORTING);

        nonPlaceableMarkers[index] = borderObj;
    }

    private void RemoveMarkerAtIndex(int index)
    {
        if (nonPlaceableMarkers.TryGetValue(index, out GameObject marker))
        {
            Destroy(marker);
            nonPlaceableMarkers.Remove(index);
        }
    }

    private void ClearAllMarkers()
    {
        foreach (var marker in nonPlaceableMarkers.Values)
        {
            if (marker != null) Destroy(marker);
        }
        nonPlaceableMarkers.Clear();
    }

    private void CreateBorderAtIndex(int index)
    {
        if (placeableBorders.ContainsKey(index)) return;

        GameObject borderObj = new GameObject($"PlaceableBorder_{index}");
        borderObj.transform.parent = transform;
        borderObj.transform.position = gridManager.IndexToWorldPosition(index);

        BorderRenderer border = borderObj.AddComponent<BorderRenderer>();
        border.Initialize(placeableColor, gridManager.cellSize, borderHeight, RenderingConstants.BORDER_SORTING);

        placeableBorders[index] = borderObj;
    }

    private void RemoveBorderAtIndex(int index)
    {
        if (placeableBorders.TryGetValue(index, out GameObject border))
        {
            Destroy(border);
            placeableBorders.Remove(index);
        }
    }

    private void ClearAllBorders()
    {
        foreach (var border in placeableBorders.Values)
        {
            if (border != null) Destroy(border);
        }
        placeableBorders.Clear();
    }

    private void OnDestroy()
    {
        ClearAllMarkers();
        ClearAllBorders();
    }
}
