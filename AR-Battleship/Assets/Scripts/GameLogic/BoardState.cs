public enum CellState
{
    Empty,
    Ship,
    Miss,
    Hit
}

public class BoardState
{
    public const int SIZE = 10;

    private CellState[,] grid = new CellState[SIZE, SIZE];

    public CellState GetCell(int x, int y) => grid[x, y];

    public void SetCell(int x, int y, CellState state) => grid[x, y] = state;

    public bool IsInBounds(int x, int y) => x >= 0 && x < SIZE && y >= 0 && y < SIZE;

    public void Reset()
    {
        for (int x = 0; x < SIZE; x++)
            for (int y = 0; y < SIZE; y++)
                grid[x, y] = CellState.Empty;
    }
}