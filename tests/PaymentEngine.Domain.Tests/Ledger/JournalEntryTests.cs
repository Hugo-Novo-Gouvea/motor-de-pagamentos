using PaymentEngine.Domain.Ledger;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Ledger;

[Trait("Category", "Unit")]
public class JournalEntryTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly Guid PaymentId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static Money Brl(long minorUnits) => new(minorUnits, Currency.BRL);

    [Fact]
    public void Create_BalancedLines_CreatesTheEntry()
    {
        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(10_000)),
            JournalLine.Credit(AccountCode.MerchantPending(MerchantId), Brl(9_680)),
            JournalLine.Credit(AccountCode.FeeRevenue, Brl(320)),
        };

        var entry = JournalEntry.Create(MerchantId, PaymentId, JournalEntryType.Capture, lines, Now);

        entry.Id.ShouldNotBe(Guid.Empty);
        entry.MerchantId.ShouldBe(MerchantId);
        entry.PaymentId.ShouldBe(PaymentId);
        entry.Type.ShouldBe(JournalEntryType.Capture);
        entry.CreatedAt.ShouldBe(Now);
        entry.Lines.ShouldBe(lines);
    }

    [Fact]
    public void Create_DebitsDifferentFromCredits_ThrowsUnbalancedJournalEntryException()
    {
        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(10_000)),
            JournalLine.Credit(AccountCode.MerchantPending(MerchantId), Brl(9_680)), // the fee line is missing
        };

        Should.Throw<UnbalancedJournalEntryException>(
            () => JournalEntry.Create(MerchantId, PaymentId, JournalEntryType.Capture, lines, Now));
    }

    [Fact]
    public void Create_SingleLine_ThrowsArgumentException()
    {
        var lines = new List<JournalLine> { JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(10_000)) };

        Should.Throw<ArgumentException>(
            () => JournalEntry.Create(MerchantId, PaymentId, JournalEntryType.Capture, lines, Now));
    }

    [Fact]
    public void Create_MixedCurrencies_ThrowsCurrencyMismatchException()
    {
        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(1_000)),
            JournalLine.Credit(AccountCode.FeeRevenue, new Money(1_000, Currency.USD)),
        };

        Should.Throw<CurrencyMismatchException>(
            () => JournalEntry.Create(MerchantId, PaymentId, JournalEntryType.Capture, lines, Now));
    }

    [Fact]
    public void Create_EmptyMerchantId_ThrowsArgumentException()
    {
        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(1_000)),
            JournalLine.Credit(AccountCode.FeeRevenue, Brl(1_000)),
        };

        Should.Throw<ArgumentException>(
            () => JournalEntry.Create(Guid.Empty, PaymentId, JournalEntryType.Capture, lines, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Create_UndefinedType_ThrowsArgumentOutOfRangeException(int typeCode)
    {
        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(1_000)),
            JournalLine.Credit(AccountCode.FeeRevenue, Brl(1_000)),
        };

        Should.Throw<ArgumentOutOfRangeException>(
            () => JournalEntry.Create(MerchantId, PaymentId, (JournalEntryType)typeCode, lines, Now));
    }

    [Fact]
    public void Create_LinesChangedAfterwards_EntryKeepsTheOriginalLines()
    {
        var lines = new List<JournalLine>
        {
            JournalLine.Debit(AccountCode.AcquirerReceivable, Brl(1_000)),
            JournalLine.Credit(AccountCode.FeeRevenue, Brl(1_000)),
        };
        var entry = JournalEntry.Create(MerchantId, PaymentId, JournalEntryType.Capture, lines, Now);

        lines.Add(JournalLine.Debit(AccountCode.PlatformCash, Brl(500)));

        entry.Lines.Count.ShouldBe(2);
    }
}