    public record FireResult(
        Coordinate Coordinate,
        ShotResult Result,
        ShipId? ShipId
    )
    {
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
    }