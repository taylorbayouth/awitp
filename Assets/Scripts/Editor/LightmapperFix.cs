using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// Editor script to automatically fix lightmapper settings for Apple Silicon.
/// Progressive CPU lightmapper is not supported on M1/M2/M3 Macs,
/// so this forces Progressive GPU lightmapper instead.
/// </summary>
[InitializeOnLoad]
public class LightmapperFix
{
    static LightmapperFix()
    {
        // Check if running on Apple Silicon (macOS)
        #if UNITY_EDITOR_OSX

        try
        {
            // Try to get active lighting settings
            var lightingSettings = Lightmapping.lightingSettings;

            if (lightingSettings != null)
            {
                // Check current lightmapper setting
                if (lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveCPU)
                {
                    Debug.LogWarning("[LightmapperFix] Progressive CPU lightmapper not supported on Apple Silicon. Switching to Progressive GPU.");

                    lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
                    EditorUtility.SetDirty(lightingSettings);

                    Debug.Log("[LightmapperFix] Successfully switched to Progressive GPU lightmapper.");
                }
            }
            else
            {
                // No lighting settings in current scene, this is fine
                Debug.Log("[LightmapperFix] No lighting settings found in scene. GPU lightmapper will be used by default.");
            }
        }
        catch (System.Exception)
        {
            // Silently ignore - no lighting settings in scene is normal for this project
            // GPU lightmapper will be used by default when/if lighting is needed
        }

        #endif
    }
}
