using System;

/// <summary>
/// Bus sự kiện tĩnh giúp tách biệt các hệ thống Enemy/Item/Travel khỏi Quest System.
/// Gửi sự kiện tại đây; các listener sẽ đăng ký từ hệ thống riêng của chúng.
/// </summary>
public static class GameEvents
{


    public static event Action<string>      OnEnemyKilled;
    public static event Action<string, int> OnItemCollected;
    public static event Action<string>      OnNPCTalkCompleted;
    public static event Action<string>      OnLocationReached;

    /// <summary>Thông báo cho các hệ thống khi tiến trình nhiệm vụ thay đổi.</summary>
    public static event Action<string>      OnQuestProgressChanged;

    /// <summary>Thông báo cho các hệ thống khi người chơi di chuyển sang bản đồ mới.</summary>
    public static event Action<string>      OnPlayerTraveled;

    /// <summary>
    /// Thông báo cho các hệ thống UI làm mới sau khi chuyển cảnh hoàn tất.
    /// Được gọi bởi TravelManager một frame sau khi cảnh mới tải xong.
    /// QuestTrackerManager lắng nghe sự kiện này để đăng ký lại và làm mới HUD.
    /// </summary>
    public static event Action OnSceneTransitionComplete;



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

    public static event Action<UnityEngine.GameObject> OnShowReward;
    public static void RaiseShowReward(UnityEngine.GameObject prefab)
        => OnShowReward?.Invoke(prefab);
}
