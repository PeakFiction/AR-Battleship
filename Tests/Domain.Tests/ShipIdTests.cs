using NUnit.Framework;
using System;
using System.Collections.Generic;

[TestFixture]
public class ShipIdTests
{
    [Test]
    public void New_Should_Create_Unique_Ids()
    {
        var id1 = ShipId.New();
        var id2 = ShipId.New();

        Assert.That(id2, Is.Not.EqualTo(id1));
        Assert.That(id1.Equals(id2), Is.False);
    }

    [Test]
    public void Equals_Should_ReturnTrue_For_SameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = new ShipId(guid);
        var id2 = new ShipId(guid);

        Assert.Multiple(() =>
        {
            Assert.That(id1.Equals(id2), Is.True);
            Assert.That(id1.Equals((object)id2), Is.True);
            Assert.That(id2, Is.EqualTo(id1));
        });
    }

    [Test]
    public void Equals_Should_ReturnFalse_For_DifferentValues()
    {
        var id1 = new ShipId(Guid.NewGuid());
        var id2 = new ShipId(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(id1.Equals(id2), Is.False);
            Assert.That(id1.Equals((object)id2), Is.False);
            Assert.That(id2, Is.Not.EqualTo(id1));
        });
    }

    [Test]
    public void GetHashCode_Should_BeEqual_For_SameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = new ShipId(guid);
        var id2 = new ShipId(guid);

        Assert.That(id2.GetHashCode(), Is.EqualTo(id1.GetHashCode()));
    }

    [Test]
    public void GetHashCode_Should_Differ_For_DifferentValues()
    {
        var id1 = new ShipId(Guid.NewGuid());
        var id2 = new ShipId(Guid.NewGuid());

        Assert.That(id2.GetHashCode(), Is.Not.EqualTo(id1.GetHashCode()));
    }

    [Test]
    public void ToString_Should_Return_GuidString()
    {
        var guid = Guid.NewGuid();
        var id = new ShipId(guid);

        Assert.That(id.ToString(), Is.EqualTo(guid.ToString()));
    }

    [Test]
    public void Can_Be_Used_As_DictionaryKey()
    {
        var id1 = ShipId.New();
        var id2 = ShipId.New();
        var dict = new Dictionary<ShipId, string>();

        dict[id1] = "Ship A";
        dict[id2] = "Ship B";

        Assert.Multiple(() =>
        {
            Assert.That(dict[id1], Is.EqualTo("Ship A"));
            Assert.That(dict[id2], Is.EqualTo("Ship B"));
        });
    }
}