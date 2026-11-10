using PaymentEngine.Domain.Ledger;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Ledger;

[Trait("Category", "Unit")]
public class JournalLineTests
{
    private static readonly Money TenReais = new(1_000, Currency.BRL);

    [Fact]
    public void Debit_PositiveAmount_CreatesADebitLine()
    {
        var line = JournalLine.Debit(AccountCode.AcquirerReceivable, TenReais);

        line.Account.ShouldBe(AccountCode.AcquirerReceivable);
        line.Direction.ShouldBe(Direction.Debit);
        line.Amount.ShouldBe(TenReais);
    }

    [Fact]
    public void Credit_PositiveAmount_CreatesACreditLine()
    {
        var line = JournalLine.Credit(AccountCode.FeeRevenue, TenReais);

        line.Account.ShouldBe(AccountCode.FeeRevenue);
        line.Direction.ShouldBe(Direction.Credit);
        line.Amount.ShouldBe(TenReais);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NotPositiveAmount_ThrowsArgumentOutOfRangeException(long minorUnits)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new JournalLine(AccountCode.FeeRevenue, Direction.Credit, new Money(minorUnits, Currency.BRL)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Constructor_UndefinedDirection_ThrowsArgumentOutOfRangeException(int directionCode)
    {
        var direction = (Direction)directionCode;

        Should.Throw<ArgumentOutOfRangeException>(() => new JournalLine(AccountCode.FeeRevenue, direction, TenReais));
    }
}