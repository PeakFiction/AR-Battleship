// Immutable command object passed to BattleshipGameService.TryFireShot.
// Encapsulates who is firing and where — no mutable state, safe to log.
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Commands
{
    /// <summary>
    /// Request to fire a shot at a specific coordinate on the opponent's board.
    /// Created by the UI or network layer and validated by the service.
    /// </summary>
    [Serializable]
    public sealed class FireShotCommand
    {
        /// <summary>The player submitting the shot.</summary>
        public PlayerId PlayerId { get; init; }

        /// <summary>The cell being targeted (0-based column and row).</summary>
        public Coordinate Coordinate { get; init; }

        /// <summary>Creates a fire shot command for the given player and target.</summary>
        public FireShotCommand(PlayerId playerId, Coordinate coordinate)
        {
            PlayerId   = playerId;
            Coordinate = coordinate;
        }
    }
}
