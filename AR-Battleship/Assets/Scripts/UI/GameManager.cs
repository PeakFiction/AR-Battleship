using System;
using System.Collections;
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
			ShotOutcome outcome)
		{
			bool localPlayerFired = shooterPlayerNumber == _localPlayerNumber;

			if (localPlayerFired)
			{
				OnPlayerShotFired?.Invoke(x, y, outcome, null, null, null);
				OnBattleLogEntry?.Invoke($"You fired at ({x},{y}): {outcome}");
			}
			else
			{
				OnEnemyShotFired?.Invoke(x, y, outcome, null, null, null);
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
			if (_useMultiplayer)
			{
				if (_multiplayerSession == null)
				{
					Debug.LogWarning("[GameManager] Multiplayer session is missing.");
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

			if (_game.Phase != GamePhase.Setup)
			{
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
			EnsureMultiplayerSession();
			if (_useMultiplayer)
			{
				if (_multiplayerSession == null)
				{
					Debug.LogWarning("[GameManager] Multiplayer session is missing.");
					return;
				}

				_multiplayerSession.StartGame();
				return;
			}

			if (_game.Phase != GamePhase.Setup)
			{
				return;
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

			StartCoroutine(TakeAITurnAfterDelay(_aiTurnDelay));
			return outcome;
		}

        public GameSnapshot GetSnapshot() => _gameService.GetSnapshot(PlayerId.PlayerOne);

        public void ResetGame()
        {
            StopAllCoroutines();
            InitialiseServices();
        }

        private void InitialiseServices()
        {
            _game = new BattleshipGame(_boardSize);
            _gameService = new BattleshipGameService(_game);
            _enemyPlacementService = new EnemyShipPlacementService();
            _aiStrategy = new HuntTargetStrategy(_game.PlayerOneBoard.Size);
            _enemyTurnService = new EnemyTurnService(_game, _aiStrategy);
        }

        private IEnumerator TakeAITurnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            TakeAITurn();
        }

        private void TakeAITurn()
        {
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

            if (_game.IsGameOver)
                OnGameOver?.Invoke(1);
        }

        private void ProcessEvents()
        {
            foreach (var gameEvent in _gameService.ConsumeEvents())
            {
                switch (gameEvent)
                {
                    case AnnouncementEvent announcement:
                        OnBattleLogEntry?.Invoke(announcement.Message);
                        break;

					case ShotFiredEvent shot:
						if (shot.PlayerId == PlayerId.PlayerOne)
							OnPlayerShotFired?.Invoke(shot.X, shot.Y, shot.Outcome, shot.HitSegmentIndex, shot.ShipOrientation, shot.ShipName);
						else
							OnEnemyShotFired?.Invoke(shot.X, shot.Y, shot.Outcome, shot.HitSegmentIndex, shot.ShipOrientation, shot.ShipName);

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