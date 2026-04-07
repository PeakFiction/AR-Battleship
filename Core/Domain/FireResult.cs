public record FireResult
{
    public Coordinate Coordinate { get; init; }
    public ShotResult Result { get; init; }
    public ShipId? ShipId { get; init; }

    public bool IsHit => Result is ShotResult.Hit or ShotResult.Sunk;
    public bool IsSunk => Result == ShotResult.Sunk;
    public string Message =>
        Result switch
        {
            ShotResult.Miss => "Miss",
            ShotResult.Hit => "Hit",
            ShotResult.Sunk => "Ship sunk!",
            _ => throw new InvalidOperationException("Invalid shot result")
        };

    private FireResult(Coordinate coordinate, ShotResult result, ShipId? shipId)
    {
        Coordinate = coordinate;
        Result = result;
        ShipId = shipId;
    }

    public static FireResult Miss(Coordinate coord) =>
        new(coord, ShotResult.Miss, null);

    public static FireResult Hit(Coordinate coord, ShipId shipId) =>
        new(coord, ShotResult.Hit, shipId);

    public static FireResult Sunk(Coordinate coord, ShipId shipId) =>
        new(coord, ShotResult.Sunk, shipId);
}