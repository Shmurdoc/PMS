namespace SAFARIstack.API.Contracts.Analytics;

/// <summary>
/// Request for revenue analysis report.
/// </summary>
public record RevenueAnalysisRequest(
    DateTime FromDate,
    DateTime ToDate,
    bool IncludeByPaymentMethod = true,
    bool IncludeByCategory = true);

/// <summary>
/// Response containing revenue analysis data.
/// </summary>
public record RevenueAnalysisResponse(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalRevenue,
    decimal AverageDaily,
    decimal PeakDay,
    DateTime PeakDayDate,
    int TotalSales,
    decimal AvgTransactionValue,
    List<PaymentMethodRevenueDto> ByPaymentMethod,
    List<DailyRevenueDto> ByDay,
    List<MonthlyRevenueDto> ByMonth,
    List<CategoryRevenueDto> ByCategory,
    decimal MonthOverMonthGrowth,
    List<string> TrendNotes,
    DateTime GeneratedAt);

public record PaymentMethodRevenueDto(
    string PaymentMethod,
    decimal Amount,
    decimal Percentage,
    int TransactionCount,
    decimal ProcessingFees);

public record DailyRevenueDto(
    DateTime Date,
    decimal Amount,
    int TransactionCount,
    decimal AverageTransaction,
    string DayOfWeek);

public record MonthlyRevenueDto(
    DateTime Month,
    decimal Amount,
    int TransactionCount,
    decimal GrowthFromPrevious);

public record CategoryRevenueDto(
    string Category,
    decimal Amount,
    decimal Percentage,
    int UnitsSold);

/// <summary>
/// Request for inventory performance analysis.
/// </summary>
public record InventoryPerformanceRequest(
    DateTime FromDate,
    DateTime ToDate,
    int? TopItemsCount = 10,
    int? SlowMoversCount = 10);

/// <summary>
/// Response with inventory metrics and performance data.
/// </summary>
public record InventoryPerformanceResponse(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    int TotalItems,
    decimal TotalInventoryValue,
    decimal InventoryTurnover,
    decimal InventoryTurnoverDays,
    List<AgingBucketDto> AgingDistribution,
    List<InventoryItemPerformanceDto> TopPerformers,
    List<InventoryItemPerformanceDto> SlowMovers,
    List<InventoryItemPerformanceDto> NearStockOut,
    int ItemsLowStock,
    int ItemsOverstock,
    decimal StockoutRisk,
    List<string> Recommendations,
    DateTime GeneratedAt);

public record AgingBucketDto(
    string Range,
    int ItemCount,
    decimal PercentageOfInventory);

public record InventoryItemPerformanceDto(
    string ItemName,
    decimal CurrentStock,
    decimal UnitsSold,
    decimal Revenue,
    decimal Turnover,
    int DaysSinceLastSale,
    string Status);

/// <summary>
/// Request for sales trend analysis with forecasting.
/// </summary>
public record SalesTrendRequest(
    int Months = 6,
    string ForecastMethod = "exponential");  // linear, exponential, seasonal

/// <summary>
/// Response with historical trends and forecasted data.
/// </summary>
public record SalesTrendResponse(
    Guid PropertyId,
    int MonthsAnalyzed,
    string ForecastMethod,
    List<MonthlySalesTrendDto> HistoricalTrends,
    List<ForecastedPeriodDto> Forecast,
    decimal Slope,
    string TrendDirection,  // Upward, Downward, Stable, Seasonal
    decimal ConfidenceInterval,
    List<SeasonalPatternDto> SeasonalPatterns,
    List<string> KeyFindings,
    DateTime GeneratedAt);

public record MonthlySalesTrendDto(
    DateTime Month,
    decimal ActualRevenue,
    int TransactionCount,
    decimal MomentumChange);

public record ForecastedPeriodDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal ForecastedRevenue,
    decimal LowerBound,
    decimal UpperBound,
    string Confidence);

public record SeasonalPatternDto(
    string Season,
    decimal AverageTrend,
    int OccurrenceCount,
    string Pattern);

/// <summary>
/// Request for payment metrics analysis.
/// </summary>
public record PaymentMetricsRequest(
    DateTime FromDate,
    DateTime ToDate,
    bool IncludeGatewayMetrics = true,
    bool IncludeDeclineAnalysis = true);

/// <summary>
/// Response with payment processing metrics.
/// </summary>
public record PaymentMetricsResponse(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    int TotalTransactions,
    int SuccessfulTransactions,
    int FailedTransactions,
    decimal SuccessRate,
    decimal TotalProcessed,
    decimal TotalRefunded,
    decimal NetPayments,
    decimal TotalProcessingFees,
    decimal AverageProcessingFee,
    List<GatewayMetricsDto> ByGateway,
    decimal DeclineRate,
    List<DeclineReasonDto> DeclineReasons,
    double AverageProcessingTimeMs,
    int TotalSettlements,
    decimal AverageSettlementAmount,
    decimal OutstandingBalance,
    decimal ChargebackRate,
    List<string> Recommendations,
    DateTime GeneratedAt);

public record GatewayMetricsDto(
    string GatewayName,
    int TransactionCount,
    decimal Amount,
    decimal SuccessRate,
    double AverageProcessingTimeMs,
    decimal FeePercentage,
    int DeclineCount);

public record DeclineReasonDto(
    string Code,
    int Count,
    decimal Percentage,
    string Description);

/// <summary>
/// Request for comparing properties.
/// </summary>
public record PropertyComparisonRequest(
    DateTime FromDate,
    DateTime ToDate,
    string Metric = "revenue");  // revenue, occupancy, turnover, payment_health

/// <summary>
/// Response with property comparison data.
/// </summary>
public record PropertyComparisonResponse(
    DateTime FromDate,
    DateTime ToDate,
    string MetricAnalyzed,
    List<PropertyMetricComparisonDto> PropertyRankings,
    decimal AverageAcrossProperties,
    decimal StandardDeviation,
    decimal CoeffientOfVariation,
    PropertyMetricComparisonDto TopPerformer,
    PropertyMetricComparisonDto BottomPerformer,
    List<PropertyVarianceDto> SignificantVariances,
    DateTime GeneratedAt);

public record PropertyMetricComparisonDto(
    Guid PropertyId,
    string PropertyName,
    decimal MetricValue,
    decimal MetricPercentage,
    int Rank,
    decimal VarianceFromAverage,
    string Status);

public record PropertyVarianceDto(
    Guid PropertyId,
    string PropertyName,
    decimal Value,
    decimal VarianceAmount,
    decimal VariancePercentage,
    string Direction);

/// <summary>
/// Request for operational insights.
/// </summary>
public record OperationalInsightsRequest(
    int Months = 12);

/// <summary>
/// Response with insights and recommendations.
/// </summary>
public record OperationalInsightsResponse(
    Guid PropertyId,
    int MonthsAnalyzed,
    List<PeakPeriodDto> PeakPeriods,
    List<SlowPeriodDto> SlowPeriods,
    string DayOfWeekPattern,
    string SeasonalPattern,
    List<AnomalyDto> DetectedAnomalies,
    List<string> StaffInsights,
    List<string> InventoryInsights,
    List<string> PaymentInsights,
    List<ActionableRecommendationDto> Recommendations,
    DateTime GeneratedAt);

public record PeakPeriodDto(
    string TimeWindow,
    decimal AverageRevenue,
    string FrequencyPattern);

public record SlowPeriodDto(
    string TimeWindow,
    decimal AverageRevenue,
    string FrequencyPattern);

public record AnomalyDto(
    DateTime DetectedDate,
    string AnomalyType,
    string Description,
    decimal ExpectedValue,
    decimal ActualValue,
    decimal VariancePercentage,
    string Impact);

public record ActionableRecommendationDto(
    string Category,
    string Recommendation,
    string BasedOn,
    string ImpactPotential,
    string TimeFrame);

/// <summary>
/// Request for custom report generation.
/// </summary>
public record CustomReportRequest(
    DateTime FromDate,
    DateTime ToDate,
    List<string> Metrics,  // revenue, inventory, payments, staff, etc.
    string GroupByPeriod = "daily",  // daily, weekly, monthly
    string ExportFormat = "json");  // json, pdf, excel

/// <summary>
/// Response with custom report data.
/// </summary>
public record CustomReportResponse(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    List<string> IncludedMetrics,
    string GroupByPeriod,
    Dictionary<string, ReportSectionDto> Sections,
    Dictionary<string, decimal> SummaryMetrics,
    string ExportFormat,
    string DownloadUrl,  // For PDF/Excel exports
    DateTime GeneratedAt,
    DateTime ExpiresAt);

public record ReportSectionDto(
    string Name,
    string Description,
    List<Dictionary<string, object>> Data,
    List<string> ColumnHeaders,
    Dictionary<string, decimal> TotalRow);

/// <summary>
/// Error response for analytics operations.
/// </summary>
public record AnalyticsErrorResponse(
    string Code,
    string Message,
    string Details,
    DateTime Timestamp);

/// <summary>
/// Dashboard summary response combining key metrics.
/// </summary>
public record DashboardSummaryResponse(
    Guid PropertyId,
    DateTime AsOfDate,
    
    // Revenue snapshot
    decimal TodayRevenue,
    decimal ThisMonthRevenue,
    decimal MonthToDateGrowth,
    
    // Inventory snapshot
    int TotalItems,
    int LowStockItems,
    decimal InventoryValue,
    
    // Payment snapshot
    int TransactionCount,
    decimal SuccessRate,
    decimal OutstandingBalance,
    
    // Operational snapshot
    string TrendStatus,  // "Upward", "Downward", "Stable"
    List<string> ImmediateAlerts,
    
    DateTime UpdatedAt);

/// <summary>
/// Export request for generating reports in specific formats.
/// </summary>
public record ExportReportRequest(
    string ReportType,  // revenue, inventory, trends, payments, etc.
    string ExportFormat,  // pdf, excel, csv, json
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// Export response with download URL and metadata.
/// </summary>
public record ExportReportResponse(
    string FileName,
    string ContentType,
    string DownloadUrl,
    long FileSizeBytes,
    DateTime ExpiresAt,
    string AccessToken);  // For secure download link
