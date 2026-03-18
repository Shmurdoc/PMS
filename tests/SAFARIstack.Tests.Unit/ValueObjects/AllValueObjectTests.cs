using FluentAssertions;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Tests.Unit.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Create_ValidRange_SetsProperties()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 5);
        var range = new DateRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var act = () => new DateRange(new DateTime(2026, 1, 5), new DateTime(2026, 1, 1));
        act.Should().Throw<ArgumentException>().WithMessage("*after*");
    }

    [Fact]
    public void Create_SameStartAndEnd_NoThrow()
    {
        var date = new DateTime(2026, 3, 15);
        var range = new DateRange(date, date);
        range.Nights.Should().Be(0);
        range.Days.Should().Be(1);
    }

    [Theory]
    [InlineData(1, 5, 4)]
    [InlineData(1, 2, 1)]
    [InlineData(1, 31, 30)]
    public void Nights_ReturnsCorrectCount(int startDay, int endDay, int expectedNights)
    {
        var range = new DateRange(new DateTime(2026, 1, startDay), new DateTime(2026, 1, endDay));
        range.Nights.Should().Be(expectedNights);
    }

    [Theory]
    [InlineData(1, 5, 5)]
    [InlineData(1, 1, 1)]
    public void Days_ReturnsNightsPlusOne(int startDay, int endDay, int expectedDays)
    {
        var range = new DateRange(new DateTime(2026, 1, startDay), new DateTime(2026, 1, endDay));
        range.Days.Should().Be(expectedDays);
    }

    [Fact]
    public void Contains_DateWithinRange_ReturnsTrue()
    {
        var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        range.Contains(new DateTime(2026, 1, 15)).Should().BeTrue();
    }

    [Fact]
    public void Contains_DateOnStart_ReturnsTrue()
    {
        var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        range.Contains(new DateTime(2026, 1, 1)).Should().BeTrue();
    }

    [Fact]
    public void Contains_DateOnEnd_ReturnsTrue()
    {
        var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        range.Contains(new DateTime(2026, 1, 31)).Should().BeTrue();
    }

    [Fact]
    public void Contains_DateOutsideRange_ReturnsFalse()
    {
        var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        range.Contains(new DateTime(2026, 2, 1)).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ReturnsTrue()
    {
        var a = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 15));
        var b = new DateRange(new DateTime(2026, 1, 10), new DateTime(2026, 1, 20));
        a.Overlaps(b).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_NonOverlapping_ReturnsFalse()
    {
        var a = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var b = new DateRange(new DateTime(2026, 1, 15), new DateTime(2026, 1, 20));
        a.Overlaps(b).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_TouchingBoundary_ReturnsTrue()
    {
        var a = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var b = new DateRange(new DateTime(2026, 1, 5), new DateTime(2026, 1, 15));
        a.Overlaps(b).Should().BeTrue();
    }

    [Fact]
    public void EachDay_Enumerates_AllDays()
    {
        var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));
        var days = range.EachDay().ToList();
        days.Should().HaveCount(3);
        days[0].Should().Be(new DateTime(2026, 1, 1));
        days[1].Should().Be(new DateTime(2026, 1, 2));
        days[2].Should().Be(new DateTime(2026, 1, 3));
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 4));
        var str = range.ToString();
        str.Should().Contain("2026-01-01");
        str.Should().Contain("2026-01-04");
        str.Should().Contain("3N");
    }

    // Record equality
    [Fact]
    public void Equality_SameRange_AreEqual()
    {
        var a = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 5));
        var b = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 5));
        a.Should().Be(b);
    }
}

public class FinancialBreakdownTests
{
    [Fact]
    public void Calculate_BasicSubtotal_CorrectBreakdown()
    {
        var subtotal = Money.FromZAR(1000);
        var breakdown = FinancialBreakdown.Calculate(subtotal);

        breakdown.Subtotal.Amount.Should().Be(1000);
        breakdown.VATAmount.Amount.Should().Be(150); // 15%
        breakdown.TourismLevy.Amount.Should().Be(10); // 1%
        breakdown.AdditionalCharges.Amount.Should().Be(0);
        breakdown.DiscountAmount.Amount.Should().Be(0);
        breakdown.Total.Amount.Should().Be(1160); // 1000 + 150 + 10
    }

    [Fact]
    public void Calculate_WithAdditionalCharges_IncludesInTotal()
    {
        var subtotal = Money.FromZAR(1000);
        var additional = Money.FromZAR(200);
        var breakdown = FinancialBreakdown.Calculate(subtotal, additional);

        // Adjusted subtotal = 1000 + 200 = 1200
        breakdown.Subtotal.Amount.Should().Be(1200);
        breakdown.VATAmount.Amount.Should().Be(180); // 15% of 1200
        breakdown.TourismLevy.Amount.Should().Be(12); // 1% of 1200
        breakdown.Total.Amount.Should().Be(1392); // 1200 + 180 + 12
    }

    [Fact]
    public void Calculate_WithDiscount_SubtractsFromSubtotal()
    {
        var subtotal = Money.FromZAR(1000);
        var discount = Money.FromZAR(100);
        var breakdown = FinancialBreakdown.Calculate(subtotal, discountAmount: discount);

        // Adjusted subtotal = 1000 - 100 = 900
        breakdown.Subtotal.Amount.Should().Be(900);
        breakdown.VATAmount.Amount.Should().Be(135); // 15% of 900
        breakdown.TourismLevy.Amount.Should().Be(9); // 1% of 900
        breakdown.Total.Amount.Should().Be(1044); // 900 + 135 + 9
    }

    [Fact]
    public void Calculate_WithBothAdditionalAndDiscount_Correct()
    {
        var subtotal = Money.FromZAR(1000);
        var additional = Money.FromZAR(500);
        var discount = Money.FromZAR(200);
        var breakdown = FinancialBreakdown.Calculate(subtotal, additional, discount);

        // Adjusted: 1000 + 500 - 200 = 1300
        breakdown.Subtotal.Amount.Should().Be(1300);
    }

    [Fact]
    public void Calculate_ZeroSubtotal_AllZero()
    {
        var breakdown = FinancialBreakdown.Calculate(Money.Zero());
        breakdown.Total.Amount.Should().Be(0);
        breakdown.VATAmount.Amount.Should().Be(0);
        breakdown.TourismLevy.Amount.Should().Be(0);
    }
}

public class PercentageTests
{
    [Fact]
    public void FromFraction_Valid_SetsValue()
    {
        var pct = Percentage.FromFraction(0.15m);
        pct.Value.Should().Be(0.15m);
    }

    [Fact]
    public void FromPercent_Valid_ConvertsFractionCorrectly()
    {
        var pct = Percentage.FromPercent(15);
        pct.Value.Should().Be(0.15m);
    }

    [Fact]
    public void FromFraction_Negative_Throws()
    {
        var act = () => Percentage.FromFraction(-0.1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromFraction_GreaterThan1_Throws()
    {
        var act = () => Percentage.FromFraction(1.1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromPercent_Negative_Throws()
    {
        var act = () => Percentage.FromPercent(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromPercent_GreaterThan100_Throws()
    {
        var act = () => Percentage.FromPercent(101);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Zero_HasZeroValue()
    {
        Percentage.Zero.Value.Should().Be(0);
    }

    [Fact]
    public void ApplyTo_CalculatesCorrectly()
    {
        var pct = Percentage.FromPercent(15);
        var result = pct.ApplyTo(1000);
        result.Should().Be(150.00m);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var pct = Percentage.FromPercent(15);
        pct.ToString().Should().Contain("15");
        pct.ToString().Should().Contain("%");
    }

    [Fact]
    public void BoundaryValues_0And100Percent()
    {
        var zero = Percentage.FromPercent(0);
        var hundred = Percentage.FromPercent(100);
        zero.ApplyTo(1000).Should().Be(0);
        hundred.ApplyTo(1000).Should().Be(1000);
    }
}

public class AddressTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var addr = Address.Create("123 Main St", "Cape Town", "Western Cape", "South Africa", "Suite 5", "8001");
        addr.Line1.Should().Be("123 Main St");
        addr.Line2.Should().Be("Suite 5");
        addr.City.Should().Be("Cape Town");
        addr.Province.Should().Be("Western Cape");
        addr.PostalCode.Should().Be("8001");
        addr.Country.Should().Be("South Africa");
    }

    [Fact]
    public void Create_DefaultCountry_IsSouthAfrica()
    {
        var addr = Address.Create("1 Long St", "Johannesburg", "Gauteng");
        addr.Country.Should().Be("South Africa");
    }

    [Fact]
    public void ToString_IncludesAllNonNullParts()
    {
        var addr = Address.Create("1 Long St", "CPT", "WC");
        var str = addr.ToString();
        str.Should().Contain("1 Long St");
        str.Should().Contain("CPT");
        str.Should().Contain("WC");
        str.Should().Contain("South Africa");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Address.Create("1 Long St", "CPT", "WC");
        var b = Address.Create("1 Long St", "CPT", "WC");
        a.Should().Be(b);
    }
}
