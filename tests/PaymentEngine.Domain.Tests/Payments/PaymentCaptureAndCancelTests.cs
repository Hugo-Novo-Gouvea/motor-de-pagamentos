using PaymentEngine.Domain.Payments;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Payments;

[Trait("Category", "Unit")]
public class PaymentCaptureAndCancelTests
{
    private static readonly DateTimeOffset Later = TestPayments.CreatedAt.AddMinutes(1);

    [Fact]
    public void Capture_Authorized_BecomesCaptured()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Authorized);

        var result = payment.Capture(Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Captured);
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Fact]
    public void Cancel_Authorized_BecomesCanceled()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Authorized);

        var result = payment.Cancel(Later);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Canceled);
        payment.UpdatedAt.ShouldBe(Later);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    public void Capture_NotAuthorized_ReturnsNotAuthorizedAndChangesNothing(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);

        var result = payment.Capture(Later);

        result.Error.ShouldBe(PaymentErrors.NotAuthorized);
        payment.Status.ShouldBe(status);
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    public void Cancel_NotAuthorized_ReturnsNotAuthorizedAndChangesNothing(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);

        var result = payment.Cancel(Later);

        result.Error.ShouldBe(PaymentErrors.NotAuthorized);
        payment.Status.ShouldBe(status);
        payment.UpdatedAt.ShouldBe(TestPayments.CreatedAt);
    }
}