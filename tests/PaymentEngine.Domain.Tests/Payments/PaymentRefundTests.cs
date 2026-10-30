using PaymentEngine.Domain.Payments;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Payments;

[Trait("Category", "Unit")]
public class PaymentRefundTests
{
    private static readonly DateTimeOffset Later = TestPayments.CreatedAt.AddMinutes(1);

    [Fact]
    public void Refund_PartOfCaptured_BecomesPartiallyRefunded()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured);

        var result = payment.Refund(TestPayments.Brl(3_000), Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.PartiallyRefunded);
        payment.AmountRefunded.ShouldBe(TestPayments.Brl(3_000));
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Fact]
    public void Refund_AllOfCaptured_BecomesRefunded()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured);

        var result = payment.Refund(TestPayments.Brl(10_000), Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        payment.AmountRefunded.ShouldBe(TestPayments.Brl(10_000));
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Fact]
    public void Refund_PartOfPartiallyRefunded_StaysPartiallyRefunded()
    {
        var payment = TestPayments.InStatus(PaymentStatus.PartiallyRefunded); // R$ 30,00 already refunded

        var result = payment.Refund(TestPayments.Brl(1_000), Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.PartiallyRefunded);
        payment.AmountRefunded.ShouldBe(TestPayments.Brl(4_000));
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Fact]
    public void Refund_RemainderOfPartiallyRefunded_BecomesRefunded()
    {
        var payment = TestPayments.InStatus(PaymentStatus.PartiallyRefunded); // R$ 70,00 left

        var result = payment.Refund(TestPayments.Brl(7_000), Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        payment.AmountRefunded.ShouldBe(TestPayments.Brl(10_000));
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Theory]
    [InlineData(PaymentStatus.Captured, 10_001)]         // R$ 100,01 of R$ 100,00 left
    [InlineData(PaymentStatus.PartiallyRefunded, 7_001)] // R$ 70,01 of R$ 70,00 left
    public void Refund_MoreThanRemaining_ReturnsRefundExceedsRemainingAndChangesNothing(
        PaymentStatus status, long refundInCents)
    {
        var payment = TestPayments.InStatus(status);
        var refundedBefore = payment.AmountRefunded;

        var result = payment.Refund(TestPayments.Brl(refundInCents), Later);

        result.Error.ShouldBe(PaymentErrors.RefundExceedsRemaining);
        payment.Status.ShouldBe(status);
        payment.AmountRefunded.ShouldBe(refundedBefore);
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Refund_NotPositiveAmount_ReturnsRefundAmountMustBePositiveAndChangesNothing(long refundInCents)
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured);

        var result = payment.Refund(TestPayments.Brl(refundInCents), Later);

        result.Error.ShouldBe(PaymentErrors.RefundAmountMustBePositive);
        payment.Status.ShouldBe(PaymentStatus.Captured);
        payment.AmountRefunded.ShouldBe(TestPayments.Brl(0));
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    [InlineData(PaymentStatus.Refunded)]
    public void Refund_NotRefundableStatus_ReturnsNotRefundableAndChangesNothing(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);
        var refundedBefore = payment.AmountRefunded;

        var result = payment.Refund(TestPayments.Brl(1_000), Later);

        result.Error.ShouldBe(PaymentErrors.NotRefundable);
        payment.Status.ShouldBe(status);
        payment.AmountRefunded.ShouldBe(refundedBefore);
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }

    [Fact]
    public void Refund_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured);

        Should.Throw<CurrencyMismatchException>(() => payment.Refund(new Money(1_000, Currency.USD), Later));
    }
}