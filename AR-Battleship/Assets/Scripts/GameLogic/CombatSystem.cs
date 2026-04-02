using System.Collections.Generic;

public enum ShotResult
{
    Miss,
    Hit,
    Sunk,
    AlreadyFired
}

public class CombatSystem
{
    public static ShotResult FireShot(BoardState board, List<Ship> ships, int x, int y)
    {
        CellState current = board.GetCell(x, y);

        if (current == CellState.Hit || current == CellState.Miss)
            return ShotResult.AlreadyFired;

        if (current == CellState.Ship)
        {
            board.SetCell(x, y, CellState.Hit);

            Ship hitShip = GetShipAtCell(ships, x, y);
            hitShip.RegisterHit();

            return hitShip.IsSunk ? ShotResult.Sunk : ShotResult.Hit;
        }

        board.SetCell(x, y, CellState.Miss);
        return ShotResult.Miss;
    }

    public static Ship GetShipAtCell(List<Ship> ships, int x, int y)
    {
        foreach (Ship ship in ships)
        {
            foreach (var (cx, cy) in PlacementValidator.GetOccupiedCells(ship))
            {
                if (cx == x && cy == y) return ship;
            }
        }

        return null;
    }

    public static bool AllShipsSunk(List<Ship> ships)
    {
        foreach (Ship ship in ships)
            if (!ship.IsSunk) return false;

        return true;
    }
}