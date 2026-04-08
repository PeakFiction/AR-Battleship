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

        Assert.AreEqual(a, b);
        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
    }

    [Test]
    public void Coordinates_WithDifferentValues_AreNotEqual()
    {
        var a = new Coordinate(2, 3);
        var b = new Coordinate(3, 2);

        Assert.AreNotEqual(a, b);
        Assert.IsTrue(a != b);
    }

    [Test]
    public void Equals_ObjectOverride_WorksCorrectly()
    {
        object a = new Coordinate(1, 1);
        object b = new Coordinate(1, 1);

        Assert.IsTrue(a.Equals(b));
    }

    [Test]
    public void EqualCoordinates_HaveSameHashCode()
    {
        var a = new Coordinate(5, 5);
        var b = new Coordinate(5, 5);

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void DifferentCoordinates_HaveDifferentHashCodes()
    {
        var a = new Coordinate(1, 2);
        var b = new Coordinate(2, 1);

        Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void Coordinate_CanBeUsedInHashSet()
    {
        var set = new HashSet<Coordinate>();

        set.Add(new Coordinate(4, 4));

        Assert.IsTrue(set.Contains(new Coordinate(4, 4)));
    }

    [Test]
    public void Coordinate_CanBeUsedAsDictionaryKey()
    {
        var dict = new Dictionary<Coordinate, string>();

        dict[new Coordinate(1, 1)] = "hit";

        Assert.IsTrue(dict.ContainsKey(new Coordinate(1, 1)));
        Assert.AreEqual("hit", dict[new Coordinate(1, 1)]);
    }

    [Test]
    public void ToString_ReturnsExpectedFormat()
    {
        var coord = new Coordinate(7, 8);

        Assert.AreEqual("(7, 8)", coord.ToString());
    }
}