using NUnit.Framework;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class FireResultTests
    {
        private static readonly Coordinate TestCoord = new Coordinate(3, 5);
        private static ShipId MakeShipId() => ShipId.New();

        [Test]
        public void Miss_ValidCoordinate_OutcomeIsMiss()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.Outcome, Is.EqualTo(ShotResult.Miss));
        }

        [Test]
        public void Miss_ValidCoordinate_CoordinateIsSet()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.Coordinate, Is.EqualTo(TestCoord));
        }

        [Test]
        public void Miss_ValidCoordinate_ShipIdIsNull()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.ShipId, Is.Null);
        }

        [Test]
        public void Miss_ValidCoordinate_ShipNameIsNull()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.ShipType, Is.Null);
        }

        [Test]
        public void Miss_ValidCoordinate_IsHitIsFalse()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.IsHit, Is.False);
        }

        [Test]
        public void Miss_ValidCoordinate_IsSunkIsFalse()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.IsSunk, Is.False);
        }

        [Test]
        public void Hit_ValidArgs_OutcomeIsHit()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Destroyer", null, null);
            Assert.That(result.Outcome, Is.EqualTo(ShotResult.Hit));
        }

        [Test]
        public void Hit_ValidArgs_CoordinateIsSet()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Destroyer", null, null);
            Assert.That(result.Coordinate, Is.EqualTo(TestCoord));
        }

        [Test]
        public void Hit_ValidArgs_ShipIdIsSet()
        {
            var id = MakeShipId();
            var result = FireResult.Hit(TestCoord, id, "Destroyer", null, null);
            Assert.That(result.ShipId, Is.EqualTo(id));
        }

        [Test]
        public void Hit_ValidArgs_ShipNameIsSet()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Destroyer", null, null);
            Assert.That(result.ShipType, Is.EqualTo("Destroyer"));
        }

        [Test]
        public void Hit_ValidArgs_IsHitIsTrue()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Destroyer", null, null);
            Assert.That(result.IsHit, Is.True);
        }

        [Test]
        public void Hit_ValidArgs_IsSunkIsFalse()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Destroyer", null, null);
            Assert.That(result.IsSunk, Is.False);
        }

        [Test]
        public void Sunk_ValidArgs_OutcomeIsSunk()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Carrier", null, null);
            Assert.That(result.Outcome, Is.EqualTo(ShotResult.Sunk));
        }

        [Test]
        public void Sunk_ValidArgs_CoordinateIsSet()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Carrier", null, null);
            Assert.That(result.Coordinate, Is.EqualTo(TestCoord));
        }

        [Test]
        public void Sunk_ValidArgs_ShipIdIsSet()
        {
            var id = MakeShipId();
            var result = FireResult.Sunk(TestCoord, id, "Carrier", null, null);
            Assert.That(result.ShipId, Is.EqualTo(id));
        }

        [Test]
        public void Sunk_ValidArgs_ShipNameIsSet()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Carrier", null, null);
            Assert.That(result.ShipType, Is.EqualTo("Carrier"));
        }

        [Test]
        public void Sunk_ValidArgs_IsHitIsTrue()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Carrier", null, null);
            Assert.That(result.IsHit, Is.True);
        }

        [Test]
        public void Sunk_ValidArgs_IsSunkIsTrue()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Carrier", null, null);
            Assert.That(result.IsSunk, Is.True);
        }

        [Test]
        public void IsHit_OutcomeIsMiss_ReturnsFalse()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.IsHit, Is.False);
        }

        [Test]
        public void IsHit_OutcomeIsHit_ReturnsTrue()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Battleship", null, null);
            Assert.That(result.IsHit, Is.True);
        }

        [Test]
        public void IsHit_OutcomeIsSunk_ReturnsTrue()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Battleship", null, null);
            Assert.That(result.IsHit, Is.True);
        }

        [Test]
        public void IsSunk_OutcomeIsMiss_ReturnsFalse()
        {
            var result = FireResult.Miss(TestCoord);
            Assert.That(result.IsSunk, Is.False);
        }

        [Test]
        public void IsSunk_OutcomeIsHit_ReturnsFalse()
        {
            var result = FireResult.Hit(TestCoord, MakeShipId(), "Submarine", null, null);
            Assert.That(result.IsSunk, Is.False);
        }

        [Test]
        public void IsSunk_OutcomeIsSunk_ReturnsTrue()
        {
            var result = FireResult.Sunk(TestCoord, MakeShipId(), "Submarine", null, null);
            Assert.That(result.IsSunk, Is.True);
        }
    }
}