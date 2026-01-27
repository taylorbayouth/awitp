using UnityEngine;

public class EditorModeManager : MonoBehaviour
{
    [Header("Background Colors")]
    public Color normalModeColor = new Color(0.192f, 0.301f, 0.474f); // Unity default blue-grey
    public Color editorModeColor = new Color(0.211f, 0.211f, 0.211f); // #363636 for level editor
    public Color playModeColor = new Color(0.1f, 0.1f, 0.1f); // Dark for play mode

    [Header("Skybox")]
    public Material normalModeSkybox;
    public Material playModeSkybox;

    [Header("References")]
    public Camera mainCamera;
    public EditorController editorController;
    private GridVisualizer gridVisualizer;

    private GameMode previousMode = GameMode.Editor;

    private void Awake()
    {
        // Find references if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("EditorModeManager: Camera.main returned null! Looking for any camera...");
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        // Look for EditorController on same GameObject first
        if (editorController == null)
        {
            editorController = GetComponent<EditorController>();
        }

        // If still not found, search scene
        if (editorController == null)
        {
            editorController = FindObjectOfType<EditorController>();
        }

        // editorController may be assigned later in Update if not found here.

        if (normalModeSkybox == null && RenderSettings.skybox != null)
        {
            normalModeSkybox = RenderSettings.skybox;
        }
    }

    private void Update()
    {
        if (editorController == null)
        {
            // Try to find it if not found
            editorController = FindObjectOfType<EditorController>();
            return;
        }

        // Check if game mode changed
        if (editorController.currentMode != previousMode)
        {
            DebugLog.Info($"EditorModeManager detected mode change: {previousMode} -> {editorController.currentMode}");
            previousMode = editorController.currentMode;
            UpdateBackgroundColor();
        }
    }

    private void UpdateBackgroundColor()
    {
        if (mainCamera == null) return;

        if (editorController.currentMode == GameMode.LevelEditor)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = editorModeColor;
            DebugLog.Info($"Background changed to LEVEL EDITOR MODE color: {editorModeColor}");
        }
        else if (editorController.currentMode == GameMode.Play)
        {
            if (playModeSkybox != null)
            {
                RenderSettings.skybox = playModeSkybox;
            }
            else
            {
                Debug.LogWarning("EditorModeManager: Play Mode Skybox not assigned. Using current skybox.");
            }
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            DebugLog.Info($"Background changed to PLAY MODE color: {playModeColor}");
        }
        else
        {
            if (normalModeSkybox != null)
            {
                RenderSettings.skybox = normalModeSkybox;
            }
            else
            {
                Debug.LogWarning("EditorModeManager: Normal Mode Skybox not assigned. Using current skybox.");
            }
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            DebugLog.Info($"Background changed to NORMAL MODE color: {normalModeColor}");
        }

        SetGridVisible(editorController.currentMode != GameMode.Play);
    }

    public void SetNormalMode()
    {
        if (mainCamera != null)
        {
            if (normalModeSkybox != null)
            {
                RenderSettings.skybox = normalModeSkybox;
            }
            else
            {
                Debug.LogWarning("EditorModeManager: Normal Mode Skybox not assigned. Using current skybox.");
            }
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            DebugLog.Info($"*** SetNormalMode called - Background set to: {normalModeColor} ***");
        }
        else
        {
            Debug.LogError("SetNormalMode: Camera is null!");
        }
    }

    public void SetEditorMode()
    {
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = editorModeColor;
            DebugLog.Info($"*** SetEditorMode called - Background set to: {editorModeColor} ***");
        }
        else
        {
            Debug.LogError("SetEditorMode: Camera is null!");
        }
    }

    private void SetGridVisible(bool visible)
    {
        if (gridVisualizer == null)
        {
            gridVisualizer = FindObjectOfType<GridVisualizer>();
        }

        if (gridVisualizer == null) return;

        gridVisualizer.SetGridVisible(visible);
    }
}
