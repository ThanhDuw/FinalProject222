using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full quest log screen — displays all quests grouped by state.
/// </summary>
public class QuestLogUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject      questLogPanel;
    [SerializeField] private Transform       questListContainer;
    [SerializeField] private GameObject      questEntryPrefab;
    [SerializeField] private Text             detailTitle;
    [SerializeField] private Text             detailDescription;
    [SerializeField] private Transform       detailObjectivesContainer;
    [SerializeField] private Text             rewardText;
    [SerializeField] private Text             requestText;


    private bool subscribed = false;
    private Coroutine waitForManagerCoroutine;

    private void OnEnable()
    {
        // Try to subscribe immediately; if QuestManager.Instance not ready, start waiting coroutine
        TrySubscribe();

        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (waitForManagerCoroutine != null)
        {
            StopCoroutine(waitForManagerCoroutine);
            waitForManagerCoroutine = null;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestChanged;
            QuestManager.Instance.OnQuestFailed    += OnQuestChanged;
            subscribed = true;

            // Initially populate with all quests
            RefreshAll();
        }
        else
        {
            // If QuestManager not yet initialized, wait a frame (or until available)
            if (waitForManagerCoroutine == null)
                waitForManagerCoroutine = StartCoroutine(WaitForManagerThenSubscribe());
        }
    }

    private IEnumerator WaitForManagerThenSubscribe()
    {
        // Wait until QuestManager.Instance is available or timeout after a few frames
        int tries = 0;
        while (QuestManager.Instance == null && tries < 60)
        {
            tries++;
            yield return null;
        }

        waitForManagerCoroutine = null;
        TrySubscribe();
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestChanged;
            QuestManager.Instance.OnQuestFailed    -= OnQuestChanged;
        }
        subscribed = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void OnQuestChanged(QuestData _)
    {
        RefreshAll();
    }

    public void Open()
    {
        if (questLogPanel != null)
            questLogPanel.SetActive(true);

        RefreshAll();
    }

    public void Close()
    {
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (questLogPanel == null) return;
        questLogPanel.SetActive(!questLogPanel.activeSelf);
        if (questLogPanel.activeSelf) RefreshAll();
    }

    private void RefreshAll()
    {
        // Combine all quests from database via QuestManager by states
        var all = new List<QuestData>();
        if (QuestManager.Instance == null) return;

        // Add Active, Inactive, Completed, Failed (grouping optional)
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Active));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Inactive));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Completed));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Failed));

        PopulateList(all);
    }

private void PopulateList(List<QuestData> quests)
    {
        if (questListContainer == null) return;

        for (int i = questListContainer.childCount - 1; i >= 0; i--)
            Destroy(questListContainer.GetChild(i).gameObject);

        if (quests == null) return;

        var fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // State colour map
        var stateColors = new System.Collections.Generic.Dictionary<QuestState, Color>
        {
            { QuestState.Active,    new Color(0.4f,  0.85f, 0.4f) },
            { QuestState.Completed, new Color(0.55f, 0.55f, 0.9f) },
            { QuestState.Failed,    new Color(0.9f,  0.35f, 0.35f) },
            { QuestState.Inactive,  new Color(0.6f,  0.6f,  0.6f) },
        };
        var stateLabels = new System.Collections.Generic.Dictionary<QuestState, string>
        {
            { QuestState.Active,    "Active" },
            { QuestState.Completed, "Done" },
            { QuestState.Failed,    "Failed" },
            { QuestState.Inactive,  "" },
        };

        foreach (var q in quests)
        {
            if (q == null) continue;

            var state = QuestManager.Instance != null
                ? QuestManager.Instance.GetQuestState(q.questID)
                : QuestState.Inactive;

            GameObject entry;

            if (questEntryPrefab != null)
            {
                entry = Instantiate(questEntryPrefab, questListContainer);

                // ── StateIcon tint ────────────────────────────────────────────
                var stateIcon = entry.transform.Find("StateIcon");
                if (stateIcon != null)
                {
                    var img = stateIcon.GetComponent<Image>();
                    if (img != null && stateColors.TryGetValue(state, out var dot))
                        img.color = dot;
                }

                // ── QuestNameLabel ────────────────────────────────────────────
                var nameLabel = entry.transform.Find("QuestNameLabel");
                if (nameLabel != null)
                {
                    var txt = nameLabel.GetComponent<Text>();
                    if (txt != null) txt.text = q.questName;
                }

                // ── StatusLabel ───────────────────────────────────────────────
                var statusLabel = entry.transform.Find("StatusLabel");
                if (statusLabel != null)
                {
                    var txt = statusLabel.GetComponent<Text>();
                    if (txt != null)
                    {
                        txt.text = stateLabels.TryGetValue(state, out var lbl) ? lbl : "";
                        if (stateColors.TryGetValue(state, out var c)) txt.color = c;
                    }
                }
            }
            else
            {
                // ── Fallback (no prefab assigned) ─────────────────────────────
                entry = new GameObject("QuestEntry");
                entry.transform.SetParent(questListContainer, false);

                var entryRect = entry.AddComponent<RectTransform>();
                entryRect.sizeDelta = new Vector2(0f, 36f);

                var le = entry.AddComponent<LayoutElement>();
                le.preferredHeight = 36f;
                le.flexibleWidth   = 1f;

                var img = entry.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

                var btn = entry.AddComponent<Button>();
                btn.targetGraphic = img;

                var labelGO = new GameObject("QuestNameLabel");
                labelGO.transform.SetParent(entry.transform, false);
                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(26f, 0f);
                labelRect.offsetMax = Vector2.zero;
                var txt = labelGO.AddComponent<Text>();
                txt.font = fallbackFont; txt.fontSize = 14;
                txt.color = Color.white; txt.alignment = TextAnchor.MiddleLeft;
                txt.raycastTarget = false;
                txt.text = q.questName;
            }

            // Wire Button click -> ShowQuestDetail
            var button = entry.GetComponentInChildren<Button>();
            if (button != null)
            {
                var captured = q;
                button.onClick.AddListener(() => ShowQuestDetail(captured));
            }
        }
    }

private void ShowQuestDetail(QuestData quest)
    {
        if (quest == null) return;

        // Quest_Name
        if (detailTitle != null)
            detailTitle.text = quest.questName;

        // Description_Content
        if (detailDescription != null)
            detailDescription.text = quest.description;

        // Request_Text — danh sách objectives dạng text
        if (requestText != null)
            requestText.text = BuildRequestText(quest);

        // Reward_Text — phần thưởng
        if (rewardText != null)
            rewardText.text = BuildRewardText(quest);

        // ObjectivesContainer — dynamic rows chi tiết tiến độ
        if (detailObjectivesContainer != null)
        {
            for (int i = detailObjectivesContainer.childCount - 1; i >= 0; i--)
                Destroy(detailObjectivesContainer.GetChild(i).gameObject);

            var fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallbackFont == null) fallbackFont = GUI.skin?.font;

            var tracker = (QuestManager.Instance != null)
                ? QuestManager.Instance.GetComponent<QuestTracker>()
                : null;
            QuestProgress progress = tracker?.GetProgress(quest.questID);

            foreach (var obj in quest.objectives)
            {
                int cur = 0;
                progress?.objectiveCounts.TryGetValue(obj.objectiveID, out cur);
                bool done = cur >= obj.requiredAmount;

                var go = new GameObject("Obj_" + obj.objectiveID);
                go.transform.SetParent(detailObjectivesContainer, false);

                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 20f;
                le.flexibleWidth   = 1f;

                var txt = go.AddComponent<Text>();
                txt.font          = fallbackFont;
                txt.fontSize      = 13;
                txt.alignment     = TextAnchor.MiddleLeft;
                txt.raycastTarget = false;
                txt.color = done
                    ? new Color(0.4f, 0.9f, 0.4f)
                    : Color.white;
                txt.text = done
                    ? string.Format("\u2713 {0}", obj.description)
                    : string.Format("\u2022 {0}  {1}/{2}", obj.description, cur, obj.requiredAmount);
            }
        }
    }

    private string BuildRequestText(QuestData quest)
    {
        if (quest.objectives == null || quest.objectives.Count == 0)
            return "No objectives.";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < quest.objectives.Count; i++)
        {
            var obj = quest.objectives[i];
            if (i > 0) sb.Append("\n");
            sb.AppendFormat("{0}. {1}  (x{2})", i + 1, obj.description, obj.requiredAmount);
        }
        return sb.ToString();
    }

    private string BuildRewardText(QuestData quest)
    {
        if (quest.goldReward == 0 && quest.experienceReward == 0)
            return "No rewards.";
        var parts = new System.Collections.Generic.List<string>();
        if (quest.goldReward > 0)       parts.Add(quest.goldReward + " Gold");
        if (quest.experienceReward > 0) parts.Add(quest.experienceReward + " XP");
        return string.Join("  |  ", parts);
    }
}
