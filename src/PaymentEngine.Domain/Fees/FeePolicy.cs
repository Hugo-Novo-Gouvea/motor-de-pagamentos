namespace PaymentEngine.Domain.Fees;

public sealed record FeePolicy
{
    // 2.9% + R$ 0,30: the platform's standard fee.
    public static readonly FeePolicy Default = new(0.029m, new Money(30, Currency.BRL));

    public FeePolicy(decimal percentage, Money fixedFee)
    {
        if (percentage is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), percentage, "The percentage must be between 0 and 1.");
        }

        if (fixedFee.MinorUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedFee), fixedFee, "The fixed fee cannot be negative.");
        }

        Percentage = percentage;
        Fixed = fixedFee;
    }

    public decimal Percentage { get; }

    public Money Fixed { get; }

    // Banker's rounding comes from Money.MultiplyBy, and the currency check from Money.Add.
    public Money Calculate(Money amount) => amount.MultiplyBy(Percentage).Add(Fixed);
}