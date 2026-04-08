using NUnit.Framework;

[TestFixture]
public class CellTests
{
    [Test]
    public void Constructor_Should_Set_Coordinate_And_InitialState()
    {
        var coordinate = new Coordinate(1, 2);
        var cell = new Cell(coordinate);

        Assert.Multiple(() =>
        {
            Assert.That(cell.Coordinate, Is.EqualTo(coordinate));
            Assert.That(cell.HasShip, Is.False);
            Assert.That(cell.IsShot, Is.False);
            Assert.That(cell.ShipId, Is.Null);
        });
    }

    [Test]
    public void PlaceShip_Should_Succeed_When_CellIsEmpty()
    {
        var cell = new Cell(new Coordinate(0, 0));
        var shipId = new ShipId(42);

        var result = cell.PlaceShip(shipId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(cell.HasShip, Is.True);
            Assert.That(cell.ShipId, Is.EqualTo(shipId));
        });
    }

    [Test]
    public void PlaceShip_Should_Fail_When_CellAlreadyHasShip()
    {
        var cell = new Cell(new Coordinate(0, 0));
        var shipId1 = new ShipId(1);
        var shipId2 = new ShipId(2);

        cell.PlaceShip(shipId1);
        var result = cell.PlaceShip(shipId2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo("Cell already has a ship."));
            Assert.That(cell.ShipId, Is.EqualTo(shipId1));
        });
    }

    [Test]
    public void Shoot_Should_Return_Miss_When_CellHasNoShip()
    {
        var cell = new Cell(new Coordinate(0, 0));

        var result = cell.Shoot();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(ShotResult.Miss));
            Assert.That(cell.IsShot, Is.True);
        });
    }

    [Test]
    public void Shoot_Should_Return_Hit_When_CellHasShip()
    {
        var cell = new Cell(new Coordinate(0, 0));
        var shipId = new ShipId(1);
        cell.PlaceShip(shipId);

        var result = cell.Shoot();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(ShotResult.Hit));
            Assert.That(cell.IsShot, Is.True);
        });
    }

    [Test]
    public void Shoot_Should_Fail_When_CellAlreadyShot()
    {
        var cell = new Cell(new Coordinate(0, 0));
        cell.Shoot();

        var result = cell.Shoot();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo("Cell already shot."));
        });
    }
}