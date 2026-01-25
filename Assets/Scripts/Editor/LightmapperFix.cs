using UnityEngine;
using UnityEditor;

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
        // Check if running on Apple Silicon
        #if UNITY_EDITOR_OSX

        // Get current lightmapper
        var lightmapper = Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper;

        // If set to CPU on Apple Silicon, switch to GPU
        if (lightmapper == LightingSettings.Lightmapper.ProgressiveCPU)
        {
            Debug.LogWarning("[LightmapperFix] Progressive CPU lightmapper not supported on Apple Silicon. Switching to Progressive GPU.");

            var lightingSettings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
            lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;

            Debug.Log("[LightmapperFix] Successfully switched to Progressive GPU lightmapper.");
        }

        #endif
    }
}
