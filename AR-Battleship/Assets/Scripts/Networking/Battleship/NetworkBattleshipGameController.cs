using ARBattleship.Core.Application.Commands;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Application.Services;
using ARBattleship.Core.Application.Events;
using ARBattleship.Core.Domain;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ARBattleship.Multiplayer.Battleship
{
    public sealed class NetworkBattleshipGameController : NetworkBehaviour
    {
        private readonly NetworkPlayerMapper _playerMapper = new();

        private BattleshipGame _game;
        private BattleshipGameService _gameService;
        private bool _gameOverBroadcasted = false;
        private readonly HashSet<ulong> _readyPlayers = new();

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            InitialiseServerGame();
            RegisterExistingPlayers();

            NetworkManager.Singleton.OnClientConnectedCallback += RegisterPlayer;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= RegisterPlayer;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
            }
        }

        private void InitialiseServerGame()
        {
            _game = new BattleshipGame();
            _gameService = new BattleshipGameService(_game);
            _gameOverBroadcasted = false;
        }

        private void RegisterExistingPlayers()
        {
            ulong hostClientId = NetworkManager.Singleton.LocalClientId;
            RegisterPlayer(hostClientId);

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId == hostClientId)
                {
                    continue;
                }

                RegisterPlayer(clientId);
            }
        }

        private void RegisterPlayer(ulong clientId)
        {
            bool registered = _playerMapper.TryRegisterPlayer(
                clientId,
                out PlayerId playerId
            );

            if (!registered)
            {
                Debug.LogWarning(
                    "A third player tried to join. Battleship only supports two players."
                );
                return;
            }

            int playerNumber = _playerMapper.ToPlayerNumber(playerId);

            AssignPlayerClientRpc(
                playerNumber,
                CreateTarget(clientId)
            );

            Debug.Log($"Client {clientId} assigned as Player {playerNumber}");
        }

        private void OnPlayerDisconnected(ulong clientId)
        {
            Debug.Log($"Battleship player disconnected: {clientId}");
        }

        [ClientRpc]
        private void AssignPlayerClientRpc(
            int playerNumber,
            ClientRpcParams clientRpcParams = default)
        {
            NetworkBattleshipEvents.RaiseLocalPlayerAssigned(playerNumber);
        }

        public void RequestPlaceShip(
            string shipType,
            int startX,
            int startY,
            Orientation orientation)
        {
            if (!IsSpawned)
            {
                Debug.LogWarning(
                    "Cannot place ship because the network object is not spawned."
                );
                return;
            }

            int orientationValue = (int)orientation;

            if (IsServer)
            {
                HandlePlaceShipServer(
                    NetworkManager.Singleton.LocalClientId,
                    shipType,
                    startX,
                    startY,
                    orientationValue
                );
            }
            else
            {
                PlaceShipServerRpc(
                    shipType,
                    startX,
                    startY,
                    orientationValue
                );
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlaceShipServerRpc(
            string shipType,
            int startX,
            int startY,
            int orientationValue,
            ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            HandlePlaceShipServer(
                senderClientId,
                shipType,
                startX,
                startY,
                orientationValue
            );
        }

        private void HandlePlaceShipServer(
            ulong senderClientId,
            string shipType,
            int startX,
            int startY,
            int orientationValue)
        {
            if (_gameService == null)
            {
                SendShipPlacementRejectedToClient(
                    senderClientId,
                    GameErrorCode.Unknown
                );
                return;
            }

            if (!_playerMapper.TryGetPlayerId(senderClientId, out PlayerId playerId))
            {
                SendShipPlacementRejectedToClient(
                    senderClientId,
                    GameErrorCode.InvalidPlayer
                );
                return;
            }

            Orientation orientation = (Orientation)orientationValue;

            var command = new ShipPlacementCommand(
                playerId,
                shipType,
                new Coordinate(startX, startY),
                orientation
            );

            var result = _gameService.TryPlaceShip(command);

            if (!result.IsSuccess)
            {
                SendShipPlacementRejectedToClient(
                    senderClientId,
                    result.Error
                );
                return;
            }

            int playerNumber = _playerMapper.ToPlayerNumber(playerId);

            ShipPlacementAcceptedClientRpc(
                playerNumber,
                shipType,
                startX,
                startY,
                orientationValue,
                CreateTarget(senderClientId)
            );

            ConsumeAndLogApplicationEvents();
        }

        private void SendShipPlacementRejectedToClient(
            ulong targetClientId,
            GameErrorCode errorCode)
        {
            int playerNumber = 0;

            if (_playerMapper.TryGetPlayerId(targetClientId, out PlayerId playerId))
            {
                playerNumber = _playerMapper.ToPlayerNumber(playerId);
            }

            ShipPlacementRejectedClientRpc(
                playerNumber,
                (int)errorCode,
                CreateTarget(targetClientId)
            );
        }

        [ClientRpc]
        private void ShipPlacementAcceptedClientRpc(
            int playerNumber,
            string shipType,
            int startX,
            int startY,
            int orientationValue,
            ClientRpcParams clientRpcParams = default)
        {
            NetworkBattleshipEvents.RaiseShipPlacementAccepted(
                playerNumber,
                shipType,
                startX,
                startY,
                orientationValue
            );
        }

        [ClientRpc]
        private void ShipPlacementRejectedClientRpc(
            int playerNumber,
            int errorCodeValue,
            ClientRpcParams clientRpcParams = default)
        {
            GameErrorCode errorCode = (GameErrorCode)errorCodeValue;

            NetworkBattleshipEvents.RaiseShipPlacementRejected(
                playerNumber,
                errorCode
            );
        }

        public void RequestStartGame()
        {
            if (!IsSpawned)
            {
                Debug.LogWarning(
                    "Cannot start game because the network object is not spawned."
                );
                return;
            }

            if (IsServer)
            {
                HandleStartGameServer(NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                StartGameServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc(ServerRpcParams rpcParams = default)
        {
            HandleStartGameServer(rpcParams.Receive.SenderClientId);
        }

        private void HandleStartGameServer(ulong senderClientId)
        {
            if (_game == null)
            {
                Debug.LogWarning("Cannot start game because BattleshipGame is missing.");
                return;
            }

            _readyPlayers.Add(senderClientId);
            Debug.Log($"Player {senderClientId} ready. {_readyPlayers.Count}/2 players ready.");

            if (_readyPlayers.Count < 2)
            {
                return;
            }

            var result = _game.StartGame();

            if (!result.IsSuccess)
            {
                Debug.LogWarning($"Could not start game: {result.Error}");
                return;
            }

            BattleStartedClientRpc(startingPlayerNumber: 1);
            ConsumeAndLogApplicationEvents();
        }

        [ClientRpc]
        private void BattleStartedClientRpc(int startingPlayerNumber)
        {
            NetworkBattleshipEvents.RaiseBattleStarted(startingPlayerNumber);
        }

        public void RequestFireShot(int x, int y)
        {
            if (!IsSpawned)
            {
                Debug.LogWarning(
                    "Cannot fire shot because the network object is not spawned."
                );
                return;
            }

            if (IsServer)
            {
                HandleFireShotServer(
                    NetworkManager.Singleton.LocalClientId,
                    x,
                    y
                );
            }
            else
            {
                FireShotServerRpc(x, y);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void FireShotServerRpc(
            int x,
            int y,
            ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            HandleFireShotServer(senderClientId, x, y);
        }

        private void HandleFireShotServer(
            ulong senderClientId,
            int x,
            int y)
        {
            if (_gameService == null)
            {
                SendShotRejectedToClient(
                    senderClientId,
                    x,
                    y,
                    GameErrorCode.Unknown
                );
                return;
            }

            if (!_playerMapper.TryGetPlayerId(senderClientId, out PlayerId playerId))
            {
                SendShotRejectedToClient(
                    senderClientId,
                    x,
                    y,
                    GameErrorCode.InvalidPlayer
                );
                return;
            }

            var command = new FireShotCommand(
                playerId,
                new Coordinate(x, y)
            );

            var result = _gameService.TryFireShot(command);

            if (!result.IsSuccess)
            {
                SendShotRejectedToClient(
                    senderClientId,
                    x,
                    y,
                    result.Error
                );
                return;
            }

            int shooterPlayerNumber = _playerMapper.ToPlayerNumber(playerId);

            int? hitSegmentIndex = null;
            string shipOrientation = null;
            string shipType = null;

            foreach (var gameEvent in _gameService.ConsumeEvents())
            {
                if (gameEvent is ShotFiredEvent shot)
                {
                    hitSegmentIndex = shot.HitSegmentIndex;
                    shipOrientation = shot.ShipOrientation;
                    shipType = shot.ShipType;
                }
                Debug.Log($"Application event: {gameEvent.GetType().Name}");
            }

            ShotResolvedClientRpc(
                shooterPlayerNumber,
                x,
                y,
                (int)result.Value,
                hitSegmentIndex ?? -1,
                shipOrientation ?? "",
                shipType ?? ""
            );

            TryBroadcastGameOver();
        }

        private void SendShotRejectedToClient(
            ulong targetClientId,
            int x,
            int y,
            GameErrorCode errorCode)
        {
            int playerNumber = 0;

            if (_playerMapper.TryGetPlayerId(targetClientId, out PlayerId playerId))
            {
                playerNumber = _playerMapper.ToPlayerNumber(playerId);
            }

            ShotRejectedClientRpc(
                playerNumber,
                x,
                y,
                (int)errorCode,
                CreateTarget(targetClientId)
            );
        }

        [ClientRpc]
        private void ShotResolvedClientRpc(
            int shooterPlayerNumber,
            int x,
            int y,
            int shotOutcomeValue,
            int hitSegmentIndex,
            string shipOrientation,
            string shipType)
        {
            ShotOutcome outcome = (ShotOutcome)shotOutcomeValue;

            NetworkBattleshipEvents.RaiseShotResolved(
                shooterPlayerNumber,
                x,
                y,
                outcome,
                hitSegmentIndex == -1 ? null : hitSegmentIndex,
                string.IsNullOrEmpty(shipOrientation) ? null : shipOrientation,
                string.IsNullOrEmpty(shipType) ? null : shipType
            );
        }

        [ClientRpc]
        private void ShotRejectedClientRpc(
            int shooterPlayerNumber,
            int x,
            int y,
            int errorCodeValue,
            ClientRpcParams clientRpcParams = default)
        {
            GameErrorCode errorCode = (GameErrorCode)errorCodeValue;

            NetworkBattleshipEvents.RaiseShotRejected(
                shooterPlayerNumber,
                x,
                y,
                errorCode
            );
        }

        private void TryBroadcastGameOver()
        {
            if (_game == null || _gameOverBroadcasted)
            {
                return;
            }

            if (!_game.IsGameOver || _game.Winner == null)
            {
                return;
            }

            int winnerPlayerNumber = _game.Winner == PlayerId.PlayerOne ? 1 : 2;

            _gameOverBroadcasted = true;

            GameOverClientRpc(winnerPlayerNumber);

            Debug.Log($"Game over. Winner: Player {winnerPlayerNumber}");
        }

        [ClientRpc]
        private void GameOverClientRpc(int winnerPlayerNumber)
        {
            NetworkBattleshipEvents.RaiseGameOver(winnerPlayerNumber);
        }

        private void ConsumeAndLogApplicationEvents()
        {
            if (_gameService == null)
            {
                return;
            }

            var events = _gameService.ConsumeEvents();

            foreach (var gameEvent in events)
            {
                Debug.Log($"Application event: {gameEvent.GetType().Name}");
            }
        }

        private ClientRpcParams CreateTarget(ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };
        }
    }
}