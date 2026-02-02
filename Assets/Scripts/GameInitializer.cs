using UnityEngine;

/// <summary>
/// Ensures all game systems are properly initialized in the correct order.
/// Add this to any GameObject in the scene to auto-setup everything.
/// </summary>
[ExecuteAlways]
public class GameInitializer : MonoBehaviour
{
    [Header("Auto-Setup")]
    public bool autoSetupOnStart = true;

    private void Start()
    {
        if (autoSetupOnStart && Application.isPlaying)
        {
            SetupGame();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureEditorInventorySetup();
        }
    }

    private void EnsureEditorInventorySetup()
    {
        GridManager gridManager = ServiceRegistry.Get<GridManager>();
        if (gridManager == null) return;

        if (gridManager.GetComponent<BlockInventory>() == null)
        {
            gridManager.gameObject.AddComponent<BlockInventory>();
        }

        InventoryUI inventoryUI = ServiceRegistry.Get<InventoryUI>();
        if (inventoryUI == null)
        {
            GameObject uiObj = new GameObject("InventoryUI");
            inventoryUI = uiObj.AddComponent<InventoryUI>();
        }

        if (inventoryUI != null)
        {
            inventoryUI.inventory = gridManager.GetComponent<BlockInventory>();
        }
    }

    public void SetupGame()
    {
        DebugLog.Info("=== Game Initializer: Starting Setup ===");

        // Ensure GridManager exists
        GridManager gridManager = ServiceRegistry.Get<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GameInitializer: GridManager not found in scene!");
            return;
        }
        else
        {
            DebugLog.Info("GameInitializer: GridManager found");
        }

        // Ensure GridManager has required components IN ORDER
        if (gridManager.GetComponent<GridVisualizer>() == null)
        {
            gridManager.gameObject.AddComponent<GridVisualizer>();
            DebugLog.Info("GameInitializer: Added GridVisualizer");
        }

        if (gridManager.GetComponent<PlaceableSpaceVisualizer>() == null)
        {
            gridManager.gameObject.AddComponent<PlaceableSpaceVisualizer>();
            DebugLog.Info("GameInitializer: Added PlaceableSpaceVisualizer");
        }

        if (gridManager.GetComponent<BlockInventory>() == null)
        {
            gridManager.gameObject.AddComponent<BlockInventory>();
            DebugLog.Info("GameInitializer: Added BlockInventory");
        }

        // IMPORTANT: Add GameModeManager BEFORE BuilderController
        // so BuilderController can find it in Awake
        if (gridManager.GetComponent<GameModeManager>() == null)
        {
            gridManager.gameObject.AddComponent<GameModeManager>();
        }

        if (gridManager.GetComponent<BuilderController>() == null)
        {
            gridManager.gameObject.AddComponent<BuilderController>();
            DebugLog.Info("GameInitializer: Added BuilderController");
        }

        if (gridManager.GetComponent<ControlsUI>() == null)
        {
            gridManager.gameObject.AddComponent<ControlsUI>();
            DebugLog.Info("GameInitializer: Added ControlsUI");
        }

        // Verify they're both present
        BuilderController ec = gridManager.GetComponent<BuilderController>();
        GameModeManager emm = gridManager.GetComponent<GameModeManager>();
        DebugLog.Info($"GameInitializer: BuilderController present: {ec != null}, GameModeManager present: {emm != null}");

        // Setup Inventory UI
        if (ServiceRegistry.Get<InventoryUI>() == null)
        {
            GameObject uiObj = new GameObject("InventoryUI");
            InventoryUI inventoryUI = uiObj.AddComponent<InventoryUI>();
            inventoryUI.inventory = gridManager.GetComponent<BlockInventory>();
            inventoryUI.builderController = ec;
            DebugLog.Info("GameInitializer: Added InventoryUI");
        }

        // Setup camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            CameraSetup cameraSetup = mainCamera.GetComponent<CameraSetup>();
            if (cameraSetup == null)
            {
                cameraSetup = mainCamera.gameObject.AddComponent<CameraSetup>();
                cameraSetup.gridManager = gridManager;
                DebugLog.Info("GameInitializer: Added CameraSetup to Main Camera");
            }

            // Set basic camera properties (CameraSetup will handle positioning)
            // mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.orthographic = true;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;

            DebugLog.Info("GameInitializer: Camera setup - CameraSetup component will handle positioning");
        }
        else
        {
            Debug.LogWarning("GameInitializer: Main Camera not found!");
        }

        // Setup lighting for non-metallic look
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f); // Bright flat lighting

        // Ensure there's a directional light
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool hasDirectionalLight = false;
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasDirectionalLight = true;
                // Make sure it's bright and white
                light.color = Color.white;
                light.intensity = 1f;
                break;
            }
        }

        if (!hasDirectionalLight)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            DebugLog.Info("GameInitializer: Created directional light");
        }

        // Force refresh of all visualizations
        Invoke(nameof(DelayedRefresh), 0.1f);

        // Check for pending level to load from UI navigation
        Invoke(nameof(LoadPendingLevel), 0.2f);

        DebugLog.Info("=== Game Initializer: Setup Complete ===");
    }

    /// <summary>
    /// Loads a level that was requested from the UI (WorldMap/MainMenu).
    /// Checks PlayerPrefs for a pending level ID.
    /// </summary>
    private void LoadPendingLevel()
    {
        string pendingLevelId = PlayerPrefs.GetString("PendingLevelId", "");

        if (!string.IsNullOrEmpty(pendingLevelId))
        {
            // Clear the pending level so it doesn't reload on scene restart
            PlayerPrefs.DeleteKey("PendingLevelId");
            PlayerPrefs.Save();

            DebugLog.Info($"GameInitializer: Loading pending level: {pendingLevelId}");

            // Try to load via LevelManager
            if (LevelManager.Instance != null)
            {
                if (LevelManager.Instance.LoadLevel(pendingLevelId))
                {
                    DebugLog.Info($"GameInitializer: Successfully loaded level: {pendingLevelId}");
                }
                else
                {
                    Debug.LogWarning($"GameInitializer: Failed to load level: {pendingLevelId}");
                }
            }
            else
            {
                Debug.LogWarning("GameInitializer: LevelManager not available to load level");
            }
        }
    }

    private void DelayedRefresh()
    {
        GridManager gridManager = ServiceRegistry.Get<GridManager>();
        if (gridManager != null)
        {
            gridManager.RefreshGrid();
            DebugLog.Info("GameInitializer: Performed delayed refresh");
        }
    }
}
