using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to set up the inventory UI sprites with proper import settings
/// </summary>
public class InventoryUiSpriteSetup : EditorWindow
{
    [MenuItem("Tools/Setup Inventory UI Sprites")]
    public static void SetupInventorySprites()
    {
        string spritePath = "Assets/Sprites/inventoryUi";
        string[] spriteNames = new string[]
        {
            "ui-ul.png",
            "ui-t.png",
            "ui-ur.png",
            "ui-r.png",
            "ui-br.png",
            "ui-b.png",
            "ui-bl.png",
            "ui-l.png"
        };

        int count = 0;
        foreach (string spriteName in spriteNames)
        {
            string fullPath = Path.Combine(spritePath, spriteName);
            if (ConfigureTextureAsSprite(fullPath))
            {
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[InventoryUiSpriteSetup] Configured {count} sprites for UI use");
        EditorUtility.DisplayDialog("Setup Complete", $"Configured {count} inventory UI sprites", "OK");
    }

    private static bool ConfigureTextureAsSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[InventoryUiSpriteSetup] Could not find texture at: {assetPath}");
            return false;
        }

        // Configure as sprite for UI
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Set max texture size
        TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
        settings.maxTextureSize = 2048;
        settings.format = TextureImporterFormat.Automatic;
        importer.SetPlatformTextureSettings(settings);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[InventoryUiSpriteSetup] Configured sprite: {assetPath}");
        return true;
    }
}
