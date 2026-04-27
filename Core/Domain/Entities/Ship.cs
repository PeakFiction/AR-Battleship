using System;
using System.Collections.Generic;

namespace ARBattleship.Core.Domain
{
    public class Ship
    {
        private readonly HashSet<Coordinate> _positions;
        private readonly HashSet<Coordinate> _hits;

        public ShipId Id { get; }
        public string ShipType { get; private init; }
        public int Size { get; private init; }

        private Ship(ShipId id, string shipType, int size, IEnumerable<Coordinate> positions)
        {
            Id = id;
            ShipType = shipType;
            Size = size;
            _positions = new HashSet<Coordinate>(positions);
            _hits = new HashSet<Coordinate>();

            if (_positions.Count != size)
                throw new ArgumentException($"Position count must match ship size ({size})");
        }

        #region Factory Methods

        public static Ship CreateCarrier(ShipId id, IEnumerable<Coordinate> positions)
            => new Ship(id, "Carrier", 5, positions);

        public static Ship CreateBattleship(ShipId id, IEnumerable<Coordinate> positions)
            => new Ship(id, "Battleship", 4, positions);

        public static Ship CreateCruiser(ShipId id, IEnumerable<Coordinate> positions)
            => new Ship(id, "Cruiser", 3, positions);

        public static Ship CreateSubmarine(ShipId id, IEnumerable<Coordinate> positions)
            => new Ship(id, "Submarine", 3, positions);

        public static Ship CreateDestroyer(ShipId id, IEnumerable<Coordinate> positions)
            => new Ship(id, "Destroyer", 2, positions);

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
        public static Ship CreateFromType(string shipType, ShipId id, IEnumerable<Coordinate> positions)
            => shipType switch
            {
                "Carrier"    => CreateCarrier(id, positions),
                "Battleship" => CreateBattleship(id, positions),
                "Cruiser"    => CreateCruiser(id, positions),
                "Submarine"  => CreateSubmarine(id, positions),
                "Destroyer"  => CreateDestroyer(id, positions),
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
    }
}