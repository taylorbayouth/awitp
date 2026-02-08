using UnityEngine;

/// <summary>
/// ScriptableObject that defines audio atmosphere for a level.
///
/// PURPOSE:
/// - Encapsulates music, ambient sounds, and audio settings
/// - Reusable across multiple levels (e.g., "Calm Theme", "Intense Theme")
/// - Git-friendly and designer-friendly
/// - Applied automatically when level loads
///
/// USAGE:
/// 1. Create: Right-click in Project → Create → AWITP → Level Audio Theme
/// 2. Assign music tracks, ambient sounds
/// 3. Assign to LevelDefinition.audioTheme
/// 4. LevelManager applies theme automatically on level load
///
/// INTEGRATION:
/// - Works with existing GameModeManager dual-track music system
/// - Provides level-specific track overrides
/// - Adds ambient sound layer (wind, birds, water, etc.)
///
/// EXAMPLES:
/// - "Theme_Peaceful" - soft piano, bird chirps
/// - "Theme_Industrial" - electronic beats, machine hums
/// - "Theme_Nature" - strings, wind, forest ambience
/// </summary>
[CreateAssetMenu(fileName = "AudioTheme", menuName = "AWITP/Level Audio Theme")]
public class LevelAudioTheme : ScriptableObject
{
    [Header("Theme Info")]
    [Tooltip("Display name for this theme (e.g., 'Peaceful', 'Intense')")]
    public string themeName;

    [Tooltip("Optional description")]
    [TextArea(2, 3)]
    public string description;

    [Header("Music Tracks")]
    [Tooltip("Override Builder Mode music track (null = use default from GameModeManager)")]
    public AudioClip builderModeTrack;

    [Tooltip("Override Play Mode music track (null = use default from GameModeManager)")]
    public AudioClip playModeTrack;

    [Tooltip("Music volume multiplier for this level")]
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Tooltip("Fade in duration when music starts (seconds)")]
    public float musicFadeInDuration = 2f;

    [Header("Ambient Sounds")]
    [Tooltip("Ambient sound layer (e.g., wind, birds, water). Loops continuously.")]
    public AudioClip ambientLoop;

    [Tooltip("Ambient sound volume")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.3f;

    [Tooltip("Enable 3D spatial audio for ambient sound")]
    public bool ambientIs3D = false;

    [Tooltip("Additional one-shot ambient sounds that play randomly (e.g., distant thunder, bird calls)")]
    public AudioClip[] randomAmbientSounds;

    [Tooltip("Min time between random ambient sounds (seconds)")]
    public float randomSoundMinInterval = 5f;

    [Tooltip("Max time between random ambient sounds (seconds)")]
    public float randomSoundMaxInterval = 15f;

    [Tooltip("Volume for random ambient sounds")]
    [Range(0f, 1f)]
    public float randomSoundVolume = 0.5f;

    [Header("Sound Effects (Future)")]
    [Tooltip("Override block placement sound (null = use default)")]
    public AudioClip blockPlacementSound;

    [Tooltip("Override block removal sound (null = use default)")]
    public AudioClip blockRemovalSound;

    [Tooltip("Override UI click sound (null = use default)")]
    public AudioClip uiClickSound;

    /// <summary>
    /// Applies this audio theme to the current scene.
    /// Called by LevelManager when level loads.
    /// </summary>
    /// <param name="audioSourceContainer">Parent GameObject for spawning audio sources</param>
    /// <returns>The AudioThemeController component managing this theme</returns>
    public AudioThemeController Apply(GameObject audioSourceContainer = null)
    {
        // Create container if not provided
        if (audioSourceContainer == null)
        {
            audioSourceContainer = new GameObject($"AudioTheme_{themeName}");
        }

        // Add controller component
        AudioThemeController controller = audioSourceContainer.GetComponent<AudioThemeController>();
        if (controller == null)
        {
            controller = audioSourceContainer.AddComponent<AudioThemeController>();
        }

        // Initialize controller with this theme
        controller.Initialize(this);

        Debug.Log($"[LevelAudioTheme] Applied theme: {themeName}");
        return controller;
    }
}

/// <summary>
/// Runtime component that manages audio playback for a LevelAudioTheme.
/// Automatically created when theme is applied.
/// Handles ambient loops, random sounds, and integration with GameModeManager.
/// </summary>
public class AudioThemeController : MonoBehaviour
{
    private LevelAudioTheme _theme;
    private AudioSource _ambientSource;
    private AudioSource _randomSoundSource;
    private float _nextRandomSoundTime;

    /// <summary>
    /// Initializes the controller with a theme and starts playback.
    /// </summary>
    public void Initialize(LevelAudioTheme theme)
    {
        _theme = theme;

        // Setup ambient loop
        if (_theme.ambientLoop != null)
        {
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.clip = _theme.ambientLoop;
            _ambientSource.loop = true;
            _ambientSource.volume = _theme.ambientVolume;
            _ambientSource.spatialBlend = _theme.ambientIs3D ? 1f : 0f;
            _ambientSource.Play();
        }

        // Setup random sound source
        if (_theme.randomAmbientSounds != null && _theme.randomAmbientSounds.Length > 0)
        {
            _randomSoundSource = gameObject.AddComponent<AudioSource>();
            _randomSoundSource.loop = false;
            _randomSoundSource.volume = _theme.randomSoundVolume;
            _randomSoundSource.spatialBlend = 0f; // Random sounds typically 2D

            // Schedule first random sound
            ScheduleNextRandomSound();
        }

        // Apply music track overrides to GameModeManager
        ApplyMusicTrackOverrides();
    }

    private void Update()
    {
        // Handle random ambient sounds
        if (_randomSoundSource != null && Time.time >= _nextRandomSoundTime && !_randomSoundSource.isPlaying)
        {
            PlayRandomAmbientSound();
        }
    }

    /// <summary>
    /// Plays a random ambient sound from the theme's array.
    /// </summary>
    private void PlayRandomAmbientSound()
    {
        if (_theme.randomAmbientSounds == null || _theme.randomAmbientSounds.Length == 0)
        {
            return;
        }

        // Pick random clip
        AudioClip clip = _theme.randomAmbientSounds[Random.Range(0, _theme.randomAmbientSounds.Length)];
        if (clip != null)
        {
            _randomSoundSource.PlayOneShot(clip);
        }

        // Schedule next sound
        ScheduleNextRandomSound();
    }

    /// <summary>
    /// Schedules the next random ambient sound.
    /// </summary>
    private void ScheduleNextRandomSound()
    {
        float interval = Random.Range(_theme.randomSoundMinInterval, _theme.randomSoundMaxInterval);
        _nextRandomSoundTime = Time.time + interval;
    }

    /// <summary>
    /// Applies music track overrides to GameModeManager if present.
    /// </summary>
    private void ApplyMusicTrackOverrides()
    {
        GameModeManager gameModeManager = ServiceRegistry.Get<GameModeManager>();
        if (gameModeManager == null)
        {
            Debug.LogWarning("[AudioThemeController] GameModeManager not found, cannot apply music overrides");
            return;
        }

        // Override builder mode track if specified
        if (_theme.builderModeTrack != null)
        {
            gameModeManager.SetBuilderModeTrack(_theme.builderModeTrack);
        }

        // Override play mode track if specified
        if (_theme.playModeTrack != null)
        {
            gameModeManager.SetPlayModeTrack(_theme.playModeTrack);
        }

        // Apply volume multiplier
        gameModeManager.SetMusicVolume(_theme.musicVolume);
    }

    /// <summary>
    /// Stops all audio playback and cleans up.
    /// </summary>
    public void Stop()
    {
        if (_ambientSource != null)
        {
            _ambientSource.Stop();
        }

        if (_randomSoundSource != null)
        {
            _randomSoundSource.Stop();
        }
    }

    private void OnDestroy()
    {
        Stop();
    }
}
