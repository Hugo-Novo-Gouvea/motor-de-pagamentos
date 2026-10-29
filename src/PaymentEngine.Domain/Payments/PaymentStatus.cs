namespace PaymentEngine.Domain.Payments;

// Explicit values: they will be stored in the database, so reordering the names
// must never change what an existing number means. No status is 0.
public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Failed = 4,
    Canceled = 5,
    PartiallyRefunded = 6,
    Refunded = 7,
}