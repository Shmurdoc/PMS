using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Guest entity with SA-specific ID types and loyalty tracking
/// </summary>
public class Guest : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? IdNumber { get; private set; } // SA ID or passport
    public IdType IdType { get; private set; } = IdType.SAId;
    public DateTime? DateOfBirth { get; private set; }
    public string? Nationality { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public string? CompanyName { get; private set; }
    public string? CompanyVATNumber { get; private set; }
    public GuestType GuestType { get; private set; } = GuestType.Individual;
    public bool MarketingOptIn { get; private set; }
    public bool IsBlacklisted { get; private set; }
    public string? BlacklistReason { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public Property Property { get; private set; } = null!;
    public GuestLoyalty? Loyalty { get; private set; }
    private readonly List<GuestPreference> _preferences = new();
    public IReadOnlyCollection<GuestPreference> Preferences => _preferences.AsReadOnly();
    private readonly List<Booking> _bookings = new();
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();
    private readonly List<GuestFeedback> _feedbacks = new();
    public IReadOnlyCollection<GuestFeedback> Feedbacks => _feedbacks.AsReadOnly();

    private Guest() { } // EF Core

    private Guest(
        Guid propertyId,
        string firstName,
        string lastName,
        string? email,
        string? phone)
    {
        PropertyId = propertyId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
    }

    public static Guest Create(
        Guid propertyId,
        string firstName,
        string lastName,
        string? email,
        string? phone)
    {
        var guest = new Guest(propertyId, firstName, lastName, email, phone);
        guest.AddDomainEvent(new GuestCreatedEvent(guest.Id, $"{firstName} {lastName}", propertyId));
        return guest;
    }

    public void UpdateContactInfo(string? email, string? phone)
    {
        Email = email;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateIdInfo(string idNumber, IdType idType, DateTime? dateOfBirth)
    {
        IdNumber = idNumber;
        IdType = idType;
        DateOfBirth = dateOfBirth;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCompanyInfo(string companyName, string? vatNumber)
    {
        CompanyName = companyName;
        CompanyVATNumber = vatNumber;
        GuestType = GuestType.Corporate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPreference(GuestPreference preference)
    {
        _preferences.Add(preference);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Blacklist(string reason)
    {
        IsBlacklisted = true;
        BlacklistReason = reason;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new GuestBlacklistedEvent(Id, FullName, reason));
    }

    public void RemoveFromBlacklist()
    {
        IsBlacklisted = false;
        BlacklistReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";
}

public enum IdType
{
    SAId,           // South African ID
    Passport,       // International Passport
    DriversLicense, // Drivers License
    Other
}

public enum GuestType
{
    Individual,
    Corporate,
    TravelAgent,
    GroupLeader,
    VIP
}

// ─── Domain Events ───────────────────────────────────────────────────
public record GuestCreatedEvent(Guid GuestId, string FullName, Guid PropertyId) : DomainEvent;
public record GuestBlacklistedEvent(Guid GuestId, string FullName, string Reason) : DomainEvent;
