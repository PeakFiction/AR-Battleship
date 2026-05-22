using NUnit.Framework;
using ARBattleship.Core.Domain;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class HuntTargetStrategyTests
    {
        private static Board MakeBoardWithDestroyer(int startX = 0, int startY = 0)
        {
            var board = new Board(10);
            var ship = Ship.CreateDestroyer(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY)
            });
            board.PlaceShip(ship);
            return board;
        }

        private static Board MakeBoardWithCruiser(int startX = 0, int startY = 0)
        {
            var board = new Board(10);
            var ship = Ship.CreateCruiser(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY),
                new Coordinate(startX + 2, startY)
            });
            board.PlaceShip(ship);
            return board;
        }

        private static FireResult FireAndNotify(HuntTargetStrategy strategy, Board board, Coordinate coord)
        {
            var result = board.FireAt(coord);
            strategy.NotifyResult(result.Value!);
            return result.Value!;
        }

        [Test]
        public void SelectMove_FreshBoard_ReturnsValidCoordinate()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            var move = strategy.SelectMove(board);

            Assert.That(move.X, Is.InRange(0, 9));
            Assert.That(move.Y, Is.InRange(0, 9));
        }

        [Test]
        public void SelectMove_HuntMode_ReturnsCheckerboardCell()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            var move = strategy.SelectMove(board);

            Assert.That((move.X + move.Y) % 2, Is.EqualTo(0));
        }

        [Test]
        public void SelectMove_AfterMiss_DoesNotRepeatCoordinate()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            var first = strategy.SelectMove(board);
            FireAndNotify(strategy, board, first);

            var seen = new HashSet<Coordinate> { first };
            for (int i = 0; i < 50; i++)
            {
                if (board.GetCell(new Coordinate(i % 10, i / 10)).IsShot) continue;
                var next = strategy.SelectMove(board);
                Assert.That(seen, Does.Not.Contain(next));
                FireAndNotify(strategy, board, next);
                seen.Add(next);
            }
        }

        [Test]
        public void SelectMove_AllParityCellsExhausted_FallsBackToAnyUnshotCell()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(4);

            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    if ((x + y) % 2 == 0)
                        FireAndNotify(strategy, board, new Coordinate(x, y));

            var move = strategy.SelectMove(board);
            Assert.That(move.X, Is.InRange(0, 3));
            Assert.That(move.Y, Is.InRange(0, 3));
        }

        [Test]
        public void NotifyResult_OnHit_NextMoveIsAdjacentToHit()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(5, 5);

            FireAndNotify(strategy, board, new Coordinate(5, 5));
            var next = strategy.SelectMove(board);

            var adjacent = new[]
            {
                new Coordinate(4, 5),
                new Coordinate(6, 5),
                new Coordinate(5, 4),
                new Coordinate(5, 6)
            };
            Assert.That(adjacent, Does.Contain(next));
        }

        [Test]
        public void NotifyResult_OnMiss_DoesNotSwitchToTargetMode()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            FireAndNotify(strategy, board, new Coordinate(9, 9));

            var next = strategy.SelectMove(board);
            Assert.That((next.X + next.Y) % 2, Is.EqualTo(0));
        }


        [Test]
        public void NotifyResult_TwoHitsHorizontal_NextMoveIsOnSameRow()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithCruiser(3, 5);

            FireAndNotify(strategy, board, new Coordinate(3, 5));
            FireAndNotify(strategy, board, new Coordinate(4, 5));

            var next = strategy.SelectMove(board);

            Assert.That(next.Y, Is.EqualTo(5));
        }

        [Test]
        public void NotifyResult_TwoHitsVertical_NextMoveIsOnSameColumn()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);
            var ship = Ship.CreateCruiser(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(5, 3),
                new Coordinate(5, 4),
                new Coordinate(5, 5)
            });
            board.PlaceShip(ship);

            FireAndNotify(strategy, board, new Coordinate(5, 3));
            FireAndNotify(strategy, board, new Coordinate(5, 4));

            var next = strategy.SelectMove(board);

            Assert.That(next.X, Is.EqualTo(5));
        }

        [Test]
        public void NotifyResult_OnSunk_ReturnsToHuntMode()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(5, 5);

            FireAndNotify(strategy, board, new Coordinate(5, 5));
            FireAndNotify(strategy, board, new Coordinate(6, 5));

            var next = strategy.SelectMove(board);

            Assert.That((next.X + next.Y) % 2, Is.EqualTo(0));
        }

        [Test]
        public void NotifyResult_OnSunk_DoesNotFireAlreadyShotCell()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(5, 5);

            FireAndNotify(strategy, board, new Coordinate(5, 5));
            FireAndNotify(strategy, board, new Coordinate(6, 5));

            var shotCells = new HashSet<Coordinate>
            {
                new Coordinate(5, 5),
                new Coordinate(6, 5)
            };

            for (int i = 0; i < 20; i++)
            {
                var next = strategy.SelectMove(board);
                Assert.That(shotCells, Does.Not.Contain(next));
                FireAndNotify(strategy, board, next);
                shotCells.Add(next);
            }
        }


        [Test]
        public void SelectMove_TargetQueueExhausted_FallsBackToHunt()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(0, 0);

            FireAndNotify(strategy, board, new Coordinate(0, 0));

            FireAndNotify(strategy, board, new Coordinate(1, 0));
            var next = strategy.SelectMove(board);
            Assert.That(next.X, Is.InRange(0, 9));
            Assert.That(next.Y, Is.InRange(0, 9));
        }


        [Test]
        public void SelectMove_BoardAlreadyHasShotCells_DoesNotSelectShotCell()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            board.FireAt(new Coordinate(0, 0));
            board.FireAt(new Coordinate(2, 0));
            board.FireAt(new Coordinate(4, 0));

            var alreadyShot = new HashSet<Coordinate>
            {
                new Coordinate(0, 0),
                new Coordinate(2, 0),
                new Coordinate(4, 0)
            };

            for (int i = 0; i < 20; i++)
            {
                var next = strategy.SelectMove(board);
                Assert.That(alreadyShot, Does.Not.Contain(next));
                board.FireAt(next);
                alreadyShot.Add(next);
            }
        }
    }
}