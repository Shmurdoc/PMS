using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SAFARIstack.API.Contracts.POS;
using SAFARIstack.Core.Domain.Exceptions;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.API.Features.POS;

/// <summary>
/// POS (Point of Sale) API endpoints for managing sales, inventory, and daily reconciliation.
/// </summary>
[ApiController]
[Route("api/pos")]
[Authorize]
[Produces("application/json")]
public class POSController : ControllerBase
{
    private readonly IPOSService _posService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<POSController> _logger;

    public POSController(IPOSService posService, IInventoryService inventoryService, ILogger<POSController> logger)
    {
        _posService = posService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    // SALES ENDPOINTS
    [HttpPost("sales")]
    public async Task<ActionResult<SaleResponse>> RecordSale([FromQuery] Guid propertyId, [FromBody] CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var sale = await _posService.RecordSaleAsync(propertyId, request.Description, request.Quantity, request.UnitPrice, request.PaymentMethod, request.RecordedByStaffId, request.Notes, cancellationToken);
            return CreatedAtAction(nameof(GetSale), new { saleId = sale.Id }, MapSaleToResponse(sale));
        }
        catch (InvalidSaleException ex) { return BadRequest(new ErrorResponse("INVALID_SALE", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error recording sale"); return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
    }

    [HttpGet("sales/{saleId}")]
    public Task<ActionResult<SaleResponse>> GetSale(Guid saleId, CancellationToken cancellationToken)
        => Task.FromResult<ActionResult<SaleResponse>>(Ok((SaleResponse?)null));

    [HttpGet("sales/history")]
    public async Task<ActionResult<List<SaleResponse>>> GetSalesHistory([FromQuery] Guid propertyId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? paymentMethod = null, CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
            return BadRequest(new ErrorResponse("INVALID_DATE_RANGE", "From date must be before to date", null));
        try
        {
            var sales = await _posService.GetSalesHistoryAsync(propertyId, fromDate, toDate, paymentMethod, cancellationToken);
            return Ok(sales.Select(s => MapSaleToResponse(s)).ToList());
        }
        catch (DomainException ex) { _logger.LogError(ex, "Error retrieving sales history"); return BadRequest(new ErrorResponse("QUERY_ERROR", ex.Message, null)); }
    }

    [HttpPost("sales/with-inventory")]
    public async Task<ActionResult<SaleResponse>> RecordSaleWithInventory([FromQuery] Guid propertyId, [FromBody] RecordSaleWithInventoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.GetItemByIdAsync(request.InventoryItemId, cancellationToken) ?? throw new DomainException($"Item {request.InventoryItemId} not found");
            var sale = await _posService.RecordSaleAsync(propertyId, item.Name, request.Quantity, request.UnitPrice > 0 ? request.UnitPrice : (item.SellingPrice ?? item.CostPrice), request.PaymentMethod, request.RecordedByStaffId, request.Notes, cancellationToken);
            await _posService.DepleteInventoryAsync(request.InventoryItemId, request.Quantity, "Sale", cancellationToken);
            return CreatedAtAction(nameof(GetSale), new { saleId = sale.Id }, MapSaleToResponse(sale));
        }
        catch (InsufficientInventoryException ex) { _logger.LogWarning("Insufficient inventory: {Message}", ex.Message); return Conflict(new ErrorResponse("INSUFFICIENT_INVENTORY", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error recording sale with inventory"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpGet("sales/daily-summary")]
    public async Task<ActionResult<DailySalesSummaryResponse>> GetDailySummary([FromQuery] Guid propertyId, [FromQuery] DateTime? date = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _posService.GetDailySummaryAsync(propertyId, date ?? DateTime.UtcNow, cancellationToken);
            return Ok(new DailySalesSummaryResponse(summary.PropertyId, summary.Date, summary.TransactionCount, summary.TotalAmount, summary.TotalVAT, summary.TotalWithVAT, summary.SalesByPaymentMethod, summary.CreatedAt));
        }
        catch (DomainException ex) { _logger.LogError(ex, "Error calculating daily summary"); return BadRequest(new ErrorResponse("CALCULATION_ERROR", ex.Message, null)); }
    }

    // DAY-END ENDPOINTS
    [HttpPost("day-end/close")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<DayEndCloseResponse>> CreateDayEndClose([FromQuery] Guid propertyId, [FromBody] CreateDayEndCloseRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var dayEnd = await _posService.CreateDayEndCloseAsync(propertyId, request.ExpectedCash, request.ActualCash, request.ClosedByStaffId, request.Notes, cancellationToken);
            return CreatedAtAction(nameof(GetDayEndClose), new { closeId = dayEnd.Id }, MapDayEndToResponse(dayEnd));
        }
        catch (DayEndAlreadyClosedException ex) { _logger.LogWarning("Day-end already closed: {Message}", ex.Message); return Conflict(new ErrorResponse("DAY_END_ALREADY_CLOSED", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error creating day-end close"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpGet("day-end/{closeId}")]
    public Task<ActionResult<DayEndCloseResponse>> GetDayEndClose(Guid closeId, CancellationToken cancellationToken)
        => Task.FromResult<ActionResult<DayEndCloseResponse>>(Ok((DayEndCloseResponse?)null));

    [HttpPost("day-end/{closeId}/verify")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<DayEndCloseResponse>> VerifyDayEndClose(Guid closeId, [FromBody] VerifyDayEndRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var verified = await _posService.VerifyDayEndAsync(closeId, request.VerifiedByStaffId, cancellationToken);
            return Ok(MapDayEndToResponse(verified));
        }
        catch (DayEndAlreadyVerifiedException ex) { _logger.LogWarning("Day-end already verified: {Message}", ex.Message); return Conflict(new ErrorResponse("ALREADY_VERIFIED", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error verifying day-end close"); return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
    }

    [HttpGet("day-end/latest")]
    public async Task<ActionResult<DayEndCloseResponse>> GetLatestDayEnd([FromQuery] Guid propertyId, CancellationToken cancellationToken = default)
    {
        var dayEnd = await _posService.GetLatestDayEndAsync(propertyId, cancellationToken);
        return dayEnd == null ? NoContent() : Ok(MapDayEndToResponse(dayEnd));
    }

    // INVENTORY ENDPOINTS
    [HttpGet("inventory")]
    public async Task<ActionResult<PaginatedInventoryResponse>> GetInventory([FromQuery] Guid propertyId, [FromQuery] SearchInventoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var filters = new Core.Domain.Services.InventorySearchFilters { Category = request.Category, SKUPattern = request.SKUPattern, NamePattern = request.NamePattern, IsActive = request.IsActive, IsLowStock = request.IsLowStock, PageNumber = request.PageNumber, PageSize = request.PageSize };
            var items = await _inventoryService.SearchItemsAsync(propertyId, filters, cancellationToken);
            var responses = items.Select(i => MapInventoryItemToResponse(i)).ToList();
            return Ok(new PaginatedInventoryResponse(Items: responses, PageNumber: request.PageNumber, PageSize: request.PageSize, TotalCount: responses.Count, TotalPages: (responses.Count + request.PageSize - 1) / request.PageSize));
        }
        catch (DomainException ex) { _logger.LogError(ex, "Error retrieving inventory"); return BadRequest(new ErrorResponse("QUERY_ERROR", ex.Message, null)); }
    }

    [HttpGet("inventory/{itemId}")]
    public async Task<ActionResult<InventoryItemResponse>> GetInventoryItem(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetItemByIdAsync(itemId, cancellationToken);
        return item == null ? NotFound(new ErrorResponse("NOT_FOUND", $"Item {itemId} not found", null)) : Ok(MapInventoryItemToResponse(item));
    }

    [HttpPost("inventory")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItemResponse>> CreateInventoryItem([FromQuery] Guid propertyId, [FromBody] CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.CreateItemAsync(propertyId, request.SKU, request.Name, request.Category, request.InitialStock, request.ReorderLevel, request.StockUnit, request.CostPrice, request.SellingPrice, cancellationToken);
            return CreatedAtAction(nameof(GetInventoryItem), new { itemId = item.Id }, MapInventoryItemToResponse(item));
        }
        catch (DuplicateSKUException ex) { _logger.LogWarning("Duplicate SKU: {Message}", ex.Message); return Conflict(new ErrorResponse("DUPLICATE_SKU", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error creating inventory item"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpPut("inventory/{itemId}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItemResponse>> UpdateInventoryItem(Guid itemId, [FromBody] UpdateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var update = new Core.Domain.Services.InventoryItemUpdate { Name = request.Name, ReorderLevel = request.ReorderLevel, CostPrice = request.CostPrice, SellingPrice = request.SellingPrice, StockUnit = request.StockUnit };
            var item = await _inventoryService.UpdateItemAsync(itemId, update, cancellationToken);
            return Ok(MapInventoryItemToResponse(item));
        }
        catch (InventoryItemNotFound ex) { return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error updating inventory item"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpPost("inventory/{itemId}/deplete")]
    public async Task<ActionResult<InventoryItemResponse>> DepleteStock(Guid itemId, [FromBody] DepeleStockRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.DepleteStockAsync(itemId, request.Quantity, request.Reason, cancellationToken);
            return Ok(MapInventoryItemToResponse(item));
        }
        catch (InventoryItemNotFound ex) { return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
        catch (InsufficientInventoryException ex) { _logger.LogWarning("Insufficient inventory: {Message}", ex.Message); return Conflict(new ErrorResponse("INSUFFICIENT_INVENTORY", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error depleting inventory"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpPost("inventory/{itemId}/receive")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItemResponse>> ReceiveStock(Guid itemId, [FromBody] ReceiveStockRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.ReceiveStockAsync(itemId, request.Quantity, request.Reason, cancellationToken);
            return Ok(MapInventoryItemToResponse(item));
        }
        catch (InventoryItemNotFound ex) { return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error receiving inventory"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpPost("inventory/{itemId}/adjust")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItemResponse>> AdjustStock(Guid itemId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.AdjustStockAsync(itemId, request.NewQuantity, request.Reason, cancellationToken);
            return Ok(MapInventoryItemToResponse(item));
        }
        catch (InventoryItemNotFound ex) { return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error adjusting inventory"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpGet("inventory/low-stock")]
    public async Task<ActionResult<LowStockReportResponse>> GetLowStockItems([FromQuery] Guid propertyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _inventoryService.GetLowStockItemsAsync(propertyId, cancellationToken);
            var alerts = items.Select(i => new LowStockAlertResponse(ItemId: i.Id, SKU: i.SKU, Name: i.Name, CurrentStock: i.CurrentStock, ReorderLevel: i.ReorderLevel, UnitsBelow: (int)(i.ReorderLevel - i.CurrentStock))).ToList();
            return Ok(new LowStockReportResponse(PropertyId: propertyId, AlertCount: alerts.Count, Items: alerts));
        }
        catch (DomainException ex) { _logger.LogError(ex, "Error retrieving low stock items"); return BadRequest(new ErrorResponse("QUERY_ERROR", ex.Message, null)); }
    }

    [HttpGet("inventory/valuation")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryValuationResponse>> GetInventoryValuation([FromQuery] Guid propertyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var valuation = await _inventoryService.GetInventoryValuationAsync(propertyId, cancellationToken);
            var margin = valuation.TotalRetailValue > 0 ? ((valuation.TotalRetailValue - valuation.TotalCostValue) / valuation.TotalRetailValue) * 100 : 0;
            return Ok(new InventoryValuationResponse(PropertyId: valuation.PropertyId, ItemCount: valuation.ItemCount, TotalQuantity: valuation.TotalQuantity, TotalCostValue: valuation.TotalCostValue, TotalRetailValue: valuation.TotalRetailValue, GrossMargin: margin, CalculatedAt: valuation.CalculatedAt));
        }
        catch (DomainException ex) { _logger.LogError(ex, "Error calculating valuation"); return BadRequest(new ErrorResponse("CALCULATION_ERROR", ex.Message, null)); }
    }

    [HttpPost("inventory/{itemId}/deactivate")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItemResponse>> DeactivateItem(Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.DeactivateItemAsync(itemId, cancellationToken);
            return Ok(MapInventoryItemToResponse(item));
        }
        catch (InventoryItemNotFound ex) { return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error deactivating item"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    [HttpPost("inventory/{itemId}/reactivate")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItemResponse>> ReactivateItem(Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _inventoryService.ReactivateItemAsync(itemId, cancellationToken);
            return Ok(MapInventoryItemToResponse(item));
        }
        catch (InventoryItemNotFound ex) { return NotFound(new ErrorResponse("NOT_FOUND", ex.Message, null)); }
        catch (DomainException ex) { _logger.LogError(ex, "Error reactivating item"); return BadRequest(new ErrorResponse("OPERATION_FAILED", ex.Message, null)); }
    }

    // MAPPERS
    private static SaleResponse MapSaleToResponse(dynamic sale) => new(sale.Id, sale.PropertyId, sale.SaleDate, sale.Description, sale.Quantity, sale.UnitPrice, sale.TotalAmount, sale.VatAmount, sale.TotalAmount + sale.VatAmount, sale.PaymentMethod, sale.RecordedByStaffId, sale.Notes, sale.CreatedAt);

    private static DayEndCloseResponse MapDayEndToResponse(dynamic dayEnd) => new(dayEnd.Id, dayEnd.PropertyId, dayEnd.CloseDate, dayEnd.ExpectedCash, dayEnd.ActualCash, dayEnd.Variance, dayEnd.Notes, dayEnd.ClosedByStaffId, dayEnd.ClosedAt, dayEnd.IsVerified, dayEnd.VerifiedByStaffId);

    private static InventoryItemResponse MapInventoryItemToResponse(dynamic item) => new(item.Id, item.PropertyId, item.SKU, item.Name, item.Category, item.CurrentStock, item.ReorderLevel, item.StockUnit, item.CostPrice, item.SellingPrice, item.IsActive, item.IsLowStock, item.LastStockCountAt, item.CreatedAt);
}
