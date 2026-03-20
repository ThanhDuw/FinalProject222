using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AudioVolumeController — UI Controller (Options Panel)
///
/// Connects Music_Slider and SFX_Slider to the actual AudioSources in the scene.
/// Persists volume settings across sessions via PlayerPrefs.
///
/// Dependency flow (per CLAUDE.md):
///   AudioVolumeController (UI) → AudioSource (MusicPlayer, SFX pool)
///
/// Setup requirements:
///   1. Assign musicSlider   → Music_Slider
///   2. Assign sfxSlider     → SFX_Slider
///   3. Assign musicSource   → MusicPlayer AudioSource (PlayerCore/Managers/MusicPlayer)
///   4. Assign sfxSources    → all pooled SFX AudioSources (optional; volume applied on play)
/// </summary>
public class AudioVolumeController : MonoBehaviour
{
    // ── PlayerPrefs keys ──────────────────────────────────────────────────────
    private const string KeyMusic = "volume_music";
    private const string KeySFX   = "volume_sfx";

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Audio Sources")]
    [Tooltip("Drag MusicPlayer AudioSource here (PlayerCore/Managers/MusicPlayer).")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Drag all pooled SFX AudioSources here. Leave empty to use FindObjectsByType fallback.")]
    [SerializeField] private AudioSource[] sfxSources;

    // ── Cached SFX volume ─────────────────────────────────────────────────────
    // Stored so SFXManager pool sources (spawned at runtime) can read it via static accessor.
    private static float s_SFXVolume = 1f;

    /// <summary>Current SFX volume (0–1). Read by SFXManager when playing sounds.</summary>
    public static float SFXVolume => s_SFXVolume;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

private void Awake()
    {
        // If musicSource not assigned in Inspector, find it at runtime (gameplay scenes)
        if (musicSource == null)
        {
            var go = GameObject.Find("MusicPlayer");
            if (go != null)
                musicSource = go.GetComponent<AudioSource>();
        }

        // Load persisted values, default to 1 if first run
        float savedMusic = PlayerPrefs.GetFloat(KeyMusic, 1f);
        float savedSFX   = PlayerPrefs.GetFloat(KeySFX,   1f);

        s_SFXVolume = savedSFX;

        // Initialise sliders without triggering callbacks yet
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value    = savedMusic;
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value    = savedSFX;
        }

        // Apply loaded values immediately
        ApplyMusicVolume(savedMusic);
        ApplySFXVolume(savedSFX);
    }

    private void OnEnable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider   != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnDisable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (sfxSlider   != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }

    // ── Slider callbacks ──────────────────────────────────────────────────────

    private void OnMusicChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(KeyMusic, value);
        PlayerPrefs.Save();
    }

    private void OnSFXChanged(float value)
    {
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat(KeySFX, value);
        PlayerPrefs.Save();
    }

    // ── Apply helpers ─────────────────────────────────────────────────────────

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;
    }

    private void ApplySFXVolume(float value)
    {
        s_SFXVolume = value;

        // Apply to any explicitly assigned SFX sources
        if (sfxSources != null)
        {
            foreach (var src in sfxSources)
                if (src != null) src.volume = value;
        }
    }
}
