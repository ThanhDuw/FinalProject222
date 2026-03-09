#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CreateQuestAssets
{
    [MenuItem("Tools/Bone Reckoning/Create Quest Assets")]
    public static void Run()
    {
        const string folder = "Assets/Data/Quests";
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Data", "Quests");

        CreateQuest(folder + "/Quest_01_NguyenLieuSaMac.asset",
            "quest_01_nguyen_lieu_sa_mac", "Nguyen Lieu Sa Mac",
            "Chu quan bar nho ban thu thap nhua tu 10 Cay Xuong Rong tai sa mac.",
            100, 50, "obj_01_collect_cactus", "Thu thap nhua Cay Xuong Rong (0/10)",
            ObjectiveType.CollectItem, "CactusItem", 10);

        CreateQuest(folder + "/Quest_02_NoiLoVungNghiaDia.asset",
            "quest_02_noi_lo_vung_nghia_dia", "Noi Lo Vung Nghia Dia",
            "Dan Skeleton dang keo den lang. Hay den vung nghia dia va tieu diet chung.",
            250, 120, "obj_02_kill_skeleton", "Tieu diet Skeleton (0/15)",
            ObjectiveType.KillEnemy, "Skeleton", 15);

        CreateQuest(folder + "/Quest_03_TruyQuetPhapSuXuong.asset",
            "quest_03_truy_quet_phap_su_xuong", "Truy Quet Phap Su Xuong",
            "Tieu diet Boss Mage Skeleton, ke dung dau hoi sinh dan Skeleton. Hay ket thuc no!",
            500, 300, "obj_03_kill_boss", "Tieu diet Mage Skeleton (0/1)",
            ObjectiveType.KillEnemy, "MageSkeleton", 1);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateQuestAssets] Done! 3 QuestData assets created in Assets/Data/Quests/");

        AssetDatabase.DeleteAsset("Assets/Editor/CreateQuestAssets.cs");
    }

    static void CreateQuest(string path, string id, string questName, string desc,
        int gold, int exp, string objID, string objDesc,
        ObjectiveType objType, string target, int amount)
    {
        if (AssetDatabase.LoadAssetAtPath<QuestData>(path) != null) return;
        var q = ScriptableObject.CreateInstance<QuestData>();
        q.questID = id; q.questName = questName; q.description = desc;
        q.goldReward = gold; q.experienceReward = exp;
        q.objectives = new List<ObjectiveData>
        {
            new ObjectiveData
            {
                objectiveID = objID, description = objDesc,
                type = objType, targetID = target, requiredAmount = amount
            }
        };
        AssetDatabase.CreateAsset(q, path);
        Debug.Log($"[CreateQuestAssets] Created: {path}");
    }
}
#endif
