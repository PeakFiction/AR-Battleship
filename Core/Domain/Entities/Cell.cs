using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// A single addressable cell on the game board.
    /// Tracks ship occupancy and shot state.
    /// </summary>
    public class Cell
    {
        private readonly Coordinate _coordinate;
        private ShipId? _shipId = null;
        private bool _isShot = false;

        /// <summary>Creates a cell at the given coordinate.  Initially empty and unshot.</summary>
        public Cell(Coordinate coordinate) => _coordinate = coordinate;

        /// <summary>The board position of this cell.</summary>
        public Coordinate Coordinate => _coordinate;

        /// <summary>True when a ship occupies this cell.</summary>
        public bool HasShip => _shipId != null;

        /// <summary>True when this cell has been targeted by a shot.</summary>
        public bool IsShot => _isShot;

        /// <summary>
        /// The ID of the ship occupying this cell, or null if the cell is empty.
        /// Used by Board.FireAt to retrieve the Ship object and register the hit.
        /// </summary>
        public ShipId? ShipId => _shipId;

        // ─────────────────────────────────────────────────────────────────────
        // Mutation Methods
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns a ship to this cell during setup.
        /// Fails if the cell is already occupied.
        /// </summary>
        public Result<bool> PlaceShip(ShipId shipId)
        {
            if (_shipId != null)
                return Result<bool>.Failure("Cell already has a ship.");

            _shipId = shipId;
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Fires at this cell.  Sets IsShot = true and returns whether the cell
        /// contained a ship (Hit) or was empty (Miss).
        /// Fails if the cell was already shot.
        /// </summary>
        public Result<ShotResult> Shoot()
        {
            if (_isShot)
                return Result<ShotResult>.Failure("Cell already shot.");

            _isShot = true;
            return Result<ShotResult>.Success(HasShip ? ShotResult.Hit : ShotResult.Miss);
        }
    }
}
