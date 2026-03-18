namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// Service for analytics and reporting across POS, Inventory, and Payment systems.
/// Integrates data from Phase 5 (POS & Inventory) and Phase 6 (Payments).
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Generate comprehensive revenue analysis report.
    /// </summary>
    /// <param name="propertyId">Property to analyze</param>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Revenue analysis with trends, breakdown by payment method, property comparison</returns>
    Task<RevenueAnalysisReport> GetRevenueAnalysisAsync(
        Guid propertyId, DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze inventory performance: turnover, slow-moving items, stock health.
    /// </summary>
    /// <param name="propertyId">Property to analyze</param>
    /// <param name="fromDate">Start date for transaction analysis</param>
    /// <param name="toDate">End date for transaction analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory metrics with aging analysis and recommendations</returns>
    Task<InventoryPerformanceReport> GetInventoryPerformanceAsync(
        Guid propertyId, DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate sales trend analysis with forecasting.
    /// </summary>
    /// <param name="propertyId">Property to analyze</param>
    /// <param name="months">Number of months to analyze (typically 3-12)</param>
    /// <param name="forecaster">Forecasting method: linear, exponential, seasonal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trend data with predicted values for next period</returns>
    Task<SalesTrendReport> GetSalesTrendAnalysisAsync(
        Guid propertyId, int months = 6, string forecaster = "exponential",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze payment processing metrics and gateway performance.
    /// </summary>
    /// <param name="propertyId">Property to analyze</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment metrics with success rates, average processing time, cost analysis</returns>
    Task<PaymentMetricsReport> GetPaymentMetricsAsync(
        Guid propertyId, DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare performance across all properties.
    /// </summary>
    /// <param name="fromDate">Start date for comparison</param>
    /// <param name="toDate">End date for comparison</param>
    /// <param name="metric">Metric to compare: revenue, occupancy, turnover</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance comparison with rankings and variances</returns>
    Task<PropertyComparisonReport> ComparePropertiesAsync(
        DateTime fromDate, DateTime toDate, string metric = "revenue",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identify operational insights: peak times, seasonal patterns, anomalies.
    /// </summary>
    /// <param name="propertyId">Property to analyze</param>
    /// <param name="months">Historical months to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Insights with actionable recommendations</returns>
    Task<OperationalInsightsReport> GetOperationalInsightsAsync(
        Guid propertyId, int months = 12,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate custom report by combining multiple metrics.
    /// </summary>
    /// <param name="propertyId">Property to report on</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="metrics">Metrics to include: revenue, inventory, payments, staff</param>
    /// <param name="groupBy">Grouping: daily, weekly, monthly</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Combined report with all requested metrics</returns>
    Task<CustomReport> GenerateCustomReportAsync(
        Guid propertyId, DateTime fromDate, DateTime toDate,
        List<string> metrics, string groupBy = "daily",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Revenue analysis with breakdown by month, payment method, room/product type.
/// Integrates data from PaymentChargeRecord and CasualSaleRecord.
/// </summary>
public record RevenueAnalysisReport(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalRevenue,  // All charges minus refunds
    decimal AverageDaily,
    decimal PeakDay,
    DateTime PeakDayDate,
    int TotalSales,  // Count of charges and sales
    decimal AvgTransactionValue,
    
    // Payment method breakdown
    List<PaymentMethodRevenue> ByPaymentMethod,  // Card, Bank, Cash, etc.
    
    // Temporal breakdown
    List<DailyRevenue> ByDay,
    List<MonthlyRevenue> ByMonth,
    
    // Product/Room type breakdown
    List<CategoryRevenue> ByCategory,
    
    // Key metrics
    decimal MonthOverMonthGrowth,  // Percentage
    List<string> TrendNotes,  // Insights: "Peak season March", "Winter decline", etc.
    
    DateTime GeneratedAt);

public record PaymentMethodRevenue(
    string PaymentMethod,  // card, bank_transfer, wallet, cash
    decimal Amount,
    decimal Percentage,
    int TransactionCount,
    decimal ProcessingFees);

public record DailyRevenue(
    DateTime Date,
    decimal Amount,
    int TransactionCount,
    decimal AverageTransaction,
    string DayOfWeek);

public record MonthlyRevenue(
    DateTime Month,
    decimal Amount,
    int TransactionCount,
    decimal GrowthFromPrevious);

public record CategoryRevenue(
    string Category,  // Room type, Product category
    decimal Amount,
    decimal Percentage,
    int UnitsSold);

/// <summary>
/// Inventory performance including turnover, aging, stock health.
/// Integrates InventoryItem and transaction data from Phase 5.
/// </summary>
public record InventoryPerformanceReport(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    
    // Overall metrics
    int TotalItems,
    decimal TotalInventoryValue,
    decimal InventoryTurnover,  // Times inventory sold and replaced
    decimal InventoryTurnoverDays,  // Average days to sell one unit
    
    // Aging analysis (days since last sale)
    List<AgingBucket> AgingDistribution,
    
    // Performance tiers
    List<InventoryItemPerformance> TopPerformers,  // Top 10 by revenue
    List<InventoryItemPerformance> SlowMovers,  // Bottom 10 by turnover
    List<InventoryItemPerformance> NearStockOut,  // Low quantity
    
    // Stock health
    int ItemsLowStock,
    int ItemsOverstock,
    decimal StockoutRisk,  // Percentage
    
    // Recommendations
    List<string> Recommendations,  // "Order more X", "Clear excess Y", etc.
    
    DateTime GeneratedAt);

public record AgingBucket(
    string Range,  // 0-30 days, 31-60 days, etc.
    int ItemCount,
    decimal PercentageOfInventory);

public record InventoryItemPerformance(
    string ItemName,
    decimal CurrentStock,
    decimal UnitsSold,
    decimal Revenue,
    decimal Turnover,  // Times sold
    int DaysSinceLastSale,
    string Status);  // Good, Concerning, Slow Moving, Overstocked

/// <summary>
/// Sales trends with forecasting for next period.
/// Uses historical data to predict future performance.
/// </summary>
public record SalesTrendReport(
    Guid PropertyId,
    int MonthsAnalyzed,
    string ForecastMethod,  // linear, exponential, seasonal
    
    // Historical data
    List<MonthlySalesTrend> HistoricalTrends,
    
    // Forecasted values
    List<ForecastedPeriod> Forecast,  // Next 1-3 months predicted
    
    // Trend analysis
    decimal Slope,  // Positive = growth, negative = decline
    string TrendDirection,  // Upward, Downward, Stable, Seasonal
    decimal ConfidenceInterval,  // 0.85 = 85% confidence
    
    // Seasonal analysis (if applicable)
    List<SeasonalPattern> SeasonalPatterns,
    
    // Key findings
    List<string> KeyFindings,
    
    DateTime GeneratedAt);

public record MonthlySalesTrend(
    DateTime Month,
    decimal ActualRevenue,
    int TransactionCount,
    decimal MomentumChange);  // Percentage change from previous

public record ForecastedPeriod(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal ForecastedRevenue,
    decimal LowerBound,  // Confidence interval lower bound
    decimal UpperBound,  // Confidence interval upper bound
    string Confidence);  // High, Medium, Low

public record SeasonalPattern(
    string Season,
    decimal AverageTrend,
    int OccurrenceCount,
    string Pattern);  // "Peak in March & April", "Winter slump", etc.

/// <summary>
/// Payment processing metrics and gateway performance analysis.
/// Integrates PaymentChargeRecord and PaymentReconciliationReport data.
/// </summary>
public record PaymentMetricsReport(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    
    // Volume metrics
    int TotalTransactions,
    int SuccessfulTransactions,
    int FailedTransactions,
    decimal SuccessRate,  // Percentage: 99.2%
    
    // Revenue metrics
    decimal TotalProcessed,
    decimal TotalRefunded,
    decimal NetPayments,
    decimal TotalProcessingFees,
    decimal AverageProcessingFee,  // Percentage
    
    // Gateway performance
    List<GatewayMetrics> ByGateway,  // Stripe, Square, PayFast
    
    // Decline analysis
    decimal DeclineRate,
    List<DeclineReason> DeclineReasons,  // Most common decline codes
    
    // Processing time
    TimeSpan AverageProcessingTime,
    TimeSpan MaxProcessingTime,
    
    // Settlement
    int TotalSettlements,
    decimal AverageSettlementAmount,
    decimal OutstandingBalance,
    
    // Risk indicators
    decimal ChargebackRate,
    decimal FraudDetectionRate,
    
    // Recommendations
    List<string> Recommendations,  // "Consider gateway X for lower fees", etc.
    
    DateTime GeneratedAt);

public record GatewayMetrics(
    string GatewayName,  // Stripe, Square, PayFast
    int TransactionCount,
    decimal Amount,
    decimal SuccessRate,
    decimal AverageProcessingTime,  // Milliseconds
    decimal FeePercentage,
    int DeclineCount);

public record DeclineReason(
    string Code,  // insufficient_funds, lost_card, etc.
    int Count,
    decimal Percentage,
    string Description);

/// <summary>
/// Compare performance metrics across all properties.
/// Shows which properties perform best and identify underperformers.
/// </summary>
public record PropertyComparisonReport(
    DateTime FromDate,
    DateTime ToDate,
    string MetricAnalyzed,  // revenue, occupancy, turnover, payment_health
    
    // Property rankings
    List<PropertyMetricComparison> PropertyRankings,
    
    // Aggregate statistics
    decimal AverageAcrossProperties,
    decimal StandardDeviation,
    decimal CoeffientOfVariation,  // 0.15 = 15% variation
    
    // Leaders and laggards
    PropertyMetricComparison TopPerformer,
    PropertyMetricComparison BottomPerformer,
    
    // Variance analysis
    List<PropertyVariance> SignificantVariances,  // Properties significantly above/below average
    
    DateTime GeneratedAt);

public record PropertyMetricComparison(
    Guid PropertyId,
    string PropertyName,
    decimal MetricValue,
    decimal MetricPercentage,
    int Rank,  // 1 = best
    decimal VarianceFromAverage,
    string Status);  // Above Average, Average, Below Average

public record PropertyVariance(
    Guid PropertyId,
    string PropertyName,
    decimal Value,
    decimal VarianceAmount,
    decimal VariancePercentage,
    string Direction);  // Above, Below

/// <summary>
/// Operational insights including peak times, patterns, and anomalies.
/// </summary>
public record OperationalInsightsReport(
    Guid PropertyId,
    int MonthsAnalyzed,
    
    // Peak analysis
    List<PeakPeriod> PeakPeriods,  // Times with highest activity
    List<SlowPeriod> SlowPeriods,  // Times with lowest activity
    
    // Patterns
    string DayOfWeekPattern,  // "Friday-Saturday peak", "Sunday slowest", etc.
    string SeasonalPattern,  // "Summer peak April-August", etc.
    
    // Anomalies
    List<Anomaly> DetectedAnomalies,  // Unusual events or patterns
    
    // Staff efficiency (if available)
    List<string> StaffInsights,
    
    // Inventory insights
    List<string> InventoryInsights,
    
    // Payment insights
    List<string> PaymentInsights,
    
    // Actionable recommendations
    List<ActionableRecommendation> Recommendations,
    
    DateTime GeneratedAt);

public record PeakPeriod(
    string TimeWindow,  // "Friday 18:00-22:00", "July", etc.
    decimal AverageRevenue,
    string FrequencyPattern);

public record SlowPeriod(
    string TimeWindow,
    decimal AverageRevenue,
    string FrequencyPattern);

public record Anomaly(
    DateTime DetectedDate,
    string AnomalyType,  // Spike, Drop, Unusual Pattern
    string Description,
    decimal ExpectedValue,
    decimal ActualValue,
    decimal VariancePercentage,
    string Impact);  // High, Medium, Low

public record ActionableRecommendation(
    string Category,  // Operations, Staffing, Marketing, Pricing
    string Recommendation,
    string BasedOn,  // What data led to this
    string ImpactPotential,  // High, Medium, Low
    string TimeFrame);  // Immediate, Short-term, Long-term

/// <summary>
/// Custom report combining multiple metrics and grouping options.
/// </summary>
public record CustomReport(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    List<string> IncludedMetrics,
    string GroupByPeriod,  // daily, weekly, monthly
    
    // Flexible data structure for included metrics
    Dictionary<string, ReportSection> Sections,
    
    // Summary statistics
    Dictionary<string, decimal> SummaryMetrics,
    
    // Export metadata
    string ExportFormat,  // pdf, excel, json
    DateTime GeneratedAt,
    DateTime ExpiresAt);  // Report expires in 30 days

public record ReportSection(
    string Name,
    string Description,
    List<Dictionary<string, object>> Data,
    List<string> ColumnHeaders,
    Dictionary<string, decimal> TotalRow);
