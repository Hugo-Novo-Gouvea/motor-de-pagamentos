namespace PaymentEngine.Domain.Ledger;

// Explicit values: they will be stored in the database. No direction is 0.
public enum Direction
{
    Debit = 1,
    Credit = 2,
}