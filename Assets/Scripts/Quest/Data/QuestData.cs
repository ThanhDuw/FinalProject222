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
    public Item itemReward; // Gán asset ScriptableObject từ Inspector

    [Header("Turn-In Dialogue (multi-step)")]
    [Tooltip("Chuỗi hội thoại hiển thị tuần tự khi trả nhiệm vụ. Bước CUỐI CÙNG sẽ kích hoạt hiển thị phần thưởng.")]
    [TextArea(2, 4)]
    public List<string> turnInDialogueSteps = new List<string>();

    [Header("Reward Display (3D)")]
    [Tooltip("Prefab 3D sẽ hiện và xoay giữa màn hình sau khi kết thúc hội thoại.")]
    public GameObject rewardDisplayPrefab;
}

[Serializable]
public class ObjectiveData
{
    public string objectiveID;
    public string description;
    public ObjectiveType type;
    public string targetID;   // ví dụ: tên prefab kẻ thù hoặc ID vật phẩm
    public int requiredAmount;
}

public enum ObjectiveType { KillEnemy, CollectItem, TalkToNPC, ReachLocation }

public enum QuestState { Inactive, Active, Completed, Failed, ReadyToTurnIn }
