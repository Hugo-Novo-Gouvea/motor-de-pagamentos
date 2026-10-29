namespace PaymentEngine.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error AmountMustBePositive = new(
        "payment.amount_must_be_positive",
        "The payment amount must be greater than zero.");

    public static readonly Error NotPending = new(
        "payment.not_pending",
        "Only a pending payment can receive the acquirer's response.");

    public static readonly Error NotAuthorized = new(
        "payment.not_authorized",
        "Only an authorized payment can be captured or canceled.");

    public static readonly Error NotRefundable = new(
        "payment.not_refundable",
        "Only a captured or partially refunded payment can be refunded.");

    public static readonly Error RefundAmountMustBePositive = new(
        "payment.refund_amount_must_be_positive",
        "The refund amount must be greater than zero.");

    public static readonly Error RefundExceedsRemaining = new(
        "payment.refund_exceeds_remaining",
        "The refund amount exceeds what is still available to refund.");
}