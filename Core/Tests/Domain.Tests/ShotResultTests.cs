using NUnit.Framework;
using System;
using System.Linq;
using ARBattleship.Core.Domain;


[TestFixture]
public class ShotResultTests
{
    [Test]
    public void ShotResult_ShouldContain_AllExpectedValues()
    {
        var values = Enum.GetValues(typeof(ShotResult)).Cast<ShotResult>().ToList();

            Assert.That(values, Does.Contain(ShotResult.Miss));
            Assert.That(values, Does.Contain(ShotResult.Hit));
            Assert.That(values, Does.Contain(ShotResult.Sunk));
            
            Assert.That(values, Has.Count.EqualTo(3));
    }

    [Test]
    public void ShotResult_UnderlyingValues_ShouldBe_Correct()
    {
            Assert.That((int)ShotResult.Miss, Is.EqualTo(0));
            Assert.That((int)ShotResult.Hit, Is.EqualTo(1));
            Assert.That((int)ShotResult.Sunk, Is.EqualTo(2));
    }

    [Test]
    public void Can_Convert_IntToShotResult()
    {
            Assert.That((ShotResult)0, Is.EqualTo(ShotResult.Miss));
            Assert.That((ShotResult)1, Is.EqualTo(ShotResult.Hit));
            Assert.That((ShotResult)2, Is.EqualTo(ShotResult.Sunk));
    }

    [Test]
    public void Can_Convert_ShotResultToString()
    {
            Assert.That(ShotResult.Miss.ToString(), Is.EqualTo("Miss"));
            Assert.That(ShotResult.Hit.ToString(), Is.EqualTo("Hit"));
            Assert.That(ShotResult.Sunk.ToString(), Is.EqualTo("Sunk"));
    }
}