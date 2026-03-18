namespace SAFARIstack.Modules.Analytics;

using Microsoft.Extensions.DependencyInjection;
using SAFARIstack.Modules.Analytics.Application.Services;
using SAFARIstack.Modules.Analytics.Domain.Interfaces;

/// <summary>
/// Analytics Module Registration
/// Registers all analytics services for dependency injection
/// </summary>
public static class AnalyticsModule
{
    /// <summary>
    /// Register analytics module services
    /// Call this from Program.cs: AnalyticsModule.RegisterServices(builder.Services)
    /// </summary>
    public static void RegisterServices(IServiceCollection services)
    {
        // Core analytics services (interfaces are in Shared for loose coupling)
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IPredictiveAnalytics, PredictiveAnalyticsEngine>();
        services.AddScoped<IGuestBehaviorAnalytics, GuestBehaviorAnalytics>();
        services.AddScoped<IReportBuilder, ReportBuilder>();

        // Background jobs for analytics aggregation
        services.AddHostedService<AnalyticsAggregationBackgroundService>();

        // Redis cache for real-time metrics (configured in the host API project)
        // services.AddStackExchangeRedisCache(options =>
        // {
        //     options.Configuration = "localhost:6379";
        // });

        // Time-series database context for analytics (separate from transactional DB)
        // services.AddDbContext<AnalyticsDbContext>(options =>
        //     options.UseNpgsql("connection-string-for-timescaledb"));
    }
}
