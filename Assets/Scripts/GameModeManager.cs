using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    [Header("Background Colors")]
    public Color normalModeColor = new Color(0.192f, 0.301f, 0.474f); // Unity default blue-grey
    public Color designerModeColor = new Color(0.85f, 0.85f, 0.85f); // Light grey for level editor (no clouds)
    public Color playModeColor = new Color(0.1f, 0.1f, 0.1f); // Dark for play mode

    [Header("Skybox")]
    public Material normalModeSkybox;
    public Material playModeSkybox;

    [Header("References")]
    public Camera mainCamera;
    public BuilderController builderController;
    public GameObject skyObject; // Reference to Sky plane/canvas (will auto-find if not set)
    private GridVisualizer gridVisualizer;

    private GameMode previousMode = GameMode.Builder;

    private void Awake()
    {
        // Find references if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("GameModeManager: Camera.main returned null! Looking for any camera...");
                mainCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }
        }

        // Look for BuilderController on same GameObject first
        if (builderController == null)
        {
            builderController = GetComponent<BuilderController>();
        }

        // If still not found, search scene
        if (builderController == null)
        {
            builderController = UnityEngine.Object.FindAnyObjectByType<BuilderController>();
        }

        // builderController may be assigned later in Update if not found here.

        if (normalModeSkybox == null && RenderSettings.skybox != null)
        {
            normalModeSkybox = RenderSettings.skybox;
        }
    }

    private void Start()
    {
        // Auto-find Sky object if not set
        if (skyObject == null)
        {
            skyObject = GameObject.Find("Sky");
            if (skyObject != null)
            {
                DebugLog.Info("GameModeManager: Found Sky object automatically");
            }
        }

        // Set initial background color on startup
        if (builderController != null)
        {
            previousMode = builderController.currentMode;
            UpdateBackgroundColor();
        }
    }

    private void Update()
    {
        if (builderController == null)
        {
            // Try to find it if not found
            builderController = UnityEngine.Object.FindAnyObjectByType<BuilderController>();
            return;
        }

        // Check if game mode changed
        if (builderController.currentMode != previousMode)
        {
            DebugLog.Info($"GameModeManager detected mode change: {previousMode} -> {builderController.currentMode}");
            previousMode = builderController.currentMode;
            UpdateBackgroundColor();
        }
    }

    private void UpdateBackgroundColor()
    {
        if (mainCamera == null) return;

        bool isDesignerMode = (builderController.currentMode == GameMode.Builder ||
                               builderController.currentMode == GameMode.Designer);

        // Both Editor and LevelBuilder modes use light grey background (designer mode)
        if (isDesignerMode)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = designerModeColor;
            DebugLog.Info($"Background changed to DESIGNER MODE (light grey, no clouds): {designerModeColor}");
        }
        else if (builderController.currentMode == GameMode.Play)
        {
            if (playModeSkybox != null)
            {
                RenderSettings.skybox = playModeSkybox;
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
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            DebugLog.Info($"Background changed to NORMAL MODE color: {normalModeColor}");
        }

        // Hide Sky object in designer mode, show in play mode
        SetSkyVisible(!isDesignerMode);

        SetGridVisible(builderController.currentMode != GameMode.Play);
    }

    public void SetNormalMode()
    {
        if (mainCamera != null)
        {
            if (normalModeSkybox != null)
            {
                RenderSettings.skybox = normalModeSkybox;
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
            mainCamera.backgroundColor = designerModeColor;
            DebugLog.Info($"*** SetEditorMode called - Background set to: {designerModeColor} ***");
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
            gridVisualizer = UnityEngine.Object.FindAnyObjectByType<GridVisualizer>();
        }

        if (gridVisualizer == null) return;

        gridVisualizer.SetGridVisible(visible);
    }

    private void SetSkyVisible(bool visible)
    {
        if (skyObject == null)
        {
            skyObject = GameObject.Find("Sky");
        }

        if (skyObject != null)
        {
            skyObject.SetActive(visible);
            DebugLog.Info($"Sky visibility set to: {visible}");
        }
    }
}
