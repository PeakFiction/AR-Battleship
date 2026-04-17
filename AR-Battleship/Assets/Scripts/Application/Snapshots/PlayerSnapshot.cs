using System;
using System.Collection.Generic;
using System.Collection.Enums;

namespace Core.Application.Snapshots;

[Serializable]
public sealed record PlayerSnapshot
{
	public PlayerId playerId { get; init; }
	public IReadOnlyList<CellView> Cells { get; init; }
	public IReadOnlyList<ShipView> Ships { get; init; }

	public PlayerSnapshot(PlayerId playerId, IReadOnlyList<CellView> cells, IReadOnlyList<ShipView> ships)
	{
		PlayerId = playerId;
		Cells = cells;
		Ships = ships;
	}
}