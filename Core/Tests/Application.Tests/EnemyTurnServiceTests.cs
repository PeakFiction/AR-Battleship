using NUnit.Framework;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application.Services;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Tests.Application
{
    [TestFixture]
    public class EnemyTurnServiceTests
    {

        private static void PlaceShip(BattleshipGame game, PlayerId player, string type, int startX, int startY)
        {
            var size = Ship.GetSize(type);
            var positions = Enumerable.Range(0, size)
                .Select(i => new Coordinate(startX + i, startY))
                .ToList();
            game.PlaceShip(player, Ship.CreateFromType(type, ShipId.New(), positions));
        }

        private static void PlaceFullFleet(BattleshipGame game, PlayerId player, int rowOffset = 0)
        {
            PlaceShip(game, player, "Carrier",    0, rowOffset + 0);
            PlaceShip(game, player, "Battleship", 0, rowOffset + 1);
            PlaceShip(game, player, "Cruiser",    0, rowOffset + 2);
            PlaceShip(game, player, "Submarine",  0, rowOffset + 3);
            PlaceShip(game, player, "Destroyer",  0, rowOffset + 4);
        }

        private static EnemyTurnService MakeService(out BattleshipGame game)
        {
            game = new BattleshipGame(10);
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();

            var strategy = new HuntTargetStrategy();
            return new EnemyTurnService(game, strategy);
        }


        [Test]
        public void TakeTurn_NotEnemyTurn_ReturnsFailure()
        {
            var service = MakeService(out _);
            var result = service.TakeTurn();
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TakeTurn_EnemyTurn_ReturnsSuccess()
        {
            var service = MakeService(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            var result = service.TakeTurn();

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void TakeTurn_EnemyTurn_ReturnsShotResult()
        {
            var service = MakeService(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            var result = service.TakeTurn();

            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.Outcome, Is.AnyOf(ShotResult.Hit, ShotResult.Miss, ShotResult.Sunk));
        }

        [Test]
        public void TakeTurn_EnemyTurn_SwitchesTurnBackToPlayerOne()
        {
            var service = MakeService(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            service.TakeTurn();

            Assert.That(game.CurrentTurn, Is.EqualTo(PlayerId.PlayerOne));
        }

        [Test]
        public void TakeTurn_GameAlreadyOver_ReturnsFailure()
        {
            var service = MakeService(out var game);

            for (int row = 0; row < 5 && !game.IsGameOver; row++)
            {
                int size = new[] { 5, 4, 3, 3, 2 }[row];
                for (int x = 0; x < size && !game.IsGameOver; x++)
                {
                    if (game.CurrentTurn == PlayerId.PlayerOne)
                        game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9 - row - x));
                    else
                        game.FireShot(PlayerId.PlayerTwo, new Coordinate(x, row));
                }
            }

            var result = service.TakeTurn();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TakeTurn_FiresValidCoordinate_CoordinateIsOnBoard()
        {
            var service = MakeService(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            var result = service.TakeTurn();

            Assert.That(result.Value!.Coordinate.X, Is.InRange(0, 9));
            Assert.That(result.Value!.Coordinate.Y, Is.InRange(0, 9));
        }

        [Test]
        public void TakeTurn_CalledTwiceInEnemyTurn_SecondCallFails()
        {
            var service = MakeService(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));
            service.TakeTurn();

            var result = service.TakeTurn();

            Assert.That(result.IsSuccess, Is.False);
        }
    }
}