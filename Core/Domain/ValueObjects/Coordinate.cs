using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Immutable (column, row) cell address on a Battleship board.
    /// Safe to use as a Dictionary key or in a HashSet.
    /// </summary>
    public readonly struct Coordinate : IEquatable<Coordinate>
    {
        /// <summary>Column index (0-based). Column A = 0, column J = 9.</summary>
        public int X { get; }

        /// <summary>Row index (0-based). Row 1 = 0, row 10 = 9.</summary>
        public int Y { get; }

        /// <summary>
        /// Creates a coordinate at the given column and row.
        /// No range validation is performed here; the Board enforces bounds.
        /// </summary>
        public Coordinate(int x, int y) { X = x; Y = y; }

        /// <summary>Returns true if both X and Y match.</summary>
        public bool Equals(Coordinate other) => X == other.X && Y == other.Y;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Coordinate other && Equals(other);

        /// <summary>Hash combining X and Y — safe for Dictionary and HashSet use.</summary>
        public override int GetHashCode() => HashCode.Combine(X, Y);

        /// <summary>True when both coordinates occupy the same cell.</summary>
        public static bool operator ==(Coordinate l, Coordinate r) => l.Equals(r);

        /// <summary>True when the coordinates occupy different cells.</summary>
        public static bool operator !=(Coordinate l, Coordinate r) => !(l == r);

        /// <summary>Returns "(X, Y)" for debugging.</summary>
        public override string ToString() => $"({X}, {Y})";
    }
}
