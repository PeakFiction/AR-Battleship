using System;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Setup,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GamePhase CurrentPhase { get; private set; }
    public int CurrentPlayerTurn { get; private set; }

    private BoardState[] boards = new BoardState[2];
    private List<Ship>[] ships = new List<Ship>[2];

    public static event Action OnGameStarted;
    public static event Action<int, int, ShotResult> OnPlayerShotFired;
    public static event Action<int, int, ShotResult> OnEnemyShotFired;
    public static event Action<int> OnGameOver;
    public static event Action<string> OnBattleLogEntry;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitGame();
    }

    private void InitGame()
    {
        for (int i = 0; i < 2; i++)
        {
            boards[i] = new BoardState();
            ships[i] = new List<Ship>();
        }

        CurrentPhase = GamePhase.Setup;
        CurrentPlayerTurn = 0;
    }

    public bool PlaceShip(int playerIndex, Ship ship)
    {
        if (CurrentPhase != GamePhase.Setup) return false;
        if (!PlacementValidator.CanPlace(boards[playerIndex], ship)) return false;

        PlacementValidator.Place(boards[playerIndex], ship);
        ships[playerIndex].Add(ship);
        return true;
    }

    public void StartGame()
    {
        if (CurrentPhase != GamePhase.Setup) return;
        CurrentPhase = GamePhase.Playing;
        CurrentPlayerTurn = 0;
        OnGameStarted?.Invoke();
    }

    public ShotResult FireShot(int x, int y)
    {
        if (CurrentPhase != GamePhase.Playing) return ShotResult.AlreadyFired;

        int targetPlayer = 1 - CurrentPlayerTurn;
        ShotResult result = CombatSystem.FireShot(boards[targetPlayer], ships[targetPlayer], x, y);

        if (result == ShotResult.AlreadyFired) return result;

        OnPlayerShotFired?.Invoke(x, y, result);

        string shipName = result == ShotResult.Hit || result == ShotResult.Sunk
            ? CombatSystem.GetShipAtCell(ships[targetPlayer], x, y)?.Type.ToString()
            : null;
        string entry = result == ShotResult.Sunk
            ? $"You sunk enemy's {shipName}!"
            : result == ShotResult.Hit
                ? $"You hit at ({x},{y})!"
                : $"You missed at ({x},{y}).";
        OnBattleLogEntry?.Invoke(entry);

        if (CombatSystem.AllShipsSunk(ships[targetPlayer]))
        {
            CurrentPhase = GamePhase.GameOver;
            OnGameOver?.Invoke(CurrentPlayerTurn);
            return ShotResult.Sunk;
        }

        CurrentPlayerTurn = targetPlayer;
        return result;
    }

    public ShotResult FireEnemyShot(int x, int y)
    {
        if (CurrentPhase != GamePhase.Playing) return ShotResult.AlreadyFired;

        ShotResult result = CombatSystem.FireShot(boards[0], ships[0], x, y);

        if (result == ShotResult.AlreadyFired) return result;

        OnEnemyShotFired?.Invoke(x, y, result);

        string shipName = result == ShotResult.Hit || result == ShotResult.Sunk
            ? CombatSystem.GetShipAtCell(ships[0], x, y)?.Type.ToString()
            : null;
        string entry = result == ShotResult.Sunk
            ? $"Enemy sunk your {shipName}!"
            : result == ShotResult.Hit
                ? $"Enemy hit at ({x},{y})!"
                : $"Enemy missed at ({x},{y}).";
        OnBattleLogEntry?.Invoke(entry);

        if (CombatSystem.AllShipsSunk(ships[0]))
        {
            CurrentPhase = GamePhase.GameOver;
            OnGameOver?.Invoke(1);
            return ShotResult.Sunk;
        }

        CurrentPlayerTurn = 0;
        return result;
    }

    public BoardState GetBoard(int playerIndex) => boards[playerIndex];
    public List<Ship> GetShips(int playerIndex) => ships[playerIndex];
    public int GetWinner() => CurrentPhase == GamePhase.GameOver ? CurrentPlayerTurn : -1;
    public void ResetGame() => InitGame();
}