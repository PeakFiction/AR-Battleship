// Server-authoritative multiplayer controller for Battleship gameplay.
// Public request methods can be called by local UI/session scripts. The server
// validates those requests against the application-layer game service and then
// broadcasts results back to clients through ClientRpc methods.

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
    /// <summary>
    /// Owns the network-facing Battleship gameplay flow for player assignment,
    /// ship placement, battle start, shot resolution, and game-over broadcasts.
    /// </summary>
    public sealed class NetworkBattleshipGameController : NetworkBehaviour
    {
        // Converts Unity Netcode client IDs into domain-level player identities.
        private readonly NetworkPlayerMapper _playerMapper = new();

        // Server-side domain game and application service. These should only be
        // created and mutated by the server.
        private BattleshipGame _game;
        private BattleshipGameService _gameService;
        // Prevents duplicate GameOver events when multiple end-game checks happen.
        private bool _gameOverBroadcasted = false;
        // Tracks which connected clients have requested to start the game.
        private readonly HashSet<ulong> _readyPlayers = new();

        public override void OnNetworkSpawn()
        {
            // Only the server owns the authoritative game state and player mapping.
            if (!IsServer)
            {
                return;
            }

            InitialiseServerGame();
            RegisterExistingPlayers();

            // Keep player assignments updated as clients connect and disconnect.
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

        /// <summary>
        /// Creates a fresh server-side Battleship game and application service.
        /// </summary>
        private void InitialiseServerGame()
        {
            _game = new BattleshipGame();
            _gameService = new BattleshipGameService(_game);
            _gameOverBroadcasted = false;
        }

        /// <summary>
        /// Assigns players that are already connected when this network object spawns.
        /// </summary>
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

        /// <summary>
        /// Registers a connected Netcode client as Player 1 or Player 2.
        /// </summary>
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

        // Runs on the target client and publishes the local player assignment.
        [ClientRpc]
        private void AssignPlayerClientRpc(
            int playerNumber,
            ClientRpcParams clientRpcParams = default)
        {
            NetworkBattleshipEvents.RaiseLocalPlayerAssigned(playerNumber);
        }

        /// <summary>
        /// Requests ship placement from the local client. Hosts handle the request
        /// directly; remote clients send it to the server through an RPC.
        /// </summary>
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

        // RequireOwnership is false so either connected player can submit gameplay input.
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

        /// <summary>
        /// Validates and applies a ship placement on the authoritative server.
        /// </summary>
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

            // Convert primitive RPC payloads back into domain/application value types.
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

            // Only the requesting client needs to know that its placement was accepted.
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

        /// <summary>
        /// Sends a placement rejection to the client that submitted the invalid request.
        /// </summary>
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

        // Converts an accepted placement RPC into a local C# event.
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

        // Converts a rejected placement RPC into a local C# event.
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

        /// <summary>
        /// Requests the start of the battle phase once players are ready.
        /// </summary>
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

        /// <summary>
        /// Marks a player as ready and starts the game after both players are ready.
        /// </summary>
        private void HandleStartGameServer(ulong senderClientId)
        {
            if (_game == null)
            {
                Debug.LogWarning("Cannot start game because BattleshipGame is missing.");
                return;
            }

            // A HashSet prevents the same client from increasing the ready count twice.
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

            // The current domain game starts with Player 1. Broadcast that to both clients.
            BattleStartedClientRpc(startingPlayerNumber: 1);
            ConsumeAndLogApplicationEvents();
        }

        // Tells all clients to enter the battle phase.
        [ClientRpc]
        private void BattleStartedClientRpc(int startingPlayerNumber)
        {
            NetworkBattleshipEvents.RaiseBattleStarted(startingPlayerNumber);
        }

        /// <summary>
        /// Requests a shot at the specified grid coordinate.
        /// </summary>
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

        // Remote clients submit shot requests here for server-side validation.
        [ServerRpc(RequireOwnership = false)]
        private void FireShotServerRpc(
            int x,
            int y,
            ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            HandleFireShotServer(senderClientId, x, y);
        }

        /// <summary>
        /// Validates, applies, and broadcasts a shot from the authoritative server.
        /// </summary>
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

            // Consume application events so hit metadata can be forwarded to visual/UI code.
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

            // Broadcast resolved shots to all clients so both boards/UI can update.
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

        /// <summary>
        /// Sends a shot rejection to the client that submitted the invalid request.
        /// </summary>
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

        // Converts a resolved-shot RPC into a local C# event for UI/visual scripts.
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

        // Converts a rejected-shot RPC into a local C# event.
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

        /// <summary>
        /// Broadcasts game-over once the domain game reports a winner.
        /// </summary>
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

        // Raises game-over locally on every client.
        [ClientRpc]
        private void GameOverClientRpc(int winnerPlayerNumber)
        {
            NetworkBattleshipEvents.RaiseGameOver(winnerPlayerNumber);
        }

        /// <summary>
        /// Clears pending application events after operations where the event data
        /// is only needed for diagnostics.
        /// </summary>
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

        /// <summary>
        /// Builds ClientRpc parameters that target a single specific client.
        /// </summary>
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