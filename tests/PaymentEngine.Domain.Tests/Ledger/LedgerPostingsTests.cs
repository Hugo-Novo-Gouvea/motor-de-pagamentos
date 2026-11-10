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

    [Fact]
    public void ForRefund_PartialRefund_MovesTheAmountFromMerchantToAcquirer()
    {
        var payment = TestPayments.InStatus(PaymentStatus.PartiallyRefunded);
        var refund = TestPayments.Brl(TestPayments.PartialRefundInCents);

        var entry = LedgerPostings.ForRefund(payment, refund, Later);

        entry.Type.ShouldBe(JournalEntryType.Refund);
        entry.MerchantId.ShouldBe(payment.MerchantId);
        entry.PaymentId.ShouldBe(payment.Id);
        entry.CreatedAt.ShouldBe(Later);
        entry.Lines.ShouldBe(new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.MerchantPending(payment.MerchantId), refund),
            JournalLine.Credit(AccountCode.AcquirerPayable, refund),
        });
    }

    [Fact]
    public void ForRefund_FullRefund_DoesNotReturnTheFee()
    {
        var payment = TestPayments.InStatus(PaymentStatus.Captured);
        var refund = TestPayments.Brl(TestPayments.AmountInCents);
        payment.Refund(refund, Later).IsSuccess.ShouldBeTrue();

        var entry = LedgerPostings.ForRefund(payment, refund, Later);

        entry.Lines.ShouldNotContain(line => line.Account == AccountCode.FeeRevenue);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    public void ForRefund_PaymentNotRefunded_ThrowsInvalidOperationException(PaymentStatus status)
    {
        var payment = TestPayments.InStatus(status);

        Should.Throw<InvalidOperationException>(() => LedgerPostings.ForRefund(payment, TestPayments.Brl(1_000), Later));
    }

    [Fact]
    public void Postings_CanceledPayment_HaveNoEntryToPost()
    {
        // Canceling an authorized payment moves no money, so nothing is posted to the ledger.
        var payment = TestPayments.InStatus(PaymentStatus.Canceled);

        Should.Throw<InvalidOperationException>(() => LedgerPostings.ForCapture(payment, FeePolicy.Default, Later));
        Should.Throw<InvalidOperationException>(() => LedgerPostings.ForRefund(payment, TestPayments.Brl(1_000), Later));
    }
}