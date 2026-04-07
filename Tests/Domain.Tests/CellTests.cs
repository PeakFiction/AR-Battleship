using NUnit.Framework;
using System.Collections.Generic;
using Domain;

namespace Tests.Domain
{
    public class CellTests
    {
        [Test]
        public void PlaceShip_Twice_ThrowsException()
        {
            var cell = new Cell(new Coordinate(0, 0));
            var shipId = new ShipId(Guid.NewGuid());

            cell.PlaceShip(shipId);

            Assert.Throws<InvalidOperationException>(() => 
                cell.PlaceShip(shipId)
            );
        }

        [Test]
        public void Shoot_FirstTime_SetsIsShot()
        {
            var cell = new Cell(new Coordinate(0, 0));

            var result = cell.Shoot();

            Assert.IsTrue(cell.IsShot);
            Assert.IsFalse(result);
        }

        [Test]
        public void Shoot_Twice_ThrowsException()
        {
            var cell = new Cell(new Coordinate(0, 0));

            cell.Shoot();

            Assert.Throws<InvalidOperationException>(() => 
                cell.Shoot()
            );
        }

        [Test]
        public void Shoot_WithShip_ReturnsTrue()
        {
            var cell = new Cell(new Coordinate(0, 0));
            var shipId = new ShipId(Guid.NewGuid());

            cell.PlaceShip(shipId);

            var result = cell.Shoot();

            Assert.IsTrue(result);
        }
    }
}