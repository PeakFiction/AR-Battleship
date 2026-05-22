// Orchestrates one AI turn: validates it is PlayerTwo's turn, asks the
// strategy for a coordinate, fires through BattleshipGame, and feeds the
// result back to the strategy so it can update its Hunt/Target state.
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Services
{
    /// <summary>
    /// Executes the AI opponent's turn by coordinating between the game domain
    /// </summary>
    public class EnemyTurnService
    {
        private readonly BattleshipGame _game;
        private readonly HuntTargetStrategy _strategy;

        /// <summary>
        /// Creates the service with the given game and AI strategy.
        /// Both references are kept for the lifetime of the game session.
        /// </summary>
        public EnemyTurnService(BattleshipGame game, HuntTargetStrategy strategy)
        {
            _game     = game;
            _strategy = strategy;
        }

        /// <summary>
        /// Executes one AI turn.  Should only be called when it is PlayerTwo's turn.
        /// </summary>
        public Result<FireResult> TakeTurn()
        {
            if (_game.IsGameOver)
                return Result<FireResult>.Failure("Game is already over.");

            if (_game.CurrentTurn != PlayerId.PlayerTwo)
                return Result<FireResult>.Failure("It is not the enemy's turn.");

            // Strategy reads PlayerOneBoard to select the next shot
            var coordinate = _strategy.SelectMove(_game.PlayerOneBoard);

            // Fire through the domain — validates the shot and advances the turn
            var result = _game.FireShot(PlayerId.PlayerTwo, coordinate);

            if (result.IsSuccess)
            {
                // Notify the strategy so it can update Hunt/Target state machine
                _strategy.NotifyResult(result.Value!);
            }

            return result;
        }
    }
}
