using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuestTrackerManager : MonoBehaviour
{
    [Header("Quest System")]
    [SerializeField] private QuestTracker questTracker;
    [Header("UI References")]
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private Text headerText;
    [SerializeField] private Text questNameText;
    [SerializeField] private Transform objectivesContainer;
    private Font _font;
    private bool _sub;
    private Coroutine _waitCo;
    private void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (trackerPanel != null) trackerPanel.SetActive(false);
        DoSub(false);
    }
    private void OnEnable()
    {
        DoSub(false);
        GameEvents.OnSceneTransitionComplete += OnSceneLoaded;
    }
    private void OnDisable()
    {
        DoUnsub();
        GameEvents.OnSceneTransitionComplete -= OnSceneLoaded;
    }
    private void OnDestroy()
    {
        DoUnsub();
        GameEvents.OnSceneTransitionComplete -= OnSceneLoaded;
    }
    private void DoSub(bool late)
    {
        if (_sub) return;
        
        // Thử lấy QuestTracker từ QuestManager (nếu nó là một component ở đó)
        if (questTracker == null && QuestManager.Instance != null)
            questTracker = QuestManager.Instance.GetComponent<QuestTracker>();

        // Nếu không tìm thấy, thử tìm kiếm trên toàn Scene
        if (questTracker == null)
            questTracker = FindFirstObjectByType<QuestTracker>();

        if (questTracker == null)
        {
            if (_waitCo == null) _waitCo = StartCoroutine(WaitAndSub());
            return;
        }

        questTracker.OnProgressUpdated      += OnProgress;
        questTracker.OnQuestTrackingStarted += OnProgress;
        questTracker.OnQuestTrackingStopped += OnStopped;
        
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestStarted += OnStarted;
            
        _sub = true;
        DoRefresh();
    }
    private void DoUnsub()
    {
        if (!_sub) return;
        if (questTracker != null)
        {
            questTracker.OnProgressUpdated      -= OnProgress;
            questTracker.OnQuestTrackingStarted -= OnProgress;
            questTracker.OnQuestTrackingStopped -= OnStopped;
        }
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestStarted -= OnStarted;
        _sub = false;
        if (_waitCo != null)
        {
            StopCoroutine(_waitCo);
            _waitCo = null;
        }
    }
    private IEnumerator WaitAndSub()
    {
        int n = 0;
        while ((questTracker == null || QuestManager.Instance == null) && n++ < 120)
        {
            yield return null;
            if (questTracker == null && QuestManager.Instance != null)
                questTracker = QuestManager.Instance.GetComponent<QuestTracker>();
        }
        _waitCo = null;
        DoSub(true);
    }
    private void OnSceneLoaded()
    {
        if (questTracker == null && QuestManager.Instance != null)
            questTracker = QuestManager.Instance.GetComponent<QuestTracker>();
        if (!_sub) DoSub(false);
        DoRefresh();
    }
    private void OnProgress(QuestProgress p)
    {
        if (p == null || p.questData == null) return;
        ShowQuest(p);
    }
    private void OnStopped(string questID)
    {
        DoRefresh();
    }
    private void OnStarted(QuestData quest)
    {
        DoRefresh();
    }
    public void TogglePanel()
    {
        if (trackerPanel == null) return;
        if (trackerPanel.activeSelf)
        {
            trackerPanel.SetActive(false);
            return;
        }
        bool hasActive = false;
        if (questTracker != null)
        {
            foreach (var p in questTracker.GetAllActiveProgresses())
            {
                if (p != null)
                {
                    hasActive = true;
                    break;
                }
            }
        }
        if (hasActive) DoRefresh();
    }
    private void HidePanel()
    {
        if (trackerPanel != null) trackerPanel.SetActive(false);
    }
    private void DoRefresh()
    {
        if (questTracker == null) return;
        QuestProgress latest = null;
        foreach (var p in questTracker.GetAllActiveProgresses())
        {
            latest = p;
        }
        if (latest != null) ShowQuest(latest);
        else HidePanel();;
    }
    private void ShowQuest(QuestProgress progress)
    {
        if (trackerPanel == null) return;
        if (headerText != null) headerText.text = "QUEST ACTIVE";
        if (questNameText != null) questNameText.text = progress.questData.questName;
        BuildRows(progress);
        trackerPanel.SetActive(true);
    }
    private void BuildRows(QuestProgress progress)
    {
        if (objectivesContainer == null) return;
        for (int i = objectivesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(objectivesContainer.GetChild(i).gameObject);
        }
        foreach (var obj in progress.questData.objectives)
        {
            progress.objectiveCounts.TryGetValue(obj.objectiveID, out int cur);
            bool done = cur >= obj.requiredAmount;
            var row = new GameObject("ObjRow");
            row.transform.SetParent(objectivesContainer, false);
            row.layer = gameObject.layer;
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 22f;
            le.flexibleWidth = 1f;
            var txt = row.AddComponent<Text>();
            txt.font = _font;
            txt.fontSize = 13;
            txt.color = done ? new Color(0.45f, 0.95f, 0.45f) : new Color(0.92f, 0.92f, 0.92f);
            txt.raycastTarget = false;
            txt.text = done ? ("Done: " + obj.description) : (obj.description + "  " + cur.ToString() + "/" + obj.requiredAmount.ToString());
        }
    }
}
