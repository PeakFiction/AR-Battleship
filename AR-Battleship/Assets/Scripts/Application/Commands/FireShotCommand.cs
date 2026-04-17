using System;

namespace Core.Application.Commands;

[Serializable]
public sealed class FireShotCommand
{
	public PlayerId PlayerId { get; init; }
	public Coordinate Target { get; init; }

	public FireShotCommand(PlayerId playerId, Coordinate target)
	{
		PlayerId = playerId;
		Target = target;
	}
}