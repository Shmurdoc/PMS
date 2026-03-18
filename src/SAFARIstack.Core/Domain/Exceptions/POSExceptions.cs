using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.Core.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to sell more inventory than is available.
/// </summary>
public class InsufficientInventoryException : DomainException
{
    public Guid ItemId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string ItemName { get; set; }

    public InsufficientInventoryException(string itemName, Guid itemId, decimal requested, decimal available)
        : base($"Insufficient inventory for '{itemName}'. Requested: {requested}, Available: {available}")
    {
        ItemName = itemName;
        ItemId = itemId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

/// <summary>
/// Thrown when a sale cannot be recorded due to validation failure.
/// </summary>
public class InvalidSaleException : DomainException
{
    public InvalidSaleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when attempting to create a day-end close when one already exists.
/// </summary>
public class DayEndAlreadyClosedException : DomainException
{
    public Guid PropertyId { get; set; }
    public DateTime Date { get; set; }

    public DayEndAlreadyClosedException(Guid propertyId, DateTime date)
        : base($"Day-end already closed for property {propertyId} on {date:yyyy-MM-dd}")
    {
        PropertyId = propertyId;
        Date = date;
    }
}

/// <summary>
/// Thrown when attempting to verify a day-end that's already verified.
/// </summary>
public class DayEndAlreadyVerifiedException : DomainException
{
    public Guid CloseId { get; set; }

    public DayEndAlreadyVerifiedException(Guid closeId)
        : base($"Day-end close {closeId} is already verified")
    {
        CloseId = closeId;
    }
}

/// <summary>
/// Thrown when inventory item is not found.
/// </summary>
public class InventoryItemNotFound : DomainException
{
    public Guid ItemId { get; set; }

    public InventoryItemNotFound(Guid itemId)
        : base($"Inventory item {itemId} not found")
    {
        ItemId = itemId;
    }
}

/// <summary>
/// Thrown when an inventory item SKU is not unique for a property.
/// </summary>
public class DuplicateSKUException : DomainException
{
    public string SKU { get; set; }
    public Guid PropertyId { get; set; }

    public DuplicateSKUException(string sku, Guid propertyId)
        : base($"Inventory item with SKU '{sku}' already exists for this property")
    {
        SKU = sku;
        PropertyId = propertyId;
    }
}

/// <summary>
/// Thrown when attempting to perform an operation on an inactive item.
/// </summary>
public class InactiveItemException : DomainException
{
    public Guid ItemId { get; set; }

    public InactiveItemException(Guid itemId)
        : base($"Cannot perform operation on inactive inventory item {itemId}")
    {
        ItemId = itemId;
    }
}

/// <summary>
/// Thrown when invalid adjustment data is provided.
/// </summary>
public class InvalidInventoryAdjustmentException : DomainException
{
    public InvalidInventoryAdjustmentException(string message) : base(message) { }
}

/// <summary>
/// Thrown when attempting to access POS data from a different property context.
/// </summary>
public class PropertyAccessDeniedException : DomainException
{
    public Guid RequestedPropertyId { get; set; }
    public Guid ActualPropertyId { get; set; }

    public PropertyAccessDeniedException(Guid requestedId, Guid actualId)
        : base($"Property mismatch: requested access to property {requestedId} but resource belongs to property {actualId}")
    {
        RequestedPropertyId = requestedId;
        ActualPropertyId = actualId;
    }
}
