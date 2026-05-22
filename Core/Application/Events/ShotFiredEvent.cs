// Raised for every valid shot (player or AI) after TryFireShot succeeds.
// Carries data needed by CombatUI
using System;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application.Enums;

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// Raised after a valid shot is processed.  Provides the full UI payload:
    /// shooter, cell coordinates, outcome, and optional ship metadata.
    /// </summary>
    [Serializable]
    public sealed record ShotFiredEvent(
        /// <summary>The player who fired the shot.</summary>
        PlayerId PlayerId,

        /// <summary>Column index of the targeted cell (0-based).</summary>
        int X,

        /// <summary>Row index of the targeted cell (0-based).</summary>
        int Y,

        /// <summary>Miss, Hit, or Sunk.</summary>
        ShotOutcome Outcome,

        /// <summary>
        /// Zero-based hull segment that was struck, or null for a Miss.
        /// Used by AR effects to position hit markers.
        /// </summary>
        int? HitSegmentIndex,

        /// <summary>
        /// "Horizontal" or "Vertical" orientation string of the struck ship,
        /// or null for a Miss.
        /// </summary>
        string? ShipOrientation,

        /// <summary>
        /// Type name of the struck ship (e.g. "Carrier"), or null for a Miss.
        /// </summary>
        string? ShipType
    ) : IGameEvent;
}
