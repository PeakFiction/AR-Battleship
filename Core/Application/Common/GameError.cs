namespace ARBattleship.Core.Application.Common
{
    public enum GameError
    {
        WrongTurn,
        OutOfBounds,
        TileAlreadyShot,
        InvalidPlacement,
        InvalidShipType,
        InvalidPlayer,
        ShipOverlap,
        GameAlreadyOver,
        Unknown
    }
}