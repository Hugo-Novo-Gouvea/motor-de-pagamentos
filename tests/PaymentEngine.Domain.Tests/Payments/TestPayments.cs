using PaymentEngine.Domain.Payments;

namespace PaymentEngine.Domain.Tests.Payments;

// Builds payments in a given status by walking the real state machine,
// so a test can never create a payment in a state the domain would not allow.
internal static class TestPayments
{
    public const long AmountInCents = 10_000;       // R$ 100,00

    public const long PartialRefundInCents = 3_000; // R$ 30,00

    public static readonly Guid MerchantId = Guid.NewGuid();

    public static readonly DateTimeOffset CreatedAt = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    public static Money Brl(long minorUnits) => new(minorUnits, Currency.BRL);

    public static Payment Pending(CaptureMethod captureMethod = CaptureMethod.Manual) =>
        Payment.Create(MerchantId, Brl(AmountInCents), captureMethod, CreatedAt).Value;

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
            case PaymentStatus.PartiallyRefunded:
                payment = Pending(CaptureMethod.Automatic);
                payment.ApproveByAcquirer(CreatedAt);
                payment.Refund(Brl(PartialRefundInCents), CreatedAt);
                break;
            case PaymentStatus.Refunded:
                payment = Pending(CaptureMethod.Automatic);
                payment.ApproveByAcquirer(CreatedAt);
                payment.Refund(Brl(AmountInCents), CreatedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "No test path to this status.");
        }

        if (payment.Status != status)
        {
            throw new InvalidOperationException($"Could not build a payment in status {status}.");
        }

        return payment;
    }
}