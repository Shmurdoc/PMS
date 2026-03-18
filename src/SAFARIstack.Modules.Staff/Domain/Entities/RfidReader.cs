using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Modules.Staff.Domain.Entities;

/// <summary>
/// RFID Reader hardware registration and tracking
/// </summary>
public class RfidReader : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string ReaderSerial { get; private set; } = string.Empty;
    public string ReaderName { get; private set; } = string.Empty;
    public RfidReaderType ReaderType { get; private set; }
    public string? LocationDescription { get; private set; }
    public string? IpAddress { get; private set; }
    public string? MacAddress { get; private set; }
    public string? ApiKey { get; private set; } // X-Reader-API-Key for authentication
    public DateTime? LastSeenAt { get; private set; }
    public ReaderStatus Status { get; private set; } = ReaderStatus.Active;

    private RfidReader() { } // EF Core

    public static RfidReader Create(
        Guid propertyId,
        string readerSerial,
        string readerName,
        RfidReaderType readerType,
        string apiKey)
    {
        var reader = new RfidReader
        {
            PropertyId = propertyId,
            ReaderSerial = readerSerial,
            ReaderName = readerName,
            ReaderType = readerType,
            ApiKey = apiKey,
            Status = ReaderStatus.Active
        };

        reader.AddDomainEvent(new RfidReaderRegisteredEvent(reader.Id, readerSerial, readerName));
        return reader;
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
        if (Status == ReaderStatus.Offline)
        {
            Status = ReaderStatus.Active;
            AddDomainEvent(new RfidReaderOnlineEvent(Id, ReaderSerial));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called by the heartbeat endpoint to update reader's last seen timestamp.
    /// </summary>
    public void RecordHeartbeat()
    {
        UpdateLastSeen();
    }

    public void MarkOffline()
    {
        Status = ReaderStatus.Offline;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new RfidReaderOfflineEvent(Id, ReaderSerial));
    }

    public bool ValidateApiKey(string providedApiKey)
    {
        return ApiKey == providedApiKey;
    }
}

public enum RfidReaderType
{
    Fixed,
    Mobile,
    Handheld
}

public enum ReaderStatus
{
    Active,
    Offline,
    Maintenance
}

public record RfidReaderRegisteredEvent(Guid ReaderId, string ReaderSerial, string ReaderName) : DomainEvent;
public record RfidReaderOnlineEvent(Guid ReaderId, string ReaderSerial) : DomainEvent;
public record RfidReaderOfflineEvent(Guid ReaderId, string ReaderSerial) : DomainEvent;
