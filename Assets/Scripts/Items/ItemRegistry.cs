using System.Collections.Generic;
using UnityEngine;

namespace CreatorKitCode
{
    /// <summary>
    /// ItemRegistry — Data Layer (ScriptableObject)
    ///
    /// Central registry of all Item assets in the project.
    /// Used by SaveSystem to resolve item names back to Item references
    /// when restoring inventory data after a scene transition.
    ///
    /// Setup:
    ///   1. Create one asset: Assets/Prefabs/ItemDatabase/ItemRegistry.asset
    ///   2. Drag all Item assets (Weapons, UsableItems, EquipmentItems)
    ///      from Assets/Prefabs/ItemDatabase/ into the Items list in the Inspector.
    ///
    /// Dependency: None (pure data — no MonoBehaviour, no scene references)
    /// </summary>
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "Inventory/Item Registry")]
    public class ItemRegistry : ScriptableObject
    {
        [Header("All Items in Project")]
        [SerializeField] private List<Item> _items = new List<Item>();

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Find an Item by its asset name (item.name).
        /// Used by SaveSystem when restoring inventory after scene load.
        /// </summary>
        /// <param name="itemName">The asset name of the item (e.g. "MetalAxe", "Potion")</param>
        /// <returns>The matching Item, or null if not found.</returns>
        public Item GetItemByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                Debug.LogWarning("[ItemRegistry] GetItemByName called with null or empty name.");
                return null;
            }

            foreach (var item in _items)
            {
                if (item != null && item.name == itemName)
                    return item;
            }

            Debug.LogWarning($"[ItemRegistry] Item '{itemName}' not found in registry. " +
                             $"Make sure it is added to the ItemRegistry asset.");
            return null;
        }

        /// <summary>
        /// Returns a read-only view of all registered items.
        /// </summary>
        public IReadOnlyList<Item> AllItems => _items;
    }
}
