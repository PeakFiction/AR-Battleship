using NUnit.Framework;
using System.Collections.Generic;

public class ShipTests
{
    private Ship CreateShip()
    {
        return new Ship(
            ShipId.New(),
            new List<Coordinate>
            {
                new Coordinate(0,0),
                new Coordinate(0,1)
            }
        );
    }

    [Test]
    public void RegisterHit_ValidHit_ReturnsTrue()
    {
        var ship = CreateShip();

        var result = ship.RegisterHit(new Coordinate(0,0));

        Assert.IsTrue(result);
    }

    [Test]
    public void RegisterHit_InvalidHit_ReturnsFalse()
    {
        var ship = CreateShip();

        var result = ship.RegisterHit(new Coordinate(5,5));

        Assert.IsFalse(result);
    }

    [Test]
    public void RegisterHit_DuplicateHit_ReturnsFalse()
    {
        var ship = CreateShip();

        ship.RegisterHit(new Coordinate(0,0));
        var result = ship.RegisterHit(new Coordinate(0,0));

        Assert.IsFalse(result);
    }

    [Test]
    public void IsSunk_WhenAllPositionsHit_ReturnsTrue()
    {
        var ship = CreateShip();

        ship.RegisterHit(new Coordinate(0,0));
        ship.RegisterHit(new Coordinate(0,1));

        Assert.IsTrue(ship.IsSunk);
    }

    [Test]
    public void IsSunk_WhenNotAllHit_ReturnsFalse()
    {
        var ship = CreateShip();

        ship.RegisterHit(new Coordinate(0,0));

        Assert.IsFalse(ship.IsSunk);
    }

    [Test]
    public void Positions_CannotBeExternallyModified()
    {
        var ship = CreateShip();

        Assert.Throws<System.NotSupportedException>(() =>
        {
            ((List<Coordinate>)ship.Positions).Add(new Coordinate(9,9));
        });
    }
}