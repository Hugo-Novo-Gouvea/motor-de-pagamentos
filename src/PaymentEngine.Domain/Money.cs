namespace PaymentEngine.Domain;

public sealed record Money
{
    public long MinorUnits { get; }

    public Currency Currency { get; }

    public Money(long minorUnits, Currency currency)
    {
        if (!Enum.IsDefined(currency))
        {
            throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown currency.");
        }

        MinorUnits = minorUnits;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(MinorUnits + other.MinorUnits, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(MinorUnits - other.MinorUnits, Currency);
    }

    // Banker's rounding: on an exact tie (.5), round to the nearest even number.
    public Money MultiplyBy(decimal factor)
    {
        var result = Math.Round(MinorUnits * factor, MidpointRounding.ToEven);
        return new Money((long)result, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new CurrencyMismatchException(Currency, other.Currency);
        }
    }
}