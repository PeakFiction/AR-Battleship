using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application;
using ARBattleship.Core.Application.Services;
using ARBattleship.Core.Application.Commands;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Application.Events;
using ARBattleship.Core.Application.Snapshots;
using ARBattleship.Multiplayer.Battleship;

namespace ARBattleship.Unity
{
    public class GameManager : MonoBehaviour
    {

        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int _boardSize = 10;
        [SerializeField] private float _aiTurnDelay = 1.0f;

		[Header("Multiplayer")]
		[SerializeField] private bool _useMultiplayer = false;
		[SerializeField] private MultiplayerBattleshipSession _multiplayerSession;

		private int _localPlayerNumber;

		// Public method to set game mode - call this when switching between modes
		public void SetMultiplayerMode(bool enabled)
		{
			Debug.Log($"[GameManager] SetMultiplayerMode: {enabled}");
			_useMultiplayer = enabled;
			if (!enabled)
			{
				_multiplayerSession = null;
			}
		}

        public static event Action OnGameStarted;
		public static event Action<int, int, ShotOutcome, int?, string?, string?> OnPlayerShotFired;
        public static event Action<int, int, ShotOutcome, int?, string?, string?> OnEnemyShotFired;
        public static event Action<int> OnGameOver;
        public static event Action<string> OnBattleLogEntry;
        public static event Action<int, string> OnShipSunk;

		public bool IsMultiplayer => _useMultiplayer;

        public GamePhase CurrentPhase
		{
			get
			{
				if (_useMultiplayer && _multiplayerSession != null)
				{
					return _multiplayerSession.Phase == MultiplayerBattlePhase.Battle
						? GamePhase.InProgress
						: GamePhase.Setup;
				}

				return _game?.Phase ?? GamePhase.Setup;
			}
		}

		public int CurrentPlayerTurn
		{
			get
			{
				if (_useMultiplayer)
				{
					/*
					* Keep returning 0 so BattleshipAR is allowed to submit the shot.
					* The real turn validation happens on the host.
					*/
					return 0;
				}

				return _game?.CurrentTurn == PlayerId.PlayerTwo ? 1 : 0;
			}
		}

        private BattleshipGame _game;
        private BattleshipGameService _gameService;
        private EnemyShipPlacementService _enemyPlacementService;
        private EnemyTurnService _enemyTurnService;
        private HuntTargetStrategy _aiStrategy;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseServices();
        }

		private void OnEnable()
		{
			if (!_useMultiplayer)
			{
				return;
			}

			NetworkBattleshipEvents.LocalPlayerAssigned += HandleLocalPlayerAssigned;
			NetworkBattleshipEvents.BattleStarted += HandleMultiplayerBattleStarted;
			NetworkBattleshipEvents.ShotResolved += HandleMultiplayerShotResolved;
			NetworkBattleshipEvents.ShotRejected += HandleMultiplayerShotRejected;
		}

		private void OnDisable()
		{
			if (!_useMultiplayer)
			{
				return;
			}

			NetworkBattleshipEvents.LocalPlayerAssigned -= HandleLocalPlayerAssigned;
			NetworkBattleshipEvents.BattleStarted -= HandleMultiplayerBattleStarted;
			NetworkBattleshipEvents.ShotResolved -= HandleMultiplayerShotResolved;
			NetworkBattleshipEvents.ShotRejected -= HandleMultiplayerShotRejected;
		}

		private void HandleLocalPlayerAssigned(int playerNumber)
		{
			_localPlayerNumber = playerNumber;
			Debug.Log($"[GameManager Multiplayer] Local player assigned: Player {playerNumber}");
		}

		private void HandleMultiplayerBattleStarted(int startingPlayerNumber)
		{
			OnGameStarted?.Invoke();
			OnBattleLogEntry?.Invoke($"Battle started. Player {startingPlayerNumber} goes first.");
		}

		private void HandleMultiplayerShotResolved(
			int shooterPlayerNumber,
			int x,
			int y,
			ShotOutcome outcome,
			int? hitSegmentIndex,
    		string? shipOrientation,
    		string? shipType)
		{
			bool localPlayerFired = shooterPlayerNumber == _localPlayerNumber;

			if (localPlayerFired)
			{
				OnPlayerShotFired?.Invoke(x, y, outcome, hitSegmentIndex, shipOrientation, shipType);
				OnBattleLogEntry?.Invoke($"You fired at ({x},{y}): {outcome}");
			}
			else
			{
                OnEnemyShotFired?.Invoke(x, y, outcome, hitSegmentIndex, shipOrientation, shipType);
				OnBattleLogEntry?.Invoke($"Opponent fired at ({x},{y}): {outcome}");
			}
		}

		private void HandleMultiplayerShotRejected(
			int shooterPlayerNumber,
			int x,
			int y,
			GameErrorCode errorCode)
		{
			if (shooterPlayerNumber != _localPlayerNumber)
			{
				return;
			}

			OnBattleLogEntry?.Invoke($"Shot rejected at ({x},{y}): {errorCode}");
		}

        public bool PlaceShip(string shipType, Coordinate startCoordinate, Orientation orientation)
		{
			EnsureMultiplayerSession();
			
			// Auto-detect mode mismatch
			bool hasValidMultiplayerSession = _multiplayerSession != null;
			if (_useMultiplayer && !hasValidMultiplayerSession)
			{
				Debug.LogWarning("[GameManager.PlaceShip] Multiplayer flag set but no session - forcing singleplayer");
				_useMultiplayer = false;
			}
			
			if (_useMultiplayer)
			{
				if (_multiplayerSession == null)
				{
					Debug.LogError("[GameManager] Multiplayer session is missing in PlaceShip!");
					return false;
				}

				_multiplayerSession.PlaceShip(
					shipType,
					startCoordinate.X,
					startCoordinate.Y,
					orientation
				);

				return true;
			}

			// Singleplayer mode
			if (_game == null)
			{
				Debug.LogError("[GameManager.PlaceShip] _game is null! Reinitializing.");
				InitialiseServices();
			}

			if (_game.Phase != GamePhase.Setup)
			{
				Debug.LogWarning($"[GameManager.PlaceShip] Game not in Setup phase: {_game.Phase}");
				return false;
			}

			var command = new ShipPlacementCommand(
				PlayerId.PlayerOne,
				shipType,
				startCoordinate,
				orientation
			);

			var result = _gameService.TryPlaceShip(command);
			ProcessEvents();

			return result.IsSuccess;
		}

        public void StartGame()
		{
			Debug.Log($"[GameManager] StartGame called - Multiplayer flag: {_useMultiplayer}");
			EnsureMultiplayerSession();
			
			// Auto-detect mode: if multiplayerSession exists, use multiplayer; otherwise singleplayer
			bool hasMultiplayerSession = _multiplayerSession != null;
			if (_useMultiplayer && !hasMultiplayerSession)
			{
				Debug.LogWarning("[GameManager] Multiplayer flag set but no session found - switching to singleplayer");
				_useMultiplayer = false;
			}
			else if (!_useMultiplayer && hasMultiplayerSession)
			{
				Debug.LogWarning("[GameManager] Multiplayer session exists but flag not set - switching to multiplayer");
				_useMultiplayer = true;
			}
			
			if (_useMultiplayer)
			{
				if (_multiplayerSession == null)
				{
					Debug.LogError("[GameManager] Multiplayer mode but session is missing - cannot start!");
					return;
				}

				Debug.Log("[GameManager] Starting multiplayer game");
				_multiplayerSession.StartGame();
				return;
			}

			// Singleplayer mode
			Debug.Log("[GameManager] Starting singleplayer game");
			if (_game == null)
			{
				Debug.LogError("[GameManager] _game is null! Reinitializing services.");
				InitialiseServices();
			}

			Debug.Log($"[GameManager] Current game phase: {_game.Phase}");
			if (_game.Phase != GamePhase.Setup)
			{
				Debug.LogWarning($"[GameManager] Game is not in Setup phase (current: {_game.Phase}). Reinitializing.");
				InitialiseServices();
			}

			var placementResult = _enemyPlacementService.PlaceAllShips(_game);

			if (!placementResult.IsSuccess)
			{
				Debug.LogError($"[GameManager] AI placement failed: {placementResult.Error}");
				return;
			}

			var startResult = _game.StartGame();

			if (!startResult.IsSuccess)
			{
				Debug.LogError($"[GameManager] StartGame failed: {startResult.Error}");
				return;
			}

			Debug.Log("[GameManager] Singleplayer game started successfully!");
			OnGameStarted?.Invoke();
		}

       public ShotOutcome FireShot(int x, int y)
		{
			EnsureMultiplayerSession();
			if (_useMultiplayer)
			{
				if (_multiplayerSession == null)
				{
					Debug.LogWarning("[GameManager] Multiplayer session is missing.");
					return ShotOutcome.None;
				}

				_multiplayerSession.FireShot(x, y);

				/*
				* In multiplayer, the result is not known immediately.
				* The host will validate the shot and send the result back through
				* NetworkBattleshipEvents.ShotResolved.
				*/
				return ShotOutcome.None;
			}

			if (_game.Phase != GamePhase.InProgress)
			{
				return ShotOutcome.None;
			}

			if (_game.CurrentTurn != PlayerId.PlayerOne)
			{
				return ShotOutcome.None;
			}

			var command = new FireShotCommand(
				PlayerId.PlayerOne,
				new Coordinate(x, y)
			);

			var result = _gameService.TryFireShot(command);
			ProcessEvents();

			if (!result.IsSuccess)
			{
				return ShotOutcome.None;
			}

			var outcome = result.Value;

			if (_game.IsGameOver)
			{
				OnGameOver?.Invoke(0);
				return outcome;
			}

			Debug.Log($"[GameManager] GameObject active: {gameObject.activeInHierarchy}, enabled: {enabled}");
			StartCoroutine(TakeAITurnAfterDelay(_aiTurnDelay));
			return outcome;
		}

        public GameSnapshot GetSnapshot() => _gameService.GetSnapshot(PlayerId.PlayerOne);

        public void ResetGame()
        {
            Debug.Log("[GameManager] ResetGame called");
            StopAllCoroutines();
            
            // Auto-detect mode: check if we have a multiplayer session
            EnsureMultiplayerSession();
            bool hasMultiplayerSession = _multiplayerSession != null;
            
            Debug.Log($"[GameManager] ResetGame - _useMultiplayer: {_useMultiplayer}, hasSession: {hasMultiplayerSession}");
            
            if (_useMultiplayer && !hasMultiplayerSession)
            {
                Debug.LogWarning("[GameManager] Multiplayer flag set but no session - forcing singleplayer mode");
                _useMultiplayer = false;
            }
            
            // Always clear session for singleplayer
            if (!_useMultiplayer)
            {
                if (_multiplayerSession != null)
                {
                    Debug.Log("[GameManager] Clearing multiplayer session for singleplayer mode");
                }
                _multiplayerSession = null;
                InitialiseServices();
            }
        }

        private void InitialiseServices()
        {
            Debug.Log($"[GameManager] InitialiseServices - Multiplayer: {_useMultiplayer}");
            _game = new BattleshipGame(_boardSize);
            _gameService = new BattleshipGameService(_game);
            _enemyPlacementService = new EnemyShipPlacementService();
            _aiStrategy = new HuntTargetStrategy(_game.PlayerOneBoard.Size);
            _enemyTurnService = new EnemyTurnService(_game, _aiStrategy);
        }

        private IEnumerator TakeAITurnAfterDelay(float delay)
        {
            Debug.Log($"[GameManager] AI turn coroutine started, waiting {delay}s");
            yield return new WaitForSeconds(delay);
            Debug.Log($"[GameManager] AI turn coroutine delay complete, calling TakeAITurn");
            TakeAITurn();
        }

        private void TakeAITurn()
        {
            Debug.Log($"[GameManager] TakeAITurn called. Phase: {_game.Phase}");
            if (_game.Phase != GamePhase.InProgress) return;

            var result = _enemyTurnService.TakeTurn();
            ProcessEvents();

            if (!result.IsSuccess)
            {
                Debug.LogError($"[GameManager] AI turn failed: {result.Error}");
                return;
            }

            var fireResult = result.Value!;
            var outcome = MapOutcome(fireResult.Outcome);
            Debug.Log($"[GameManager] AI turn completed with outcome: {outcome}");
            
            // WORKAROUND: EnemyTurnService.TakeTurn() doesn't generate events
            // Manually fire OnEnemyShotFired with the result
            Debug.Log($"[GameManager] Manually firing OnEnemyShotFired: ({fireResult.Coordinate.X},{fireResult.Coordinate.Y}) -> {outcome}");
            OnEnemyShotFired?.Invoke(
                fireResult.Coordinate.X, 
                fireResult.Coordinate.Y, 
                outcome,
                null,  // No segment index for enemy shots
                null,  // No orientation
                null   // No ship type
            );

            if (_game.IsGameOver)
                OnGameOver?.Invoke(1);
        }

        private void ProcessEvents()
        {
            var events = _gameService.ConsumeEvents().ToList();
            Debug.Log($"[GameManager] ProcessEvents found {events.Count} events");
            
            foreach (var gameEvent in events)
            {
                Debug.Log($"[GameManager] Processing event: {gameEvent.GetType().Name}");
                switch (gameEvent)
                {
                    case AnnouncementEvent announcement:
                        OnBattleLogEntry?.Invoke(announcement.Message);
                        break;

                    case ShotFiredEvent shot:
						if (shot.PlayerId == PlayerId.PlayerOne)
						{
							Debug.Log($"[GameManager] Firing OnPlayerShotFired event: ({shot.X},{shot.Y}) -> {shot.Outcome}");
							OnPlayerShotFired?.Invoke(shot.X, shot.Y, shot.Outcome, shot.HitSegmentIndex, shot.ShipOrientation, shot.ShipType);
						}
						else
						{
							Debug.Log($"[GameManager] Firing OnEnemyShotFired event: ({shot.X},{shot.Y}) -> {shot.Outcome}");
							OnEnemyShotFired?.Invoke(shot.X, shot.Y, shot.Outcome, shot.HitSegmentIndex, shot.ShipOrientation, shot.ShipType);
						}
                        var entry = shot.Outcome switch
                        {
                            ShotOutcome.Hit  => shot.PlayerId == PlayerId.PlayerOne
                                ? $"You hit at ({shot.X},{shot.Y})!"
                                : $"Enemy hit at ({shot.X},{shot.Y})!",
                            ShotOutcome.Sunk => shot.PlayerId == PlayerId.PlayerOne
                                ? "You sunk an enemy ship!"
                                : "Enemy sunk your ship!",
                            _ => shot.PlayerId == PlayerId.PlayerOne
                                ? $"You missed at ({shot.X},{shot.Y})."
                                : $"Enemy missed at ({shot.X},{shot.Y})."
                        };
                        OnBattleLogEntry?.Invoke(entry);
                        break;

                    case ShipSunkEvent sunk:
                        OnShipSunk?.Invoke(sunk.PlayerId == PlayerId.PlayerOne ? 0 : 1, sunk.ShipType);
                        break;

                    case GameEndedEvent ended:
                        OnGameOver?.Invoke(ended.Winner == PlayerId.PlayerOne ? 0 : 1);
                        break;
                }
            }
        }

        private static ShotOutcome MapOutcome(ShotResult domainResult) => domainResult switch
        {
            ShotResult.Hit  => ShotOutcome.Hit,
            ShotResult.Sunk => ShotOutcome.Sunk,
            _               => ShotOutcome.Miss
        };

		private void EnsureMultiplayerSession()
		{
			if (_useMultiplayer && _multiplayerSession == null)
				_multiplayerSession = FindObjectOfType<MultiplayerBattleshipSession>();
		}
    }
}