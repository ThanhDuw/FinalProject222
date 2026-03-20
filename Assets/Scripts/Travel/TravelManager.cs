using System.Collections;
using CreatorKitCode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TravelManager : MonoBehaviour
{
    public static TravelManager Instance { get; private set; }

    [SerializeField] private ItemRegistry _itemRegistry;

    private string _pendingSpawnPointID;
    private bool _isTraveling;
    private const string PlayerTag = "Player";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    public void TravelTo(TravelDestinationData destination)
    {
        if (destination == null) { Debug.LogWarning("[TravelManager] Null destination."); return; }
        if (!destination.IsAvailable) { Debug.LogWarning($"[TravelManager] '{destination.DestinationName}' not available."); return; }
        if (_isTraveling) { Debug.LogWarning("[TravelManager] Already traveling."); return; }

        _pendingSpawnPointID = destination.SpawnPointID;
        _isTraveling = true;

        RunSaveQuestData();
        RunSaveInventory();
        RunSaveEquipment();

        GameEvents.RaisePlayerTraveled(destination.DestinationName);
        Debug.Log($"[TravelManager] Traveling to '{destination.DestinationName}' (Build Index: {destination.BuildIndex})");
        SceneManager.LoadScene(destination.BuildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isTraveling) return;

        GameObject player = GameObject.FindWithTag(PlayerTag);
        if (player == null) { ResetTravelState(); return; }

        Transform sp = FindSpawnPoint(_pendingSpawnPointID);
        if (sp != null) { player.transform.position = sp.position; player.transform.rotation = sp.rotation; }

        ResetTravelState();
        RunRestoreInventory(player);
        RunRestoreEquipment(player);
        StartCoroutine(NotifyComplete());
    }

    private Transform FindSpawnPoint(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        GameObject go = GameObject.Find(id);
        return go != null ? go.transform : null;
    }

    private void RunSaveQuestData()
    {
        if (QuestManager.Instance == null) return;
        SaveSystem ss = FindFirstObjectByType<SaveSystem>();
        if (ss == null) return;
        QuestTracker qt = FindFirstObjectByType<QuestTracker>();
        var prog = qt != null ? qt.GetAllActiveProgresses() : null;
        var states = new System.Collections.Generic.Dictionary<string, QuestState>();
        foreach (QuestState s in System.Enum.GetValues(typeof(QuestState)))
            foreach (var q in QuestManager.Instance.GetQuestsByState(s))
                states[q.questID] = s;
        ss.SaveQuestData(states, prog);
        Debug.Log("[TravelManager] Quest data saved.");
    }

    private void RunSaveInventory()
    {
        GameObject p = GameObject.FindWithTag(PlayerTag);
        if (p == null) return;
        var cd = p.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        SaveSystem ss = FindFirstObjectByType<SaveSystem>();
        if (ss == null) return;
        ss.SaveInventoryData(cd.Inventory);
    }

    private void RunSaveEquipment()
    {
        GameObject p = GameObject.FindWithTag(PlayerTag);
        if (p == null) return;
        var cd = p.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        SaveSystem ss = FindFirstObjectByType<SaveSystem>();
        if (ss == null) return;
        ss.SaveEquipmentData(cd.Equipment);
    }

    private void RunRestoreInventory(GameObject player)
    {
        if (_itemRegistry == null) { Debug.LogWarning("[TravelManager] ItemRegistry not assigned."); return; }
        SaveSystem ss = FindFirstObjectByType<SaveSystem>();
        if (ss == null) return;
        var data = ss.LoadInventoryData();
        if (data == null || data.items == null || data.items.Count == 0) return;
        var cd = player.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        foreach (var model in data.items)
        {
            var item = _itemRegistry.GetItemByName(model.itemName);
            if (item == null) continue;
            for (int i = 0; i < model.count; i++)
                cd.Inventory.AddItem(item);
        }
        Debug.Log($"[TravelManager] Inventory restored: {data.items.Count} slot(s).");
    }

    private void RunRestoreEquipment(GameObject player)
    {
        if (_itemRegistry == null) return;
        SaveSystem ss = FindFirstObjectByType<SaveSystem>();
        if (ss == null) return;
        var data = ss.LoadEquipmentData();
        if (data == null) return;
        var cd = player.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        if (!string.IsNullOrEmpty(data.weaponName))
        {
            var weapon = _itemRegistry.GetItemByName(data.weaponName) as Weapon;
            if (weapon != null) cd.Equipment.Equip(weapon);
        }
        RestoreArmorSlot(cd, data.headName,      EquipmentItem.EquipmentSlot.Head);
        RestoreArmorSlot(cd, data.torsoName,     EquipmentItem.EquipmentSlot.Torso);
        RestoreArmorSlot(cd, data.legsName,      EquipmentItem.EquipmentSlot.Legs);
        RestoreArmorSlot(cd, data.feetName,      EquipmentItem.EquipmentSlot.Feet);
        RestoreArmorSlot(cd, data.accessoryName, EquipmentItem.EquipmentSlot.Accessory);
        Debug.Log("[TravelManager] Equipment restored.");
    }

    private void RestoreArmorSlot(CharacterData cd, string itemName, EquipmentItem.EquipmentSlot slot)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        var item = _itemRegistry.GetItemByName(itemName) as EquipmentItem;
        if (item != null) cd.Equipment.Equip(item);
    }

    private System.Collections.IEnumerator NotifyComplete()
    {
        yield return null;
        GameEvents.RaiseSceneTransitionComplete();
        Debug.Log("[TravelManager] Scene transition complete.");
    }

    private void ResetTravelState()
    {
        _isTraveling = false;
        _pendingSpawnPointID = null;
    }
}
