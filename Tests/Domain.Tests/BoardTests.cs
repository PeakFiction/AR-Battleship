using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class BoardTests
{
    private Board _board;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(5);
    }

    [Test]
    public void Constructor_Should_Initialize_AllCells()
    {
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var coord = new Coordinate(x, y);
                Assert.That(() => _board.FireAt(coord), Throws.Nothing);
            }
        }
    }

    [Test]
    public void CanPlaceShip_Should_ReturnTrue_For_ValidPositions()
    {
        var ship = new Ship(ShipId.New(), new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(0, 1)
        });

        Assert.That(_board.CanPlaceShip(ship.Positions), Is.True);
    }

    [Test]
    public void CanPlaceShip_Should_ReturnFalse_For_OverlappingShip()
    {
        var ship1 = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(0, 0) });
        var ship2 = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(0, 0) });

        _board.PlaceShip(ship1);

        Assert.That(_board.CanPlaceShip(ship2.Positions), Is.False);
    }

    [Test]
    public void CanPlaceShip_Should_ReturnFalse_For_OutOfBounds()
    {
        var ship = new Ship(ShipId.New(), new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(5, 5)
        });

        Assert.That(_board.CanPlaceShip(ship.Positions), Is.False);
    }

    [Test]
    public void PlaceShip_Should_Succeed_For_ValidShip()
    {
        var ship = new Ship(ShipId.New(), new List<Coordinate>
        {
            new Coordinate(1, 1),
            new Coordinate(1, 2)
        });

        var result = _board.PlaceShip(ship);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void PlaceShip_Should_Fail_For_OverlappingShip()
    {
        var ship1 = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(0, 0) });
        var ship2 = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(0, 0) });

        _board.PlaceShip(ship1);
        var result = _board.PlaceShip(ship2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Invalid ship placement (out of bounds or overlapping)."));
        });
    }

    [Test]
    public void FireAt_Should_ReturnMiss_When_NoShip()
    {
        var result = _board.FireAt(new Coordinate(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Result, Is.EqualTo(ShotResult.Miss));
        });
    }

    [Test]
    public void FireAt_Should_ReturnHit_When_ShipIsHit()
    {
        var ship = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(1, 1) });
        _board.PlaceShip(ship);

        var result = _board.FireAt(new Coordinate(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Result, Is.EqualTo(ShotResult.Hit));
        });
    }

    [Test]
    public void FireAt_Should_ReturnSunk_When_ShipIsFullyHit()
    {
        var ship = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(2, 2) });
        _board.PlaceShip(ship);

        var result = _board.FireAt(new Coordinate(2, 2));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Result, Is.EqualTo(ShotResult.Sunk));
            Assert.That(ship.IsSunk, Is.True);
        });
    }

    [Test]
    public void FireAt_Should_Fail_For_OutOfBounds()
    {
        var result = _board.FireAt(new Coordinate(10, 10));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Coordinate out of bounds."));
        });
    }

    [Test]
    public void AllShipsSunk_Should_ReturnTrue_When_AllShipsAreSunk()
    {
        var ship = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(0, 0) });
        _board.PlaceShip(ship);

        _board.FireAt(new Coordinate(0, 0));

        Assert.That(_board.AllShipsSunk(), Is.True);
    }

    [Test]
    public void AllShipsSunk_Should_ReturnFalse_When_AnyShipIsNotSunk()
    {
        var ship1 = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(0, 0) });
        var ship2 = new Ship(ShipId.New(), new List<Coordinate> { new Coordinate(1, 1) });

        _board.PlaceShip(ship1);
        _board.PlaceShip(ship2);

        _board.FireAt(new Coordinate(0, 0));

        Assert.That(_board.AllShipsSunk(), Is.False);
    }
}