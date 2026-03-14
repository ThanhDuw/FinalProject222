using UnityEngine;

/// <summary>
/// ScriptableObject that defines a single travel destination.
/// Create via: Assets > Create > Travel > Destination
/// 
/// Assign to NPCTraveler.availableDestinations list in the Inspector.
/// Each asset represents one map the player can travel to.
/// </summary>
[CreateAssetMenu(fileName = "TravelDestination", menuName = "Travel/Destination")]
public class TravelDestinationData : ScriptableObject
{
    [Header("Destination Info")]
    [Tooltip("Display name shown in the Travel Menu UI.")]
    [SerializeField] private string _destinationName;

    [Tooltip("The build index of the target scene (must match Build Settings).")]
    [SerializeField] private int _buildIndex;

    [Tooltip("The SpawnPoint ID in the target scene where the player will appear.")]
    [SerializeField] private string _spawnPointID;

    [Header("UI Display")]
    [Tooltip("Short description shown in the Travel Menu.")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    [Tooltip("Optional icon displayed next to the destination name in the UI.")]
    [SerializeField] private Sprite _icon;

    [Header("Availability")]
    [Tooltip("If false, this destination will be grayed out in the Travel Menu.")]
    [SerializeField] private bool _isAvailable = true;

    // ── Public Accessors ──────────────────────────────────────────────────────

    /// <summary>Display name of this destination.</summary>
    public string DestinationName => _destinationName;

    /// <summary>Build index of the target scene.</summary>
    public int BuildIndex => _buildIndex;

    /// <summary>ID of the SpawnPoint GameObject in the target scene.</summary>
    public string SpawnPointID => _spawnPointID;

    /// <summary>Short description shown in the Travel Menu.</summary>
    public string Description => _description;

    /// <summary>Icon displayed in the Travel Menu UI.</summary>
    public Sprite Icon => _icon;

    /// <summary>Whether this destination is currently available to travel to.</summary>
    public bool IsAvailable => _isAvailable;
}
