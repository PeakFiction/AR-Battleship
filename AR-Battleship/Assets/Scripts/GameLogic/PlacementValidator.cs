using System.Collections.Generic;

public class PlacementValidator
{
    public static bool CanPlace(BoardState board, Ship ship)
    {
        List<(int x, int y)> cells = GetOccupiedCells(ship);

        foreach (var (x, y) in cells)
        {
            if (!board.IsInBounds(x, y)) return false;
            if (board.GetCell(x, y) == CellState.Ship) return false;
        }

        return true;
    }

    public static void Place(BoardState board, Ship ship)
    {
        foreach (var (x, y) in GetOccupiedCells(ship))
            board.SetCell(x, y, CellState.Ship);
    }

    public static List<(int x, int y)> GetOccupiedCells(Ship ship)
    {
        var cells = new List<(int, int)>();

        for (int i = 0; i < ship.Size; i++)
        {
            int x = ship.Orientation == Orientation.Horizontal ? ship.OriginX + i : ship.OriginX;
            int y = ship.Orientation == Orientation.Vertical   ? ship.OriginY + i : ship.OriginY;
            cells.Add((x, y));
        }

        return cells;
    }
}