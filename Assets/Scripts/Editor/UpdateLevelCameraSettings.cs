using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility to update camera settings in all LevelDefinition assets.
/// </summary>
public class UpdateLevelCameraSettings : EditorWindow
{
    private float cameraDistance = 150f;
    private float gridMargin = 1f;
    private float nearClipPlane = 0.24f;
    private float farClipPlane = 500f;

    private bool updateCameraDistance = true;
    private bool updateGridMargin = false;
    private bool updateNearClipPlane = false;
    private bool updateFarClipPlane = false;

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
            "Simple framing system:\n" +
            "- Fixed camera distance (far away for flat perspective)\n" +
            "- Auto-calculated FOV to fit any grid size\n" +
            "- Camera always points at grid center",
            MessageType.Info);

        EditorGUILayout.Space();

        updateCameraDistance = EditorGUILayout.Toggle("Update Camera Distance", updateCameraDistance);
        using (new EditorGUI.DisabledScope(!updateCameraDistance))
        {
            cameraDistance = EditorGUILayout.Slider("  Camera Distance", cameraDistance, 1f, 2000f);
        }

        updateGridMargin = EditorGUILayout.Toggle("Update Grid Margin", updateGridMargin);
        using (new EditorGUI.DisabledScope(!updateGridMargin))
        {
            gridMargin = EditorGUILayout.Slider("  Grid Margin", gridMargin, 0f, 5f);
        }

        updateNearClipPlane = EditorGUILayout.Toggle("Update Near Clip", updateNearClipPlane);
        using (new EditorGUI.DisabledScope(!updateNearClipPlane))
        {
            nearClipPlane = EditorGUILayout.Slider("  Near Clip", nearClipPlane, 0.01f, 10f);
        }

        updateFarClipPlane = EditorGUILayout.Toggle("Update Far Clip", updateFarClipPlane);
        using (new EditorGUI.DisabledScope(!updateFarClipPlane))
        {
            farClipPlane = EditorGUILayout.Slider("  Far Clip", farClipPlane, 100f, 5000f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

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
        if (updateCameraDistance) changes += $"• Camera Distance → {cameraDistance:F1}\n";
        if (updateGridMargin) changes += $"• Grid Margin → {gridMargin:F2}\n";
        if (updateNearClipPlane) changes += $"• Near Clip → {nearClipPlane:F2}\n";
        if (updateFarClipPlane) changes += $"• Far Clip → {farClipPlane:F0}\n";

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
            LevelData levelData = level.GetLevelData();
            if (levelData.cameraSettings == null)
                levelData.cameraSettings = new LevelData.CameraSettings();

            if (updateCameraDistance) levelData.cameraSettings.cameraDistance = cameraDistance;
            if (updateGridMargin) levelData.cameraSettings.gridMargin = gridMargin;
            if (updateNearClipPlane) levelData.cameraSettings.nearClipPlane = nearClipPlane;
            if (updateFarClipPlane) levelData.cameraSettings.farClipPlane = farClipPlane;

            level.SetLevelData(levelData);
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
        CameraSetup cameraSetup = ServiceRegistry.Get<CameraSetup>();
        if (cameraSetup == null)
        {
            EditorUtility.DisplayDialog("Not Found", "No CameraSetup in scene.", "OK");
            return;
        }

        Undo.RecordObject(cameraSetup, "Update Camera Settings");

        if (updateCameraDistance) cameraSetup.cameraDistance = cameraDistance;
        if (updateGridMargin) cameraSetup.gridMargin = gridMargin;
        if (updateNearClipPlane) cameraSetup.nearClipPlane = nearClipPlane;
        if (updateFarClipPlane) cameraSetup.farClipPlane = farClipPlane;

        cameraSetup.RefreshCamera();
        EditorUtility.SetDirty(cameraSetup);

        Debug.Log($"Updated scene camera: Dist={cameraSetup.cameraDistance:F1}, Margin={cameraSetup.gridMargin:F2}");
    }
}
