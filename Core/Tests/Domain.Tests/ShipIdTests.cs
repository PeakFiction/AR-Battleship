using NUnit.Framework;
using ARBattleship.Core.Domain;
using System;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class ShipIdTests
    {
        [Test]
        public void Constructor_ValidGuid_ToStringMatchesGuid()
        {
            var guid = Guid.NewGuid();
            var id = new ShipId(guid);
            Assert.That(id.ToString(), Is.EqualTo(guid.ToString()));
        }

        [Test]
        public void Constructor_EmptyGuid_ToStringMatchesEmptyGuid()
        {
            var id = new ShipId(Guid.Empty);
            Assert.That(id.ToString(), Is.EqualTo(Guid.Empty.ToString()));
        }

        [Test]
        public void New_CalledTwice_ReturnsDifferentIds()
        {
            var a = ShipId.New();
            var b = ShipId.New();
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void New_ReturnsNonEmptyGuid()
        {
            var id = ShipId.New();
            Assert.That(id.ToString(), Is.Not.EqualTo(Guid.Empty.ToString()));
        }

        [Test]
        public void Equals_SameGuid_ReturnsTrue()
        {
            var guid = Guid.NewGuid();
            var a = new ShipId(guid);
            var b = new ShipId(guid);
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void Equals_DifferentGuid_ReturnsFalse()
        {
            var a = ShipId.New();
            var b = ShipId.New();
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_SameInstance_ReturnsTrue()
        {
            var a = ShipId.New();
            Assert.That(a.Equals(a), Is.True);
        }

        [Test]
        public void Equals_Object_SameGuid_ReturnsTrue()
        {
            var guid = Guid.NewGuid();
            var a = new ShipId(guid);
            object b = new ShipId(guid);
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void Equals_Object_DifferentGuid_ReturnsFalse()
        {
            var a = ShipId.New();
            object b = ShipId.New();
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_Object_Null_ReturnsFalse()
        {
            var a = ShipId.New();
            Assert.That(a.Equals(null), Is.False);
        }

        [Test]
        public void Equals_Object_DifferentType_ReturnsFalse()
        {
            var a = ShipId.New();
            Assert.That(a.Equals("not a ship id"), Is.False);
        }

        [Test]
        public void GetHashCode_SameGuid_ReturnsSameHash()
        {
            var guid = Guid.NewGuid();
            var a = new ShipId(guid);
            var b = new ShipId(guid);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void GetHashCode_DifferentGuids_ReturnsDifferentHash()
        {
            var a = ShipId.New();
            var b = ShipId.New();
            Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsGuidString()
        {
            var guid = Guid.NewGuid();
            var id = new ShipId(guid);
            Assert.That(id.ToString(), Is.EqualTo(guid.ToString()));
        }
    }
}