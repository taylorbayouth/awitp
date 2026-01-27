using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelLoader))]
public class LevelLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("levelDefinition"));
        serializedObject.ApplyModifiedProperties();

        LevelLoader loader = (LevelLoader)target;

        if (loader.levelDefinition == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a LevelDefinition to load this level on Start.\n" +
                "The level's inventory will be loaded from the LevelDefinition's JSON data.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Level: {loader.levelDefinition.levelName}\n" +
                $"World: {loader.levelDefinition.worldId}\n" +
                "Inventory will be loaded from the LevelDefinition's JSON data.",
                MessageType.Info
            );
        }
    }
}
