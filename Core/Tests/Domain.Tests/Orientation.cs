using NUnit.Framework;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class OrientationTests
    {
        // ── GetOffset ─────────────────────────────────────────────────────────────

        [Test]
        public void GetOffset_Horizontal_ReturnsXOffsetOfOne()
        {
            var offset = Orientation.Horizontal.GetOffset();
            Assert.That(offset.X, Is.EqualTo(1));
        }

        [Test]
        public void GetOffset_Horizontal_ReturnsYOffsetOfZero()
        {
            var offset = Orientation.Horizontal.GetOffset();
            Assert.That(offset.Y, Is.EqualTo(0));
        }

        [Test]
        public void GetOffset_Vertical_ReturnsXOffsetOfZero()
        {
            var offset = Orientation.Vertical.GetOffset();
            Assert.That(offset.X, Is.EqualTo(0));
        }

        [Test]
        public void GetOffset_Vertical_ReturnsYOffsetOfOne()
        {
            var offset = Orientation.Vertical.GetOffset();
            Assert.That(offset.Y, Is.EqualTo(1));
        }

        [Test]
        public void GetOffset_Horizontal_OffsetIsCorrectCoordinate()
        {
            var offset = Orientation.Horizontal.GetOffset();
            Assert.That(offset, Is.EqualTo(new Coordinate(1, 0)));
        }

        [Test]
        public void GetOffset_Vertical_OffsetIsCorrectCoordinate()
        {
            var offset = Orientation.Vertical.GetOffset();
            Assert.That(offset, Is.EqualTo(new Coordinate(0, 1)));
        }
    }
}