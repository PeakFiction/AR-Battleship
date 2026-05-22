using System;
using System.Collections.Generic;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// A Battleship ship entity.  Maintains its identity, occupied positions,
    /// received hits, and orientation.  Created only via the factory methods.
    /// </summary>
    public class Ship
    {
        /// <summary>Set of all cells this ship occupies (for O(1) hit checks).</summary>
        private readonly HashSet<Coordinate> _positions;

        /// <summary>Set of cells that have been struck (for sunk detection).</summary>
        private readonly HashSet<Coordinate> _hits;

        /// <summary>
        /// Ordered list of cells for segment index lookup.
        /// Preserves insertion order; used by GetSegmentIndex.
        /// </summary>
        private readonly List<Coordinate> _orderedPositions;

        /// <summary>Unique identifier for this ship instance.</summary>
        public ShipId Id { get; }

        /// <summary>Type name string (e.g. "Carrier").  Matches the factory method's name.</summary>
        public string ShipType { get; private init; }

        /// <summary>Total number of cells this ship occupies.</summary>
        public int Size { get; private init; }

        /// <summary>Whether the ship is placed horizontally or vertically.</summary>
        public Orientation Orientation { get; private init; }

        /// <summary>All cell positions the ship occupies.</summary>
        public IEnumerable<Coordinate> Positions => _positions;

        /// <summary>True when every position has been hit.</summary>
        public bool IsSunk => _hits.Count == _positions.Count;

        private Ship(ShipId id, string shipType, int size, IEnumerable<Coordinate> positions, Orientation orientation)
        {
            Id          = id;
            ShipType    = shipType;
            Size        = size;
            Orientation = orientation;
            _positions  = new HashSet<Coordinate>(positions);
            _hits       = new HashSet<Coordinate>();
            _orderedPositions = new List<Coordinate>(positions);

            if (_positions.Count != size)
                throw new ArgumentException($"Position count must match ship size ({size})");
        }

        #region Factory Methods

        /// <summary>Creates a Carrier (5 cells).</summary>
        public static Ship CreateCarrier(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation = Orientation.Horizontal)
            => new(id, "Carrier", 5, positions, orientation);

        /// <summary>Creates a Battleship (4 cells).</summary>
        public static Ship CreateBattleship(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation = Orientation.Horizontal)
            => new(id, "Battleship", 4, positions, orientation);

        /// <summary>Creates a Cruiser (3 cells).</summary>
        public static Ship CreateCruiser(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation = Orientation.Horizontal)
            => new(id, "Cruiser", 3, positions, orientation);

        /// <summary>Creates a Submarine (3 cells).</summary>
        public static Ship CreateSubmarine(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation = Orientation.Horizontal)
            => new(id, "Submarine", 3, positions, orientation);

        /// <summary>Creates a Destroyer (2 cells).</summary>
        public static Ship CreateDestroyer(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation = Orientation.Horizontal)
            => new(id, "Destroyer", 2, positions, orientation);

        /// <summary>
        /// Returns the canonical cell count for a given ship type string.
        /// Centralises ship-size knowledge so the application layer does not need it.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown for unknown type strings.</exception>
        public static int GetSize(string shipType) => shipType switch
        {
            "Carrier"    => 5,
            "Battleship" => 4,
            "Cruiser"    => 3,
            "Submarine"  => 3,
            "Destroyer"  => 2,
            _ => throw new ArgumentException($"Unknown ship type: {shipType}")
        };

        /// <summary>
        /// Creates the correct Ship subtype from a string name and pre-calculated positions.
        /// Used by BattleshipGameService to avoid switch statements in the application layer.
        /// </summary>
        public static Ship CreateFromType(string shipType, ShipId id, IEnumerable<Coordinate> positions, Orientation orientation = Orientation.Horizontal)
            => shipType switch
            {
                "Carrier"    => CreateCarrier(id, positions, orientation),
                "Battleship" => CreateBattleship(id, positions, orientation),
                "Cruiser"    => CreateCruiser(id, positions, orientation),
                "Submarine"  => CreateSubmarine(id, positions, orientation),
                "Destroyer"  => CreateDestroyer(id, positions, orientation),
                _ => throw new ArgumentException($"Unknown ship type: {shipType}")
            };

        #endregion

        /// <summary>
        /// Records a hit at coordinate.
        /// Returns false if the coordinate is not part of this ship or was already hit
        /// (prevents double-counting on repeated shots to the same cell).
        /// </summary>
        public bool RegisterHit(Coordinate coordinate)
        {
            if (!_positions.Contains(coordinate) || _hits.Contains(coordinate))
                return false;

            _hits.Add(coordinate);
            return true;
        }

        /// <summary>
        /// Used by AR effects to align hit markers.
        /// </summary>
        public int? GetSegmentIndex(Coordinate coordinate)
        {
            var index = _orderedPositions.IndexOf(coordinate);
            return index >= 0 ? index : null;
        }
    }
}
