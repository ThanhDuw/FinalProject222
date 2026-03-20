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
        GameEvents.OnQuestProgressChanged  += HandleQuestProgressChanged;
        GameEvents.OnSceneTransitionComplete += RestoreQuestStateAfterSceneLoad;

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
        GameEvents.OnQuestProgressChanged  -= HandleQuestProgressChanged;
        GameEvents.OnSceneTransitionComplete -= RestoreQuestStateAfterSceneLoad;
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

        // Stop tracking
        questTracker?.UntrackQuest(questID);
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

    // -- Quest Restore After Scene Load ------------------------------------

    /// <summary>
    /// Called one frame after a new scene finishes loading
    /// (via GameEvents.OnSceneTransitionComplete raised by TravelManager).
    ///
    /// Because QuestManager is DontDestroyOnLoad its questStates dict
    /// survives the scene transition, but QuestTracker is an in-scene
    /// MonoBehaviour that gets recreated. This method:
    ///   1. Re-finds the new QuestTracker and ObjectiveSystem references.
    ///   2. Re-tracks every quest that was Active before the transition.
    ///   3. Restores per-objective counts from PlayerPrefs via SaveSystem.
    /// </summary>
    private void RestoreQuestStateAfterSceneLoad()
    {
        // 1. Re-acquire in-scene references that were destroyed with the old scene
        questTracker    = FindFirstObjectByType<QuestTracker>();
        objectiveSystem = FindFirstObjectByType<ObjectiveSystem>();

        if (questTracker == null)
        {
            Debug.LogWarning("[QuestManager] RestoreQuestStateAfterSceneLoad: QuestTracker not found in new scene.");
            return;
        }

        // 2. Load saved objective counts from PlayerPrefs
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
        SaveSystem.QuestWrapper savedData = saveSystem != null ? saveSystem.LoadQuestData() : null;

        // Build a quick lookup: questID -> list of (objectiveID, count)
        var savedCounts = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, int>>();
        if (savedData != null && savedData.quests != null)
        {
            foreach (var q in savedData.quests)
            {
                if (q == null || q.objectives == null) continue;
                var objMap = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var obj in q.objectives)
                    objMap[obj.objectiveID] = obj.currentCount;
                savedCounts[q.questID] = objMap;
            }
        }

        // 3. Re-track every Active quest and restore its objective progress
        int retracked = 0;
        foreach (var kvp in questStates)
        {
            if (kvp.Value != QuestState.Active) continue;

            var questData = questDatabase?.GetQuestByID(kvp.Key);
            if (questData == null) continue;

            // TrackQuest initialises all counts to 0
            questTracker.TrackQuest(questData);

            // Restore saved counts on top
            if (savedCounts.TryGetValue(kvp.Key, out var objMap))
            {
                foreach (var objEntry in objMap)
                {
                    if (objEntry.Value > 0)
                        questTracker.UpdateObjective(kvp.Key, objEntry.Key, objEntry.Value);
                }
            }

            retracked++;
        }

        Debug.Log($"[QuestManager] Restored {retracked} active quest(s) after scene transition.");
    }

}
