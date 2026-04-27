namespace ARBattleship.Core.Application.Snapshots {
    public record CellView
    {
        public int X { get; init; }
        public int Y { get; init; }
        
        // The "State" likely covers: Hidden, Miss, Hit, Sunk
        public Enums.CellViewState State { get; init; }
        
        // Instead of a raw string, you might want the ShipId or Type
        public string? ShipType { get; init; }
        
        public bool IsOccupied => ShipType != null;

        // Helper to let the UI know if this cell is part of a destroyed ship
        public bool IsSunk => State == Enums.CellViewState.Sunk;
    }
}