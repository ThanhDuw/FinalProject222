using System;

/// <summary>
/// Static event bus decoupling Enemy/Item systems from the Quest System.
/// Raise events here; ObjectiveSystem subscribes.
/// </summary>
public static class GameEvents
{
    public static event Action<string>      OnEnemyKilled;      // enemyID
    public static event Action<string, int> OnItemCollected;    // itemID, amount
    public static event Action<string>      OnNPCTalkCompleted; // npcID
    public static event Action<string>      OnLocationReached;  // locationID

    // Notifies systems when a quest's progress changed (questID)
    public static event Action<string>      OnQuestProgressChanged;
    // Notifies systems when the player travels to a new map (destinationName)
    public static event Action<string>      OnPlayerTraveled;


    public static void RaiseEnemyKilled(string enemyID)           => OnEnemyKilled?.Invoke(enemyID);
    public static void RaiseItemCollected(string itemID, int amt)  => OnItemCollected?.Invoke(itemID, amt);
    public static void RaiseNPCTalkCompleted(string npcID)        => OnNPCTalkCompleted?.Invoke(npcID);
    public static void RaiseLocationReached(string locationID)    => OnLocationReached?.Invoke(locationID);

    public static void RaiseQuestProgressChanged(string questID)  => OnQuestProgressChanged?.Invoke(questID);
    public static void RaisePlayerTraveled(string destinationName) => OnPlayerTraveled?.Invoke(destinationName);

}
