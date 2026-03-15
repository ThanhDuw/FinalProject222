using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TravelMenuUI — UI Controller (Travel System)
///
/// Uses pre-wired buttons defined directly in the scene hierarchy.
/// No runtime Instantiate — buttons are always present, shown/hidden per availability.
///
/// Implements ITravelMenu so NPCTraveler can call Show/Hide.
///
/// Hierarchy:
///   Canvas
///   └── TravelMenuPanel                  (_menuPanel)
///       ├── TitleText (Text)             (_titleText)
///       ├── DestinationList
///       │   ├── Button_WesternVillage    (_destinationButtons[0])
///       │   ├── Button_Desert            (_destinationButtons[1])
///       │   └── Button_Necrom            (_destinationButtons[2])
///       └── CloseButton (Button)         (_closeButton)
/// </summary>
public class TravelMenuUI : MonoBehaviour, ITravelMenu
{
    // ── Inspector — Panel ─────────────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("Root panel GameObject to show/hide.")]
    [SerializeField] private GameObject _menuPanel;

    [Tooltip("Title text at the top of the menu.")]
    [SerializeField] private Text _titleText;

    [Header("Pre-wired Destination Buttons")]
    [Tooltip("Drag Button_WesternVillage, Button_Desert, Button_Necrom here in order.")]
    [SerializeField] private List<Button> _destinationButtons = new List<Button>();

    [Header("Close Button")]
    [SerializeField] private Button _closeButton;

    [Header("Settings")]
    [SerializeField] private string _menuTitle = "Where would you like to go?";

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Action<TravelDestinationData> _onDestinationSelected;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        ValidateSetup();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);

        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);

        // Remove all button listeners to avoid stale references
        ClearButtonListeners();
    }

    // ── ITravelMenu Implementation ────────────────────────────────────────────

    /// <summary>
    /// Shows the menu and wires each pre-wired button to its destination.
    /// Buttons with no matching destination are hidden.
    /// </summary>
    public void Show(List<TravelDestinationData> destinations, Action<TravelDestinationData> onSelected)
    {
        if (destinations == null || destinations.Count == 0)
        {
            Debug.LogWarning("[TravelMenuUI] Show called with null or empty destinations list.");
            return;
        }

        _onDestinationSelected = onSelected;

        // Wire each button to its corresponding destination
        for (int i = 0; i < _destinationButtons.Count; i++)
        {
            Button btn = _destinationButtons[i];
            if (btn == null) continue;

            if (i < destinations.Count && destinations[i] != null)
            {
                TravelDestinationData dest = destinations[i];

                // Update button label text
                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = dest.IsAvailable
                        ? dest.DestinationName
                        : $"{dest.DestinationName} (Unavailable)";

                // Wire onClick — remove old listeners first to avoid duplicates
                btn.onClick.RemoveAllListeners();
                btn.interactable = dest.IsAvailable;

                if (dest.IsAvailable)
                {
                    TravelDestinationData captured = dest;
                    btn.onClick.AddListener(() => OnDestinationButtonClicked(captured));
                }

                btn.gameObject.SetActive(true);
            }
            else
            {
                // No destination for this slot — hide the button
                btn.gameObject.SetActive(false);
            }
        }

        if (_titleText != null)
            _titleText.text = _menuTitle;

        if (_menuPanel != null)
            _menuPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the menu and clears all button listeners.
    /// </summary>
    public void Hide()
    {
        if (_menuPanel != null)
            _menuPanel.SetActive(false);

        ClearButtonListeners();
        _onDestinationSelected = null;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void OnDestinationButtonClicked(TravelDestinationData destination)
    {
        if (_onDestinationSelected == null)
        {
            Debug.LogWarning("[TravelMenuUI] OnDestinationButtonClicked: no callback registered.");
            return;
        }

        _onDestinationSelected.Invoke(destination);
    }

    private void ClearButtonListeners()
    {
        foreach (var btn in _destinationButtons)
        {
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private void ValidateSetup()
    {
        if (_menuPanel == null)
            Debug.LogWarning("[TravelMenuUI] _menuPanel is not assigned.");

        if (_destinationButtons == null || _destinationButtons.Count == 0)
            Debug.LogWarning("[TravelMenuUI] No destination buttons assigned. Drag Button_WesternVillage, Button_Desert, Button_Necrom into _destinationButtons.");

        if (_closeButton == null)
            Debug.LogWarning("[TravelMenuUI] _closeButton is not assigned.");
    }
}
