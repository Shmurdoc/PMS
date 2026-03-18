using SAFARIstack.Shared.Domain;
using System.Text.Json.Nodes;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Outbox Event — Transactional Outbox pattern for reliable event publishing.
/// 
/// Pattern:
/// 1. Domain event occurs → OutboxEvent created in SAME database transaction
/// 2. Commits to database atomically with business data
/// 3. BackgroundService polls OutboxEvents regularly
/// 4. For each unprocessed event:
///    a. Publish to message bus (MassTransit)
///    b. On success, mark as Processed
///    c. On failure, increment RetryCount (max 5 retries)
///    d. Move to DeadLetterQueue if max retries exceeded
///
/// Guarantees:
/// - No event loss: event persisted before publish attempt
/// - No duplicate processing: idempotent key prevents re-processing
/// - At-least-once delivery: retries on failure
/// - Audit trail: all events logged with timestamps
/// </summary>
public class OutboxEvent : AuditableEntity
{
    public Guid Id { get; private set; }
    
    /// <summary>Type of event (fully qualified class name for deserialization)</summary>
    public string EventType { get; private set; } = string.Empty;
    
    /// <summary>Serialized event payload (JSON)</summary>
    public string EventData { get; private set; } = string.Empty;
    
    /// <summary>Property that triggered this event (multi-tenancy)</summary>
    public Guid PropertyId { get; private set; }
    
    /// <summary>Aggregate root that generated this event</summary>
    public string AggregateType { get; private set; } = string.Empty;
    
    /// <summary>ID of the aggregate that generated this event (for correlation)</summary>
    public Guid AggregateId { get; private set; }
    
    /// <summary>Whether this event has been published to message bus</summary>
    public bool IsPublished { get; private set; } = false;
    
    /// <summary>When the event was successfully published</summary>
    public DateTime? PublishedAt { get; private set; }
    
    /// <summary>Number of publish attempts</summary>
    public int RetryCount { get; private set; } = 0;
    
    /// <summary>Max retries allowed before moving to DLQ</summary>
    public const int MaxRetries = 5;
    
    /// <summary>Whether this event failed permanently and moved to DLQ</summary>
    public bool IsMovedToDeadLetter { get; private set; } = false;
    
    /// <summary>Error message if publication failed</summary>
    public string? ErrorMessage { get; private set; }
    
    /// <summary>Last attempt timestamp</summary>
    public DateTime? LastAttemptAt { get; private set; }
    
    /// <summary>Uniqueness key for idempotent processing</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;
    
    // Navigation
    public Property Property { get; private set; } = null!;

    private OutboxEvent() { } // EF Core

    /// <summary>Create a new outbox event from a domain event</summary>
    public static OutboxEvent Create(
        Guid propertyId,
        string aggregateType,
        Guid aggregateId,
        string eventType,
        string eventData,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(eventData))
            throw new ArgumentException("Event data is required.", nameof(eventData));

        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            AggregateType = aggregateType.Trim(),
            AggregateId = aggregateId,
            EventType = eventType.Trim(),
            EventData = eventData,
            IdempotencyKey = idempotencyKey,
            IsPublished = false,
            RetryCount = 0,
            IsMovedToDeadLetter = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Mark event as successfully published</summary>
    public void MarkAsPublished()
    {
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Record a failed publish attempt</summary>
    public void RecordFailedAttempt(string? errorMessage = null)
    {
        RetryCount++;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;

        if (RetryCount >= MaxRetries)
        {
            IsMovedToDeadLetter = true;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Move to dead letter queue (permanent failure)</summary>
    public void MoveToDeadLetter(string reason)
    {
        IsMovedToDeadLetter = true;
        ErrorMessage = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Reset retry count (for manual replay)</summary>
    public void ResetForReplay()
    {
        RetryCount = 0;
        IsPublished = false;
        PublishedAt = null;
        IsMovedToDeadLetter = false;
        ErrorMessage = null;
        LastAttemptAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"OutboxEvent [Id={Id}, Type={EventType}, Aggregate={AggregateType}#{AggregateId}, Published={IsPublished}, Retries={RetryCount}]";
    }
}
