using System;

namespace Core.Application.Commands;

[Serializable]
public sealed class ShipPlacementCommand
{
	public PlayerId PlayerId { get; init; }
	public ShipType ShipType { get; init; }
	public Coordinate Start { get; init; }
	public Orientation Orientation { get; init; }

	public ShipPlacementCommand(
		PlayerId playerId,
		ShipType shipType,
		Coordinate start,
		Orientation orientation)
	{
		PlayerId = playerId;
		ShipType = shipType;
		Start = start;
		Orientation = orientation;
	}
}