using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks runtime progress of all active quests.
/// Notifies UI layer via OnProgressUpdated.
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
        if (activeProgresses.ContainsKey(quest.questID)) return; // already tracking

        var progress = new QuestProgress
        {
            questData = quest,
            state = QuestState.Active,
            objectiveCounts = new Dictionary<string, int>()
        };

        // initialize counts to zero for each objective
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

    /// <summary>Called by ObjectiveSystem when an event matches an objective.</summary>
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

        // Notify listeners about progress change
        OnProgressUpdated?.Invoke(progress);

        // Broadcast to other systems that a quest progressed (QuestManager listens to check completion)
        GameEvents.RaiseQuestProgressChanged(progress.questData.questID);
    }

    // Public helper so other systems (like QuestManager) can request a notification
    // without trying to invoke the event directly (invoking events from outside the declaring type is not allowed).
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
