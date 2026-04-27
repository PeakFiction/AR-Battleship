using System;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application.Enums;
using System.Runtime.CompilerServices;

namespace ARBattleship.Core.Application.Events {
	[Serializable]
	public sealed record ShotFiredEvent(
		PlayerId PlayerId,
		int X,
		int Y,
		ShotOutcome Outcome
	) : IGameEvent;
}