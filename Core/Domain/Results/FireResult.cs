using System;

namespace ARBattleship.Core.Domain 
{
    public record FireResult
    {
        public Coordinate Coordinate { get; init; }
        public ShotResult Outcome { get; init; }
        public ShipId? ShipId { get; init; }
        
        // This holds the name from Figure 2 (e.g., "Carrier", "Submarine")
        public string? ShipName { get; init; } 

        public bool IsHit => Outcome is ShotResult.Hit or ShotResult.Sunk;
        public bool IsSunk => Outcome == ShotResult.Sunk;

        // Private constructor ensures the "Miss" case doesn't accidentally have a ShipName
        private FireResult(Coordinate coordinate, ShotResult outcome, ShipId? shipId, string? shipName)
        {
            Coordinate = coordinate;
            Outcome = outcome;
            ShipId = shipId;
            ShipName = shipName;
        }

        /// <summary>
        /// Creates a result for a shot that hit water.
        /// </summary>
        public static FireResult Miss(Coordinate coord) =>
            new(coord, ShotResult.Miss, null, null);

        /// <summary>
        /// Creates a result for a shot that hit a specific ship type.
        /// </summary>
        public static FireResult Hit(Coordinate coord, ShipId shipId, string shipName) =>
            new(coord, ShotResult.Hit, shipId, shipName);

        /// <summary>
        /// Creates a result for a shot that finished off a ship.
        /// </summary>
        public static FireResult Sunk(Coordinate coord, ShipId shipId, string shipName) =>
            new(coord, ShotResult.Sunk, shipId, shipName);
    }
}