using System;

namespace ARBattleship.Core.Domain
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public static class OrientationExtensions
    {
        public static Coordinate GetOffset(this Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => new Coordinate(1, 0),
                Orientation.Vertical => new Coordinate(0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
            };
        }
    }
}