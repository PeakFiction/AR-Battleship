public enum ShipType
{
    AircraftCarrier,
    Battleship,
    Cruiser,
    Submarine,
    Destroyer
}

public enum Orientation
{
    Horizontal,
    Vertical
}

public class Ship
{
    public ShipType Type { get; private set; }
    public int Size { get; private set; }
    public int OriginX { get; private set; }
    public int OriginY { get; private set; }
    public Orientation Orientation { get; private set; }
    public int HitsReceived { get; private set; }

    public bool IsSunk => HitsReceived >= Size;

    public Ship(ShipType type, int originX, int originY, Orientation orientation)
    {
        Type = type;
        Size = GetSize(type);
        OriginX = originX;
        OriginY = originY;
        Orientation = orientation;
        HitsReceived = 0;
    }

    public void RegisterHit() => HitsReceived++;

    public static int GetSize(ShipType type)
    {
        return type switch
        {
            ShipType.AircraftCarrier => 5,
            ShipType.Battleship      => 4,
            ShipType.Cruiser         => 3,
            ShipType.Submarine       => 3,
            ShipType.Destroyer       => 2,
            _                        => 0
        };
    }
}