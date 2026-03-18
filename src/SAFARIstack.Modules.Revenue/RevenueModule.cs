namespace SAFARIstack.Modules.Revenue;

using Microsoft.Extensions.DependencyInjection;
using SAFARIstack.Modules.Revenue.Application.Services;
using SAFARIstack.Modules.Revenue.Domain.Interfaces;

/// <summary>
/// Revenue Management Module Registration
/// </summary>
public static class RevenueModule
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IRevenueManagementSystem, RevenueManagementSystem>();
        services.AddScoped<IPricingAlgorithm, BasicPricingAlgorithm>();
        services.AddHostedService<DemandSignalAggregationService>();
    }
}
