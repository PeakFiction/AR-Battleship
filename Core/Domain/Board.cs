public class Board
{
    private readonly int _size;
    private readonly Dictionary<Coordinate, Cell> _cells;
    private readonly Dictionary<ShipId, Ship> _ships;

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

    private bool IsWithinBounds(Coordinate coord)
    {
        return coord.X >= 0 && coord.X < _size &&
               coord.Y >= 0 && coord.Y < _size;
    }

    public bool CanPlaceShip(IEnumerable<Coordinate> positions)
    {
        foreach (var pos in positions)
        {
            if (!IsWithinBounds(pos))
                return false;

            if (_cells[pos].HasShip)
                return false;
        }

        return true;
    }

    public Result<bool> PlaceShip(Ship ship)
    {
        if (!CanPlaceShip(ship.Positions))
            return Result<bool>.Failure("Invalid ship placement (out of bounds or overlapping).");

        if (_ships.ContainsKey(ship.Id))
            return Result<bool>.Failure("Ship with the same ID already exists.");

        _ships[ship.Id] = ship;

        foreach (var pos in ship.Positions)
        {
            var placeResult = _cells[pos].PlaceShip(ship.Id);
            if (!placeResult.IsSuccess)
            {
                _ships.Remove(ship.Id);
                return Result<bool>.Failure($"Failed to place ship at {pos}: {placeResult.Error}");
            }
        }

        return Result<bool>.Success(true);
    }

    public Result<FireResult> FireAt(Coordinate coord)
    {
        if (!IsWithinBounds(coord))
            return Result<FireResult>.Failure("Coordinate out of bounds.");

        var cell = _cells[coord];

        var shootResult = cell.Shoot();
        if (!shootResult.IsSuccess)
            return Result<FireResult>.Failure(shootResult.Error!);

        switch (shootResult.Value)
        {
            case ShotResult.Miss:
                return Result<FireResult>.Success(FireResult.Miss(coord));

            case ShotResult.Hit:
            case ShotResult.Sunk:
                if (cell.ShipId == null || !_ships.TryGetValue(cell.ShipId.Value, out var ship))
                    return Result<FireResult>.Failure("Hit cell has no associated ship.");

                ship.RegisterHit(coord);

                return ship.IsSunk
                    ? Result<FireResult>.Success(FireResult.Sunk(coord, ship.Id))
                    : Result<FireResult>.Success(FireResult.Hit(coord, ship.Id));

            default:
                return Result<FireResult>.Failure("Invalid shot result");
        }
    }

    public bool AllShipsSunk()
    {
        return _ships.All(kvp => kvp.Value.IsSunk);
    }
}