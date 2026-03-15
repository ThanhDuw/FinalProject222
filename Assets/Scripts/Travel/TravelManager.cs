using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TravelManager — Infrastructure Layer (Singleton)
///
/// Responsibilities:
///   - Receives travel requests from NPCTraveler
///   - Persists the target SpawnPoint ID across scene loads via DontDestroyOnLoad
///   - Loads the destination scene via SceneManager
///   - Places the player at the correct SpawnPoint after the new scene loads
///
/// Dependency flow (per CLAUDE.md):
///   NPCTraveler -> TravelManager -> SceneManager
/// </summary>
public class TravelManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TravelManager Instance { get; private set; }

    // ── Runtime State ─────────────────────────────────────────────────────────

    /// <summary>SpawnPoint name to find when the next scene finishes loading.</summary>
    private string _pendingSpawnPointID;

    /// <summary>Whether a travel is currently in progress.</summary>
    private bool _isTraveling;

    // ── Constants ─────────────────────────────────────────────────────────────

    private const string PlayerTag = "Player";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates travel to the given destination.
    /// Called by NPCTraveler when the player confirms a destination in the Travel Menu.
    /// </summary>
    /// <param name="destination">The TravelDestinationData ScriptableObject for the target map.</param>
    public void TravelTo(TravelDestinationData destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("[TravelManager] TravelTo called with null destination.");
            return;
        }

        if (!destination.IsAvailable)
        {
            Debug.LogWarning($"[TravelManager] Destination '{destination.DestinationName}' is not available.");
            return;
        }

        if (_isTraveling)
        {
            Debug.LogWarning("[TravelManager] Travel already in progress. Ignoring duplicate request.");
            return;
        }

        _pendingSpawnPointID = destination.SpawnPointID;
        _isTraveling = true;

        // Notify other systems (e.g., Quest objectives) that player is traveling
        // Save quest data before scene transition to preserve progress
        SaveQuestDataBeforeTravel();

        GameEvents.RaisePlayerTraveled(destination.DestinationName);

        Debug.Log($"[TravelManager] Traveling to '{destination.DestinationName}' (Build Index: {destination.BuildIndex})");
        SceneManager.LoadScene(destination.BuildIndex);
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Called automatically by Unity when a new scene finishes loading.
    /// Teleports the player to the correct SpawnPoint.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isTraveling) return;

        GameObject player = GameObject.FindWithTag(PlayerTag);
        if (player == null)
        {
            Debug.LogWarning($"[TravelManager] Player (tag: '{PlayerTag}') not found in scene '{scene.name}' after load.");
            ResetTravelState();
            return;
        }

        Transform spawnPoint = FindSpawnPoint(_pendingSpawnPointID);
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            Debug.Log($"[TravelManager] Player placed at SpawnPoint '{_pendingSpawnPointID}' in scene '{scene.name}'.");
        }
        else
        {
            Debug.LogWarning($"[TravelManager] SpawnPoint '{_pendingSpawnPointID}' not found in scene '{scene.name}'. Player remains at default position.");
        }

        ResetTravelState();

        // Notify UI systems (e.g. QuestTrackerManager) to refresh after scene load
        // Wait one frame so all Start() methods in the new scene have run first
        StartCoroutine(NotifySceneTransitionComplete());
    }

    /// <summary>
    /// Finds a SpawnPoint GameObject in the current scene by its name.
    /// The SpawnPointID in TravelDestinationData should match the GameObject's name in the target scene.
    /// </summary>
    /// <param name="spawnPointID">The name of the SpawnPoint GameObject to find.</param>
    /// <returns>The matching Transform, or null if not found.</returns>
    private Transform FindSpawnPoint(string spawnPointID)
    {
        if (string.IsNullOrEmpty(spawnPointID))
        {
            Debug.LogWarning("[TravelManager] SpawnPointID is null or empty. Cannot find SpawnPoint.");
            return null;
        }

        GameObject found = GameObject.Find(spawnPointID);
        if (found != null)
            return found.transform;

        Debug.LogWarning($"[TravelManager] No GameObject named '{spawnPointID}' found in the current scene.");
        return null;
    }

    /// <summary>
    /// Resets travel state after a scene load completes.
    /// </summary>
    /// <summary>
    /// Saves quest data to PlayerPrefs before scene transition.
    /// Ensures active quest progress is not lost when the old scene is destroyed.
    /// QuestManager and SaveSystem must be available via their Singletons.
    /// </summary>
    private void SaveQuestDataBeforeTravel()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[TravelManager] QuestManager.Instance is null — quest data will not be saved before travel.");
            return;
        }

        // Find SaveSystem in scene — it lives inside QuestSystem GameObject
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
        if (saveSystem == null)
        {
            Debug.LogWarning("[TravelManager] SaveSystem not found — quest data will not be saved before travel.");
            return;
        }

        // Find QuestTracker to get active objective progress
        QuestTracker questTracker = FindFirstObjectByType<QuestTracker>();
        var activeProgresses = questTracker != null
            ? questTracker.GetAllActiveProgresses()
            : null;

        // Get all quest states from QuestManager
        var allStates = new System.Collections.Generic.Dictionary<string, QuestState>();
        foreach (QuestState state in System.Enum.GetValues(typeof(QuestState)))
        {
            var quests = QuestManager.Instance.GetQuestsByState(state);
            foreach (var q in quests)
                allStates[q.questID] = state;
        }

        saveSystem.SaveQuestData(allStates, activeProgresses);
        Debug.Log("[TravelManager] Quest data saved before travel.");
    }

    /// <summary>
    /// Waits one frame then raises OnSceneTransitionComplete so UI systems
    /// (e.g. QuestTrackerManager) can refresh their display after the new
    /// scene's Start() methods have all run.
    /// </summary>
    private IEnumerator NotifySceneTransitionComplete()
    {
        yield return null; // wait one frame
        GameEvents.RaiseSceneTransitionComplete();
        Debug.Log("[TravelManager] Scene transition complete — notified UI systems.");
    }

    private void ResetTravelState()
    {
        _isTraveling = false;
        _pendingSpawnPointID = null;
    }
}
