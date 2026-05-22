// View-projection of a single cell's visible state as seen by a specific player.
// Used in CellView (Snapshots) to drive board rendering without exposing raw
// domain Cell objects to the UI.

namespace ARBattleship.Core.Application.Enums
{
    /// <summary>
    /// Describes what a cell looks like from one player's perspective.
    /// Produced by <c>BattleshipGameService.ProjectCellState</c>.
    /// </summary>
    public enum CellViewState
    {
        /// <summary>The cell is on the opponent's board and has not been shot — contents unknown.</summary>
        Unknown,

        /// <summary>The cell has been revealed (owner view) and contains no ship.</summary>
        Empty,

        /// <summary>The cell contains a ship (owner view only — never revealed to the opponent).</summary>
        Ship,

        /// <summary>The cell was shot and hit a ship that is not yet sunk.</summary>
        Hit,

        /// <summary>The cell was shot and hit open water.</summary>
        Miss,

        /// <summary>The cell was shot and the ship it belonged to has since been fully sunk.</summary>
        Sunk
    }
}
