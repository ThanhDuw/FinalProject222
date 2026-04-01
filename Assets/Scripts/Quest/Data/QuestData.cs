using System;
using System.Collections.Generic;
using UnityEngine;
using CreatorKitCode;


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
    public Item itemReward; // ScriptableObject asset -- assign MetalAxe / WarriorHelmet / LegendaryRake in Inspector

    [Header("Turn-In Dialogue (multi-step)")]
    [Tooltip("Chuỗi hội thoại hiển thị tuần tự khi trả nhiệm vụ. Bước CUỐI CÙNG sẽ trigger hiển thị Reward.")]
    [TextArea(2, 4)]
    public List<string> turnInDialogueSteps = new List<string>();

    [Header("Reward Display (3D)")]
    [Tooltip("Prefab 3D sẽ spawn và xoay giữa màn hình sau dialogue cuối.")]
    public GameObject rewardDisplayPrefab;
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

public enum QuestState { Inactive, Active, Completed, Failed, ReadyToTurnIn }
