using UnityEngine;

/// <summary>
/// Spawns Lem characters in the scene.
/// Can be configured to spawn automatically on Start or manually via API calls.
/// Useful for testing Lem behavior outside of the level editor.
/// </summary>
public class LemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("If true, spawns a Lem when the scene starts")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("World position where Lem will spawn (if not using this GameObject's position)")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(2, 5, 0);

    [Tooltip("If true, uses this GameObject's position instead of spawnPosition field")]
    [SerializeField] private bool useThisPosition = false;

    private void Start()
    {
        // Auto-spawn if enabled
        if (spawnOnStart)
        {
            SpawnLem();
        }
    }

    /// <summary>
    /// Spawns a Lem at the configured spawn position.
    /// Uses either this GameObject's position or the spawnPosition field.
    /// </summary>
    /// <returns>The spawned Lem GameObject</returns>
    public GameObject SpawnLem()
    {
        Vector3 pos = useThisPosition ? transform.position : spawnPosition;
        return LemController.CreateLem(pos);
    }

    /// <summary>
    /// Spawns a Lem at a specific world position.
    /// </summary>
    /// <param name="position">World position where the Lem should spawn</param>
    /// <returns>The spawned Lem GameObject</returns>
    public GameObject SpawnLemAt(Vector3 position)
    {
        return LemController.CreateLem(position);
    }

    /// <summary>
    /// Draws a gizmo showing where the Lem will spawn.
    /// Visible in Scene view for easy level design.
    /// </summary>
    private void OnDrawGizmos()
    {
        Vector3 pos = useThisPosition ? transform.position : spawnPosition;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, 0.3f);
        Gizmos.DrawLine(pos, pos + Vector3.up);
    }
}
