// =============================================================================
// CellView.cs  |  ARBattleship.Core.Application.Snapshots
// =============================================================================
// Read-only projection of a single cell for UI rendering.
// BattleshipGameService builds a CellView per cell based on the viewer's
// perspective (owner sees ships; opponent sees only shots).
// =============================================================================

namespace ARBattleship.Core.Application.Snapshots
{
    /// <summary>
    /// Immutable view of one cell on a board, projected from a specific player's
    /// perspective.  Passed to the minimap and 2D enemy-grid renderers.
    /// </summary>
    public record CellView
    {
        /// <summary>Column index (0-based).</summary>
        public int X { get; init; }

        /// <summary>Row index (0-based).</summary>
        public int Y { get; init; }

        /// <summary>
        /// What the viewing player can see in this cell.
        /// Unknown for unshot opponent cells; Ship only for the board owner.
        /// </summary>
        public Enums.CellViewState State { get; init; }

        /// <summary>
        /// Ship type occupying the cell, or null if empty or hidden.
        /// Visible to the board owner for all own cells.
        /// Visible to the opponent only after a ship segment is hit or the ship sinks.
        /// </summary>
        public string? ShipType { get; init; }

        /// <summary>True when a ship type is known for this cell.</summary>
        public bool IsOccupied => ShipType != null;

        /// <summary>True when the cell is part of a fully sunk ship.</summary>
        public bool IsSunk => State == Enums.CellViewState.Sunk;
    }
}
