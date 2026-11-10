using PaymentEngine.Domain.Fees;
using PaymentEngine.Domain.Payments;

namespace PaymentEngine.Domain.Ledger;

public static class LedgerPostings
{
    public static JournalEntry ForCapture(Payment payment, FeePolicy feePolicy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(feePolicy);

        if (payment.Status != PaymentStatus.Captured)
        {
            throw new InvalidOperationException(
                $"Cannot post a capture for a payment in status {payment.Status}.");
        }

        var fee = feePolicy.Calculate(payment.Amount);
        var merchantShare = payment.Amount.Subtract(fee);

        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, payment.Amount),
            JournalLine.Credit(AccountCode.MerchantPending(payment.MerchantId), merchantShare),
        };

        if (fee.MinorUnits > 0)
        {
            lines.Add(JournalLine.Credit(AccountCode.FeeRevenue, fee));
        }

        return JournalEntry.Create(payment.MerchantId, payment.Id, JournalEntryType.Capture, lines, now);
    }

    // The fee is not returned on a refund: the merchant gives back the full amount (see the ADR).
    public static JournalEntry ForRefund(Payment payment, Money amount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(amount);

        if (payment.Status is not (PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
        {
            throw new InvalidOperationException(
                $"Cannot post a refund for a payment in status {payment.Status}.");
        }

        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.MerchantPending(payment.MerchantId), amount),
            JournalLine.Credit(AccountCode.AcquirerPayable, amount),
        };

        return JournalEntry.Create(payment.MerchantId, payment.Id, JournalEntryType.Refund, lines, now);
    }
}