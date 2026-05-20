using System;
using System.Collections.Generic;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application
{
    /// <summary>
    /// Randomly places all standard ships on the enemy's board (PlayerTwo).
    /// Retries placement for each ship until a valid position is found.
    /// </summary>
    public class EnemyShipPlacementService
    {
        private readonly Random _random;

        // The five standard ship types in placement order (largest first to reduce
        // retry count on a filling board)
        private static readonly (Func<ShipId, IEnumerable<Coordinate>, Orientation, Ship> Factory, int Size, string Name)[] ShipDefinitions =
        {
            (Ship.CreateCarrier,    5, "Carrier"),
            (Ship.CreateBattleship, 4, "Battleship"),
            (Ship.CreateCruiser,    3, "Cruiser"),
            (Ship.CreateSubmarine,  3, "Submarine"),
            (Ship.CreateDestroyer,  2, "Destroyer"),
        };

        public EnemyShipPlacementService(Random? random = null)
        {
            _random = random ?? new Random();
        }

        /// <summary>
        /// Places all ships for PlayerTwo on the given game. Returns a failure Result
        /// if any ship could not be placed after the maximum number of retries.
        /// </summary>
        public Result<bool> PlaceAllShips(BattleshipGame game, int maxRetriesPerShip = 200)
        {
            foreach (var (factory, size, name) in ShipDefinitions)
            {
                var placed = false;

                for (int attempt = 0; attempt < maxRetriesPerShip; attempt++)
                {
                    var positions = GenerateRandomPositions(size, game.PlayerTwoBoard.Size, out var orientation);
                    var ship = factory(ShipId.New(), positions, orientation);
                    var result = game.PlaceShip(PlayerId.PlayerTwo, ship);

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

        // ── Private helpers ───────────────────────────────────────────────────────

        private IEnumerable<Coordinate> GenerateRandomPositions(int shipSize, int boardSize, out Orientation orientation)
        {
            bool horizontal = _random.Next(2) == 0;
            orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;

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