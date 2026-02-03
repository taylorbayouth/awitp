using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Complete setup utility for the Inventory UI system with border frame
/// </summary>
public class InventoryUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Inventory UI System")]
    public static void SetupInventoryUISystem()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "Setup Inventory UI System",
            "This will:\n" +
            "1. Configure all inventory UI sprites\n" +
            "2. Set up the inventory panel frame\n" +
            "3. Link all sprite references\n\n" +
            "Continue?",
            "Yes",
            "Cancel"
        );

        if (!proceed) return;

        // Step 1: Configure sprites
        InventoryUiSpriteSetup.SetupInventorySprites();

        // Step 2: Find or create InventoryUI
        InventoryUI inventoryUI = GameObject.FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogWarning("[InventoryUISetup] No InventoryUI found in scene. Please add an InventoryUI component first.");
            EditorUtility.DisplayDialog("Setup Incomplete", "No InventoryUI found in scene.\nPlease add an InventoryUI component to a GameObject first.", "OK");
            return;
        }

        // Step 3: Wait for sprites to be imported, then assign them
        EditorApplication.delayCall += () =>
        {
            AssignSpritesToFrame(inventoryUI);
        };
    }

    private static void AssignSpritesToFrame(InventoryUI inventoryUI)
    {
        if (inventoryUI == null) return;

        // Force the UI to rebuild if needed
        inventoryUI.enabled = false;
        inventoryUI.enabled = true;

        // Find the frame component (it should be created by InventoryUI)
        InventoryPanelFrame frame = inventoryUI.GetComponentInChildren<InventoryPanelFrame>();
        if (frame != null)
        {
            // Auto-load sprites
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
            Debug.Log("[InventoryUISetup] Sprites assigned to frame successfully!");
        }

        EditorUtility.DisplayDialog(
            "Setup Complete!",
            "Inventory UI system has been set up successfully!\n\n" +
            "The border frame will automatically wrap your inventory panel.",
            "OK"
        );
    }

    private static Sprite LoadSpriteAtPath(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
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

        return sprite;
    }
}
