using System;
using Core.Application.Enums;

namespace Core.Application.Events;

[Serializable]
public sealed record GameEndedEvent(
	PlayerId Winner
) : IGameEvent;