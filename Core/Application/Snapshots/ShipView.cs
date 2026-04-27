using System;

namespace ARBattleship.Core.Application.Snapshots
{	
	[Serializable]
	public sealed record ShipView
	{
		public string ShipType { get; init; }
		public int Length { get; init; }
		public bool IsSunk { get; init; }

		public ShipView(String shipType, int length, bool isSunk)
		{
			ShipType = shipType;
			Length = length;
			IsSunk = isSunk;
		}
	}
}