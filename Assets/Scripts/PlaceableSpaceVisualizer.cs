using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages visual borders for placeable and non-placeable grid spaces.
/// Renders on XY plane with Z depth for layering.
/// </summary>
public class PlaceableSpaceVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color placeableColor = BlockColors.PlaceableBorder;
    [Tooltip("Line width for placeable space borders")]
    public float borderLineWidth = RenderingConstants.BORDER_LINE_WIDTH;

    private GridManager gridManager;
    private Dictionary<int, GameObject> placeableBorders = new Dictionary<int, GameObject>();
    private bool isVisible = true;

    private void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (gridManager == null) gridManager = GetComponent<GridManager>();
        if (gridManager != null)
        {
            RefreshVisuals();
        }
    }

    public void RefreshVisuals()
    {
        ClearAllBorders();

        for (int i = 0; i < gridManager.gridWidth * gridManager.gridHeight; i++)
        {
            if (gridManager.IsSpacePlaceable(i))
            {
                CreateBorderAtIndex(i);
            }
        }
    }

    public void UpdateMarkerAtIndex(int index)
    {
        bool isPlaceable = gridManager.IsSpacePlaceable(index);

        if (isPlaceable)
        {
            if (!placeableBorders.ContainsKey(index)) CreateBorderAtIndex(index);
        }
        else
        {
            RemoveBorderAtIndex(index);
        }
    }

    private void CreateBorderAtIndex(int index)
    {
        if (placeableBorders.ContainsKey(index)) return;

        GameObject borderObj = new GameObject($"PlaceableBorder_{index}");
        borderObj.transform.parent = transform;
        borderObj.transform.position = gridManager.IndexToWorldPosition(index);
        borderObj.SetActive(isVisible);

        BorderRenderer border = borderObj.AddComponent<BorderRenderer>();
        border.Initialize(placeableColor, 1f, RenderingConstants.BORDER_DEPTH, RenderingConstants.BORDER_SORTING, borderLineWidth);

        placeableBorders[index] = borderObj;
    }

    public void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        isVisible = visible;
        foreach (var border in placeableBorders.Values)
        {
            if (border != null) border.SetActive(visible);
        }
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
        ClearAllBorders();
    }
}
