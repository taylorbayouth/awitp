using UnityEngine;

/// <summary>
/// Simple text-only control hints shown per mode, plus a Refresh Grid button in Play mode.
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

    [Header("Refresh Button")]
    public float buttonWidth = 140f;
    public float buttonHeight = 32f;
    public float buttonBottomMargin = 12f;

    private GUIStyle textStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        if (builderController == null)
        {
            builderController = ServiceRegistry.Get<BuilderController>();
        }
    }

    private void OnGUI()
    {
        if (builderController == null) return;

        EnsureStyle();

        GameMode mode = builderController.currentMode;
        string[] lines = GetLinesForMode(mode);
        if (lines == null || lines.Length == 0) return;

        float lineHeight = textStyle.lineHeight > 0 ? textStyle.lineHeight : (fontSize + 6f);
        float boxHeight = lineHeight * lines.Length;
        float x = Screen.width - boxWidth - rightMargin;
        float y = Screen.height - boxHeight - bottomMargin;

        Rect rect = new Rect(x, y, boxWidth, boxHeight);
        GUI.Label(rect, string.Join("\n", lines), textStyle);

        // Show Refresh Grid button in Play mode
        if (mode == GameMode.Play)
        {
            float btnX = Screen.width - buttonWidth - rightMargin;
            float btnY = y - buttonHeight - buttonBottomMargin;
            Rect btnRect = new Rect(btnX, btnY, buttonWidth, buttonHeight);

            if (GUI.Button(btnRect, "Refresh Grid (R)", buttonStyle))
            {
                GridManager grid = GridManager.Instance;
                if (grid != null)
                {
                    grid.RefreshGridSettings();
                }
            }
        }
    }

    private void EnsureStyle()
    {
        if (textStyle != null) return;

        textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = fontSize,
            normal = { textColor = Color.black }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter
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
                    "Refresh Grid: R",
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
                    "Refresh Grid: R",
                    "Play: P"
                };
            case GameMode.Play:
                return new[]
                {
                    "Refresh Grid: R",
                    "Exit Play: P"
                };
            default:
                return null;
        }
    }
}
