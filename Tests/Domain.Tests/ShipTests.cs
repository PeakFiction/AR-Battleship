using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
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

        var result = ship.RegisterHit(new Coordinate(0, 0));

        Assert.That(result, Is.True);
    }

    [Test]
    public void RegisterHit_InvalidHit_ReturnsFalse()
    {
        var ship = CreateShip();

        var result = ship.RegisterHit(new Coordinate(5, 5));

        Assert.That(result, Is.False);
    }

    [Test]
    public void RegisterHit_DuplicateHit_ReturnsFalse()
    {
        var ship = CreateShip();

        ship.RegisterHit(new Coordinate(0, 0));
        var result = ship.RegisterHit(new Coordinate(0, 0));

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSunk_WhenAllPositionsHit_ReturnsTrue()
    {
        var ship = CreateShip();

        ship.RegisterHit(new Coordinate(0, 0));
        ship.RegisterHit(new Coordinate(0, 1));

        Assert.That(ship.IsSunk, Is.True);
    }

    [Test]
    public void IsSunk_WhenNotAllHit_ReturnsFalse()
    {
        var ship = CreateShip();

        ship.RegisterHit(new Coordinate(0, 0));

        Assert.That(ship.IsSunk, Is.False);
    }

    [Test]
    public void Positions_CannotBeExternallyModified()
    {
        var ship = CreateShip();

        Assert.That(() =>
        {
            ((List<Coordinate>)ship.Positions).Add(new Coordinate(9, 9));
        }, Throws.TypeOf<System.NotSupportedException>());
    }
}