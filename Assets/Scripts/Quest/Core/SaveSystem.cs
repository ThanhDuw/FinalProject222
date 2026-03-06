using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles serialization and deserialization of quest states.
/// Stores per-quest and per-objective progress so it can be restored.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    private const string SaveKey = "QuestSaveData";

    // Save both high-level quest states and detailed objective counts for active quests.
    public void SaveQuestData(Dictionary<string, QuestState> questStates, IEnumerable<QuestProgress> activeProgresses)
    {
        if (questStates == null) return;

        var list = new List<QuestSaveModel>();
        foreach (var kvp in questStates)
        {
            var model = new QuestSaveModel
            {
                questID = kvp.Key,
                state = kvp.Value,
                objectives = new List<ObjectiveSaveModel>()
            };

            // If we have runtime progress for this quest, include objective counts
            if (activeProgresses != null)
            {
                foreach (var p in activeProgresses)
                {
                    if (p == null || p.questData == null) continue;
                    if (p.questData.questID != kvp.Key) continue;

                    foreach (var oc in p.objectiveCounts)
                    {
                        model.objectives.Add(new ObjectiveSaveModel { objectiveID = oc.Key, currentCount = oc.Value });
                    }

                    break;
                }
            }

            list.Add(model);
        }

        var json = JsonUtility.ToJson(new Wrapper { quests = list });
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    // Backwards-compatible simple save (stores states only)
    public void SaveQuestData(Dictionary<string, QuestState> questStates)
    {
        SaveQuestData(questStates, null);
    }

    // Load the saved wrapper. Caller will reconstruct dictionaries and restore objective counts.
    public Wrapper LoadQuestData()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return null;

        var json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(json);
            return wrapper;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to load quest save data: " + e.Message);
            return null;
        }
    }

    public void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    // ── Internal save model ──────────────────────────────────────────────────
    [Serializable]
    public class QuestSaveModel
    {
        public string     questID;
        public QuestState state;
        public List<ObjectiveSaveModel> objectives;
    }

    [Serializable]
    public class ObjectiveSaveModel
    {
        public string objectiveID;
        public int    currentCount;
    }

    [Serializable]
    public class Wrapper
    {
        public List<QuestSaveModel> quests;
    }
}
