using System.Collections.Generic;
using ARBattleship.Core.Application.Commands;
using ARBattleship.Core.Application.Common;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Application.Events;
using ARBattleship.Core.Application.Snapshots;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Services
{
    /// <summary>
    /// Contract for the application service that mediates between the UI/network
    /// layer and the Battleship domain model.
    /// </summary>
    public interface IBattleshipGameService
    {
        /// <summary>
        /// Returns a perspective-based snapshot of the current board state.
        /// Own ships are visible; opponent ships are hidden until shot or sunk.
        /// </summary>
        /// <param name="viewer">The player requesting the snapshot.</param>
        GameSnapshot GetSnapshot(PlayerId viewer);

        /// <summary>
        /// Attempts to fire a shot on the viewer's behalf.
        /// Validates phase, turn, and coordinate before delegating to the domain.
        /// </summary>
        Result<ShotOutcome, GameErrorCode> TryFireShot(FireShotCommand command);

        /// <summary>
        /// Attempts to place a ship during the Setup phase.
        /// Calculates all cell positions from the command's start coordinate,
        /// orientation, and ship type, then delegates to the domain.
        /// </summary>
        Result<bool, GameErrorCode> TryPlaceShip(ShipPlacementCommand command);

        /// <summary>
        /// Retrieves all events queued since the last call and clears the buffer.
        /// Events should be processed by the caller immediately after each service call.
        /// </summary>
        IReadOnlyList<IGameEvent> ConsumeEvents();
    }
}
