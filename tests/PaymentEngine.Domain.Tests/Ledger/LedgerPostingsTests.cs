using PaymentEngine.Domain.Fees;
using PaymentEngine.Domain.Ledger;
using PaymentEngine.Domain.Payments;
using PaymentEngine.Domain.Tests.Payments;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Ledger;

[Trait("Category", "Unit")]
public class LedgerPostingsTests
{
    private static readonly DateTimeOffset Later = TestPayments.CreatedAt.AddMinutes(1);

    [Fact]
    public void ForCapture_DefaultFee_SplitsTheAmountBetweenMerchantAndPlatform()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured); // R$ 100,00

        var entry = LedgerPostings.ForCapture(payment, FeePolicy.Default, Later);

        entry.Type.ShouldBe(JournalEntryType.Capture);
        entry.MerchantId.ShouldBe(payment.MerchantId);
        entry.PaymentId.ShouldBe(payment.Id);
        entry.CreatedAt.ShouldBe(Later);
        entry.Lines.ShouldBe(new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, TestPayments.Brl(10_000)),
            JournalLine.Credit(AccountCode.MerchantPending(payment.MerchantId), TestPayments.Brl(9_680)),
            JournalLine.Credit(AccountCode.FeeRevenue, TestPayments.Brl(320)),
        });
    }

    [Fact]
    public void ForCapture_ZeroFee_HasNoFeeLine()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured);
        var noFee = new FeePolicy(0m, TestPayments.Brl(0));

        var entry = LedgerPostings.ForCapture(payment, noFee, Later);

        entry.Lines.ShouldBe(new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, TestPayments.Brl(10_000)),
            JournalLine.Credit(AccountCode.MerchantPending(payment.MerchantId), TestPayments.Brl(10_000)),
        });
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    [InlineData(PaymentStatus.PartiallyRefunded)]
    [InlineData(PaymentStatus.Refunded)]
    public void ForCapture_PaymentNotCaptured_ThrowsInvalidOperationException(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);

        Should.Throw<InvalidOperationException>(() => LedgerPostings.ForCapture(payment, FeePolicy.Default, Later));
    }
}