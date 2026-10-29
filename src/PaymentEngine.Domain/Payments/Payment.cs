namespace PaymentEngine.Domain.Payments;

public sealed class Payment
{
    private Payment(Guid id, Guid merchantId, Money amount, CaptureMethod captureMethod, DateTimeOffset now)
    {
        Id = id;
        MerchantId = merchantId;
        Amount = amount;
        CaptureMethod = captureMethod;
        Status = PaymentStatus.Pending;
        AmountRefunded = new Money(0, amount.Currency);
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; }

    public Guid MerchantId { get; }

    public Money Amount { get; }

    public CaptureMethod CaptureMethod { get; }

    public PaymentStatus Status { get; private set; }

    public Money AmountRefunded { get; private set; }

    public string? FailureCode { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Payment> Create(Guid merchantId, Money amount, CaptureMethod captureMethod, DateTimeOffset now)
    {
        if (!Enum.IsDefined(captureMethod))
        {
            throw new ArgumentOutOfRangeException(nameof(captureMethod), captureMethod, "Unknown capture method.");
        }

        if (amount.MinorUnits <= 0)
        {
            return Result<Payment>.Failure(PaymentErrors.AmountMustBePositive);
        }

        var payment = new Payment(Guid.CreateVersion7(now), merchantId, amount, captureMethod, now);
        return Result<Payment>.Success(payment);
    }
}