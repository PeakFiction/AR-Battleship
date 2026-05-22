// Raised when a specific ship is confirmed sunk.
// Preferred over relying on ShotOutcome.Sunk alone because it names the ship.
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// Raised when a ship is fully sunk.  Provides the owning player and ship type
    /// so fleet-status UI (ShipStatusUI) can mark the correct row.
    /// </summary>
    [Serializable]
    public sealed record ShipSunkEvent(
        /// <summary>The player whose ship was sunk.</summary>
        PlayerId PlayerId,

        /// <summary>Type name of the sunk ship (e.g. "Destroyer").</summary>
        string ShipType
    ) : IGameEvent;
}
