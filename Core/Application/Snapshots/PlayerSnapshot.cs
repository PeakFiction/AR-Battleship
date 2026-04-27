using System;
using System.Collections.Generic;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Snapshots
{	
	[Serializable]
	public sealed record PlayerSnapshot
	{
		public PlayerId Id { get; init; }
		public IReadOnlyList<CellView> Cells { get; init; }
		public IReadOnlyList<ShipView> Ships { get; init; }

		public PlayerSnapshot(PlayerId playerId, IReadOnlyList<CellView> cells, IReadOnlyList<ShipView> ships)
		{
			Id = playerId;
			Cells = cells;
			Ships = ships;
		}
	}
}