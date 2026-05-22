using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Describes the lifecycle phase of a BattleshipGame.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>
        /// Both players are placing their ships.
        /// FireShot is not allowed; PlaceShip is allowed for both players.
        /// </summary>
        Setup,

        /// <summary>
        /// All ships placed; the match is underway.
        /// FireShot is allowed; PlaceShip is forbidden.
        /// </summary>
        InProgress,

        /// <summary>
        /// One player's fleet has been fully sunk.
        /// No further moves are accepted.
        /// </summary>
        Finished
    }
}
