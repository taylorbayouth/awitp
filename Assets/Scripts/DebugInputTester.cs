using UnityEngine;

/// <summary>
/// Simple script to test if Tab key is being detected properly.
/// Attach this to any GameObject to verify input is working.
/// </summary>
public class DebugInputTester : MonoBehaviour
{
    private void Update()
    {
        // Test all keys
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.Log("!!! TAB KEY DETECTED BY DebugInputTester !!!");
            }
        }
    }
}
