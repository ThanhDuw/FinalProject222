using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central coordinator — Singleton.
/// Reads QuestDatabase, orchestrates QuestTracker / ObjectiveSystem / SaveSystem.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static QuestManager Instance { get; private set; }

    // ── Inspector references ─────────────────────────────────────────────────
    [Header("Database")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Sub-systems")]
    [SerializeField] private QuestTracker    questTracker;
    [SerializeField] private ObjectiveSystem objectiveSystem;
    [SerializeField] private SaveSystem      saveSystem;

    // ── Runtime state ────────────────────────────────────────────────────────
    private Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();

    // Hold quests restored at startup so we can notify listeners after other Start() methods run
    private List<QuestData> deferredStartedNotifications = new List<QuestData>();

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action<QuestData> OnQuestStarted;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<QuestData> OnQuestFailed;

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Load saved quest states (and per-objective progress when available)
        SaveSystem.Wrapper wrapper = null;
        if (saveSystem != null)
        {
            wrapper = saveSystem.LoadQuestData();
        }

        // If we have wrapper data, restore states and detailed objective counts
        if (wrapper != null && wrapper.quests != null)
        {
            foreach (var saved in wrapper.quests)
            {
                if (saved == null || string.IsNullOrEmpty(saved.questID)) continue;
                questStates[saved.questID] = saved.state;

                // If quest was active when saved, re-create tracking and restore objective counts
                if (saved.state == QuestState.Active && questDatabase != null && questTracker != null)
                {
                    var q = questDatabase.GetQuestByID(saved.questID);
                    if (q != null)
                    {
                        questTracker.TrackQuest(q);

                        // Restore objective counts
                        var progress = questTracker.GetProgress(q.questID);
                        if (progress != null && saved.objectives != null)
                        {
                            foreach (var o in saved.objectives)
                            {
                                if (o == null || string.IsNullOrEmpty(o.objectiveID)) continue;
                                // single assignment (previous code duplicated the same branch)
                                progress.objectiveCounts[o.objectiveID] = o.currentCount;
                            }

                            // Notify listeners that progress has been restored
                            // Cannot invoke QuestTracker's event from here (only the declaring class can invoke its event)
                            // Use the public helper on QuestTracker to notify listeners instead.
                            questTracker.NotifyProgressUpdated(progress);
                        }

                        // Defer firing started event so UI components that subscribe in Start() won't miss it
                        deferredStartedNotifications.Add(q);
                    }
                }
            }
        }

        // Ensure known quests have entries (if not present in save)
        if (questDatabase != null)
        {
            foreach (var q in questDatabase.AllQuests)
            {
                if (q == null) continue;
                if (!questStates.ContainsKey(q.questID))
                    questStates[q.questID] = QuestState.Inactive;
            }
        }

        // Subscribe to progress changed events to detect completion
        GameEvents.OnQuestProgressChanged += HandleQuestProgressChanged;

        // Start deferred notification coroutine so other Start() methods (e.g., UI) can subscribe first
        if (deferredStartedNotifications.Count > 0)
            StartCoroutine(NotifyDeferredStartedNextFrame());
    }

    private IEnumerator NotifyDeferredStartedNextFrame()
    {
        // Wait one frame to allow other MonoBehaviour.Start() to run and subscribe
        yield return null;

        foreach (var q in deferredStartedNotifications)
        {
            OnQuestStarted?.Invoke(q);
        }

        deferredStartedNotifications.Clear();
    }

    private void OnDestroy()
    {
        GameEvents.OnQuestProgressChanged -= HandleQuestProgressChanged;
    }

    private void HandleQuestProgressChanged(string questID)
    {
        // Called when QuestTracker reports changes — check if quest is completed
        var progress = questTracker?.GetProgress(questID);
        if (progress == null) return;

        bool allSatisfied = true;
        foreach (var obj in progress.questData.objectives)
        {
            progress.objectiveCounts.TryGetValue(obj.objectiveID, out int count);
            if (count < obj.requiredAmount)
            {
                allSatisfied = false;
                break;
            }
        }

        if (allSatisfied)
        {
            CompleteQuest(questID);
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Called by NPCQuestDialog to begin a quest.</summary>
    public void StartQuest(QuestData quest)
    {
        if (quest == null) return;

        // Vấn đề 3: kiểm tra quest tồn tại trong database để tránh nhận quest "lạ"
        if (questDatabase == null || !questDatabase.Contains(quest.questID))
        {
            Debug.LogWarning($"Attempted to start unknown quest '{quest?.questID}'. Ignored.");
            return;
        }

        if (questStates.TryGetValue(quest.questID, out var state) && state == QuestState.Active)
            return; // already active

        questStates[quest.questID] = QuestState.Active;
        questTracker?.TrackQuest(quest);
        saveSystem?.SaveQuestData(questStates, questTracker?.GetAllActiveProgresses());
        OnQuestStarted?.Invoke(quest);
    }

    public void CompleteQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;
        if (!questStates.ContainsKey(questID)) questStates[questID] = QuestState.Inactive;

        questStates[questID] = QuestState.Completed;

        var quest = questDatabase?.GetQuestByID(questID);
        if (quest != null)
            OnQuestCompleted?.Invoke(quest);

        // Stop tracking and persist
        questTracker?.UntrackQuest(questID);
        saveSystem?.SaveQuestData(questStates, questTracker?.GetAllActiveProgresses());
    }

    public void FailQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;
        if (!questStates.ContainsKey(questID)) questStates[questID] = QuestState.Inactive;

        questStates[questID] = QuestState.Failed;

        var quest = questDatabase?.GetQuestByID(questID);
        if (quest != null)
            OnQuestFailed?.Invoke(quest);

        questTracker?.UntrackQuest(questID);
        saveSystem?.SaveQuestData(questStates, questTracker?.GetAllActiveProgresses());
    }

    public QuestState GetQuestState(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return QuestState.Inactive;
        if (questStates.TryGetValue(questID, out var state)) return state;
        return QuestState.Inactive;
    }

    public List<QuestData> GetQuestsByState(QuestState state)
    {
        var list = new List<QuestData>();
        if (questDatabase == null) return list;

        foreach (var q in questDatabase.AllQuests)
        {
            if (q == null) continue;
            var s = GetQuestState(q.questID);
            if (s == state) list.Add(q);
        }

        return list;
    }
}
