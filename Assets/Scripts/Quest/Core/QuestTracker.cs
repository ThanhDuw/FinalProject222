using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Theo dõi tiến trình lúc chạy của tất cả các nhiệm vụ đang kích hoạt.
/// Thông báo cho lớp UI thông qua OnProgressUpdated.
/// </summary>
public class QuestTracker : MonoBehaviour
{
    private Dictionary<string, QuestProgress> activeProgresses = new Dictionary<string, QuestProgress>();

    public event Action<QuestProgress> OnProgressUpdated;
    public event Action<QuestProgress> OnQuestTrackingStarted;
    public event Action<string>        OnQuestTrackingStopped;

    public void TrackQuest(QuestData quest)
    {
        if (quest == null || string.IsNullOrEmpty(quest.questID)) return;
        if (activeProgresses.ContainsKey(quest.questID)) return; // đã theo dõi

        var progress = new QuestProgress
        {
            questData = quest,
            state = QuestState.Active,
            objectiveCounts = new Dictionary<string, int>()
        };

        // khởi tạo đếm về không cho từng mục tiêu
        foreach (var obj in quest.objectives)
        {
            if (obj == null || string.IsNullOrEmpty(obj.objectiveID)) continue;
            progress.objectiveCounts[obj.objectiveID] = 0;
        }

        activeProgresses[quest.questID] = progress;
        OnQuestTrackingStarted?.Invoke(progress);
        OnProgressUpdated?.Invoke(progress);
    }

    public void UntrackQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;
        if (!activeProgresses.ContainsKey(questID)) return;

        activeProgresses.Remove(questID);
        OnQuestTrackingStopped?.Invoke(questID);
    }

    /// <summary>Được gọi bởi ObjectiveSystem khi một sự kiện khớp với một mục tiêu.</summary>
    public void UpdateObjective(string questID, string objectiveID, int amount)
    {
        if (string.IsNullOrEmpty(questID) || string.IsNullOrEmpty(objectiveID)) return;
        if (!activeProgresses.TryGetValue(questID, out var progress)) return;
        if (progress.state != QuestState.Active) return;

        if (!progress.objectiveCounts.ContainsKey(objectiveID))
            progress.objectiveCounts[objectiveID] = 0;

        int current = progress.objectiveCounts[objectiveID];
        int updated = Mathf.Clamp(current + amount, 0, int.MaxValue);
        if (updated == current) return;

        progress.objectiveCounts[objectiveID] = updated;

        // Thông báo cho các hệ thống đang lắng nghe về sự thay đổi tiến trình
        OnProgressUpdated?.Invoke(progress);

        // Phát sự kiện cho các hệ thống khác biết tiến trình nhiệm vụ (QuestManager lắng nghe để kiểm tra hoàn thành)
        GameEvents.RaiseQuestProgressChanged(progress.questData.questID);
    }

    // Helper công khai để các hệ thống khác (như QuestManager) có thể yêu cầu thông báo
    // mà không cần cố gọi sự kiện trực tiếp (gọi sự kiện từ ngoài lớp khai báo là không được phép).
    public void NotifyProgressUpdated(QuestProgress progress)
    {
        OnProgressUpdated?.Invoke(progress);
    }

    public QuestProgress GetProgress(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return null;
        activeProgresses.TryGetValue(questID, out var progress);
        return progress;
    }

    public IEnumerable<QuestProgress> GetAllActiveProgresses()
    {
        return activeProgresses.Values;
    }
}

[Serializable]
public class QuestProgress
{
    public QuestData questData;
    public Dictionary<string, int> objectiveCounts = new Dictionary<string, int>();
    public QuestState state;
}
