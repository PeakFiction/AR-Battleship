// Raised after each valid shot to signal whose turn is next.
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// Raised after a valid shot to notify subscribers of the new current player.
    /// CombatUI can use this to update the turn indicator without polling GameManager.
    /// </summary>
    [Serializable]
    public sealed record TurnChangedEvent(
        /// <summary>The player who now has the right to fire.</summary>
        PlayerId CurrentTurn
    ) : IGameEvent;
}
