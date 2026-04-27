namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Defines how the computer enemy selects a coordinate to fire at.
    /// Implementations live in the domain layer and have no Unity dependencies.
    /// </summary>
    public interface IEnemyStrategy
    {
        /// <summary>
        /// Selects the next coordinate to fire at on the opponent's board.
        /// </summary>
        /// <param name="opponentBoard">The player's board — used to read cell states only.</param>
        /// <returns>The coordinate the enemy has chosen to fire at.</returns>
        Coordinate SelectMove(Board opponentBoard);
    }
}