using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class CoordinateTests
{
    [Test]
    public void Coordinates_WithSameValues_AreEqual()
    {
        var a = new Coordinate(2, 3);
        var b = new Coordinate(2, 3);

        Assert.That(b, Is.EqualTo(a));
        Assert.That(a.Equals(b), Is.True);
        Assert.That(a == b, Is.True);
    }

    [Test]
    public void Coordinates_WithDifferentValues_AreNotEqual()
    {
        var a = new Coordinate(2, 3);
        var b = new Coordinate(3, 2);

        Assert.That(b, Is.Not.EqualTo(a));
        Assert.That(a != b, Is.True);
    }

    [Test]
    public void Equals_ObjectOverride_WorksCorrectly()
    {
        object a = new Coordinate(1, 1);
        object b = new Coordinate(1, 1);

        Assert.That(a.Equals(b), Is.True);
    }

    [Test]
    public void EqualCoordinates_HaveSameHashCode()
    {
        var a = new Coordinate(5, 5);
        var b = new Coordinate(5, 5);

        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
    }

    [Test]
    public void DifferentCoordinates_HaveDifferentHashCodes()
    {
        var a = new Coordinate(1, 2);
        var b = new Coordinate(2, 1);

        Assert.That(b.GetHashCode(), Is.Not.EqualTo(a.GetHashCode()));
    }

    [Test]
    public void Coordinate_CanBeUsedInHashSet()
    {
        var set = new HashSet<Coordinate>();
        var coord = new Coordinate(4, 4);

        set.Add(coord);

        Assert.That(set, Does.Contain(new Coordinate(4, 4)));
    }

    [Test]
    public void Coordinate_CanBeUsedAsDictionaryKey()
    {
        var dict = new Dictionary<Coordinate, string>();
        var coord = new Coordinate(1, 1);

        dict[coord] = "hit";

        Assert.Multiple(() =>
        {
            Assert.That(dict.ContainsKey(new Coordinate(1, 1)), Is.True);
            Assert.That(dict[new Coordinate(1, 1)], Is.EqualTo("hit"));
        });
    }

    [Test]
    public void ToString_ReturnsExpectedFormat()
    {
        var coord = new Coordinate(7, 8);

        Assert.That(coord.ToString(), Is.EqualTo("(7, 8)"));
    }
}