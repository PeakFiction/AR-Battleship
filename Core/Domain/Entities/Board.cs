using System;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// One player's game board.  Stores cells and ships, enforces placement
    /// rules, and processes shots.
    /// </summary>
    public class Board
    {
        private readonly int _size;
        private readonly Dictionary<Coordinate, Cell> _cells;
        private readonly Dictionary<ShipId, Ship> _ships;

        /// <summary>Edge length of the board (default 10 for 10×10 grid).</summary>
        public int Size => _size;

        /// <summary>True when at least one ship has been placed on this board.</summary>
        public bool HasAnyShips() => _ships.Count > 0;

        /// <summary>
        /// Creates a board of the given size and initialises all cells.
        /// </summary>
        /// <param name="size">Edge length (both width and height).  Default is 10.</param>
        public Board(int size)
        {
            _size  = size;
            _cells = new Dictionary<Coordinate, Cell>();
            _ships = new Dictionary<ShipId, Ship>();
            InitializeCells();
        }

        /// <summary>Populates _cells with a fresh Cell for every valid coordinate.</summary>
        private void InitializeCells()
        {
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                {
                    var coord = new Coordinate(x, y);
                    _cells[coord] = new Cell(coord);
                }
        }

        /// <summary>Returns the cell at the given coordinate.  Throws if not initialised.</summary>
        public Cell GetCell(Coordinate coord) => _cells[coord];

        /// <summary>Returns all ships currently placed on this board.</summary>
        public IEnumerable<Ship> GetShips() => _ships.Values;

        /// <summary>
        /// Returns true when all cells in all the given positions are within bounds
        /// and unoccupied.  Used by HuntTargetStrategy to pre-validate placements.
        /// </summary>
        public bool CanPlaceShip(IEnumerable<Coordinate> positions)
        {
            foreach (var pos in positions)
                if (!IsWithinBounds(pos) || _cells[pos].HasShip)
                    return false;

            return true;
        }

        /// <summary>Returns true when every placed ship has been fully sunk.</summary>
        public bool AllShipsSunk() => _ships.Values.All(ship => ship.IsSunk);

        /// <summary>
        /// Places a ship on the board, enforcing all placement rules:
        /// 1. Positions must be within bounds and unoccupied (CanPlaceShip).
        /// 2. No cell in the 8-neighbour zone of any existing ship may be used.
        /// 3. The ShipId must be unique.
        /// 4. Only one ship of each ShipType is allowed per board.
        /// </summary>
        /// <returns>
        /// Success(true) if placement was committed;
        /// Failure with a descriptive message otherwise.
        /// </returns>
        public Result<bool> PlaceShip(Ship ship)
        {
            if (!CanPlaceShip(ship.Positions))
                return Result<bool>.Failure("Invalid placement: Ship is out of bounds or overlaps another ship.");

            if (IsAdjacentToExistingShip(ship.Positions))
                return Result<bool>.Failure("Invalid placement: Ships cannot be placed adjacent to each other.");

            if (_ships.ContainsKey(ship.Id))
                return Result<bool>.Failure("Ship with this ID already exists on the board.");

            if (_ships.Values.Any(s => s.ShipType == ship.ShipType))
                return Result<bool>.Failure($"A {ship.ShipType} has already been placed.");

            _ships[ship.Id] = ship;

            foreach (var pos in ship.Positions)
            {
                var cellResult = _cells[pos].PlaceShip(ship.Id);

                if (!cellResult.IsSuccess)
                {
                    _ships.Remove(ship.Id);
                    return Result<bool>.Failure($"Cell placement failed at {pos}: {cellResult.Error}");
                }
            }

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Fires a shot at coord and returns a FireResult
        /// describing the outcome (Miss, Hit, or Sunk).
        /// </summary>
        public Result<FireResult> FireAt(Coordinate coord)
        {
            if (!IsWithinBounds(coord))
                return Result<FireResult>.Failure("Coordinate out of bounds.");

            var cell = _cells[coord];

            var shootResult = cell.Shoot();
            if (!shootResult.IsSuccess)
                return Result<FireResult>.Failure(shootResult.Error!);

            if (shootResult.Value == ShotResult.Miss)
                return Result<FireResult>.Success(FireResult.Miss(coord));

            if (cell.ShipId == null || !_ships.TryGetValue(cell.ShipId.Value, out var ship))
                return Result<FireResult>.Failure("Data integrity error: Hit cell has no associated ship object.");

            ship.RegisterHit(coord);

            if (ship.IsSunk)
                return Result<FireResult>.Success(
                    FireResult.Sunk(coord, ship.Id, ship.ShipType, ship.GetSegmentIndex(coord), ship.Orientation.ToString()));

            return Result<FireResult>.Success(
                FireResult.Hit(coord, ship.Id, ship.ShipType, ship.GetSegmentIndex(coord), ship.Orientation.ToString()));
        }

        /// <summary>Returns true when the coordinate falls inside the board boundary.</summary>
        private bool IsWithinBounds(Coordinate coord) =>
            coord.X >= 0 && coord.X < _size &&
            coord.Y >= 0 && coord.Y < _size;

        /// <summary>
        /// Returns true when any cell in positions is within one step
        /// (including diagonals) of any already-placed ship cell.
        /// Implements no adjacency rule.
        /// </summary>
        private bool IsAdjacentToExistingShip(IEnumerable<Coordinate> positions)
        {
            foreach (var pos in positions)
            {
                var neighbours = new[]
                {
                    new Coordinate(pos.X - 1, pos.Y),     // W
                    new Coordinate(pos.X + 1, pos.Y),     // E
                    new Coordinate(pos.X, pos.Y - 1),     // N
                    new Coordinate(pos.X, pos.Y + 1),     // S
                    new Coordinate(pos.X - 1, pos.Y - 1), // NW
                    new Coordinate(pos.X + 1, pos.Y - 1), // NE
                    new Coordinate(pos.X - 1, pos.Y + 1), // SW
                    new Coordinate(pos.X + 1, pos.Y + 1), // SE
                };

                foreach (var neighbour in neighbours)
                    if (IsWithinBounds(neighbour) && _cells[neighbour].HasShip)
                        return true;
            }
            return false;
        }
    }
}
