using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CreatorKitCode;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Điều khiển Main Menu.
/// Các nút được gán sự kiện qua AddListener trong Awake — không cần thiết lập onClick trong Inspector.
/// Gán tất cả các tham chiếu trong Inspector trên đối tượng MainMenuManager.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // Nút chính
    [Header("Main Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button quitButton;

    // Nút đóng bảng (Panel)
    [Header("Panel Close Buttons")]
    [SerializeField] private Button optionsCloseButton;
    [SerializeField] private Button helpCloseButton;

    // Các bảng (Panels)
    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject helpPanel;

    // Cảnh (Scene)
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Western Village";

    // Vòng đời (Lifecycle)

    private void Awake()
    {
        if (startButton        != null) { startButton.onClick.AddListener(PlayClickSound);        startButton.onClick.AddListener(OnStartPressed); }
        if (loadButton         != null) { loadButton.onClick.AddListener(PlayClickSound);         loadButton.onClick.AddListener(OnLoadPressed); }
        if (optionsButton      != null) { optionsButton.onClick.AddListener(PlayClickSound);      optionsButton.onClick.AddListener(OnOptionsPressed); }
        if (helpButton         != null) { helpButton.onClick.AddListener(PlayClickSound);         helpButton.onClick.AddListener(OnHelpPressed); }
        if (quitButton         != null) { quitButton.onClick.AddListener(PlayClickSound);         quitButton.onClick.AddListener(OnQuitPressed); }
        if (optionsCloseButton != null) { optionsCloseButton.onClick.AddListener(PlayClickSound); optionsCloseButton.onClick.AddListener(OnOptionsPressed); }
        if (helpCloseButton    != null) { helpCloseButton.onClick.AddListener(PlayClickSound);    helpCloseButton.onClick.AddListener(OnHelpPressed); }
    }

    private void Start()
    {
        SetPanel(optionsPanel, false);
        SetPanel(helpPanel, false);

        // Hiệu ứng mờ dần (Fade in) khi MainMenu mở lần đầu
        SceneTransitionUI.Instance?.FadeIn();

        // ── Phát nhạc nền (BGM) ──
        if (mainMenuBGM != null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            bgmAudioSource.clip = mainMenuBGM;
            bgmAudioSource.loop = true;
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.volume = AudioVolumeController.MusicVolume;
            bgmAudioSource.Play();
        }
    }

    private void OnEnable()
    {
        AudioVolumeController.OnMusicVolumeChanged += OnMusicVolumeChanged;
    }

    private void OnDisable()
    {
        AudioVolumeController.OnMusicVolumeChanged -= OnMusicVolumeChanged;
    }

    private void OnDestroy()
    {
        if (startButton        != null) { startButton.onClick.RemoveListener(PlayClickSound);        startButton.onClick.RemoveListener(OnStartPressed); }
        if (loadButton         != null) { loadButton.onClick.RemoveListener(PlayClickSound);         loadButton.onClick.RemoveListener(OnLoadPressed); }
        if (optionsButton      != null) { optionsButton.onClick.RemoveListener(PlayClickSound);      optionsButton.onClick.RemoveListener(OnOptionsPressed); }
        if (helpButton         != null) { helpButton.onClick.RemoveListener(PlayClickSound);         helpButton.onClick.RemoveListener(OnHelpPressed); }
        if (quitButton         != null) { quitButton.onClick.RemoveListener(PlayClickSound);         quitButton.onClick.RemoveListener(OnQuitPressed); }
        if (optionsCloseButton != null) { optionsCloseButton.onClick.RemoveListener(PlayClickSound); optionsCloseButton.onClick.RemoveListener(OnOptionsPressed); }
        if (helpCloseButton    != null) { helpCloseButton.onClick.RemoveListener(PlayClickSound);    helpCloseButton.onClick.RemoveListener(OnHelpPressed); }
    }

    // Các hàm phản hồi (Callbacks)

    /// <summary>
    /// Được gọi khi nhấn nút Start.
    /// Xóa toàn bộ dữ liệu đã lưu trước khi bắt đầu một trò chơi mới hoàn toàn.
    /// </summary>
    public void OnStartPressed()
    {
        // Xóa toàn bộ dữ liệu để đảm bảo khởi đầu mới
        SaveSystem.ClearAllData();
        Debug.Log("[MainMenuController] New Game - all save data cleared.");
        LoadScene(gameSceneName);
    }

    /// <summary>
    /// Được gọi khi nhấn nút Continue/Load.
    /// Tải trò chơi đã lưu nếu có, nếu không thì bắt đầu trò chơi mới.
    /// </summary>
    public void OnLoadPressed()
    {
        if (HasSaveData())
        {
            // Tải trò chơi qua TravelManager
            if (TravelManager.Instance != null)
            {
                TravelManager.Instance.LoadSavedGame();
                Debug.Log("[MainMenuController] Loading saved game...");
            }
            else
            {
                Debug.LogError("[MainMenuController] TravelManager not found. Cannot load game.");
            }
        }
        else
        {
            Debug.LogWarning("[MainMenuController] No save data found - starting new game instead.");
            SaveSystem.ClearAllData();
            LoadScene(gameSceneName);
        }
    }

    public void OnOptionsPressed()
    {
        TogglePanel(optionsPanel);
        if (helpPanel != null && helpPanel.activeSelf)
            SetPanel(helpPanel, false);
    }

    public void OnHelpPressed()
    {
        TogglePanel(helpPanel);
        if (optionsPanel != null && optionsPanel.activeSelf)
            SetPanel(optionsPanel, false);
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    // Hiệu ứng âm thanh (SFX)

    [Header("Audio (Fallback)")]
    [Tooltip("Gắn MainMenu_BGM.mp3 vào đây để phát nhạc nền.")]
    [SerializeField] private AudioClip mainMenuBGM;
    [Tooltip("Gắn Button_Click.mp3 vào đây nếu Scene không có SFXManager.")]
    [SerializeField] private AudioClip buttonClickSound;
    
    private AudioSource localAudioSource;
    private AudioSource bgmAudioSource;

    private void PlayClickSound()
    {
        // 1. Cố gắng sử dụng hệ thống chung nếu Managers prefab đã được nạp
        if (SFXManager.Instance != null && SFXManager.Instance.ButtonClickSound != null)
        {
            SFXManager.PlayButtonClick();
            return;
        }

        // 2. Dự phòng: Phát bằng AudioSource cục bộ, tuân thủ theo SFXVolume
        if (buttonClickSound != null)
        {
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.playOnAwake = false;
            }

            localAudioSource.PlayOneShot(buttonClickSound, AudioVolumeController.SFXVolume);
        }
    }

    /// <summary>
    /// Callback: cập nhật volume BGM ngay khi người chơi kéo thanh trượt Music.
    /// </summary>
    private void OnMusicVolumeChanged(float volume)
    {
        if (bgmAudioSource != null)
            bgmAudioSource.volume = volume;
    }

    // Các hàm bổ trợ (Helpers)

    private void TogglePanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[MainMenuController] Panel reference not assigned in Inspector.");
            return;
        }
        panel.SetActive(!panel.activeSelf);
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[MainMenuController] gameSceneName is empty. Check Inspector.");
            return;
        }
        if (TravelManager.Instance != null)
            TravelManager.Instance.TravelFromMainMenu(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private bool HasSaveData()
    {
        // Sử dụng phương thức tĩnh của SaveSystem để kiểm tra dữ liệu đã lưu
        return SaveSystem.HasSaveData();
    }
}
