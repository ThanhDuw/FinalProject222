using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest System/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [SerializeField] private List<QuestData> allQuests = new List<QuestData>();

    public IReadOnlyList<QuestData> AllQuests => allQuests;

    public QuestData GetQuestByID(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return null;
        for (int i = 0; i < allQuests.Count; i++)
        {
            if (allQuests[i] != null && allQuests[i].questID == questID)
                return allQuests[i];
        }
        return null;
    }

    public bool Contains(string questID)
    {
        return GetQuestByID(questID) != null;
    }
}
