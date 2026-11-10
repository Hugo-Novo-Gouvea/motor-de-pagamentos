namespace PaymentEngine.Domain.Ledger;

// Explicit values: they will be stored in the database. Settlement comes with phase 4.
public enum JournalEntryType
{
    Capture = 1,
    Refund = 2,
}