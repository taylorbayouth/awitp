using UnityEngine;

/// <summary>
/// Simple helper to load a LevelDefinition at runtime.
/// Attach to a GameObject in the scene and assign a LevelDefinition in the Inspector.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    [Tooltip("LevelDefinition asset to load on Start")]
    public LevelDefinition levelDefinition;

    private void Start()
    {
        if (levelDefinition == null)
        {
            Debug.LogWarning("[LevelLoader] No LevelDefinition set. Assign one in the Inspector.");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("[LevelLoader] LevelManager.Instance not found. Ensure LevelManager exists in the scene.");
            return;
        }

        LevelManager.Instance.LoadLevel(levelDefinition);
    }
}
