using UnityEngine;

/// <summary>
/// Simple text-only control hints shown per mode.
/// </summary>
public class ControlsUI : MonoBehaviour
{
    [Header("References")]
    public BuilderController builderController;

    [Header("Layout")]
    public float rightMargin = 24f;
    public float bottomMargin = 24f;
    public float boxWidth = 420f;
    public int fontSize = 18;

    private GUIStyle textStyle;

    private void Awake()
    {
        if (builderController == null)
        {
            builderController = UnityEngine.Object.FindAnyObjectByType<BuilderController>();
        }
    }

    private void OnGUI()
    {
        if (builderController == null) return;

        EnsureStyle();

        string[] lines = GetLinesForMode(builderController.currentMode);
        if (lines == null || lines.Length == 0) return;

        float lineHeight = textStyle.lineHeight > 0 ? textStyle.lineHeight : (fontSize + 6f);
        float boxHeight = lineHeight * lines.Length;
        float x = Screen.width - boxWidth - rightMargin;
        float y = Screen.height - boxHeight - bottomMargin;

        Rect rect = new Rect(x, y, boxWidth, boxHeight);
        GUI.Label(rect, string.Join("\n", lines), textStyle);
    }

    private void EnsureStyle()
    {
        if (textStyle != null) return;

        textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = fontSize,
            normal = { textColor = Color.white }
        };
    }

    private string[] GetLinesForMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Builder:
                return new[]
                {
                    "Move: Arrow / WASD",
                    "Place Block: Space / Enter",
                    "Remove: Delete / Backspace",
                    "Select Block: 1-9 / [ ]",
                    "Level Editor: E",
                    "Play: P"
                };
            case GameMode.Designer:
                return new[]
                {
                    "Move: Arrow / WASD",
                    "Toggle Placeable: Space / Enter",
                    "Place Permanent Block: B",
                    "Place Lem: L",
                    "Remove: Delete / Backspace",
                    "Select Block: 1-9 / [ ]",
                    "Editor: E",
                    "Play: P"
                };
            case GameMode.Play:
                return new[]
                {
                    "Exit Play: P"
                };
            default:
                return null;
        }
    }
}
