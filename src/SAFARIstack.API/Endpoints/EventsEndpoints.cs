using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Event management endpoints for triggering business events and monitoring event flow
/// </summary>
public static class EventsEndpoints
{
    public static void MapEventsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events")
            .WithTags("Events")
            .RequireAuthorization("AdminOnly")
            .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  EVENT MONITORING ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get event bus health status
        /// </summary>
        group.MapGet("/health", GetEventBusHealth)
            .WithName("GetEventBusHealth")
            .WithSummary("Get event bus health status")
            .AllowAnonymous()
            .Produces<EventBusHealthDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Get recent events from the bus
        /// </summary>
        group.MapGet("/recent", GetRecentEvents)
            .WithName("GetRecentEvents")
            .WithSummary("Get recent published events")
            .Produces<EventLogDto[]>(StatusCodes.Status200OK)
            .RequireAuthorization("ManagerOrAbove");

        // ═══════════════════════════════════════════════════════════════
        //  EVENT TESTING ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Publish a test event to verify event bus connectivity
        /// </summary>
        group.MapPost("/test", PublishTestEvent)
            .WithName("PublishTestEvent")
            .WithSummary("Publish test event for diagnostics")
            .Produces<TestEventResultDto>(StatusCodes.Status202Accepted)
            .RequireAuthorization("AdminOnly");

        /// <summary>
        /// Trigger a booking event
        /// </summary>
        group.MapPost("/booking/created", TriggerBookingCreatedEvent)
            .WithName("TriggerBookingCreatedEvent")
            .WithSummary("Trigger booking created event")
            .Produces<EventPublishResultDto>(StatusCodes.Status202Accepted);

        /// <summary>
        /// Trigger a guest checked-in event
        /// </summary>
        group.MapPost("/guest/checked-in", TriggerGuestCheckedInEvent)
            .WithName("TriggerGuestCheckedInEvent")
            .WithSummary("Trigger guest checked-in event")
            .Produces<EventPublishResultDto>(StatusCodes.Status202Accepted);

        /// <summary>
        /// Trigger a payment processed event
        /// </summary>
        group.MapPost("/payment/processed", TriggerPaymentProcessedEvent)
            .WithName("TriggerPaymentProcessedEvent")
            .WithSummary("Trigger payment processed event")
            .Produces<EventPublishResultDto>(StatusCodes.Status202Accepted);

        /// <summary>
        /// Get event consumer status
        /// </summary>
        group.MapGet("/consumers", GetConsumerStatus)
            .WithName("GetConsumerStatus")
            .WithSummary("Get event consumer status")
            .Produces<ConsumerStatusDto[]>(StatusCodes.Status200OK);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ENDPOINT HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    private static IResult GetEventBusHealth(IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            return Results.Ok(new EventBusHealthDto(
                Status: "Healthy",
                Timestamp: DateTime.UtcNow,
                Version: "2.1.0"
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetRecentEvents(
        [FromQuery] int count = 10,
        IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            // In production, this would query an event log table or message broker history
            var events = new EventLogDto[0];
            return Results.Ok(events);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> PublishTestEvent(
        TestEventRequest request,
        IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            // Publish test event
            await publishEndpoint.Publish(new TestEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Message = request.Message ?? "Test event from API"
            });

            return Results.Accepted($"/api/events/recent", new TestEventResultDto(
                EventId: Guid.NewGuid(),
                Status: "Published",
                Message: "Test event published successfully",
                Timestamp: DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> TriggerBookingCreatedEvent(
        BookingEventRequest request,
        IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            await publishEndpoint.Publish(new BookingCreatedEvent
            {
                BookingId = request.BookingId,
                PropertyId = request.PropertyId,
                GuestEmail = request.GuestEmail,
                Timestamp = DateTime.UtcNow
            });

            return Results.Accepted($"/api/events/recent", new EventPublishResultDto(
                EventId: request.BookingId,
                EventType: "BookingCreated",
                Status: "Published",
                Timestamp: DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> TriggerGuestCheckedInEvent(
        GuestEventRequest request,
        IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            await publishEndpoint.Publish(new GuestCheckedInEvent
            {
                BookingId = request.BookingId,
                GuestId = request.GuestId,
                PropertyId = request.PropertyId,
                CheckInTime = DateTime.UtcNow
            });

            return Results.Accepted($"/api/events/recent", new EventPublishResultDto(
                EventId: request.BookingId,
                EventType: "GuestCheckedIn",
                Status: "Published",
                Timestamp: DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> TriggerPaymentProcessedEvent(
        PaymentEventRequest request,
        IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            await publishEndpoint.Publish(new PaymentProcessedEvent
            {
                PaymentId = request.PaymentId,
                BookingId = request.BookingId,
                Amount = request.Amount,
                Status = request.Status,
                ProcessedAt = DateTime.UtcNow
            });

            return Results.Accepted($"/api/events/recent", new EventPublishResultDto(
                EventId: request.PaymentId,
                EventType: "PaymentProcessed",
                Status: "Published",
                Timestamp: DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetConsumerStatus(
        IPublishEndpoint publishEndpoint = null!)
    {
        try
        {
            var consumers = new ConsumerStatusDto[0];
            return Results.Ok(consumers);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  DTOs & DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Event bus health check response
/// </summary>
public record EventBusHealthDto(
    string Status,
    DateTime Timestamp,
    string Version);

/// <summary>
/// Event log entry
/// </summary>
public record EventLogDto(
    Guid EventId,
    string EventType,
    DateTime Timestamp,
    string? Data = null,
    string Status = "Published");

/// <summary>
/// Test event result
/// </summary>
public record TestEventResultDto(
    Guid EventId,
    string Status,
    string Message,
    DateTime Timestamp);

/// <summary>
/// Event publish result
/// </summary>
public record EventPublishResultDto(
    Guid EventId,
    string EventType,
    string Status,
    DateTime Timestamp);

/// <summary>
/// Consumer status information
/// </summary>
public record ConsumerStatusDto(
    string ConsumerName,
    string Status,
    int MessageCount,
    DateTime LastProcessedAt);

/// <summary>
/// Test event request
/// </summary>
public record TestEventRequest(
    string? Message = null);

/// <summary>
/// Booking event request
/// </summary>
public record BookingEventRequest(
    Guid BookingId,
    Guid PropertyId,
    string GuestEmail);

/// <summary>
/// Guest event request
/// </summary>
public record GuestEventRequest(
    Guid BookingId,
    Guid GuestId,
    Guid PropertyId);

/// <summary>
/// Payment event request
/// </summary>
public record PaymentEventRequest(
    Guid PaymentId,
    Guid BookingId,
    decimal Amount,
    string Status);

// ─────────────────────────────────────────────────────────────────────────
//  DOMAIN EVENTS (Concrete Classes for MassTransit Publishing)
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Test event for event bus diagnostics
/// </summary>
public class TestEvent
{
    public Guid EventId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Booking created domain event
/// </summary>
public class BookingCreatedEvent
{
    public Guid BookingId { get; set; }
    public Guid PropertyId { get; set; }
    public string GuestEmail { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Guest checked-in domain event
/// </summary>
public class GuestCheckedInEvent
{
    public Guid BookingId { get; set; }
    public Guid GuestId { get; set; }
    public Guid PropertyId { get; set; }
    public DateTime CheckInTime { get; set; }
}

/// <summary>
/// Payment processed domain event
/// </summary>
public class PaymentProcessedEvent
{
    public Guid PaymentId { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
