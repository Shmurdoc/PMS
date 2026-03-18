using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
// MULTI-PROPERTY ENTERPRISE — Property Groups, Rate Copying, Group Inventory
// ═══════════════════════════════════════════════════════════════════════

public class PropertyGroup : AuditableAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? HeadquartersAddress { get; private set; }
    public Guid? PrimaryContactUserId { get; private set; }
    public string BillingCycle { get; private set; } = "monthly"; // monthly, quarterly, annual
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    private readonly List<PropertyGroupMembership> _memberships = new();
    public IReadOnlyCollection<PropertyGroupMembership> Memberships => _memberships.AsReadOnly();

    private PropertyGroup() { }

    public static PropertyGroup Create(string name, string? description = null, string? headquartersAddress = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Property group name is required.", nameof(name));

        return new PropertyGroup
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            HeadquartersAddress = headquartersAddress?.Trim()
        };
    }

    public PropertyGroupMembership AddProperty(Guid propertyId, bool isFlagship = false)
    {
        if (_memberships.Any(m => m.PropertyId == propertyId))
            throw new InvalidOperationException($"Property {propertyId} is already a member of this group.");

        var membership = PropertyGroupMembership.Create(propertyId, Id, isFlagship);
        _memberships.Add(membership);
        return membership;
    }

    public void RemoveProperty(Guid propertyId)
    {
        var membership = _memberships.FirstOrDefault(m => m.PropertyId == propertyId)
            ?? throw new InvalidOperationException($"Property {propertyId} is not a member of this group.");
        _memberships.Remove(membership);
    }

    public void SetPrimaryContact(Guid userId) => PrimaryContactUserId = userId;
    public void UpdateDetails(string name, string? description, string? headquartersAddress, string billingCycle)
    {
        Name = name.Trim();
        Description = description?.Trim();
        HeadquartersAddress = headquartersAddress?.Trim();
        BillingCycle = billingCycle;
    }
    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}

public class PropertyGroupMembership : Entity
{
    public Guid PropertyId { get; private set; }
    public Guid GroupId { get; private set; }
    public bool IsFlagship { get; private set; }
    public DateTime JoinDate { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Property Property { get; private set; } = null!;
    public PropertyGroup Group { get; private set; } = null!;

    private PropertyGroupMembership() { }

    public static PropertyGroupMembership Create(Guid propertyId, Guid groupId, bool isFlagship = false)
    {
        return new PropertyGroupMembership
        {
            PropertyId = propertyId,
            GroupId = groupId,
            IsFlagship = isFlagship,
            JoinDate = DateTime.UtcNow
        };
    }

    public void SetFlagship(bool isFlagship) => IsFlagship = isFlagship;
}

public class RateCopyJob : AuditableEntity
{
    public Guid SourcePropertyId { get; private set; }
    public List<Guid> TargetPropertyIds { get; private set; } = new();
    public List<Guid> RatePlanIds { get; private set; } = new();
    public List<Guid>? SeasonIds { get; private set; }
    public decimal RateAdjustmentPercentage { get; private set; }
    public bool OverrideExisting { get; private set; }
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public Guid ExecutedByUserId { get; private set; }
    public RateCopyJobStatus Status { get; private set; } = RateCopyJobStatus.Pending;
    public string? ErrorMessage { get; private set; }
    public int TotalRatesCopied { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private RateCopyJob() { }

    public static RateCopyJob Create(
        Guid sourcePropertyId,
        List<Guid> targetPropertyIds,
        List<Guid> ratePlanIds,
        Guid executedByUserId,
        decimal rateAdjustmentPercentage = 0,
        bool overrideExisting = false,
        List<Guid>? seasonIds = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        if (!targetPropertyIds.Any())
            throw new ArgumentException("At least one target property is required.", nameof(targetPropertyIds));

        return new RateCopyJob
        {
            SourcePropertyId = sourcePropertyId,
            TargetPropertyIds = targetPropertyIds,
            RatePlanIds = ratePlanIds,
            ExecutedByUserId = executedByUserId,
            RateAdjustmentPercentage = rateAdjustmentPercentage,
            OverrideExisting = overrideExisting,
            SeasonIds = seasonIds,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }

    public void MarkInProgress() => Status = RateCopyJobStatus.InProgress;
    public void MarkCompleted(int ratesCopied)
    {
        Status = RateCopyJobStatus.Completed;
        TotalRatesCopied = ratesCopied;
        CompletedAt = DateTime.UtcNow;
    }
    public void MarkFailed(string errorMessage)
    {
        Status = RateCopyJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum RateCopyJobStatus { Pending, InProgress, Completed, Failed }

public class GroupInventoryAllocation : AuditableEntity
{
    public Guid GroupId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int AllocatedRooms { get; private set; }
    public int SellLimitPerProperty { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public PropertyGroup Group { get; private set; } = null!;
    public RoomType RoomType { get; private set; } = null!;

    private GroupInventoryAllocation() { }

    public static GroupInventoryAllocation Create(
        Guid groupId, Guid roomTypeId,
        DateTime startDate, DateTime endDate,
        int allocatedRooms, int sellLimitPerProperty,
        string? notes = null)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        return new GroupInventoryAllocation
        {
            GroupId = groupId,
            RoomTypeId = roomTypeId,
            StartDate = startDate,
            EndDate = endDate,
            AllocatedRooms = allocatedRooms,
            SellLimitPerProperty = sellLimitPerProperty,
            Notes = notes
        };
    }

    public void UpdateAllocation(int allocatedRooms, int sellLimitPerProperty)
    {
        AllocatedRooms = allocatedRooms;
        SellLimitPerProperty = sellLimitPerProperty;
    }

    public void Deactivate() => IsActive = false;
}
