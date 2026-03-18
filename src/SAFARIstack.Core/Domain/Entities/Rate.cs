using SAFARIstack.Shared.Domain;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
//  SEASON — Defines peak / shoulder / off-peak periods per property
// ═══════════════════════════════════════════════════════════════════════
public class Season : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;        // "Peak Summer"
    public string Code { get; private set; } = string.Empty;        // "PEAK_SUMMER"
    public SeasonType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal PriceMultiplier { get; private set; } = 1.0m;    // 1.3 = +30%
    public int Priority { get; private set; }                        // Higher wins on overlap
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Property Property { get; private set; } = null!;

    private Season() { }

    public static Season Create(
        Guid propertyId, string name, string code, SeasonType type,
        DateTime startDate, DateTime endDate, decimal priceMultiplier, int priority = 0)
    {
        if (endDate <= startDate) throw new ArgumentException("Season end must be after start.");
        if (priceMultiplier <= 0) throw new ArgumentException("Price multiplier must be positive.");

        return new Season
        {
            PropertyId = propertyId,
            Name = name,
            Code = code,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            PriceMultiplier = priceMultiplier,
            Priority = priority
        };
    }

    public bool CoversDate(DateTime date) => date.Date >= StartDate.Date && date.Date <= EndDate.Date;
}

public enum SeasonType
{
    OffPeak,
    Shoulder,
    Peak,
    SuperPeak,     // e.g. Christmas / Easter
    Event          // Special events (e.g. rugby weekend)
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE PLAN — Named pricing strategy linked to room types
// ═══════════════════════════════════════════════════════════════════════
public class RatePlan : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;        // "Standard", "Bed & Breakfast"
    public string Code { get; private set; } = string.Empty;        // "STD", "BB"
    public string? Description { get; private set; }
    public RatePlanType Type { get; private set; }
    public bool IncludesBreakfast { get; private set; }
    public bool IsRefundable { get; private set; } = true;
    public int? MinimumNights { get; private set; }
    public int? MaximumNights { get; private set; }
    public int? MinimumAdvanceDays { get; private set; }            // Book ≥ X days ahead
    public int? MaximumAdvanceDays { get; private set; }
    public Guid? CancellationPolicyId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Property Property { get; private set; } = null!;
    public CancellationPolicy? CancellationPolicy { get; private set; }
    private readonly List<Rate> _rates = new();
    public IReadOnlyCollection<Rate> Rates => _rates.AsReadOnly();

    private RatePlan() { }

    public static RatePlan Create(
        Guid propertyId, string name, string code, RatePlanType type,
        bool includesBreakfast = false, bool isRefundable = true)
    {
        var plan = new RatePlan
        {
            PropertyId = propertyId,
            Name = name,
            Code = code,
            Type = type,
            IncludesBreakfast = includesBreakfast,
            IsRefundable = isRefundable
        };
        plan.AddDomainEvent(new RatePlanCreatedEvent(plan.Id, name, propertyId));
        return plan;
    }

    public void AddRate(Rate rate) => _rates.Add(rate);

    public void SetRestrictions(int? minNights, int? maxNights, int? minAdvanceDays, int? maxAdvanceDays)
    {
        MinimumNights = minNights;
        MaximumNights = maxNights;
        MinimumAdvanceDays = minAdvanceDays;
        MaximumAdvanceDays = maxAdvanceDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}

public enum RatePlanType
{
    Standard,
    BedAndBreakfast,
    HalfBoard,
    FullBoard,
    AllInclusive,
    Promotional,
    Corporate,
    GroupRate,
    LongStay,
    OTARate            // Rate specifically for OTA channels
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE — Specific price for a RoomType + RatePlan + DateRange
// ═══════════════════════════════════════════════════════════════════════
public class Rate : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public Guid RatePlanId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public decimal AmountPerNight { get; private set; }             // Base price per night (ZAR excl.)
    public decimal? SingleOccupancyRate { get; private set; }       // Discount for single occupancy
    public decimal? ExtraAdultRate { get; private set; }            // Per extra adult per night
    public decimal? ExtraChildRate { get; private set; }            // Per extra child per night
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string Currency { get; private set; } = Money.DEFAULT_CURRENCY;
    public bool IsActive { get; private set; } = true;

    // Navigation
    public RoomType RoomType { get; private set; } = null!;
    public RatePlan RatePlan { get; private set; } = null!;
    public Season? Season { get; private set; }

    private Rate() { }

    public static Rate Create(
        Guid propertyId, Guid roomTypeId, Guid ratePlanId,
        decimal amountPerNight, DateTime effectiveFrom, DateTime effectiveTo,
        Guid? seasonId = null)
    {
        if (amountPerNight < 0) throw new ArgumentException("Rate amount cannot be negative.");
        if (effectiveTo <= effectiveFrom) throw new ArgumentException("Rate end must be after start.");

        return new Rate
        {
            PropertyId = propertyId,
            RoomTypeId = roomTypeId,
            RatePlanId = ratePlanId,
            SeasonId = seasonId,
            AmountPerNight = amountPerNight,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }

    public bool IsEffectiveOn(DateTime date) => date.Date >= EffectiveFrom.Date && date.Date <= EffectiveTo.Date;

    public void UpdateAmount(decimal newAmount)
    {
        if (newAmount < 0) throw new ArgumentException("Rate amount cannot be negative.");
        AmountPerNight = newAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  CANCELLATION POLICY
// ═══════════════════════════════════════════════════════════════════════
public class CancellationPolicy : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;        // "Flexible", "Moderate", "Strict"
    public string? Description { get; private set; }
    public int FreeCancellationHours { get; private set; }          // Hours before check-in
    public decimal PenaltyPercentage { get; private set; }          // e.g. 50 = 50% of total
    public decimal? NoShowPenaltyPercentage { get; private set; }   // 100 = full charge
    public bool IsDefault { get; private set; }

    private CancellationPolicy() { }

    public static CancellationPolicy Create(
        Guid propertyId, string name, int freeCancellationHours,
        decimal penaltyPercentage, bool isDefault = false)
    {
        return new CancellationPolicy
        {
            PropertyId = propertyId,
            Name = name,
            FreeCancellationHours = freeCancellationHours,
            PenaltyPercentage = penaltyPercentage,
            NoShowPenaltyPercentage = 100,
            IsDefault = isDefault
        };
    }

    /// <summary>
    /// Calculates the cancellation penalty for a given booking total and check-in time
    /// </summary>
    public decimal CalculatePenalty(decimal bookingTotal, DateTime checkInDate, DateTime cancellationTime)
    {
        var hoursBeforeCheckIn = (checkInDate - cancellationTime).TotalHours;
        if (hoursBeforeCheckIn >= FreeCancellationHours)
            return 0;

        return decimal.Round(bookingTotal * (PenaltyPercentage / 100m), 2, MidpointRounding.AwayFromZero);
    }
}

// ─── Domain Events ───────────────────────────────────────────────────
public record RatePlanCreatedEvent(Guid RatePlanId, string Name, Guid PropertyId) : DomainEvent;
