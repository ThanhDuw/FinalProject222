using System;
using UnityEngine;
using UnityEngine.UI;
using CreatorKitCodeInternal;

/// <summary>
/// AudioVolumeController — UI Controller (Options Panel)
///
/// Connects Music_Slider and VFX_Slider to the audio system.
/// Persists volume settings across sessions via PlayerPrefs.
///
/// Music Slider  → MusicPlayer AudioSource + AmbiencePlayer master volume
/// VFX Slider    → SFXManager pool (via static SFXVolume property)
///
/// Setup:
///   1. Attach to OptionPanel_UI
///   2. musicSlider  → Music_Slider/Slider
///   3. vfxSlider    → VFX_Slider/Slider
///   4. musicSource  → MusicPlayer AudioSource (auto-find fallback)
///   5. ambiencePlayer → AmbiencePlayer (auto-find fallback)
/// </summary>
public class AudioVolumeController : MonoBehaviour
{
    // ── PlayerPrefs keys ──────────────────────────────────────────────────────
    private const string KeyMusic = "volume_music";
    private const string KeySFX   = "volume_sfx";

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider vfxSlider;

    [Header("Audio Sources")]
    [Tooltip("Drag MusicPlayer AudioSource here (PlayerCore/Managers/MusicPlayer).")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Drag AmbiencePlayer here (PlayerCore/Managers/AmbiencePlayer).")]
    [SerializeField] private AmbiencePlayer ambiencePlayer;

    // ── Static Events ─────────────────────────────────────────────────────────
    /// <summary>Fired when the music slider changes. Listeners should update their AudioSource.volume.</summary>
    public static event Action<float> OnMusicVolumeChanged;
    /// <summary>Fired when the SFX slider changes.</summary>
    public static event Action<float> OnSFXVolumeChanged;

    // ── Cached SFX volume ─────────────────────────────────────────────────────
    private static float s_SFXVolume = 1f;

    /// <summary>Current SFX volume (0–1). Read by SFXManager when playing sounds.</summary>
    public static float SFXVolume => s_SFXVolume;

    /// <summary>Current Music volume (0–1). Reads directly from PlayerPrefs so it works even before the Options panel is opened.</summary>
    public static float MusicVolume => PlayerPrefs.GetFloat(KeyMusic, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find MusicPlayer if not assigned in Inspector
        if (musicSource == null)
        {
            var go = GameObject.Find("MusicPlayer");
            if (go != null)
                musicSource = go.GetComponent<AudioSource>();
        }

        // Auto-find AmbiencePlayer if not assigned in Inspector
        if (ambiencePlayer == null)
        {
            var go = GameObject.Find("AmbiencePlayer");
            if (go != null)
                ambiencePlayer = go.GetComponent<AmbiencePlayer>();
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
            musicSlider.SetValueWithoutNotify(savedMusic);
        }

        if (vfxSlider != null)
        {
            vfxSlider.minValue = 0f;
            vfxSlider.maxValue = 1f;
            vfxSlider.SetValueWithoutNotify(savedSFX);
        }

        // Apply loaded values immediately (local sources + broadcast)
        ApplyMusicVolume(savedMusic);
        ApplySFXVolume(savedSFX);
    }

    private void OnEnable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (vfxSlider   != null) vfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnDisable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (vfxSlider   != null) vfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
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

        // Sync ambience volume with music slider
        if (ambiencePlayer != null)
            ambiencePlayer.SetMasterVolume(value);

        // Broadcast to all listeners (MainMenu BGM, RandomBGMPlayer, etc.)
        OnMusicVolumeChanged?.Invoke(value);
    }

    private void ApplySFXVolume(float value)
    {
        s_SFXVolume = value;
        // SFX pool sources read AudioVolumeController.SFXVolume at play-time
        // via SFXManager.PlaySound — no explicit source assignment needed.

        // Broadcast to any SFX listeners
        OnSFXVolumeChanged?.Invoke(value);
    }
}

