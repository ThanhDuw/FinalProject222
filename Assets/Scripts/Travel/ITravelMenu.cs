using System;
using System.Collections.Generic;

/// <summary>
/// ITravelMenu — Interface for the Travel Menu UI.
///
/// Implemented by TravelMenuUI (created in Step 8).
/// Used by NPCTraveler to communicate with the UI without a direct type dependency,
/// allowing the UI layer to be created independently.
///
/// Per CLAUDE.md: systems communicate through interfaces/events, not direct references.
/// </summary>
public interface ITravelMenu
{
    /// <summary>
    /// Shows the travel menu populated with the given destinations.
    /// </summary>
    /// <param name="destinations">List of available travel destinations to display.</param>
    /// <param name="onSelected">Callback invoked when the player selects a destination.</param>
    void Show(List<TravelDestinationData> destinations, Action<TravelDestinationData> onSelected);

    /// <summary>
    /// Hides the travel menu panel.
    /// </summary>
    void Hide();
}
