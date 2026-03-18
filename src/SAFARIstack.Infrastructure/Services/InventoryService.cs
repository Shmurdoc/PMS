using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Exceptions;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Implementation of inventory management service. Handles CRUD operations,
/// stock tracking, adjustments, and valuation calculations.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        ApplicationDbContext db,
        ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InventoryItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _db.Set<InventoryItem>()
            .FirstOrDefaultAsync(ii => ii.Id == itemId, cancellationToken);
    }

    public async Task<IEnumerable<InventoryItem>> SearchItemsAsync(
        Guid propertyId,
        InventorySearchFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId);

        if (!string.IsNullOrWhiteSpace(filters.Category))
            query = query.Where(ii => ii.Category == filters.Category);

        if (!string.IsNullOrWhiteSpace(filters.SKUPattern))
            query = query.Where(ii => ii.SKU.Contains(filters.SKUPattern));

        if (!string.IsNullOrWhiteSpace(filters.NamePattern))
            query = query.Where(ii => ii.Name.Contains(filters.NamePattern));

        if (filters.IsActive.HasValue)
            query = query.Where(ii => ii.IsActive == filters.IsActive.Value);

        if (filters.IsLowStock.HasValue && filters.IsLowStock.Value)
            query = query.Where(ii => ii.CurrentStock <= ii.ReorderLevel);

        var items = await query
            .OrderBy(ii => ii.Category)
            .ThenBy(ii => ii.SKU)
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<InventoryItem> CreateItemAsync(
        Guid propertyId,
        string sku,
        string name,
        string category,
        decimal initialStock,
        decimal reorderLevel,
        string stockUnit,
        decimal costPrice,
        decimal? sellingPrice = null,
        CancellationToken cancellationToken = default)
    {
        // Verify property exists
        var property = await _db.Set<Property>()
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken)
            ?? throw new DomainException($"Property {propertyId} not found");

        // Check SKU uniqueness per property
        var skuExists = await _db.Set<InventoryItem>()
            .AnyAsync(ii => ii.PropertyId == propertyId && ii.SKU == sku, cancellationToken);

        if (skuExists)
            throw new DuplicateSKUException(sku, propertyId);

        try
        {
            var item = InventoryItem.Create(
                propertyId: propertyId,
                sku: sku,
                name: name,
                category: category,
                initialStock: initialStock,
                reorderLevel: reorderLevel,
                stockUnit: stockUnit,
                costPrice: costPrice,
                sellingPrice: sellingPrice);

            _db.Set<InventoryItem>().Add(item);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Inventory item created: SKU {SKU}, Name {Name}, Initial Stock {Stock} {Unit}",
                sku, name, initialStock, stockUnit);

            return item;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating inventory item");
            throw new DomainException("Failed to create inventory item.");
        }
    }

    public async Task<InventoryItem> UpdateItemAsync(
        Guid itemId,
        InventoryItemUpdate update,
        CancellationToken cancellationToken = default)
    {
        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        // Note: InventoryItem entity properties are read-only by design.
        // For now, we only support updating via domain methods (Deplete, Receive, etc.)
        // Full property updates would require adding setters or update methods to the entity.
        // This is intentional to maintain domain integrity.

        // Placeholder for actual updates - would need entity modifications
        _logger.LogInformation("Inventory item update requested for {ItemId}, but entity properties are immutable", itemId);
        
        return item;
    }

    public async Task<InventoryItem> AdjustStockAsync(
        Guid itemId,
        decimal newQuantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (newQuantity < 0)
            throw new InvalidInventoryAdjustmentException("Stock quantity cannot be negative");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidInventoryAdjustmentException("Adjustment reason is required for audit trail");

        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        if (!item.IsActive)
            throw new InactiveItemException(itemId);

        try
        {
            item.AdjustForCount(newQuantity, reason);
            _db.Set<InventoryItem>().Update(item);
            await _db.SaveChangesAsync(cancellationToken);

            var oldQty = item.CurrentStock;
            var diff = newQuantity - oldQty;

            _logger.LogInformation(
                "Inventory adjusted: Item {ItemId} ({SKU}) - was {OldQty}, now {NewQty} (diff: {Diff}). Reason: {Reason}",
                itemId, item.SKU, oldQty, newQuantity, diff, reason);

            return item;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error adjusting inventory");
            throw new DomainException("Failed to adjust inventory.");
        }
    }

    public async Task<InventoryItem> ReceiveStockAsync(
        Guid itemId,
        decimal quantity,
        string reason = "Stock Receipt",
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidInventoryAdjustmentException("Receipt quantity must be greater than zero");

        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        if (!item.IsActive)
            throw new InactiveItemException(itemId);

        try
        {
            item.Receive(quantity);
            _db.Set<InventoryItem>().Update(item);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Stock received: Item {ItemId} ({SKU}) - received {Quantity} {Unit}. Reason: {Reason}",
                itemId, item.SKU, quantity, item.StockUnit, reason);

            return item;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error receiving stock");
            throw new DomainException("Failed to receive stock.");
        }
    }

    public async Task<InventoryItem> DepleteStockAsync(
        Guid itemId,
        decimal quantity,
        string reason = "Stock Depletion",
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidInventoryAdjustmentException("Depletion quantity must be greater than zero");

        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        if (!item.IsActive)
            throw new InactiveItemException(itemId);

        if (item.CurrentStock < quantity)
            throw new InsufficientInventoryException(item.Name, itemId, quantity, item.CurrentStock);

        try
        {
            item.Deplete(quantity);
            _db.Set<InventoryItem>().Update(item);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Stock depleted: Item {ItemId} ({SKU}) - depleted {Quantity} {Unit}. Remaining: {Remaining}. Reason: {Reason}",
                itemId, item.SKU, quantity, item.StockUnit, item.CurrentStock, reason);

            return item;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error depleting stock");
            throw new DomainException("Failed to deplete stock.");
        }
    }

    public async Task<InventoryItem> DeactivateItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        try
        {
            item.Deactivate();
            _db.Set<InventoryItem>().Update(item);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inventory item deactivated: {ItemId} ({SKU})", itemId, item.SKU);
            return item;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deactivating item");
            throw new DomainException("Failed to deactivate item.");
        }
    }

    public async Task<InventoryItem> ReactivateItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        try
        {
            item.Reactivate();
            _db.Set<InventoryItem>().Update(item);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inventory item reactivated: {ItemId} ({SKU})", itemId, item.SKU);
            return item;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error reactivating item");
            throw new DomainException("Failed to reactivate item.");
        }
    }

    public async Task<IEnumerable<InventoryItem>> GetActiveItemsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId && ii.IsActive)
            .OrderBy(ii => ii.Category)
            .ThenBy(ii => ii.SKU)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> CalculateCOGSAsync(Guid itemId, decimal quantity, CancellationToken cancellationToken = default)
    {
        var item = await GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        return item.CalculateCOGS(quantity);
    }

    public async Task<InventoryValuation> GetInventoryValuationAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var items = await _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId && ii.IsActive)
            .ToListAsync(cancellationToken);

        var totalCostValue = items.Sum(i => i.CurrentStock * i.CostPrice);
        var totalRetailValue = items
            .Where(i => i.SellingPrice.HasValue)
            .Sum(i => i.CurrentStock * i.SellingPrice.Value);

        return new InventoryValuation
        {
            PropertyId = propertyId,
            ItemCount = items.Count,
            TotalQuantity = items.Sum(i => i.CurrentStock),
            TotalCostValue = totalCostValue,
            TotalRetailValue = totalRetailValue,
            CalculatedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId && ii.IsActive && ii.CurrentStock <= ii.ReorderLevel)
            .OrderBy(ii => ii.CurrentStock)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryTurnoverReport> GetTurnoverReportAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var sales = await _db.Set<CasualSale>()
            .Where(cs => cs.PropertyId == propertyId && cs.SaleDate >= fromDate && cs.SaleDate <= toDate)
            .ToListAsync(cancellationToken);

        var items = await _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId)
            .ToListAsync(cancellationToken);

        var totalQuantitySold = sales.Sum(s => s.Quantity);
        var totalCostOfSold = sales.Sum(s => s.Quantity * (items.FirstOrDefault(i => i.PropertyId == propertyId)?.CostPrice ?? 0)); // Simplified

        var periodsPerYear = (decimal)(365.0 / ((toDate - fromDate).Days + 1));
        var totalAverageCostPerItem = items.Any() ? items.Average(i => i.CostPrice) : 1;
        var denominator = items.Sum(i => i.CurrentStock) + (totalQuantitySold / periodsPerYear);
        var averageTurnover = totalQuantitySold > 0 && denominator > 0 ? totalQuantitySold / denominator : 0;

        var byCategory = items
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => new CategoryTurnover
            {
                Category = g.Key,
                ItemCount = g.Count(),
                QuantitySold = sales.Sum(s => s.Quantity), // Simplified
                CostOfGoodsSold = g.Sum(i => i.CurrentStock * i.CostPrice),
                TurnoverRate = averageTurnover
            });

        return new InventoryTurnoverReport
        {
            PropertyId = propertyId,
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            TotalItems = items.Count,
            TotalQuantitySold = totalQuantitySold,
            TotalCostOfSold = totalCostOfSold,
            AverageTurnover = averageTurnover,
            ByCategory = byCategory
        };
    }
}
