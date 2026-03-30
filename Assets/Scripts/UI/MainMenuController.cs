using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CreatorKitCode;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main Menu controller.
/// Buttons wired via AddListener in Awake — no Inspector onClick needed.
/// Assign all references in the Inspector on MainMenuManager.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // ── Main Buttons ──────────────────────────────────────────────────────────
    [Header("Main Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button quitButton;

    // ── Panel Close Buttons ───────────────────────────────────────────────────
    [Header("Panel Close Buttons")]
    [SerializeField] private Button optionsCloseButton;
    [SerializeField] private Button helpCloseButton;

    // ── Panels ────────────────────────────────────────────────────────────────
    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject helpPanel;

    // ── Scene ─────────────────────────────────────────────────────────────────
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Western Village";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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

        // Fade in when MainMenu first opens
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

    // ── Callbacks ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when Start button is clicked.
    /// Step 6: Clears all save data before starting a fresh new game.
    /// </summary>
    public void OnStartPressed()
    {
        // Clear all save data to ensure fresh start
        SaveSystem.ClearAllData();
        Debug.Log("[MainMenuController] New Game - all save data cleared.");
        LoadScene(gameSceneName);
    }

    /// <summary>
    /// Called when Continue/Load button is clicked.
    /// Step 7: Loads saved game if available, otherwise starts new game.
    /// </summary>
    public void OnLoadPressed()
    {
        if (HasSaveData())
        {
            // Load saved game via TravelManager
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

    // ── SFX ─────────────────────────────────────────────────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────────

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
        // Use SaveSystem's static method to check for saved game data
        return SaveSystem.HasSaveData();
    }
}
