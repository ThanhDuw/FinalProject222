using System;

/// <summary>
/// Static event bus decoupling Enemy/Item/Travel systems from the Quest System.
/// Raise events here; listeners subscribe from their own systems.
/// </summary>
public static class GameEvents
{
    // ── Gameplay Events ───────────────────────────────────────────────────────

    public static event Action<string>      OnEnemyKilled;          // enemyID
    public static event Action<string, int> OnItemCollected;        // itemID, amount
    public static event Action<string>      OnNPCTalkCompleted;     // npcID
    public static event Action<string>      OnLocationReached;      // locationID

    // ── Quest Events ──────────────────────────────────────────────────────────

    /// <summary>Notifies systems when a quest's progress changed (questID).</summary>
    public static event Action<string>      OnQuestProgressChanged;

    // ── Travel Events ─────────────────────────────────────────────────────────

    /// <summary>Notifies systems when the player travels to a new map (destinationName).</summary>
    public static event Action<string>      OnPlayerTraveled;

    /// <summary>
    /// Notifies UI systems to refresh after a scene transition completes.
    /// Raised by TravelManager one frame after a new scene finishes loading.
    /// QuestTrackerManager listens to this to re-subscribe and refresh the HUD.
    /// </summary>
    public static event Action OnSceneTransitionComplete;

    // ── Raise Methods ─────────────────────────────────────────────────────────

    public static void RaiseEnemyKilled(string enemyID)
        => OnEnemyKilled?.Invoke(enemyID);

    public static void RaiseItemCollected(string itemID, int amt)
        => OnItemCollected?.Invoke(itemID, amt);

    public static void RaiseNPCTalkCompleted(string npcID)
        => OnNPCTalkCompleted?.Invoke(npcID);

    public static void RaiseLocationReached(string locationID)
        => OnLocationReached?.Invoke(locationID);

    public static void RaiseQuestProgressChanged(string questID)
        => OnQuestProgressChanged?.Invoke(questID);

    public static void RaisePlayerTraveled(string destinationName)
        => OnPlayerTraveled?.Invoke(destinationName);

    public static void RaiseSceneTransitionComplete()
        => OnSceneTransitionComplete?.Invoke();
}
