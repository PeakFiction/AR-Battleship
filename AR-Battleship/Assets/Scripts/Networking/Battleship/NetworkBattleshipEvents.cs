// Static event bridge used by the network controller to notify scene/UI scripts.
// Client RPCs raise these events locally so gameplay UI does not need to know
// about Netcode RPC implementation details.

using System;
using ARBattleship.Core.Application.Enums;

namespace ARBattleship.Multiplayer.Battleship
{
    /// <summary>
    /// Central event bus for multiplayer Battleship network events.
    /// </summary>
    public static class NetworkBattleshipEvents
    {
        /// <summary>Raised when this client is assigned Player 1 or Player 2.</summary>
        public static event Action<int>? LocalPlayerAssigned;

        /// <summary>Raised when the battle phase starts.</summary>
        public static event Action<int>? BattleStarted;

        /// <summary>Raised when a ship placement request is accepted by the server.</summary>
        public static event Action<int, string, int, int, int>? ShipPlacementAccepted;

        /// <summary>Raised when a ship placement request is rejected by the server.</summary>
        public static event Action<int, GameErrorCode>? ShipPlacementRejected;

        /// <summary>Raised when a shot is accepted and resolved by the server.</summary>
        public static event Action<int, int, int, ShotOutcome, int?, string?, string?>? ShotResolved;

        /// <summary>Raised when a shot request is rejected by the server.</summary>
        public static event Action<int, int, int, GameErrorCode>? ShotRejected;

        /// <summary>Raised when the server detects that the game has ended.</summary>
        public static event Action<int> GameOver;

        public static void RaiseLocalPlayerAssigned(int playerNumber)
        {
            LocalPlayerAssigned?.Invoke(playerNumber);
        }

        public static void RaiseBattleStarted(int startingPlayerNumber)
        {
            BattleStarted?.Invoke(startingPlayerNumber);
        }

        public static void RaiseShipPlacementAccepted(
            int playerNumber,
            string shipType,
            int startX,
            int startY,
            int orientationValue)
        {
            ShipPlacementAccepted?.Invoke(
                playerNumber,
                shipType,
                startX,
                startY,
                orientationValue
            );
        }

        public static void RaiseShipPlacementRejected(
            int playerNumber,
            GameErrorCode errorCode)
        {
            ShipPlacementRejected?.Invoke(playerNumber, errorCode);
        }

        public static void RaiseShotResolved(
            int shooterPlayerNumber,
            int x,
            int y,
            ShotOutcome outcome,
            int? hitSegmentIndex,
            string? shipOrientation,
            string? shipType)
        {
            ShotResolved?.Invoke(
                shooterPlayerNumber,
                x,
                y,
                outcome,
                hitSegmentIndex,
                shipOrientation,
                shipType
            );
        }

        public static void RaiseShotRejected(
            int shooterPlayerNumber,
            int x,
            int y,
            GameErrorCode errorCode)
        {
            ShotRejected?.Invoke(shooterPlayerNumber, x, y, errorCode);
        }

        public static void RaiseGameOver(int winnerPlayerNumber)
        {
            GameOver?.Invoke(winnerPlayerNumber);
        }
    }
}
