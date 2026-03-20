using System.Collections;
using System.Collections.Generic;
using CreatorKitCode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TravelManager : MonoBehaviour
{
    public static TravelManager Instance { get; private set; }

    [SerializeField] private ItemRegistry        _itemRegistry;
    [SerializeField] private SceneTransitionUI   _transitionUI;

    private string _pendingSpawnPointID;
    private bool   _isTraveling;
    private const string PlayerTag = "Player";

    // -- Lifecycle ------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Initiates travel to the destination.
    /// Caches SaveSystem, QuestTracker, and Player ONCE then passes them
    /// to all Save helpers -- avoids repeated FindFirstObjectByType calls.
    /// </summary>
    public void TravelTo(TravelDestinationData destination)
    {
        if (destination == null)      { Debug.LogWarning("[TravelManager] Null destination."); return; }
        if (!destination.IsAvailable) { Debug.LogWarning($"[TravelManager] '{destination.DestinationName}' not available."); return; }
        if (_isTraveling)             { Debug.LogWarning("[TravelManager] Already traveling."); return; }

        _pendingSpawnPointID = destination.SpawnPointID;
        _isTraveling = true;

        // FindFirstObjectByType called ONCE each -- shared across all Save helpers
        GameObject   player       = GameObject.FindWithTag(PlayerTag);
        SaveSystem   saveSystem   = FindFirstObjectByType<SaveSystem>();
        QuestTracker questTracker = FindFirstObjectByType<QuestTracker>();

        SaveQuestData(saveSystem, questTracker);
        SaveInventory(player, saveSystem);
        SaveEquipment(player, saveSystem);
        SaveHealth(player, saveSystem);

        GameEvents.RaisePlayerTraveled(destination.DestinationName);
        Debug.Log($"[TravelManager] Traveling to '{destination.DestinationName}' (Build Index: {destination.BuildIndex})");
        if (_transitionUI != null)
        {
            int buildIndex = destination.BuildIndex;
            _transitionUI.FadeOut(() => SceneManager.LoadScene(buildIndex));
        }
        else
        {
            SceneManager.LoadScene(destination.BuildIndex);
        }
    }

    // -- Scene Loaded ---------------------------------------------------------

    /// <summary>
    /// Called when new scene finishes loading.
    /// Caches SaveSystem ONCE then passes it to all Restore helpers.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isTraveling) return;

        GameObject player = GameObject.FindWithTag(PlayerTag);
        if (player == null) { ResetTravelState(); return; }

        Transform sp = FindSpawnPoint(_pendingSpawnPointID);
        if (sp != null)
        {
            player.transform.position = sp.position;
            player.transform.rotation = sp.rotation;
            Debug.Log($"[TravelManager] Player placed at '{_pendingSpawnPointID}' in '{scene.name}'.");
        }
        else
        {
            Debug.LogWarning($"[TravelManager] SpawnPoint '{_pendingSpawnPointID}' not found in '{scene.name}'.");
        }

        ResetTravelState();

        // FindFirstObjectByType called ONCE -- passed to RestoreAndNotify coroutine
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();

        // Wait one frame so CharacterData.Init() (called in CharacterControl.Start())
        // runs BEFORE we restore inventory and equipment.
        // Without this wait, m_DefaultWeapon is null during RestoreEquipment,
        // causing Init() to later add the starting weapon to inventory as a duplicate.
        StartCoroutine(RestoreAndNotify(player, saveSystem));
    }

    private Transform FindSpawnPoint(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        GameObject go = GameObject.Find(id);
        if (go != null) return go.transform;
        Debug.LogWarning($"[TravelManager] SpawnPoint '{id}' not found in current scene.");
        return null;
    }

    // -- Save Helpers ---------------------------------------------------------

    private void SaveQuestData(SaveSystem saveSystem, QuestTracker questTracker)
    {
        if (QuestManager.Instance == null || saveSystem == null) return;
        var prog   = questTracker != null ? questTracker.GetAllActiveProgresses() : null;
        var states = new Dictionary<string, QuestState>();
        foreach (QuestState s in System.Enum.GetValues(typeof(QuestState)))
            foreach (var q in QuestManager.Instance.GetQuestsByState(s))
                states[q.questID] = s;
        saveSystem.SaveQuestData(states, prog);
        Debug.Log("[TravelManager] Quest data saved.");
    }

    private void SaveInventory(GameObject player, SaveSystem saveSystem)
    {
        if (player == null || saveSystem == null) return;
        var cd = player.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        saveSystem.SaveInventoryData(cd.Inventory);
    }

    private void SaveEquipment(GameObject player, SaveSystem saveSystem)
    {
        if (player == null || saveSystem == null) return;
        var cd = player.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        saveSystem.SaveEquipmentData(cd.Equipment);
    }

    // -- Restore Helpers ------------------------------------------------------

    private void RestoreInventory(GameObject player, SaveSystem saveSystem)
    {
        if (_itemRegistry == null) { Debug.LogWarning("[TravelManager] ItemRegistry not assigned."); return; }
        if (saveSystem == null) return;
        var data = saveSystem.LoadInventoryData();
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

    private void RestoreEquipment(GameObject player, SaveSystem saveSystem)
    {
        if (_itemRegistry == null || saveSystem == null) return;
        var data = saveSystem.LoadEquipmentData();
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

    // -- Coroutine / State ----------------------------------------------------

    /// <summary>
    /// Waits one frame so all Start() methods in the new scene run first.
    /// Critically: CharacterData.Init() must run before RestoreEquipment()
    /// so that m_DefaultWeapon is set. Without this, Init() later calls
    /// StartingWeapon.UsedBy() -> Equip() -> Unequip() and since m_DefaultWeapon
    /// was null, it adds the starting weapon to inventory as a duplicate.
    /// </summary>
    private IEnumerator RestoreAndNotify(GameObject player, SaveSystem saveSystem)
    {
        yield return null; // wait one frame -- Start() / CharacterData.Init() runs here

        RestoreInventory(player, saveSystem);
        RestoreEquipment(player, saveSystem);
        RestoreHealth(player, saveSystem);

        // Fade back in now that the new scene is fully ready
        _transitionUI?.FadeIn();

        GameEvents.RaiseSceneTransitionComplete();
        Debug.Log("[TravelManager] Scene transition complete.");
    }

    private void SaveHealth(GameObject player, SaveSystem saveSystem)
    {
        if (player == null || saveSystem == null) return;
        var cd = player.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        saveSystem.SaveHealthData(cd);
    }

    private void RestoreHealth(GameObject player, SaveSystem saveSystem)
    {
        if (saveSystem == null) return;
        float pct = saveSystem.LoadHealthData();
        if (pct < 0f) return;
        var cd = player.GetComponentInChildren<CharacterData>();
        if (cd == null) return;
        // Apply as delta on top of the current (Init-set) health
        int targetHp = Mathf.RoundToInt(pct * cd.Stats.stats.health);
        int delta    = targetHp - cd.Stats.CurrentHealth;
        if (delta != 0) cd.Stats.ChangeHealth(delta);
        Debug.Log("[TravelManager] Health restored: " + cd.Stats.CurrentHealth + "/" + cd.Stats.stats.health);
    }

    private void ResetTravelState()
    {
        _isTraveling         = false;
        _pendingSpawnPointID = null;
    }
}

