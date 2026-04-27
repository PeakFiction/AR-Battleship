namespace ARBattleship.Core.Application.Enums
{
    public enum GameErrorCode
    {
        None = 0,

        Unknown = 1,

        InvalidPlayer = 10,
        NotPlayersTurn = 11,

        GameNotStarted = 20,
        GameAlreadyStarted = 21,
        GameAlreadyFinished = 22,

        InvalidCoordinate = 30,
        CellAlreadyTargeted = 31,

        InvalidShipPlacement = 40,
        ShipOutOfBounds = 41,
        ShipOverlap = 42
    }
}