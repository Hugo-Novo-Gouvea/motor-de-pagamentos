using PaymentEngine.Domain.Payments;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Payments;

[Trait("Category", "Unit")]
public class PaymentCreateTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidData_ReturnsPendingPayment()
    {
        var amount = new Money(1050, Currency.BRL);

        var result = Payment.Create(MerchantId, amount, CaptureMethod.Manual, Now);

        result.IsSuccess.ShouldBeTrue();
        var payment = result.Value;
        payment.Id.ShouldNotBe(Guid.Empty);
        payment.MerchantId.ShouldBe(MerchantId);
        payment.Amount.ShouldBe(amount);
        payment.CaptureMethod.ShouldBe(CaptureMethod.Manual);
        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.AmountRefunded.ShouldBe(new Money(0, Currency.BRL));
        payment.FailureCode.ShouldBeNull();
        payment.CreatedAt.ShouldBe(Now);
        payment.UpdatedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_AmountNotPositive_ReturnsAmountMustBePositive(long minorUnits)
    {
        var result = Payment.Create(MerchantId, new Money(minorUnits, Currency.BRL), CaptureMethod.Manual, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.AmountMustBePositive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Create_UndefinedCaptureMethod_ThrowsArgumentOutOfRangeException(int captureMethodCode)
    {
        var captureMethod = (CaptureMethod)captureMethodCode;

        Should.Throw<ArgumentOutOfRangeException>(
            () => Payment.Create(MerchantId, new Money(1050, Currency.BRL), captureMethod, Now));
    }

    [Fact]
    public void Create_TwoPayments_HaveDifferentIds()
    {
        var first = Payment.Create(MerchantId, new Money(1050, Currency.BRL), CaptureMethod.Manual, Now).Value;
        var second = Payment.Create(MerchantId, new Money(1050, Currency.BRL), CaptureMethod.Manual, Now).Value;

        first.Id.ShouldNotBe(second.Id);
    }
}