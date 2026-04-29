using NUnit.Framework;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class ResultTests
    {
        // ── Success ───────────────────────────────────────────────────────────────

        [Test]
        public void Success_WithValue_IsSuccessIsTrue()
        {
            var result = Result<int>.Success(42);
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Success_WithValue_ValueIsSet()
        {
            var result = Result<int>.Success(42);
            Assert.That(result.Value, Is.EqualTo(42));
        }

        [Test]
        public void Success_WithValue_ErrorIsNull()
        {
            var result = Result<int>.Success(42);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void Success_WithStringValue_ValueIsSet()
        {
            var result = Result<string>.Success("hello");
            Assert.That(result.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void Success_WithBoolValue_ValueIsTrue()
        {
            var result = Result<bool>.Success(true);
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void Success_WithNullReferenceValue_IsSuccessIsTrue()
        {
            var result = Result<string>.Success(null!);
            Assert.That(result.IsSuccess, Is.True);
        }

        // ── Failure ───────────────────────────────────────────────────────────────

        [Test]
        public void Failure_WithMessage_IsSuccessIsFalse()
        {
            var result = Result<int>.Failure("something went wrong");
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Failure_WithMessage_ErrorIsSet()
        {
            var result = Result<int>.Failure("something went wrong");
            Assert.That(result.Error, Is.EqualTo("something went wrong"));
        }

        [Test]
        public void Failure_WithMessage_ValueIsDefault()
        {
            var result = Result<int>.Failure("something went wrong");
            Assert.That(result.Value, Is.EqualTo(default(int)));
        }

        [Test]
        public void Failure_WithBoolType_ValueIsDefault()
        {
            var result = Result<bool>.Failure("error");
            Assert.That(result.Value, Is.False);
        }

        [Test]
        public void Failure_WithReferenceType_ValueIsNull()
        {
            var result = Result<string>.Failure("error");
            Assert.That(result.Value, Is.Null);
        }

        // ── Type coverage ─────────────────────────────────────────────────────────

        [Test]
        public void Success_WithFireResult_ValueIsSet()
        {
            var coord = new Coordinate(3, 5);
            var fireResult = FireResult.Miss(coord);

            var result = Result<FireResult>.Success(fireResult);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(fireResult));
        }

        [Test]
        public void Failure_WithFireResult_IsSuccessIsFalse()
        {
            var result = Result<FireResult>.Failure("out of bounds");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("out of bounds"));
        }
    }
}