using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TravelMenuUI — Controller UI (Hệ thống Dịch chuyển)
///
/// Sử dụng các nút đã được gán sẵn trực tiếp trong cấu trúc scene (hierarchy).
/// Không khởi tạo (Instantiate) khi chạy — các nút luôn hiện diện, được ẩn/hiện tùy theo tính khả dụng.
///
/// Thực thi ITravelMenu để NPCTraveler có thể gọi Show/Hide.


/// </summary>
public class TravelMenuUI : MonoBehaviour, ITravelMenu
{
    // Thiết lập Inspector - Bảng điều khiển (Panel)

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

    // Các biến khi chạy (Runtime)

    private Action<TravelDestinationData> _onDestinationSelected;

    // Vòng đời (Lifecycle)

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

        // Xóa tất cả các sự kiện (listener) của nút để tránh tham chiếu cũ
        ClearButtonListeners();
    }

    // Thực thi interface ITravelMenu

    /// <summary>
    /// Hiển thị menu và liên kết từng nút đã tạo sẵn với điểm đến tương ứng.
    /// Các nút không khớp với điểm đến nào sẽ bị ẩn.
    /// </summary>
    public void Show(List<TravelDestinationData> destinations, Action<TravelDestinationData> onSelected)
    {
        if (destinations == null || destinations.Count == 0)
        {
            Debug.LogWarning("[TravelMenuUI] Show called with null or empty destinations list.");
            return;
        }

        _onDestinationSelected = onSelected;

        // Liên kết mỗi nút với điểm đến tương ứng của nó
        for (int i = 0; i < _destinationButtons.Count; i++)
        {
            Button btn = _destinationButtons[i];
            if (btn == null) continue;

            if (i < destinations.Count && destinations[i] != null)
            {
                TravelDestinationData dest = destinations[i];

                // Cập nhật văn bản hiển thị trên nút
                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = dest.IsAvailable
                        ? dest.DestinationName
                        : $"{dest.DestinationName} (Unavailable)";

                // Gán sự kiện onClick — xóa các sự kiện cũ trước để tránh bị gọi trùng lặp
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
                // Không có điểm đến cho ô này — ẩn nút đi
                btn.gameObject.SetActive(false);
            }
        }

        if (_titleText != null)
            _titleText.text = _menuTitle;

        if (_menuPanel != null)
            _menuPanel.SetActive(true);
    }

    /// <summary>
    /// Ẩn phần menu và xóa tất cả các sự kiện của nút.
    /// </summary>
    public void Hide()
    {
        if (_menuPanel != null)
            _menuPanel.SetActive(false);

        ClearButtonListeners();
        _onDestinationSelected = null;
    }

    // Các hàm bổ trợ (Helpers)

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

    // Xác thực (Validation)

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
