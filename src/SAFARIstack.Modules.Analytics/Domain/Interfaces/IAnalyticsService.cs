namespace SAFARIstack.Modules.Analytics.Domain.Interfaces;

using SAFARIstack.Modules.Analytics.Domain.Events;

/// <summary>
/// Contract for analytics services - implemented by Analytics module, consumed by others
/// Maintains loose coupling through interfaces rather than direct database access
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Record guest interaction for behavior analysis (anonymized)
    /// </summary>
    Task RecordGuestInteraction(Guid guestSegmentId, string interactionType, Dictionary<string, object> metadata, CancellationToken ct = default);

    /// <summary>
    /// Get occupancy forecast for property
    /// </summary>
    Task<OccupancyForecast> GetOccupancyForecast(Guid propertyId, int daysAhead, CancellationToken ct = default);

    /// <summary>
    /// Get revenue forecast for property
    /// </summary>
    Task<RevenueForecast> GetRevenueForecast(Guid propertyId, DateRange period, CancellationToken ct = default);

    /// <summary>
    /// Get guest behavior profile (anonymized, POPIA compliant)
    /// </summary>
    Task<GuestBehaviorProfile?> GetGuestBehaviorProfile(Guid guestSegmentId, CancellationToken ct = default);

    /// <summary>
    /// Generate custom report based on definition
    /// </summary>
    Task<GeneratedReport> GenerateReport(ReportDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// Get pre-built operational report
    /// </summary>
    Task<GeneratedReport> GetOperationalReport(Guid propertyId, DateRange period, CancellationToken ct = default);

    /// <summary>
    /// Get pre-built financial report
    /// </summary>
    Task<GeneratedReport> GetFinancialReport(Guid propertyId, DateRange period, CancellationToken ct = default);

    /// <summary>
    /// Get compliance report (SARS, B-BBEE, POPIA)
    /// </summary>
    Task<GeneratedReport> GetComplianceReport(Guid propertyId, DateRange period, CancellationToken ct = default);
}

/// <summary>
/// Contract for report builder - metadata-driven report construction
/// </summary>
public interface IReportBuilder
{
    IReportBuilder ForProperty(Guid propertyId);
    IReportBuilder OfType(string reportType);
    IReportBuilder WithPeriod(DateRange period);
    IReportBuilder WithMetrics(params string[] metrics);
    IReportBuilder GroupBy(params string[] dimensions);
    IReportBuilder Filter(string filterName, object filterValue);
    Task<GeneratedReport> BuildAsync(CancellationToken ct = default);
}

/// <summary>
/// Contract for predictive analytics engine
/// </summary>
public interface IPredictiveAnalytics
{
    /// <summary>
    /// Forecast occupancy using historical data + seasonality
    /// </summary>
    Task<OccupancyForecast> PredictOccupancy(Guid propertyId, int daysAhead, CancellationToken ct = default);

    /// <summary>
    /// Forecast revenue based on demand signals + historical rates
    /// </summary>
    Task<RevenueForecast> PredictRevenue(Guid propertyId, DateRange period, CancellationToken ct = default);

    /// <summary>
    /// Analyze seasonal patterns
    /// </summary>
    Task<Dictionary<string, decimal>> AnalyzeSeasonality(Guid propertyId, int yearsOfHistory = 2, CancellationToken ct = default);
}

/// <summary>
/// Contract for guest behavior analysis (anonymized)
/// </summary>
public interface IGuestBehaviorAnalytics
{
    /// <summary>
    /// Record guest interaction (booking, service usage, spending)
    /// All PII removed - only aggregate behavior tracked
    /// </summary>
    Task RecordInteraction(Guid guestSegmentId, string eventType, Dictionary<string, object> data, CancellationToken ct = default);

    /// <summary>
    /// Segment guests by behavior
    /// </summary>
    Task<Dictionary<string, int>> SegmentGuests(Guid propertyId, CancellationToken ct = default);

    /// <summary>
    /// Get profile for guest segment (anonymized, POPIA compliant)
    /// </summary>
    Task<GuestBehaviorProfile?> GetSegmentProfile(Guid guestSegmentId, CancellationToken ct = default);

    /// <summary>
    /// Identify high-value guest segments
    /// </summary>
    Task<IEnumerable<GuestBehaviorProfile>> GetHighValueSegments(Guid propertyId, int topCount = 10, CancellationToken ct = default);
}
