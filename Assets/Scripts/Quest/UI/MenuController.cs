using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CreatorKitCode;

/// <summary>
/// Wires up button onClick events in the MenuManager and bootstraps
/// Quest UI connections that cannot be set via Inspector serialized fields.
///
/// Attach to: MenuManager GameObject
/// References: assign in Inspector
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

    private void Start()
    {
        // ── Menu open/close ───────────────────────────────────────────────────
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

        // ── Main Menu ────────────────────────────────────────────────────────
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(PlayClickSound);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        // ── Quest Log ─────────────────────────────────────────────────────────
        if (questButton != null && questLogUI != null)
        {
            questButton.onClick.AddListener(PlayClickSound);
            questButton.onClick.AddListener(questLogUI.Toggle);
        }

        // ── Options Panel ────────────────────────────────────────────────────
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(PlayClickSound);
            optionButton.onClick.AddListener(ToggleOptionPanel);
        }

        // ── Help Panel ───────────────────────────────────────────────────────
        if (helpButton != null)
        {
            helpButton.onClick.AddListener(PlayClickSound);
            helpButton.onClick.AddListener(ToggleHelpPanel);
        }

        // ── Save Game ─────────────────────────────────────────────────────────
        if (saveButton != null && saveSystem != null)
        {
            saveButton.onClick.AddListener(PlayClickSound);
            saveButton.onClick.AddListener(SaveGame);
        }

        // Start hidden
        questLogUI?.Close();
        if (menuPanel   != null) menuPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (helpPanel   != null) helpPanel.SetActive(false);
    }

    private void Update()
    {
        // ── Esc key → toggle menu ────────────────────────────────────────────
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        // ── Quest Log hotkey ─────────────────────────────────────────────────
        if (GameInput.Instance != null && GameInput.Instance.QuestLogPressed)
        {
            if (menuPanel != null && !menuPanel.activeSelf)
                menuPanel.SetActive(true);
            questLogUI?.Toggle();
        }
    }

    private void OnDestroy()
    {
        if (menuOpenButton  != null) { menuOpenButton.onClick.RemoveListener(PlayClickSound);  menuOpenButton.onClick.RemoveListener(ToggleMenu); }
        if (menuCloseButton != null) { menuCloseButton.onClick.RemoveListener(PlayClickSound); menuCloseButton.onClick.RemoveListener(CloseMenu); }
        if (mainMenuButton  != null) { mainMenuButton.onClick.RemoveListener(PlayClickSound);  mainMenuButton.onClick.RemoveListener(ReturnToMainMenu); }
        if (questButton     != null && questLogUI != null) { questButton.onClick.RemoveListener(PlayClickSound); questButton.onClick.RemoveListener(questLogUI.Toggle); }
        if (optionButton    != null) { optionButton.onClick.RemoveListener(PlayClickSound);    optionButton.onClick.RemoveListener(ToggleOptionPanel); }
        if (helpButton      != null) { helpButton.onClick.RemoveListener(PlayClickSound);      helpButton.onClick.RemoveListener(ToggleHelpPanel); }
        if (saveButton      != null) { saveButton.onClick.RemoveListener(PlayClickSound);      saveButton.onClick.RemoveListener(SaveGame); }
    }

    // ── Called by whatever opens the pause/menu panel ────────────────────────
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

    // ── Sub-panel toggles ────────────────────────────────────────────────

    private void ToggleOptionPanel()
    {
        if (optionPanel == null) return;
        optionPanel.SetActive(!optionPanel.activeSelf);
        // Close other sub-panels
        if (helpPanel != null && optionPanel.activeSelf) helpPanel.SetActive(false);
    }

    private void ToggleHelpPanel()
    {
        if (helpPanel == null) return;
        helpPanel.SetActive(!helpPanel.activeSelf);
        // Close other sub-panels
        if (optionPanel != null && helpPanel.activeSelf) optionPanel.SetActive(false);
    }

    // ── SFX ──────────────────────────────────────────────────────────────────

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
        // Unpause if timescale was modified, then load scene 0 (MainMenu)
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // ── Save Game Logic ──────────────────────────────────────────────────────

    /// <summary>
    /// Called when the Save button is clicked.
    /// Saves all game progress: quest, inventory, equipment, health, scene, metadata.
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

        // Step 1: Save Quest Data via TravelManager
        if (TravelManager.Instance != null)
        {
            TravelManager.Instance.SaveCurrentQuestData();
        }
        else
        {
            Debug.LogWarning("[MenuController] TravelManager not found. Quest data not saved.");
        }

        // Step 2: Find Player and get CharacterData
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

        // Step 3: Get current scene index
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        // Step 4: Get current spawn point ID from TravelManager
        string spawnPointID = TravelManager.Instance != null 
            ? TravelManager.Instance.CurrentSpawnPointID 
            : "";

        // Step 5: Call SaveSystem.SaveAll()
        // This saves: Inventory, Equipment, Health, Scene, Metadata
        saveSystem.SaveAll(characterData, currentSceneIndex, spawnPointID);

        Debug.Log("[MenuController] ✅ Game saved successfully!");
        
        // Optional: Show UI feedback to player
        // TODO: Add UI notification popup when UISystem supports it
    }
}
