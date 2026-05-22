// =============================================================================
// ShipView.cs  |  ARBattleship.Core.Application.Snapshots
// =============================================================================
// Read-only projection of a ship's metadata for UI rendering.
// Included in PlayerSnapshot; used by ShipStatusUI to display fleet state.
// =============================================================================
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Snapshots
{
    /// <summary>
    /// Immutable view of a ship's metadata.  Enemy ships are only included in
    /// PlayerSnapshot when they have been sunk (avoids revealing hidden ships).
    /// </summary>
    [Serializable]
    public sealed record ShipView
    {
        /// <summary>Type name (e.g. "Carrier", "Destroyer").</summary>
        public string ShipType { get; init; }

        /// <summary>Total number of cells this ship occupies.</summary>
        public int Length { get; init; }

        /// <summary>True when all cells of this ship have been hit.</summary>
        public bool IsSunk { get; init; }

        /// <summary>Horizontal or Vertical placement orientation.</summary>
        public Orientation Orientation { get; init; }

        /// <summary>Creates a ShipView with the given metadata.</summary>
        public ShipView(string shipType, int length, bool isSunk, Orientation orientation)
        {
            ShipType    = shipType;
            Length      = length;
            IsSunk      = isSunk;
            Orientation = orientation;
        }
    }
}
