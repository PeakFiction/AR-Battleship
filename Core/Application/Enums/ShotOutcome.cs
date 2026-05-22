// Application-layer shot result enum.  Mirrors the domain ShotResult but is
// exposed across the application boundary (service return values, network events,
// UI callbacks).  Adds None = 0 as a safe default / "not yet resolved" value.

namespace ARBattleship.Core.Application.Enums
{
    /// <summary>
    /// The outcome of a fired shot as returned to the UI and network layers.
    /// Mapped from <c>ARBattleship.Core.Domain.ShotResult</c> by
    /// <c>BattleshipGameService.MapToOutcome</c>.
    /// </summary>
    public enum ShotOutcome
    {
        /// <summary>Default / unresolved.  Used by multiplayer before the host confirms the shot.</summary>
        None = 0,

        /// <summary>The shot hit open water.</summary>
        Miss = 1,

        /// <summary>The shot struck a ship that is not yet sunk.</summary>
        Hit = 2,

        /// <summary>The shot struck and sank a ship.</summary>
        Sunk = 3
    }
}
