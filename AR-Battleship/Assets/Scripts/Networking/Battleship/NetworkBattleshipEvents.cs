using System;
using ARBattleship.Core.Application.Enums;

namespace ARBattleship.Multiplayer.Battleship
{
    public static class NetworkBattleshipEvents
    {
        public static event Action<int>? LocalPlayerAssigned;

        public static event Action<int>? BattleStarted;

        public static event Action<int, string, int, int, int>? ShipPlacementAccepted;

        public static event Action<int, GameErrorCode>? ShipPlacementRejected;

        public static event Action<int, int, int, ShotOutcome>? ShotResolved;

        public static event Action<int, int, int, GameErrorCode>? ShotRejected;

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
            ShotOutcome outcome)
        {
            ShotResolved?.Invoke(shooterPlayerNumber, x, y, outcome);
        }

        public static void RaiseShotRejected(
            int shooterPlayerNumber,
            int x,
            int y,
            GameErrorCode errorCode)
        {
            ShotRejected?.Invoke(shooterPlayerNumber, x, y, errorCode);
        }
    }
}