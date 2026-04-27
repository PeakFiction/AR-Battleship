using System;
using ARBattleship.Core.Domain;
namespace ARBattleship.Core.Application.Events {
	[Serializable]
	public sealed record TurnChangedEvent(
		PlayerId CurrentTurn
	) : IGameEvent;
}