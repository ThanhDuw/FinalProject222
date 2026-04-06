using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CreatorKitCode;
using CreatorKitCodeInternal;

/// <summary>
/// Định tuyến các sự kiện onClick của nút bấm trong MenuManager và khởi tạo
/// các kết nối UI Nhiệm vụ không thể thiết lập qua các trường trong Inspector.
///
/// Gắn vào: MenuManager GameObject
/// Tham chiếu: gán trong Inspector
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Menu Toggle")]
    [Tooltip("Button that opens/closes the menu panel (e.g. the hamburger Menu_Button).")]
    [SerializeField] private Button     menuOpenButton;
    [Tooltip("X / Close button inside Menu_Panel that dismisses the menu.")]
    [SerializeField] private Button     menuCloseButton;
    [Tooltip("The root panel to show/hide.")]
    [SerializeField] private GameObject menuPanel;

    [Header("Quest Log")]
    [SerializeField] private Button     questButton;
    [SerializeField] private QuestLogUI questLogUI;

    [Header("Main Menu")]
    [SerializeField] private Button     mainMenuButton;

    [Header("Options Panel")]
    [SerializeField] private Button     optionButton;
    [SerializeField] private GameObject optionPanel;

    [Header("Help Panel")]
    [SerializeField] private Button     helpButton;
    [SerializeField] private GameObject helpPanel;

    [Header("Save Game")]
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private Button     saveButton;

    [Header("End Game Credits")]
    [Tooltip("If not assigned, will try to find in scene automatically")]
    [SerializeField] private RewardDisplayUI rewardDisplayUI;
    [SerializeField] private GameObject      simpleCreditsPanel;
    [Tooltip("Kéo Assets/Audios/Ending.mp3 vào đây")]
    [SerializeField] private AudioClip       endingMusicClip;

    private AudioSource m_EndingSource;

    private void Awake()
    {
        // Khởi tạo AudioSource riêng cho nhạc Ending
        m_EndingSource             = gameObject.AddComponent<AudioSource>();
        m_EndingSource.loop        = true;
        m_EndingSource.playOnAwake = false;
        m_EndingSource.volume      = AudioVolumeController.MusicVolume;
        AudioVolumeController.OnMusicVolumeChanged += OnMusicVolumeChanged;
    }

    private void Start()
    {

        if (menuOpenButton != null)
        {
            menuOpenButton.onClick.AddListener(PlayClickSound);
            menuOpenButton.onClick.AddListener(ToggleMenu);
        }

        if (menuCloseButton != null)
        {
            menuCloseButton.onClick.AddListener(PlayClickSound);
            menuCloseButton.onClick.AddListener(CloseMenu);
        }


        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(PlayClickSound);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }


        if (questButton != null && questLogUI != null)
        {
            questButton.onClick.AddListener(PlayClickSound);
            questButton.onClick.AddListener(questLogUI.Toggle);
        }


        if (optionButton != null)
        {
            optionButton.onClick.AddListener(PlayClickSound);
            optionButton.onClick.AddListener(ToggleOptionPanel);
        }


        if (helpButton != null)
        {
            helpButton.onClick.AddListener(PlayClickSound);
            helpButton.onClick.AddListener(ToggleHelpPanel);
        }


        if (saveButton != null)
        {
            saveButton.onClick.AddListener(PlayClickSound);
            saveButton.onClick.AddListener(SaveGame);
        }

        // Start hidden
        questLogUI?.Close();
        if (menuPanel   != null) menuPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (helpPanel   != null) helpPanel.SetActive(false);


        if (rewardDisplayUI == null) rewardDisplayUI = UnityEngine.Object.FindFirstObjectByType<RewardDisplayUI>();
        if (simpleCreditsPanel == null) 
        {
            CreditScript cs = UnityEngine.Object.FindFirstObjectByType<CreditScript>(UnityEngine.FindObjectsInactive.Include);
            if (cs != null && cs.transform.parent != null)
            {
                simpleCreditsPanel = cs.transform.parent.gameObject;
            }
        }

        if (rewardDisplayUI != null && simpleCreditsPanel != null)
        {
            // Tự động chờ 3s sau khi bảng Reward xuất hiện rồi kích hoạt CreditPanel
            rewardDisplayUI.OnPanelOpened.AddListener(() => StartCoroutine(ShowCreditsAfterDelay()));
        }
    }

    private System.Collections.IEnumerator ShowCreditsAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (rewardDisplayUI    != null) rewardDisplayUI.HideReward();
        if (simpleCreditsPanel != null) simpleCreditsPanel.SetActive(true);

        // Dừng nhạc nền BGM của scene ngay lập tức
        var bgm = FindFirstObjectByType<RandomBGMPlayer>();
        if (bgm != null)
        {
            var bgmSource = bgm.GetComponent<AudioSource>();
            if (bgmSource != null) bgmSource.Stop();
        }

        // Phát nhạc Ending (loop, theo MusicVolume)
        if (m_EndingSource != null && endingMusicClip != null)
        {
            m_EndingSource.clip   = endingMusicClip;
            m_EndingSource.volume = AudioVolumeController.MusicVolume;
            m_EndingSource.Play();
        }
    }

    private void OnMusicVolumeChanged(float volume)
    {
        if (m_EndingSource != null) m_EndingSource.volume = volume;
    }

    private void Update()
    {

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }


        if (GameInput.Instance != null && GameInput.Instance.QuestLogPressed)
        {
            if (menuPanel != null && !menuPanel.activeSelf)
                menuPanel.SetActive(true);
            questLogUI?.Toggle();
        }
    }

    private void OnDestroy()
    {
        AudioVolumeController.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        if (menuOpenButton  != null) { menuOpenButton.onClick.RemoveListener(PlayClickSound);  menuOpenButton.onClick.RemoveListener(ToggleMenu); }
        if (menuCloseButton != null) { menuCloseButton.onClick.RemoveListener(PlayClickSound); menuCloseButton.onClick.RemoveListener(CloseMenu); }
        if (mainMenuButton  != null) { mainMenuButton.onClick.RemoveListener(PlayClickSound);  mainMenuButton.onClick.RemoveListener(ReturnToMainMenu); }
        if (questButton     != null && questLogUI != null) { questButton.onClick.RemoveListener(PlayClickSound); questButton.onClick.RemoveListener(questLogUI.Toggle); }
        if (optionButton    != null) { optionButton.onClick.RemoveListener(PlayClickSound);    optionButton.onClick.RemoveListener(ToggleOptionPanel); }
        if (helpButton      != null) { helpButton.onClick.RemoveListener(PlayClickSound);      helpButton.onClick.RemoveListener(ToggleHelpPanel); }
        if (saveButton      != null) { saveButton.onClick.RemoveListener(PlayClickSound);      saveButton.onClick.RemoveListener(SaveGame); }
    }


    public void OpenMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        questLogUI?.Close();
        if (optionPanel != null) optionPanel.SetActive(false);
        if (helpPanel   != null) helpPanel.SetActive(false);
        if (menuPanel   != null) menuPanel.SetActive(false);
    }



    private void ToggleOptionPanel()
    {
        if (optionPanel == null) return;
        optionPanel.SetActive(!optionPanel.activeSelf);
        if (helpPanel != null && optionPanel.activeSelf) helpPanel.SetActive(false);
    }

    private void ToggleHelpPanel()
    {
        if (helpPanel == null) return;
        helpPanel.SetActive(!helpPanel.activeSelf);
        if (optionPanel != null && helpPanel.activeSelf) optionPanel.SetActive(false);
    }



    private void PlayClickSound()
    {
        SFXManager.PlayButtonClick();
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        if (menuPanel.activeSelf) CloseMenu();
        else                      OpenMenu();
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Được gọi khi nút Save được nhấn.
    /// Lưu lại toàn bộ tiến độ game: nhiệm vụ, túi đồ, trang bị, máu, cảnh, siêu dữ liệu.
    /// </summary>
    private void SaveGame()
    {
        // Fallback: Nếu tham chiếu bị thiếu trong Inspector, thử tìm kiếm trong Scene
        if (saveSystem == null)
        {
            saveSystem = FindFirstObjectByType<SaveSystem>();
        }

        if (saveSystem == null)
        {
            Debug.LogWarning("[MenuController] SaveSystem reference not assigned in Inspector and could not be found in scene.");
            return;
        }

        // Lưu dữ liệu Nhiệm vụ qua TravelManager
        if (TravelManager.Instance != null)
        {
            TravelManager.Instance.SaveCurrentQuestData();
        }
        else
        {
            Debug.LogWarning("[MenuController] TravelManager not found. Quest data not saved.");
        }

        // Tìm người chơi và lấy CharacterData
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[MenuController] Player not found. Cannot save.");
            return;
        }

        CharacterData characterData = player.GetComponentInChildren<CharacterData>();
        if (characterData == null)
        {
            Debug.LogError("[MenuController] CharacterData not found on Player.");
            return;
        }

        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        string spawnPointID = TravelManager.Instance != null 
            ? TravelManager.Instance.CurrentSpawnPointID 
            : "";

        saveSystem.SaveAll(characterData, currentSceneIndex, spawnPointID);

        Debug.Log("[MenuController] ✅ Game saved successfully!");
        
        // TODO: Thêm popup thông báo UI khi hệ thống UI hỗ trợ
    }
}
