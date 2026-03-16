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
    private void Start()
    {
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
        var tracker = FindFirstObjectByType<QuestTrackerManager>();
        if (tracker != null)
            tracker.TogglePanel();
        else
            Debug.LogWarning("[TrackQuestButton] QuestTrackerManager not found in scene.");
    }
}
