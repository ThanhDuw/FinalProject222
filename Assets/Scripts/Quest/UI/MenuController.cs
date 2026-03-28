using UnityEngine;
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

    [Header("Save Game")]
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private Button     saveButton;

    private void Start()
    {
        // ── Menu open/close ───────────────────────────────────────────────────
        if (menuOpenButton != null)
            menuOpenButton.onClick.AddListener(ToggleMenu);

        if (menuCloseButton != null)
            menuCloseButton.onClick.AddListener(CloseMenu);

        // ── Quest Log ─────────────────────────────────────────────────────────
        if (questButton != null && questLogUI != null)
            questButton.onClick.AddListener(questLogUI.Toggle);

        // ── Save Game ─────────────────────────────────────────────────────────
        if (saveButton != null && saveSystem != null)
            saveButton.onClick.AddListener(SaveGame);

        // Start hidden
        questLogUI?.Close();
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (menuPanel != null && !menuPanel.activeSelf)
                menuPanel.SetActive(true);
            questLogUI?.Toggle();
        }
    }

    private void OnDestroy()
    {
        if (menuOpenButton  != null) menuOpenButton.onClick.RemoveAllListeners();
        if (menuCloseButton != null) menuCloseButton.onClick.RemoveAllListeners();
        if (questButton     != null) questButton.onClick.RemoveAllListeners();
        if (saveButton      != null) saveButton.onClick.RemoveAllListeners();
    }

    // ── Called by whatever opens the pause/menu panel ────────────────────────
    public void OpenMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        questLogUI?.Close();
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        if (menuPanel.activeSelf) CloseMenu();
        else                      OpenMenu();
    }

    // ── Save Game Logic ──────────────────────────────────────────────────────

    /// <summary>
    /// Called when the Save button is clicked.
    /// Saves all game progress: quest, inventory, equipment, health, scene, metadata.
    /// </summary>
    private void SaveGame()
    {
        if (saveSystem == null)
        {
            Debug.LogWarning("[MenuController] SaveSystem reference not assigned in Inspector.");
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
