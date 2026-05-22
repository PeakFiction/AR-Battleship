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
    /// <summary>
    /// Central game controller that manages both singleplayer and multiplayer Battleship sessions.
    /// Handles game initialization, turn management, ship placement, and shot firing.
    /// Acts as the bridge between Unity UI and the core game logic.
    /// </summary>
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

		/// <summary>
		/// Explicitly sets the game mode. Call this when switching between singleplayer and multiplayer.
		/// Clears multiplayer session when switching to singleplayer.
		/// </summary>
		public void SetMultiplayerMode(bool enabled)
		{
			Debug.Log($"[GameManager] SetMultiplayerMode: {enabled}");
			_useMultiplayer = enabled;
			if (!enabled)
			{
				_multiplayerSession = null;
			}
		}

        // Events fired to notify UI components of game state changes
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
					// In multiplayer, always return 0 (local player) to allow shot submission
					// The host validates the actual turn - this just allows UI interaction
					return 0;
				}

				return _game?.CurrentTurn == PlayerId.PlayerTwo ? 1 : 0;
			}
		}

        // Singleplayer game services
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

		// Subscribe to multiplayer network events when enabled
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

		// Unsubscribe from multiplayer events when disabled
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

		// Store which player number we are in multiplayer (0 or 1)
		private void HandleLocalPlayerAssigned(int playerNumber)
		{
			_localPlayerNumber = playerNumber;
			Debug.Log($"[GameManager Multiplayer] Local player assigned: Player {playerNumber}");
		}

		// Multiplayer battle has started - notify UI
		private void HandleMultiplayerBattleStarted(int startingPlayerNumber)
		{
			OnGameStarted?.Invoke();
			OnBattleLogEntry?.Invoke($"Battle started. Player {startingPlayerNumber} goes first.");
		}

		// Handle shot outcomes from the network in multiplayer
		// Determines if the local or remote player fired and notifies UI accordingly
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

		// Handle rejected shots in multiplayer (out of turn, invalid target, etc.)
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

        /// <summary>
        /// Places a ship on the board during setup phase.
        /// Handles both singleplayer (local game) and multiplayer (network) placement.
        /// Auto-detects and corrects mode mismatches to prevent errors after switching modes.
        /// </summary>
        public bool PlaceShip(string shipType, Coordinate startCoordinate, Orientation orientation)
		{
			EnsureMultiplayerSession();
			
			// Auto-detect mode mismatch - prevents errors when switching from multiplayer to singleplayer
			// If multiplayer flag is set but no session exists, force singleplayer mode
			bool hasValidMultiplayerSession = _multiplayerSession != null;
			if (_useMultiplayer && !hasValidMultiplayerSession)
			{
				Debug.LogWarning("[GameManager.PlaceShip] Multiplayer flag set but no session - forcing singleplayer");
				_useMultiplayer = false;
			}
			
			// Multiplayer path - delegate to network session
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

			// Singleplayer path - use local game instance
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
			ProcessEvents(); // Generate UI events from placement result

			return result.IsSuccess;
		}

        /// <summary>
        /// Starts the game after all ships have been placed.
        /// In singleplayer: places AI ships and begins battle.
        /// In multiplayer: notifies the network session to start.
        /// Includes defensive mode detection to recover from invalid states.
        /// </summary>
        public void StartGame()
		{
			Debug.Log($"[GameManager] StartGame called - Multiplayer flag: {_useMultiplayer}");
			EnsureMultiplayerSession();
			
			// Auto-detect mode: match flag to actual session availability
			// This prevents errors when transitioning between multiplayer and singleplayer
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
			
			// Multiplayer path
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

			// Singleplayer path
			Debug.Log("[GameManager] Starting singleplayer game");
			if (_game == null)
			{
				Debug.LogError("[GameManager] _game is null! Reinitializing services.");
				InitialiseServices();
			}

			// Defensive check - if not in Setup phase, reinitialize to recover
			Debug.Log($"[GameManager] Current game phase: {_game.Phase}");
			if (_game.Phase != GamePhase.Setup)
			{
				Debug.LogWarning($"[GameManager] Game is not in Setup phase (current: {_game.Phase}). Reinitializing.");
				InitialiseServices();
			}

			// Place AI ships
			var placementResult = _enemyPlacementService.PlaceAllShips(_game);

			if (!placementResult.IsSuccess)
			{
				Debug.LogError($"[GameManager] AI placement failed: {placementResult.Error}");
				return;
			}

			// Transition to battle phase
			var startResult = _game.StartGame();

			if (!startResult.IsSuccess)
			{
				Debug.LogError($"[GameManager] StartGame failed: {startResult.Error}");
				return;
			}

			Debug.Log("[GameManager] Singleplayer game started successfully!");
			OnGameStarted?.Invoke();
		}

       /// <summary>
       /// Fires a shot at the specified coordinates.
       /// In singleplayer: executes immediately, then triggers AI turn after delay.
       /// In multiplayer: sends shot to network, waits for host validation.
       /// </summary>
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

				// In multiplayer, submit shot to network - result comes back via event
				_multiplayerSession.FireShot(x, y);
				return ShotOutcome.None; // Actual outcome delivered through NetworkBattleshipEvents.ShotResolved
			}

			// Singleplayer validation
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
			ProcessEvents(); // Fire UI events for the shot result

			if (!result.IsSuccess)
			{
				return ShotOutcome.None;
			}

			var outcome = result.Value;

			if (_game.IsGameOver)
			{
				OnGameOver?.Invoke(0); // Player wins
				return outcome;
			}

			// Queue AI turn after delay to give player time to see their shot result
			Debug.Log($"[GameManager] GameObject active: {gameObject.activeInHierarchy}, enabled: {enabled}");
			StartCoroutine(TakeAITurnAfterDelay(_aiTurnDelay));
			return outcome;
		}

        public GameSnapshot GetSnapshot() => _gameService.GetSnapshot(PlayerId.PlayerOne);

        /// <summary>
        /// Resets the game to initial state for a new match.
        /// Clears multiplayer session if switching to singleplayer.
        /// Auto-detects mode mismatches and forces correct mode.
        /// Only reinitializes singleplayer services if not in multiplayer.
        /// </summary>
        public void ResetGame()
        {
            Debug.Log("[GameManager] ResetGame called");
            StopAllCoroutines();
            
            // Check for multiplayer session to detect mode
            EnsureMultiplayerSession();
            bool hasMultiplayerSession = _multiplayerSession != null;
            
            Debug.Log($"[GameManager] ResetGame - _useMultiplayer: {_useMultiplayer}, hasSession: {hasMultiplayerSession}");
            
            // If flag says multiplayer but no session exists, force singleplayer
            if (_useMultiplayer && !hasMultiplayerSession)
            {
                Debug.LogWarning("[GameManager] Multiplayer flag set but no session - forcing singleplayer mode");
                _useMultiplayer = false;
            }
            
            // Clear session when in singleplayer mode
            if (!_useMultiplayer)
            {
                if (_multiplayerSession != null)
                {
                    Debug.Log("[GameManager] Clearing multiplayer session for singleplayer mode");
                }
                _multiplayerSession = null;
                InitialiseServices(); // Create fresh singleplayer game
            }
        }

        /// <summary>
        /// Initializes all singleplayer game services with a fresh game state.
        /// </summary>
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

        /// <summary>
        /// Executes the AI's turn in singleplayer.
        /// WORKAROUND: EnemyTurnService doesn't generate events, so we manually fire OnEnemyShotFired.
        /// </summary>
        private void TakeAITurn()
        {
            Debug.Log($"[GameManager] TakeAITurn called. Phase: {_game.Phase}");
            if (_game.Phase != GamePhase.InProgress) return;

            var result = _enemyTurnService.TakeTurn();
            ProcessEvents(); // Check for any events (will be 0 due to service bug)

            if (!result.IsSuccess)
            {
                Debug.LogError($"[GameManager] AI turn failed: {result.Error}");
                return;
            }

            var fireResult = result.Value!;
            var outcome = MapOutcome(fireResult.Outcome);
            Debug.Log($"[GameManager] AI turn completed with outcome: {outcome}");
            
            // WORKAROUND: EnemyTurnService.TakeTurn() executes internally but doesn't generate ShotFiredEvent
            // Manually fire the event so UI components (AR board, minimap) can respond
            Debug.Log($"[GameManager] Manually firing OnEnemyShotFired: ({fireResult.Coordinate.X},{fireResult.Coordinate.Y}) -> {outcome}");
            OnEnemyShotFired?.Invoke(
                fireResult.Coordinate.X, 
                fireResult.Coordinate.Y, 
                outcome,
                null,  // Enemy shots don't include hit segment index
                null,  // Enemy shots don't include ship orientation
                null   // Enemy shots don't include ship type
            );

            if (_game.IsGameOver)
                OnGameOver?.Invoke(1); // AI wins
        }

        /// <summary>
        /// Consumes game events from the core game service and converts them to Unity events.
        /// This is how game logic updates (shots, sunk ships, game over) notify the UI layer.
        /// </summary>
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
                        // Determine if player or enemy fired, then notify appropriate listeners
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

        // Convert domain ShotResult enum to Unity ShotOutcome enum
        private static ShotOutcome MapOutcome(ShotResult domainResult) => domainResult switch
        {
            ShotResult.Hit  => ShotOutcome.Hit,
            ShotResult.Sunk => ShotOutcome.Sunk,
            _               => ShotOutcome.Miss
        };

		// Attempts to find multiplayer session if in multiplayer mode
		private void EnsureMultiplayerSession()
		{
			if (_useMultiplayer && _multiplayerSession == null)
				_multiplayerSession = FindObjectOfType<MultiplayerBattleshipSession>();
		}
    }
}