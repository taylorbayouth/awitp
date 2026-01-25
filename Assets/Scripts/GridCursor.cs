using UnityEngine;

/// <summary>
/// Visual cursor that follows mouse input and changes color based on space state.
/// Rendered at the highest layer to always be visible above other elements.
/// </summary>
public class GridCursor : MonoBehaviour
{
    public enum CursorState
    {
        Placeable,      // White - can place blocks here
        Editable,       // Green - can toggle placeable state
        NonPlaceable    // Grey - cannot place blocks here
    }

    [Header("Cursor Colors")]
    public Color placeableColor = BlockColors.CursorPlaceable;
    public Color editableColor = BlockColors.CursorEditable;
    public Color nonPlaceableColor = BlockColors.CursorNonPlaceable;

    [Header("Cursor Settings")]
    public float lineWidth = RenderingConstants.CURSOR_LINE_WIDTH;
    public float heightOffset = RenderingConstants.CURSOR_HEIGHT;

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

    public void Initialize(float cellSize)
    {
        if (borderRenderer == null) SetupCursor();

        Color currentColor = GetColorForState(currentState);
        borderRenderer.Initialize(currentColor, cellSize, heightOffset, RenderingConstants.CURSOR_SORTING);
    }

    public void SetState(CursorState state)
    {
        currentState = state;
        UpdateColor();
    }

    public CursorState GetState()
    {
        return currentState;
    }

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
