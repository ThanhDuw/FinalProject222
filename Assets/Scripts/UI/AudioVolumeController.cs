using System;
using UnityEngine;
using UnityEngine.UI;
using CreatorKitCodeInternal;

/// <summary>
/// AudioVolumeController — Điều khiển UI (Bảng tùy chọn âm lượng)
///
/// Kết nối Music_Slider và VFX_Slider với hệ thống âm thanh.
/// Lưu trữ cài đặt âm lượng qua các phiên chơi bằng PlayerPrefs.
///
/// Thanh trượt Music  → AudioSource của MusicPlayer + Âm lượng tổng của AmbiencePlayer
/// Thanh trượt VFX    → Các nguồn âm trong SFXManager pool (qua thuộc tính tĩnh SFXVolume)
///
/// Thiết lập:
///   1. Gắn vào OptionPanel_UI
///   2. musicSlider  → Tham chiếu đến Slider của Music
///   3. vfxSlider    → Tham chiếu đến Slider của VFX
///   4. musicSource  → AudioSource của MusicPlayer (tự động tìm nếu để trống)
///   5. ambiencePlayer → AmbiencePlayer (tự động tìm nếu để trống)
/// </summary>
public class AudioVolumeController : MonoBehaviour
{
    // Các phím PlayerPrefs
    private const string KeyMusic = "volume_music";
    private const string KeySFX   = "volume_sfx";

    // Inspector Settings
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider vfxSlider;

    [Header("Audio Sources")]
    [Tooltip("Drag MusicPlayer AudioSource here (PlayerCore/Managers/MusicPlayer).")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Drag AmbiencePlayer here (PlayerCore/Managers/AmbiencePlayer).")]
    [SerializeField] private AmbiencePlayer ambiencePlayer;

    // Các Sự kiện Tĩnh (Static Events)
    /// <summary>Kích hoạt khi thanh trượt âm nhạc thay đổi. Các đối tượng lắng nghe nên cập nhật AudioSource.volume.</summary>
    public static event Action<float> OnMusicVolumeChanged;
    /// <summary>Kích hoạt khi thanh trượt SFX thay đổi.</summary>
    public static event Action<float> OnSFXVolumeChanged;

    // Lưu trữ âm lượng SFX tạm thời
    private static float s_SFXVolume = 1f;

    /// <summary>Âm lượng SFX hiện tại (0–1). Được SFXManager đọc khi phát âm thanh.</summary>
    public static float SFXVolume => s_SFXVolume;

    /// <summary>Âm lượng Nhạc hiện tại (0–1). Đọc trực tiếp từ PlayerPrefs để hoạt động ngay cả trước khi bảng Options được mở.</summary>
    public static float MusicVolume => PlayerPrefs.GetFloat(KeyMusic, 1f);

    // Vòng đời của Script (Lifecycle)

    private void Awake()
    {
        // Tự động tìm MusicPlayer nếu chưa được gán trong Inspector
        if (musicSource == null)
        {
            var go = GameObject.Find("MusicPlayer");
            if (go != null)
                musicSource = go.GetComponent<AudioSource>();
        }

        // Tự động tìm AmbiencePlayer nếu chưa được gán trong Inspector
        if (ambiencePlayer == null)
        {
            var go = GameObject.Find("AmbiencePlayer");
            if (go != null)
                ambiencePlayer = go.GetComponent<AmbiencePlayer>();
        }

        // Tải các giá trị đã lưu, mặc định là 1 nếu chạy lần đầu
        float savedMusic = PlayerPrefs.GetFloat(KeyMusic, 1f);
        float savedSFX   = PlayerPrefs.GetFloat(KeySFX,   1f);

        s_SFXVolume = savedSFX;

        // Khởi tạo các thanh trượt mà chưa kích hoạt callback (tránh bị lặp)
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

        // Áp dụng các giá trị đã tải ngay lập tức (cho local sources và broadcast)
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

    // Các hàm Callbacks của Slider

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

    // Các hàm bổ trợ để áp dụng âm lượng

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;

        // Đồng bộ âm lượng môi trường với thanh trượt nhạc nền
        if (ambiencePlayer != null)
            ambiencePlayer.SetMasterVolume(value);

        // Phát tín hiệu đến tất cả các đối tượng đang lắng nghe (MainMenu BGM, RandomBGMPlayer, v.v.)
        OnMusicVolumeChanged?.Invoke(value);
    }

    private void ApplySFXVolume(float value)
    {
        s_SFXVolume = value;
        // Các nguồn âm SFX pool sẽ đọc AudioVolumeController.SFXVolume lúc bắt đầu phát
        // thông qua SFXManager.PlaySound — không cần gán thủ công từng nguồn.

        // Phát tín hiệu đến bất kỳ đối tượng lắng nghe SFX nào
        OnSFXVolumeChanged?.Invoke(value);
    }
}

