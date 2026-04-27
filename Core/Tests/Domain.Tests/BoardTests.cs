using NUnit.Framework;
using ARBattleship.Core.Domain;
using System.Collections.Generic;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class BoardTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Board MakeBoard(int size = 10) => new Board(size);

        private static Ship MakeDestroyer(int startX = 0, int startY = 0) =>
            Ship.CreateDestroyer(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY)
            });

        private static Ship MakeCruiser(int startX = 0, int startY = 0) =>
            Ship.CreateCruiser(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY),
                new Coordinate(startX + 2, startY)
            });

        private static Ship MakeCarrier(int startX = 0, int startY = 0) =>
            Ship.CreateCarrier(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY),
                new Coordinate(startX + 2, startY),
                new Coordinate(startX + 3, startY),
                new Coordinate(startX + 4, startY)
            });

        // ── Constructor ───────────────────────────────────────────────────────────

        [Test]
        public void Constructor_ValidSize_SizeIsSet()
        {
            var board = MakeBoard(10);
            Assert.That(board.Size, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_ValidSize_AllCellsInitialised()
        {
            var board = MakeBoard(10);
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    Assert.That(board.GetCell(new Coordinate(x, y)), Is.Not.Null);
        }

        [Test]
        public void Constructor_NewBoard_HasNoShips()
        {
            var board = MakeBoard();
            Assert.That(board.HasAnyShips(), Is.False);
        }

        // ── HasAnyShips ───────────────────────────────────────────────────────────

        [Test]
        public void HasAnyShips_AfterPlacingShip_ReturnsTrue()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer());
            Assert.That(board.HasAnyShips(), Is.True);
        }

        // ── CanPlaceShip ──────────────────────────────────────────────────────────

        [Test]
        public void CanPlaceShip_ValidPositions_ReturnsTrue()
        {
            var board = MakeBoard();
            var positions = new List<Coordinate>
            {
                new Coordinate(0, 0),
                new Coordinate(1, 0)
            };
            Assert.That(board.CanPlaceShip(positions), Is.True);
        }

        [Test]
        public void CanPlaceShip_OutOfBoundsPosition_ReturnsFalse()
        {
            var board = MakeBoard();
            var positions = new List<Coordinate>
            {
                new Coordinate(9, 0),
                new Coordinate(10, 0) // out of bounds
            };
            Assert.That(board.CanPlaceShip(positions), Is.False);
        }

        [Test]
        public void CanPlaceShip_OverlappingOccupiedCell_ReturnsFalse()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0)); // occupies (0,0) and (1,0)

            var positions = new List<Coordinate>
            {
                new Coordinate(1, 0), // overlaps
                new Coordinate(1, 1)
            };
            Assert.That(board.CanPlaceShip(positions), Is.False);
        }

        // ── PlaceShip ─────────────────────────────────────────────────────────────

        [Test]
        public void PlaceShip_ValidShip_ReturnsSuccess()
        {
            var board = MakeBoard();
            var result = board.PlaceShip(MakeDestroyer());
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void PlaceShip_ValidShip_CellsMarkAsOccupied()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));

            Assert.That(board.GetCell(new Coordinate(0, 0)).HasShip, Is.True);
            Assert.That(board.GetCell(new Coordinate(1, 0)).HasShip, Is.True);
        }

        [Test]
        public void PlaceShip_ValidShip_AppearsInGetShips()
        {
            var board = MakeBoard();
            var ship = MakeDestroyer();
            board.PlaceShip(ship);

            Assert.That(board.GetShips(), Has.Member(ship));
        }

        [Test]
        public void PlaceShip_OutOfBounds_ReturnsFailure()
        {
            var board = MakeBoard();
            var positions = new List<Coordinate>
            {
                new Coordinate(9, 0),
                new Coordinate(10, 0)
            };
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            var result = board.PlaceShip(ship);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_OverlappingShip_ReturnsFailure()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));

            var overlapping = Ship.CreateCruiser(ShipId.New(), new List<Coordinate>
            {
                new Coordinate(1, 0),
                new Coordinate(2, 0),
                new Coordinate(3, 0)
            });
            var result = board.PlaceShip(overlapping);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_DuplicateShipType_ReturnsFailure()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));

            // Try placing a second destroyer
            var secondDestroyer = MakeDestroyer(0, 2);
            var result = board.PlaceShip(secondDestroyer);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_DuplicateShipId_ReturnsFailure()
        {
            var board = MakeBoard();
            var id = ShipId.New();

            var ship1 = Ship.CreateDestroyer(id, new List<Coordinate>
            {
                new Coordinate(0, 0),
                new Coordinate(1, 0)
            });
            var ship2 = Ship.CreateCruiser(id, new List<Coordinate>
            {
                new Coordinate(0, 2),
                new Coordinate(1, 2),
                new Coordinate(2, 2)
            });

            board.PlaceShip(ship1);
            var result = board.PlaceShip(ship2);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_MultipleDistinctShips_AllSucceed()
        {
            var board = MakeBoard();

            Assert.That(board.PlaceShip(MakeDestroyer(0, 0)).IsSuccess, Is.True);
            Assert.That(board.PlaceShip(MakeCruiser(0, 2)).IsSuccess, Is.True);
            Assert.That(board.PlaceShip(MakeCarrier(0, 4)).IsSuccess, Is.True);
        }

        // ── FireAt ────────────────────────────────────────────────────────────────

        [Test]
        public void FireAt_OutOfBounds_ReturnsFailure()
        {
            var board = MakeBoard();
            var result = board.FireAt(new Coordinate(10, 10));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireAt_EmptyCell_ReturnsMiss()
        {
            var board = MakeBoard();
            var result = board.FireAt(new Coordinate(9, 9));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Outcome, Is.EqualTo(ShotResult.Miss));
        }

        [Test]
        public void FireAt_CellWithShip_ReturnsHit()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));

            var result = board.FireAt(new Coordinate(0, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Outcome, Is.EqualTo(ShotResult.Hit));
        }

        [Test]
        public void FireAt_HitResult_ContainsCorrectShipName()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));

            var result = board.FireAt(new Coordinate(0, 0));

            Assert.That(result.Value!.ShipName, Is.EqualTo("Destroyer"));
        }

        [Test]
        public void FireAt_LastCellOfShip_ReturnsSunk()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0)); // (0,0) and (1,0)

            board.FireAt(new Coordinate(0, 0));
            var result = board.FireAt(new Coordinate(1, 0));

            Assert.That(result.Value!.Outcome, Is.EqualTo(ShotResult.Sunk));
        }

        [Test]
        public void FireAt_SunkResult_ContainsCorrectShipName()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));

            board.FireAt(new Coordinate(0, 0));
            var result = board.FireAt(new Coordinate(1, 0));

            Assert.That(result.Value!.ShipName, Is.EqualTo("Destroyer"));
        }

        [Test]
        public void FireAt_SameCoordinateTwice_ReturnsFailure()
        {
            var board = MakeBoard();
            board.FireAt(new Coordinate(0, 0));

            var result = board.FireAt(new Coordinate(0, 0));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireAt_MissResult_CoordinateIsCorrect()
        {
            var board = MakeBoard();
            var coord = new Coordinate(5, 5);

            var result = board.FireAt(coord);

            Assert.That(result.Value!.Coordinate, Is.EqualTo(coord));
        }

        [Test]
        public void FireAt_HitResult_CoordinateIsCorrect()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));
            var coord = new Coordinate(0, 0);

            var result = board.FireAt(coord);

            Assert.That(result.Value!.Coordinate, Is.EqualTo(coord));
        }

        // ── AllShipsSunk ──────────────────────────────────────────────────────────

        [Test]
        public void AllShipsSunk_NoShipsPlaced_ReturnsTrue()
        {
            // Vacuously true — all zero ships are sunk
            var board = MakeBoard();
            Assert.That(board.AllShipsSunk(), Is.True);
        }

        [Test]
        public void AllShipsSunk_ShipsNotYetHit_ReturnsFalse()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer());
            Assert.That(board.AllShipsSunk(), Is.False);
        }

        [Test]
        public void AllShipsSunk_OneShipPartiallyHit_ReturnsFalse()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));
            board.FireAt(new Coordinate(0, 0)); // Hit but not sunk

            Assert.That(board.AllShipsSunk(), Is.False);
        }

        [Test]
        public void AllShipsSunk_AllShipsSunk_ReturnsTrue()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));
            board.FireAt(new Coordinate(0, 0));
            board.FireAt(new Coordinate(1, 0));

            Assert.That(board.AllShipsSunk(), Is.True);
        }

        [Test]
        public void AllShipsSunk_MultipleShipsOnlyOneSunk_ReturnsFalse()
        {
            var board = MakeBoard();
            board.PlaceShip(MakeDestroyer(0, 0));
            board.PlaceShip(MakeCruiser(0, 2));

            // Sink only the destroyer
            board.FireAt(new Coordinate(0, 0));
            board.FireAt(new Coordinate(1, 0));

            Assert.That(board.AllShipsSunk(), Is.False);
        }

        // ── GetCell ───────────────────────────────────────────────────────────────

        [Test]
        public void GetCell_ValidCoordinate_ReturnsCell()
        {
            var board = MakeBoard();
            var cell = board.GetCell(new Coordinate(0, 0));
            Assert.That(cell, Is.Not.Null);
        }

        [Test]
        public void GetCell_AfterFiring_CellIsShot()
        {
            var board = MakeBoard();
            board.FireAt(new Coordinate(3, 3));

            Assert.That(board.GetCell(new Coordinate(3, 3)).IsShot, Is.True);
        }

        [Test]
        public void GetCell_BeforeFiring_CellIsNotShot()
        {
            var board = MakeBoard();
            Assert.That(board.GetCell(new Coordinate(3, 3)).IsShot, Is.False);
        }
    }
}