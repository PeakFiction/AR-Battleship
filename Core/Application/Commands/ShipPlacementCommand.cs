using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Commands
{
    /// <summary>
    /// Command representing a request to place a ship on the board.
    /// All fields are required — a malformed command should never reach the service.
    /// </summary>
    public sealed class ShipPlacementCommand
    {
        public PlayerId PlayerId { get; init; }
        public string ShipType { get; init; }
        public Coordinate StartCoordinate { get; init; }
        public Orientation Orientation { get; init; }

        public ShipPlacementCommand(
            PlayerId playerId,
            string shipType,
            Coordinate startCoordinate,
            Orientation orientation)
        {
            PlayerId = playerId;
            ShipType = shipType;
            StartCoordinate = startCoordinate;
            Orientation = orientation;
        }
    }
}