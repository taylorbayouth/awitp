using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD element that displays lock progress during gameplay.
/// Shows "n of X locks opened" and hides when there are no locks or the level is complete.
/// </summary>
public class LockProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component displaying lock progress (e.g., '1 of 3 locks opened')")]
    public Text progressText;

    [Tooltip("Root GameObject to show/hide the entire progress display")]
    public GameObject progressPanel;

    [Header("Settings")]
    [Tooltip("Format string for progress display. {0} = filled, {1} = total")]
    [SerializeField] private string progressFormat = "{0} of {1} locks opened";

    [Tooltip("Message shown when all locks are opened")]
    [SerializeField] private string allLocksOpenedMessage = "All locks opened!";

    private int _lastFilled = -1;
    private int _lastTotal = -1;

    private void Start()
    {
        // Subscribe to lock progress changes
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLockProgressChanged += OnLockProgressChanged;
            LevelManager.Instance.OnLevelLoaded += OnLevelLoaded;
            LevelManager.Instance.OnLevelUnloaded += OnLevelUnloaded;
        }

        // Hide initially until we get lock data
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLockProgressChanged -= OnLockProgressChanged;
            LevelManager.Instance.OnLevelLoaded -= OnLevelLoaded;
            LevelManager.Instance.OnLevelUnloaded -= OnLevelUnloaded;
        }
    }

    private void OnLevelLoaded(LevelDefinition levelDef)
    {
        // Reset display; will be updated when lock state is first evaluated
        _lastFilled = -1;
        _lastTotal = -1;
    }

    private void OnLevelUnloaded()
    {
        SetVisible(false);
        _lastFilled = -1;
        _lastTotal = -1;
    }

    private void OnLockProgressChanged(int filled, int total)
    {
        if (total == 0)
        {
            // No locks in this level - hide the display
            SetVisible(false);
            return;
        }

        _lastFilled = filled;
        _lastTotal = total;

        SetVisible(true);
        UpdateDisplay(filled, total);
    }

    private void UpdateDisplay(int filled, int total)
    {
        if (progressText == null) return;

        if (filled >= total)
        {
            progressText.text = allLocksOpenedMessage;
        }
        else
        {
            progressText.text = string.Format(progressFormat, filled, total);
        }
    }

    private void SetVisible(bool visible)
    {
        if (progressPanel != null)
        {
            progressPanel.SetActive(visible);
        }
        else if (progressText != null)
        {
            progressText.gameObject.SetActive(visible);
        }
    }
}
