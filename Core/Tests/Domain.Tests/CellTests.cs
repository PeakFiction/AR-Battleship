// =============================================================================
// CellTests.cs  |  ARBattleship.Core.Tests.Domain
// =============================================================================
// NUnit unit tests for the Domain layer: Cell.
// Tests verify domain invariants, factory constraints, and edge cases.
// All tests are pure C# — no Unity dependency required.
// =============================================================================
using NUnit.Framework;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class CellTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Cell MakeCell(int x = 0, int y = 0) =>
            new Cell(new Coordinate(x, y));

        private static ShipId MakeShipId() => ShipId.New();

        // ── Constructor ───────────────────────────────────────────────────────────

        [Test]
        public void Constructor_NewCell_CoordinateIsSet()
        {
            var coord = new Coordinate(3, 5);
            var cell = new Cell(coord);
            Assert.That(cell.Coordinate, Is.EqualTo(coord));
        }

        [Test]
        public void Constructor_NewCell_HasShipIsFalse()
        {
            var cell = MakeCell();
            Assert.That(cell.HasShip, Is.False);
        }

        [Test]
        public void Constructor_NewCell_IsShotIsFalse()
        {
            var cell = MakeCell();
            Assert.That(cell.IsShot, Is.False);
        }

        [Test]
        public void Constructor_NewCell_ShipIdIsNull()
        {
            var cell = MakeCell();
            Assert.That(cell.ShipId, Is.Null);
        }

        // ── PlaceShip ─────────────────────────────────────────────────────────────

        [Test]
        public void PlaceShip_EmptyCell_ReturnsSuccess()
        {
            var cell = MakeCell();
            var result = cell.PlaceShip(MakeShipId());
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void PlaceShip_EmptyCell_HasShipBecomesTrue()
        {
            var cell = MakeCell();
            cell.PlaceShip(MakeShipId());
            Assert.That(cell.HasShip, Is.True);
        }

        [Test]
        public void PlaceShip_EmptyCell_ShipIdIsSet()
        {
            var cell = MakeCell();
            var id = MakeShipId();
            cell.PlaceShip(id);
            Assert.That(cell.ShipId, Is.EqualTo(id));
        }

        [Test]
        public void PlaceShip_AlreadyOccupied_ReturnsFailure()
        {
            var cell = MakeCell();
            cell.PlaceShip(MakeShipId());

            var result = cell.PlaceShip(MakeShipId());

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_AlreadyOccupied_ShipIdUnchanged()
        {
            var cell = MakeCell();
            var originalId = MakeShipId();
            cell.PlaceShip(originalId);

            cell.PlaceShip(MakeShipId());

            Assert.That(cell.ShipId, Is.EqualTo(originalId));
        }

        // ── Shoot ─────────────────────────────────────────────────────────────────

        [Test]
        public void Shoot_EmptyCell_ReturnsSuccess()
        {
            var cell = MakeCell();
            var result = cell.Shoot();
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Shoot_EmptyCell_ReturnsMiss()
        {
            var cell = MakeCell();
            var result = cell.Shoot();
            Assert.That(result.Value, Is.EqualTo(ShotResult.Miss));
        }

        [Test]
        public void Shoot_CellWithShip_ReturnsSuccess()
        {
            var cell = MakeCell();
            cell.PlaceShip(MakeShipId());

            var result = cell.Shoot();

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Shoot_CellWithShip_ReturnsHit()
        {
            var cell = MakeCell();
            cell.PlaceShip(MakeShipId());

            var result = cell.Shoot();

            Assert.That(result.Value, Is.EqualTo(ShotResult.Hit));
        }

        [Test]
        public void Shoot_AfterShooting_IsShotIsTrue()
        {
            var cell = MakeCell();
            cell.Shoot();
            Assert.That(cell.IsShot, Is.True);
        }

        [Test]
        public void Shoot_AlreadyShot_ReturnsFailure()
        {
            var cell = MakeCell();
            cell.Shoot();

            var result = cell.Shoot();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Shoot_AlreadyShotCellWithShip_ReturnsFailure()
        {
            var cell = MakeCell();
            cell.PlaceShip(MakeShipId());
            cell.Shoot();

            var result = cell.Shoot();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Shoot_AfterShooting_HasShipUnchanged()
        {
            var cell = MakeCell();
            cell.PlaceShip(MakeShipId());
            cell.Shoot();

            Assert.That(cell.HasShip, Is.True);
        }
    }
}