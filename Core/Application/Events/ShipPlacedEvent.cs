// Raised when a ship is successfully placed during setup.
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// Raised after TryPlaceShip succeeds.  Can be used to update placement-progress UI
    /// (e.g. enable the START button only after all ships are placed).
    /// </summary>
    [Serializable]
    public sealed record ShipPlacedEvent(
        /// <summary>The player who placed the ship.</summary>
        PlayerId PlayerId,

        /// <summary>Type name of the placed ship (e.g. "Carrier").</summary>
        string ShipType
    ) : IGameEvent;
}
