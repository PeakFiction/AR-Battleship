// Raised when the game reaches the Finished phase.
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// Raised when a player's last ship is sunk and the game ends.
    /// GameManager uses this to invoke OnGameOver with the winner index.
    /// </summary>
    [Serializable]
    public sealed record GameEndedEvent(
        /// <summary>The PlayerId of the winning player.</summary>
        PlayerId Winner
    ) : IGameEvent;
}
