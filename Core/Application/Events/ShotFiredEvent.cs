using System;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application.Enums;

namespace ARBattleship.Core.Application.Events {
    [Serializable]
    public sealed record ShotFiredEvent(
        PlayerId PlayerId,
        int X,
        int Y,
        ShotOutcome Outcome,
        int? HitSegmentIndex,
        string? ShipOrientation,
		string? ShipType
    ) : IGameEvent;
}