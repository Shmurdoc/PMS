using SAFARIstack.Shared.Domain;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Booking aggregate root with SA financial compliance, rate plan, and folio linkage
/// </summary>
public class Booking : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid GuestId { get; private set; }
    public Guid? RatePlanId { get; private set; }
    public string BookingReference { get; private set; } = string.Empty;
    public BookingSource Source { get; private set; } = BookingSource.Direct;
    public BookingStatus Status { get; private set; } = BookingStatus.Confirmed;
    public DateTime CheckInDate { get; private set; }
    public DateTime CheckOutDate { get; private set; }
    public DateTime? ActualCheckInTime { get; private set; }
    public DateTime? ActualCheckOutTime { get; private set; }
    public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days;
    public int AdultCount { get; private set; }
    public int ChildCount { get; private set; }
    public string? SpecialRequests { get; private set; }
    public string? Notes { get; private set; }
    public string? ExternalReference { get; private set; }          // OTA booking ref

    // Financial (using Money value object and SA compliance)
    public decimal SubtotalAmount { get; private set; }
    public decimal VATAmount { get; private set; }
    public decimal TourismLevyAmount { get; private set; }
    public decimal AdditionalCharges { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount => TotalAmount - PaidAmount;

    // Audit
    public Guid? CheckedInByUserId { get; private set; }
    public Guid? CheckedOutByUserId { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation
    public Property Property { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;
    public RatePlan? RatePlan { get; private set; }
    public Folio? Folio { get; private set; }
    private readonly List<BookingRoom> _bookingRooms = new();
    public IReadOnlyCollection<BookingRoom> BookingRooms => _bookingRooms.AsReadOnly();

    private Booking() { } // EF Core

    private Booking(
        Guid propertyId,
        Guid guestId,
        string bookingReference,
        DateTime checkInDate,
        DateTime checkOutDate,
        int adultCount,
        int childCount)
    {
        if (checkOutDate <= checkInDate)
            throw new ArgumentException("Check-out date must be after check-in date");

        PropertyId = propertyId;
        GuestId = guestId;
        BookingReference = bookingReference;
        CheckInDate = checkInDate;
        CheckOutDate = checkOutDate;
        AdultCount = adultCount;
        ChildCount = childCount;
    }

    public static Booking Create(
        Guid propertyId,
        Guid guestId,
        string bookingReference,
        DateTime checkInDate,
        DateTime checkOutDate,
        int adultCount,
        int childCount,
        Guid? createdByUserId,
        Guid? ratePlanId = null,
        BookingSource source = BookingSource.Direct,
        string? externalReference = null)
    {
        var booking = new Booking(
            propertyId,
            guestId,
            bookingReference,
            checkInDate,
            checkOutDate,
            adultCount,
            childCount);

        booking.RatePlanId = ratePlanId;
        booking.Source = source;
        booking.ExternalReference = externalReference;
        if (createdByUserId.HasValue) booking.SetCreatedBy(createdByUserId.Value);
        booking.AddDomainEvent(new BookingCreatedEvent(booking.Id, bookingReference, propertyId));
        return booking;
    }

    public void CalculateFinancials(Money subtotal, Money? additionalCharges = null, Money? discount = null)
    {
        var breakdown = FinancialBreakdown.Calculate(subtotal, additionalCharges, discount);

        SubtotalAmount = breakdown.Subtotal.Amount;
        VATAmount = breakdown.VATAmount.Amount;
        TourismLevyAmount = breakdown.TourismLevy.Amount;
        AdditionalCharges = breakdown.AdditionalCharges.Amount;
        DiscountAmount = breakdown.DiscountAmount.Amount;
        TotalAmount = breakdown.Total.Amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRoom(BookingRoom bookingRoom)
    {
        _bookingRooms.Add(bookingRoom);
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckIn(Guid checkedInByUserId)
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot check in booking with status {Status}");

        Status = BookingStatus.CheckedIn;
        CheckedInByUserId = checkedInByUserId;
        ActualCheckInTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BookingCheckedInEvent(Id, BookingReference));
    }

    public void CheckOut(Guid checkedOutByUserId)
    {
        if (Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException($"Cannot check out booking with status {Status}");

        Status = BookingStatus.CheckedOut;
        CheckedOutByUserId = checkedOutByUserId;
        ActualCheckOutTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BookingCheckedOutEvent(Id, BookingReference));
    }

    public void Cancel(Guid cancelledByUserId, string reason)
    {
        if (Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled");

        Status = BookingStatus.Cancelled;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BookingCancelledEvent(Id, BookingReference, reason));
    }

    public void MarkNoShow()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be marked as no-show.");
        Status = BookingStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BookingNoShowEvent(Id, BookingReference));
    }

    public void RecordPayment(decimal amount)
    {
        PaidAmount += amount;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BookingPaymentReceivedEvent(Id, BookingReference, amount));
    }
}

public enum BookingStatus
{
    Tentative,
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled,
    NoShow,
    WaitListed
}

public enum BookingSource
{
    Direct,
    Website,
    BookingCom,
    Expedia,
    Airbnb,
    Agoda,
    TripAdvisor,
    Phone,
    Email,
    WalkIn,
    TravelAgent,
    Corporate,
    Other
}

// ─── Domain Events ───────────────────────────────────────────────────
public record BookingCreatedEvent(Guid BookingId, string BookingReference, Guid PropertyId) : DomainEvent;
public record BookingCheckedInEvent(Guid BookingId, string BookingReference) : DomainEvent;
public record BookingCheckedOutEvent(Guid BookingId, string BookingReference) : DomainEvent;
public record BookingCancelledEvent(Guid BookingId, string BookingReference, string Reason) : DomainEvent;
public record BookingNoShowEvent(Guid BookingId, string BookingReference) : DomainEvent;
public record BookingPaymentReceivedEvent(Guid BookingId, string BookingReference, decimal Amount) : DomainEvent;
