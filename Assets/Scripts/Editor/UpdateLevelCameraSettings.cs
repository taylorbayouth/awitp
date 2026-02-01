using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility to update camera settings in all LevelDefinition assets.
/// </summary>
public class UpdateLevelCameraSettings : EditorWindow
{
    // Position
    private float verticalOffset = 10.4f;
    private float horizontalOffset = 0f;
    private bool updateVerticalOffset = false;
    private bool updateHorizontalOffset = false;

    // Rotation
    private float tiltAngle = 3.7f;
    private float panAngle = 0f;
    private bool updateTiltAngle = false;
    private bool updatePanAngle = false;

    // Perspective
    private float focalLength = 756f;
    private float distanceMultiplier = 23.7f;
    private bool updateFocalLength = true;
    private bool updateDistanceMultiplier = true;

    [MenuItem("Tools/Update Level Camera Settings")]
    public static void ShowWindow()
    {
        GetWindow<UpdateLevelCameraSettings>("Update Camera Settings");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Update Camera Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Update camera settings across all levels.\n\n" +
            "Camera starts centered with grid. Use offsets to move it, tilt to angle it down.",
            MessageType.Info);

        EditorGUILayout.Space();

        // === POSITION ===
        EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);

        updateVerticalOffset = EditorGUILayout.Toggle("Update Vertical Offset", updateVerticalOffset);
        using (new EditorGUI.DisabledScope(!updateVerticalOffset))
        {
            verticalOffset = EditorGUILayout.Slider("  Vertical Offset", verticalOffset, 0f, 28f);
        }

        updateHorizontalOffset = EditorGUILayout.Toggle("Update Horizontal Offset", updateHorizontalOffset);
        using (new EditorGUI.DisabledScope(!updateHorizontalOffset))
        {
            horizontalOffset = EditorGUILayout.Slider("  Horizontal Offset", horizontalOffset, -10f, 10f);
        }

        EditorGUILayout.Space();

        // === ROTATION ===
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);

        updateTiltAngle = EditorGUILayout.Toggle("Update Tilt Angle", updateTiltAngle);
        using (new EditorGUI.DisabledScope(!updateTiltAngle))
        {
            tiltAngle = EditorGUILayout.Slider("  Tilt (pitch)", tiltAngle, -5f, 20f);
            EditorGUILayout.HelpBox("Negative = look down, Positive = look up", MessageType.None);
        }

        updatePanAngle = EditorGUILayout.Toggle("Update Pan Angle", updatePanAngle);
        using (new EditorGUI.DisabledScope(!updatePanAngle))
        {
            panAngle = EditorGUILayout.Slider("  Pan (yaw)", panAngle, -15f, 15f);
        }

        EditorGUILayout.Space();

        // === PERSPECTIVE ===
        EditorGUILayout.LabelField("Perspective", EditorStyles.boldLabel);

        updateFocalLength = EditorGUILayout.Toggle("Update Focal Length", updateFocalLength);
        using (new EditorGUI.DisabledScope(!updateFocalLength))
        {
            focalLength = EditorGUILayout.Slider("  Focal Length (mm)", focalLength, 100f, 1200f);
            EditorGUILayout.HelpBox("756mm = default. Higher = more telephoto (flatter)", MessageType.None);
        }

        updateDistanceMultiplier = EditorGUILayout.Toggle("Update Distance Multiplier", updateDistanceMultiplier);
        using (new EditorGUI.DisabledScope(!updateDistanceMultiplier))
        {
            distanceMultiplier = EditorGUILayout.Slider("  Distance Multiplier", distanceMultiplier, 5f, 40f);
            EditorGUILayout.HelpBox("Higher = farther away = flatter perspective", MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // === BUTTONS ===
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Update Scene Camera", GUILayout.Height(35)))
        {
            UpdateSceneCamera();
        }
        if (GUILayout.Button("Update All Levels", GUILayout.Height(35)))
        {
            UpdateAllLevels();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void UpdateAllLevels()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
        List<LevelDefinition> levels = new List<LevelDefinition>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level != null)
            {
                levels.Add(level);
            }
        }

        if (levels.Count == 0)
        {
            EditorUtility.DisplayDialog("No Levels Found", "No LevelDefinition assets found.", "OK");
            return;
        }

        string changes = "";
        if (updateVerticalOffset) changes += $"• Vertical Offset → {verticalOffset:F1}\n";
        if (updateHorizontalOffset) changes += $"• Horizontal Offset → {horizontalOffset:F1}\n";
        if (updateTiltAngle) changes += $"• Tilt Angle → {tiltAngle:F1}°\n";
        if (updatePanAngle) changes += $"• Pan Angle → {panAngle:F1}°\n";
        if (updateFocalLength) changes += $"• Focal Length → {focalLength:F0}mm\n";
        if (updateDistanceMultiplier) changes += $"• Distance Multiplier → {distanceMultiplier:F1}x\n";

        if (string.IsNullOrEmpty(changes))
        {
            EditorUtility.DisplayDialog("No Changes Selected", "Select at least one setting to update.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog(
            "Update Camera Settings",
            $"Update {levels.Count} level(s):\n\n{changes}",
            "Update All",
            "Cancel");

        if (!proceed) return;

        int updated = 0;
        foreach (LevelDefinition level in levels)
        {
            if (UpdateLevelDefinition(level))
                updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete", $"Updated {updated} level(s).", "OK");
    }

    private bool UpdateLevelDefinition(LevelDefinition level)
    {
        try
        {
            LevelData levelData = level.ToLevelData();
            if (levelData == null) return false;

            if (levelData.cameraSettings == null)
                levelData.cameraSettings = new LevelData.CameraSettings();

            if (updateVerticalOffset) levelData.cameraSettings.verticalOffset = verticalOffset;
            if (updateHorizontalOffset) levelData.cameraSettings.horizontalOffset = horizontalOffset;
            if (updateTiltAngle) levelData.cameraSettings.tiltAngle = tiltAngle;
            if (updatePanAngle) levelData.cameraSettings.panAngle = panAngle;
            if (updateFocalLength)
            {
                // Convert focal length to FOV and store both
                const float sensorHeight = 24f;
                float fov = 2f * Mathf.Atan(sensorHeight / (2f * focalLength)) * Mathf.Rad2Deg;
                levelData.cameraSettings.fieldOfView = fov;
                levelData.cameraSettings.tiltOffset = focalLength;  // Store focal length here
            }
            if (updateDistanceMultiplier) levelData.cameraSettings.distanceMultiplier = distanceMultiplier;

            level.FromLevelData(levelData);
            EditorUtility.SetDirty(level);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating {level.levelName}: {e.Message}");
            return false;
        }
    }

    private void UpdateSceneCamera()
    {
        CameraSetup cameraSetup = UnityEngine.Object.FindAnyObjectByType<CameraSetup>();
        if (cameraSetup == null)
        {
            EditorUtility.DisplayDialog("Not Found", "No CameraSetup in scene.", "OK");
            return;
        }

        Undo.RecordObject(cameraSetup, "Update Camera Settings");

        if (updateVerticalOffset) cameraSetup.verticalOffset = verticalOffset;
        if (updateHorizontalOffset) cameraSetup.horizontalOffset = horizontalOffset;
        if (updateTiltAngle) cameraSetup.tiltAngle = tiltAngle;
        if (updatePanAngle) cameraSetup.panAngle = panAngle;
        if (updateFocalLength) cameraSetup.focalLength = focalLength;
        if (updateDistanceMultiplier) cameraSetup.distanceMultiplier = distanceMultiplier;

        cameraSetup.RefreshCamera();
        EditorUtility.SetDirty(cameraSetup);

        Debug.Log($"Updated scene camera: V={cameraSetup.verticalOffset}, Tilt={cameraSetup.tiltAngle}°, Focal={cameraSetup.focalLength}mm");
    }
}
