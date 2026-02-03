using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for InventoryPanelFrame that provides automatic sprite loading
/// </summary>
[CustomEditor(typeof(InventoryPanelFrame))]
public class InventoryPanelFrameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InventoryPanelFrame frame = (InventoryPanelFrame)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Load Border Sprites"))
        {
            LoadBorderSprites(frame);
        }

        if (GUILayout.Button("Rebuild Frame"))
        {
            // Force rebuild by calling Update in edit mode
            EditorApplication.delayCall += () =>
            {
                if (frame != null)
                {
                    EditorUtility.SetDirty(frame);
                }
            };
        }
    }

    private void LoadBorderSprites(InventoryPanelFrame frame)
    {
        string basePath = "Assets/Sprites/inventoryUi";

        frame.topLeftSprite = LoadSpriteAtPath($"{basePath}/ui-ul.png");
        frame.topSprite = LoadSpriteAtPath($"{basePath}/ui-t.png");
        frame.topRightSprite = LoadSpriteAtPath($"{basePath}/ui-ur.png");
        frame.rightSprite = LoadSpriteAtPath($"{basePath}/ui-r.png");
        frame.bottomRightSprite = LoadSpriteAtPath($"{basePath}/ui-br.png");
        frame.bottomSprite = LoadSpriteAtPath($"{basePath}/ui-b.png");
        frame.bottomLeftSprite = LoadSpriteAtPath($"{basePath}/ui-bl.png");
        frame.leftSprite = LoadSpriteAtPath($"{basePath}/ui-l.png");

        EditorUtility.SetDirty(frame);

        Debug.Log("[InventoryPanelFrame] Border sprites loaded successfully!");
    }

    private Sprite LoadSpriteAtPath(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            // Try loading the texture and getting its sprite
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (Object asset in assets)
                {
                    if (asset is Sprite s)
                    {
                        sprite = s;
                        break;
                    }
                }
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[InventoryPanelFrame] Could not load sprite at: {path}");
        }

        return sprite;
    }
}
