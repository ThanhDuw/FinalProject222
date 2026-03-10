using UnityEngine;
using UnityEngine.UI;

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

        // Start hidden
        questLogUI?.Close();
        if (menuPanel != null) menuPanel.SetActive(false);
    }

private void OnDestroy()
    {
        if (menuOpenButton  != null) menuOpenButton.onClick.RemoveAllListeners();
        if (menuCloseButton != null) menuCloseButton.onClick.RemoveAllListeners();
        if (questButton     != null) questButton.onClick.RemoveAllListeners();
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
}
