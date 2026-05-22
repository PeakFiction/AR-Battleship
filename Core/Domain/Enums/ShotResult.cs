using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Low-level outcome of a single shot as determined by the domain.
    /// Mapped to ARBattleship.Core.Application.Enums.ShotOutcome
    /// before being returned to UI or network layers.
    /// </summary>
    public enum ShotResult
    {
        /// <summary>The shot hit open water — no ship at that coordinate.</summary>
        Miss = 0,

        /// <summary>The shot hit a ship that is not yet fully sunk.</summary>
        Hit = 1,

        /// <summary>The shot hit the last remaining cell of a ship, sinking it.</summary>
        Sunk = 2
    }
}
