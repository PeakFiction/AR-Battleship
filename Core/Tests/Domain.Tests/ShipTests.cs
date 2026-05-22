using NUnit.Framework;
using ARBattleship.Core.Domain;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class ShipTests
    {
        private static List<Coordinate> MakePositions(int count, int startX = 0, int startY = 0)
        {
            var positions = new List<Coordinate>();
            for (int i = 0; i < count; i++)
                positions.Add(new Coordinate(startX + i, startY));
            return positions;
        }

        [Test]
        public void CreateCarrier_ValidPositions_ShipTypeIsCarrier()
        {
            var ship = Ship.CreateCarrier(ShipId.New(), MakePositions(5));
            Assert.That(ship.ShipType, Is.EqualTo("Carrier"));
        }

        [Test]
        public void CreateCarrier_ValidPositions_SizeIsFive()
        {
            var ship = Ship.CreateCarrier(ShipId.New(), MakePositions(5));
            Assert.That(ship.Size, Is.EqualTo(5));
        }

        [Test]
        public void CreateBattleship_ValidPositions_ShipTypeIsBattleship()
        {
            var ship = Ship.CreateBattleship(ShipId.New(), MakePositions(4));
            Assert.That(ship.ShipType, Is.EqualTo("Battleship"));
        }

        [Test]
        public void CreateBattleship_ValidPositions_SizeIsFour()
        {
            var ship = Ship.CreateBattleship(ShipId.New(), MakePositions(4));
            Assert.That(ship.Size, Is.EqualTo(4));
        }

        [Test]
        public void CreateCruiser_ValidPositions_ShipTypeIsCruiser()
        {
            var ship = Ship.CreateCruiser(ShipId.New(), MakePositions(3));
            Assert.That(ship.ShipType, Is.EqualTo("Cruiser"));
        }

        [Test]
        public void CreateCruiser_ValidPositions_SizeIsThree()
        {
            var ship = Ship.CreateCruiser(ShipId.New(), MakePositions(3));
            Assert.That(ship.Size, Is.EqualTo(3));
        }

        [Test]
        public void CreateSubmarine_ValidPositions_ShipTypeIsSubmarine()
        {
            var ship = Ship.CreateSubmarine(ShipId.New(), MakePositions(3));
            Assert.That(ship.ShipType, Is.EqualTo("Submarine"));
        }

        [Test]
        public void CreateSubmarine_ValidPositions_SizeIsThree()
        {
            var ship = Ship.CreateSubmarine(ShipId.New(), MakePositions(3));
            Assert.That(ship.Size, Is.EqualTo(3));
        }

        [Test]
        public void CreateDestroyer_ValidPositions_ShipTypeIsDestroyer()
        {
            var ship = Ship.CreateDestroyer(ShipId.New(), MakePositions(2));
            Assert.That(ship.ShipType, Is.EqualTo("Destroyer"));
        }

        [Test]
        public void CreateDestroyer_ValidPositions_SizeIsTwo()
        {
            var ship = Ship.CreateDestroyer(ShipId.New(), MakePositions(2));
            Assert.That(ship.Size, Is.EqualTo(2));
        }

        [Test]
        public void CreateDestroyer_ValidPositions_IdIsSet()
        {
            var id = ShipId.New();
            var ship = Ship.CreateDestroyer(id, MakePositions(2));
            Assert.That(ship.Id, Is.EqualTo(id));
        }

        [Test]
        public void CreateDestroyer_ValidPositions_PositionsAreSet()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            Assert.That(ship.Positions, Is.EquivalentTo(positions));
        }

        [Test]
        public void CreateCarrier_WrongPositionCount_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() =>
                Ship.CreateCarrier(ShipId.New(), MakePositions(4)));
        }

        [Test]
        public void CreateDestroyer_WrongPositionCount_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() =>
                Ship.CreateDestroyer(ShipId.New(), MakePositions(3)));
        }

        [Test]
        public void IsSunk_NewShip_ReturnsFalse()
        {
            var ship = Ship.CreateDestroyer(ShipId.New(), MakePositions(2));
            Assert.That(ship.IsSunk, Is.False);
        }

        [Test]
        public void IsSunk_PartiallyHit_ReturnsFalse()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            ship.RegisterHit(positions[0]);
            Assert.That(ship.IsSunk, Is.False);
        }

        [Test]
        public void IsSunk_AllPositionsHit_ReturnsTrue()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            ship.RegisterHit(positions[0]);
            ship.RegisterHit(positions[1]);
            Assert.That(ship.IsSunk, Is.True);
        }

        [Test]
        public void IsSunk_AllPositionsHitOnCruiser_ReturnsTrue()
        {
            var positions = MakePositions(3);
            var ship = Ship.CreateCruiser(ShipId.New(), positions);
            foreach (var pos in positions)
                ship.RegisterHit(pos);
            Assert.That(ship.IsSunk, Is.True);
        }

        [Test]
        public void RegisterHit_ValidCoordinate_ReturnsTrue()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            Assert.That(ship.RegisterHit(positions[0]), Is.True);
        }

        [Test]
        public void RegisterHit_CoordinateNotOnShip_ReturnsFalse()
        {
            var ship = Ship.CreateDestroyer(ShipId.New(), MakePositions(2, 0, 0));
            Assert.That(ship.RegisterHit(new Coordinate(9, 9)), Is.False);
        }

        [Test]
        public void RegisterHit_SameCoordinateTwice_ReturnsFalseOnSecond()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            ship.RegisterHit(positions[0]);
            Assert.That(ship.RegisterHit(positions[0]), Is.False);
        }

        [Test]
        public void RegisterHit_SameCoordinateTwice_DoesNotSinkShip()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            ship.RegisterHit(positions[0]);
            ship.RegisterHit(positions[0]);
            Assert.That(ship.IsSunk, Is.False);
        }

        [Test]
        public void RegisterHit_AllPositions_ShipIsSunk()
        {
            var positions = MakePositions(2);
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            foreach (var pos in positions)
                ship.RegisterHit(pos);
            Assert.That(ship.IsSunk, Is.True);
        }
    }
}