using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// Service for inventory management including item CRUD, stock tracking, and adjustments.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Retrieves an inventory item by ID.
    /// </summary>
    Task<InventoryItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches inventory items with optional filters (category, activity status, SKU pattern).
    /// </summary>
    Task<IEnumerable<InventoryItem>> SearchItemsAsync(
        Guid propertyId,
        InventorySearchFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inventory item for a property.
    /// </summary>
    Task<InventoryItem> CreateItemAsync(
        Guid propertyId,
        string sku,
        string name,
        string category,
        decimal initialStock,
        decimal reorderLevel,
        string stockUnit,
        decimal costPrice,
        decimal? sellingPrice = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inventory item.
    /// </summary>
    Task<InventoryItem> UpdateItemAsync(
        Guid itemId,
        InventoryItemUpdate update,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts inventory stock due to count discrepancy or damage.
    /// Logs the reason for audit trail.
    /// </summary>
    Task<InventoryItem> AdjustStockAsync(
        Guid itemId,
        decimal newQuantity,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a stock receipt (purchase, donation, transfer).
    /// </summary>
    Task<InventoryItem> ReceiveStockAsync(
        Guid itemId,
        decimal quantity,
        string reason = "Stock Receipt",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records inventory depletion for any reason (sale, damage, loss, sample).
    /// Throws InsufficientInventoryException if quantity exceeds current stock.
    /// </summary>
    Task<InventoryItem> DepleteStockAsync(
        Guid itemId,
        decimal quantity,
        string reason = "Stock Depletion",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an inventory item (soft delete - not removed from DB).
    /// </summary>
    Task<InventoryItem> DeactivateItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deactivated inventory item.
    /// </summary>
    Task<InventoryItem> ReactivateItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active items for a property.
    /// </summary>
    Task<IEnumerable<InventoryItem>> GetActiveItemsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates cost of goods sold for a given quantity.
    /// </summary>
    Task<decimal> CalculateCOGSAsync(Guid itemId, decimal quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory valuation (total cost value of all stock).
    /// </summary>
    Task<InventoryValuation> GetInventoryValuationAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory items with low stock (at or below reorder level).
    /// </summary>
    Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory turnover metrics for a period.
    /// </summary>
    Task<InventoryTurnoverReport> GetTurnoverReportAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter criteria for inventory search.
/// </summary>
public class InventorySearchFilters
{
    public string? Category { get; set; }
    public string? SKUPattern { get; set; }
    public string? NamePattern { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsLowStock { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Update data for inventory items.
/// </summary>
public class InventoryItemUpdate
{
    public string? Name { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public string? StockUnit { get; set; }
}

/// <summary>
/// Inventory valuation summary.
/// </summary>
public class InventoryValuation
{
    public Guid PropertyId { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Inventory turnover analysis.
/// </summary>
public class InventoryTurnoverReport
{
    public Guid PropertyId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalQuantitySold { get; set; }
    public decimal TotalCostOfSold { get; set; }
    public decimal AverageTurnover { get; set; } // Times rotated per period
    public Dictionary<string, CategoryTurnover> ByCategory { get; set; } = new();
}

/// <summary>
/// Turnover metrics by category.
/// </summary>
public class CategoryTurnover
{
    public string Category { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal TurnoverRate { get; set; }
}
