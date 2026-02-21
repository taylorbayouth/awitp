using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps EventSystem UI input modules valid at runtime.
///
/// Why:
/// - Scenes may contain legacy StandaloneInputModule.
/// - In projects using the new Input System, legacy modules do not drive UI clicks.
/// - InputSystemUIInputModule may exist without assigned actions.
/// </summary>
public static class InputModuleFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Fix();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Fix();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Fix();
    }

    private static void Fix()
    {
        foreach (EventSystem es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (es == null) continue;

            InputSystemUIInputModule inputSystemModule = es.GetComponent<InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                inputSystemModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log($"[InputModuleFixer] Added InputSystemUIInputModule on '{es.gameObject.name}'");
            }

            if (inputSystemModule.actionsAsset == null)
            {
                inputSystemModule.AssignDefaultActions();
                Debug.Log($"[InputModuleFixer] Assigned default actions on '{es.gameObject.name}'");
            }

            StandaloneInputModule legacyModule = es.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                Object.Destroy(legacyModule);
                Debug.Log($"[InputModuleFixer] Removed StandaloneInputModule on '{es.gameObject.name}'");
            }
        }
    }
}
