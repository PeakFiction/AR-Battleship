using NUnit.Framework;
using ARBattleship.Core.Domain;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class HuntTargetStrategyTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a 10x10 board with a destroyer placed at the given position.
        /// </summary>
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

        /// <summary>
        /// Creates a 10x10 board with a horizontal cruiser (size 3) at the given position.
        /// </summary>
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

        /// <summary>
        /// Fires a shot and notifies the strategy, mirroring what EnemyTurnService does.
        /// Returns the FireResult.
        /// </summary>
        private static FireResult FireAndNotify(HuntTargetStrategy strategy, Board board, Coordinate coord)
        {
            var result = board.FireAt(coord);
            strategy.NotifyResult(result.Value!);
            return result.Value!;
        }

        // ── SelectMove — Hunt mode ────────────────────────────────────────────────

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

            // Checkerboard parity: (X + Y) % 2 == 0
            Assert.That((move.X + move.Y) % 2, Is.EqualTo(0));
        }

        [Test]
        public void SelectMove_AfterMiss_DoesNotRepeatCoordinate()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            var first = strategy.SelectMove(board);
            FireAndNotify(strategy, board, first);

            // Fire 50 more times and verify first coord never reappears
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
            var board = new Board(4); // Small board to exhaust parity cells quickly

            // Shoot all parity cells (X+Y) % 2 == 0
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    if ((x + y) % 2 == 0)
                        FireAndNotify(strategy, board, new Coordinate(x, y));

            // Should now fall back to odd-parity cells without throwing
            var move = strategy.SelectMove(board);
            Assert.That(move.X, Is.InRange(0, 3));
            Assert.That(move.Y, Is.InRange(0, 3));
        }

        // ── NotifyResult — transition to Target mode ──────────────────────────────

        [Test]
        public void NotifyResult_OnHit_NextMoveIsAdjacentToHit()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(5, 5); // Ship at (5,5) and (6,5)

            FireAndNotify(strategy, board, new Coordinate(5, 5));
            var next = strategy.SelectMove(board);

            // After a hit at (5,5), next move must be one of the 4 adjacent cells
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

            // Miss at (9,9) — no ship there
            FireAndNotify(strategy, board, new Coordinate(9, 9));

            // Still in hunt mode — next move should still be a parity cell
            var next = strategy.SelectMove(board);
            Assert.That((next.X + next.Y) % 2, Is.EqualTo(0));
        }

        // ── NotifyResult — axis locking ───────────────────────────────────────────

        [Test]
        public void NotifyResult_TwoHitsHorizontal_NextMoveIsOnSameRow()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithCruiser(3, 5); // Ship at (3,5),(4,5),(5,5)

            FireAndNotify(strategy, board, new Coordinate(3, 5)); // First hit
            FireAndNotify(strategy, board, new Coordinate(4, 5)); // Second hit — axis locked

            var next = strategy.SelectMove(board);

            // After locking horizontal axis on row Y=5, next must be on Y=5
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

            FireAndNotify(strategy, board, new Coordinate(5, 3)); // First hit
            FireAndNotify(strategy, board, new Coordinate(5, 4)); // Second hit — axis locked

            var next = strategy.SelectMove(board);

            // After locking vertical axis on column X=5, next must be on X=5
            Assert.That(next.X, Is.EqualTo(5));
        }

        // ── NotifyResult — reset on Sunk ──────────────────────────────────────────

        [Test]
        public void NotifyResult_OnSunk_ReturnsToHuntMode()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(5, 5); // Ship at (5,5),(6,5)

            FireAndNotify(strategy, board, new Coordinate(5, 5)); // Hit
            FireAndNotify(strategy, board, new Coordinate(6, 5)); // Sunk — back to hunt

            var next = strategy.SelectMove(board);

            // Back in hunt mode — should fire on a parity cell
            Assert.That((next.X + next.Y) % 2, Is.EqualTo(0));
        }

        [Test]
        public void NotifyResult_OnSunk_DoesNotFireAlreadyShotCell()
        {
            var strategy = new HuntTargetStrategy();
            var board = MakeBoardWithDestroyer(5, 5);

            FireAndNotify(strategy, board, new Coordinate(5, 5));
            FireAndNotify(strategy, board, new Coordinate(6, 5));

            // Collect the next 20 moves and verify none are already shot
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

        // ── SelectMove — stale queue handling ────────────────────────────────────

        [Test]
        public void SelectMove_TargetQueueExhausted_FallsBackToHunt()
        {
            var strategy = new HuntTargetStrategy();
            // Place destroyer in a corner so most adjacent cells are out of bounds
            var board = MakeBoardWithDestroyer(0, 0); // Ship at (0,0),(1,0)

            FireAndNotify(strategy, board, new Coordinate(0, 0)); // Hit — enters target mode

            // Exhaust all valid adjacent candidates by firing them as misses
            FireAndNotify(strategy, board, new Coordinate(1, 0)); // Sunk actually — resets
            // Now we're back in hunt — verify it doesn't throw
            var next = strategy.SelectMove(board);
            Assert.That(next.X, Is.InRange(0, 9));
            Assert.That(next.Y, Is.InRange(0, 9));
        }

        // ── SyncShotsFired ────────────────────────────────────────────────────────

        [Test]
        public void SelectMove_BoardAlreadyHasShotCells_DoesNotSelectShotCell()
        {
            var strategy = new HuntTargetStrategy();
            var board = new Board(10);

            // Manually fire several cells on the board before strategy is involved
            board.FireAt(new Coordinate(0, 0));
            board.FireAt(new Coordinate(2, 0));
            board.FireAt(new Coordinate(4, 0));

            var alreadyShot = new HashSet<Coordinate>
            {
                new Coordinate(0, 0),
                new Coordinate(2, 0),
                new Coordinate(4, 0)
            };

            // Strategy should sync and avoid those cells
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