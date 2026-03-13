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
    
    [SerializeField] private Text             rewardText;
    [SerializeField] private Text             requestText;


    private bool subscribed = false;
    private Coroutine waitForManagerCoroutine;

private void Awake()
    {
        // Hide visual panel; script stays ACTIVE to receive events even when panel is hidden
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }

    private void Start()
    {
        // Subscribe in Start so QuestManager.Instance has already initialized in its own Awake
        TrySubscribe();
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
        // Refresh even when panel is hidden — data stays current
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
        if (QuestManager.Instance == null) return;

        var all = new List<QuestData>();
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Active));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Inactive));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Completed));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Failed));

        PopulateList(all);

        // Auto-select: first Active quest, or first quest overall
        QuestData autoSelect = null;
        foreach (var q in all)
        {
            if (QuestManager.Instance.GetQuestState(q.questID) == QuestState.Active)
            { autoSelect = q; break; }
        }
        if (autoSelect == null && all.Count > 0)
            autoSelect = all[0];

        if (autoSelect != null)
            ShowQuestDetail(autoSelect);
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

        if (detailTitle != null)
            detailTitle.text = quest.questName;

        if (detailDescription != null)
            detailDescription.text = quest.description;

        if (requestText != null)
            requestText.text = BuildRequestText(quest);

        if (rewardText != null)
            rewardText.text = BuildRewardText(quest);
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
