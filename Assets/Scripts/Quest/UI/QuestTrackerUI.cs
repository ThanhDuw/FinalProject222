using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD widget showing objectives for the active quest.
/// Subscribes to QuestTracker.OnProgressUpdated.
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuestTracker       questTracker;
    [SerializeField] private GameObject         trackerPanel;
    [SerializeField] private Text               questNameLabel; 
    [SerializeField] private Transform          objectiveContainer;
    [SerializeField] private GameObject         objectiveEntryPrefab;

    private void OnEnable()
    {
        if (questTracker != null)
            questTracker.OnProgressUpdated += Refresh;
    }

    private void OnDisable()
    {
        if (questTracker != null)
            questTracker.OnProgressUpdated -= Refresh;
    }

private void Refresh(QuestProgress progress)
    {
        if (progress == null || progress.questData == null)
        {
            if (trackerPanel != null)
                trackerPanel.SetActive(false);
            return;
        }

        if (questNameLabel != null)
            questNameLabel.text = progress.questData.questName;

        if (objectiveContainer != null)
        {
            for (int i = objectiveContainer.childCount - 1; i >= 0; i--)
                Destroy(objectiveContainer.GetChild(i).gameObject);

            foreach (var obj in progress.questData.objectives)
            {
                GameObject go;

                if (objectiveEntryPrefab != null)
                {
                    go = Instantiate(objectiveEntryPrefab, objectiveContainer);
                }
                else
                {
                    go = new GameObject("ObjectiveEntry");
                    go.transform.SetParent(objectiveContainer, false);

                    var txtComp = go.AddComponent<Text>();
                    var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null && GUI.skin != null) font = GUI.skin.font;
                    txtComp.font = font;
                    txtComp.color = Color.white;
                    txtComp.raycastTarget = false;

                    var layout = go.AddComponent<LayoutElement>();
                    layout.preferredHeight = 22f;
                }

                var txt = go.GetComponentInChildren<Text>();
                if (txt == null)
                {
                    txt = go.AddComponent<Text>();
                    var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null && GUI.skin != null) font = GUI.skin.font;
                    txt.font = font;
                    txt.color = Color.white;
                    txt.raycastTarget = false;
                }

                progress.objectiveCounts.TryGetValue(obj.objectiveID, out int current);
                txt.text = string.Format("{0}: {1}/{2}", obj.description, current, obj.requiredAmount);
            }
        }

        if (trackerPanel != null)
            trackerPanel.SetActive(true);
    }

    public void SetVisible(bool visible)
    {
        if (trackerPanel != null)
            trackerPanel.SetActive(visible);
    }
}
