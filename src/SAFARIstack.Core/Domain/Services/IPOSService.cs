using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// Service for Point of Sale operations including sales recording, inventory management,
/// and daily reconciliation. Integrates with OutboxEvent for transactional reliability.
/// </summary>
public interface IPOSService
{
    /// <summary>
    /// Records a casual sale transaction.
    /// </summary>
    Task<CasualSale> RecordSaleAsync(
        Guid propertyId,
        string description,
        decimal quantity,
        decimal unitPrice,
        string paymentMethod = "cash",
        Guid? recordedByStaffId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Depletes inventory item stock when a sale is recorded.
    /// Throws InsufficientInventoryException if stock is insufficient.
    /// </summary>
    Task DepleteInventoryAsync(
        Guid itemId,
        decimal quantity,
        string reason = "Sale",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all inventory items for a property, optionally filtered by category.
    /// </summary>
    Task<IEnumerable<InventoryItem>> GetPropertyInventoryAsync(
        Guid propertyId,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inventory items that are below or at reorder level.
    /// </summary>
    Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a day-end cash close record. Calculates variance between expected and actual cash.
    /// </summary>
    Task<DayEndClose> CreateDayEndCloseAsync(
        Guid propertyId,
        decimal expectedCash,
        decimal actualCash,
        Guid closedByStaffId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a day-end close record (management approval).
    /// </summary>
    Task<DayEndClose> VerifyDayEndAsync(
        Guid closeId,
        Guid verifiedByStaffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves sales history for a property within a date range.
    /// </summary>
    Task<IEnumerable<CasualSale>> GetSalesHistoryAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily sales summary (total, VAT, by payment method).
    /// </summary>
    Task<DailySalesSummary> GetDailySummaryAsync(
        Guid propertyId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest day-end close for a property.
    /// </summary>
    Task<DayEndClose?> GetLatestDayEndAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a property already has a completed day-end close for the specified date.
    /// </summary>
    Task<bool> HasDayEndCloseAsync(
        Guid propertyId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory item by ID.
    /// </summary>
    Task<InventoryItem?> GetInventoryItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Daily sales summary for reporting.
/// </summary>
public class DailySalesSummary
{
    public Guid PropertyId { get; set; }
    public DateTime Date { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalVAT { get; set; }
    public decimal TotalWithVAT { get; set; }
    public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
