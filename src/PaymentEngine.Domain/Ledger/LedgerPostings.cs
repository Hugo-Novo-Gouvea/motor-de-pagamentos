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
}