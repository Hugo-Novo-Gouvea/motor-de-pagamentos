namespace PaymentEngine.Domain.Ledger;

public sealed class JournalEntry
{
    private JournalEntry(
        Guid id, Guid merchantId, Guid? paymentId, JournalEntryType type, IReadOnlyList<JournalLine> lines, DateTimeOffset createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        PaymentId = paymentId;
        Type = type;
        Lines = lines;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid MerchantId { get; }

    public Guid? PaymentId { get; }

    public JournalEntryType Type { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<JournalLine> Lines { get; }

    public static JournalEntry Create(
        Guid merchantId, Guid? paymentId, JournalEntryType type, IReadOnlyList<JournalLine> lines, DateTimeOffset now)
    {
        if (merchantId == Guid.Empty)
        {
            throw new ArgumentException("A journal entry needs a merchant id.", nameof(merchantId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown journal entry type.");
        }

        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count < 2)
        {
            throw new ArgumentException("A journal entry needs at least two lines.", nameof(lines));
        }

        EnsureBalanced(lines);

        // A copy nobody can change: once checked, the entry stays exactly as it was.
        var linesCopy = lines.ToList().AsReadOnly();

        return new JournalEntry(Guid.CreateVersion7(now), merchantId, paymentId, type, linesCopy, now);
    }

    private static void EnsureBalanced(IReadOnlyList<JournalLine> lines)
    {
        var currency = lines[0].Amount.Currency;
        var debits = new Money(0, currency);
        var credits = new Money(0, currency);

        foreach (var line in lines)
        {
            // Money.Add throws CurrencyMismatchException if a line is in another currency.
            if (line.Direction == Direction.Debit)
            {
                debits = debits.Add(line.Amount);
            }
            else
            {
                credits = credits.Add(line.Amount);
            }
        }

        if (debits != credits)
        {
            throw new UnbalancedJournalEntryException(debits, credits);
        }
    }
}