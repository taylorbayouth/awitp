using UnityEngine;

/// <summary>
/// Ensures all game systems are properly initialized in the correct order.
/// Add this to any GameObject in the scene to auto-setup everything.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Auto-Setup")]
    public bool autoSetupOnStart = true;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupGame();
        }
    }

    public void SetupGame()
    {
        Debug.Log("=== Game Initializer: Starting Setup ===");

        // Ensure GridManager exists
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GameInitializer: GridManager not found in scene!");
            return;
        }
        else
        {
            Debug.Log("GameInitializer: GridManager found");
        }

        // Ensure GridManager has required components IN ORDER
        if (gridManager.GetComponent<GridVisualizer>() == null)
        {
            gridManager.gameObject.AddComponent<GridVisualizer>();
            Debug.Log("GameInitializer: Added GridVisualizer");
        }

        if (gridManager.GetComponent<PlaceableSpaceVisualizer>() == null)
        {
            gridManager.gameObject.AddComponent<PlaceableSpaceVisualizer>();
            Debug.Log("GameInitializer: Added PlaceableSpaceVisualizer");
        }

        if (gridManager.GetComponent<BlockInventory>() == null)
        {
            gridManager.gameObject.AddComponent<BlockInventory>();
            Debug.Log("GameInitializer: Added BlockInventory");
        }

        // IMPORTANT: Add EditorModeManager BEFORE EditorController
        // so EditorController can find it in Awake
        if (gridManager.GetComponent<EditorModeManager>() == null)
        {
            gridManager.gameObject.AddComponent<EditorModeManager>();
            Debug.Log("GameInitializer: Added EditorModeManager");
        }

        if (gridManager.GetComponent<EditorController>() == null)
        {
            gridManager.gameObject.AddComponent<EditorController>();
            Debug.Log("GameInitializer: Added EditorController");
        }

        // Verify they're both present
        EditorController ec = gridManager.GetComponent<EditorController>();
        EditorModeManager emm = gridManager.GetComponent<EditorModeManager>();
        Debug.Log($"GameInitializer: EditorController present: {ec != null}, EditorModeManager present: {emm != null}");

        // Setup Inventory UI
        if (FindObjectOfType<InventoryUI>() == null)
        {
            GameObject uiObj = new GameObject("InventoryUI");
            InventoryUI inventoryUI = uiObj.AddComponent<InventoryUI>();
            inventoryUI.inventory = gridManager.GetComponent<BlockInventory>();
            inventoryUI.editorController = ec;
            Debug.Log("GameInitializer: Added InventoryUI");
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
                Debug.Log("GameInitializer: Added CameraSetup to Main Camera");
            }

            // Set basic camera properties (CameraSetup will handle positioning)
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.orthographic = true;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;

            Debug.Log("GameInitializer: Camera setup - CameraSetup component will handle positioning");
        }
        else
        {
            Debug.LogWarning("GameInitializer: Main Camera not found!");
        }

        // Force refresh of all visualizations
        Invoke(nameof(DelayedRefresh), 0.1f);

        Debug.Log("=== Game Initializer: Setup Complete ===");
    }

    private void DelayedRefresh()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.RefreshGrid();
            Debug.Log("GameInitializer: Performed delayed refresh");
        }
    }
}
