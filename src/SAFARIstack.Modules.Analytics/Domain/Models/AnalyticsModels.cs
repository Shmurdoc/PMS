namespace SAFARIstack.Modules.Analytics.Domain.Events;

/// <summary>
/// Represents occupancy forecast for a property
/// </summary>
public class OccupancyForecast
{
    public Guid PropertyId { get; init; }
    public DateTime ForecastDate { get; init; }
    public int DaysAhead { get; init; }
    public decimal PredictedOccupancyRate { get; init; } // 0.0 - 1.0
    public int PredictedBookedRooms { get; init; }
    public int TotalAvailableRooms { get; init; }
    public decimal Confidence { get; init; } // 0.0 - 1.0
    public string[] InfluencingFactors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents revenue forecast for a property
/// </summary>
public class RevenueForecast
{
    public Guid PropertyId { get; init; }
    public DateTime ForecastDate { get; init; }
    public DateRange Period { get; init; }
    public decimal PredictedRevenue { get; init; }
    public decimal AvgDailyRate { get; init; }
    public decimal RevPAR { get; init; } // Revenue Per Available Room
    public decimal Confidence { get; init; }
    public Dictionary<string, decimal> RevenueBySource { get; init; } = new();
}

/// <summary>
/// Represents guest behavior profile (anonymized)
/// </summary>
public class GuestBehaviorProfile
{
    public Guid GuestSegmentId { get; init; } // Anonymized ID
    public int TotalStays { get; init; }
    public int AverageStayLength { get; init; }
    public decimal AverageSpend { get; init; }
    public string[] PreferredRoomTypes { get; init; } = Array.Empty<string>();
    public string[] ServicePreferences { get; init; } = Array.Empty<string>();
    public int BookingLeadDays { get; init; }
    public bool IsRepeatingGuest { get; init; }
    public string GuestSegment { get; init; } = "Standard"; // VIP, Corporate, Leisure, etc.
}

/// <summary>
/// Date range value object
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalDays => (EndDate.Date - StartDate.Date).Days;

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>
/// Analytics report definition (metadata-driven)
/// </summary>
public class ReportDefinition
{
    public Guid ReportId { get; init; } = Guid.NewGuid();
    public Guid PropertyId { get; init; }
    public string ReportName { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty; // "Operational", "Financial", "Compliance"
    public Dictionary<string, object> FilterCriteria { get; init; } = new();
    public string[] MetricsToInclude { get; init; } = Array.Empty<string>();
    public string[] DimensionsToGroup { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public bool IsScheduled { get; init; }
    public string? ScheduleCron { get; init; }
}

/// <summary>
/// Generated report instance
/// </summary>
public class GeneratedReport
{
    public Guid ReportInstanceId { get; init; } = Guid.NewGuid();
    public Guid ReportDefinitionId { get; init; }
    public Guid PropertyId { get; init; }
    public DateTime GeneratedAt { get; init; }
    public DateRange CoveragePeriod { get; init; }
    public Dictionary<string, object> ReportData { get; init; } = new();
    public decimal ExecutionTimeMs { get; init; }
    public string? DataFormat { get; init; } // "JSON", "CSV", "PDF"
}
