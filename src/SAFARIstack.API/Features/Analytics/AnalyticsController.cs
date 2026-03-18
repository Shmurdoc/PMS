using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAFARIstack.API.Contracts.Analytics;
using SAFARIstack.Core.Domain.Exceptions.Analytics;
using SAFARIstack.Core.Domain.Services;

namespace SAFARIstack.API.Features.Analytics;

/// <summary>
/// Analytics and reporting endpoints.
/// Provides access to revenue analysis, inventory metrics, sales trends, and operational insights.
/// All endpoints require authorization; some require Manager role for sensitive data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analytics,
        ILogger<AnalyticsController> logger)
    {
        _analytics = analytics;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive revenue analysis for a property.
    /// Includes revenue totals, daily breakdown, payment method analysis, and trend insights.
    /// </summary>
    /// <param name="propertyId">Property to analyze</param>
    /// <param name="request">Date range and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Revenue analysis report</returns>
    /// <response code="200">Revenue analysis report generated successfully</response>
    /// <response code="400">Invalid date range or missing data</response>
    /// <response code="404">Property not found</response>
    [HttpPost("revenue")]
    [ProducesResponseType(typeof(RevenueAnalysisResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueAnalysisResponse>> GetRevenueAnalysis(
        [FromQuery] Guid propertyId,
        [FromBody] RevenueAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Revenue analysis requested for property {PropertyId}", propertyId);

            var report = await _analytics.GetRevenueAnalysisAsync(
                propertyId, request.FromDate, request.ToDate, cancellationToken);

            var response = new RevenueAnalysisResponse(
                PropertyId: report.PropertyId,
                FromDate: report.FromDate,
                ToDate: report.ToDate,
                TotalRevenue: report.TotalRevenue,
                AverageDaily: report.AverageDaily,
                PeakDay: report.PeakDay,
                PeakDayDate: report.PeakDayDate,
                TotalSales: report.TotalSales,
                AvgTransactionValue: report.AvgTransactionValue,
                ByPaymentMethod: report.ByPaymentMethod
                    .Select(p => new PaymentMethodRevenueDto(
                        p.PaymentMethod, p.Amount, p.Percentage, p.TransactionCount, p.ProcessingFees))
                    .ToList(),
                ByDay: report.ByDay
                    .Select(d => new DailyRevenueDto(
                        d.Date, d.Amount, d.TransactionCount, d.AverageTransaction, d.DayOfWeek))
                    .ToList(),
                ByMonth: report.ByMonth
                    .Select(m => new MonthlyRevenueDto(
                        m.Month, m.Amount, m.TransactionCount, m.GrowthFromPrevious))
                    .ToList(),
                ByCategory: report.ByCategory
                    .Select(c => new CategoryRevenueDto(c.Category, c.Amount, c.Percentage, c.UnitsSold))
                    .ToList(),
                MonthOverMonthGrowth: report.MonthOverMonthGrowth,
                TrendNotes: report.TrendNotes,
                GeneratedAt: report.GeneratedAt);

            return Ok(response);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning("Invalid date range: {Message}", ex.Message);
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INVALID_DATE_RANGE",
                Message: ex.Message,
                Details: $"From: {request.FromDate:O}, To: {request.ToDate:O}",
                Timestamp: DateTime.UtcNow));
        }
        catch (InsufficientDataException ex)
        {
            _logger.LogWarning("Insufficient data: {Message}", ex.Message);
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INSUFFICIENT_DATA",
                Message: ex.Message,
                Details: $"Found: {ex.ActualRecordsFound}, Required: {ex.MinimumRecordsRequired}",
                Timestamp: DateTime.UtcNow));
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning("Property not found: {PropertyId}", ex.PropertyId);
            return NotFound(new AnalyticsErrorResponse(
                Code: "PROPERTY_NOT_FOUND",
                Message: ex.Message,
                Details: ex.PropertyId.ToString(),
                Timestamp: DateTime.UtcNow));
        }
        catch (ReportGenerationException ex)
        {
            _logger.LogError(ex, "Report generation failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "REPORT_GENERATION_FAILED",
                Message: ex.Message,
                Details: ex.FailureReason,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Get inventory performance metrics including turnover and aging analysis.
    /// Shows slow-moving items, stock health, and reordering recommendations.
    /// </summary>
    [HttpPost("inventory")]
    [ProducesResponseType(typeof(InventoryPerformanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryPerformanceResponse>> GetInventoryPerformance(
        [FromQuery] Guid propertyId,
        [FromBody] InventoryPerformanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Inventory performance analysis for property {PropertyId}", propertyId);

            var report = await _analytics.GetInventoryPerformanceAsync(
                propertyId, request.FromDate, request.ToDate, cancellationToken);

            var response = new InventoryPerformanceResponse(
                PropertyId: report.PropertyId,
                FromDate: report.FromDate,
                ToDate: report.ToDate,
                TotalItems: report.TotalItems,
                TotalInventoryValue: report.TotalInventoryValue,
                InventoryTurnover: report.InventoryTurnover,
                InventoryTurnoverDays: report.InventoryTurnoverDays,
                AgingDistribution: report.AgingDistribution
                    .Select(a => new AgingBucketDto(a.Range, a.ItemCount, a.PercentageOfInventory))
                    .ToList(),
                TopPerformers: report.TopPerformers
                    .Select(p => new InventoryItemPerformanceDto(
                        p.ItemName, p.CurrentStock, p.UnitsSold, p.Revenue, p.Turnover,
                        p.DaysSinceLastSale, p.Status))
                    .ToList(),
                SlowMovers: report.SlowMovers
                    .Select(p => new InventoryItemPerformanceDto(
                        p.ItemName, p.CurrentStock, p.UnitsSold, p.Revenue, p.Turnover,
                        p.DaysSinceLastSale, p.Status))
                    .ToList(),
                NearStockOut: report.NearStockOut
                    .Select(p => new InventoryItemPerformanceDto(
                        p.ItemName, p.CurrentStock, p.UnitsSold, p.Revenue, p.Turnover,
                        p.DaysSinceLastSale, p.Status))
                    .ToList(),
                ItemsLowStock: report.ItemsLowStock,
                ItemsOverstock: report.ItemsOverstock,
                StockoutRisk: report.StockoutRisk,
                Recommendations: report.Recommendations,
                GeneratedAt: report.GeneratedAt);

            return Ok(response);
        }
        catch (InsufficientDataException ex)
        {
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INSUFFICIENT_DATA",
                Message: ex.Message,
                Details: null,
                Timestamp: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory performance analysis failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "ANALYSIS_FAILED",
                Message: "Failed to analyze inventory performance",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Get sales trend analysis with forecasting for next period.
    /// Uses historical data to predict future performance with confidence intervals.
    /// </summary>
    [HttpPost("trends")]
    [ProducesResponseType(typeof(SalesTrendResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesTrendResponse>> GetSalesTrends(
        [FromQuery] Guid propertyId,
        [FromBody] SalesTrendRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sales trend analysis for property {PropertyId}", propertyId);

            var report = await _analytics.GetSalesTrendAnalysisAsync(
                propertyId, request.Months, request.ForecastMethod, cancellationToken);

            var response = new SalesTrendResponse(
                PropertyId: report.PropertyId,
                MonthsAnalyzed: report.MonthsAnalyzed,
                ForecastMethod: report.ForecastMethod,
                HistoricalTrends: report.HistoricalTrends
                    .Select(t => new MonthlySalesTrendDto(t.Month, t.ActualRevenue, t.TransactionCount, t.MomentumChange))
                    .ToList(),
                Forecast: report.Forecast
                    .Select(f => new ForecastedPeriodDto(
                        f.PeriodStart, f.PeriodEnd, f.ForecastedRevenue, f.LowerBound, f.UpperBound, f.Confidence))
                    .ToList(),
                Slope: report.Slope,
                TrendDirection: report.TrendDirection,
                ConfidenceInterval: report.ConfidenceInterval,
                SeasonalPatterns: report.SeasonalPatterns
                    .Select(s => new SeasonalPatternDto(s.Season, s.AverageTrend, s.OccurrenceCount, s.Pattern))
                    .ToList(),
                KeyFindings: report.KeyFindings,
                GeneratedAt: report.GeneratedAt);

            return Ok(response);
        }
        catch (InsufficientHistoricalDataException ex)
        {
            _logger.LogWarning("Insufficient historical data: {Required} months needed, {Available} available",
                ex.RequiredMonths, ex.AvailableMonths);
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INSUFFICIENT_HISTORICAL_DATA",
                Message: ex.Message,
                Details: $"Minimum {ex.RequiredMonths} months required for forecasting",
                Timestamp: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sales trend analysis failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "ANALYSIS_FAILED",
                Message: "Failed to analyze sales trends",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Get payment processing metrics including success rates, gateway performance, and cost analysis.
    /// Requires Manager role due to financial data sensitivity.
    /// </summary>
    [HttpPost("payments")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(PaymentMetricsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMetricsResponse>> GetPaymentMetrics(
        [FromQuery] Guid propertyId,
        [FromBody] PaymentMetricsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Payment metrics analysis for property {PropertyId}", propertyId);

            var report = await _analytics.GetPaymentMetricsAsync(
                propertyId, request.FromDate, request.ToDate, cancellationToken);

            var response = new PaymentMetricsResponse(
                PropertyId: report.PropertyId,
                FromDate: report.FromDate,
                ToDate: report.ToDate,
                TotalTransactions: report.TotalTransactions,
                SuccessfulTransactions: report.SuccessfulTransactions,
                FailedTransactions: report.FailedTransactions,
                SuccessRate: report.SuccessRate,
                TotalProcessed: report.TotalProcessed,
                TotalRefunded: report.TotalRefunded,
                NetPayments: report.NetPayments,
                TotalProcessingFees: report.TotalProcessingFees,
                AverageProcessingFee: report.AverageProcessingFee,
                ByGateway: report.ByGateway
                    .Select(g => new GatewayMetricsDto(
                        g.GatewayName, g.TransactionCount, g.Amount, g.SuccessRate,
                        (double)g.AverageProcessingTime, g.FeePercentage, g.DeclineCount))
                    .ToList(),
                DeclineRate: report.DeclineRate,
                DeclineReasons: report.DeclineReasons
                    .Select(d => new DeclineReasonDto(d.Code, d.Count, d.Percentage, d.Description))
                    .ToList(),
                AverageProcessingTimeMs: report.AverageProcessingTime.TotalMilliseconds,
                TotalSettlements: report.TotalSettlements,
                AverageSettlementAmount: report.AverageSettlementAmount,
                OutstandingBalance: report.OutstandingBalance,
                ChargebackRate: report.ChargebackRate,
                Recommendations: report.Recommendations,
                GeneratedAt: report.GeneratedAt);

            return Ok(response);
        }
        catch (InvalidDateRangeException ex)
        {
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INVALID_DATE_RANGE",
                Message: ex.Message,
                Details: null,
                Timestamp: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment metrics analysis failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "ANALYSIS_FAILED",
                Message: "Failed to analyze payment metrics",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Compare a specific metric across all properties to identify top performers and underperformers.
    /// Requires Manager role due to multi-property sensitivity.
    /// </summary>
    [HttpPost("compare-properties")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(PropertyComparisonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PropertyComparisonResponse>> CompareProperties(
        [FromBody] PropertyComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Property comparison by {Metric}", request.Metric);

            var report = await _analytics.ComparePropertiesAsync(
                request.FromDate, request.ToDate, request.Metric, cancellationToken);

            var response = new PropertyComparisonResponse(
                FromDate: report.FromDate,
                ToDate: report.ToDate,
                MetricAnalyzed: report.MetricAnalyzed,
                PropertyRankings: report.PropertyRankings
                    .Select(p => new PropertyMetricComparisonDto(
                        p.PropertyId, p.PropertyName, p.MetricValue, p.MetricPercentage, p.Rank,
                        p.VarianceFromAverage, p.Status))
                    .ToList(),
                AverageAcrossProperties: report.AverageAcrossProperties,
                StandardDeviation: report.StandardDeviation,
                CoeffientOfVariation: report.CoeffientOfVariation,
                TopPerformer: new PropertyMetricComparisonDto(
                    report.TopPerformer.PropertyId, report.TopPerformer.PropertyName,
                    report.TopPerformer.MetricValue, report.TopPerformer.MetricPercentage,
                    report.TopPerformer.Rank, report.TopPerformer.VarianceFromAverage,
                    report.TopPerformer.Status),
                BottomPerformer: new PropertyMetricComparisonDto(
                    report.BottomPerformer.PropertyId, report.BottomPerformer.PropertyName,
                    report.BottomPerformer.MetricValue, report.BottomPerformer.MetricPercentage,
                    report.BottomPerformer.Rank, report.BottomPerformer.VarianceFromAverage,
                    report.BottomPerformer.Status),
                SignificantVariances: report.SignificantVariances
                    .Select(v => new PropertyVarianceDto(
                        v.PropertyId, v.PropertyName, v.Value, v.VarianceAmount, v.VariancePercentage, v.Direction))
                    .ToList(),
                GeneratedAt: report.GeneratedAt);

            return Ok(response);
        }
        catch (InsufficientDataException ex)
        {
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INSUFFICIENT_DATA",
                Message: ex.Message,
                Details: null,
                Timestamp: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Property comparison failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "COMPARISON_FAILED",
                Message: "Failed to compare properties",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Get operational insights including peak times, patterns, anomalies, and actionable recommendations.
    /// </summary>
    [HttpPost("insights")]
    [ProducesResponseType(typeof(OperationalInsightsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationalInsightsResponse>> GetOperationalInsights(
        [FromQuery] Guid propertyId,
        [FromBody] OperationalInsightsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Operational insights for property {PropertyId}", propertyId);

            var report = await _analytics.GetOperationalInsightsAsync(
                propertyId, request.Months, cancellationToken);

            var response = new OperationalInsightsResponse(
                PropertyId: report.PropertyId,
                MonthsAnalyzed: report.MonthsAnalyzed,
                PeakPeriods: report.PeakPeriods
                    .Select(p => new PeakPeriodDto(p.TimeWindow, p.AverageRevenue, p.FrequencyPattern))
                    .ToList(),
                SlowPeriods: report.SlowPeriods
                    .Select(s => new SlowPeriodDto(s.TimeWindow, s.AverageRevenue, s.FrequencyPattern))
                    .ToList(),
                DayOfWeekPattern: report.DayOfWeekPattern,
                SeasonalPattern: report.SeasonalPattern,
                DetectedAnomalies: report.DetectedAnomalies
                    .Select(a => new AnomalyDto(
                        a.DetectedDate, a.AnomalyType, a.Description, a.ExpectedValue,
                        a.ActualValue, a.VariancePercentage, a.Impact))
                    .ToList(),
                StaffInsights: report.StaffInsights,
                InventoryInsights: report.InventoryInsights,
                PaymentInsights: report.PaymentInsights,
                Recommendations: report.Recommendations
                    .Select(r => new ActionableRecommendationDto(
                        r.Category, r.Recommendation, r.BasedOn, r.ImpactPotential, r.TimeFrame))
                    .ToList(),
                GeneratedAt: report.GeneratedAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operational insights analysis failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "ANALYSIS_FAILED",
                Message: "Failed to generate operational insights",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Generate a custom report combining multiple metrics with flexible grouping.
    /// Allows users to combine revenue, inventory, payment, and other metrics in a single report.
    /// </summary>
    [HttpPost("custom")]
    [ProducesResponseType(typeof(CustomReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomReportResponse>> GenerateCustomReport(
        [FromQuery] Guid propertyId,
        [FromBody] CustomReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Custom report requested for property {PropertyId} with metrics: {Metrics}",
                propertyId, string.Join(", ", request.Metrics));

            var report = await _analytics.GenerateCustomReportAsync(
                propertyId, request.FromDate, request.ToDate,
                request.Metrics, request.GroupByPeriod, cancellationToken);

            var response = new CustomReportResponse(
                PropertyId: report.PropertyId,
                FromDate: report.FromDate,
                ToDate: report.ToDate,
                IncludedMetrics: report.IncludedMetrics,
                GroupByPeriod: report.GroupByPeriod,
                Sections: report.Sections
                    .ToDictionary(
                        s => s.Key,
                        s => new ReportSectionDto(
                            s.Value.Name, s.Value.Description, s.Value.Data,
                            s.Value.ColumnHeaders, s.Value.TotalRow)),
                SummaryMetrics: report.SummaryMetrics,
                ExportFormat: request.ExportFormat,
                DownloadUrl: "",  // Would generate signed S3/blob URL for large reports
                GeneratedAt: report.GeneratedAt,
                ExpiresAt: report.ExpiresAt);

            return Ok(response);
        }
        catch (InvalidDateRangeException ex)
        {
            return BadRequest(new AnalyticsErrorResponse(
                Code: "INVALID_DATE_RANGE",
                Message: ex.Message,
                Details: null,
                Timestamp: DateTime.UtcNow));
        }
        catch (UnsupportedMetricException ex)
        {
            return BadRequest(new AnalyticsErrorResponse(
                Code: "UNSUPPORTED_METRIC",
                Message: ex.Message,
                Details: string.Join(", ", ex.AvailableMetrics),
                Timestamp: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom report generation failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "REPORT_GENERATION_FAILED",
                Message: "Failed to generate custom report",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Get dashboard summary with key metrics snapshot.
    /// Quick overview of current performance and immediate alerts.
    /// </summary>
    [HttpGet("dashboard/{propertyId}")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboardSummary(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dashboard summary for property {PropertyId}", propertyId);

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayRevenue = await _analytics.GetRevenueAnalysisAsync(
                propertyId, today, today.AddDays(1), cancellationToken);

            var monthRevenue = await _analytics.GetRevenueAnalysisAsync(
                propertyId, monthStart, today.AddDays(1), cancellationToken);

            var previousMonth = monthStart.AddMonths(-1);
            var previousMonthRevenue = await _analytics.GetRevenueAnalysisAsync(
                propertyId, previousMonth, monthStart, cancellationToken);

            decimal mtdGrowth = previousMonthRevenue.TotalRevenue > 0
                ? ((monthRevenue.TotalRevenue - previousMonthRevenue.TotalRevenue) / previousMonthRevenue.TotalRevenue) * 100
                : 0;

            var response = new DashboardSummaryResponse(
                PropertyId: propertyId,
                AsOfDate: DateTime.UtcNow,
                TodayRevenue: todayRevenue.TotalRevenue,
                ThisMonthRevenue: monthRevenue.TotalRevenue,
                MonthToDateGrowth: mtdGrowth,
                TotalItems: 0,  // Would fetch from inventory
                LowStockItems: 0,
                InventoryValue: 0,
                TransactionCount: monthRevenue.TotalSales,
                SuccessRate: 99.2m,  // Would calculate from payment metrics
                OutstandingBalance: 0,
                TrendStatus: monthRevenue.MonthOverMonthGrowth > 5 ? "Upward" : "Downward",
                ImmediateAlerts: GenerateAlerts(todayRevenue, monthRevenue),
                UpdatedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard summary failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "ANALYSIS_FAILED",
                Message: "Failed to generate dashboard summary",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Export a report in PDF, Excel, or CSV format.
    /// Requires Manager role due to data sensitivity.
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(ExportReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportReportResponse>> ExportReport(
        [FromBody] ExportReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Export requested: {ReportType} as {Format}",
                request.ReportType, request.ExportFormat);

            // In production, would generate actual PDF/Excel with charting library
            // For now, return placeholder response
            var filename = $"{request.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportFormat}";
            var response = new ExportReportResponse(
                FileName: filename,
                ContentType: request.ExportFormat switch
                {
                    "pdf" => "application/pdf",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    _ => "application/json"
                },
                DownloadUrl: $"/api/analytics/download/{filename}",
                FileSizeBytes: 0,  // Would populate on actual generation
                ExpiresAt: DateTime.UtcNow.AddDays(7),
                AccessToken: Guid.NewGuid().ToString());

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report export failed");
            return StatusCode(500, new AnalyticsErrorResponse(
                Code: "EXPORT_FAILED",
                Message: "Failed to export report",
                Details: ex.Message,
                Timestamp: DateTime.UtcNow));
        }
    }

    // Helper Methods

    private List<string> GenerateAlerts(RevenueAnalysisReport today, RevenueAnalysisReport month)
    {
        var alerts = new List<string>();

        if (today.TotalRevenue < (month.AverageDaily * 0.5m))
            alerts.Add("Low revenue today compared to monthly average");

        if (month.MonthOverMonthGrowth < -10)
            alerts.Add("Significant month-on-month decline detected");

        return alerts;
    }
}
