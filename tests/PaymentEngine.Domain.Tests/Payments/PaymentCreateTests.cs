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
    [InlineData(49, Currency.BRL)] // one cent below the minimum
    [InlineData(49, Currency.USD)]
    [InlineData(0, Currency.BRL)]
    [InlineData(-1, Currency.BRL)]
    public void Create_AmountBelowMinimum_ReturnsAmountBelowMinimum(long minorUnits, Currency currency)
    {
        var result = Payment.Create(MerchantId, new Money(minorUnits, currency), CaptureMethod.Manual, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.AmountBelowMinimum);
    }

    [Theory]
    [InlineData(Currency.BRL)]
    [InlineData(Currency.USD)]
    public void Create_AmountExactlyAtMinimum_IsAccepted(Currency currency)
    {
        var result = Payment.Create(MerchantId, new Money(50, currency), CaptureMethod.Manual, Now);

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(Currency.BRL)]
    [InlineData(Currency.USD)]
    public void MinimumAmountFor_Currency_IsFiftyMinorUnits(Currency currency)
    {
        Payment.MinimumAmountFor(currency).ShouldBe(new Money(50, currency));
    }

    [Fact]
    public void MinimumAmountFor_EveryCurrency_HasAPositiveMinimum()
    {
        foreach (var currency in Enum.GetValues<Currency>())
        {
            Payment.MinimumAmountFor(currency).MinorUnits.ShouldBeGreaterThan(0L);
        }
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