namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Defines how the computer enemy selects a coordinate to fire at.
    /// All implementations must be free of Unity dependencies.
    /// </summary>
    public interface IEnemyStrategy
    {
        /// <summary>
        /// Selects the next coordinate for the AI to fire at.
        /// </summary>
        Coordinate SelectMove(Board opponentBoard);
    }
}
