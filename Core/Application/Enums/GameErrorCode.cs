// Granular error code enum used in GameResult<T> to describe why an application
// service call failed.  Grouped by category for clarity:

namespace ARBattleship.Core.Application.Enums
{
    /// <summary>
    /// Typed error code for application-layer service failures.
    /// Returned inside <see cref="Common.GameResult{T}"/> and surfaced to the
    /// multiplayer network layer for client-side rejection messages.
    /// </summary>
    public enum GameErrorCode
    {
        /// <summary>No error; operation succeeded.</summary>
        None = 0,

        /// <summary>An unexpected error that does not fit any specific category.</summary>
        Unknown = 1,

        // ── Player / turn ──────────────────────────────────────────────────

        /// <summary>The supplied PlayerId does not correspond to a valid player in this game.</summary>
        InvalidPlayer   = 10,

        /// <summary>The player attempted an action during the opponent's turn.</summary>
        NotPlayersTurn  = 11,

        // ── Game lifecycle ─────────────────────────────────────────────────

        /// <summary>A shot was attempted before StartGame() was called.</summary>
        GameNotStarted       = 20,

        /// <summary>StartGame() was called while the game was already InProgress.</summary>
        GameAlreadyStarted   = 21,

        /// <summary>An action was attempted after the game reached the Finished phase.</summary>
        GameAlreadyFinished  = 22,

        // ── Coordinate / cell ──────────────────────────────────────────────

        /// <summary>The coordinate falls outside the board boundaries.</summary>
        InvalidCoordinate  = 30,

        /// <summary>The targeted cell has already been shot in a previous turn.</summary>
        CellAlreadyTargeted = 31,

        // ── Ship placement ─────────────────────────────────────────────────

        /// <summary>The ship placement violates at least one placement rule.</summary>
        InvalidShipPlacement = 40,

        /// <summary>One or more ship cells extend outside the board boundary.</summary>
        ShipOutOfBounds = 41,

        /// <summary>The ship would overlap a ship already placed on the board.</summary>
        ShipOverlap = 42
    }
}
