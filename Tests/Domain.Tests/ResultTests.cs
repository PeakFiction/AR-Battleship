using NUnit.Framework;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Success_Should_Create_ResultWithValue_And_IsSuccessTrue()
    {
        var result = Result<int>.Success(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.Error, Is.Null);
        });
    }

    [Test]
    public void Failure_Should_Create_ResultWithError_And_IsSuccessFalse()
    {
        var result = Result<int>.Failure("Something went wrong");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Something went wrong"));
            Assert.That(result.Value, Is.EqualTo(default(int)));
        });
    }

    [Test]
    public void Success_Result_Should_HaveNullError()
    {
        var result = Result<string>.Success("Hello");

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.Null);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("Hello"));
        });
    }

    [Test]
    public void Failure_Result_Should_HaveDefaultValue()
    {
        var result = Result<double>.Failure("Failed operation");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.EqualTo(default(double)));
            Assert.That(result.Error, Is.EqualTo("Failed operation"));
        });
    }

    [Test]
    public void Success_Result_Can_Hold_NullValue()
    {
        var result = Result<string>.Success(null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Error, Is.Null);
        });
    }
}