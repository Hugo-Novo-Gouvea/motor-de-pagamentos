using System.Globalization;
using PaymentEngine.Domain.Fees;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Fees;

[Trait("Category", "Unit")]
public class FeePolicyTests
{
    [Theory]
    [InlineData(10_000, 320)] // R$ 100,00: 290 + 30
    [InlineData(1_050, 60)]   // R$ 10,50: 30,45 -> 30 (below .5), + 30
    [InlineData(1_090, 62)]   // R$ 10,90: 31,61 -> 32 (above .5), + 30
    [InlineData(500, 44)]     // R$ 5,00: 14,5 -> 14 (tie: to even), + 30
    [InlineData(1_500, 74)]   // R$ 15,00: 43,5 -> 44 (tie: to even), + 30
    public void Calculate_DefaultPolicy_ReturnsPercentagePlusFixedWithBankersRounding(
        long amountInCents, long expectedFeeInCents)
    {
        var fee = FeePolicy.Default.Calculate(new Money(amountInCents, Currency.BRL));

        fee.ShouldBe(new Money(expectedFeeInCents, Currency.BRL));
    }

    [Fact]
    public void Default_IsTwoPointNinePercentPlusThirtyCents()
    {
        FeePolicy.Default.Percentage.ShouldBe(0.029m);
        FeePolicy.Default.Fixed.ShouldBe(new Money(30, Currency.BRL));
    }

    [Fact]
    public void Calculate_CustomPolicy_UsesItsOwnValues()
    {
        var policy = new FeePolicy(0.05m, new Money(100, Currency.BRL)); // 5% + R$ 1,00

        var fee = policy.Calculate(new Money(10_000, Currency.BRL));

        fee.ShouldBe(new Money(600, Currency.BRL)); // 500 + 100
    }

    [Fact]
    public void Calculate_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        Should.Throw<CurrencyMismatchException>(
            () => FeePolicy.Default.Calculate(new Money(10_000, Currency.USD)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.01")]
    public void Constructor_PercentageOutOfRange_ThrowsArgumentOutOfRangeException(string percentage)
    {
        var value = decimal.Parse(percentage, CultureInfo.InvariantCulture);

        Should.Throw<ArgumentOutOfRangeException>(() => new FeePolicy(value, new Money(30, Currency.BRL)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void Constructor_PercentageAtLimits_IsAccepted(string percentage)
    {
        var value = decimal.Parse(percentage, CultureInfo.InvariantCulture);

        var policy = new FeePolicy(value, new Money(0, Currency.BRL));

        policy.Percentage.ShouldBe(value);
    }

    [Fact]
    public void Constructor_NegativeFixedFee_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new FeePolicy(0.029m, new Money(-1, Currency.BRL)));
    }
}