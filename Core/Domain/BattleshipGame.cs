using System;

namespace ARBattleship.Core.Domain
{
    public class BattleshipGame
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Board PlayerOneBoard { get; }
        public Board PlayerTwoBoard { get; }
        public PlayerId CurrentTurn { get; private set; }
        public GamePhase Phase { get; private set; }

        // Kept for backwards compatibility — delegates to Phase
        public bool IsGameOver => Phase == GamePhase.Finished;

        public PlayerId? Winner { get; private set; }

        public BattleshipGame(int size = 10)
        {
            PlayerOneBoard = new Board(size);
            PlayerTwoBoard = new Board(size);
            CurrentTurn = PlayerId.PlayerOne;
            Phase = GamePhase.Setup;
        }

        /// <summary>
        /// Transitions the game from Setup to InProgress.
        /// Fails if either board has no ships placed, or if the game is not in Setup phase.
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
        /// Fires a shot on behalf of the given player. Only valid during InProgress.
        /// </summary>
        public Result<FireResult> FireShot(PlayerId shooter, Coordinate coordinate)
        {
            if (Phase == GamePhase.Setup)
                return Result<FireResult>.Failure("Cannot fire during Setup. Call StartGame() first.");

            if (Phase == GamePhase.Finished)
                return Result<FireResult>.Failure("Game is already over.");

            if (shooter != CurrentTurn)
                return Result<FireResult>.Failure("Not your turn.");

            var targetBoard = (shooter == PlayerId.PlayerOne) ? PlayerTwoBoard : PlayerOneBoard;
            var result = targetBoard.FireAt(coordinate);

            if (result.IsSuccess)
            {
                if (targetBoard.AllShipsSunk())
                {
                    Phase = GamePhase.Finished;
                    Winner = shooter;
                }
                else
                {
                    // Switch turn only on a successful valid shot
                    CurrentTurn = (CurrentTurn == PlayerId.PlayerOne) ? PlayerId.PlayerTwo : PlayerId.PlayerOne;
                }
            }

            return result;
        }

        /// <summary>
        /// Places a ship for the given player. Only valid during Setup.
        /// Both players can place freely without taking turns.
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