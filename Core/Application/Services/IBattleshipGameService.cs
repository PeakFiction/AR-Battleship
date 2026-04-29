using System.Collections.Generic;
using ARBattleship.Core.Application.Commands;
using ARBattleship.Core.Application.Common;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Application.Events;
using ARBattleship.Core.Application.Snapshots;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Services
{
    public interface IBattleshipGameService
    {
        /// <summary>
        /// Generates a perspective-based snapshot of the game state for a specific player.
        /// </summary>
        GameSnapshot GetSnapshot(PlayerId viewer);

        /// <summary>
        /// Attempts to fire a shot at the opponent's board.
        /// </summary>
        Result<ShotOutcome, GameErrorCode> TryFireShot(FireShotCommand command);

        /// <summary>
        /// Attempts to place a ship on the player's own board.
        /// </summary>
<<<<<<< HEAD
        Result<bool, GameErrorCode> TryPlaceShip(ShipPlacementCommand command);
=======
        Result<PlacementOutcome, GameErrorCode> TryPlaceShip(ShipPlacementCommand command);
>>>>>>> parent of abccf51 ([feat] Completed Networking Logic)

        /// <summary>
        /// Retrieves all pending game events and clears the internal buffer.
        /// </summary>
        IReadOnlyList<IGameEvent> ConsumeEvents();
    }
}