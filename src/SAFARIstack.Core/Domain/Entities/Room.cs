using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Room Type entity — defines a category of rooms with pricing and capacity
/// </summary>
public class RoomType : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public int MaxGuests { get; private set; }
    public int MaxAdults { get; private set; }
    public int MaxChildren { get; private set; }
    public int RoomCount { get; private set; }
    public int? SizeInSquareMeters { get; private set; }
    public string? BedConfiguration { get; private set; }           // "1 King" / "2 Twin"
    public string? ViewType { get; private set; }                   // "Bush", "Pool", "Garden"
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Property Property { get; private set; } = null!;
    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();
    private readonly List<RoomTypeAmenity> _amenities = new();
    public IReadOnlyCollection<RoomTypeAmenity> Amenities => _amenities.AsReadOnly();
    private readonly List<Rate> _rates = new();
    public IReadOnlyCollection<Rate> Rates => _rates.AsReadOnly();

    private RoomType() { } // EF Core

    public static RoomType Create(
        Guid propertyId,
        string name,
        string code,
        decimal basePrice,
        int maxGuests,
        int maxAdults = 2,
        int maxChildren = 0)
    {
        return new RoomType
        {
            PropertyId = propertyId,
            Name = name,
            Code = code,
            BasePrice = basePrice,
            MaxGuests = maxGuests,
            MaxAdults = maxAdults,
            MaxChildren = maxChildren
        };
    }

    public void UpdatePricing(decimal newBasePrice)
    {
        if (newBasePrice < 0) throw new ArgumentException("Base price cannot be negative.");
        BasePrice = newBasePrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}

/// <summary>
/// Physical Room entity — an individual bookable unit
/// </summary>
public class Room : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public string RoomNumber { get; private set; } = string.Empty;
    public int? Floor { get; private set; }
    public string? Wing { get; private set; }
    public RoomStatus Status { get; private set; } = RoomStatus.Available;
    public HousekeepingStatus HkStatus { get; private set; } = HousekeepingStatus.Clean;
    public string? Notes { get; private set; }
    public DateTime? LastCleanedAt { get; private set; }
    public DateTime? NextMaintenanceDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Property Property { get; private set; } = null!;
    public RoomType RoomType { get; private set; } = null!;

    private Room() { } // EF Core

    public static Room Create(
        Guid propertyId,
        Guid roomTypeId,
        string roomNumber,
        int? floor = null,
        string? wing = null)
    {
        return new Room
        {
            PropertyId = propertyId,
            RoomTypeId = roomTypeId,
            RoomNumber = roomNumber,
            Floor = floor,
            Wing = wing
        };
    }

    public void UpdateStatus(RoomStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkClean(DateTime cleanedAt)
    {
        HkStatus = HousekeepingStatus.Clean;
        LastCleanedAt = cleanedAt;
        if (Status == RoomStatus.Cleaning) Status = RoomStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDirty()
    {
        HkStatus = HousekeepingStatus.Dirty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PutOutOfService(string reason)
    {
        Status = RoomStatus.OutOfService;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}

public enum RoomStatus
{
    Available,
    Occupied,
    Maintenance,
    Cleaning,
    OutOfService,
    Blocked             // Manually blocked (renovation, VIP hold)
}

public enum HousekeepingStatus
{
    Clean,
    Dirty,
    Inspected,
    OutOfOrder
}

/// <summary>
/// Room Block — manually block a room for a date range (renovation, VIP hold, etc.)
/// </summary>
public class RoomBlock : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid RoomId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public RoomBlockReason Reason { get; private set; }
    public string? Notes { get; private set; }

    public Room Room { get; private set; } = null!;

    private RoomBlock() { }

    public static RoomBlock Create(Guid propertyId, Guid roomId, DateTime start, DateTime end,
        RoomBlockReason reason, string? notes = null)
    {
        if (end <= start) throw new ArgumentException("Block end must be after start.");
        return new RoomBlock
        {
            PropertyId = propertyId,
            RoomId = roomId,
            StartDate = start,
            EndDate = end,
            Reason = reason,
            Notes = notes
        };
    }
}

public enum RoomBlockReason { Renovation, VIPHold, Maintenance, OwnerUse, GroupBlock, Other }

/// <summary>
/// Booking-Room many-to-many relationship with nightly rate tracking
/// </summary>
public class BookingRoom : AuditableEntity
{
    public Guid BookingId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public Guid? RatePlanId { get; private set; }
    public decimal RateApplied { get; private set; }
    public string? GuestNames { get; private set; }

    // Navigation
    public Booking Booking { get; private set; } = null!;
    public Room Room { get; private set; } = null!;
    public RoomType RoomType { get; private set; } = null!;
    public RatePlan? RatePlan { get; private set; }

    private BookingRoom() { } // EF Core

    public static BookingRoom Create(
        Guid bookingId,
        Guid roomId,
        Guid roomTypeId,
        decimal rateApplied,
        Guid? ratePlanId = null)
    {
        return new BookingRoom
        {
            BookingId = bookingId,
            RoomId = roomId,
            RoomTypeId = roomTypeId,
            RateApplied = rateApplied,
            RatePlanId = ratePlanId
        };
    }
}
