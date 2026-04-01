using UnityEngine;
using TMPro;

/// <summary>
/// Utility class để build UI cho credit items.
/// Static helper methods để tạo và populate credit entries.
/// 
/// Sử dụng:
/// - Được gọi bởi CreditsSequenceManager khi build content
/// - Tạo UI elements từ prefab hoặc code
/// </summary>
public static class CreditItemBuilder
{
    #region Public Static Methods
    
    /// <summary>
    /// Populate một credit item prefab với data
    /// </summary>
    /// <param name="itemObject">GameObject đã được instantiate từ prefab</param>
    /// <param name="entry">Credit entry data</param>
    /// <param name="creditsData">Credits data cho styling settings</param>
    public static void PopulateItem(
        GameObject itemObject, 
        CreditsData.CreditEntry entry, 
        CreditsData creditsData)
    {
        if (itemObject == null || entry == null || creditsData == null) return;

        TextMeshProUGUI roleText = itemObject.transform.Find("RoleText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI namesText = itemObject.transform.Find("NamesText")?.GetComponent<TextMeshProUGUI>();
        
        if (roleText != null)
        {
            roleText.text = entry.role;
            roleText.fontSize = entry.roleFontSize;
            roleText.color = creditsData.roleColor;
        }
        
        if (namesText != null)
        {
            namesText.text = FormatNamesList(entry.names);
            namesText.fontSize = entry.nameFontSize;
            namesText.color = creditsData.nameColor;
        }
    }
    
    /// <summary>
    /// Tạo credit item từ code (không dùng prefab)
    /// Alternative method nếu không muốn dùng prefab
    /// </summary>
    public static GameObject CreateCreditItemProgrammatically(
        Transform parent,
        CreditsData.CreditEntry entry,
        CreditsData creditsData)
    {
        if (entry == null || creditsData == null) return null;

        GameObject itemObject = new GameObject($"CreditItem_{entry.role}");
        itemObject.transform.SetParent(parent, false);
        
        RectTransform itemRect = itemObject.AddComponent<RectTransform>();
        var layoutGroup = itemObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        
        // Tạo khoảng trống dưới item
        layoutGroup.padding = new RectOffset(0, 0, 0, (int)entry.spacingAfter);
        
        GameObject roleObj = CreateTextElement(itemObject.transform, "RoleText");
        GameObject namesObj = CreateTextElement(itemObject.transform, "NamesText");
        
        // Tái sử dụng hàm Populate
        PopulateItem(itemObject, entry, creditsData);
        
        return itemObject;
    }
    
    #endregion
    
    #region Private Helper Methods
    
    /// <summary>
    /// Tạo một TextMeshPro element
    /// </summary>
    private static GameObject CreateTextElement(Transform parent, string name)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.sizeDelta = new Vector2(800, 100); 
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        
        return textObj;
    }
    
    /// <summary>
    /// Format danh sách names thành string
    /// </summary>
    private static string FormatNamesList(System.Collections.Generic.List<string> names)
    {
        if (names == null || names.Count == 0) return string.Empty;
        return string.Join("\n", names);
    }
    
    #endregion
    
    #region Validation Helpers
    
    /// <summary>
    /// Validate credit item structure
    /// </summary>
    public static bool ValidateCreditItemPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[CreditItemBuilder] Prefab bị null!");
            return false;
        }
        
        if (prefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("[CreditItemBuilder] Prefab không có RectTransform!");
            return false;
        }
        
        if (prefab.transform.Find("RoleText")?.GetComponent<TextMeshProUGUI>() == null)
        {
            Debug.LogWarning("[CreditItemBuilder] Prefab thiếu Game Object 'RoleText' có component TextMeshProUGUI!");
        }

        if (prefab.transform.Find("NamesText")?.GetComponent<TextMeshProUGUI>() == null)
        {
            Debug.LogWarning("[CreditItemBuilder] Prefab thiếu Game Object 'NamesText' có component TextMeshProUGUI!");
        }
        
        return true;
    }
    
    #endregion
}
