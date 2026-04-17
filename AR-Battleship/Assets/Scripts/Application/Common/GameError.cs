namespace Core.Application.Common;

public enum GameErrorCode
{
    WrongTurn,
    OutOfBounds,
    TileAlreadyShot,
    InvalidPlacement,
    ShipOverlap,
    GameAlreadyOver,
    Unknown
}