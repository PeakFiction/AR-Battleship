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

namespace ARBattleship.Unity
{
    public class GameManager : MonoBehaviour
    {

        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int _boardSize = 10;
        [SerializeField] private float _aiTurnDelay = 1.0f;

        public static event Action OnGameStarted;
        public static event Action<int, int, ShotOutcome> OnPlayerShotFired;
        public static event Action<int, int, ShotOutcome> OnEnemyShotFired;
        public static event Action<int> OnGameOver;
        public static event Action<string> OnBattleLogEntry;
        public static event Action<int, string> OnShipSunk;

        public GamePhase CurrentPhase => _game?.Phase ?? GamePhase.Setup;

        public int CurrentPlayerTurn => _game?.CurrentTurn == PlayerId.PlayerTwo ? 1 : 0;

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

        public bool PlaceShip(string shipType, Coordinate startCoordinate, Orientation orientation)
        {
            if (_game.Phase != GamePhase.Setup) return false;
            var command = new ShipPlacementCommand(PlayerId.PlayerOne, shipType, startCoordinate, orientation);
            var result = _gameService.TryPlaceShip(command);
            ProcessEvents();
            return result.IsSuccess;
        }

        public void StartGame()
        {
            if (_game.Phase != GamePhase.Setup) return;

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
            if (_game.Phase != GamePhase.InProgress) return ShotOutcome.None;
            if (_game.CurrentTurn != PlayerId.PlayerOne) return ShotOutcome.None;

            var command = new FireShotCommand(PlayerId.PlayerOne, new Coordinate(x, y));
            var result = _gameService.TryFireShot(command);
            ProcessEvents();

            if (!result.IsSuccess) return ShotOutcome.None;

            var outcome = result.Value;
            OnPlayerShotFired?.Invoke(x, y, outcome);

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
            _aiStrategy = new HuntTargetStrategy();
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
            OnEnemyShotFired?.Invoke(fireResult.Coordinate.X, fireResult.Coordinate.Y, outcome);

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
    }
}