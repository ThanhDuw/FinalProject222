using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toàn bộ màn hình nhật ký nhiệm vụ — hiển thị tất cả các nhiệm vụ được nhóm theo trạng thái.
/// Ưu tiên tự động chọn: 1. ReadyToTurnIn  2. Active  3. Nothing
/// </summary>
public class QuestLogUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject questLogPanel;
    [SerializeField] private Transform  questListContainer;
    [SerializeField] private GameObject questEntryPrefab;
    [SerializeField] private Text       detailTitle;
    [SerializeField] private Text       detailDescription;
    [SerializeField] private Text       rewardText;
    [SerializeField] private Text       requestText;

    private bool      subscribed = false;
    private Coroutine waitForManagerCoroutine;



    private void Awake()
    {
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted   += OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestChanged;
            QuestManager.Instance.OnQuestFailed    += OnQuestChanged;
            subscribed = true;
            Invoke("InitialRefresh", 0f);
        }
        else
        {
            if (waitForManagerCoroutine == null)
                waitForManagerCoroutine = StartCoroutine(WaitForManagerThenInit());
        }
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted   -= OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestChanged;
            QuestManager.Instance.OnQuestFailed    -= OnQuestChanged;
        }
        subscribed = false;
    }



    private void InitialRefresh()
    {
        BuildDisplay();
    }

    private IEnumerator WaitForManagerThenInit()
    {
        int tries = 0;
        while (QuestManager.Instance == null && tries < 60)
        {
            tries++;
            yield return null;
        }
        waitForManagerCoroutine = null;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted   += OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestChanged;
            QuestManager.Instance.OnQuestFailed    += OnQuestChanged;
            subscribed = true;
        }
        BuildDisplay();
    }



    private void OnQuestChanged(QuestData _)
    {
        BuildDisplay();
    }



    public void Open()
    {
        if (questLogPanel != null) questLogPanel.SetActive(true);
        BuildDisplay();
    }

    public void Close()
    {
        if (questLogPanel != null) questLogPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (questLogPanel == null) return;
        questLogPanel.SetActive(!questLogPanel.activeSelf);
        if (questLogPanel.activeSelf) BuildDisplay();
    }



    private void BuildDisplay()
    {
        if (QuestManager.Instance == null) return;

        var all = new List<QuestData>();
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.ReadyToTurnIn));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Active));
        all.AddRange(QuestManager.Instance.GetQuestsByState(QuestState.Inactive));

        PopulateList(all);
        AutoSelectEntry(all);
    }

    private void AutoSelectEntry(List<QuestData> all)
    {
        QuestData pick = null;

        // Ưu tiên 1: Sẵn sàng trả nhiệm vụ (ReadyToTurnIn)
        foreach (var q in all)
        {
            if (QuestManager.Instance.GetQuestState(q.questID) == QuestState.ReadyToTurnIn)
            { pick = q; break; }
        }

        // Ưu tiên 2: Đang làm (Active)
        if (pick == null)
        {
            foreach (var q in all)
            {
                if (QuestManager.Instance.GetQuestState(q.questID) == QuestState.Active)
                { pick = q; break; }
            }
        }

        if (pick != null) ShowQuestDetail(pick);
        else             ClearDetails();
    }

    private void PopulateList(List<QuestData> quests)
    {
        if (questListContainer == null) return;
        for (int i = questListContainer.childCount - 1; i >= 0; i--)
            Destroy(questListContainer.GetChild(i).gameObject);
        if (quests == null) return;

        var fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var stateColors = new Dictionary<QuestState, Color>
        {
            { QuestState.ReadyToTurnIn, new Color(1.0f, 0.75f, 0.2f) },
            { QuestState.Active,        new Color(0.4f, 0.85f, 0.4f) },
            { QuestState.Inactive,      new Color(0.6f, 0.6f,  0.6f) },
        };

        var stateLabels = new Dictionary<QuestState, string>
        {
            { QuestState.ReadyToTurnIn, "Ready!" },
            { QuestState.Active,        "Active" },
            { QuestState.Inactive,      ""       },
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

                var stateIcon = entry.transform.Find("StateIcon");
                if (stateIcon != null)
                {
                    var img = stateIcon.GetComponent<Image>();
                    if (img != null && stateColors.TryGetValue(state, out var dot))
                        img.color = dot;
                }

                var nameLabel = entry.transform.Find("QuestNameLabel");
                if (nameLabel != null)
                {
                    var txt = nameLabel.GetComponent<Text>();
                    if (txt != null) txt.text = q.questName;
                }

                var statusLabel = entry.transform.Find("StatusLabel");
                if (statusLabel != null)
                {
                    var txt = statusLabel.GetComponent<Text>();
                    if (txt != null)
                    {
                        txt.text  = stateLabels.TryGetValue(state, out var lbl) ? lbl : "";
                        if (stateColors.TryGetValue(state, out var c)) txt.color = c;
                    }
                }
            }
            else
            {
                var go              = new GameObject("QuestEntry");
                go.transform.SetParent(questListContainer, false);

                var entryRect       = go.AddComponent<RectTransform>();
                entryRect.sizeDelta = new Vector2(0f, 36f);

                var le              = go.AddComponent<LayoutElement>();
                le.preferredHeight  = 36f;
                le.flexibleWidth    = 1f;

                var img   = go.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

                var btn           = go.AddComponent<Button>();
                btn.targetGraphic = img;

                var labelGO = new GameObject("QuestNameLabel");
                labelGO.transform.SetParent(go.transform, false);

                var labelRect       = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(26f, 0f);
                labelRect.offsetMax = Vector2.zero;

                var txt           = labelGO.AddComponent<Text>();
                txt.font          = fallbackFont;
                txt.fontSize      = 14;
                txt.color         = Color.white;
                txt.alignment     = TextAnchor.MiddleLeft;
                txt.raycastTarget = false;
                txt.text          = q.questName;

                entry = go;
            }

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
        if (detailTitle != null)       detailTitle.text       = quest.questName;
        if (detailDescription != null) detailDescription.text = quest.description;
        if (requestText != null)       requestText.text       = MakeObjectivesText(quest);
        if (rewardText != null)        rewardText.text        = MakeRewardsText(quest);
    }

    private void ClearDetails()
    {
        if (detailTitle != null)       detailTitle.text       = "";
        if (detailDescription != null) detailDescription.text = "No active quest.";
        if (requestText != null)       requestText.text       = "";
        if (rewardText != null)        rewardText.text        = "";
    }



    private string MakeObjectivesText(QuestData quest)
    {
        if (quest.objectives == null || quest.objectives.Count == 0)
            return "No objectives.";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < quest.objectives.Count; i++)
        {
            if (i > 0) sb.Append("\n");
            var obj = quest.objectives[i];
            sb.AppendFormat("{0}. {1}  (x{2})", i + 1, obj.description, obj.requiredAmount);
        }
        return sb.ToString();
    }

    private string MakeRewardsText(QuestData quest)
    {
        if (quest.goldReward == 0 && quest.experienceReward == 0) return "No rewards.";
        var parts = new List<string>();
        if (quest.goldReward       > 0) parts.Add(quest.goldReward       + " Gold");
        if (quest.experienceReward > 0) parts.Add(quest.experienceReward + " XP");
        return string.Join("  |  ", parts);
    }
}
