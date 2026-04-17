using System;
using Core.Application.Enums;

namespace Core.Application.Events;

[Serializable]
public sealed record ShotFiredEvent(
	PlayerId playerId,
	int X,
	int Y
) : IGameEvent;