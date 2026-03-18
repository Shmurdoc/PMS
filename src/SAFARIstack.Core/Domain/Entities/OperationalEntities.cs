using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
//  SERVICE REQUEST — Guest in-stay requests (towels, room service, etc.)
// ═══════════════════════════════════════════════════════════════════════
public class ServiceRequest : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? GuestId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? RoomId { get; private set; }
    public ServiceRequestType RequestType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ServiceRequestStatus Status { get; private set; } = ServiceRequestStatus.Submitted;
    public ServiceRequestPriority Priority { get; private set; } = ServiceRequestPriority.Normal;
    public Guid? AssignedToStaffId { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public int? GuestRating { get; private set; } // 1-5 post-completion

    // Navigation
    public Guest? Guest { get; private set; }
    public Room? Room { get; private set; }

    private ServiceRequest() { }

    public static ServiceRequest Create(
        Guid propertyId, ServiceRequestType type, string description,
        Guid? guestId = null, Guid? bookingId = null, Guid? roomId = null,
        ServiceRequestPriority priority = ServiceRequestPriority.Normal)
    {
        return new ServiceRequest
        {
            PropertyId = propertyId,
            GuestId = guestId,
            BookingId = bookingId,
            RoomId = roomId,
            RequestType = type,
            Description = description,
            Priority = priority
        };
    }

    public void Acknowledge(Guid staffId)
    {
        Status = ServiceRequestStatus.Acknowledged;
        AssignedToStaffId = staffId;
        AcknowledgedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        Status = ServiceRequestStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(string? notes = null)
    {
        Status = ServiceRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ResolutionNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = ServiceRequestStatus.Cancelled;
        ResolutionNotes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rate(int rating)
    {
        if (rating < 1 || rating > 5) throw new ArgumentException("Rating must be 1-5");
        GuestRating = rating;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ServiceRequestType
{
    Towels, ExtraPillow, ExtraBlanket, RoomService,
    Maintenance, Housekeeping, Amenities, WakeUpCall,
    Transportation, Luggage, Laundry, Other
}

public enum ServiceRequestStatus
{
    Submitted, Acknowledged, InProgress, Completed, Cancelled
}

public enum ServiceRequestPriority
{
    Low, Normal, High, Urgent
}

// ═══════════════════════════════════════════════════════════════════════
//  MAINTENANCE TASK — Work orders for property maintenance
// ═══════════════════════════════════════════════════════════════════════
public class MaintenanceTask : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? RoomId { get; private set; }
    public Guid? ReportedByUserId { get; private set; }
    public Guid? AssignedToStaffId { get; private set; }
    public MaintenanceCategory Category { get; private set; }
    public MaintenancePriority Priority { get; private set; } = MaintenancePriority.Medium;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public MaintenanceStatus Status { get; private set; } = MaintenanceStatus.Open;
    public DateTime? ScheduledDate { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public string? VendorName { get; private set; }
    public string? Notes { get; private set; }
    public bool IsRecurring { get; private set; }
    public int? RecurrenceIntervalDays { get; private set; }

    // Navigation
    public Room? Room { get; private set; }

    private MaintenanceTask() { }

    public static MaintenanceTask Create(
        Guid propertyId, string title, string description,
        MaintenanceCategory category, MaintenancePriority priority = MaintenancePriority.Medium,
        Guid? roomId = null, Guid? reportedByUserId = null)
    {
        return new MaintenanceTask
        {
            PropertyId = propertyId,
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            RoomId = roomId,
            ReportedByUserId = reportedByUserId
        };
    }

    public void Assign(Guid staffId)
    {
        AssignedToStaffId = staffId;
        Status = MaintenanceStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        Status = MaintenanceStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(decimal? actualCost = null, string? notes = null)
    {
        Status = MaintenanceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ActualCost = actualCost;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        Status = MaintenanceStatus.Verified;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum MaintenanceCategory
{
    Plumbing, Electrical, HVAC, Structural, Appliance,
    Furniture, Painting, Flooring, Exterior, Landscaping,
    Elevator, FireSafety, Security, IT, General, Other
}

public enum MaintenancePriority { Low, Medium, High, Emergency }

public enum MaintenanceStatus
{
    Open, Assigned, InProgress, Completed, Verified, OnHold, Cancelled
}

// ═══════════════════════════════════════════════════════════════════════
//  DINING VENUE — Restaurant/bar/lounge information
// ═══════════════════════════════════════════════════════════════════════
public class DiningVenue : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? CuisineType { get; private set; }
    public string? OpeningTime { get; private set; }
    public string? ClosingTime { get; private set; }
    public string? MenuUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public int? Capacity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DiningVenueType VenueType { get; private set; } = DiningVenueType.Restaurant;

    private DiningVenue() { }

    public static DiningVenue Create(
        Guid propertyId, string name, DiningVenueType venueType,
        string? description = null, string? cuisineType = null,
        string? openingTime = null, string? closingTime = null)
    {
        return new DiningVenue
        {
            PropertyId = propertyId,
            Name = name,
            VenueType = venueType,
            Description = description,
            CuisineType = cuisineType,
            OpeningTime = openingTime,
            ClosingTime = closingTime
        };
    }
}

public enum DiningVenueType { Restaurant, Bar, Lounge, PoolBar, RoomService, Cafe, Buffet }
