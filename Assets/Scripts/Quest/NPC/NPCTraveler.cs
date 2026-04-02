using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPCTraveler — Điều khiển NPC (Hệ thống Du lịch)
///
/// Gắn vào bất kỳ NPC nào cung cấp khả năng dịch chuyển bản đồ (ví dụ: NPC Dân làng).
///
/// Luồng hoạt động:
///   - Người chơi đi vào vùng kích hoạt -> _isPlayerInRange = true, lời nhắc nhấp nháy
///   - Người chơi nhấn E                  -> OpenTravelMenu()
///   - TravelMenuUI hiển thị các điểm đến -> Người chơi chọn một điểm
///   - OnDestinationSelected()           -> TravelManager.TravelTo(destination)
///   - Người chơi rời đi                 -> Tự động gọi CloseTravelMenu(), ẩn lời nhắc
///
/// Yêu cầu thiết lập:
///   1. Thêm CapsuleCollider (isTrigger = true) vào GameObject này
///   2. Đặt tag "Player" cho GameObject người chơi
///   3. Gán tham chiếu TravelMenuUI
///   4. Thêm các ScriptableObject TravelDestinationData vào danh sách availableDestinations
///   5. (Tùy chọn) Gán interactPrompt - một GameObject chứa nhãn "E" trong thế giới 3D làm con
///
/// Luồng phụ thuộc:
///   NPCTraveler -> TravelMenuUI -> TravelManager -> SceneManager
/// </summary>
public class NPCTraveler : MonoBehaviour
{
    [Header("NPC Info")]
    [SerializeField] private string _npcName = "Peasant";

    [Header("Travel Destinations")]
    [Tooltip("Danh sách các bản đồ mà NPC này có thể đưa người chơi tới. Gán các asset TravelDestinationData.")]
    [SerializeField] private List<TravelDestinationData> _availableDestinations = new List<TravelDestinationData>();

    [Header("Interaction")]
    [SerializeField] private float   _interactionRadius = 2f;

    [Header("Interact Prompt")]
    [Tooltip("GameObject con trong không gian thế giới với nhãn E - hiển thị và nhấp nháy khi người chơi trong phạm vi.")]
    [SerializeField] private GameObject _interactPrompt;

    [Header("UI Reference")]
    [Tooltip("Tham chiếu đến thành phần TravelMenuUI trong Canvas của cảnh.")]
    [SerializeField] private TravelMenuUI _travelMenuUI;

    private bool  _isPlayerInRange;
    private bool  _isMenuOpen;
    private float _blinkTimer;

    private const float BlinkOnDuration      = 0.30f;
    private const float BlinkCycleDuration   = 0.45f;

    private void Start()
    {
        ValidateSetup();

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
        if (_isMenuOpen)
            CloseTravelMenu();
    }

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

    /// <summary>
    /// Mở giao diện Menu Du lịch và nạp danh sách các điểm đến khả dụng.
    /// Được gọi khi người chơi nhấn E trong phạm vi.
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
        _travelMenuUI.Show(_availableDestinations, OnDestinationSelected);
    }

    /// <summary>
    /// Đóng giao diện Menu Du lịch.
    /// Được gọi khi người chơi đi xa hoặc hủy bỏ.
    /// </summary>
    public void CloseTravelMenu()
    {
        if (!_isMenuOpen) return;

        _isMenuOpen = false;
        _travelMenuUI.Hide();
    }

    /// <summary>
    /// Callback nhận từ TravelMenuUI khi người chơi chọn một điểm đến.
    /// Kích hoạt quá trình dịch chuyển thực tế thông qua TravelManager.
    /// </summary>
    public void OnDestinationSelected(TravelDestinationData destination)
    {
        if (destination == null)
        {
            Debug.LogWarning($"[NPCTraveler] '{name}': OnDestinationSelected received null destination.");
            return;
        }

        // Đóng menu trước khi dịch chuyển để không tồn tại sang Scene khác
        CloseTravelMenu();

        if (TravelManager.Instance == null)
        {
            Debug.LogWarning($"[NPCTraveler] '{name}': TravelManager.Instance is null. Cannot travel.");
            return;
        }

        Debug.Log($"[NPCTraveler] '{_npcName}' sending player to '{destination.DestinationName}'.");
        TravelManager.Instance.TravelTo(destination);
    }

    private void HandleInteractInput()
    {
        if (!_isPlayerInRange) return;

        if (GameInput.Instance != null && GameInput.Instance.InteractPressed)
        {
            if (!_isMenuOpen) OpenTravelMenu();
            else              CloseTravelMenu();
        }
    }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _interactionRadius);
    }
}
