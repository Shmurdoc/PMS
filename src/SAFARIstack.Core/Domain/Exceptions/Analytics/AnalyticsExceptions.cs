using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.Core.Domain.Exceptions.Analytics;

/// <summary>
/// Base exception for analytics and reporting errors.
/// </summary>
public class AnalyticsException : DomainException
{
    public AnalyticsException(string message)
        : base(message)
    {
    }

    public override string ErrorCode => "ANALYTICS_ERROR";
    public override int StatusCode => 400;
}

/// <summary>
/// Thrown when insufficient data is available to generate report.
/// </summary>
public class InsufficientDataException : DomainException
{
    public DateTime RequiredFromDate { get; }
    public DateTime RequiredToDate { get; }
    public int MinimumRecordsRequired { get; }
    public int ActualRecordsFound { get; }

    public InsufficientDataException(
        DateTime fromDate, DateTime toDate,
        int minimumRequired, int actualFound)
        : base($"Insufficient data for analytics. Required: {minimumRequired}, Found: {actualFound}")
    {
        RequiredFromDate = fromDate;
        RequiredToDate = toDate;
        MinimumRecordsRequired = minimumRequired;
        ActualRecordsFound = actualFound;
    }

    public override string ErrorCode => "INSUFFICIENT_DATA";
    public override int StatusCode => 400;
}

/// <summary>
/// Thrown when invalid date range is provided.
/// </summary>
public class InvalidDateRangeException : DomainException
{
    public DateTime FromDate { get; }
    public DateTime ToDate { get; }

    public InvalidDateRangeException(DateTime fromDate, DateTime toDate)
        : base($"Invalid date range: from {fromDate:O} must be before to {toDate:O}")
    {
        FromDate = fromDate;
        ToDate = toDate;
    }

    public override string ErrorCode => "INVALID_DATE_RANGE";
    public override int StatusCode => 400;
}

/// <summary>
/// Thrown when requested metric is not supported or not available.
/// </summary>
public class UnsupportedMetricException : DomainException
{
    public string RequestedMetric { get; }
    public List<string> AvailableMetrics { get; }

    public UnsupportedMetricException(string metric, List<string> available)
        : base($"Metric '{metric}' is not supported. Available: {string.Join(", ", available)}")
    {
        RequestedMetric = metric;
        AvailableMetrics = available;
    }

    public override string ErrorCode => "UNSUPPORTED_METRIC";
    public override int StatusCode => 400;
}

/// <summary>
/// Thrown when property is not found or inaccessible.
/// </summary>
public class PropertyNotFoundException : DomainException
{
    public Guid PropertyId { get; }

    public PropertyNotFoundException(Guid propertyId)
        : base($"Property {propertyId} not found or not accessible")
    {
        PropertyId = propertyId;
    }

    public override string ErrorCode => "PROPERTY_NOT_FOUND";
    public override int StatusCode => 404;
}

/// <summary>
/// Thrown when report generation fails due to data issues.
/// </summary>
public class ReportGenerationException : DomainException
{
    public string ReportType { get; }
    public string FailureReason { get; }

    public ReportGenerationException(string reportType, string reason)
        : base($"Failed to generate {reportType} report: {reason}")
    {
        ReportType = reportType;
        FailureReason = reason;
    }

    public override string ErrorCode => "REPORT_GENERATION_FAILED";
    public override int StatusCode => 500;
}

/// <summary>
/// Thrown when forecasting cannot be completed due to insufficient historical data.
/// </summary>
public class InsufficientHistoricalDataException : DomainException
{
    public int RequiredMonths { get; }
    public int AvailableMonths { get; }

    public InsufficientHistoricalDataException(int required, int available)
        : base($"Forecasting requires {required} months of data, but only {available} months available")
    {
        RequiredMonths = required;
        AvailableMonths = available;
    }

    public override string ErrorCode => "INSUFFICIENT_HISTORICAL_DATA";
    public override int StatusCode => 400;
}

/// <summary>
/// Thrown when analytics service is temporarily unavailable.
/// </summary>
public class AnalyticsServiceUnavailableException : DomainException
{
    public string ServiceName { get; }

    public AnalyticsServiceUnavailableException(string serviceName)
        : base($"Analytics service '{serviceName}' is temporarily unavailable")
    {
        ServiceName = serviceName;
    }

    public override string ErrorCode => "ANALYTICS_SERVICE_UNAVAILABLE";
    public override int StatusCode => 503;
}

/// <summary>
/// Thrown when report export fails.
/// </summary>
public class ReportExportException : DomainException
{
    public string ExportFormat { get; }

    public ReportExportException(string format, string reason)
        : base($"Failed to export report as {format}: {reason}")
    {
        ExportFormat = format;
    }

    public override string ErrorCode => "EXPORT_FAILED";
    public override int StatusCode => 500;
}
