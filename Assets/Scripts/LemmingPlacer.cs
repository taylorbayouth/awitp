using UnityEngine;

/// <summary>
/// Allows placing a lemming character on the grid in editor mode.
/// Press L to place/move lemming, R to toggle direction.
/// </summary>
public class LemmingPlacer : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public GameObject lemmingPrefab;

    [Header("Settings")]
    public KeyCode placeLemmingKey = KeyCode.L;
    public KeyCode toggleDirectionKey = KeyCode.R;
    public float lemmingHeight = 1.5f; // How high above block to spawn

    private GameObject currentLemming;
    private bool lemmingFacingRight = true;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
    }

    private void Update()
    {
        // Toggle direction
        if (Input.GetKeyDown(toggleDirectionKey) && currentLemming != null)
        {
            ToggleLemmingDirection();
        }

        // Place lemming
        if (Input.GetKeyDown(placeLemmingKey))
        {
            PlaceLemmingAtMouse();
        }
    }

    private void PlaceLemmingAtMouse()
    {
        if (gridManager == null) return;

        // Get mouse position in world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

        if (gridPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            int gridIndex = gridManager.WorldToGridIndex(worldPos);

            if (gridIndex >= 0 && gridIndex < gridManager.gridWidth * gridManager.gridHeight)
            {
                // Get center of grid cell
                Vector3 cellCenter = gridManager.IndexToWorldPosition(gridIndex);
                cellCenter.y = lemmingHeight; // Position above block

                // Create or move existing lemming
                if (currentLemming == null)
                {
                    CreateLemming(cellCenter);
                }
                else
                {
                    // Move existing lemming
                    currentLemming.transform.position = cellCenter;
                    Debug.Log($"Moved Lem to grid index {gridIndex}");
                }
            }
        }
    }

    private void CreateLemming(Vector3 position)
    {
        if (lemmingPrefab != null)
        {
            currentLemming = Instantiate(lemmingPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic lemming if no prefab assigned
            currentLemming = new GameObject("Lem");
            currentLemming.transform.position = position;

            // Add CharacterController
            CharacterController controller = currentLemming.AddComponent<CharacterController>();
            controller.radius = 0.25f;
            controller.height = 1.0f;
            controller.center = new Vector3(0, 0.5f, 0);
            controller.stepOffset = 0.3f; // Fix the warning

            // Add LemmingController
            LemmingController lemController = currentLemming.AddComponent<LemmingController>();
            lemController.facingRight = lemmingFacingRight;

            // Add simple visual (cube)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.parent = currentLemming.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);

            // Remove cube collider (CharacterController handles collision)
            Destroy(visual.GetComponent<Collider>());
        }

        currentLemming.name = "Lem";
        SetLemmingDirection(lemmingFacingRight);

        Debug.Log($"Placed Lem at {position}, facing {(lemmingFacingRight ? "right" : "left")}");
    }

    private void ToggleLemmingDirection()
    {
        lemmingFacingRight = !lemmingFacingRight;
        SetLemmingDirection(lemmingFacingRight);
        Debug.Log($"Lem now facing {(lemmingFacingRight ? "right" : "left")}");
    }

    private void SetLemmingDirection(bool facingRight)
    {
        if (currentLemming == null) return;

        LemmingController controller = currentLemming.GetComponent<LemmingController>();
        if (controller != null)
        {
            controller.SetDirection(facingRight);
        }
    }

    private void OnGUI()
    {
        // Show controls hint
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.white;
        style.fontSize = 16;

        string hint = $"[L] Place Lem  [R] Toggle Direction: {(lemmingFacingRight ? "→ Right" : "← Left")}";
        GUI.Label(new Rect(40, Screen.height - 80, 500, 30), hint, style);
    }
}
