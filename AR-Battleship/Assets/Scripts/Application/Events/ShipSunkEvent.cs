using System;
using Core.Application.Enums;

namespace Core.Application.Events;

[Serializable]
public sealed record ShipSunkEvent(
	PlayerId PlayerId,
	string ShipType
) : IGameEvent;