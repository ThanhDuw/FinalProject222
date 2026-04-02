using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào GameObject của nút Track_Quest.
/// Kết nối sự kiện onClick của UI Button với QuestTrackerManager.TogglePanel().
/// Tự động tìm QuestTrackerManager lúc chạy (runtime) để hoạt động trên mọi cảnh
/// có chứa PlayerCore (và QuestTrackerManager).
/// </summary>
[RequireComponent(typeof(Button))]
public class TrackQuestButton : MonoBehaviour
{
private QuestTrackerManager _tracker;

    
private void Start()
    {
        _tracker = FindFirstObjectByType<QuestTrackerManager>();
        if (_tracker == null)
            Debug.LogWarning("[TrackQuestButton] QuestTrackerManager not found in scene.");
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.RemoveListener(OnClick);
    }

private void OnClick()
    {
        if (_tracker != null)
            _tracker.TogglePanel();
    }
}
