using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    private List<(int x, int y)> remainingCells = new List<(int, int)>();

    private void Start()
    {
        AutoPlaceShips();
    }

    private void OnEnable()
    {
        GameManager.OnGameStarted += HandleGameStarted;
        GameManager.OnPlayerShotFired += HandlePlayerShotFired;
    }

    private void OnDisable()
    {
        GameManager.OnGameStarted -= HandleGameStarted;
        GameManager.OnPlayerShotFired -= HandlePlayerShotFired;
    }

    private void HandleGameStarted()
    {
        BuildRemainingCells();
    }

    private void AutoPlaceShips()
    {
        ShipType[] types = new ShipType[]
        {
            ShipType.AircraftCarrier,
            ShipType.Battleship,
            ShipType.Cruiser,
            ShipType.Submarine,
            ShipType.Destroyer
        };

        foreach (ShipType type in types)
        {
            bool placed = false;
            int attempts = 0;

            while (!placed && attempts < 100)
            {
                int x = Random.Range(0, BoardState.SIZE);
                int y = Random.Range(0, BoardState.SIZE);
                Orientation orientation = Random.value > 0.5f ? Orientation.Horizontal : Orientation.Vertical;
                Ship ship = new Ship(type, x, y, orientation);
                placed = GameManager.Instance.PlaceShip(1, ship);
                attempts++;
            }
        }
    foreach (Ship s in GameManager.Instance.GetShips(1))
    Debug.Log($"AI placed {s.Type} at ({s.OriginX},{s.OriginY}) {s.Orientation} size {s.Size}");
    }

    private void BuildRemainingCells()
    {
        remainingCells.Clear();
        for (int x = 0; x < BoardState.SIZE; x++)
            for (int y = 0; y < BoardState.SIZE; y++)
                remainingCells.Add((x, y));
    }

    private void HandlePlayerShotFired(int x, int y, ShotResult result)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Playing) return;
        StartCoroutine(FireWithDelay());
    }

    private IEnumerator FireWithDelay()
    {
        yield return new WaitForSeconds(1f);

        if (remainingCells.Count == 0) yield break;

        int index = Random.Range(0, remainingCells.Count);
        var (ax, ay) = remainingCells[index];
        remainingCells.RemoveAt(index);

        ShotResult result = GameManager.Instance.FireEnemyShot(ax, ay);
        Debug.Log($"AI fired at ({ax},{ay}): {result}");

        if (result == ShotResult.AlreadyFired)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Playing)
                StartCoroutine(FireWithDelay());
        }
    }
}