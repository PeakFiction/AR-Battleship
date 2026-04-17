using System;
using Core.Application.Enums;

namespace Core.Application.Events;

[Serializable]
public sealed record TurnChangedEvent(
	PlayerId CurrentTurn
) : IGameEvent;