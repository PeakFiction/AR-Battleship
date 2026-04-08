using NUnit.Framework;
using System;

[TestFixture]
public class FireResultTests
{
    [Test]
    public void Miss_Should_Create_FireResult_WithCorrectProperties()
    {
        var coord = new Coordinate(0, 0);
        var result = FireResult.Miss(coord);

        Assert.Multiple(() =>
        {
            Assert.That(result.Coordinate, Is.EqualTo(coord));
            Assert.That(result.Result, Is.EqualTo(ShotResult.Miss));
            Assert.That(result.ShipId, Is.Null);
            Assert.That(result.IsHit, Is.False);
            Assert.That(result.IsSunk, Is.False);
            Assert.That(result.Message, Is.EqualTo("Miss"));
        });
    }

    [Test]
    public void Hit_Should_Create_FireResult_WithCorrectProperties()
    {
        var coord = new Coordinate(1, 1);
        var shipId = ShipId.New();
        var result = FireResult.Hit(coord, shipId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Coordinate, Is.EqualTo(coord));
            Assert.That(result.Result, Is.EqualTo(ShotResult.Hit));
            Assert.That(result.ShipId, Is.EqualTo(shipId));
            Assert.That(result.IsHit, Is.True);
            Assert.That(result.IsSunk, Is.False);
            Assert.That(result.Message, Is.EqualTo("Hit"));
        });
    }

    [Test]
    public void Sunk_Should_Create_FireResult_WithCorrectProperties()
    {
        var coord = new Coordinate(2, 2);
        var shipId = ShipId.New();
        var result = FireResult.Sunk(coord, shipId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Coordinate, Is.EqualTo(coord));
            Assert.That(result.Result, Is.EqualTo(ShotResult.Sunk));
            Assert.That(result.ShipId, Is.EqualTo(shipId));
            Assert.That(result.IsHit, Is.True);
            Assert.That(result.IsSunk, Is.True);
            Assert.That(result.Message, Is.EqualTo("Ship sunk!"));
        });
    }

    [Test]
    public void Message_Should_Throw_For_InvalidResult()
    {
        var fireResult = new FireResult(new Coordinate(0, 0), (ShotResult)99, null);

        Assert.That(() => { var _ = fireResult.Message; }, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void IsHit_Should_BeTrue_For_HitOrSunk()
    {
        var hitResult = FireResult.Hit(new Coordinate(0, 0), ShipId.New());
        var sunkResult = FireResult.Sunk(new Coordinate(1, 1), ShipId.New());
        var missResult = FireResult.Miss(new Coordinate(2, 2));

        Assert.Multiple(() =>
        {
            Assert.That(hitResult.IsHit, Is.True);
            Assert.That(sunkResult.IsHit, Is.True);
            Assert.That(missResult.IsHit, Is.False);
        });
    }

    [Test]
    public void IsSunk_Should_BeTrue_OnlyForSunk()
    {
        var hitResult = FireResult.Hit(new Coordinate(0, 0), ShipId.New());
        var sunkResult = FireResult.Sunk(new Coordinate(1, 1), ShipId.New());
        var missResult = FireResult.Miss(new Coordinate(2, 2));

        Assert.Multiple(() =>
        {
            Assert.That(hitResult.IsSunk, Is.False);
            Assert.That(sunkResult.IsSunk, Is.True);
            Assert.That(missResult.IsSunk, Is.False);
        });
    }
}