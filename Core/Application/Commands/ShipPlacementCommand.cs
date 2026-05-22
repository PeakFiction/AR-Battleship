// Immutable command object passed to BattleshipGameService.TryPlaceShip.
// The service uses ShipType + StartCoordinate + Orientation to calculate
// all ship cell positions via Ship.GetSize and Orientation.GetOffset.
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Commands
{
    /// <summary>
    /// Request to place a ship of a given type at a starting coordinate with
    /// the specified orientation.  All fields are required.
    /// </summary>
    public sealed class ShipPlacementCommand
    {
        /// <summary>The player placing the ship.</summary>
        public PlayerId PlayerId { get; init; }

        /// <summary>
        /// Ship type string (e.g. "Carrier", "Destroyer").
        /// Must match one of the types recognised by <see cref="Ship.GetSize"/>.
        /// </summary>
        public string ShipType { get; init; }

        /// <summary>The top-left / bow cell of the ship.</summary>
        public Coordinate StartCoordinate { get; init; }

        /// <summary>Horizontal (along X) or Vertical (along Y).</summary>
        public Orientation Orientation { get; init; }

        /// <summary>Creates a complete ship placement command.</summary>
        public ShipPlacementCommand(
            PlayerId playerId,
            string shipType,
            Coordinate startCoordinate,
            Orientation orientation)
        {
            PlayerId        = playerId;
            ShipType        = shipType;
            StartCoordinate = startCoordinate;
            Orientation     = orientation;
        }
    }
}
