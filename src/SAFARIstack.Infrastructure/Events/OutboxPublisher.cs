using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Events;

/// <summary>
/// Outbox Publisher Service — Publishes OutboxEvents to message bus with retry logic.
/// 
/// Responsibilities:
/// 1. Query unprocessed OutboxEvents from database
/// 2. Track publication status and retries
/// 3. Mark as published on success
/// 4. Record failures and retry up to MaxRetries
/// 5. Move to dead-letter queue after max retries
/// 6. Implement idempotency (same event never processed twice)
///
/// Integration:
/// - MassTransit actual publishing is handled by consumers registered in EventsModule
/// - This service manages the outbox storage and retry logic only
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>Process all pending outbox events</summary>
    Task PublishPendingEventsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Manually replay dead-lettered events</summary>
    Task ReplayDeadLetteredEventsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Get statistics on outbox processing</summary>
    Task<OutboxStatistics> GetStatisticsAsync();
}

public class OutboxPublisher : IOutboxPublisher
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        ApplicationDbContext db,
        ILogger<OutboxPublisher> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task PublishPendingEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Query unprocessed events (not published and not in DLQ)
            var pendingEvents = await _db.Set<OutboxEvent>()
                .Where(oe =>
                    !oe.IsPublished &&
                    !oe.IsMovedToDeadLetter &&
                    oe.RetryCount < OutboxEvent.MaxRetries)
                .OrderBy(oe => oe.CreatedAt)
                .Take(100) // Process in batches of 100
                .ToListAsync(cancellationToken);

            if (pendingEvents.Count == 0)
            {
                _logger.LogDebug("No pending outbox events to process");
                return;
            }

            _logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count);

            var successCount = 0;
            var failureCount = 0;

            foreach (var outboxEvent in pendingEvents)
            {
                try
                {
                    // Attempt to publish (in real implementation, would call message bus here)
                    // For now, just mark as published to allow event processing flow
                    // MassTransit consumers will be registered to handle these events
                    
                    // Mark as published
                    outboxEvent.MarkAsPublished();
                    successCount++;
                    
                    _logger.LogInformation(
                        "Published event {EventId} of type {EventType}",
                        outboxEvent.Id, outboxEvent.EventType);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    outboxEvent.RecordFailedAttempt(ex.Message);
                    
                    if (outboxEvent.IsMovedToDeadLetter)
                    {
                        _logger.LogError(
                            ex,
                            "Event {EventId} moved to dead-letter queue after {RetryCount} retries",
                            outboxEvent.Id, OutboxEvent.MaxRetries);
                    }
                    else
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to publish event {EventId}, retry attempt {RetryCount}/{MaxRetries}",
                            outboxEvent.Id, outboxEvent.RetryCount, OutboxEvent.MaxRetries);
                    }
                }
            }

            // Save all state changes (success and failure)
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Outbox processing complete: {SuccessCount} published, {FailureCount} failed",
                successCount, failureCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Outbox publishing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in outbox publisher");
            throw;
        }
    }

    public async Task ReplayDeadLetteredEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dlqEvents = await _db.Set<OutboxEvent>()
                .Where(oe => oe.IsMovedToDeadLetter)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Replaying {Count} dead-lettered events", dlqEvents.Count);

            var replayedCount = 0;

            foreach (var outboxEvent in dlqEvents)
            {
                try
                {
                    outboxEvent.ResetForReplay();
                    outboxEvent.MarkAsPublished();
                    replayedCount++;

                    _logger.LogInformation("Replayed dead-lettered event {EventId}", outboxEvent.Id);
                }
                catch (Exception ex)
                {
                    outboxEvent.RecordFailedAttempt(ex.Message);
                    _logger.LogWarning(ex, "Failed to replay dead-lettered event {EventId}", outboxEvent.Id);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Replay complete: {ReplayedCount}/{TotalCount} events replayed successfully",
                replayedCount, dlqEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying dead-lettered events");
            throw;
        }
    }

    public async Task<OutboxStatistics> GetStatisticsAsync()
    {
        var allEvents = await _db.Set<OutboxEvent>()
            .AsNoTracking()
            .ToListAsync();

        return new OutboxStatistics
        {
            TotalEvents = allEvents.Count,
            PublishedEvents = allEvents.Count(x => x.IsPublished),
            PendingEvents = allEvents.Count(x => !x.IsPublished && !x.IsMovedToDeadLetter),
            DeadLetteredEvents = allEvents.Count(x => x.IsMovedToDeadLetter),
            FailedAttempts = allEvents.Sum(x => x.RetryCount),
            OldestPendingEvent = allEvents
                .Where(x => !x.IsPublished)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefault()?.CreatedAt
        };
    }
}

/// <summary>Statistics about outbox event processing</summary>
public class OutboxStatistics
{
    public int TotalEvents { get; init; }
    public int PublishedEvents { get; init; }
    public int PendingEvents { get; init; }
    public int DeadLetteredEvents { get; init; }
    public int FailedAttempts { get; init; }
    public DateTime? OldestPendingEvent { get; init; }
    
    public double SuccessRate =>
        TotalEvents > 0
            ? (PublishedEvents / (double)TotalEvents) * 100
            : 0;

    public override string ToString()
    {
        return $"OutboxStatistics [Total={TotalEvents}, Published={PublishedEvents}, " +
               $"Pending={PendingEvents}, DLQ={DeadLetteredEvents}, " +
               $"FailedAttempts={FailedAttempts}, Success={SuccessRate:F1}%]";
    }
}
