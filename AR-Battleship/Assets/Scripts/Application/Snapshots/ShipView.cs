using System;

namespace Core.Application.Snapshots;

[Serializable]
public sealed record ShipView
{
	public string ShipType { get; init; }
	public int Length { get; init; }
	public bool IsSunk { get; init; }

	public ShipView(String shipType, int length, bool IsSunk)
	{
		ShipType = shipType;
		Length = length;
		IsSunk = isSunk;
	}
}