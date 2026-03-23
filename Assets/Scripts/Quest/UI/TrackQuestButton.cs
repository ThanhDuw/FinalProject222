using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the Track_Quest button GameObject.
/// Bridges the UI Button onClick event to QuestTrackerManager.TogglePanel().
/// Resolves QuestTrackerManager at runtime so it works across all scenes
/// where PlayerCore (and QuestTrackerManager) is present.
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
