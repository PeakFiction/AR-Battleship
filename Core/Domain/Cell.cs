public class Cell(Coordinate coordinate)
{
    private readonly Coordinate _coordinate = coordinate;
    private ShipId? _shipId = null;
    private bool _isShot = false;

    public Coordinate Coordinate => _coordinate;
    public bool HasShip => _shipId != null;
    public bool IsShot => _isShot;
    public ShipId? ShipId => _shipId;

    public void PlaceShip(ShipId shipId)
    {
        if (_shipId != null)
            throw new InvalidOperationException("Cell already has a ship.");

        _shipId = shipId;
    }

    public ShotResult Shoot()
    {
        if (_isShot)
            throw new InvalidOperationException("Cell already shot.");

        _isShot = true;

        return HasShip ? ShotResult.Hit : ShotResult.Miss;
    }
}