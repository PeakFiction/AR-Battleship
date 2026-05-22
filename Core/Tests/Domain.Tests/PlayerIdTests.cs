using NUnit.Framework;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class PlayerIdTests
    {
        [Test]
        public void PlayerOne_Value_IsPlayer1()
        {
            Assert.That(PlayerId.PlayerOne.Value, Is.EqualTo("Player1"));
        }

        [Test]
        public void PlayerTwo_Value_IsPlayer2()
        {
            Assert.That(PlayerId.PlayerTwo.Value, Is.EqualTo("Player2"));
        }

        [Test]
        public void PlayerOne_And_PlayerTwo_HaveDifferentValues()
        {
            Assert.That(PlayerId.PlayerOne.Value, Is.Not.EqualTo(PlayerId.PlayerTwo.Value));
        }

        [Test]
        public void PlayerOne_NotEqualToPlayerTwo_ReturnsTrue()
        {
            Assert.That(PlayerId.PlayerOne, Is.Not.EqualTo(PlayerId.PlayerTwo));
        }

        [Test]
        public void EqualityOperator_TwoPlayerOnes_ReturnsTrue()
        {
            var a = PlayerId.PlayerOne;
            var b = PlayerId.PlayerOne;
            Assert.That(a == b, Is.True);
        }

        [Test]
        public void EqualityOperator_PlayerOneAndPlayerTwo_ReturnsFalse()
        {
            Assert.That(PlayerId.PlayerOne == PlayerId.PlayerTwo, Is.False);
        }

        [Test]
        public void InequalityOperator_PlayerOneAndPlayerTwo_ReturnsTrue()
        {
            Assert.That(PlayerId.PlayerOne != PlayerId.PlayerTwo, Is.True);
        }

        [Test]
        public void InequalityOperator_TwoPlayerOnes_ReturnsFalse()
        {
            var a = PlayerId.PlayerOne;
            var b = PlayerId.PlayerOne;
            Assert.That(a != b, Is.False);
        }

        [Test]
        public void IsOpponentOf_PlayerOneVsPlayerTwo_ReturnsTrue()
        {
            Assert.That(PlayerId.PlayerOne.IsOpponentOf(PlayerId.PlayerTwo), Is.True);
        }

        [Test]
        public void IsOpponentOf_PlayerTwoVsPlayerOne_ReturnsTrue()
        {
            Assert.That(PlayerId.PlayerTwo.IsOpponentOf(PlayerId.PlayerOne), Is.True);
        }

        [Test]
        public void IsOpponentOf_PlayerOneVsPlayerOne_ReturnsFalse()
        {
            Assert.That(PlayerId.PlayerOne.IsOpponentOf(PlayerId.PlayerOne), Is.False);
        }

        [Test]
        public void IsOpponentOf_PlayerTwoVsPlayerTwo_ReturnsFalse()
        {
            Assert.That(PlayerId.PlayerTwo.IsOpponentOf(PlayerId.PlayerTwo), Is.False);
        }

        [Test]
        public void ToString_PlayerOne_ReturnsPlayer1()
        {
            Assert.That(PlayerId.PlayerOne.ToString(), Is.EqualTo("Player1"));
        }

        [Test]
        public void ToString_PlayerTwo_ReturnsPlayer2()
        {
            Assert.That(PlayerId.PlayerTwo.ToString(), Is.EqualTo("Player2"));
        }
    }
}