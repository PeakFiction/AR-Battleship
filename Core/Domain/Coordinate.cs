using System;

namespace Domain
{
    public readonly struct Coordinate(int x, int y) : IEquatable<Coordinate>
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        public bool Equals(Coordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Coordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Coordinate left, Coordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Coordinate left, Coordinate right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}