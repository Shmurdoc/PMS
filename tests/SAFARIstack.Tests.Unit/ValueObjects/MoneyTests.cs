using FluentAssertions;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Tests.Unit.ValueObjects;

public class MoneyTests
{
    // ─── Creation ────────────────────────────────────────────────────
    [Fact]
    public void FromZAR_ValidAmount_CreatesCorrectly()
    {
        var money = Money.FromZAR(100.50m);
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("ZAR");
    }

    [Fact]
    public void FromAmount_ValidCurrency_CreatesCorrectly()
    {
        var money = Money.FromAmount(250.00m, "usd");
        money.Currency.Should().Be("USD"); // Uppercased
        money.Amount.Should().Be(250.00m);
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        var money = Money.Zero();
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("ZAR");
    }

    [Fact]
    public void Zero_WithCurrency_ReturnsZeroWithCurrency()
    {
        var money = Money.Zero("USD");
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsArgumentException()
    {
        var act = () => Money.FromZAR(-1);
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_EmptyCurrency_ThrowsArgumentException()
    {
        var act = () => Money.FromAmount(100, "");
        act.Should().Throw<ArgumentException>().WithMessage("*Currency*");
    }

    [Fact]
    public void Create_WhitespaceCurrency_ThrowsArgumentException()
    {
        var act = () => Money.FromAmount(100, "   ");
        act.Should().Throw<ArgumentException>().WithMessage("*Currency*");
    }

    [Fact]
    public void Create_RoundsToTwoDecimalPlaces()
    {
        var money = Money.FromZAR(100.555m);
        money.Amount.Should().Be(100.56m); // AwayFromZero rounding
    }

    [Fact]
    public void Create_RoundsAwayFromZero()
    {
        var money = Money.FromZAR(100.545m);
        money.Amount.Should().Be(100.55m);
    }

    // ─── SA Financial Calculations ───────────────────────────────────
    [Fact]
    public void CalculateVAT_Returns15Percent()
    {
        var money = Money.FromZAR(1000m);
        var vat = money.CalculateVAT();
        vat.Amount.Should().Be(150.00m);
        vat.Currency.Should().Be("ZAR");
    }

    [Fact]
    public void CalculateTourismLevy_Returns1Percent()
    {
        var money = Money.FromZAR(1000m);
        var levy = money.CalculateTourismLevy();
        levy.Amount.Should().Be(10.00m);
    }

    [Fact]
    public void WithVAT_Returns115Percent()
    {
        var money = Money.FromZAR(1000m);
        var withVat = money.WithVAT();
        withVat.Amount.Should().Be(1150.00m);
    }

    [Fact]
    public void WithVATAndTourismLevy_Returns116Percent()
    {
        var money = Money.FromZAR(1000m);
        var total = money.WithVATAndTourismLevy();
        total.Amount.Should().Be(1160.00m);
    }

    [Fact]
    public void ExtractVAT_FromVATInclusive_ReturnsCorrectVATAmount()
    {
        var inclusive = Money.FromZAR(1150m);
        var vat = inclusive.ExtractVAT();
        vat.Amount.Should().Be(150.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(999999.99)]
    public void CalculateVAT_BoundaryAmounts_NoException(decimal amount)
    {
        var money = Money.FromZAR(amount);
        var vat = money.CalculateVAT();
        vat.Amount.Should().BeGreaterThanOrEqualTo(0);
    }

    // ─── Arithmetic ──────────────────────────────────────────────────
    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = Money.FromZAR(100);
        var b = Money.FromZAR(200);
        var result = a.Add(b);
        result.Amount.Should().Be(300);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var zar = Money.FromZAR(100);
        var usd = Money.FromAmount(100, "USD");
        var act = () => zar.Add(usd);
        act.Should().Throw<InvalidOperationException>().WithMessage("*currencies*");
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var a = Money.FromZAR(300);
        var b = Money.FromZAR(100);
        var result = a.Subtract(b);
        result.Amount.Should().Be(200);
    }

    [Fact]
    public void Subtract_DifferentCurrency_Throws()
    {
        var zar = Money.FromZAR(100);
        var usd = Money.FromAmount(100, "USD");
        var act = () => zar.Subtract(usd);
        act.Should().Throw<InvalidOperationException>().WithMessage("*currencies*");
    }

    [Fact]
    public void Multiply_ReturnsProduct()
    {
        var money = Money.FromZAR(100);
        var result = money.Multiply(1.5m);
        result.Amount.Should().Be(150);
    }

    [Fact]
    public void OperatorPlus_Works()
    {
        var result = Money.FromZAR(100) + Money.FromZAR(50);
        result.Amount.Should().Be(150);
    }

    [Fact]
    public void OperatorMinus_Works()
    {
        var result = Money.FromZAR(100) - Money.FromZAR(30);
        result.Amount.Should().Be(70);
    }

    [Fact]
    public void OperatorMultiply_LeftMoney_Works()
    {
        var result = Money.FromZAR(100) * 2;
        result.Amount.Should().Be(200);
    }

    [Fact]
    public void OperatorMultiply_LeftDecimal_Works()
    {
        var result = 3m * Money.FromZAR(100);
        result.Amount.Should().Be(300);
    }

    // ─── Equality & ToString ─────────────────────────────────────────
    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.FromZAR(100);
        var b = Money.FromZAR(100);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        var a = Money.FromZAR(100);
        var b = Money.FromZAR(200);
        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var money = Money.FromZAR(1234.56m);
        var str = money.ToString();
        str.Should().Contain("ZAR");
        // Normalize whitespace (SA locale uses non-breaking space U+00A0 as thousands separator)
        var normalized = str.Replace('\u00A0', ' ');
        (normalized.Contains("1 234,56") || normalized.Contains("1,234.56") || normalized.Contains("1234.56"))
            .Should().BeTrue($"ToString returned '{str}' which doesn't contain expected amount format");
    }

    // ─── Constants ───────────────────────────────────────────────────
    [Fact]
    public void SA_VAT_RATE_Is15Percent()
    {
        Money.SA_VAT_RATE.Should().Be(0.15m);
    }

    [Fact]
    public void SA_TOURISM_LEVY_RATE_Is1Percent()
    {
        Money.SA_TOURISM_LEVY_RATE.Should().Be(0.01m);
    }

    [Fact]
    public void DEFAULT_CURRENCY_IsZAR()
    {
        Money.DEFAULT_CURRENCY.Should().Be("ZAR");
    }
}
