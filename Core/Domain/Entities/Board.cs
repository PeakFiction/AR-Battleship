using System;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Domain 
{
    public class Board
    {
        private readonly int _size;
        private readonly Dictionary<Coordinate, Cell> _cells;
        private readonly Dictionary<ShipId, Ship> _ships;
        public bool HasAnyShips() => _ships.Count > 0;

        public int Size => _size;

        public Board(int size)
        {
            _size = size;
            _cells = new Dictionary<Coordinate, Cell>();
            _ships = new Dictionary<ShipId, Ship>();
            InitializeCells();
        }

        private void InitializeCells()
        {
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    var coord = new Coordinate(x, y);
                    _cells[coord] = new Cell(coord);
                }
            }
        }

        public Cell GetCell(Coordinate coord) => _cells[coord];
        public IEnumerable<Ship> GetShips() => _ships.Values;

        private bool IsWithinBounds(Coordinate coord)
        {
            return coord.X >= 0 && coord.X < _size &&
                   coord.Y >= 0 && coord.Y < _size;
        }

        public bool CanPlaceShip(IEnumerable<Coordinate> positions)
        {
            foreach (var pos in positions)
            {
                if (!IsWithinBounds(pos) || _cells[pos].HasShip)
                    return false;
            }
            return true;
        }

        private bool IsAdjacentToExistingShip(IEnumerable<Coordinate> positions)
        {
            foreach (var pos in positions)
            {
                var neighbours = new[]
                {
                    new Coordinate(pos.X - 1, pos.Y),
                    new Coordinate(pos.X + 1, pos.Y),
                    new Coordinate(pos.X, pos.Y - 1),
                    new Coordinate(pos.X, pos.Y + 1),
                    new Coordinate(pos.X - 1, pos.Y - 1), // NW
                    new Coordinate(pos.X + 1, pos.Y - 1), // NE
                    new Coordinate(pos.X - 1, pos.Y + 1), // SW
                    new Coordinate(pos.X + 1, pos.Y + 1), // SE
                };

                foreach (var neighbour in neighbours)
                {
                    if (IsWithinBounds(neighbour) && _cells[neighbour].HasShip)
                        return true;
                }
            }
            return false;
        }

        public Result<bool> PlaceShip(Ship ship)
        {
            // 1. Boundary & Overlap Check
            if (!CanPlaceShip(ship.Positions))
                return Result<bool>.Failure("Invalid placement: Ship is out of bounds or overlaps another ship.");

            // 2. Adjacency Check
            if (IsAdjacentToExistingShip(ship.Positions))
                return Result<bool>.Failure("Invalid placement: Ships cannot be placed adjacent to each other.");
            
            // 3. Unique ID Check
            if (_ships.ContainsKey(ship.Id))
                return Result<bool>.Failure("Ship with this ID already exists on the board.");

            // 4. Business Rule: Only one of each ship type allowed
            if (_ships.Values.Any(s => s.ShipType == ship.ShipType))
                return Result<bool>.Failure($"A {ship.ShipType} has already been placed.");

            // 5. Commit to Board State
            _ships[ship.Id] = ship;

            foreach (var pos in ship.Positions)
            {
                // Update the individual cells to let them know they now hold a ship
                var cellResult = _cells[pos].PlaceShip(ship.Id);
                
                if (!cellResult.IsSuccess)
                {
                    // Simple Rollback: If one cell fails, remove the ship record
                    _ships.Remove(ship.Id);
                    return Result<bool>.Failure($"Cell placement failed at {pos}: {cellResult.Error}");
                }
            }

            return Result<bool>.Success(true);
        }

        public Result<FireResult> FireAt(Coordinate coord)
        {
            // 1. Boundary Check
            if (!IsWithinBounds(coord))
                return Result<FireResult>.Failure("Coordinate out of bounds.");

            var cell = _cells[coord];

            // 2. Cell State Check (Prevents shooting the same spot twice)
            var shootResult = cell.Shoot();
            if (!shootResult.IsSuccess)
                return Result<FireResult>.Failure(shootResult.Error!);

            // 3. Handle a Miss
            if (shootResult.Value == ShotResult.Miss)
                return Result<FireResult>.Success(FireResult.Miss(coord));

            // 4. Handle a Hit
            // At this point, the cell confirmed it has a ship, but we must retrieve the Ship object
            if (cell.ShipId == null || !_ships.TryGetValue(cell.ShipId.Value, out var ship))
            {
                return Result<FireResult>.Failure("Data integrity error: Hit cell has no associated ship object.");
            }

            // Register the hit on the ship instance
            ship.RegisterHit(coord);

            // 5. Determine if the ship is now sunk
            // We pass ship.ShipType ("Carrier", "Destroyer", etc.) so the UI knows exactly what was hit
            if (ship.IsSunk)
            {
                return Result<FireResult>.Success(FireResult.Sunk(coord, ship.Id, ship.ShipType, ship.GetSegmentIndex(coord)));
            }

            return Result<FireResult>.Success(FireResult.Hit(coord, ship.Id, ship.ShipType, ship.GetSegmentIndex(coord)));
        }

        public bool AllShipsSunk() => _ships.Values.All(ship => ship.IsSunk);
    }
}