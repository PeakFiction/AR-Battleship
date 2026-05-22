using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Root domain aggregate for a Battleship match.
    /// </summary>
    public class BattleshipGame
    {

        /// <summary>Globally unique identifier for this game session.</summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>The human / local player's board (ships placed here).</summary>
        public Board PlayerOneBoard { get; }

        /// <summary>The AI / remote opponent's board.</summary>
        public Board PlayerTwoBoard { get; }

        /// <summary>The player whose turn it currently is.</summary>
        public PlayerId CurrentTurn { get; private set; }

        /// <summary>Current lifecycle phase of the game.</summary>
        public GamePhase Phase { get; private set; }

        /// <summary>
        /// Convenience shorthand for Phase == GamePhase.Finished.
        /// Kept for backwards compatibility with code that predates the Phase property.
        /// </summary>
        public bool IsGameOver => Phase == GamePhase.Finished;

        /// <summary>
        /// The player who won the game, or null if the game is still in progress.
        /// Set when AllShipsSunk() returns true after a shot.
        /// </summary>
        public PlayerId? Winner { get; private set; }

        /// <summary>
        /// Creates a new game with two empty boards.
        /// The game starts in GamePhase.Setup and it is PlayerOne's turn.
        /// </summary>
        public BattleshipGame(int size = 10)
        {
            PlayerOneBoard = new Board(size);
            PlayerTwoBoard = new Board(size);
            CurrentTurn    = PlayerId.PlayerOne; // PlayerOne always fires first
            Phase          = GamePhase.Setup;
        }

        /// <summary>
        /// Transitions the game from Setup to InProgress.
        /// Requires that both players have placed at least one ship.
        /// </summary>
        public Result<bool> StartGame()
        {
            if (Phase != GamePhase.Setup)
                return Result<bool>.Failure("StartGame can only be called during Setup.");

            if (!PlayerOneBoard.HasAnyShips())
                return Result<bool>.Failure("Player One has not placed any ships.");

            if (!PlayerTwoBoard.HasAnyShips())
                return Result<bool>.Failure("Player Two has not placed any ships.");

            Phase = GamePhase.InProgress;
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Fires a shot on behalf of shooter at
        /// coordinate on the opponent's board.
        /// If the shot is valid, advances the turn (or ends the game if all ships sunk).
        /// </summary>
        public Result<FireResult> FireShot(PlayerId shooter, Coordinate coordinate)
        {
            if (Phase == GamePhase.Setup)
                return Result<FireResult>.Failure("Cannot fire during Setup. Call StartGame() first.");

            if (Phase == GamePhase.Finished)
                return Result<FireResult>.Failure("Game is already over.");

            if (shooter != CurrentTurn)
                return Result<FireResult>.Failure("Not your turn.");

            // Each player fires at the opponent's board
            var targetBoard = (shooter == PlayerId.PlayerOne) ? PlayerTwoBoard : PlayerOneBoard;
            var result      = targetBoard.FireAt(coordinate);

            if (result.IsSuccess)
            {
                if (targetBoard.AllShipsSunk())
                {
                    // All opponent ships sunk — this player wins
                    Phase  = GamePhase.Finished;
                    Winner = shooter;
                }
                else
                {
                    // Alternate turns only on a valid shot; invalid shots (already shot)
                    // do not waste the player's turn.
                    CurrentTurn = (CurrentTurn == PlayerId.PlayerOne)
                        ? PlayerId.PlayerTwo
                        : PlayerId.PlayerOne;
                }
            }

            return result;
        }

        /// <summary>
        /// Places ship on the board belonging to player
        /// Only valid during GamePhase.Setup
        /// Both players may place ships simultaneously without turn restrictions.
        /// </summary>
        public Result<bool> PlaceShip(PlayerId player, Ship ship)
        {
            if (Phase != GamePhase.Setup)
                return Result<bool>.Failure("Ships can only be placed during Setup.");

            var targetBoard = (player == PlayerId.PlayerOne) ? PlayerOneBoard : PlayerTwoBoard;
            return targetBoard.PlaceShip(ship);
        }
    }
}
