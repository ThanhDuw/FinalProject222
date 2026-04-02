using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối viên trung tâm — Singleton.
/// Đọc QuestDatabase, điều phối QuestTracker / ObjectiveSystem / SaveSystem.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Sub-systems")]
    [SerializeField] private QuestTracker    questTracker;
    [SerializeField] private ObjectiveSystem objectiveSystem;

    private Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();

    // Giữ các nhiệm vụ được khôi phục lúc khởi động để có thể thông báo cho các listener sau khi các hàm Start() khác chạy
    private List<QuestData> deferredStartedNotifications = new List<QuestData>();

    public event Action<QuestData> OnQuestStarted;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<QuestData> OnQuestFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Đảm bảo các nhiệm vụ đã biết có mục lưu (nếu không có trong save)
        if (questDatabase != null)
        {
            foreach (var q in questDatabase.AllQuests)
            {
                if (q == null) continue;
                if (!questStates.ContainsKey(q.questID))
                    questStates[q.questID] = QuestState.Inactive;
            }
        }

        // Đăng ký theo dõi sự kiện thay đổi tiến trình để phát hiện khi hoàn thành
        GameEvents.OnQuestProgressChanged   += HandleQuestProgressChanged;
        GameEvents.OnSceneTransitionComplete += RestoreQuestStateAfterSceneLoad;

        // Bắt đầu coroutine thông báo bị trì hoãn để các hàm Start() khác (ví dụ UI) có thể đăng ký trước
        if (deferredStartedNotifications.Count > 0)
            StartCoroutine(NotifyDeferredStartedNextFrame());
    }

    private IEnumerator NotifyDeferredStartedNextFrame()
    {
        // Đợi một frame để cho phép các MonoBehaviour.Start() khác chạy và đăng ký
        yield return null;

        foreach (var q in deferredStartedNotifications)
        {
            OnQuestStarted?.Invoke(q);
        }

        deferredStartedNotifications.Clear();
    }

    private void OnDestroy()
    {
        GameEvents.OnQuestProgressChanged   -= HandleQuestProgressChanged;
        GameEvents.OnSceneTransitionComplete -= RestoreQuestStateAfterSceneLoad;
    }

    private void HandleQuestProgressChanged(string questID)
    {
        // Được gọi khi QuestTracker báo cáo thay đổi -- kiểm tra xem tất cả các mục tiêu đã được thỏa mãn chưa
        var progress = questTracker?.GetProgress(questID);
        if (progress == null) return;

        // Đã qua trạng thái Active -- bỏ qua các sự kiện thừa
        if (questStates.TryGetValue(questID, out var currentState) &&
            currentState != QuestState.Active) return;

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
            // KHÔNG hoàn thành nhiệm vụ một cách tự động.
            // Đánh dấu là ReadyToTurnIn để người chơi phải quay lại NPC để trả nhiệm vụ.
            MarkReadyToTurnIn(questID);
        }
    }

    /// <summary>
    /// Đánh dấu một nhiệm vụ là sẵn sàng để trả.
    /// Tất cả mục tiêu đã hoàn thành nhưng phần thưởng chưa được trao.
    /// Người chơi phải quay lại NPC và xác nhận qua hội thoại.
    /// </summary>
    public void MarkReadyToTurnIn(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;
        if (!questStates.ContainsKey(questID)) return;
        if (questStates[questID] != QuestState.Active) return; // only transition from Active

        questStates[questID] = QuestState.ReadyToTurnIn;

        // Tiếp tục theo dõi nhiệm vụ để HUD có thể tiếp tục hiển thị
        // (Chưa loại bỏ khỏi QuestTracker cho đến khi CompleteQuest được gọi bởi hội thoại NPC)
        var quest = questDatabase?.GetQuestByID(questID);
        if (quest != null)
            Debug.Log($"[QuestManager] Quest '{quest.questName}' is ready to turn in.");
    }

    /// <summary>Được gọi bởi NPCQuestDialog để bắt đầu một nhiệm vụ.</summary>
    public void StartQuest(QuestData quest)
    {
        if (quest == null) return;

        if (questDatabase == null || !questDatabase.Contains(quest.questID))
        {
            Debug.LogWarning($"Attempted to start unknown quest '{quest?.questID}'. Ignored.");
            return;
        }

        if (questStates.TryGetValue(quest.questID, out var state) && state == QuestState.Active)
            return; // đã kích hoạt

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
        {
            OnQuestCompleted?.Invoke(quest);

            // Trao phần thưởng item nếu có.
            // Sửa lỗi: PlayerCore (gắn tag "Player") không chứa CharacterData trực tiếp --
            // CharacterData nằm ở GameObject con "Character".
            // Sử dụng FindWithTag + GetComponentInChildren để tìm nó một cách đáng tin cậy.
            if (quest.itemReward != null)
            {
                var playerGO   = GameObject.FindWithTag("Player");
                var playerData = playerGO != null
                    ? playerGO.GetComponentInChildren<CreatorKitCode.CharacterData>()
                    : null;
                if (playerData != null)
                    playerData.Inventory.AddItem(quest.itemReward);
                else
                    Debug.LogWarning($"[QuestManager] CompleteQuest '{questID}': CharacterData not found -- item reward not granted.");
            }
        }

        // Dừng theo dõi
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

    /// <summary>
    /// Được gọi một frame sau khi cảnh mới tải xong
    /// (thông qua GameEvents.OnSceneTransitionComplete được gọi bởi TravelManager).
    ///
    /// Do QuestManager là DontDestroyOnLoad, từ điển questStates của nó
    /// tồn tại qua việc chuyển cảnh, nhưng QuestTracker là một MonoBehaviour cục bộ
    /// được tạo lại. Phương thức này:
    ///   1. Tìm lại các tham chiếu QuestTracker và ObjectiveSystem mới.
    ///   2. Đăng ký lại mọi nhiệm vụ đã Active trước khi chuyển cảnh.
    ///   3. Khôi phục lại tiến độ từng mục tiêu từ PlayerPrefs thông qua SaveSystem.
    /// </summary>
    private void RestoreQuestStateAfterSceneLoad()
    {
        // 1. Tìm lại các tham chiếu trong cảnh
        questTracker    = FindFirstObjectByType<QuestTracker>();
        objectiveSystem = FindFirstObjectByType<ObjectiveSystem>();

        if (questTracker == null)
        {
            Debug.LogWarning("[QuestManager] RestoreQuestStateAfterSceneLoad: QuestTracker not found in new scene.");
            return;
        }

        // 2. Tải số lượng vật phẩm nhiệm vụ đã lưu từ PlayerPrefs
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
        SaveSystem.QuestWrapper savedData = saveSystem != null ? saveSystem.LoadQuestData() : null;

        // Xây dựng một lookup nhanh
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

        // 3. Khôi phục trạng thái nhiệm vụ
        if (savedData != null && savedData.quests != null)
        {
            foreach (var q in savedData.quests)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;
                questStates[q.questID] = q.state;
            }
        }

        // 4. Bắt đầu theo dõi lại
        int retracked = 0;
        foreach (var kvp in questStates)
        {
            if (kvp.Value != QuestState.Active && kvp.Value != QuestState.ReadyToTurnIn) continue;

            var questData = questDatabase?.GetQuestByID(kvp.Key);
            if (questData == null) continue;

            // TrackQuest khởi tạo các đếm về 0
            questTracker.TrackQuest(questData);

            // Khôi phục bộ đếm
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
