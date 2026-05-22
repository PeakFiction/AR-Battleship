using NUnit.Framework;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class CoordinateTests
    {
        [Test]
        public void Constructor_ValidValues_XIsSet()
        {
            var coord = new Coordinate(3, 5);
            Assert.That(coord.X, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_ValidValues_YIsSet()
        {
            var coord = new Coordinate(3, 5);
            Assert.That(coord.Y, Is.EqualTo(5));
        }

        [Test]
        public void Constructor_ZeroValues_XAndYAreZero()
        {
            var coord = new Coordinate(0, 0);
            Assert.That(coord.X, Is.EqualTo(0));
            Assert.That(coord.Y, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_NegativeValues_XAndYAreSet()
        {
            var coord = new Coordinate(-1, -5);
            Assert.That(coord.X, Is.EqualTo(-1));
            Assert.That(coord.Y, Is.EqualTo(-5));
        }

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(3, 5);
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void Equals_DifferentX_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(4, 5);
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_DifferentY_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(3, 6);
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_BothDifferent_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(1, 2);
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_SameInstance_ReturnsTrue()
        {
            var a = new Coordinate(3, 5);
            Assert.That(a.Equals(a), Is.True);
        }

        [Test]
        public void Equals_Object_SameValues_ReturnsTrue()
        {
            var a = new Coordinate(3, 5);
            object b = new Coordinate(3, 5);
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void Equals_Object_DifferentValues_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            object b = new Coordinate(1, 2);
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_Object_Null_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            Assert.That(a.Equals(null), Is.False);
        }

        [Test]
        public void Equals_Object_DifferentType_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            Assert.That(a.Equals("not a coordinate"), Is.False);
        }

        [Test]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(3, 5);
            Assert.That(a == b, Is.True);
        }

        [Test]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(1, 2);
            Assert.That(a == b, Is.False);
        }

        [Test]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(1, 2);
            Assert.That(a != b, Is.True);
        }

        [Test]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(3, 5);
            Assert.That(a != b, Is.False);
        }

        [Test]
        public void GetHashCode_SameValues_ReturnsSameHash()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(3, 5);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void GetHashCode_DifferentValues_ReturnsDifferentHash()
        {
            var a = new Coordinate(3, 5);
            var b = new Coordinate(5, 3);
            Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_ValidCoordinate_ReturnsFormattedString()
        {
            var coord = new Coordinate(3, 5);
            Assert.That(coord.ToString(), Is.EqualTo("(3, 5)"));
        }

        [Test]
        public void ToString_ZeroCoordinate_ReturnsFormattedString()
        {
            var coord = new Coordinate(0, 0);
            Assert.That(coord.ToString(), Is.EqualTo("(0, 0)"));
        }

        [Test]
        public void ToString_NegativeCoordinate_ReturnsFormattedString()
        {
            var coord = new Coordinate(-1, -5);
            Assert.That(coord.ToString(), Is.EqualTo("(-1, -5)"));
        }
    }
}