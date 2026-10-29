using System.Globalization;
using Shouldly;

namespace PaymentEngine.Domain.Tests;

[Trait("Category", "Unit")]
public class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = new Money(1050, Currency.BRL);
        var b = new Money(250, Currency.BRL);

        var total = a.Add(b);

        total.ShouldBe(new Money(1300, Currency.BRL));
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var a = new Money(1050, Currency.BRL);
        var b = new Money(250, Currency.BRL);

        var difference = a.Subtract(b);

        difference.ShouldBe(new Money(800, Currency.BRL));
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsCurrencyMismatchException()
    {
        var brl = new Money(1000, Currency.BRL);
        var usd = new Money(1000, Currency.USD);

        Should.Throw<CurrencyMismatchException>(() => brl.Add(usd));
    }

    [Fact]
    public void Subtract_DifferentCurrencies_ThrowsCurrencyMismatchException()
    {
        var brl = new Money(1000, Currency.BRL);
        var usd = new Money(1000, Currency.USD);

        Should.Throw<CurrencyMismatchException>(() => brl.Subtract(usd));
    }

    [Fact]
    public void Add_SameCurrency_DoesNotModifyOriginal()
    {
        var original = new Money(1050, Currency.BRL);

        original.Add(new Money(250, Currency.BRL));

        original.MinorUnits.ShouldBe(1050);
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        var a = new Money(1000, Currency.BRL);
        var b = new Money(1000, Currency.BRL);

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameAmountDifferentCurrency_ReturnsFalse()
    {
        var brl = new Money(1000, Currency.BRL);
        var usd = new Money(1000, Currency.USD);

        (brl == usd).ShouldBeFalse();
    }

    [Theory]
    [InlineData(1000, "0.029", 29)] // 29,00  -> 29 (sem arredondamento)
    [InlineData(1050, "0.029", 30)] // 30,45  -> 30 (abaixo de ,5: desce)
    [InlineData(1090, "0.029", 32)] // 31,61  -> 32 (acima de ,5: sobe)
    [InlineData(250, "0.01", 2)]    // 2,5    -> 2  (empate: vai para o par)
    [InlineData(350, "0.01", 4)]    // 3,5    -> 4  (empate: vai para o par)
    public void MultiplyBy_Factor_RoundsHalfToEven(long minorUnits, string factor, long expected)
    {
        var money = new Money(minorUnits, Currency.BRL);

        var result = money.MultiplyBy(decimal.Parse(factor, CultureInfo.InvariantCulture));

        result.ShouldBe(new Money(expected, Currency.BRL));
    }

    [Theory]
    [InlineData(0)]   // the default value of any enum: a currency nobody filled in
    [InlineData(123)] // a number that has no name in the enum
    public void Constructor_UndefinedCurrency_ThrowsArgumentOutOfRangeException(int currencyCode)
    {
        var currency = (Currency)currencyCode;

        Should.Throw<ArgumentOutOfRangeException>(() => new Money(1050, currency));
    }
}