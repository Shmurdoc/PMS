using SAFARIstack.Shared.Domain;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Property (Lodge/Hotel) aggregate root
/// </summary>
public class Property : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty; // SA Province
    public string? PostalCode { get; private set; }
    public string Country { get; private set; } = "South Africa";
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public TimeSpan CheckInTime { get; private set; } = new(14, 0, 0);
    public TimeSpan CheckOutTime { get; private set; } = new(10, 0, 0);
    public string Currency { get; private set; } = Money.DEFAULT_CURRENCY;
    public decimal VATRate { get; private set; } = Money.SA_VAT_RATE;
    public decimal TourismLevyRate { get; private set; } = Money.SA_TOURISM_LEVY_RATE;
    public string Timezone { get; private set; } = "Africa/Johannesburg";
    public bool IsActive { get; private set; } = true;

    private Property() { } // EF Core

    private Property(
        string name,
        string slug,
        string address,
        string city,
        string province)
    {
        Name = name;
        Slug = slug;
        Address = address;
        City = city;
        Province = province;
    }

    public static Property Create(
        string name,
        string slug,
        string address,
        string city,
        string province)
    {
        var property = new Property(name, slug, address, city, province);
        property.AddDomainEvent(new PropertyCreatedEvent(property.Id, name));
        return property;
    }

    public void UpdateDetails(
        string name,
        string address,
        string city,
        string province)
    {
        Name = name;
        Address = address;
        City = city;
        Province = province;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PropertyDeactivatedEvent(Id, Name));
    }
}

public record PropertyCreatedEvent(Guid PropertyId, string PropertyName) : DomainEvent;
public record PropertyDeactivatedEvent(Guid PropertyId, string PropertyName) : DomainEvent;
