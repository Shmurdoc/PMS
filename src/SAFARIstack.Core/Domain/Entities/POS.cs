using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// CasualSale — Records of casual point-of-sale transactions (F&B, gifts, etc.)
/// 
/// Used for:
/// - Recording ad-hoc sales (e.g., coffee, snacks, merchandise)
/// - Tracking cash/card payments outside folio system
/// - Day-end cash reconciliation
/// - Revenue analytics
/// </summary>
public class CasualSale : AuditableEntity
{
    /// <summary>The property where sale occurred</summary>
    public Guid PropertyId { get; private set; }

    /// <summary>Date and time of sale</summary>
    public DateTime SaleDate { get; private set; }

    /// <summary>Description of item sold (e.g., "Coffee", "Juice", "Merchandise")</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Quantity of item sold</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Unit price per item</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Total sale amount (before VAT)</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>VAT/Tax rate applied (default 15% in South Africa)</summary>
    public decimal VatRate { get; private set; } = 0.15m;

    /// <summary>VAT/Tax amount</summary>
    public decimal VatAmount { get; private set; }

    /// <summary>Final amount including VAT</summary>
    public decimal TotalWithVat => TotalAmount + VatAmount;

    /// <summary>Payment method used (e.g., "cash", "card", "mobile")</summary>
    public string PaymentMethod { get; private set; } = "cash";

    /// <summary>Staff member who recorded the sale</summary>
    public Guid? RecordedByStaffId { get; private set; }

    /// <summary>Notes or additional details about the sale</summary>
    public string? Notes { get; private set; }

    /// <summary>Navigation property</summary>
    public Property? Property { get; private set; }

    /// <summary>Factory method to create a new casual sale</summary>
    public static CasualSale Create(
        Guid propertyId,
        string description,
        decimal quantity,
        decimal unitPrice,
        string paymentMethod = "cash",
        Guid? recordedByStaffId = null,
        string? notes = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        var totalAmount = quantity * unitPrice;
        var vatAmount = totalAmount * 0.15m; // 15% VAT

        return new CasualSale
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            SaleDate = DateTime.UtcNow,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalAmount = totalAmount,
            VatAmount = vatAmount,
            PaymentMethod = paymentMethod,
            RecordedByStaffId = recordedByStaffId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Update sale details (only if not reconciled)</summary>
    public void Update(string description, decimal quantity, decimal unitPrice, string paymentMethod)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero");
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative");

        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        PaymentMethod = paymentMethod;
        TotalAmount = quantity * unitPrice;
        VatAmount = TotalAmount * 0.15m;
    }
}

/// <summary>
/// DayEndClose — Daily reconciliation of cash drawer and sales
/// 
/// Used for:
/// - Recording expected vs. actual cash at end of day
/// - Tracking cash variance (overage/shortage)
/// - Audit trail of daily closes
/// - Staff accountability
/// </summary>
public class DayEndClose : AuditableEntity
{
    /// <summary>The property being closed</summary>
    public Guid PropertyId { get; private set; }

    /// <summary>Date of close (truncated to date only)</summary>
    public DateTime CloseDate { get; private set; }

    /// <summary>Expected cash from all sales and payments</summary>
    public decimal ExpectedCash { get; private set; }

    /// <summary>Actual cash counted in register</summary>
    public decimal ActualCash { get; private set; }

    /// <summary>Variance (ActualCash - ExpectedCash). Negative = shortage, Positive = overage</summary>
    public decimal Variance => ActualCash - ExpectedCash;

    /// <summary>Notes about any discrepancies</summary>
    public string? Notes { get; private set; }

    /// <summary>Staff member who performed the close</summary>
    public Guid? ClosedByStaffId { get; private set; }

    /// <summary>Timestamp of close</summary>
    public DateTime ClosedAt { get; private set; }

    /// <summary>Whether close has been verified by manager</summary>
    public bool IsVerified { get; private set; }

    /// <summary>Staff member who verified the close</summary>
    public Guid? VerifiedByStaffId { get; private set; }

    /// <summary>Navigation property</summary>
    public Property? Property { get; private set; }

    /// <summary>Factory method to create a new day-end close</summary>
    public static DayEndClose Create(
        Guid propertyId,
        decimal expectedCash,
        decimal actualCash,
        Guid? closedByStaffId = null,
        string? notes = null)
    {
        return new DayEndClose
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            CloseDate = DateTime.UtcNow.Date,
            ExpectedCash = expectedCash,
            ActualCash = actualCash,
            Notes = notes,
            ClosedByStaffId = closedByStaffId,
            ClosedAt = DateTime.UtcNow,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Verify the close (manager approval)</summary>
    public void Verify(Guid verifiedByStaffId)
    {
        IsVerified = true;
        VerifiedByStaffId = verifiedByStaffId;
    }

    /// <summary>Update with actual cash count</summary>
    public void UpdateActualCash(decimal actualCash, string? notes = null)
    {
        ActualCash = actualCash;
        if (notes != null) Notes = notes;
    }
}

/// <summary>
/// InventoryItem — Stock levels for POS items (consumables, merchandise)
/// 
/// Used for:
/// - Tracking stock of POS items
/// - Depletion on sale
/// - Reorder alerts when stock low
/// - Cost of goods sold (COGS) calculations
/// </summary>
public class InventoryItem : AuditableEntity
{
    /// <summary>The property holding the inventory</summary>
    public Guid PropertyId { get; private set; }

    /// <summary>SKU (stock keeping unit) — unique product identifier</summary>
    public string SKU { get; private set; } = string.Empty;

    /// <summary>Product name/description</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Category (e.g., "Beverages", "Merchandise", "Supplies")</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Current quantity in stock</summary>
    public decimal CurrentStock { get; private set; }

    /// <summary>Minimum quantity before reorder alert</summary>
    public decimal ReorderLevel { get; private set; }

    /// <summary>Unit of measure (e.g., "unit", "kg", "liter")</summary>
    public string StockUnit { get; private set; } = "unit";

    /// <summary>Cost price per unit</summary>
    public decimal CostPrice { get; private set; }

    /// <summary>Selling price per unit (for POS)</summary>
    public decimal? SellingPrice { get; private set; }

    /// <summary>Whether this item is still in use</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Last date physical stock count was performed</summary>
    public DateTime? LastStockCountAt { get; private set; }

    /// <summary>Navigation property</summary>
    public Property? Property { get; private set; }

    /// <summary>Factory method to create a new inventory item</summary>
    public static InventoryItem Create(
        Guid propertyId,
        string sku,
        string name,
        string category,
        decimal initialStock,
        decimal reorderLevel,
        string stockUnit = "unit",
        decimal costPrice = 0,
        decimal? sellingPrice = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU is required");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
        if (costPrice < 0) throw new ArgumentException("Cost price cannot be negative");
        if (sellingPrice < 0) throw new ArgumentException("Selling price cannot be negative");

        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            SKU = sku.Trim(),
            Name = name.Trim(),
            Category = category.Trim(),
            CurrentStock = initialStock,
            ReorderLevel = reorderLevel,
            StockUnit = stockUnit,
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            IsActive = true,
            LastStockCountAt = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Decrease stock on sale</summary>
    public void Deplete(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero");
        if (quantity > CurrentStock) throw new InvalidOperationException("Insufficient stock");

        CurrentStock -= quantity;
    }

    /// <summary>Increase stock on receipt</summary>
    public void Receive(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero");

        CurrentStock += quantity;
        LastStockCountAt = DateTime.UtcNow;
    }

    /// <summary>Adjust stock for physical count discrepancy</summary>
    public void AdjustForCount(decimal countedQuantity, string reason = "Physical count")
    {
        CurrentStock = countedQuantity;
        LastStockCountAt = DateTime.UtcNow;
    }

    /// <summary>Check if stock is below reorder level</summary>
    public bool IsLowStock => CurrentStock <= ReorderLevel;

    /// <summary>Deactivate item from inventory</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivate item in inventory</summary>
    public void Reactivate() => IsActive = true;

    /// <summary>Calculate cost of goods sold for given quantity</summary>
    public decimal CalculateCOGS(decimal quantity) => quantity * CostPrice;
}
