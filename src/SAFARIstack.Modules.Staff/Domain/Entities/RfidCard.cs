using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Modules.Staff.Domain.Entities;

/// <summary>
/// RFID Card entity for staff attendance tracking
/// </summary>
public class RfidCard : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid StaffId { get; private set; }
    public string CardUid { get; private set; } = string.Empty; // 7-byte UID in hex
    public RfidCardType CardType { get; private set; } = RfidCardType.Card;
    public RfidCardStatus Status { get; private set; } = RfidCardStatus.Active;
    public DateTime IssueDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public StaffMember StaffMember { get; private set; } = null!;

    private RfidCard() { } // EF Core

    public static RfidCard Create(
        Guid staffId,
        string cardUid,
        RfidCardType cardType,
        Guid propertyId)
    {
        var card = new RfidCard
        {
            StaffId = staffId,
            CardUid = cardUid.ToUpperInvariant(),
            CardType = cardType,
            IssueDate = DateTime.UtcNow,
            Status = RfidCardStatus.Active,
            PropertyId = propertyId
        };

        card.AddDomainEvent(new RfidCardIssuedEvent(card.Id, staffId, cardUid));
        return card;
    }

    public void Deactivate(string reason)
    {
        Status = RfidCardStatus.Deactivated;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReportLost()
    {
        Status = RfidCardStatus.Lost;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new RfidCardLostEvent(Id, StaffId, CardUid));
    }

    public void ReportStolen()
    {
        Status = RfidCardStatus.Stolen;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new RfidCardStolenEvent(Id, StaffId, CardUid));
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}

public enum RfidCardType
{
    Card,
    Wristband,
    Keyfob
}

public enum RfidCardStatus
{
    Active,
    Lost,
    Stolen,
    Deactivated,
    Expired
}

public record RfidCardIssuedEvent(Guid CardId, Guid StaffId, string CardUid) : DomainEvent;
public record RfidCardLostEvent(Guid CardId, Guid StaffId, string CardUid) : DomainEvent;
public record RfidCardStolenEvent(Guid CardId, Guid StaffId, string CardUid) : DomainEvent;
