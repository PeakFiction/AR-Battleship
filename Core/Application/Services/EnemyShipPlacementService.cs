using System;
using System.Collections.Generic;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application
{
    /// <summary>
    /// Places all standard ships randomly on PlayerTwo's board.
    /// Used by GameManager when starting a singleplayer game.
    /// </summary>
    public class EnemyShipPlacementService
    {
        private readonly Random _random;

        /// <summary>
        /// Five standard ship definitions ordered largest-first for optimal placement.
        /// Each entry holds a factory delegate, the ship's cell count, and its type name.
        /// </summary>
        private static readonly (Func<ShipId, IEnumerable<Coordinate>, Orientation, Ship> Factory, int Size, string Name)[] ShipDefinitions =
        {
            (Ship.CreateCarrier,    5, "Carrier"),
            (Ship.CreateBattleship, 4, "Battleship"),
            (Ship.CreateCruiser,    3, "Cruiser"),
            (Ship.CreateSubmarine,  3, "Submarine"),
            (Ship.CreateDestroyer,  2, "Destroyer"),
        };

        /// <summary>
        /// Creates the service with an optional seeded Random for reproducible layouts.
        /// If <paramref name="random"/> is null, a new unseeded Random is used.
        /// </summary>
        public EnemyShipPlacementService(Random? random = null)
        {
            _random = random ?? new Random();
        }

        /// <summary>
        /// Places all five ships for PlayerTwo on the given game.
        /// Each ship is retried up to maxRetriesPerShip times
        /// until a valid non-overlapping, non-adjacent position is found.
        /// </summary>
        public Result<bool> PlaceAllShips(BattleshipGame game, int maxRetriesPerShip = 200)
        {
            foreach (var (factory, size, name) in ShipDefinitions)
            {
                bool placed = false;

                for (int attempt = 0; attempt < maxRetriesPerShip; attempt++)
                {
                    var positions = GenerateRandomPositions(size, game.PlayerTwoBoard.Size, out var orientation);
                    var ship      = factory(ShipId.New(), positions, orientation);
                    var result    = game.PlaceShip(PlayerId.PlayerTwo, ship);

                    if (result.IsSuccess)
                    {
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                    return Result<bool>.Failure($"Failed to place {name} after {maxRetriesPerShip} attempts.");
            }

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Generates a random list of coordinates for a ship of the given size
        /// and outputs the chosen orientation.  The start position is constrained
        /// so the ship fits entirely within the board.
        /// </summary>
        private IEnumerable<Coordinate> GenerateRandomPositions(
            int shipSize,
            int boardSize,
            out Orientation orientation)
        {
            bool horizontal = _random.Next(2) == 0;
            orientation     = horizontal ? Orientation.Horizontal : Orientation.Vertical;

            // Constrain start so the ship fits without going out of bounds
            int startX = horizontal
                ? _random.Next(0, boardSize - shipSize + 1)
                : _random.Next(0, boardSize);

            int startY = horizontal
                ? _random.Next(0, boardSize)
                : _random.Next(0, boardSize - shipSize + 1);

            var positions = new List<Coordinate>(shipSize);

            for (int i = 0; i < shipSize; i++)
            {
                positions.Add(horizontal
                    ? new Coordinate(startX + i, startY)
                    : new Coordinate(startX, startY + i));
            }

            return positions;
        }
    }
}
