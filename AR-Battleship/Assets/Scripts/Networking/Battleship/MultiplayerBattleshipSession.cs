using System.Collections.Generic;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;
using UnityEngine;

namespace ARBattleship.Multiplayer.Battleship
{
    public enum MultiplayerBattlePhase
    {
        WaitingForAssignment,
        ShipPlacement,
        Battle,
        GameOver
    }

    public sealed class MultiplayerBattleshipSession : MonoBehaviour
    {
        [SerializeField] private NetworkBattleshipGameController networkController;

        private readonly HashSet<string> placedShips = new HashSet<string>();
        private readonly bool[,] firedCells = new bool[10, 10];

        private int localPlayerNumber;
        private MultiplayerBattlePhase phase = MultiplayerBattlePhase.WaitingForAssignment;

        public int LocalPlayerNumber => localPlayerNumber;
        public MultiplayerBattlePhase Phase => phase;

        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted += OnShipPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected += OnShipPlacementRejected;
            NetworkBattleshipEvents.BattleStarted += OnBattleStarted;
            NetworkBattleshipEvents.ShotResolved += OnShotResolved;
            NetworkBattleshipEvents.ShotRejected += OnShotRejected;
        }

        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted -= OnShipPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected -= OnShipPlacementRejected;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
            NetworkBattleshipEvents.ShotResolved -= OnShotResolved;
            NetworkBattleshipEvents.ShotRejected -= OnShotRejected;
        }

        public void PlaceShip(string shipType, int x, int y, Orientation orientation)
        {
            if (phase != MultiplayerBattlePhase.ShipPlacement)
            {
                Debug.LogWarning("Cannot place ship outside placement phase.");
                return;
            }

            if (placedShips.Contains(shipType))
            {
                Debug.LogWarning($"{shipType} has already been placed.");
                return;
            }

            networkController.RequestPlaceShip(shipType, x, y, orientation);
        }

        public void StartGame()
        {
            if (phase != MultiplayerBattlePhase.ShipPlacement)
            {
                Debug.LogWarning("Cannot start game outside placement phase.");
                return;
            }

            networkController.RequestStartGame();
        }

        public void FireShot(int x, int y)
        {
            if (phase != MultiplayerBattlePhase.Battle)
            {
                Debug.LogWarning("Cannot fire before battle starts.");
                return;
            }

            if (x < 0 || x >= 10 || y < 0 || y >= 10)
            {
                Debug.LogWarning($"Invalid shot coordinate: ({x}, {y})");
                return;
            }

            if (firedCells[x, y])
            {
                Debug.LogWarning($"Already fired at ({x}, {y}).");
                return;
            }

            networkController.RequestFireShot(x, y);
        }

        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
            phase = MultiplayerBattlePhase.ShipPlacement;

            Debug.Log($"[Multiplayer] You are Player {playerNumber}.");
        }

        private void OnShipPlacementAccepted(
            int playerNumber,
            string shipType,
            int startX,
            int startY,
            int orientationValue)
        {
            if (playerNumber != localPlayerNumber)
            {
                return;
            }

            placedShips.Add(shipType);

            Debug.Log(
                $"[Multiplayer] Placed {shipType} at ({startX}, {startY}) " +
                $"{(Orientation)orientationValue}."
            );
        }

        private void OnShipPlacementRejected(int playerNumber, GameErrorCode errorCode)
        {
            if (playerNumber != localPlayerNumber)
            {
                return;
            }

            Debug.LogWarning($"[Multiplayer] Placement rejected: {errorCode}");
        }

        private void OnBattleStarted(int startingPlayerNumber)
        {
            phase = MultiplayerBattlePhase.Battle;

            Debug.Log($"[Multiplayer] Battle started. Player {startingPlayerNumber} goes first.");
        }

        private void OnShotResolved(
            int shooterPlayerNumber,
            int x,
            int y,
            ShotOutcome outcome,
            int? hitSegmentIndex,
            string? shipOrientation,
            string? shipType)
        {
            if (shooterPlayerNumber == localPlayerNumber)
            {
                firedCells[x, y] = true;
                Debug.Log($"[Multiplayer] You fired at ({x}, {y}): {outcome}");
            }
            else
            {
                Debug.Log($"[Multiplayer] Opponent fired at your ({x}, {y}): {outcome}");
            }
        }

        private void OnShotRejected(
            int shooterPlayerNumber,
            int x,
            int y,
            GameErrorCode errorCode)
        {
            if (shooterPlayerNumber != localPlayerNumber)
            {
                return;
            }

            Debug.LogWarning($"[Multiplayer] Shot rejected at ({x}, {y}): {errorCode}");
        }
    }
}