using UnityEngine;
using System.Collections;

public class GameModeManager : MonoBehaviour
{
    [Header("Background Colors")]
    public Color normalModeColor = new Color(0.192f, 0.301f, 0.474f); // Unity default blue-grey
    public Color designerModeColor = new Color(0.85f, 0.85f, 0.85f); // Light grey for level editor (no clouds)
    public Color playModeColor = new Color(0.1f, 0.1f, 0.1f); // Dark for play mode

    [Header("Skybox")]
    public Material normalModeSkybox;
    public Material playModeSkybox;

    [Header("References")]
    public Camera mainCamera;
    public BuilderController builderController;
    public GameObject skyObject; // Reference to Sky plane/canvas (will auto-find if not set)
    private GridVisualizer gridVisualizer;

    [Header("Music")]
    public string builderMusicResourcePath = "Music/SoundtrackBuilder";
    public string playMusicResourcePath = "Music/soundtrackPlay";
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    [Range(0.05f, 2f)]
    public float musicFadeDuration = 0.35f;

    private AudioSource _musicA;
    private AudioSource _musicB;
    private AudioSource _activeMusic;
    private Coroutine _musicRoutine;

    private GameMode previousMode = GameMode.Builder;

    private void Awake()
    {
        // Find references if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("GameModeManager: Camera.main returned null! Looking for any camera...");
                mainCamera = ServiceRegistry.TryGet<Camera>();
            }
        }

        // Look for BuilderController on same GameObject first
        if (builderController == null)
        {
            builderController = GetComponent<BuilderController>();
        }

        // If still not found, search scene
        if (builderController == null)
        {
            builderController = ServiceRegistry.TryGet<BuilderController>();
        }

        // builderController may be assigned later in Update if not found here.

        if (normalModeSkybox == null && RenderSettings.skybox != null)
        {
            normalModeSkybox = RenderSettings.skybox;
        }
    }

    private void Start()
    {
        // Auto-find Sky object if not set
        if (skyObject == null)
        {
            skyObject = GameObject.Find("Sky");
        }

        // Set initial background color on startup
        if (builderController != null)
        {
            previousMode = builderController.currentMode;
            UpdateBackgroundColor();
        }

        EnsureMusicSources();
        UpdateMusicImmediate();
    }

    private void Update()
    {
        if (builderController == null)
        {
            // Try to find it if not found
            builderController = ServiceRegistry.TryGet<BuilderController>();
            return;
        }

        // Check if game mode changed
        if (builderController.currentMode != previousMode)
        {
            previousMode = builderController.currentMode;
            UpdateBackgroundColor();
            UpdateMusic();
        }
    }

    private void UpdateBackgroundColor()
    {
        if (mainCamera == null) return;

        bool isDesignerMode = (builderController.currentMode == GameMode.Designer);

        // Designer mode uses light grey background (no sky)
        if (isDesignerMode)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = designerModeColor;
        }
        else if (builderController.currentMode == GameMode.Play)
        {
            if (playModeSkybox != null)
            {
                RenderSettings.skybox = playModeSkybox;
            }
            mainCamera.clearFlags = CameraClearFlags.Skybox;
        }
        else // Builder mode and other modes use skybox
        {
            if (normalModeSkybox != null)
            {
                RenderSettings.skybox = normalModeSkybox;
            }
            mainCamera.clearFlags = CameraClearFlags.Skybox;
        }

        // Show Sky object for Builder and Play modes, hide in Designer mode
        SetSkyVisible(!isDesignerMode);

        SetGridVisible(builderController.currentMode != GameMode.Play);
    }

    private void EnsureMusicSources()
    {
        if (_musicA == null)
        {
            _musicA = gameObject.AddComponent<AudioSource>();
        }
        if (_musicB == null)
        {
            _musicB = gameObject.AddComponent<AudioSource>();
        }

        SetupMusicSource(_musicA);
        SetupMusicSource(_musicB);

        if (_activeMusic == null)
        {
            _activeMusic = _musicA;
        }
    }

    private void SetupMusicSource(AudioSource source)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
    }

    private AudioClip LoadMusicClip(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        return Resources.Load<AudioClip>(resourcePath);
    }

    private void UpdateMusicImmediate()
    {
        if (builderController == null) return;

        EnsureMusicSources();
        AudioClip target = GetTargetMusicClip();
        if (target == null) return;

        _activeMusic.clip = target;
        _activeMusic.volume = musicVolume;
        if (!_activeMusic.isPlaying)
        {
            _activeMusic.Play();
        }
    }

    private void UpdateMusic()
    {
        if (builderController == null) return;

        EnsureMusicSources();
        AudioClip target = GetTargetMusicClip();
        if (target == null) return;

        if (_activeMusic != null && _activeMusic.clip == target)
        {
            if (!_activeMusic.isPlaying)
            {
                _activeMusic.volume = musicVolume;
                _activeMusic.Play();
            }
            return;
        }

        if (_musicRoutine != null)
        {
            StopCoroutine(_musicRoutine);
        }
        _musicRoutine = StartCoroutine(CrossfadeTo(target));
    }

    private AudioClip GetTargetMusicClip()
    {
        bool isPlayMode = builderController.currentMode == GameMode.Play;
        string path = isPlayMode ? playMusicResourcePath : builderMusicResourcePath;
        AudioClip clip = LoadMusicClip(path);
        if (clip == null)
        {
            Debug.LogWarning($"GameModeManager: Music clip not found at Resources/{path}");
        }
        return clip;
    }

    private IEnumerator CrossfadeTo(AudioClip target)
    {
        if (target == null) yield break;

        AudioSource from = _activeMusic;
        AudioSource to = (_activeMusic == _musicA) ? _musicB : _musicA;
        if (to == null) yield break;

        to.clip = target;
        to.volume = 0f;
        to.Play();

        float duration = Mathf.Max(0.01f, musicFadeDuration);
        float startFrom = from != null ? from.volume : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (from != null) from.volume = Mathf.Lerp(startFrom, 0f, t);
            to.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.volume = 0f;
        }

        to.volume = musicVolume;
        _activeMusic = to;
        _musicRoutine = null;
    }

    public void SetNormalMode()
    {
        if (mainCamera != null)
        {
            if (normalModeSkybox != null)
            {
                RenderSettings.skybox = normalModeSkybox;
            }
            mainCamera.clearFlags = CameraClearFlags.Skybox;
        }
        else
        {
            Debug.LogError("SetNormalMode: Camera is null!");
        }
    }

    public void SetEditorMode()
    {
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = designerModeColor;
        }
        else
        {
            Debug.LogError("SetEditorMode: Camera is null!");
        }
    }

    private void SetGridVisible(bool visible)
    {
        if (gridVisualizer == null)
        {
            gridVisualizer = ServiceRegistry.Get<GridVisualizer>();
        }

        if (gridVisualizer == null) return;

        gridVisualizer.SetGridVisible(visible);
    }

    private void SetSkyVisible(bool visible)
    {
        if (skyObject == null)
        {
            skyObject = GameObject.Find("Sky");
        }

        if (skyObject != null)
        {
            skyObject.SetActive(visible);
        }
    }
}
