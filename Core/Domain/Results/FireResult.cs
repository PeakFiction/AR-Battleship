using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Immutable shot outcome record returned by Board.FireAt.
    /// Created only via the three factory methods: Miss, Hit, and Sunk.
    /// </summary>
    public record FireResult
    {
        /// <summary>The cell that was targeted.</summary>
        public Coordinate Coordinate { get; init; }

        /// <summary>Whether the shot was a Miss, Hit, or Sunk.</summary>
        public ShotResult Outcome { get; init; }

        /// <summary>
        /// Unique ID of the ship that was hit or sunk.
        /// Null for a Miss — there is no ship to identify.
        /// </summary>
        public ShipId? ShipId { get; init; }

        /// <summary>
        /// Human-readable ship type (e.g. "Carrier", "Destroyer").
        /// Null for a Miss.  Used in sunk announcements and AR visuals.
        /// </summary>
        public string? ShipType { get; init; }

        /// <summary>
        /// Zero-based index of the ship segment that was struck (0 = bow).
        /// Null for a Miss.  Used by AR effects to position hit markers along the hull.
        /// </summary>
        public int? HitSegmentIndex { get; init; }

        /// <summary>
        /// "Horizontal" or "Vertical", the orientation string of the struck ship.
        /// </summary>
        public string? ShipOrientation { get; init; }

        /// <summary>True for Hit and Sunk outcomes; false for Miss.</summary>
        public bool IsHit => Outcome is ShotResult.Hit or ShotResult.Sunk;

        /// <summary>True only when a ship was fully sunk by this shot.</summary>
        public bool IsSunk => Outcome == ShotResult.Sunk;

        private FireResult(
            Coordinate coordinate,
            ShotResult outcome,
            ShipId? shipId,
            string? shipType,
            int? hitSegmentIndex,
            string? shipOrientation)
        {
            Coordinate     = coordinate;
            Outcome        = outcome;
            ShipId         = shipId;
            ShipType       = shipType;
            HitSegmentIndex = hitSegmentIndex;
            ShipOrientation = shipOrientation;
        }

        /// <summary>
        /// Creates a result for a shot that hit open water.
        /// All ship-related fields are null.
        /// </summary>
        public static FireResult Miss(Coordinate coord) =>
            new(coord, ShotResult.Miss, null, null, null, null);

        /// <summary>
        /// Creates a result for a shot that struck a ship that is not yet sunk.
        /// </summary>
        public static FireResult Hit(
            Coordinate coord,
            ShipId shipId,
            string shipType,
            int? segmentIndex,
            string? orientation) =>
            new(coord, ShotResult.Hit, shipId, shipType, segmentIndex, orientation);

        /// <summary>
        /// Creates a result for a shot that sank a ship entirely.
        /// </summary>
        public static FireResult Sunk(
            Coordinate coord,
            ShipId shipId,
            string shipType,
            int? segmentIndex,
            string? orientation) =>
            new(coord, ShotResult.Sunk, shipId, shipType, segmentIndex, orientation);
    }
}
