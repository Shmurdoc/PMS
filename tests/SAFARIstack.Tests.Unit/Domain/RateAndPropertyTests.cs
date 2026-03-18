using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Tests.Unit.Domain;

public class SeasonTests
{
    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var season = Season.Create(Guid.NewGuid(), "Peak Summer", "PEAK_SUMMER", SeasonType.Peak,
            new DateTime(2026, 12, 1), new DateTime(2027, 1, 31), 1.3m, 10);

        season.Name.Should().Be("Peak Summer");
        season.Code.Should().Be("PEAK_SUMMER");
        season.Type.Should().Be(SeasonType.Peak);
        season.PriceMultiplier.Should().Be(1.3m);
        season.Priority.Should().Be(10);
        season.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var act = () => Season.Create(Guid.NewGuid(), "Bad", "BAD", SeasonType.OffPeak,
            new DateTime(2026, 12, 1), new DateTime(2026, 11, 1), 1.0m);
        act.Should().Throw<ArgumentException>().WithMessage("*after*");
    }

    [Fact]
    public void Create_ZeroMultiplier_Throws()
    {
        var act = () => Season.Create(Guid.NewGuid(), "Free", "FREE", SeasonType.OffPeak,
            new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), 0m);
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void Create_NegativeMultiplier_Throws()
    {
        var act = () => Season.Create(Guid.NewGuid(), "Neg", "NEG", SeasonType.OffPeak,
            new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), -0.5m);
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Theory]
    [InlineData(2026, 6, 15, true)]  // Within range
    [InlineData(2026, 6, 1, true)]   // Start boundary
    [InlineData(2026, 6, 30, true)]  // End boundary
    [InlineData(2026, 5, 31, false)] // Before start
    [InlineData(2026, 7, 1, false)]  // After end
    public void CoversDate_ReturnExpectedResult(int year, int month, int day, bool expected)
    {
        var season = Season.Create(Guid.NewGuid(), "Test", "TST", SeasonType.Shoulder,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), 1.2m);

        season.CoversDate(new DateTime(year, month, day)).Should().Be(expected);
    }
}

public class RatePlanTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var rp = RatePlan.Create(Guid.NewGuid(), "Bed & Breakfast", "BB", RatePlanType.BedAndBreakfast, true, true);
        rp.Name.Should().Be("Bed & Breakfast");
        rp.Code.Should().Be("BB");
        rp.Type.Should().Be(RatePlanType.BedAndBreakfast);
        rp.IncludesBreakfast.Should().BeTrue();
        rp.IsRefundable.Should().BeTrue();
        rp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RaisesEvent()
    {
        var rp = RatePlan.Create(Guid.NewGuid(), "Test", "TST", RatePlanType.Standard);
        rp.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RatePlanCreatedEvent>();
    }

    [Fact]
    public void SetRestrictions_SetsAllFields()
    {
        var rp = RatePlan.Create(Guid.NewGuid(), "Long Stay", "LS", RatePlanType.LongStay);
        rp.SetRestrictions(7, 30, 14, 90);

        rp.MinimumNights.Should().Be(7);
        rp.MaximumNights.Should().Be(30);
        rp.MinimumAdvanceDays.Should().Be(14);
        rp.MaximumAdvanceDays.Should().Be(90);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var rp = RatePlan.Create(Guid.NewGuid(), "Old", "OLD", RatePlanType.Promotional);
        rp.Deactivate();
        rp.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AddRate_IncreasesRates()
    {
        var rp = RatePlan.Create(Guid.NewGuid(), "Std", "STD", RatePlanType.Standard);
        var rate = Rate.Create(Guid.NewGuid(), Guid.NewGuid(), rp.Id, 1500,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(6));
        rp.AddRate(rate);
        rp.Rates.Should().HaveCount(1);
    }
}

public class RateTests
{
    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var rate = Rate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        rate.AmountPerNight.Should().Be(1500);
        rate.Currency.Should().Be("ZAR");
        rate.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var act = () => Rate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -100,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var act = () => Rate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500,
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*after*");
    }

    [Theory]
    [InlineData(15, true)]   // Within range
    [InlineData(1, true)]    // Start boundary
    [InlineData(30, true)]   // End boundary
    [InlineData(31, false)]  // Outside range
    public void IsEffectiveOn_ReturnExpected(int day, bool expected)
    {
        var rate = Rate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 30));
        rate.IsEffectiveOn(new DateTime(2026, 1, day)).Should().Be(expected);
    }

    [Fact]
    public void UpdateAmount_ValidAmount_Updates()
    {
        var rate = Rate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        rate.UpdateAmount(2000);
        rate.AmountPerNight.Should().Be(2000);
    }

    [Fact]
    public void UpdateAmount_NegativeAmount_Throws()
    {
        var rate = Rate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        var act = () => rate.UpdateAmount(-500);
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }
}

public class CancellationPolicyTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var policy = CancellationPolicy.Create(Guid.NewGuid(), "Flexible", 48, 50m, true);
        policy.Name.Should().Be("Flexible");
        policy.FreeCancellationHours.Should().Be(48);
        policy.PenaltyPercentage.Should().Be(50);
        policy.NoShowPenaltyPercentage.Should().Be(100);
        policy.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void CalculatePenalty_WithinFreePeriod_ReturnsZero()
    {
        var policy = CancellationPolicy.Create(Guid.NewGuid(), "Flexible", 48, 50m);
        var checkIn = DateTime.UtcNow.AddHours(72);
        var cancellation = DateTime.UtcNow;

        policy.CalculatePenalty(10000, checkIn, cancellation).Should().Be(0);
    }

    [Fact]
    public void CalculatePenalty_AfterFreePeriod_ReturnsPenaltyAmount()
    {
        var policy = CancellationPolicy.Create(Guid.NewGuid(), "Strict", 24, 100m);
        var checkIn = DateTime.UtcNow.AddHours(12);
        var cancellation = DateTime.UtcNow;

        policy.CalculatePenalty(10000, checkIn, cancellation).Should().Be(10000);
    }

    [Fact]
    public void CalculatePenalty_ExactlyAtBoundary_ReturnsZero()
    {
        var policy = CancellationPolicy.Create(Guid.NewGuid(), "Moderate", 48, 50m);
        var cancellation = DateTime.UtcNow;
        var checkIn = cancellation.AddHours(48);

        policy.CalculatePenalty(10000, checkIn, cancellation).Should().Be(0);
    }

    [Fact]
    public void CalculatePenalty_50Percent_CalculatesCorrectly()
    {
        var policy = CancellationPolicy.Create(Guid.NewGuid(), "Moderate", 48, 50m);
        var cancellation = DateTime.UtcNow;
        var checkIn = cancellation.AddHours(24); // Within penalty window

        policy.CalculatePenalty(10000, checkIn, cancellation).Should().Be(5000);
    }
}

public class PropertyTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var prop = Property.Create("Kruger Lodge", "kruger-lodge", "123 Safari Rd", "Hoedspruit", "Limpopo");
        prop.Name.Should().Be("Kruger Lodge");
        prop.Slug.Should().Be("kruger-lodge");
        prop.Address.Should().Be("123 Safari Rd");
        prop.City.Should().Be("Hoedspruit");
        prop.Province.Should().Be("Limpopo");
        prop.Country.Should().Be("South Africa");
        prop.Currency.Should().Be("ZAR");
        prop.VATRate.Should().Be(0.15m);
        prop.TourismLevyRate.Should().Be(0.01m);
        prop.Timezone.Should().Be("Africa/Johannesburg");
        prop.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RaisesEvent()
    {
        var prop = Property.Create("Test", "test", "Addr", "City", "Province");
        prop.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PropertyCreatedEvent>();
    }

    [Fact]
    public void UpdateDetails_ChangesProperties()
    {
        var prop = Property.Create("Old", "old", "Old Addr", "Old City", "Old Province");
        prop.UpdateDetails("New Lodge", "New Addr", "New City", "Gauteng");

        prop.Name.Should().Be("New Lodge");
        prop.Address.Should().Be("New Addr");
        prop.City.Should().Be("New City");
        prop.Province.Should().Be("Gauteng");
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var prop = Property.Create("Test", "test", "Addr", "City", "Province");
        prop.ClearDomainEvents();
        prop.Deactivate();

        prop.IsActive.Should().BeFalse();
        prop.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PropertyDeactivatedEvent>();
    }
}
