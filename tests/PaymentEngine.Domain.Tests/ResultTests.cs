using Shouldly;

namespace PaymentEngine.Domain.Tests;

[Trait("Category", "Unit")]
public class ResultTests
{
    private static readonly Error SampleError = new("test.sample_error", "Something expected went wrong.");

    [Fact]
    public void Success_WhenCreated_IsSuccessWithoutError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_WithError_IsFailureCarryingTheError()
    {
        var result = Result.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Failure_NullError_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void GenericSuccess_WithValue_ExposesTheValue()
    {
        var money = new Money(1050, Currency.BRL);

        var result = Result<Money>.Success(money);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(money);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void GenericFailure_WithError_IsFailureCarryingTheError()
    {
        var result = Result<Money>.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void GenericFailure_ReadingValue_ThrowsInvalidOperationException()
    {
        var result = Result<Money>.Failure(SampleError);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericFailure_NullError_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Result<Money>.Failure(null!));
    }

    [Fact]
    public void Error_SameCodeAndMessage_AreEqual()
    {
        var a = new Error("test.code", "Message.");
        var b = new Error("test.code", "Message.");

        (a == b).ShouldBeTrue();
    }
}