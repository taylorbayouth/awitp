using UnityEngine;

public class EditorModeManager : MonoBehaviour
{
    [Header("Background Colors")]
    public Color normalModeColor = new Color(0.192f, 0.301f, 0.474f); // Unity default blue-grey
    public Color editorModeColor = new Color(0.3f, 0.2f, 0.1f); // Brown tint for editor mode

    [Header("References")]
    public Camera mainCamera;
    public EditorController editorController;

    private bool wasEditingPlaceableSpaces = false;

    private void Awake()
    {
        Debug.Log("EditorModeManager: Awake called");

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

        // Set camera to use solid color (not skybox)
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = normalModeColor;
            Debug.Log($"EditorModeManager: Camera set to solid color mode. Initial color: {normalModeColor}");
            Debug.Log($"EditorModeManager: Camera clearFlags = {mainCamera.clearFlags}");
        }
        else
        {
            Debug.LogWarning("EditorModeManager: Main camera not found!");
        }

        if (editorController != null)
        {
            Debug.Log("EditorModeManager: Found EditorController");
        }
        else
        {
            Debug.LogWarning("EditorModeManager: EditorController not found!");
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

        // Check if editor mode changed
        if (editorController.isEditingPlaceableSpaces != wasEditingPlaceableSpaces)
        {
            Debug.Log($"EditorModeManager detected change: {wasEditingPlaceableSpaces} -> {editorController.isEditingPlaceableSpaces}");
            wasEditingPlaceableSpaces = editorController.isEditingPlaceableSpaces;
            UpdateBackgroundColor();
        }
    }

    private void UpdateBackgroundColor()
    {
        if (mainCamera == null) return;

        if (editorController.isEditingPlaceableSpaces)
        {
            mainCamera.backgroundColor = editorModeColor;
            Debug.Log($"Background changed to EDITOR MODE color: {editorModeColor}");
        }
        else
        {
            mainCamera.backgroundColor = normalModeColor;
            Debug.Log($"Background changed to NORMAL MODE color: {normalModeColor}");
        }
    }

    public void SetNormalMode()
    {
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = normalModeColor;
            Debug.Log($"*** SetNormalMode called - Background set to: {normalModeColor} ***");
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
            mainCamera.backgroundColor = editorModeColor;
            Debug.Log($"*** SetEditorMode called - Background set to: {editorModeColor} ***");
        }
        else
        {
            Debug.LogError("SetEditorMode: Camera is null!");
        }
    }
}
