using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrickCubeGenerator))]
public sealed class BrickCubeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var packingModeProp = serializedObject.FindProperty("packingMode");
        if (packingModeProp != null && packingModeProp.propertyType == SerializedPropertyType.Enum)
        {
            // 0 = RelaxedGrid, 1 = TiledLayers (see BrickCubeGenerator.PackingMode)
            if (packingModeProp.enumValueIndex == 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Packing Mode = Tiled Layers runs in 3 stages:\n" +
                    "1) Tile/Pack: guaranteed filled layers (domino tiling)\n" +
                    "2) Noise: applies Rotation/Position/Scale variation (keeps bricks horizontal)\n" +
                    "3) Repack: if Compact After Generate is enabled, it packs again and enforces cube bounds\n\n" +
                    "Notes:\n" +
                    "- Margin is ignored in this mode (use Compact Margin instead).\n" +
                    "- Rotation Max Y only applies if Use Cardinal Horizontal Rotation is disabled.",
                    MessageType.Info
                );
            }
        }

        var generator = (BrickCubeGenerator)target;

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate"))
            {
                generator.Generate();
            }

            if (GUILayout.Button("Clear"))
            {
                generator.Clear();
            }
        }
    }
}
