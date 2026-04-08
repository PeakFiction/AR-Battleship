using NUnit.Framework;

[TestFixture]
public class CellTests
{
    [Test]
    public void Constructor_Should_Set_Coordinate_And_InitialState()
    {
        var coordinate = new Coordinate(1, 2);
        var cell = new Cell(coordinate);

        Assert.AreEqual(coordinate, cell.Coordinate);
        Assert.IsFalse(cell.HasShip);
        Assert.IsFalse(cell.IsShot);
        Assert.IsNull(cell.ShipId);
    }

    [Test]
    public void PlaceShip_Should_Succeed_When_CellIsEmpty()
    {
        var cell = new Cell(new Coordinate(0, 0));
        var shipId = new ShipId(42);

        var result = cell.PlaceShip(shipId);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(cell.HasShip);
        Assert.AreEqual(shipId, cell.ShipId);
    }

    [Test]
    public void PlaceShip_Should_Fail_When_CellAlreadyHasShip()
    {
        var cell = new Cell(new Coordinate(0, 0));
        var shipId1 = new ShipId(1);
        var shipId2 = new ShipId(2);

        cell.PlaceShip(shipId1);
        var result = cell.PlaceShip(shipId2);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("Cell already has a ship.", result.Error);
        Assert.AreEqual(shipId1, cell.ShipId); // original ship remains
    }

    [Test]
    public void Shoot_Should_Return_Miss_When_CellHasNoShip()
    {
        var cell = new Cell(new Coordinate(0, 0));

        var result = cell.Shoot();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(ShotResult.Miss, result.Value);
        Assert.IsTrue(cell.IsShot);
    }

    [Test]
    public void Shoot_Should_Return_Hit_When_CellHasShip()
    {
        var cell = new Cell(new Coordinate(0, 0));
        var shipId = new ShipId(1);
        cell.PlaceShip(shipId);

        var result = cell.Shoot();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(ShotResult.Hit, result.Value);
        Assert.IsTrue(cell.IsShot);
    }

    [Test]
    public void Shoot_Should_Fail_When_CellAlreadyShot()
    {
        var cell = new Cell(new Coordinate(0, 0));
        cell.Shoot(); // first shot

        var result = cell.Shoot(); // second shot

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("Cell already shot.", result.Error);
    }
}