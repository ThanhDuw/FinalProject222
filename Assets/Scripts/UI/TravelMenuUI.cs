using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TravelMenuUI — UI Controller (Travel System)
///
/// Displays a panel listing available travel destinations.
/// Player clicks a destination button to confirm travel.
///
/// Implements ITravelMenu so NPCTraveler can call Show/Hide
/// without a direct type dependency.
///
/// Setup requirements:
///   1. Attach to a UI GameObject inside the scene Canvas
///   2. Assign all Inspector references (menuPanel, titleText, etc.)
///   3. Assign destinationButtonPrefab — a prefab with Button + Text components
///   4. Drag this component reference into NPCTraveler._travelMenuUI
///
/// Hierarchy suggestion:
///   Canvas
///   └── TravelMenuPanel              (_menuPanel)
///       ├── TitleText (Text)         (_titleText)
///       ├── DestinationList          (_destinationListContainer)
///       │   └── [buttons at runtime]
///       └── CloseButton (Button)     (_closeButton)
/// </summary>
public class TravelMenuUI : MonoBehaviour, ITravelMenu
{
    // ── Inspector — UI References ─────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("Root panel GameObject to show/hide.")]
    [SerializeField] private GameObject _menuPanel;

    [Tooltip("Title text displayed at the top of the travel menu.")]
    [SerializeField] private Text _titleText;

    [Header("Destination List")]
    [Tooltip("Parent Transform where destination buttons will be spawned at runtime.")]
    [SerializeField] private Transform _destinationListContainer;

    [Tooltip("Prefab for each destination entry. Must contain a Button and a Text component.")]
    [SerializeField] private GameObject _destinationButtonPrefab;

    [Header("Close Button")]
    [Tooltip("Button that closes the travel menu without traveling.")]
    [SerializeField] private Button _closeButton;

    [Header("Settings")]
    [Tooltip("Title shown at the top of the menu panel.")]
    [SerializeField] private string _menuTitle = "Where would you like to go?";

    // ── Runtime ───────────────────────────────────────────────────────────────

    /// <summary>Callback invoked when the player selects a destination.</summary>
    private Action<TravelDestinationData> _onDestinationSelected;

    /// <summary>Tracks all instantiated destination buttons for cleanup.</summary>
    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        ValidateSetup();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);

        // Always start hidden
        if (_menuPanel != null)
            _menuPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);

        ClearButtons();
    }

    // ── ITravelMenu Implementation ────────────────────────────────────────────

    /// <summary>
    /// Shows the travel menu populated with the given destinations.
    /// Called by NPCTraveler.OpenTravelMenu().
    /// </summary>
    /// <param name="destinations">List of TravelDestinationData to display as buttons.</param>
    /// <param name="onSelected">Callback invoked when the player picks a destination.</param>
    public void Show(List<TravelDestinationData> destinations, Action<TravelDestinationData> onSelected)
    {
        if (destinations == null || destinations.Count == 0)
        {
            Debug.LogWarning("[TravelMenuUI] Show called with null or empty destinations list.");
            return;
        }

        // Store callback for use when a button is clicked
        _onDestinationSelected = onSelected;

        // Clear stale buttons from any previous Show call
        ClearButtons();

        // Populate fresh buttons
        PopulateDestinations(destinations);

        // Set title
        if (_titleText != null)
            _titleText.text = _menuTitle;

        // Show the panel
        if (_menuPanel != null)
            _menuPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the travel menu and clears all spawned buttons.
    /// Called by NPCTraveler.CloseTravelMenu() or the close button.
    /// </summary>
    public void Hide()
    {
        if (_menuPanel != null)
            _menuPanel.SetActive(false);

        ClearButtons();

        // Reset callback so no stale reference remains
        _onDestinationSelected = null;
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Instantiates one button per destination inside _destinationListContainer.
    /// Unavailable destinations are shown grayed out and non-interactable.
    /// </summary>
    private void PopulateDestinations(List<TravelDestinationData> destinations)
    {
        if (_destinationButtonPrefab == null || _destinationListContainer == null)
        {
            Debug.LogWarning("[TravelMenuUI] Cannot populate destinations — prefab or container is missing.");
            return;
        }

        foreach (var destination in destinations)
        {
            if (destination == null) continue;

            // Instantiate button under the list container
            GameObject buttonGO = Instantiate(_destinationButtonPrefab, _destinationListContainer);
            _spawnedButtons.Add(buttonGO);

            // Set the button label
            Text label = buttonGO.GetComponentInChildren<Text>();
            if (label != null)
                label.text = destination.DestinationName;

            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                if (destination.IsAvailable)
                {
                    // Capture destination in closure for onClick
                    TravelDestinationData captured = destination;
                    button.onClick.AddListener(() => OnDestinationButtonClicked(captured));
                    button.interactable = true;
                }
                else
                {
                    // Show grayed out — destination not available
                    button.interactable = false;

                    if (label != null)
                        label.text = $"{destination.DestinationName} (Unavailable)";
                }
            }
        }
    }

    /// <summary>
    /// Destroys all previously spawned destination buttons and clears the list.
    /// </summary>
    private void ClearButtons()
    {
        foreach (var btn in _spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }

        _spawnedButtons.Clear();
    }

    /// <summary>
    /// Called when the player clicks a destination button.
    /// Invokes the callback registered by NPCTraveler.
    /// </summary>
    private void OnDestinationButtonClicked(TravelDestinationData destination)
    {
        if (_onDestinationSelected == null)
        {
            Debug.LogWarning("[TravelMenuUI] OnDestinationButtonClicked: no callback registered.");
            return;
        }

        _onDestinationSelected.Invoke(destination);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private void ValidateSetup()
    {
        if (_menuPanel == null)
            Debug.LogWarning("[TravelMenuUI] _menuPanel is not assigned. Drag the root panel GameObject here.");

        if (_destinationListContainer == null)
            Debug.LogWarning("[TravelMenuUI] _destinationListContainer is not assigned. Drag the list container Transform here.");

        if (_destinationButtonPrefab == null)
            Debug.LogWarning("[TravelMenuUI] _destinationButtonPrefab is not assigned. Assign a prefab with Button + Text components.");

        if (_closeButton == null)
            Debug.LogWarning("[TravelMenuUI] _closeButton is not assigned. Drag the Close Button here.");
    }
}
