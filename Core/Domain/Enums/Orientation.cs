using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>Ship placement orientation on the board.</summary>
    public enum Orientation
    {
        /// <summary>Ship occupies consecutive cells along the X axis (left to right).</summary>
        Horizontal,

        /// <summary>Ship occupies consecutive cells along the Y axis (top to bottom).</summary>
        Vertical
    }

    /// <summary>
    /// Extension methods that convert an Orientation into spatial step vectors.
    /// </summary>
    public static class OrientationExtensions
    {
        /// <summary>
        /// Returns the (dx, dy) step to apply when iterating ship cells.
        /// Used by BattleshipGameService.CalculatePositions.
        /// </summary>
        public static Coordinate GetOffset(this Orientation orientation) =>
            orientation switch
            {
                Orientation.Horizontal => new Coordinate(1, 0),
                Orientation.Vertical   => new Coordinate(0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
            };
    }
}
