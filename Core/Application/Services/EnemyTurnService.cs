using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Services
{
    /// 
    /// Responsibilities:
    ///   - Validates it is actually the enemy's turn
    ///   - Asks the strategy to select a move
    ///   - Fires the shot via BattleshipGame
    ///   - Notifies the strategy of the outcome so it can update its internal state
    ///
    /// The Unity layer should call TakeTurn() after the human player's shot resolves
    /// and CurrentTurn == PlayerTwo.
    /// </summary>
    public class EnemyTurnService
    {
        private readonly BattleshipGame _game;
        private readonly HuntTargetStrategy _strategy;

        public EnemyTurnService(BattleshipGame game, HuntTargetStrategy strategy)
        {
            _game = game;
            _strategy = strategy;
        }

        /// <summary>
        /// Executes the enemy's turn. Should only be called when it is PlayerTwo's turn.
        /// </summary>
        /// <returns>
        /// A Result containing the FireResult (Miss / Hit / Sunk) on success,
        /// or a failure message if the turn could not be taken.
        /// </returns>
        public Result<FireResult> TakeTurn()
        {
            if (_game.IsGameOver)
                return Result<FireResult>.Failure("Game is already over.");

            if (_game.CurrentTurn != PlayerId.PlayerTwo)
                return Result<FireResult>.Failure("It is not the enemy's turn.");

            // Ask the strategy for a coordinate to fire at (reads PlayerOneBoard state)
            var coordinate = _strategy.SelectMove(_game.PlayerOneBoard);

            // Fire through the game — this validates the shot and advances the turn
            var result = _game.FireShot(PlayerId.PlayerTwo, coordinate);

            if (result.IsSuccess)
            {
                // Let the strategy know what happened so it can update hunt/target state
                _strategy.NotifyResult(result.Value!);
            }

            return result;
        }
    }
}