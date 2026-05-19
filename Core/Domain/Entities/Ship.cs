using System;
using System.Collections.Generic;

namespace ARBattleship.Core.Domain
{
    public class Ship
    {
        private readonly HashSet<Coordinate> _positions;
        private readonly HashSet<Coordinate> _hits;
        private readonly List<Coordinate> _orderedPositions;


        public ShipId Id { get; }
        public string ShipType { get; private init; }
        public int Size { get; private init; }
        public Orientation Orientation { get; private init; }

        private Ship(ShipId id, string shipType, int size, IEnumerable<Coordinate> positions, Orientation orientation)
        {
            Id = id;
            ShipType = shipType;
            Size = size;
            Orientation = orientation;
            _positions = new HashSet<Coordinate>(positions);
            _hits = new HashSet<Coordinate>();
            _orderedPositions = new List<Coordinate>(positions);


            if (_positions.Count != size)
                throw new ArgumentException($"Position count must match ship size ({size})");
        }

        #region Factory Methods

        public static Ship CreateCarrier(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation)
            => new Ship(id, "Carrier", 5, positions, orientation);

        public static Ship CreateBattleship(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation)
            => new Ship(id, "Battleship", 4, positions, orientation);

        public static Ship CreateCruiser(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation)
            => new Ship(id, "Cruiser", 3, positions, orientation);

        public static Ship CreateSubmarine(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation)
            => new Ship(id, "Submarine", 3, positions, orientation);

        public static Ship CreateDestroyer(ShipId id, IEnumerable<Coordinate> positions, Orientation orientation)
            => new Ship(id, "Destroyer", 2, positions, orientation);

        /// <summary>
        /// Returns the canonical size for a given ship type string.
        /// Centralises ship size knowledge so the application layer doesn't need it.
        /// </summary>
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
        /// Removes the need for switch statements in the application layer.
        /// </summary>
        public static Ship CreateFromType(string shipType, ShipId id, IEnumerable<Coordinate> positions, Orientation orientation)
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

        public IEnumerable<Coordinate> Positions => _positions;
        public bool IsSunk => _hits.Count == _positions.Count;

        public bool RegisterHit(Coordinate coordinate)
        {
            if (!_positions.Contains(coordinate) || _hits.Contains(coordinate))
                return false;
            _hits.Add(coordinate);
            return true;
        }

        public int? GetSegmentIndex(Coordinate coordinate)
        {
            var index = _orderedPositions.IndexOf(coordinate);
            return index >= 0 ? index : null;
        }
    }
}