// =============================================================================
// PlayerSnapshot.cs  |  ARBattleship.Core.Application.Snapshots
// =============================================================================
// Read-only view of one player's board state (cells + ships) at a point in time.
// Included as PlayerOne / PlayerTwo inside GameSnapshot.
// =============================================================================
using System;
using System.Collections.Generic;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Snapshots
{
    /// <summary>
    /// Immutable snapshot of a single player's board.
    /// Contains all cells (projected for the viewer) and a list of known ships.
    /// </summary>
    [Serializable]
    public sealed record PlayerSnapshot
    {
        /// <summary>The player whose board this snapshot describes.</summary>
        public PlayerId Id { get; init; }

        /// <summary>
        /// All cells on the board, projected from the viewer's perspective.
        /// Count = Size × Size (100 for a 10×10 board).
        /// </summary>
        public IReadOnlyList<CellView> Cells { get; init; }

        /// <summary>
        /// Ships visible to the viewer.
        /// Owner's view: all ships.  Opponent's view: only sunk ships.
        /// </summary>
        public IReadOnlyList<ShipView> Ships { get; init; }

        /// <summary>Creates a player snapshot with the given cells and ships.</summary>
        public PlayerSnapshot(PlayerId playerId, IReadOnlyList<CellView> cells, IReadOnlyList<ShipView> ships)
        {
            Id    = playerId;
            Cells = cells;
            Ships = ships;
        }
    }
}
