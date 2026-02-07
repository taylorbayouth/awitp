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

    [Header("Music - Synchronized Dual-Track System")]
    [Tooltip("Both tracks play simultaneously from start, only volumes crossfade")]
    public string builderMusicResourcePath = "Music/SoundtrackBuild";
    public string playMusicResourcePath = "Music/SoundtrackPlay";
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    [Range(0.05f, 2f)]
    public float musicFadeDuration = 0.35f;

    // Synchronized music system - both tracks play continuously, volumes crossfade
    private AudioSource _musicA;  // Builder/Designer soundtrack (always playing, volume 0 or musicVolume)
    private AudioSource _musicB;  // Play mode soundtrack (always playing, volume 0 or musicVolume)
    private AudioSource _activeMusic;  // Reference to whichever track is currently at full volume
    private Coroutine _musicRoutine;
    private bool _musicInitialized = false;

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

        // Load both music tracks
        AudioClip builderClip = LoadMusicClip(builderMusicResourcePath);
        AudioClip playClip = LoadMusicClip(playMusicResourcePath);

        if (builderClip == null || playClip == null)
        {
            Debug.LogWarning("GameModeManager: Failed to load one or both music clips");
            return;
        }

        // Assign clips to audio sources
        _musicA.clip = builderClip;  // Builder/Designer music
        _musicB.clip = playClip;      // Play mode music

        // Start both playing simultaneously at time 0
        _musicA.Play();
        _musicB.Play();

        // Set initial volumes based on current mode
        bool isPlayMode = builderController.currentMode == GameMode.Play;
        _musicA.volume = isPlayMode ? 0f : musicVolume;
        _musicB.volume = isPlayMode ? musicVolume : 0f;

        _activeMusic = isPlayMode ? _musicB : _musicA;
        _musicInitialized = true;

        DebugLog.Info($"[GameModeManager] Both music tracks started in sync. Mode: {builderController.currentMode}");
    }

    private void UpdateMusic()
    {
        if (builderController == null) return;

        // Initialize music system if not already done
        if (!_musicInitialized)
        {
            UpdateMusicImmediate();
            return;
        }

        // Determine which track should be active based on mode
        bool isPlayMode = builderController.currentMode == GameMode.Play;
        AudioSource targetSource = isPlayMode ? _musicB : _musicA;

        // If already on the correct track, do nothing
        if (_activeMusic == targetSource)
        {
            return;
        }

        // Start crossfade to the other track
        if (_musicRoutine != null)
        {
            StopCoroutine(_musicRoutine);
        }
        _musicRoutine = StartCoroutine(CrossfadeVolumes(targetSource));
    }


    private IEnumerator CrossfadeVolumes(AudioSource targetSource)
    {
        if (targetSource == null) yield break;

        AudioSource from = _activeMusic;
        AudioSource to = targetSource;

        float duration = Mathf.Max(0.01f, musicFadeDuration);
        float startFromVolume = from != null ? from.volume : 0f;
        float startToVolume = to.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Crossfade volumes (both tracks keep playing)
            if (from != null) from.volume = Mathf.Lerp(startFromVolume, 0f, t);
            to.volume = Mathf.Lerp(startToVolume, musicVolume, t);

            yield return null;
        }

        // Ensure final volumes are exact
        if (from != null) from.volume = 0f;
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
