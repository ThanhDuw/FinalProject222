using UnityEngine;
using System.Linq;

/// <summary>
/// Listens to GameEvents (Enemy / Item System) and forwards
/// objective updates to QuestTracker.
/// </summary>
public class ObjectiveSystem : MonoBehaviour
{
    [SerializeField] private QuestTracker questTracker;

    private void OnEnable()
    {
        GameEvents.OnEnemyKilled    += HandleEnemyKilled;
        GameEvents.OnItemCollected  += HandleItemCollected;
        GameEvents.OnNPCTalkCompleted += HandleNPCTalkCompleted;
        GameEvents.OnLocationReached  += HandleLocationReached;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilled    -= HandleEnemyKilled;
        GameEvents.OnItemCollected  -= HandleItemCollected;
        GameEvents.OnNPCTalkCompleted -= HandleNPCTalkCompleted;
        GameEvents.OnLocationReached  -= HandleLocationReached;
    }

    public void HandleEnemyKilled(string enemyID)
    {
        if (questTracker == null) return;

        // For each active quest, check objectives of type KillEnemy that target this enemyID
        foreach (var progress in questTracker.GetAllActiveProgresses())
        {
            var quest = progress.questData;
            if (quest == null) continue;

            foreach (var obj in quest.objectives.Where(o => o.type == ObjectiveType.KillEnemy && o.targetID == enemyID))
            {
                questTracker.UpdateObjective(quest.questID, obj.objectiveID, 1);
            }
        }
    }

    public void HandleItemCollected(string itemID, int amount)
    {
        if (questTracker == null) return;

        foreach (var progress in questTracker.GetAllActiveProgresses())
        {
            var quest = progress.questData;
            if (quest == null) continue;

            foreach (var obj in quest.objectives.Where(o => o.type == ObjectiveType.CollectItem && o.targetID == itemID))
            {
                questTracker.UpdateObjective(quest.questID, obj.objectiveID, amount);
            }
        }
    }

    public void HandleNPCTalkCompleted(string npcID)
    {
        if (questTracker == null) return;

        foreach (var progress in questTracker.GetAllActiveProgresses())
        {
            var quest = progress.questData;
            if (quest == null) continue;

            foreach (var obj in quest.objectives.Where(o => o.type == ObjectiveType.TalkToNPC && o.targetID == npcID))
            {
                questTracker.UpdateObjective(quest.questID, obj.objectiveID, 1);
            }
        }
    }

    public void HandleLocationReached(string locationID)
    {
        if (questTracker == null) return;

        foreach (var progress in questTracker.GetAllActiveProgresses())
        {
            var quest = progress.questData;
            if (quest == null) continue;

            foreach (var obj in quest.objectives.Where(o => o.type == ObjectiveType.ReachLocation && o.targetID == locationID))
            {
                questTracker.UpdateObjective(quest.questID, obj.objectiveID, 1);
            }
        }
    }
}
