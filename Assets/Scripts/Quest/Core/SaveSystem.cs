using System;
using System.Collections.Generic;
using CreatorKitCode;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private const string QuestSaveKey     = "QuestSaveData";
    private const string InventorySaveKey = "InventorySaveData";
    private const string EquipmentSaveKey = "EquipmentSaveData";
    private const string HealthSaveKey    = "PlayerHealthData";
    private const string SceneSaveKey     = "PlayerSceneData";
    private const string MetadataSaveKey  = "SaveMetadata";



    public void SaveQuestData(Dictionary<string, QuestState> questStates, IEnumerable<QuestProgress> activeProgresses)
    {
        if (questStates == null) return;
        var list = new List<QuestSaveModel>();
        foreach (var kvp in questStates)
        {
            var model = new QuestSaveModel { questID = kvp.Key, state = kvp.Value, objectives = new List<ObjectiveSaveModel>() };
            if (activeProgresses != null)
                foreach (var p in activeProgresses)
                {
                    if (p == null || p.questData == null || p.questData.questID != kvp.Key) continue;
                    foreach (var oc in p.objectiveCounts)
                        model.objectives.Add(new ObjectiveSaveModel { objectiveID = oc.Key, currentCount = oc.Value });
                    break;
                }
            list.Add(model);
        }
        var json = JsonUtility.ToJson(new QuestWrapper { quests = list });
        PlayerPrefs.SetString(QuestSaveKey, json);
        PlayerPrefs.Save();
    }

    public void SaveQuestData(Dictionary<string, QuestState> questStates) { SaveQuestData(questStates, null); }

    public QuestWrapper LoadQuestData()
    {
        if (!PlayerPrefs.HasKey(QuestSaveKey)) return null;
        var json = PlayerPrefs.GetString(QuestSaveKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<QuestWrapper>(json); }
        catch (Exception e) { Debug.LogWarning("[SaveSystem] Load quest failed: " + e.Message); return null; }
    }

    public void ClearQuestData() { PlayerPrefs.DeleteKey(QuestSaveKey); PlayerPrefs.Save(); }



    public void SaveInventoryData(InventorySystem inventory)
    {
        if (inventory == null) { Debug.LogWarning("[SaveSystem] SaveInventoryData: null."); return; }
        var list = new List<InventorySaveModel>();
        foreach (var entry in inventory.Entries)
        {
            if (entry == null || entry.Item == null) continue;
            list.Add(new InventorySaveModel { itemName = entry.Item.name, count = entry.Count });
        }
        var json = JsonUtility.ToJson(new InventoryWrapper { items = list });
        PlayerPrefs.SetString(InventorySaveKey, json);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Inventory saved: " + list.Count + " slot(s).");
    }

    public InventoryWrapper LoadInventoryData()
    {
        if (!PlayerPrefs.HasKey(InventorySaveKey)) return null;
        var json = PlayerPrefs.GetString(InventorySaveKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<InventoryWrapper>(json); }
        catch (Exception e) { Debug.LogWarning("[SaveSystem] Load inventory failed: " + e.Message); return null; }
    }

    public void ClearInventoryData() { PlayerPrefs.DeleteKey(InventorySaveKey); PlayerPrefs.Save(); }



    public void SaveEquipmentData(EquipmentSystem equipment)
    {
        if (equipment == null) { Debug.LogWarning("[SaveSystem] SaveEquipmentData: null."); return; }
        var model = new EquipmentSaveModel
        {
            weaponName    = equipment.Weapon != null ? equipment.Weapon.name : "",
            headName      = equipment.GetItem(EquipmentItem.EquipmentSlot.Head)      != null ? equipment.GetItem(EquipmentItem.EquipmentSlot.Head).name      : "",
            torsoName     = equipment.GetItem(EquipmentItem.EquipmentSlot.Torso)     != null ? equipment.GetItem(EquipmentItem.EquipmentSlot.Torso).name     : "",
            legsName      = equipment.GetItem(EquipmentItem.EquipmentSlot.Legs)      != null ? equipment.GetItem(EquipmentItem.EquipmentSlot.Legs).name      : "",
            feetName      = equipment.GetItem(EquipmentItem.EquipmentSlot.Feet)      != null ? equipment.GetItem(EquipmentItem.EquipmentSlot.Feet).name      : "",
            accessoryName = equipment.GetItem(EquipmentItem.EquipmentSlot.Accessory) != null ? equipment.GetItem(EquipmentItem.EquipmentSlot.Accessory).name : ""
        };
        var json = JsonUtility.ToJson(model);
        PlayerPrefs.SetString(EquipmentSaveKey, json);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Equipment saved.");
    }

    public EquipmentSaveModel LoadEquipmentData()
    {
        if (!PlayerPrefs.HasKey(EquipmentSaveKey)) return null;
        var json = PlayerPrefs.GetString(EquipmentSaveKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<EquipmentSaveModel>(json); }
        catch (Exception e) { Debug.LogWarning("[SaveSystem] Load equipment failed: " + e.Message); return null; }
    }

    public void ClearEquipmentData() { PlayerPrefs.DeleteKey(EquipmentSaveKey); PlayerPrefs.Save(); }

    /// <summary>
    /// Lưu trữ lượng máu hiện tại của người chơi dưới dạng phần trăm (percentage).
    /// Việc lưu theo phần trăm giúp giá trị giữ nguyên tính hợp lệ nếu lượng máu tối đa thay đổi
    /// sau khi đổi trang bị ở cảnh mới.
    /// </summary>
    public void SaveHealthData(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[SaveSystem] SaveHealthData: null.");
            return;
        }
        int maxHp  = Mathf.Max(1, characterData.Stats.stats.health);
        float pct  = Mathf.Clamp01((float)characterData.Stats.CurrentHealth / maxHp);
        PlayerPrefs.SetFloat(HealthSaveKey, pct);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Health saved: " + characterData.Stats.CurrentHealth + "/" + maxHp);
    }

    /// <summary>Trả về lượng máu đã lưu dưới dạng phần trăm (0..1). Trả về -1 nếu không có dữ liệu.</summary>
    public float LoadHealthData()
    {
        if (!PlayerPrefs.HasKey(HealthSaveKey)) return -1f;
        return PlayerPrefs.GetFloat(HealthSaveKey, 1f);
    }

    public void ClearHealthData()
    {
        PlayerPrefs.DeleteKey(HealthSaveKey);
        PlayerPrefs.Save();
    }



    public void ClearAllSaveData()
    {
        ClearQuestData();
        ClearInventoryData();
        ClearEquipmentData();
        ClearHealthData();
        ClearSceneData();
        ClearMetadata();
    }
    /// <summary>Lưu index của cảnh đích và ID của điểm hồi sinh (spawn point ID) trước khi tải cảnh.</summary>
    public void SaveSceneData(int sceneIndex, string spawnPointID)
    {
        var model = new SceneSaveModel { currentSceneIndex = sceneIndex, spawnPointID = spawnPointID ?? "" };
        var json  = JsonUtility.ToJson(model);
        PlayerPrefs.SetString(SceneSaveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"[SaveSystem] Scene saved: index={sceneIndex}, spawn='{spawnPointID}'");
    }

    /// <summary>Trả về dữ liệu cảnh đã lưu, hoặc null nếu không có dữ liệu. Là hàm static để gọi được từ Main Menu.</summary>
    public static SceneSaveModel LoadSceneData()
    {
        if (!PlayerPrefs.HasKey(SceneSaveKey)) return null;
        var json = PlayerPrefs.GetString(SceneSaveKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<SceneSaveModel>(json); }
        catch (Exception e) { Debug.LogWarning("[SaveSystem] Load scene failed: " + e.Message); return null; }
    }

    public void ClearSceneData() { PlayerPrefs.DeleteKey(SceneSaveKey); PlayerPrefs.Save(); }

    /// <summary>Lưu thời gian và phiên bản lưu trữ vào PlayerPrefs.</summary>
    public void SaveMetadataEntry()
    {
        var model = new SaveMetadata
        {
            timestamp   = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            saveVersion = 1
        };
        PlayerPrefs.SetString(MetadataSaveKey, JsonUtility.ToJson(model));
        PlayerPrefs.Save();
    }

    /// <summary>Trả về metadata của dữ liệu đã lưu, hoặc null nếu không có.</summary>
    public SaveMetadata LoadMetadata()
    {
        if (!PlayerPrefs.HasKey(MetadataSaveKey)) return null;
        var json = PlayerPrefs.GetString(MetadataSaveKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<SaveMetadata>(json); }
        catch (Exception e) { Debug.LogWarning("[SaveSystem] Load metadata failed: " + e.Message); return null; }
    }

    public void ClearMetadata() { PlayerPrefs.DeleteKey(MetadataSaveKey); PlayerPrefs.Save(); }

    /// <summary>
    /// Trả về true nếu có dữ liệu lưu hợp lệ.
    /// Hàm dạng tĩnh (Static) nên MainMenuController có thể gọi nó mà không cần instance của MonoBehaviour.
    /// </summary>
    public static bool HasSaveData() => PlayerPrefs.HasKey(SceneSaveKey);

    /// <summary>
    /// Xóa toàn bộ key đã lưu. Được gọi bởi MainMenuController khi tạo New Game.
    /// Hàm dạng tĩnh nên có thể được gọi mà không cần instance của MonoBehaviour.
    /// </summary>
    public static void ClearAllData()
    {
        PlayerPrefs.DeleteKey(QuestSaveKey);
        PlayerPrefs.DeleteKey(InventorySaveKey);
        PlayerPrefs.DeleteKey(EquipmentSaveKey);
        PlayerPrefs.DeleteKey(HealthSaveKey);
        PlayerPrefs.DeleteKey(SceneSaveKey);
        PlayerPrefs.DeleteKey(MetadataSaveKey);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] All save data cleared.");
    }


    /// <summary>
    /// Lưu túi đồ, trang bị, máu, cảnh và metadata tất cả trong một lần gọi.
    /// Dữ liệu nhiệm vụ (Quest) bị loại trừ -- vì nó yêu cầu QuestManager/QuestTracker
    /// và được lưu riêng bởi TravelManager.SaveQuestData().
    /// Dành cho nút Pause / kích hoạt lưu thủ công.
    /// </summary>
    public void SaveAll(CharacterData characterData, int sceneIndex, string spawnPointID)
    {
        if (characterData != null)
        {
            SaveInventoryData(characterData.Inventory);
            SaveEquipmentData(characterData.Equipment);
            SaveHealthData(characterData);
        }
        SaveSceneData(sceneIndex, spawnPointID);
        SaveMetadataEntry();
        Debug.Log("[SaveSystem] SaveAll complete.");
    }



    [Serializable] public class QuestSaveModel { public string questID; public QuestState state; public List<ObjectiveSaveModel> objectives; }
    [Serializable] public class ObjectiveSaveModel { public string objectiveID; public int currentCount; }
    [Serializable] public class QuestWrapper { public List<QuestSaveModel> quests; }
    [Serializable] public class InventorySaveModel { public string itemName; public int count; }
    [Serializable] public class InventoryWrapper { public List<InventorySaveModel> items; }
    [Serializable] public class EquipmentSaveModel { public string weaponName; public string headName; public string torsoName; public string legsName; public string feetName; public string accessoryName; }
    [Serializable] public class SceneSaveModel { public int currentSceneIndex; public string spawnPointID; }
    [Serializable] public class SaveMetadata   { public string timestamp; public int saveVersion; }
}
