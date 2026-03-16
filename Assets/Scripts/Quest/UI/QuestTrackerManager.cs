using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD widget — hiển thị quest đang active và tiến độ objectives.
///
/// Behaviour:
///   - Hiển thị khi quest Active và cập nhật theo tiến độ.
///   - Khi quest Complete: vẫn hiển thị trạng thái hoàn thành
///     (tất cả objectives tick xanh) thay vì ẩn ngay.
///   - Chỉ ẩn panel khi quest mới bắt đầu (OnQuestStarted).
///   - TogglePanel() — gọi bởi Track_Quest button trong Quest Log
///     để bật/tắt panel thủ công.
///
/// Scene Transition:
///   Listens to GameEvents.OnSceneTransitionComplete (raised by TravelManager).
/// </summary>
public class QuestTrackerManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Quest System")]
    [SerializeField] private QuestTracker questTracker;

    [Header("UI Refs (auto-created if empty)")]
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private Text       questNameText;
    [SerializeField] private Transform  objectivesContainer;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Font      _fallbackFont;
    private bool      _subscribed;
    private Coroutine _waitCoroutine;

    // Last displayed progress — kept so we can re-show completed state
    private QuestProgress _lastProgress;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        TrySubscribe();

        if (trackerPanel != null)
            trackerPanel.SetActive(false);
    }

    private void OnEnable()
    {
        TrySubscribe();
        GameEvents.OnSceneTransitionComplete += OnSceneTransitionComplete;
    }

    private void OnDisable()
    {
        Unsubscribe();
        GameEvents.OnSceneTransitionComplete -= OnSceneTransitionComplete;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        GameEvents.OnSceneTransitionComplete -= OnSceneTransitionComplete;
    }

    // ── Subscribe helpers ─────────────────────────────────────────────────────

    private void TrySubscribe()
    {
        if (_subscribed) return;

        if (questTracker == null && QuestManager.Instance != null)
            questTracker = QuestManager.Instance.GetComponent<QuestTracker>();

        if (questTracker == null)
        {
            if (_waitCoroutine == null)
                _waitCoroutine = StartCoroutine(WaitThenSubscribe());
            return;
        }

        questTracker.OnProgressUpdated      += OnProgressUpdated;
        questTracker.OnQuestTrackingStopped += OnQuestStopped;
        _subscribed = true;

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestStarted += OnQuestStarted;

        RefreshFromAllActive();
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (questTracker != null)
        {
            questTracker.OnProgressUpdated      -= OnProgressUpdated;
            questTracker.OnQuestTrackingStopped -= OnQuestStopped;
        }

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestStarted -= OnQuestStarted;

        _subscribed = false;

        if (_waitCoroutine != null)
        {
            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }
    }

    private IEnumerator WaitThenSubscribe()
    {
        int tries = 0;
        while ((questTracker == null || QuestManager.Instance == null) && tries < 120)
        {
            tries++;
            yield return null;
            if (questTracker == null && QuestManager.Instance != null)
                questTracker = QuestManager.Instance.GetComponent<QuestTracker>();
        }
        _waitCoroutine = null;
        TrySubscribe();
    }

    // ── Scene Transition Handler ──────────────────────────────────────────────

    private void OnSceneTransitionComplete()
    {
        if (questTracker == null && QuestManager.Instance != null)
            questTracker = QuestManager.Instance.GetComponent<QuestTracker>();

        if (!_subscribed)
            TrySubscribe();

        RefreshFromAllActive();
        Debug.Log("[QuestTrackerManager] Scene transition complete — quest tracker refreshed.");
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnProgressUpdated(QuestProgress progress)
    {
        if (progress == null || progress.questData == null) return;
        _lastProgress = progress;
        ShowProgress(progress);
    }

    /// <summary>
    /// Called when quest tracking stops (quest completed or failed).
    /// Keeps showing the completed state — panel hides only when a new
    /// quest starts (OnQuestStarted).
    /// </summary>
    private void OnQuestStopped(string questID)
    {
        if (_lastProgress != null && _lastProgress.questData != null &&
            _lastProgress.questData.questID == questID)
        {
            ShowCompletedState(_lastProgress);
            return;
        }

        bool anyActive = false;
        if (questTracker != null)
            foreach (var p in questTracker.GetAllActiveProgresses())
                if (p != null) { anyActive = true; break; }

        if (!anyActive && trackerPanel != null)
            trackerPanel.SetActive(false);
    }

    /// <summary>
    /// Called when a new quest starts. Clears completed state and hides panel
    /// until the new quest's OnProgressUpdated fires.
    /// </summary>
    private void OnQuestStarted(QuestData quest)
    {
        _lastProgress = null;

        if (trackerPanel != null)
            trackerPanel.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Toggles the QuestTracker panel on/off.
    /// Called by the Track_Quest button inside the Quest Log.
    /// When turning back on: shows live quest if active, or last completed
    /// state if available.
    /// </summary>
    public void TogglePanel()
    {
        EnsurePanel();

        if (trackerPanel.activeSelf)
        {
            trackerPanel.SetActive(false);
            return;
        }

        // Turn on — prefer live active quest, fall back to last known progress
        bool hasActive = false;
        if (questTracker != null)
            foreach (var p in questTracker.GetAllActiveProgresses())
                if (p != null) { hasActive = true; break; }

        if (hasActive)
            RefreshFromAllActive();
        else if (_lastProgress != null)
            ShowCompletedState(_lastProgress);
        else
            trackerPanel.SetActive(true);
    }

    // ── Display ───────────────────────────────────────────────────────────────

    private void RefreshFromAllActive()
    {
        if (questTracker == null) return;

        QuestProgress latest = null;
        foreach (var p in questTracker.GetAllActiveProgresses())
            latest = p;

        if (latest != null)
        {
            _lastProgress = latest;
            ShowProgress(latest);
        }
        else if (_lastProgress != null)
        {
            ShowCompletedState(_lastProgress);
        }
    }

    private void ShowProgress(QuestProgress progress)
    {
        EnsurePanel();

        if (questNameText != null)
            questNameText.text = progress.questData.questName;

        // Reset header to active state
        var header = trackerPanel.transform.Find("Header");
        if (header != null)
        {
            var txt = header.GetComponent<Text>();
            if (txt != null) txt.text = "✦ QUEST ACTIVE";
        }

        BuildObjectiveRows(progress, isCompleted: false);

        if (trackerPanel != null)
            trackerPanel.SetActive(true);
    }

    private void ShowCompletedState(QuestProgress progress)
    {
        EnsurePanel();

        if (questNameText != null)
            questNameText.text = progress.questData.questName;

        var header = trackerPanel.transform.Find("Header");
        if (header != null)
        {
            var txt = header.GetComponent<Text>();
            if (txt != null) txt.text = "✦ QUEST COMPLETE";
        }

        BuildObjectiveRows(progress, isCompleted: true);

        if (trackerPanel != null)
            trackerPanel.SetActive(true);
    }

    private void BuildObjectiveRows(QuestProgress progress, bool isCompleted)
    {
        if (objectivesContainer == null) return;

        for (int i = objectivesContainer.childCount - 1; i >= 0; i--)
            Destroy(objectivesContainer.GetChild(i).gameObject);

        foreach (var obj in progress.questData.objectives)
        {
            progress.objectiveCounts.TryGetValue(obj.objectiveID, out int cur);
            bool done = isCompleted || cur >= obj.requiredAmount;

            var row            = new GameObject("ObjRow");
            row.transform.SetParent(objectivesContainer, false);

            var rowRect        = row.AddComponent<RectTransform>();
            rowRect.sizeDelta  = new Vector2(220f, 22f);

            var le             = row.AddComponent<LayoutElement>();
            le.preferredHeight = 22f;
            le.flexibleWidth   = 1f;

            var txt            = row.AddComponent<Text>();
            txt.font           = _fallbackFont;
            txt.fontSize       = 13;
            txt.color          = done
                ? new Color(0.45f, 0.95f, 0.45f)
                : new Color(0.92f, 0.92f, 0.92f);
            txt.raycastTarget  = false;
            txt.text           = done
                ? string.Format("✓ {0}", obj.description)
                : string.Format("• {0}  {1}/{2}", obj.description, cur, obj.requiredAmount);
        }
    }

    // ── Panel auto-builder ────────────────────────────────────────────────────

    private void EnsurePanel()
    {
        if (trackerPanel != null) return;

        var panel      = new GameObject("TrackerPanel");
        panel.transform.SetParent(transform, false);
        panel.layer    = gameObject.layer;

        var panelRect  = panel.AddComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(1f, 1f);
        panelRect.anchorMax        = new Vector2(1f, 1f);
        panelRect.pivot            = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);
        panelRect.sizeDelta        = new Vector2(240f, 160f);

        var panelImg   = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.1f, 0.82f);

        var vlg                    = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding                = new RectOffset(10, 10, 8, 8);
        vlg.spacing                = 4f;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;

        var csf         = panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        trackerPanel = panel;

        MakeText(panel.transform, "Header", "✦ QUEST ACTIVE",
            14, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), 20f);

        var nameGO    = MakeText(panel.transform, "QuestName", "—",
            15, FontStyle.Bold, Color.white, 22f);
        questNameText = nameGO.GetComponent<Text>();

        var sep       = new GameObject("Separator");
        sep.transform.SetParent(panel.transform, false);
        sep.layer     = gameObject.layer;
        var sepRect   = sep.AddComponent<RectTransform>();
        sepRect.sizeDelta  = new Vector2(0f, 2f);
        var sepImg    = sep.AddComponent<Image>();
        sepImg.color  = new Color(1f, 0.85f, 0.2f, 0.45f);
        var sepLE     = sep.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 2f;
        sepLE.flexibleWidth   = 1f;

        var objContainer = new GameObject("ObjectivesContainer");
        objContainer.transform.SetParent(panel.transform, false);
        objContainer.layer = gameObject.layer;
        objContainer.AddComponent<RectTransform>();

        var objVLG                    = objContainer.AddComponent<VerticalLayoutGroup>();
        objVLG.spacing                = 3f;
        objVLG.childAlignment         = TextAnchor.UpperLeft;
        objVLG.childForceExpandWidth  = true;
        objVLG.childForceExpandHeight = false;
        objVLG.childControlWidth      = true;
        objVLG.childControlHeight     = false;

        var objCSF        = objContainer.AddComponent<ContentSizeFitter>();
        objCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var objLE         = objContainer.AddComponent<LayoutElement>();
        objLE.flexibleWidth = 1f;

        objectivesContainer = objContainer.transform;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private GameObject MakeText(Transform parent, string goName, string content,
        int size, FontStyle style, Color color, float height)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.layer = gameObject.layer;
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, height);

        var le             = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;

        var txt            = go.AddComponent<Text>();
        txt.font           = _fallbackFont;
        txt.text           = content;
        txt.fontSize       = size;
        txt.fontStyle      = style;
        txt.color          = color;
        txt.raycastTarget  = false;

        return go;
    }
}
