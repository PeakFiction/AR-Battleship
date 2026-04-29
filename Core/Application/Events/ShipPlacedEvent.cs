using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Events {
	[Serializable]
	public sealed record ShipPlacedEvent(
		PlayerId PlayerId,
		string ShipType
	) : IGameEvent;
}