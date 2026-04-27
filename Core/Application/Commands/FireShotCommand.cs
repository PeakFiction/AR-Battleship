using System;
using ARBattleship.Core.Domain;
using System.Runtime.CompilerServices;

namespace ARBattleship.Core.Application.Commands 
{
	[Serializable]
	public sealed class FireShotCommand
	{
		public PlayerId PlayerId { get; init; }
		public Coordinate Coordinate { get; init; }

		public FireShotCommand(PlayerId playerId, Coordinate coordinate)
		{
			PlayerId = playerId;
			Coordinate = coordinate;
		}
	}
}