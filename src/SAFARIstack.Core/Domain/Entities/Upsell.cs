using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
// UPSELL ENGINE — Automated, Personalized Revenue Offers
// ═══════════════════════════════════════════════════════════════════════

public class UpsellOffer : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public UpsellOfferType OfferType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal OriginalPrice { get; private set; }
    public decimal OfferPrice { get; private set; }
    public decimal CostPrice { get; private set; } // For margin reporting
    public decimal Savings => OriginalPrice - OfferPrice;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int? InventoryTotal { get; private set; }
    public int? InventoryRemaining { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }

    // Targeting conditions (JSON-backed)
    public int? MinNights { get; private set; }
    public string? MinLoyaltyTier { get; private set; }
    public string? GuestType { get; private set; } // Leisure, Corporate, etc.
    public string? BookingSource { get; private set; } // Direct, OTA, etc.
    public string? ApplicableDays { get; private set; } // Comma-separated: Mon,Tue,Wed
    public int? MaxDaysBeforeArrival { get; private set; }

    // Navigation
    private readonly List<UpsellTransaction> _transactions = new();
    public IReadOnlyCollection<UpsellTransaction> Transactions => _transactions.AsReadOnly();

    private UpsellOffer() { }

    public static UpsellOffer Create(
        Guid propertyId,
        UpsellOfferType offerType,
        string title,
        decimal originalPrice,
        decimal offerPrice,
        string? description = null,
        decimal costPrice = 0,
        string? imageUrl = null,
        int? inventoryTotal = null,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        if (offerPrice > originalPrice)
            throw new ArgumentException("Offer price cannot exceed original price.");

        return new UpsellOffer
        {
            PropertyId = propertyId,
            OfferType = offerType,
            Title = title.Trim(),
            Description = description?.Trim(),
            OriginalPrice = originalPrice,
            OfferPrice = offerPrice,
            CostPrice = costPrice,
            ImageUrl = imageUrl,
            InventoryTotal = inventoryTotal,
            InventoryRemaining = inventoryTotal,
            ValidFrom = validFrom,
            ValidTo = validTo
        };
    }

    public void SetTargeting(int? minNights, string? minLoyaltyTier, string? guestType,
        string? bookingSource, string? applicableDays, int? maxDaysBeforeArrival)
    {
        MinNights = minNights;
        MinLoyaltyTier = minLoyaltyTier;
        GuestType = guestType;
        BookingSource = bookingSource;
        ApplicableDays = applicableDays;
        MaxDaysBeforeArrival = maxDaysBeforeArrival;
    }

    public bool IsEligible(Booking booking, GuestLoyalty? loyalty)
    {
        if (!IsActive) return false;
        if (ValidFrom.HasValue && DateTime.UtcNow < ValidFrom.Value) return false;
        if (ValidTo.HasValue && DateTime.UtcNow > ValidTo.Value) return false;
        if (InventoryRemaining.HasValue && InventoryRemaining.Value <= 0) return false;

        var nights = (booking.CheckOutDate - booking.CheckInDate).Days;
        if (MinNights.HasValue && nights < MinNights.Value) return false;

        if (!string.IsNullOrEmpty(MinLoyaltyTier) && loyalty != null)
        {
            var tierOrder = new[] { "None", "Bronze", "Silver", "Gold", "Platinum" };
            var requiredIdx = Array.IndexOf(tierOrder, MinLoyaltyTier);
            var guestIdx = Array.IndexOf(tierOrder, loyalty.Tier.ToString());
            if (guestIdx < requiredIdx) return false;
        }

        return true;
    }

    public UpsellTransaction Purchase(Guid bookingId, Guid guestId, int quantity = 1)
    {
        if (!IsActive) throw new InvalidOperationException("Offer is not active.");
        if (InventoryRemaining.HasValue && InventoryRemaining.Value < quantity)
            throw new InvalidOperationException("Insufficient inventory.");

        if (InventoryRemaining.HasValue)
            InventoryRemaining -= quantity;

        var transaction = UpsellTransaction.Create(Id, bookingId, guestId, OfferPrice, quantity);
        _transactions.Add(transaction);
        return transaction;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
    public void UpdatePricing(decimal originalPrice, decimal offerPrice, decimal costPrice)
    {
        OriginalPrice = originalPrice;
        OfferPrice = offerPrice;
        CostPrice = costPrice;
    }
}

public enum UpsellOfferType
{
    RoomUpgrade,
    EarlyCheckIn,
    LateCheckOut,
    Spa,
    Dining,
    Activity,
    Transfer,
    Champagne,
    Celebration,
    Parking,
    Minibar,
    Laundry,
    Safari,
    Other
}

public class UpsellTransaction : AuditableEntity
{
    public Guid OfferId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid GuestId { get; private set; }
    public int Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public bool AddedToFolio { get; private set; }
    public Guid? FolioId { get; private set; }
    public DateTime? RedeemedAt { get; private set; }
    public string? RedemptionNotes { get; private set; }
    public UpsellTransactionStatus Status { get; private set; } = UpsellTransactionStatus.Purchased;

    // Navigation
    public UpsellOffer Offer { get; private set; } = null!;
    public Booking Booking { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;

    private UpsellTransaction() { }

    public static UpsellTransaction Create(Guid offerId, Guid bookingId, Guid guestId, decimal unitPrice, int quantity = 1)
    {
        return new UpsellTransaction
        {
            OfferId = offerId,
            BookingId = bookingId,
            GuestId = guestId,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }

    public void AddToFolio(Guid folioId)
    {
        FolioId = folioId;
        AddedToFolio = true;
    }

    public void MarkRedeemed(string? notes = null)
    {
        RedeemedAt = DateTime.UtcNow;
        RedemptionNotes = notes;
        Status = UpsellTransactionStatus.Redeemed;
    }

    public void Cancel(string reason)
    {
        Status = UpsellTransactionStatus.Cancelled;
        RedemptionNotes = reason;
    }
}

public enum UpsellTransactionStatus { Purchased, AddedToFolio, Redeemed, Cancelled, Refunded }
