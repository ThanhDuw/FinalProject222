using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ data cho credits sequence.
/// Cho phép designer config credits content mà không cần sửa code.
/// 
/// Sử dụng: Create → Game → Credits Data
/// </summary>
[CreateAssetMenu(fileName = "CreditsData", menuName = "Game/Credits Data")]
public class CreditsData : ScriptableObject
{
    #region Data Structures
    
    /// <summary>
    /// Định nghĩa một mục credit (role + danh sách tên)
    /// Ví dụ: "Game Design" - ["Player 1", "Player 2"]
    /// </summary>
    [System.Serializable]
    public class CreditEntry
    {
        [Header("Content")]
        [Tooltip("Vai trò/chức danh (VD: Game Design, Programming)")]
        public string role;
        
        [Tooltip("Danh sách tên người trong vai trò này")]
        public List<string> names = new List<string>();
        
        [Header("Styling")]
        [Tooltip("Font size cho role title")]
        public int roleFontSize = 32;
        
        [Tooltip("Font size cho names")]
        public int nameFontSize = 24;
        
        [Tooltip("Khoảng cách giữa entry này và entry tiếp theo")]
        public float spacingAfter = 40f;
    }
    
    #endregion
    
    #region Credits Content
    
    [Header("Credits Content")]
    [Tooltip("Danh sách tất cả credits entries (cuộn từ trên xuống)")]
    public List<CreditEntry> creditEntries = new List<CreditEntry>();
    
    #endregion
    
    #region Animation Settings
    
    [Header("Animation Settings")]
    [Tooltip("Tốc độ cuộn (pixels per second)")]
    [Range(10f, 200f)]
    public float rollSpeed = 50f;
    
    [Tooltip("Delay trước khi bắt đầu cuộn (seconds)")]
    [Range(0f, 5f)]
    public float startDelay = 1f;
    
    [Tooltip("Tự động skip sau bao lâu (seconds). 0 = không auto skip")]
    [Range(0f, 120f)]
    public float autoSkipDuration = 30f;
    
    #endregion
    
    #region Visual Settings
    
    [Header("Visual Settings")]
    [Tooltip("Màu chữ cho role titles")]
    public Color roleColor = Color.white;
    
    [Tooltip("Màu chữ cho names")]
    public Color nameColor = new Color(0.8f, 0.8f, 0.8f);
    
    [Tooltip("Màu background overlay")]
    public Color backgroundColor = new Color(0, 0, 0, 0.9f);
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validate data trong Editor
    /// </summary>
    private void OnValidate()
    {
        if (creditEntries == null || creditEntries.Count == 0)
        {
            Debug.LogWarning("[CreditsData] Danh sách credits đang trống! Vui lòng thêm ít nhất một mục.");
        }

        if (rollSpeed <= 0f) rollSpeed = 50f;
        if (startDelay < 0f) startDelay = 0f;
        if (autoSkipDuration < 0f) autoSkipDuration = 0f;
    }
    
    #endregion
}
