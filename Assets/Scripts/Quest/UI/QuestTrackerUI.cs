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
        if (progress == null || progress.questData == null) return;

        if (questNameLabel != null)
            questNameLabel.text = progress.questData.questName;

        // Clear existing objective entries
        if (objectiveContainer != null)
        {
            for (int i = objectiveContainer.childCount - 1; i >= 0; i--)
                Destroy(objectiveContainer.GetChild(i).gameObject);

            // Instantiate entries for each objective
            foreach (var obj in progress.questData.objectives)
            {
                GameObject go;

                if (objectiveEntryPrefab != null)
                {
                    go = Instantiate(objectiveEntryPrefab, objectiveContainer);
                }
                else
                {
                    // Fallback: create a simple Text entry if prefab not assigned
                    go = new GameObject("ObjectiveEntry");
                    go.transform.SetParent(objectiveContainer, false);
                    var txtComp = go.AddComponent<Text>();
                    // Try builtin font, fallback to GUI skin font
                    var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (font == null) font = GUI.skin?.font;
                    txtComp.font = font;
                }

                // Find or add a Text component to set the label
                var txt = go.GetComponentInChildren<Text>();
                if (txt == null)
                {
                    txt = go.AddComponent<Text>();
                    var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (font == null) font = GUI.skin?.font;
                    txt.font = font;
                }

                progress.objectiveCounts.TryGetValue(obj.objectiveID, out int current);
                txt.text = $"{obj.description}: {current}/{obj.requiredAmount}";
            }
        }

        // Ensure panel visible if there's active progress
        if (trackerPanel != null)
            trackerPanel.SetActive(true);
    }

    public void SetVisible(bool visible)
    {
        if (trackerPanel != null)
            trackerPanel.SetActive(visible);
    }
}
