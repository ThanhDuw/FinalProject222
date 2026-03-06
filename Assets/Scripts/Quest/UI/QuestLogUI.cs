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

        // Clear existing entries
        for (int i = questListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(questListContainer.GetChild(i).gameObject);
        }

        if (quests == null) return;

        foreach (var q in quests)
        {
            if (q == null) continue;

            GameObject entry = null;
            if (questEntryPrefab != null)
            {
                entry = Instantiate(questEntryPrefab, questListContainer);
            }
            else
            {
                entry = new GameObject("QuestEntry");
                entry.transform.SetParent(questListContainer, false);
                var txt = entry.AddComponent<Text>();
                // Resources.GetBuiltinResource can be unavailable on some Unity versions; fallback to GUI skin
                var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font == null) font = GUI.skin?.font;
                txt.font = font;
            }

            // Try to find a Text component to set label
            var textComp = entry.GetComponentInChildren<Text>();
            if (textComp != null)
                textComp.text = q.questName;

            // Try to wire up a Button to show detail when clicked
            var button = entry.GetComponentInChildren<Button>();
            if (button != null)
            {
                var captured = q; // capture for closure
                button.onClick.AddListener(() => ShowQuestDetail(captured));
            }
            else
            {
                // If no button, add a simple clickable component
                var btn = entry.GetComponent<Button>();
                if (btn == null) btn = entry.AddComponent<Button>();
                var captured = q;
                btn.onClick.AddListener(() => ShowQuestDetail(captured));

                // Ensure entry has a Graphic for Button to work
                if (entry.GetComponent<Image>() == null)
                {
                    entry.AddComponent<Image>().color = new Color(0,0,0,0);
                }
            }
        }
    }

    private void ShowQuestDetail(QuestData quest)
    {
        if (quest == null) return;

        if (detailTitle != null) detailTitle.text = quest.questName;
        if (detailDescription != null) detailDescription.text = quest.description;

        // Clear existing detail objectives
        if (detailObjectivesContainer != null)
        {
            for (int i = detailObjectivesContainer.childCount - 1; i >= 0; i--)
                Destroy(detailObjectivesContainer.GetChild(i).gameObject);

            // Try to obtain a fallback font
            var fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (fallbackFont == null) fallbackFont = GUI.skin?.font;

            foreach (var obj in quest.objectives)
            {
                var go = new GameObject("Obj");
                go.transform.SetParent(detailObjectivesContainer, false);
                var txt = go.AddComponent<Text>();
                txt.font = fallbackFont;
                txt.text = $"{obj.description} ({obj.requiredAmount})";
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = Color.white;
            }
        }
    }
}
