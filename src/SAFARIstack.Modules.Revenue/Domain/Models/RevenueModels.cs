namespace SAFARIstack.Modules.Revenue.Domain.Models;

/// <summary>
/// Pricing recommendation from RMS
/// Suggests rates without enforcing them - human oversight preserved
/// </summary>
public class PricingRecommendation
{
    public Guid RecommendationId { get; init; } = Guid.NewGuid();
    public Guid PropertyId { get; init; }
    public Guid RoomTypeId { get; init; }
    public DateTime Date { get; init; }
    public decimal CurrentRate { get; init; }
    public decimal RecommendedRate { get; init; }
    public decimal? UpperBoundRate { get; init; } // Max recommended
    public decimal? LowerBoundRate { get; init; } // Min recommended
    public string[] InfluencingFactors { get; init; } = Array.Empty<string>(); // "High demand", "Competitor rates", "Seasonality"
    public decimal ConfidenceScore { get; init; } // 0.0 - 1.0
    public string AnalysisReason { get; init; } = string.Empty;
    public bool IsAccepted { get; init; }
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Rate shopping insight - what competitors are charging
/// </summary>
public class RateShoppingInsight
{
    public Guid InsightId { get; init; } = Guid.NewGuid();
    public Guid PropertyId { get; init; }
    public string RoomType { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public decimal OurRate { get; init; }
    public decimal? Booking_comRate { get; init; }
    public decimal? ExpediaRate { get; init; }
    public decimal? AirbnbRate { get; init; }
    public decimal AverageCompetitorRate { get; init; }
    public decimal RateIndex { get; init; } // Our rate / Average competitor = position
    public string Recommendation { get; init; } = string.Empty; // "increase", "decrease", "maintain"
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Revenue alert when significant opportunities/risks detected
/// </summary>
public class RevenueAlert
{
    public Guid AlertId { get; init; } = Guid.NewGuid();
    public Guid PropertyId { get; init; }
    public string AlertType { get; init; } = string.Empty; // "LowOccupancy", "HighDemand", "PriceUndercutting", "CompetitorAlert"
    public string Severity { get; init; } = "Info"; // "Info", "Warning", "Critical"
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object> RecommendedActions { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public bool IsResolved { get; init; }
}

/// <summary>
/// Demand signal aggregated from bookings, searches, inquiries
/// </summary>
public class DemandSignal
{
    public Guid PropertyId { get; init; }
    public DateTime Date { get; init; }
    public int SearchesReceived { get; init; }
    public int BookingsConfirmed { get; init; }
    public decimal ConversionRate { get; init; }
    public int CancellationsRequested { get; init; }
    public int ModificationsRequested { get; init; }
    public decimal AverageLeadDays { get; init; }
}
