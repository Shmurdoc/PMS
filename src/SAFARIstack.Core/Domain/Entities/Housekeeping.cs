using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
//  HOUSEKEEPING TASK — Room turnover and cleaning management
// ═══════════════════════════════════════════════════════════════════════
public class HousekeepingTask : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid? AssignedToStaffId { get; private set; }
    public HousekeepingTaskType TaskType { get; private set; }
    public HousekeepingPriority Priority { get; private set; } = HousekeepingPriority.Normal;
    public HousekeepingTaskStatus Status { get; private set; } = HousekeepingTaskStatus.Pending;
    public DateTime ScheduledDate { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public string? InspectionNotes { get; private set; }
    public Guid? InspectedByStaffId { get; private set; }
    public bool PassedInspection { get; private set; }

    // Checklist tracking (stored as JSON or related items)
    public bool LinenChanged { get; private set; }
    public bool BathroomCleaned { get; private set; }
    public bool FloorsCleaned { get; private set; }
    public bool MinibarRestocked { get; private set; }
    public bool AmenitiesReplenished { get; private set; }

    // Navigation
    public Room Room { get; private set; } = null!;

    private HousekeepingTask() { }

    public static HousekeepingTask Create(
        Guid propertyId, Guid roomId, HousekeepingTaskType taskType,
        DateTime scheduledDate, HousekeepingPriority priority = HousekeepingPriority.Normal)
    {
        var task = new HousekeepingTask
        {
            PropertyId = propertyId,
            RoomId = roomId,
            TaskType = taskType,
            ScheduledDate = scheduledDate,
            Priority = priority
        };
        task.AddDomainEvent(new HousekeepingTaskCreatedEvent(task.Id, roomId, taskType));
        return task;
    }

    public void AssignTo(Guid staffId)
    {
        AssignedToStaffId = staffId;
        Status = HousekeepingTaskStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        if (Status != HousekeepingTaskStatus.Assigned && Status != HousekeepingTaskStatus.Pending)
            throw new InvalidOperationException($"Cannot start task in {Status} status.");

        Status = HousekeepingTaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(bool linenChanged, bool bathroomCleaned, bool floorsCleaned,
        bool minibarRestocked, bool amenitiesReplenished)
    {
        if (Status != HousekeepingTaskStatus.InProgress)
            throw new InvalidOperationException("Task must be in progress to complete.");

        LinenChanged = linenChanged;
        BathroomCleaned = bathroomCleaned;
        FloorsCleaned = floorsCleaned;
        MinibarRestocked = minibarRestocked;
        AmenitiesReplenished = amenitiesReplenished;
        CompletedAt = DateTime.UtcNow;
        DurationMinutes = StartedAt.HasValue ? (int)(CompletedAt.Value - StartedAt.Value).TotalMinutes : null;
        Status = HousekeepingTaskStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new HousekeepingTaskCompletedEvent(Id, RoomId, DurationMinutes ?? 0));
    }

    public void Inspect(Guid inspectorStaffId, bool passed, string? notes)
    {
        if (Status != HousekeepingTaskStatus.Completed)
            throw new InvalidOperationException("Task must be completed before inspection.");

        InspectedByStaffId = inspectorStaffId;
        PassedInspection = passed;
        InspectionNotes = notes;
        Status = passed ? HousekeepingTaskStatus.Inspected : HousekeepingTaskStatus.FailedInspection;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum HousekeepingTaskType
{
    Turnover,          // Full changeover between guests
    StayOver,          // Daily clean during guest stay
    DeepClean,         // Periodic deep cleaning
    Inspection,        // Quality check only
    TouchUp,           // Quick refresh
    Maintenance         // Repair or fix required
}

public enum HousekeepingPriority { Low, Normal, High, Urgent }

public enum HousekeepingTaskStatus
{
    Pending,
    Assigned,
    InProgress,
    Completed,
    Inspected,
    FailedInspection
}

// ═══════════════════════════════════════════════════════════════════════
//  AMENITY — Room and property amenity catalogue
// ═══════════════════════════════════════════════════════════════════════
public class Amenity : Entity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }                       // CSS icon class
    public AmenityCategory Category { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Amenity() { }

    public static Amenity Create(Guid propertyId, string name, AmenityCategory category, string? icon = null) =>
        new() { PropertyId = propertyId, Name = name, Category = category, Icon = icon };
}

public enum AmenityCategory
{
    RoomBasic,         // WiFi, AC, TV
    Bathroom,          // Shower, bath, hairdryer
    Kitchen,           // Minibar, kettle, fridge
    Outdoor,           // Pool, garden, braai area
    Services,          // Laundry, room service
    Safari,            // Game drive, bush walk
    Business,          // Desk, meeting room
    Accessibility      // Wheelchair, hearing aid
}

/// <summary>
/// Many-to-many: which amenities each room type offers
/// </summary>
public class RoomTypeAmenity : Entity
{
    public Guid RoomTypeId { get; private set; }
    public Guid AmenityId { get; private set; }

    public RoomType RoomType { get; private set; } = null!;
    public Amenity Amenity { get; private set; } = null!;

    private RoomTypeAmenity() { }

    public static RoomTypeAmenity Create(Guid roomTypeId, Guid amenityId) =>
        new() { RoomTypeId = roomTypeId, AmenityId = amenityId };
}

// ─── Domain Events ───────────────────────────────────────────────────
public record HousekeepingTaskCreatedEvent(Guid TaskId, Guid RoomId, HousekeepingTaskType TaskType) : DomainEvent;
public record HousekeepingTaskCompletedEvent(Guid TaskId, Guid RoomId, int DurationMinutes) : DomainEvent;
