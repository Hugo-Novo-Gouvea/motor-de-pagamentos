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

    public Result ApproveByAcquirer(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
        {
            return Result.Failure(PaymentErrors.NotPending);
        }

        Status = CaptureMethod == CaptureMethod.Automatic
            ? PaymentStatus.Captured
            : PaymentStatus.Authorized;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result DeclineByAcquirer(string failureCode, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        if (Status != PaymentStatus.Pending)
        {
            return Result.Failure(PaymentErrors.NotPending);
        }

        Status = PaymentStatus.Failed;
        FailureCode = failureCode;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result Capture(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Authorized)
        {
            return Result.Failure(PaymentErrors.NotAuthorized);
        }

        Status = PaymentStatus.Captured;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result Cancel(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Authorized)
        {
            return Result.Failure(PaymentErrors.NotAuthorized);
        }

        Status = PaymentStatus.Canceled;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result Refund(Money amount, DateTimeOffset now)
    {
        if (amount.Currency != Amount.Currency)
        {
            throw new CurrencyMismatchException(Amount.Currency, amount.Currency);
        }

        if (Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
        {
            return Result.Failure(PaymentErrors.NotRefundable);
        }

        if (amount.MinorUnits <= 0)
        {
            return Result.Failure(PaymentErrors.RefundAmountMustBePositive);
        }

        var remaining = Amount.Subtract(AmountRefunded);

        if (amount.MinorUnits > remaining.MinorUnits)
        {
            return Result.Failure(PaymentErrors.RefundExceedsRemaining);
        }

        AmountRefunded = AmountRefunded.Add(amount);
        Status = AmountRefunded == Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        UpdatedAt = now;

        return Result.Success();
    }
}