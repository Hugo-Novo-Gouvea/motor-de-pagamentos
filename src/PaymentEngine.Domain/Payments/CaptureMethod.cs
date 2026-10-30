namespace PaymentEngine.Domain.Payments;

// Automatic: the acquirer approves and captures in one step.
// Manual: the acquirer only authorizes, and the merchant captures later.
public enum CaptureMethod
{
    Automatic = 1,
    Manual = 2,
}