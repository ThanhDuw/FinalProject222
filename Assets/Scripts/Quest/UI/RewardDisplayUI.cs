using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Quản lý lộ trình hiển thị 3D reward:
/// - Lắng nghe event từ GameEvents.OnShowReward
/// - Spawn prefab lên layer RewardDisplay (để nó không hiện trong Scene Cam chính)
/// - Điều khiển RewardCamera chĩa vào object
/// - Hiển thị ảnh (RenderTexture) từ camera đó qua RawImage
/// - Bật nút tương tác, dọn dẹp sau khi nhận.
/// </summary>
public class RewardDisplayUI : MonoBehaviour
{
    [Header("Camera Setup")]
    [SerializeField] private Camera rewardCamera;        // Camera chuyên trách
    [SerializeField] private RenderTexture renderTexture; // RT_RewardDisplay
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, -100, 0); // Vị trí giấu kín
    
    [Header("UI References")]
    [SerializeField] private GameObject rewardPanel;     // Panel bao ngoài lớn
    [SerializeField] private RawImage rewardRawImage;    // Chứa RenderTexture (hiển thị mô hình 3D)
    [SerializeField] private Button closeButton;         // Nút "Nhận Thưởng!"
    [SerializeField] private Text rewardTitleText;       // Chữ (tùy chọn) hiển thị tên báu vật
    [Tooltip("Thêm component Canvas Group vào RewardPanel để hỗ trợ hiệu ứng mờ (Fade).")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    
    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    
    private GameObject _currentRewardInstance;
    
    private void OnEnable()  
    { 
        GameEvents.OnShowReward += ShowReward; 
        if (closeButton != null)
            closeButton.onClick.AddListener(HideReward);
    }

    private void OnDisable() 
    { 
        GameEvents.OnShowReward -= ShowReward; 
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HideReward);
    }
    
    private void Start()
    {
        // Khởi tạo trạng thái ẩn qua CanvasGroup, giữ GameObject Active để script vẫn chạy
        SetPanelState(false);
    }

    private void SetPanelState(bool active)
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = active ? 1f : 0f;
            panelCanvasGroup.interactable = active;
            panelCanvasGroup.blocksRaycasts = active;
        }
        
        // Đảm bảo chính nó luôn Active để lắng nghe sự kiện
        if (rewardPanel != null && !rewardPanel.activeSelf)
            rewardPanel.SetActive(true);
    }

    public void ShowReward(GameObject prefab)
    {
        if (prefab == null) return;
        
        // --- 1. Sinh prefab vào scene tại vị trí định sẵn ---
        _currentRewardInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        
        // --- 2. Gán Layer "RewardDisplay" nếu có ---
        int rewardLayer = LayerMask.NameToLayer("RewardDisplay");
        if (rewardLayer != -1)
        {
            SetLayerRecursively(_currentRewardInstance, rewardLayer);
        }
        else
        {
            Debug.LogWarning("[RewardDisplayUI] Vui lòng tạo Layer tên 'RewardDisplay' trong Edit -> Project Settings.");
        }

        // --- 3. Điều chỉnh Camera ---
        if (rewardCamera != null)
        {
            // Đưa camera lại gần vật phẩm (cách khoảng 2.5 đơn vị theo trục Z và hơi cao hơn một chút)
            rewardCamera.transform.position = spawnPosition + new Vector3(0, 0.5f, 2.5f);
            rewardCamera.transform.LookAt(spawnPosition);
            
            if (renderTexture != null)
                rewardCamera.targetTexture = renderTexture;
        }

        // --- 4. Gắn kết cấu vào hình ảnh thô ---
        if (rewardRawImage != null && renderTexture != null)
        {
            rewardRawImage.texture = renderTexture;
        }

        // --- 5. Chín tên (lấy từ prefab) ---
        if (rewardTitleText != null)
        {
            rewardTitleText.text = "Bạn Nhận Được: " + prefab.name.Replace("(Clone)", "");
        }

        // --- 6. Hiện Panel với hiệu ứng (Fade) ---
        if (rewardPanel != null)
        {
            // Đảm bảo panel được kích hoạt
            rewardPanel.SetActive(true);
            
            // Hiện dần qua CanvasGroup
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
                StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
            }

            // Xử lý chuột
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    public void HideReward()
    {
        if (panelCanvasGroup != null && rewardPanel != null && rewardPanel.activeSelf)
        {
            StartCoroutine(FadePanel(1f, 0f, 0.2f, () => 
            {
                Cleanup();
            }));
        }
        else
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (_currentRewardInstance != null)
            Destroy(_currentRewardInstance);
            
        if (rewardCamera != null)
            rewardCamera.targetTexture = null;
            
        if (rewardPanel != null)
        {
             // Không Deactivate GameObject để script vẫn nghe được Event
             // Chỉ cần ẩn qua CanvasGroup
             SetPanelState(false);
        }

        // Khoá lại trạng thái chuột (tuỳ hệ thống game của bạn setup)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        panelCanvasGroup.alpha = startAlpha;
        while (elapsed < duration)
        {
            // unscaledDeltaTime để animation vẫn chạy cả khi Time.timeScale = 0 (nếu game tạm dừng)
            elapsed += Time.unscaledDeltaTime; 
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        panelCanvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }
    
    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
