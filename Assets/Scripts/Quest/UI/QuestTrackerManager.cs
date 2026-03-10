using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD widget — hiển thị quest đang active và tiến độ objectives.
///
/// Hierarchy mong đợi (tự tạo nếu thiếu):
///   QuestTrackerManager
///     └─ TrackerPanel          (Image background)
///          ├─ Header           (Text: "✦ QUEST")
///          ├─ QuestName        (Text: tên quest)
///          ├─ Separator        (Image: đường kẻ ngang)
///          └─ ObjectivesContainer (VerticalLayoutGroup)
///               └─ (dynamic) ObjectiveRow_N
///
/// Setup:
///   1. Kéo QuestTracker component (từ QuestSystem) vào questTracker
///   2. Tất cả UI refs tự được tạo khi start nếu chưa assign
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
    private Font _fallbackFont;
    private bool _subscribed;
    private Coroutine _waitCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        TrySubscribe();

        // Panel hidden until a quest becomes active
        if (trackerPanel != null)
            trackerPanel.SetActive(false);
    }

    private void OnEnable()  => TrySubscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    // ── Subscribe helpers ─────────────────────────────────────────────────────
    private void TrySubscribe()
    {
        if (_subscribed) return;

        // Resolve QuestTracker from QuestSystem singleton if not assigned
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

        // Nếu đã có quest active khi script enable (e.g. reload scene)
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
        _subscribed = false;
        if (_waitCoroutine != null) { StopCoroutine(_waitCoroutine); _waitCoroutine = null; }
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

    // ── Event handlers ────────────────────────────────────────────────────────
    private void OnProgressUpdated(QuestProgress progress)
    {
        if (progress == null || progress.questData == null) return;
        ShowProgress(progress);
    }

    private void OnQuestStopped(string questID)
    {
        // If no more active quests, hide panel
        bool anyActive = false;
        if (questTracker != null)
            foreach (var p in questTracker.GetAllActiveProgresses())
                if (p != null) { anyActive = true; break; }

        if (!anyActive && trackerPanel != null)
            trackerPanel.SetActive(false);
    }

    // ── Display ───────────────────────────────────────────────────────────────
    private void RefreshFromAllActive()
    {
        if (questTracker == null) return;
        QuestProgress latest = null;
        foreach (var p in questTracker.GetAllActiveProgresses())
            latest = p; // show last / only active quest
        if (latest != null)
            ShowProgress(latest);
    }

    private void ShowProgress(QuestProgress progress)
    {
        EnsurePanel();

        // Quest name
        if (questNameText != null)
            questNameText.text = progress.questData.questName;

        // Rebuild objectives list
        if (objectivesContainer != null)
        {
            // Clear old rows
            for (int i = objectivesContainer.childCount - 1; i >= 0; i--)
                Destroy(objectivesContainer.GetChild(i).gameObject);

            foreach (var obj in progress.questData.objectives)
            {
                progress.objectiveCounts.TryGetValue(obj.objectiveID, out int cur);
                bool done = cur >= obj.requiredAmount;

                var row = new GameObject("ObjRow");
                row.transform.SetParent(objectivesContainer, false);

                var rowRect = row.AddComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(220f, 22f);

                var le = row.AddComponent<LayoutElement>();
                le.preferredHeight = 22f;
                le.flexibleWidth   = 1f;

                var txt = row.AddComponent<Text>();
                txt.font          = _fallbackFont;
                txt.fontSize      = 13;
                txt.color         = done
                    ? new Color(0.45f, 0.95f, 0.45f)   // xanh lá = hoàn thành
                    : new Color(0.92f, 0.92f, 0.92f);   // trắng   = chưa xong
                txt.raycastTarget = false;
                txt.text = done
                    ? string.Format("✓ {0}", obj.description)
                    : string.Format("• {0}  {1}/{2}", obj.description, cur, obj.requiredAmount);
            }
        }

        if (trackerPanel != null)
            trackerPanel.SetActive(true);
    }

    // ── Panel auto-builder ────────────────────────────────────────────────────
    /// <summary>Tạo toàn bộ UI nếu chưa được assign từ Inspector.</summary>
    private void EnsurePanel()
    {
        if (trackerPanel != null) return;

        // ── Root panel ───────────────────────────────────────────────────────
        var panel = new GameObject("TrackerPanel");
        panel.transform.SetParent(transform, false);
        panel.layer = gameObject.layer;

        var panelRect = panel.AddComponent<RectTransform>();
        // Anchor: top-right corner
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot     = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);
        panelRect.sizeDelta = new Vector2(240f, 160f);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.1f, 0.82f);

        // VerticalLayoutGroup on panel for automatic stacking
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding          = new RectOffset(10, 10, 8, 8);
        vlg.spacing          = 4f;
        vlg.childAlignment   = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;

        var csf = panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        trackerPanel = panel;

        // ── Header label ─────────────────────────────────────────────────────
        var header = MakeText(panel.transform, "Header", "✦ QUEST ACTIVE",
            14, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), 20f);

        // ── Quest name ───────────────────────────────────────────────────────
        var nameGO = MakeText(panel.transform, "QuestName", "—",
            15, FontStyle.Bold, Color.white, 22f);
        questNameText = nameGO.GetComponent<Text>();

        // ── Separator ────────────────────────────────────────────────────────
        var sep    = new GameObject("Separator");
        sep.transform.SetParent(panel.transform, false);
        sep.layer = gameObject.layer;
        var sepRect  = sep.AddComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(0f, 2f);
        var sepImg = sep.AddComponent<Image>();
        sepImg.color = new Color(1f, 0.85f, 0.2f, 0.45f);
        var sepLE = sep.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 2f;
        sepLE.flexibleWidth   = 1f;

        // ── Objectives container ──────────────────────────────────────────────
        var objContainer = new GameObject("ObjectivesContainer");
        objContainer.transform.SetParent(panel.transform, false);
        objContainer.layer = gameObject.layer;
        objContainer.AddComponent<RectTransform>();

        var objVLG = objContainer.AddComponent<VerticalLayoutGroup>();
        objVLG.spacing          = 3f;
        objVLG.childAlignment   = TextAnchor.UpperLeft;
        objVLG.childForceExpandWidth  = true;
        objVLG.childForceExpandHeight = false;
        objVLG.childControlWidth  = true;
        objVLG.childControlHeight = false;

        var objCSF = objContainer.AddComponent<ContentSizeFitter>();
        objCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var objLE = objContainer.AddComponent<LayoutElement>();
        objLE.flexibleWidth = 1f;

        objectivesContainer = objContainer.transform;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private GameObject MakeText(Transform parent, string goName, string content,
        int size, FontStyle style, Color color, float height)
    {
        var go   = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.layer = gameObject.layer;
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, height);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;

        var txt = go.AddComponent<Text>();
        txt.font          = _fallbackFont;
        txt.text          = content;
        txt.fontSize      = size;
        txt.fontStyle     = style;
        txt.color         = color;
        txt.raycastTarget = false;

        return go;
    }
}
