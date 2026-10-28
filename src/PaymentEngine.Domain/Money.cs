namespace PaymentEngine.Domain;

public sealed record Money(long MinorUnits, Currency Currency)
{
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
