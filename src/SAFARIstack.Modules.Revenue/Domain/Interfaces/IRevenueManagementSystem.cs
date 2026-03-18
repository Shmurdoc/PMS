namespace SAFARIstack.Modules.Revenue.Domain.Interfaces;

using SAFARIstack.Modules.Revenue.Domain.Models;

/// <summary>
/// Revenue Management System contract
/// Decision-support engine for pricing, not direct rate enforcement
/// Preserves human oversight while providing AI-driven insights
/// </summary>
public interface IRevenueManagementSystem
{
    /// <summary>
    /// Generate pricing recommendation based on demand, competition, seasonality
    /// Manager reviews and accepts/rejects before rates are updated
    /// </summary>
    Task<PricingRecommendation> GeneratePricingRecommendationAsync(
        Guid propertyId,
        Guid roomTypeId,
        DateTime date,
        CancellationToken ct = default);

    /// <summary>
    /// Get competitor rate intelligence
    /// Tracks what booking.com, expedia, airbnb are charging
    /// </summary>
    Task<RateShoppingInsight> GetRateShoppingInsightAsync(
        Guid propertyId,
        string roomType,
        DateTime date,
        CancellationToken ct = default);

    /// <summary>
    /// Generate alert if opportunity or risk detected
    /// </summary>
    Task<RevenueAlert?> CheckForRevenueAlertAsync(
        Guid propertyId,
        CancellationToken ct = default);

    /// <summary>
    /// Accept pricing recommendation and update rates
    /// Publishes RateUpdatedEvent for channel manager to sync
    /// </summary>
    Task<bool> AcceptPricingRecommendationAsync(
        Guid recommendationId,
        CancellationToken ct = default);

    /// <summary>
    /// Get demand signal aggregation
    /// </summary>
    Task<DemandSignal> GetDemandSignalAsync(
        Guid propertyId,
        DateTime date,
        CancellationToken ct = default);
}

/// <summary>
/// Pricing algorithm abstraction
/// Can swap different algorithms (rule-based, ML-based, etc.)
/// </summary>
public interface IPricingAlgorithm
{
    Task<PricingRecommendation> RecommendPriceAsync(
        Guid propertyId,
        Guid roomTypeId,
        DateTime date,
        DemandSignal demand,
        RateShoppingInsight shopping,
        CancellationToken ct = default);
}
