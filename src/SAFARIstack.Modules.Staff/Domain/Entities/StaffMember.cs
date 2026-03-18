using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Modules.Staff.Domain.Entities;

/// <summary>
/// Staff member entity
/// </summary>
public class StaffMember : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public StaffRole Role { get; private set; }
    public string? IdNumber { get; private set; }
    public DateTime? EmploymentStartDate { get; private set; }
    public EmploymentType EmploymentType { get; private set; } = EmploymentType.Permanent;
    public decimal? HourlyRate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }

    // Navigation
    private readonly List<RfidCard> _rfidCards = new();
    public IReadOnlyCollection<RfidCard> RfidCards => _rfidCards.AsReadOnly();

    private StaffMember() { } // EF Core

    public static StaffMember Create(
        Guid propertyId,
        string email,
        string firstName,
        string lastName,
        StaffRole role)
    {
        var staff = new StaffMember
        {
            PropertyId = propertyId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role
        };

        staff.AddDomainEvent(new StaffMemberCreatedEvent(staff.Id, email, $"{firstName} {lastName}"));
        return staff;
    }

    public void AssignRfidCard(RfidCard card)
    {
        _rfidCards.Add(card);
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";
}

public enum StaffRole
{
    Owner,
    Manager,
    Receptionist,
    Housekeeping,
    Maintenance,
    Kitchen,
    Security
}

public enum EmploymentType
{
    Permanent,
    FixedTerm,
    Casual,
    Contractor
}

public record StaffMemberCreatedEvent(Guid StaffId, string Email, string FullName) : DomainEvent;
