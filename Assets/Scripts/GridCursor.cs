using UnityEngine;

/// <summary>
/// Visual cursor that changes color based on space state.
/// Renders on XY plane with Z depth for layering.
/// </summary>
public class GridCursor : MonoBehaviour
{
    public enum CursorState
    {
        Placeable,
        Editable,
        NonPlaceable
    }

    [Header("Cursor Colors")]
    [Tooltip("Blue - shown when hovering over empty space where you can place a new block")]
    public Color placeableColor = BlockColors.CursorPlaceable;

    [Tooltip("Green - shown when hovering over an existing block that can be edited/moved")]
    public Color editableColor = BlockColors.CursorEditable;

    [Tooltip("Red - shown when hovering over a space that's blocked or invalid for placement")]
    public Color nonPlaceableColor = BlockColors.CursorNonPlaceable;

    private BorderRenderer borderRenderer;
    private CursorState currentState = CursorState.Placeable;

    private void Awake()
    {
        SetupCursor();
    }

    private void SetupCursor()
    {
        borderRenderer = gameObject.AddComponent<BorderRenderer>();
        UpdateColor();
    }

    public void Initialize()
    {
        if (borderRenderer == null) SetupCursor();

        const float cellSize = 1f; // Grid cells are normalized to 1.0 world unit
        Color currentColor = GetColorForState(currentState);
        borderRenderer.Initialize(currentColor, cellSize, RenderingConstants.CURSOR_DEPTH, RenderingConstants.CURSOR_SORTING, RenderingConstants.CURSOR_LINE_WIDTH);
    }

    public void SetState(CursorState state)
    {
        currentState = state;
        UpdateColor();
    }

    public CursorState GetState() => currentState;

    private Color GetColorForState(CursorState state)
    {
        return state switch
        {
            CursorState.Placeable => placeableColor,
            CursorState.Editable => editableColor,
            CursorState.NonPlaceable => nonPlaceableColor,
            _ => Color.white
        };
    }

    private void UpdateColor()
    {
        if (borderRenderer == null) return;
        borderRenderer.SetColor(GetColorForState(currentState));
    }

    public void SetVisible(bool visible)
    {
        if (borderRenderer != null) borderRenderer.SetVisible(visible);
    }
}
