using PaymentEngine.Domain.Payments;

namespace PaymentEngine.Domain.Tests.Payments;

// Builds payments in a given status by walking the real state machine,
// so a test can never create a payment in a state the domain would not allow.
internal static class TestPayments
{
    public static readonly Guid MerchantId = Guid.NewGuid();

    public static readonly DateTimeOffset CreatedAt = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    public static Payment Pending(CaptureMethod captureMethod = CaptureMethod.Manual) =>
        Payment.Create(MerchantId, new Money(10_000, Currency.BRL), captureMethod, CreatedAt).Value;

    public static Payment InStatus(PaymentStatus status)
    {
        var payment = Pending();

        switch (status)
        {
            case PaymentStatus.Pending:
                break;
            case PaymentStatus.Authorized:
                payment.ApproveByAcquirer(CreatedAt);
                break;
            case PaymentStatus.Captured:
                payment = Pending(CaptureMethod.Automatic);
                payment.ApproveByAcquirer(CreatedAt);
                break;
            case PaymentStatus.Failed:
                payment.DeclineByAcquirer("card_declined", CreatedAt);
                break;
            case PaymentStatus.Canceled:
                payment.ApproveByAcquirer(CreatedAt);
                payment.Cancel(CreatedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "No test path to this status yet.");
        }

        if (payment.Status != status)
        {
            throw new InvalidOperationException($"Could not build a payment in status {status}.");
        }

        return payment;
    }
}