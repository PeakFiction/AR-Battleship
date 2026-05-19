using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Snapshots
{   
    [Serializable]
    public sealed record ShipView
    {
        public string ShipType { get; init; }
        public int Length { get; init; }
        public bool IsSunk { get; init; }
        public Orientation Orientation { get; init; }

        public ShipView(string shipType, int length, bool isSunk, Orientation orientation)
        {
            ShipType = shipType;
            Length = length;
            IsSunk = isSunk;
            Orientation = orientation;
        }
    }
}