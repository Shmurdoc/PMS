namespace SAFARIstack.API.Contracts.POS;

// ==================== SALE DTOs ====================

/// <summary>
/// Request to record a casual sale.
/// </summary>
public record CreateSaleRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string PaymentMethod = "cash",
    Guid? RecordedByStaffId = null,
    string? Notes = null);

/// <summary>
/// Response containing recorded sale details.
/// </summary>
public record SaleResponse(
    Guid Id,
    Guid PropertyId,
    DateTime SaleDate,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    decimal VatAmount,
    decimal TotalWithVat,
    string PaymentMethod,
    Guid? RecordedByStaffId,
    string? Notes,
    DateTime CreatedAt);

// ==================== DAY-END DTOs ====================

/// <summary>
/// Request to create a day-end close.
/// </summary>
public record CreateDayEndCloseRequest(
    decimal ExpectedCash,
    decimal ActualCash,
    Guid ClosedByStaffId,
    string? Notes = null);

/// <summary>
/// Response containing day-end close details.
/// </summary>
public record DayEndCloseResponse(
    Guid Id,
    Guid PropertyId,
    DateTime CloseDate,
    decimal ExpectedCash,
    decimal ActualCash,
    decimal Variance,
    string? Notes,
    Guid? ClosedByStaffId,
    DateTime ClosedAt,
    bool IsVerified,
    Guid? VerifiedByStaffId);

/// <summary>
/// Request to verify a day-end close.
/// </summary>
public record VerifyDayEndRequest(Guid VerifiedByStaffId);

// ==================== INVENTORY ITEM DTOs ====================

/// <summary>
/// Request to create a new inventory item.
/// </summary>
public record CreateInventoryItemRequest(
    string SKU,
    string Name,
    string Category,
    decimal InitialStock,
    decimal ReorderLevel,
    string StockUnit = "unit",
    decimal CostPrice = 0,
    decimal? SellingPrice = null);

/// <summary>
/// Request to update an inventory item.
/// </summary>
public record UpdateInventoryItemRequest(
    string? Name = null,
    decimal? ReorderLevel = null,
    decimal? CostPrice = null,
    decimal? SellingPrice = null,
    string? StockUnit = null);

/// <summary>
/// Response containing inventory item details.
/// </summary>
public record InventoryItemResponse(
    Guid Id,
    Guid PropertyId,
    string SKU,
    string Name,
    string Category,
    decimal CurrentStock,
    decimal ReorderLevel,
    string StockUnit,
    decimal CostPrice,
    decimal? SellingPrice,
    bool IsActive,
    bool IsLowStock,
    DateTime? LastStockCountAt,
    DateTime CreatedAt);

/// <summary>
/// Request to deplete inventory stock (typically on sale).
/// </summary>
public record DepeleStockRequest(
    decimal Quantity,
    string Reason = "Sale");

/// <summary>
/// Request to receive inventory stock.
/// </summary>
public record ReceiveStockRequest(
    decimal Quantity,
    string Reason = "Stock Receipt");

/// <summary>
/// Request to adjust inventory stock based on physical count.
/// </summary>
public record AdjustStockRequest(
    decimal NewQuantity,
    string Reason);

// ==================== SUMMARY & ANALYTICS DTOs ====================

/// <summary>
/// Daily sales summary response.
/// </summary>
public record DailySalesSummaryResponse(
    Guid PropertyId,
    DateTime Date,
    int TransactionCount,
    decimal TotalAmount,
    decimal TotalVAT,
    decimal TotalWithVAT,
    Dictionary<string, decimal> SalesByPaymentMethod,
    DateTime CalculatedAt);

/// <summary>
/// Inventory valuation response.
/// </summary>
public record InventoryValuationResponse(
    Guid PropertyId,
    int ItemCount,
    decimal TotalQuantity,
    decimal TotalCostValue,
    decimal TotalRetailValue,
    decimal GrossMargin,
    DateTime CalculatedAt);

/// <summary>
/// Low stock alert response.
/// </summary>
public record LowStockAlertResponse(
    Guid ItemId,
    string SKU,
    string Name,
    decimal CurrentStock,
    decimal ReorderLevel,
    int UnitsBelow);

/// <summary>
/// Inventory search filter request.
/// </summary>
public record SearchInventoryRequest(
    string? Category = null,
    string? SKUPattern = null,
    string? NamePattern = null,
    bool? IsActive = null,
    bool? IsLowStock = null,
    int PageNumber = 1,
    int PageSize = 50);

/// <summary>
/// Paginated inventory response.
/// </summary>
public record PaginatedInventoryResponse(
    List<InventoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

/// <summary>
/// Low stock report response.
/// </summary>
public record LowStockReportResponse(
    Guid PropertyId,
    int AlertCount,
    List<LowStockAlertResponse> Items);

// ==================== ERROR DTOs ====================

/// <summary>
/// Error response for API failures.
/// </summary>
public record ErrorResponse(
    string Code,
    string Message,
    string? Details = null,
    DateTime Timestamp = default)
{
    public ErrorResponse(string code, string message, string? details = null)
        : this(code, message, details, DateTime.UtcNow)
    {
    }
}

/// <summary>
/// Validation error response.
/// </summary>
public record ValidationErrorResponse(
    string Code,
    string Message,
    Dictionary<string, string[]> Errors,
    DateTime Timestamp = default)
{
    public ValidationErrorResponse(string code, string message, Dictionary<string, string[]> errors)
        : this(code, message, errors, DateTime.UtcNow)
    {
    }
}

// ==================== INLINE OPERATIONS ====================

/// <summary>
/// Request to record a sale and deplete inventory in a single operation.
/// </summary>
public record RecordSaleWithInventoryRequest(
    Guid InventoryItemId,
    decimal Quantity,
    string PaymentMethod = "cash",
    Guid? RecordedByStaffId = null,
    string? Notes = null)
{
    // Derived from inventory item details
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
}
