using PaymentEngine.Domain.Payments;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Payments;

[Trait("Category", "Unit")]
public class PaymentAcquirerResponseTests
{
    private static readonly DateTimeOffset Later = TestPayments.CreatedAt.AddMinutes(1);

    [Fact]
    public void ApproveByAcquirer_PendingWithManualCapture_BecomesAuthorized()
    {
        var payment = TestPayments.Pending(CaptureMethod.Manual);

        var result = payment.ApproveByAcquirer(Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Authorized);
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Fact]
    public void ApproveByAcquirer_PendingWithAutomaticCapture_BecomesCaptured()
    {
        var payment = TestPayments.Pending(CaptureMethod.Automatic);

        var result = payment.ApproveByAcquirer(Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Captured);
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Fact]
    public void DeclineByAcquirer_Pending_BecomesFailedWithFailureCode()
    {
        var payment = TestPayments.Pending();

        var result = payment.DeclineByAcquirer("insufficient_funds", Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.FailureCode.ShouldBe("insufficient_funds");
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    public void ApproveByAcquirer_NotPending_ReturnsNotPendingAndChangesNothing(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);

        var result = payment.ApproveByAcquirer(Later);

        result.Error.ShouldBe(PaymentErrors.NotPending);
        payment.Status.ShouldBe(status);
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }

    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    public void DeclineByAcquirer_NotPending_ReturnsNotPendingAndChangesNothing(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);
        var failureCodeBefore = payment.FailureCode;

        var result = payment.DeclineByAcquirer("insufficient_funds", Later);

        result.Error.ShouldBe(PaymentErrors.NotPending);
        payment.Status.ShouldBe(status);
        payment.FailureCode.ShouldBe(failureCodeBefore);
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeclineByAcquirer_BlankFailureCode_ThrowsArgumentException(string failureCode)
    {
        var payment = TestPayments.Pending();

        Should.Throw<ArgumentException>(() => payment.DeclineByAcquirer(failureCode, Later));
    }
}