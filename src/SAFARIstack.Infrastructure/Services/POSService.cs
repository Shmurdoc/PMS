using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Exceptions;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Implementation of POS service. Handles sales recording, inventory depletion,
/// and daily reconciliation with transactional integrity.
/// </summary>
public class POSService : IPOSService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<POSService> _logger;
    private readonly IInventoryService _inventoryService;

    public POSService(
        ApplicationDbContext db,
        ILogger<POSService> logger,
        IInventoryService inventoryService)
    {
        _db = db;
        _logger = logger;
        _inventoryService = inventoryService;
    }

    public async Task<CasualSale> RecordSaleAsync(
        Guid propertyId,
        string description,
        decimal quantity,
        decimal unitPrice,
        string paymentMethod = "cash",
        Guid? recordedByStaffId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidSaleException("Quantity must be greater than zero");

        if (unitPrice < 0)
            throw new InvalidSaleException("Unit price cannot be negative");

        // Verify property exists
        var propertyExists = await _db.Set<Property>().AnyAsync(p => p.Id == propertyId, cancellationToken);
        if (!propertyExists)
            throw new DomainException($"Property {propertyId} not found");

        try
        {
            var sale = CasualSale.Create(
                propertyId: propertyId,
                description: description,
                quantity: quantity,
                unitPrice: unitPrice,
                paymentMethod: paymentMethod,
                recordedByStaffId: recordedByStaffId,
                notes: notes);

            _db.Set<CasualSale>().Add(sale);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sale recorded: {SaleId} for {Description} (Qty: {Quantity}, Total: {Total} + VAT {VAT})",
                sale.Id, description, quantity, sale.TotalAmount, sale.VatAmount);

            return sale;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error recording sale");
            throw new InvalidSaleException("Failed to record sale. Please try again.");
        }
    }

    public async Task DepleteInventoryAsync(
        Guid itemId,
        decimal quantity,
        string reason = "Sale",
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidInventoryAdjustmentException("Depletion quantity must be greater than zero");

        var item = await _inventoryService.GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new InventoryItemNotFound(itemId);

        if (item.CurrentStock < quantity)
            throw new InsufficientInventoryException(item.Name, itemId, quantity, item.CurrentStock);

        await _inventoryService.DepleteStockAsync(itemId, quantity, reason, cancellationToken);

        _logger.LogInformation(
            "Inventory depleted: Item {ItemId} ({SKU}) by {Quantity} {Unit} due to {Reason}",
            itemId, item.SKU, quantity, item.StockUnit, reason);
    }

    public async Task<IEnumerable<InventoryItem>> GetPropertyInventoryAsync(
        Guid propertyId,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId && ii.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(ii => ii.Category == category);

        return await query
            .OrderBy(ii => ii.Category)
            .ThenBy(ii => ii.SKU)
            .ToListAsync(cancellationToken);
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

    public async Task<DayEndClose> CreateDayEndCloseAsync(
        Guid propertyId,
        decimal expectedCash,
        decimal actualCash,
        Guid closedByStaffId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Check if already closed today
        var today = DateTime.UtcNow.Date;
        var existingClose = await _db.Set<DayEndClose>()
            .FirstOrDefaultAsync(
                dec => dec.PropertyId == propertyId && dec.CloseDate == today,
                cancellationToken);

        if (existingClose != null)
            throw new DayEndAlreadyClosedException(propertyId, today);

        try
        {
            var dayEndClose = DayEndClose.Create(
                propertyId: propertyId,
                expectedCash: expectedCash,
                actualCash: actualCash,
                closedByStaffId: closedByStaffId,
                notes: notes);

            _db.Set<DayEndClose>().Add(dayEndClose);
            await _db.SaveChangesAsync(cancellationToken);

            var variance = dayEndClose.Variance;
            var varianceType = variance > 0 ? "OVERAGE" : variance < 0 ? "SHORTAGE" : "EXACT";

            _logger.LogInformation(
                "Day-end closed for property {PropertyId}: Expected {Expected}, Actual {Actual}, Variance {Variance} ({Type})",
                propertyId, expectedCash, actualCash, Math.Abs(variance), varianceType);

            return dayEndClose;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating day-end close");
            throw new DomainException("Failed to create day-end close. Please try again.");
        }
    }

    public async Task<DayEndClose> VerifyDayEndAsync(
        Guid closeId,
        Guid verifiedByStaffId,
        CancellationToken cancellationToken = default)
    {
        var dayEnd = await _db.Set<DayEndClose>()
            .FirstOrDefaultAsync(dec => dec.Id == closeId, cancellationToken)
            ?? throw new DomainException($"Day-end close {closeId} not found");

        if (dayEnd.IsVerified)
            throw new DayEndAlreadyVerifiedException(closeId);

        try
        {
            dayEnd.Verify(verifiedByStaffId);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Day-end close {CloseId} verified by staff {StaffId}",
                closeId, verifiedByStaffId);

            return dayEnd;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error verifying day-end close");
            throw new DomainException("Failed to verify day-end close.");
        }
    }

    public async Task<IEnumerable<CasualSale>> GetSalesHistoryAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Set<CasualSale>()
            .Where(cs => cs.PropertyId == propertyId && cs.SaleDate >= fromDate && cs.SaleDate <= toDate);

        if (!string.IsNullOrWhiteSpace(paymentMethod))
            query = query.Where(cs => cs.PaymentMethod == paymentMethod);

        return await query
            .OrderByDescending(cs => cs.SaleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<DailySalesSummary> GetDailySummaryAsync(
        Guid propertyId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

        var sales = await _db.Set<CasualSale>()
            .Where(cs => cs.PropertyId == propertyId && cs.SaleDate >= startOfDay && cs.SaleDate <= endOfDay)
            .ToListAsync(cancellationToken);

        var summary = new DailySalesSummary
        {
            PropertyId = propertyId,
            Date = date,
            TransactionCount = sales.Count,
            TotalAmount = sales.Sum(s => s.TotalAmount),
            TotalVAT = sales.Sum(s => s.VatAmount),
            TotalWithVAT = sales.Sum(s => s.TotalAmount + s.VatAmount),
            SalesByPaymentMethod = sales
                .GroupBy(s => s.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount + s.VatAmount)),
            CreatedAt = DateTime.UtcNow
        };

        return summary;
    }

    public async Task<DayEndClose?> GetLatestDayEndAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<DayEndClose>()
            .Where(dec => dec.PropertyId == propertyId)
            .OrderByDescending(dec => dec.CloseDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasDayEndCloseAsync(
        Guid propertyId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var closeDate = date.Date;
        return await _db.Set<DayEndClose>()
            .AnyAsync(dec => dec.PropertyId == propertyId && dec.CloseDate == closeDate, cancellationToken);
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<InventoryItem>()
            .FirstOrDefaultAsync(ii => ii.Id == itemId, cancellationToken);
    }
}
