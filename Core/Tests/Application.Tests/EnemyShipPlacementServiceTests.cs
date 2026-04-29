using NUnit.Framework;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application;
using System;
using System.Linq;

namespace ARBattleship.Core.Tests.Application
{
    [TestFixture]
    public class EnemyShipPlacementServiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        private static EnemyShipPlacementService MakeService(int? seed = null)
            => new EnemyShipPlacementService(seed.HasValue ? new Random(seed.Value) : null);

        private static BattleshipGame MakeGame() => new BattleshipGame(10);

        // ── PlaceAllShips ─────────────────────────────────────────────────────────

        [Test]
        public void PlaceAllShips_StandardGame_ReturnsSuccess()
        {
            var service = MakeService(42);
            var game = MakeGame();

            var result = service.PlaceAllShips(game);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void PlaceAllShips_StandardGame_PlayerTwoBoardHasShips()
        {
            var service = MakeService(42);
            var game = MakeGame();

            service.PlaceAllShips(game);

            Assert.That(game.PlayerTwoBoard.HasAnyShips(), Is.True);
        }

        [Test]
        public void PlaceAllShips_StandardGame_PlacesFiveShips()
        {
            var service = MakeService(42);
            var game = MakeGame();

            service.PlaceAllShips(game);

            Assert.That(game.PlayerTwoBoard.GetShips().Count(), Is.EqualTo(5));
        }

        [Test]
        public void PlaceAllShips_StandardGame_AllShipTypesPresent()
        {
            var service = MakeService(42);
            var game = MakeGame();

            service.PlaceAllShips(game);

            var shipTypes = game.PlayerTwoBoard.GetShips().Select(s => s.ShipType).ToList();
            Assert.That(shipTypes, Does.Contain("Carrier"));
            Assert.That(shipTypes, Does.Contain("Battleship"));
            Assert.That(shipTypes, Does.Contain("Cruiser"));
            Assert.That(shipTypes, Does.Contain("Submarine"));
            Assert.That(shipTypes, Does.Contain("Destroyer"));
        }

        [Test]
        public void PlaceAllShips_StandardGame_DoesNotPlaceOnPlayerOneBoard()
        {
            var service = MakeService(42);
            var game = MakeGame();

            service.PlaceAllShips(game);

            Assert.That(game.PlayerOneBoard.HasAnyShips(), Is.False);
        }

        [Test]
        public void PlaceAllShips_CalledTwice_SecondCallFails()
        {
            var service = MakeService(42);
            var game = MakeGame();

            service.PlaceAllShips(game);
            var result = service.PlaceAllShips(game);

            // All ship types already placed — second call should fail
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceAllShips_ZeroRetries_ReturnsFailure()
        {
            var service = MakeService(42);
            var game = MakeGame();

            var result = service.PlaceAllShips(game, maxRetriesPerShip: 0);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceAllShips_MultipleCalls_ProduceDifferentLayouts()
        {
            // Two services with different seeds should produce different placements
            var service1 = MakeService(1);
            var service2 = MakeService(99);

            var game1 = MakeGame();
            var game2 = MakeGame();

            service1.PlaceAllShips(game1);
            service2.PlaceAllShips(game2);

            var positions1 = game1.PlayerTwoBoard.GetShips()
                .SelectMany(s => s.Positions)
                .OrderBy(c => c.X).ThenBy(c => c.Y)
                .ToList();

            var positions2 = game2.PlayerTwoBoard.GetShips()
                .SelectMany(s => s.Positions)
                .OrderBy(c => c.X).ThenBy(c => c.Y)
                .ToList();

            // Different seeds should produce different layouts
            Assert.That(positions1.SequenceEqual(positions2), Is.False);
        }

        [Test]
        public void PlaceAllShips_AllShipsWithinBoardBounds()
        {
            var service = MakeService(42);
            var game = MakeGame();
            service.PlaceAllShips(game);

            var allPositions = game.PlayerTwoBoard.GetShips()
                .SelectMany(s => s.Positions);

            foreach (var pos in allPositions)
            {
                Assert.That(pos.X, Is.InRange(0, 9));
                Assert.That(pos.Y, Is.InRange(0, 9));
            }
        }

        [Test]
        public void PlaceAllShips_NoShipsOverlap()
        {
            var service = MakeService(42);
            var game = MakeGame();
            service.PlaceAllShips(game);

            var allPositions = game.PlayerTwoBoard.GetShips()
                .SelectMany(s => s.Positions)
                .ToList();

            var distinctPositions = allPositions.Distinct().ToList();
            Assert.That(allPositions.Count, Is.EqualTo(distinctPositions.Count));
        }
    }
}