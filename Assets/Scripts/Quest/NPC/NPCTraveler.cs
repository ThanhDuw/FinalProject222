using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPCTraveler — NPC Controller (Travel System)
///
/// Attach to any NPC that offers map travel (e.g., Peasant NPC).
///
/// Flow:
///   - Player walks into trigger radius  -> _isPlayerInRange = true, prompt blinks
///   - Player presses E                  -> OpenTravelMenu()
///   - TravelMenuUI shows destinations   -> Player selects one
///   - OnDestinationSelected()           -> TravelManager.TravelTo(destination)
///   - Player walks away                 -> CloseTravelMenu() auto-called, prompt hidden
///
/// Setup requirements:
///   1. CapsuleCollider (isTrigger = true) on this GameObject
///   2. Player GameObject tagged "Player"
///   3. Assign TravelMenuUI reference in Inspector
///   4. Populate availableDestinations list with TravelDestinationData ScriptableObjects
///   5. (Optional) Assign interactPrompt — a child world-space "E" label GameObject
///
/// Dependency flow (per CLAUDE.md):
///   NPCTraveler -> TravelMenuUI -> TravelManager -> SceneManager
///
/// NOTE: _travelMenuUI is typed as MonoBehaviour temporarily until TravelMenuUI
///       is created in Step 8. It will be replaced with the concrete type then.
/// </summary>
public class NPCTraveler : MonoBehaviour
{
    // ── Inspector — NPC Info ──────────────────────────────────────────────────

    [Header("NPC Info")]
    [SerializeField] private string _npcName = "Peasant";

    // ── Inspector — Travel Destinations ──────────────────────────────────────

    [Header("Travel Destinations")]
    [Tooltip("List of maps this NPC can send the player to. Assign TravelDestinationData assets.")]
    [SerializeField] private List<TravelDestinationData> _availableDestinations = new List<TravelDestinationData>();

    // ── Inspector — Interaction ───────────────────────────────────────────────

    [Header("Interaction")]
    [SerializeField] private float   _interactionRadius = 2f;
    [SerializeField] private KeyCode _interactKey       = KeyCode.E;

    [Header("Interact Prompt")]
    [Tooltip("Child world-space GameObject with an 'E' label — shown and blinks when player is in range.")]
    [SerializeField] private GameObject _interactPrompt;

    // ── Inspector — UI Reference ──────────────────────────────────────────────

    [Header("UI Reference")]
    [Tooltip("Reference to the TravelMenuUI component in the scene Canvas. (Will be typed as TravelMenuUI in Step 8)")]
    [SerializeField] private MonoBehaviour _travelMenuUI;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool  _isPlayerInRange;
    private bool  _isMenuOpen;
    private float _blinkTimer;

    // Blink timing constants — same pattern as NPCQuestDialog
    private const float BlinkOnDuration  = 0.30f;
    private const float BlinkCycleDuration = 0.45f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        ValidateSetup();

        // Hide prompt until player enters range
        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
    }

    private void Update()
    {
        HandlePromptBlink();
        HandleInteractInput();
    }

    private void OnDestroy()
    {
        // Guard: close menu cleanly if NPC is destroyed mid-interaction
        if (_isMenuOpen)
            CloseTravelMenu();
    }

    // ── Trigger Detection ─────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
            _isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            CloseTravelMenu();
        }
    }

    // ── Travel Menu Control ───────────────────────────────────────────────────

    /// <summary>
    /// Opens the Travel Menu UI and populates it with available destinations.
    /// Called when player presses E while in range.
    /// </summary>
    public void OpenTravelMenu()
    {
        if (_isMenuOpen) return;

        if (_travelMenuUI == null)
        {
            Debug.LogWarning($"[NPCTraveler] '{name}': TravelMenuUI reference is not assigned.");
            return;
        }

        if (_availableDestinations == null || _availableDestinations.Count == 0)
        {
            Debug.LogWarning($"[NPCTraveler] '{name}': No destinations configured.");
            return;
        }

        _isMenuOpen = true;

        // Cast to ITravelMenu interface and call Show
        // Will be updated to TravelMenuUI concrete type in Step 8
        var menu = GetTravelMenuUI();
        if (menu != null)
            menu.Show(_availableDestinations, OnDestinationSelected);
    }

    /// <summary>
    /// Closes the Travel Menu UI.
    /// Called when player walks away or cancels.
    /// </summary>
    public void CloseTravelMenu()
    {
        if (!_isMenuOpen) return;

        _isMenuOpen = false;

        var menu = GetTravelMenuUI();
        if (menu != null)
            menu.Hide();
    }

    /// <summary>
    /// Callback received from TravelMenuUI when player selects a destination.
    /// Triggers the actual travel via TravelManager.
    /// </summary>
    /// <param name="destination">The selected TravelDestinationData.</param>
    public void OnDestinationSelected(TravelDestinationData destination)
    {
        if (destination == null)
        {
            Debug.LogWarning($"[NPCTraveler] '{name}': OnDestinationSelected received null destination.");
            return;
        }

        // Close menu before travel so it doesn't persist across scene load
        CloseTravelMenu();

        if (TravelManager.Instance == null)
        {
            Debug.LogWarning($"[NPCTraveler] '{name}': TravelManager.Instance is null. Cannot travel.");
            return;
        }

        Debug.Log($"[NPCTraveler] '{_npcName}' sending player to '{destination.DestinationName}'.");
        TravelManager.Instance.TravelTo(destination);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Handles the E key press to open or close the travel menu.
    /// </summary>
    private void HandleInteractInput()
    {
        if (!_isPlayerInRange) return;

        if (Input.GetKeyDown(_interactKey))
        {
            if (!_isMenuOpen) OpenTravelMenu();
            else              CloseTravelMenu();
        }
    }

    /// <summary>
    /// Blinks the interact prompt while player is in range and menu is closed.
    /// Hides prompt when player is out of range or menu is open.
    /// </summary>
    private void HandlePromptBlink()
    {
        if (_interactPrompt == null) return;

        if (_isPlayerInRange && !_isMenuOpen)
        {
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= BlinkCycleDuration) _blinkTimer = 0f;
            _interactPrompt.SetActive(_blinkTimer < BlinkOnDuration);
        }
        else
        {
            _interactPrompt.SetActive(false);
            _blinkTimer = 0f;
        }
    }

    /// <summary>
    /// Retrieves the TravelMenuUI interface from the MonoBehaviour reference.
    /// Will be replaced with a direct cast once TravelMenuUI is created in Step 8.
    /// </summary>
    private ITravelMenu GetTravelMenuUI()
    {
        if (_travelMenuUI is ITravelMenu menu)
            return menu;

        Debug.LogWarning($"[NPCTraveler] '{name}': Assigned _travelMenuUI does not implement ITravelMenu.");
        return null;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private void ValidateSetup()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning($"[NPCTraveler] '{name}': Missing Collider. Add CapsuleCollider with isTrigger = true.");
        else if (!col.isTrigger)
            Debug.LogWarning($"[NPCTraveler] '{name}': Collider.isTrigger is false. Set isTrigger = true.");

        if (GameObject.FindWithTag("Player") == null)
            Debug.LogWarning($"[NPCTraveler] '{name}': No GameObject with tag 'Player' found in scene.");

        if (_travelMenuUI == null)
            Debug.LogWarning($"[NPCTraveler] '{name}': TravelMenuUI is not assigned. Drag TravelMenuUI component here.");

        if (_availableDestinations == null || _availableDestinations.Count == 0)
            Debug.LogWarning($"[NPCTraveler] '{name}': No TravelDestinationData assets assigned to availableDestinations.");
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _interactionRadius);
    }
}
