using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller cho rolling animation của credits.
/// Điều khiển smooth scrolling từ đầu đến cuối content.
/// 
/// Dependencies:
/// - ScrollRect component
/// - RectTransform của content
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class CreditsScrollController : MonoBehaviour
{
    #region Inspector References
    
    [Header("References")]
    [Tooltip("ScrollRect component (auto-assigned)")]
    [SerializeField] private ScrollRect scrollRect;
    
    [Tooltip("Content RectTransform bên trong ScrollRect")]
    [SerializeField] private RectTransform contentRect;
    
    #endregion
    
    #region Settings
    
    [Header("Scroll Settings")]
    [Tooltip("Tốc độ cuộn hiện tại (pixels/second)")]
    [SerializeField] private float currentSpeed = 50f;
    
    [Tooltip("Có smooth lerp không")]
    [SerializeField] private bool useSmoothScroll = true;
    
    [Tooltip("Smooth factor cho lerp")]
    [Range(0.1f, 1f)]
    [SerializeField] private float smoothFactor = 0.5f;
    
    #endregion
    
    #region Runtime State
    
    private bool isScrolling = false;
    private float targetScrollPosition;
    private float currentScrollPosition;
    
    // Cached values
    private float contentHeight;
    private float viewportHeight;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (contentRect == null && scrollRect != null) contentRect = scrollRect.content;
    }
    
    private void Update()
    {
        if (!isScrolling || scrollRect == null || contentRect == null) return;

        CalculateScrollPosition();

        if (useSmoothScroll)
        {
            ApplySmoothScroll();
        }
        else
        {
            currentScrollPosition = targetScrollPosition;
            scrollRect.verticalNormalizedPosition = currentScrollPosition;
        }

        if (IsScrollComplete())
        {
            isScrolling = false;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Bắt đầu rolling animation
    /// </summary>
    /// <param name="speed">Tốc độ cuộn (pixels/second)</param>
    public void StartScroll(float speed)
    {
        if (scrollRect == null || contentRect == null) return;
        
        currentSpeed = speed;
        contentHeight = contentRect.rect.height;
        viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : GetComponent<RectTransform>().rect.height;
        
        ResetToTop();
        isScrolling = true;
    }
    
    /// <summary>
    /// Dừng rolling animation
    /// </summary>
    public void StopScroll()
    {
        isScrolling = false;
    }
    
    /// <summary>
    /// Thay đổi tốc độ đang cuộn
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        currentSpeed = newSpeed;
    }
    
    /// <summary>
    /// Reset về vị trí đầu
    /// </summary>
    public void ResetToTop()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            currentScrollPosition = 1f;
            targetScrollPosition = 1f;
        }
    }
    
    /// <summary>
    /// Kiểm tra đã cuộn hết chưa
    /// </summary>
    public bool IsScrollComplete()
    {
        if (scrollRect == null) return true;
        return scrollRect.verticalNormalizedPosition <= 0.001f;
    }
    
    /// <summary>
    /// Lấy progress hiện tại (0-1)
    /// </summary>
    public float GetScrollProgress()
    {
        if (scrollRect == null) return 1f;
        return 1f - scrollRect.verticalNormalizedPosition; // Từ 0 đến 1
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Tính toán scroll position mới dựa trên speed và deltaTime
    /// </summary>
    private void CalculateScrollPosition()
    {
        // Cập nhật lại dimension phòng trường hợp layout thay đổi
        contentHeight = contentRect.rect.height;
        float totalScrollDistance = Mathf.Max(1f, contentHeight - viewportHeight);
        
        // Tốc độ (pixels/sec) quy đổi sang normalizedPosition giảm đi
        float normalizedSpeed = currentSpeed / totalScrollDistance;
        
        targetScrollPosition -= normalizedSpeed * Time.deltaTime;
        targetScrollPosition = Mathf.Clamp01(targetScrollPosition);
    }
    
    /// <summary>
    /// Apply smooth lerp nếu enabled
    /// </summary>
    private void ApplySmoothScroll()
    {
        // Lerp với hệ số smoothFactor để tạo cảm giác trôi mượt
        currentScrollPosition = Mathf.Lerp(currentScrollPosition, targetScrollPosition, Time.deltaTime * (10f / Mathf.Max(0.1f, smoothFactor)));
        scrollRect.verticalNormalizedPosition = currentScrollPosition;
    }
    
    #endregion
    
    #region Validation
    
    private void OnValidate()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (contentRect == null && scrollRect != null) contentRect = scrollRect.content;
    }
    
    #endregion
}
