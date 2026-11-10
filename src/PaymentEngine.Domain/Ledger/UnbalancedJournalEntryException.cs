namespace PaymentEngine.Domain.Ledger;

// Debits different from credits means money appeared or vanished: always a bug in our code.
public sealed class UnbalancedJournalEntryException(Money debits, Money credits)
    : InvalidOperationException(
        $"Journal entry is unbalanced: debits {debits.MinorUnits}, credits {credits.MinorUnits} ({debits.Currency}).");