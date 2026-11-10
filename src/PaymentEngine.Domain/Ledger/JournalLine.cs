namespace PaymentEngine.Domain.Ledger;

public sealed record JournalLine
{
    public JournalLine(AccountCode account, Direction direction, Money amount)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.");
        }

        // The direction says whether money comes in or goes out, never the sign of the amount.
        if (amount.MinorUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "A journal line amount must be positive.");
        }

        Account = account;
        Direction = direction;
        Amount = amount;
    }

    public AccountCode Account { get; }

    public Direction Direction { get; }

    public Money Amount { get; }

    public static JournalLine Debit(AccountCode account, Money amount) => new(account, Direction.Debit, amount);

    public static JournalLine Credit(AccountCode account, Money amount) => new(account, Direction.Credit, amount);
}