using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest System/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Quest Info")]
    public string questID;
    public string questName;
    [TextArea] public string description;

    [Header("Objectives")]
    public List<ObjectiveData> objectives = new List<ObjectiveData>();

    [Header("Rewards")]
    public int experienceReward;
    public int goldReward;
}

[Serializable]
public class ObjectiveData
{
    public string objectiveID;
    public string description;
    public ObjectiveType type;
    public string targetID;   // e.g. enemy prefab name or item ID
    public int requiredAmount;
}

public enum ObjectiveType { KillEnemy, CollectItem, TalkToNPC, ReachLocation }

public enum QuestState { Inactive, Active, Completed, Failed }
