using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manager chính cho credits sequence.
/// Điều phối toàn bộ flow: show panel → build content → scroll → handle skip → cleanup
/// 
/// Architecture:
/// - Lắng nghe event từ RewardOverlayPanel
/// - Build credits UI từ CreditsData
/// - Điều khiển CreditsScrollController
/// - Raise events khi hoàn thành
/// </summary>
public class CreditsSequenceManager : MonoBehaviour
{
    #region Inspector References
    
    [Header("Data")]
    [Tooltip("ScriptableObject chứa credits content")]
    [SerializeField] private CreditsData creditsData;
    
    [Header("UI References")]
    [Tooltip("Root panel chứa toàn bộ credits UI")]
    [SerializeField] private GameObject creditsPanel;
    
    [Tooltip("Content parent cho credits items (inside ScrollRect)")]
    [SerializeField] private RectTransform contentParent;
    
    [Tooltip("Prefab template cho mỗi credit item")]
    [SerializeField] private GameObject creditItemPrefab;
    
    [Tooltip("Background overlay image")]
    [SerializeField] private UnityEngine.UI.Image backgroundOverlay;
    
    [Tooltip("Skip instruction text")]
    [SerializeField] private TMPro.TextMeshProUGUI skipText;
    
    [Header("Components")]
    [Tooltip("Scroll controller component")]
    [SerializeField] private CreditsScrollController scrollController;
    
    [Tooltip("Canvas group cho fade effects (optional)")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    #endregion
    
    #region Events
    
    [Header("Events")]
    [Tooltip("Triggered khi credits bắt đầu hiển thị")]
    public UnityEvent OnCreditsStarted;
    
    [Tooltip("Triggered khi credits hoàn thành (auto hoặc skip)")]
    public UnityEvent OnCreditsCompleted;
    
    [Tooltip("Triggered khi user skip credits")]
    public UnityEvent OnCreditsSkipped;
    
    #endregion
    
    #region Runtime State
    
    private bool isShowingCredits = false;
    private float skipTimer = 0f;
    private Coroutine creditsCoroutine;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (scrollController == null) scrollController = GetComponentInChildren<CreditsScrollController>();
        if (canvasGroup == null && creditsPanel != null) canvasGroup = creditsPanel.GetComponent<CanvasGroup>();
        // Fallback: lấy CanvasGroup trên chính GameObject này nếu creditsPanel chưa gán
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        // Auto-assign background overlay từ Image component của Panel nếu chưa gán
        if (backgroundOverlay == null && creditsPanel != null) 
        {
            backgroundOverlay = creditsPanel.GetComponent<UnityEngine.UI.Image>();
        }
        if (backgroundOverlay == null)
        {
            backgroundOverlay = GetComponent<UnityEngine.UI.Image>();
        }
        
        // Ẩn qua CanvasGroup thay vì SetActive để Update/Awake luôn chạy
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    private void OnEnable()
    {
        // Subscribe via Inspector UnityEvent (như bản kế hoạch)
    }
    
    private void OnDisable()
    {
    }
    
    private void Update()
    {
        if (isShowingCredits)
        {
            if (CheckSkipInput())
            {
                SkipCredits();
            }
        }
    }
    
    #endregion
    
    #region Public Methods - Main Control
    
    /// <summary>
    /// Hiển thị credits sequence
    /// Public method được gọi từ external events hoặc UI buttons
    /// </summary>
    public void ShowCredits()
    {
        if (creditsData == null) return;
        
        if (creditsCoroutine != null)
        {
            StopCoroutine(creditsCoroutine);
        }
        
        creditsCoroutine = StartCoroutine(CreditsSequenceCoroutine());
    }
    
    /// <summary>
    /// Ẩn credits và cleanup
    /// </summary>
    public void HideCredits()
    {
        if (scrollController != null) scrollController.StopScroll();
        ClearContent();
        isShowingCredits = false;
        
        // Ẩn qua CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// Skip credits ngay lập tức
    /// </summary>
    public void SkipCredits()
    {
        if (!isShowingCredits) return;
        
        if (creditsCoroutine != null)
        {
            StopCoroutine(creditsCoroutine);
            creditsCoroutine = null;
        }
        
        OnCreditsSkipped?.Invoke();
        HideCredits();
        OnCreditsCompleted?.Invoke();
    }
    
    #endregion
    
    #region Private Methods - Content Building
    
    /// <summary>
    /// Build toàn bộ credits UI từ CreditsData
    /// </summary>
    private void BuildCreditsContent()
    {
        ClearContent();
        
        if (creditsData.creditEntries == null) return;

        foreach (var entry in creditsData.creditEntries)
        {
            if (creditItemPrefab != null)
            {
                GameObject item = Instantiate(creditItemPrefab, contentParent);
                CreditItemBuilder.PopulateItem(item, entry, creditsData);
            }
            else
            {
                // Fallback tạo bằng code nếu không truyền Prefab
                CreditItemBuilder.CreateCreditItemProgrammatically(contentParent, entry, creditsData);
            }
        }
        
        // Force update layout để ScrollRect tính đúng kích thước Content
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
    }
    
    /// <summary>
    /// Xóa toàn bộ credits content
    /// </summary>
    private void ClearContent()
    {
        if (contentParent == null) return;
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    #endregion
    
    #region Private Methods - Sequence Control
    
    /// <summary>
    /// Coroutine chính cho credits sequence
    /// </summary>
    private IEnumerator CreditsSequenceCoroutine()
    {
        isShowingCredits = true;
        skipTimer = 0f;
        
        // Bắt đầu hiện panel: set blocksRaycasts trước, alpha sẽ do FadeIn tăng dần
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
        }
        
        if (backgroundOverlay != null) backgroundOverlay.color = creditsData.backgroundColor;
        
        yield return FadeIn();
        
        BuildCreditsContent();
        
        // Chờ 1 frame để TextMeshPro tính xong preferredSize
        yield return null;
        // Rebuild layout lần 2 sau khi TMP đã xác định kích thước text thực tế
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
        
        yield return new WaitForSeconds(creditsData.startDelay);
        
        OnCreditsStarted?.Invoke();
        
        if (scrollController != null)
        {
            scrollController.StartScroll(creditsData.rollSpeed);
            
            while (!scrollController.IsScrollComplete() && isShowingCredits)
            {
                if (creditsData.autoSkipDuration > 0)
                {
                    skipTimer += Time.deltaTime;
                    if (skipTimer >= creditsData.autoSkipDuration)
                    {
                        break;
                    }
                }
                
                // Allow player to speed up with input hold
                if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                {
                    scrollController.SetSpeed(creditsData.rollSpeed * 3f);
                }
                else
                {
                    scrollController.SetSpeed(creditsData.rollSpeed);
                }

                yield return null;
            }
        }
        
        yield return FadeOut();
        HideCredits();
        OnCreditsCompleted?.Invoke();
    }
    
    /// <summary>
    /// Fade in animation
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * 1.5f;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed);
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// Fade out animation
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * 1.5f;
                canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed);
                yield return null;
            }
        }
    }
    
    #endregion
    
    #region Private Methods - Input Handling
    
    /// <summary>
    /// Kiểm tra skip input
    /// </summary>
    private bool CheckSkipInput()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }
    
    #endregion
    
    #region Validation
    
    private void OnValidate()
    {
        if (scrollController == null) scrollController = GetComponentInChildren<CreditsScrollController>();
        if (canvasGroup == null && creditsPanel != null) canvasGroup = creditsPanel.GetComponent<CanvasGroup>();
    }
    
    #endregion
}
