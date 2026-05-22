// Legacy error enum used in early application-layer code before GameErrorCode
// was introduced.  Retained for backwards compatibility.
// New code should use GameErrorCode (Application/Enums/GameErrorCode.cs).

namespace ARBattleship.Core.Application.Common
{
    /// <summary>
    /// Legacy error category enum.
    /// Prefer <see cref="ARBattleship.Core.Application.Enums.GameErrorCode"/> in new code.
    /// </summary>
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
